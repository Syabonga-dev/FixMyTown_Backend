using FixMyTownApi.Data;
using FixMyTownApi.Models.Dtos.Admin;
using FixMyTownApi.Models.Dtos.Common;
using FixMyTownApi.Models.Dtos.Issues;
using FixMyTownApi.Models.Entities;
using FixMyTownApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FixMyTownApi.Controllers
{
    [Route("api/admin")]
    [ApiController]
    [Authorize(Roles = "admin")]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly PasswordService _passwords;
        private readonly ReportNumberGenerator _refNumbers;

        public AdminController(
            AppDbContext db,
            PasswordService passwords,
            ReportNumberGenerator refNumbers)
        {
            _db = db;
            _passwords = passwords;
            _refNumbers = refNumbers;
        }

        // =========================================================
        // DASHBOARD
        // =========================================================

        [HttpGet("dashboard")]
        public async Task<ActionResult<AdminDashboardDto>> GetDashboard()
        {
            var counts = new DashboardCountsDto
            {
                TotalReports = await _db.Reports
                    .CountAsync(r => r.DeletedAt == null),

                Pending = await _db.Reports
                    .CountAsync(r =>
                        r.Status == "Reported" &&
                        r.DeletedAt == null),

                InProgress = await _db.Reports
                    .CountAsync(r =>
                        r.Status == "InProgress" &&
                        r.DeletedAt == null),

                Resolved = await _db.Reports
                    .CountAsync(r =>
                        r.Status == "Resolved" &&
                        r.DeletedAt == null)
            };

            var recentReports = await GetReportReadDtos(
                _db.Reports
                    .Where(r => r.DeletedAt == null)
                    .OrderByDescending(r => r.CreatedAt)
                    .Take(5)
            );

            var workerOverview = await _db.Users
                .Where(u =>
                    u.Role == "Worker" &&
                    u.IsActive)
                .Select(w => new WorkerOverviewDto
                {
                    WorkerName =
                        w.FirstName + " " + w.LastName,

                    DepartmentName =
                        _db.WorkerDepartments
                            .Where(wd =>
                                wd.UserId == w.UserId &&
                                wd.IsPrimary)
                            .Select(wd =>
                                wd.Department.DisplayName)
                            .FirstOrDefault()
                        ?? "Unassigned",

                    ActiveIssues =
                        _db.Assignments
                            .Count(a =>
                                a.WorkerId == w.UserId &&
                                a.IsActive)
                })
                .ToListAsync();

            return Ok(
                new AdminDashboardDto
                {
                    counts = counts,
                    recentReports = recentReports,
                    workerOverview = workerOverview
                }
            );
        }


        // =========================================================
        // ALL REPORTS
        // =========================================================

        [HttpGet("reports")]
        public async Task<ActionResult<IEnumerable<IssueReadDto>>> GetAllReports(
            [FromQuery] string? status,
            [FromQuery] string? search)
        {
            var query = _db.Reports
                .Where(r => r.DeletedAt == null);

            if (!string.IsNullOrWhiteSpace(status) &&
                status != "All Status")
            {
                var dbStatus = StatusMapper.ToDb(status);

                query = query.Where(r =>
                    r.Status == dbStatus);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(r =>
                    r.ReferenceNumber.Contains(search) ||
                    r.Title.Contains(search) ||
                    (r.Location.AddressDescription != null &&
                     r.Location.AddressDescription.Contains(search)));
            }

            var reports = await GetReportReadDtos(
                query.OrderByDescending(r => r.CreatedAt)
            );

            return Ok(reports);
        }


        // =========================================================
        // UNASSIGNED REPORTS
        // =========================================================

        [HttpGet("reports/unassigned")]
        public async Task<ActionResult<IEnumerable<IssueReadDto>>>
            GetUnassignedReports()
        {
            var reports = await GetReportReadDtos(
                _db.Reports
                    .Where(r =>
                        r.Status == "Reported" &&
                        r.DeletedAt == null)
                    .OrderByDescending(r => r.CreatedAt)
            );

            return Ok(reports);
        }


        // =========================================================
        // GET SINGLE REPORT
        // =========================================================

        [HttpGet("reports/{id:int}")]
        public async Task<ActionResult<IssueDetailDto>>
            GetReportById(int id)
        {
            var report = await _db.Reports
                .Include(r => r.Category)
                    .ThenInclude(c => c.Department)

                .Include(r => r.Location)

                .Include(r => r.Photos)

                .Include(r => r.ProgressUpdates)

                .FirstOrDefaultAsync(r =>
                    r.ReportId == id &&
                    r.DeletedAt == null);

            if (report == null)
            {
                return NotFound(
                    new ApiMessageDto("Report not found.")
                );
            }

            var workerIds = report.ProgressUpdates
                .Select(u => u.WorkerId)
                .Distinct()
                .ToList();

            var workerNames = await _db.Users
                .Where(u => workerIds.Contains(u.UserId))
                .ToDictionaryAsync(
                    u => u.UserId,
                    u => $"{u.FirstName} {u.LastName}"
                );

            var result = new IssueDetailDto
            {
                ReportID = report.ReportId,

                ReportCode = report.ReferenceNumber,

                Title = report.Title,

                Description = report.Description,

                Status = StatusMapper.ToApp(report.Status),

                Priority = report.Priority,

                Latitude =
                    report.Location?.Latitude ?? 0,

                Longitude =
                    report.Location?.Longitude ?? 0,

                LocationName =
                    report.Location?.AddressDescription,

                CreatedAt = report.CreatedAt,

                UpdatedAt =
                    report.UpdatedAt ??
                    report.CreatedAt,

                CategoryName =
                    report.Category?.DisplayName,

                DepartmentName =
                    report.Category?.Department?.DisplayName,

                Photos =
                    report.Photos
                        .Select(p => p.FileUrl)
                        .Where(url =>
                            !string.IsNullOrWhiteSpace(url))
                        .ToList(),

                Updates =
                    report.ProgressUpdates
                        .OrderBy(u => u.CreatedAt)
                        .Select(u =>
                            new ProgressUpdateReadDto
                            {
                                Note = u.ProgressNote,

                                PhotoURL = null,

                                StatusAtUpdate =
                                    StatusMapper.ToApp(report.Status),

                                CreatedAt =
                                    u.CreatedAt,

                                WorkerName =
                                    workerNames.GetValueOrDefault(
                                        u.WorkerId,
                                        "Unknown Worker"
                                    )
                            })
                        .ToList()
            };

            return Ok(result);
        }


        // =========================================================
        // UPDATE REPORT
        // =========================================================

        [HttpPut("reports/{id:int}")]
        public async Task<ActionResult<ApiMessageDto>>
            UpdateReport(
                int id,
                [FromBody] IssueUpdateDto dto)
        {
            var report = await _db.Reports
                .FirstOrDefaultAsync(r =>
                    r.ReportId == id &&
                    r.DeletedAt == null);

            if (report == null)
            {
                return NotFound(
                    new ApiMessageDto("Report not found.")
                );
            }

            report.Title = dto.Title;
            report.Description = dto.Description;
            report.Priority = dto.Priority;
            report.UpdatedAt = DateTime.UtcNow;
            report.UpdatedBy = User.CurrentUserId();

            await _db.SaveChangesAsync();

            return Ok(
                new ApiMessageDto(
                    "Report updated successfully."
                )
            );
        }


        // =========================================================
        // DELETE REPORT
        // =========================================================

        [HttpDelete("reports/{id:int}")]
        public async Task<ActionResult<ApiMessageDto>>
            DeleteReport(int id)
        {
            var report = await _db.Reports
                .FirstOrDefaultAsync(r =>
                    r.ReportId == id &&
                    r.DeletedAt == null);

            if (report == null)
            {
                return NotFound(
                    new ApiMessageDto("Report not found.")
                );
            }

            report.DeletedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return Ok(
                new ApiMessageDto(
                    "Report deleted successfully."
                )
            );
        }


        // =========================================================
        // ASSIGN REPORT
        // =========================================================

        [HttpPost("reports/{id:int}/assign")]
        public async Task<ActionResult<ApiMessageDto>>
            AssignReport(
                int id,
                [FromBody] AssignReportDto dto)
        {
            try
            {
                // -------------------------------------------------
                // Validate DTO
                // -------------------------------------------------

                if (dto == null)
                {
                    return BadRequest(
                        new ApiMessageDto(
                            "Assignment data is required."
                        )
                    );
                }

                if (dto.WorkerId <= 0)
                {
                    return BadRequest(
                        new ApiMessageDto(
                            "Please choose a valid worker."
                        )
                    );
                }


                // -------------------------------------------------
                // Find report
                // -------------------------------------------------

                var report = await _db.Reports
                    .FirstOrDefaultAsync(r =>
                        r.ReportId == id &&
                        r.DeletedAt == null);

                if (report == null)
                {
                    return NotFound(
                        new ApiMessageDto(
                            "Report not found."
                        )
                    );
                }


                // -------------------------------------------------
                // Find worker
                // -------------------------------------------------

                var worker = await _db.Users
                    .FirstOrDefaultAsync(u =>
                        u.UserId == dto.WorkerId &&
                        u.Role == "Worker" &&
                        u.IsActive);

                if (worker == null)
                {
                    return BadRequest(
                        new ApiMessageDto(
                            "Please choose a valid active worker."
                        )
                    );
                }


                // -------------------------------------------------
                // Get logged-in administrator
                // -------------------------------------------------

                var adminId = User.CurrentUserId();

                if (adminId <= 0)
                {
                    return Unauthorized(
                        new ApiMessageDto(
                            "Could not identify the logged-in administrator."
                        )
                    );
                }


                // -------------------------------------------------
                // Verify administrator exists
                // -------------------------------------------------

                var adminExists = await _db.Users
                    .AnyAsync(u =>
                        u.UserId == adminId);

                if (!adminExists)
                {
                    return Unauthorized(
                        new ApiMessageDto(
                            "Administrator account could not be found."
                        )
                    );
                }


                // -------------------------------------------------
                // Check existing active assignment
                // -------------------------------------------------

                var existingAssignment =
                    await _db.Assignments
                        .FirstOrDefaultAsync(a =>
                            a.ReportId == id &&
                            a.IsActive);

                if (existingAssignment != null)
                {
                    return BadRequest(
                        new ApiMessageDto(
                            "This report is already assigned to a worker."
                        )
                    );
                }


                // -------------------------------------------------
                // Create assignment
                // -------------------------------------------------

                var assignment = new Assignment
                {
                    ReportId = id,
                    WorkerId = dto.WorkerId,
                    AssignedById = adminId,
                    AssignedAt = DateTime.UtcNow,
                    IsActive = true,
                    Notes = dto.Note
                };

                _db.Assignments.Add(assignment);


                // -------------------------------------------------
                // Update report
                // -------------------------------------------------

                var oldStatus = report.Status;

                report.Status = "Assigned";
                report.UpdatedAt = DateTime.UtcNow;
                report.UpdatedBy = adminId;


                // -------------------------------------------------
                // Status history
                // -------------------------------------------------

                var statusHistory =
                    new ReportStatusHistory
                    {
                        ReportId = id,

                        OldStatus = oldStatus,

                        NewStatus = "Assigned",

                        ChangedById = adminId,

                        Comment =
                            string.IsNullOrWhiteSpace(dto.Note)
                                ? "Assigned to worker."
                                : dto.Note
                    };

                _db.ReportStatusHistories.Add(
                    statusHistory
                );


                // -------------------------------------------------
                // Notify citizen
                // -------------------------------------------------

                var notification =
                    new Notification
                    {
                        UserId = report.CitizenId,

                        ReportId = id,

                        Type = "StatusUpdate",

                        Title = "Report Assigned",

                        Message =
                            $"Your report \"{report.Title}\" " +
                            $"({report.ReferenceNumber}) " +
                            $"has been assigned to a worker."
                    };

                _db.Notifications.Add(
                    notification
                );


                // -------------------------------------------------
                // Save everything
                // -------------------------------------------------

                await _db.SaveChangesAsync();


                return Ok(
                    new ApiMessageDto(
                        "Issue assigned successfully."
                    )
                );
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine(
                    "========================================"
                );

                Console.WriteLine(
                    "ASSIGNMENT DATABASE ERROR"
                );

                Console.WriteLine(
                    ex.Message
                );

                Console.WriteLine(
                    "----------------------------------------"
                );

                if (ex.InnerException != null)
                {
                    Console.WriteLine(
                        "INNER EXCEPTION:"
                    );

                    Console.WriteLine(
                        ex.InnerException.Message
                    );
                }

                Console.WriteLine(
                    "========================================"
                );

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new ApiMessageDto(
                        "The issue could not be assigned because of a database error."
                    )
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    "========================================"
                );

                Console.WriteLine(
                    "ASSIGNMENT ERROR"
                );

                Console.WriteLine(
                    ex.Message
                );

                Console.WriteLine(
                    "----------------------------------------"
                );

                if (ex.InnerException != null)
                {
                    Console.WriteLine(
                        "INNER EXCEPTION:"
                    );

                    Console.WriteLine(
                        ex.InnerException.Message
                    );
                }

                Console.WriteLine(
                    "========================================"
                );

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new ApiMessageDto(
                        "An unexpected error occurred while assigning the issue."
                    )
                );
            }
        }


        // =========================================================
        // WORKERS
        // =========================================================

        [HttpGet("workers")]
        public async Task<ActionResult<IEnumerable<WorkerReadDto>>>
            GetWorkers()
        {
            var workers = await _db.Users
                .Where(u => u.Role == "Worker")
                .OrderBy(u => u.FirstName)
                .Select(w => new WorkerReadDto
                {
                    WorkerID = w.UserId,

                    UserID = w.UserId,

                    FullName =
                        w.FirstName + " " + w.LastName,

                    Email = w.Email,

                    Phone = w.PhoneNumber,

                    IsActive = w.IsActive,

                    DepartmentID =
                        _db.WorkerDepartments
                            .Where(wd =>
                                wd.UserId == w.UserId &&
                                wd.IsPrimary)
                            .Select(wd =>
                                wd.DepartmentId)
                            .FirstOrDefault(),

                    DepartmentName =
                        _db.WorkerDepartments
                            .Where(wd =>
                                wd.UserId == w.UserId &&
                                wd.IsPrimary)
                            .Select(wd =>
                                wd.Department.DisplayName)
                            .FirstOrDefault()
                        ?? "Unassigned",

                    ActiveIssues =
                        _db.Assignments
                            .Count(a =>
                                a.WorkerId == w.UserId &&
                                a.IsActive)
                })
                .ToListAsync();

            return Ok(workers);
        }


        [HttpGet("workers/{id:int}")]
        public async Task<ActionResult<WorkerReadDto>>
            GetWorkerById(int id)
        {
            var worker = await _db.Users
                .FirstOrDefaultAsync(u =>
                    u.UserId == id &&
                    u.Role == "Worker");

            if (worker == null)
            {
                return NotFound(
                    new ApiMessageDto(
                        "Worker not found."
                    )
                );
            }

            var primaryDept =
                await _db.WorkerDepartments
                    .Include(wd => wd.Department)
                    .FirstOrDefaultAsync(wd =>
                        wd.UserId == id &&
                        wd.IsPrimary);

            return Ok(
                new WorkerReadDto
                {
                    WorkerID = worker.UserId,

                    UserID = worker.UserId,

                    FullName =
                        $"{worker.FirstName} {worker.LastName}",

                    Email = worker.Email,

                    Phone = worker.PhoneNumber,

                    IsActive = worker.IsActive,

                    DepartmentID =
                        primaryDept?.DepartmentId ?? 0,

                    DepartmentName =
                        primaryDept?.Department?.DisplayName
                        ?? "Unassigned",

                    ActiveIssues =
                        await _db.Assignments
                            .CountAsync(a =>
                                a.WorkerId == id &&
                                a.IsActive)
                }
            );
        }


        // =========================================================
        // CREATE WORKER
        // =========================================================

        [HttpPost("workers")]
        public async Task<ActionResult<ApiMessageDto>>
            CreateWorker(
                [FromBody] WorkerCreateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.FullName))
            {
                return BadRequest(
                    new ApiMessageDto(
                        "Full name is required."
                    )
                );
            }

            var department =
                await _db.Departments
                    .FindAsync(dto.DepartmentId);

            if (department == null)
            {
                return BadRequest(
                    new ApiMessageDto(
                        "Please choose a valid department."
                    )
                );
            }

            var email =
                string.IsNullOrWhiteSpace(dto.Email)
                    ? $"{dto.FullName.ToLower().Replace(" ", ".")}@grp0339.gov.za"
                    : dto.Email.Trim();

            if (await _db.Users.AnyAsync(
                u => u.Email == email))
            {
                return Conflict(
                    new ApiMessageDto(
                        "A user with that email already exists."
                    )
                );
            }

            var generatedPassword =
                string.IsNullOrWhiteSpace(dto.Password)
                    ? Guid.NewGuid().ToString("N")[..8] + "!Rv"
                    : dto.Password;

            var fullName =
                dto.FullName.Trim();

            var spaceIndex =
                fullName.IndexOf(' ');

            var firstName =
                spaceIndex < 0
                    ? fullName
                    : fullName[..spaceIndex];

            var lastName =
                spaceIndex < 0
                    ? string.Empty
                    : fullName[(spaceIndex + 1)..];

            var user = new User
            {
                FirstName = firstName,

                LastName = lastName,

                Email = email,

                PasswordHash =
                    _passwords.Hash(generatedPassword),

                PhoneNumber = dto.Phone,

                Role = "Worker",

                IsVerified = true,

                IsActive = true
            };

            _db.Users.Add(user);

            await _db.SaveChangesAsync();

            _db.WorkerDepartments.Add(
                new WorkerDepartment
                {
                    UserId = user.UserId,

                    DepartmentId =
                        dto.DepartmentId,

                    IsPrimary = true
                }
            );

            await _db.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetWorkerById),
                new { id = user.UserId },
                new
                {
                    message =
                        "Worker registered successfully.",

                    email = user.Email,

                    generatedPassword =
                        string.IsNullOrWhiteSpace(dto.Password)
                            ? generatedPassword
                            : null
                }
            );
        }


        // =========================================================
        // UPDATE WORKER
        // =========================================================

        [HttpPut("workers/{id:int}")]
        public async Task<ActionResult<ApiMessageDto>>
            UpdateWorker(
                int id,
                [FromBody] WorkerUpdateDto dto)
        {
            var worker = await _db.Users
                .FirstOrDefaultAsync(u =>
                    u.UserId == id &&
                    u.Role == "Worker");

            if (worker == null)
            {
                return NotFound(
                    new ApiMessageDto(
                        "Worker not found."
                    )
                );
            }

            if (string.IsNullOrWhiteSpace(dto.FullName))
            {
                return BadRequest(
                    new ApiMessageDto(
                        "Full name is required."
                    )
                );
            }

            var fullName =
                dto.FullName.Trim();

            var spaceIndex =
                fullName.IndexOf(' ');

            worker.FirstName =
                spaceIndex < 0
                    ? fullName
                    : fullName[..spaceIndex];

            worker.LastName =
                spaceIndex < 0
                    ? string.Empty
                    : fullName[(spaceIndex + 1)..];

            worker.PhoneNumber = dto.Phone;

            worker.IsActive = dto.IsActive;


            var existingPrimary =
                await _db.WorkerDepartments
                    .Where(wd =>
                        wd.UserId == id &&
                        wd.IsPrimary)
                    .ToListAsync();

            _db.WorkerDepartments.RemoveRange(
                existingPrimary
            );

            _db.WorkerDepartments.Add(
                new WorkerDepartment
                {
                    UserId = id,

                    DepartmentId =
                        dto.DepartmentId,

                    IsPrimary = true
                }
            );

            await _db.SaveChangesAsync();

            return Ok(
                new ApiMessageDto(
                    "Worker updated successfully."
                )
            );
        }


        // =========================================================
        // DELETE / DEACTIVATE WORKER
        // =========================================================

        [HttpDelete("workers/{id:int}")]
        public async Task<ActionResult<ApiMessageDto>>
            DeleteWorker(int id)
        {
            var worker = await _db.Users
                .FirstOrDefaultAsync(u =>
                    u.UserId == id &&
                    u.Role == "Worker");

            if (worker == null)
            {
                return NotFound(
                    new ApiMessageDto(
                        "Worker not found."
                    )
                );
            }

            var stillAssigned =
                await _db.Assignments
                    .AnyAsync(a =>
                        a.WorkerId == id &&
                        a.IsActive);

            if (stillAssigned)
            {
                return BadRequest(
                    new ApiMessageDto(
                        "This worker still has active issues assigned - reassign them first."
                    )
                );
            }

            worker.IsActive = false;
            worker.DeletedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return Ok(
                new ApiMessageDto(
                    "Worker removed successfully."
                )
            );
        }


        // =========================================================
        // DEPARTMENTS
        // =========================================================

        [HttpGet("departments")]
        public async Task<ActionResult<IEnumerable<DepartmentReadDto>>>
            GetDepartments()
        {
            var departments = await _db.Departments
                .OrderBy(d => d.DisplayName)
                .Select(d => new DepartmentReadDto
                {
                    DepartmentID =
                        d.DepartmentId,

                    Name =
                        d.DisplayName,

                    OpenIssues =
                        _db.Reports.Count(r =>
                            r.Category.DepartmentId ==
                                d.DepartmentId &&
                            r.Status != "Resolved" &&
                            r.DeletedAt == null),

                    WorkerCount =
                        d.WorkerDepartments.Count(
                            wd => wd.IsPrimary)
                })
                .ToListAsync();

            return Ok(departments);
        }


        [HttpPost("departments")]
        public async Task<ActionResult<ApiMessageDto>>
            CreateDepartment(
                [FromBody] string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return BadRequest(
                    new ApiMessageDto(
                        "Department name is required."
                    )
                );
            }

            name = name.Trim();

            if (await _db.Departments.AnyAsync(
                d => d.DisplayName == name))
            {
                return Conflict(
                    new ApiMessageDto(
                        "A department with that name already exists."
                    )
                );
            }

            _db.Departments.Add(
                new Department
                {
                    DepartmentName =
                        name.Replace(" ", ""),

                    DisplayName = name
                }
            );

            await _db.SaveChangesAsync();

            return Ok(
                new ApiMessageDto(
                    "Department created successfully."
                )
            );
        }


        [HttpPut("departments/{id:int}")]
        public async Task<ActionResult<ApiMessageDto>>
            UpdateDepartment(
                int id,
                [FromBody] string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return BadRequest(
                    new ApiMessageDto(
                        "Department name is required."
                    )
                );
            }

            var department =
                await _db.Departments.FindAsync(id);

            if (department == null)
            {
                return NotFound(
                    new ApiMessageDto(
                        "Department not found."
                    )
                );
            }

            department.DisplayName =
                name.Trim();

            await _db.SaveChangesAsync();

            return Ok(
                new ApiMessageDto(
                    "Department updated successfully."
                )
            );
        }


        [HttpDelete("departments/{id:int}")]
        public async Task<ActionResult<ApiMessageDto>>
            DeleteDepartment(int id)
        {
            var department =
                await _db.Departments.FindAsync(id);

            if (department == null)
            {
                return NotFound(
                    new ApiMessageDto(
                        "Department not found."
                    )
                );
            }

            var inUse =
                await _db.WorkerDepartments
                    .AnyAsync(wd =>
                        wd.DepartmentId == id)

                ||

                await _db.Categories
                    .AnyAsync(c =>
                        c.DepartmentId == id);

            if (inUse)
            {
                return BadRequest(
                    new ApiMessageDto(
                        "This department still has workers or categories linked to it."
                    )
                );
            }

            _db.Departments.Remove(
                department
            );

            await _db.SaveChangesAsync();

            return Ok(
                new ApiMessageDto(
                    "Department deleted successfully."
                )
            );
        }


        // =========================================================
        // ANALYTICS
        // =========================================================

        [HttpGet("analytics")]
        public async Task<ActionResult<AnalyticsDto>>
            GetAnalytics()
        {
            var totalIssues =
                await _db.Reports
                    .CountAsync(r =>
                        r.DeletedAt == null);

            var resolvedCount =
                await _db.Reports
                    .CountAsync(r =>
                        r.Status == "Resolved" &&
                        r.DeletedAt == null);

            var byCategory =
                await _db.Reports
                    .Where(r =>
                        r.DeletedAt == null)
                    .GroupBy(r =>
                        r.Category.DisplayName)
                    .Select(g =>
                        new CategoryCountDto
                        {
                            CategoryName = g.Key,
                            IssueCount = g.Count()
                        })
                    .OrderByDescending(c =>
                        c.IssueCount)
                    .ToListAsync();

            var byLocation =
                await _db.Reports
                    .Where(r =>
                        r.DeletedAt == null &&
                        r.Location.AddressDescription != null)
                    .GroupBy(r =>
                        r.Location.AddressDescription)
                    .Select(g =>
                        new LocationCountDto
                        {
                            LocationName = g.Key,
                            IssueCount = g.Count()
                        })
                    .OrderByDescending(l =>
                        l.IssueCount)
                    .ToListAsync();

            var resolvedReports =
                await _db.Reports
                    .Where(r =>
                        r.Status == "Resolved" &&
                        r.DeletedAt == null)
                    .ToListAsync();

            var avgResolutionDays =
                resolvedReports.Count > 0
                    ? resolvedReports.Average(r =>
                        (
                            (r.ResolvedAt ??
                             r.UpdatedAt ??
                             r.CreatedAt)
                            - r.CreatedAt
                        ).TotalDays)
                    : 0;

            return Ok(
                new AnalyticsDto
                {
                    TotalIssues =
                        totalIssues,

                    TotalWorkers =
                        await _db.Users
                            .CountAsync(u =>
                                u.Role == "Worker" &&
                                u.IsActive),

                    ResolutionRate =
                        totalIssues == 0
                            ? 0
                            : (int)Math.Round(
                                100.0 *
                                resolvedCount /
                                totalIssues),

                    AvgResponseHours = 0,

                    AvgResolutionDays =
                        Math.Round(
                            avgResolutionDays,
                            1),

                    byCategory =
                        byCategory,

                    byLocation =
                        byLocation
                }
            );
        }


        // =========================================================
        // DYNAMIC REPORT QUERY
        // =========================================================

        private IQueryable<Report>
            BuildDynamicReportQuery(
                DateTime? from,
                DateTime? to,
                int? categoryId,
                int? departmentId,
                string? status)
        {
            var query =
                _db.Reports
                    .Where(r =>
                        r.DeletedAt == null);

            if (from.HasValue)
            {
                query = query.Where(r =>
                    r.CreatedAt.Date >=
                    from.Value.Date);
            }

            if (to.HasValue)
            {
                query = query.Where(r =>
                    r.CreatedAt.Date <=
                    to.Value.Date);
            }

            if (categoryId.HasValue)
            {
                query = query.Where(r =>
                    r.CategoryId ==
                    categoryId.Value);
            }

            if (departmentId.HasValue)
            {
                query = query.Where(r =>
                    r.Category.DepartmentId ==
                    departmentId.Value);
            }

            if (!string.IsNullOrWhiteSpace(status) &&
                status != "All Status")
            {
                var dbStatus =
                    StatusMapper.ToDb(status);

                query = query.Where(r =>
                    r.Status == dbStatus);
            }

            return query
                .OrderByDescending(r =>
                    r.CreatedAt);
        }

        // =========================================================
        // TOGGLE WORKER STATUS (ACTIVATE / DEACTIVATE)
        // =========================================================

        [HttpPut("workers/{id:int}/status")]
        public async Task<ActionResult<ApiMessageDto>>
            SetWorkerStatus(
                int id,
                [FromBody] WorkerStatusDto dto)
        {
            var worker = await _db.Users
                .FirstOrDefaultAsync(u =>
                    u.UserId == id &&
                    u.Role == "Worker");

            if (worker == null)
            {
                return NotFound(
                    new ApiMessageDto(
                        "Worker not found."
                    )
                );
            }

            if (!dto.IsActive)
            {
                var stillAssigned =
                    await _db.Assignments
                        .AnyAsync(a =>
                            a.WorkerId == id &&
                            a.IsActive);

                if (stillAssigned)
                {
                    return BadRequest(
                        new ApiMessageDto(
                            "This worker still has active issues assigned - reassign them first."
                        )
                    );
                }
            }

            worker.IsActive = dto.IsActive;
            worker.DeletedAt = dto.IsActive ? null : DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return Ok(
                new ApiMessageDto(
                    dto.IsActive
                        ? "Worker reactivated successfully."
                        : "Worker deactivated successfully."
                )
            );
        }


        // =========================================================
        // DYNAMIC REPORT
        // =========================================================

        [HttpGet("reports/dynamic")]
        public async Task<ActionResult<DynamicReportDto>>
            GetDynamicReport(
                [FromQuery] DateTime? from,
                [FromQuery] DateTime? to,
                [FromQuery] int? categoryId,
                [FromQuery] int? departmentId,
                [FromQuery] string? status)
        {
            var query =
                BuildDynamicReportQuery(
                    from,
                    to,
                    categoryId,
                    departmentId,
                    status
                );

            var reports =
                await GetReportReadDtos(query);

            var categoryName =
                categoryId.HasValue
                    ? (await _db.Categories
                        .FindAsync(categoryId.Value))
                        ?.DisplayName
                        ?? "Unknown category"
                    : "All categories";

            var departmentName =
                departmentId.HasValue
                    ? (await _db.Departments
                        .FindAsync(departmentId.Value))
                        ?.DisplayName
                        ?? "Unknown department"
                    : "All departments";

            return Ok(
                new DynamicReportDto
                {
                    filtersApplied =
                        new DynamicReportFiltersDto
                        {
                            From =
                                from?.ToString(
                                    "yyyy-MM-dd"),

                            To =
                                to?.ToString(
                                    "yyyy-MM-dd"),

                            CategoryName =
                                categoryName,

                            DepartmentName =
                                departmentName,

                            StatusName =
                                string.IsNullOrWhiteSpace(status)
                                    ? "All statuses"
                                    : status
                        },

                    lineItems = reports,

                    summary =
                        new DynamicReportSummaryDto
                        {
                            TotalReports =
                                reports.Count,

                            Resolved =
                                reports.Count(r =>
                                    r.Status ==
                                    "Resolved"),

                            InProgress =
                                reports.Count(r =>
                                    r.Status ==
                                    "In Progress"),

                            Reported =
                                reports.Count(r =>
                                    r.Status ==
                                    "Reported"),

                            Assigned =
                                reports.Count(r =>
                                    r.Status ==
                                    "Assigned")
                        }
                }
            );
        }


        // =========================================================
        // EXPORT EXCEL
        // =========================================================

        [HttpGet("reports/export/excel")]
        public async Task<IActionResult>
            ExportDynamicReportExcel(
                [FromQuery] DateTime? from,
                [FromQuery] DateTime? to,
                [FromQuery] int? categoryId,
                [FromQuery] int? departmentId,
                [FromQuery] string? status)
        {
            var query =
                BuildDynamicReportQuery(
                    from,
                    to,
                    categoryId,
                    departmentId,
                    status
                );

            var reports =
                await query
                    .Include(r => r.Category)
                    .Include(r => r.Location)
                    .ToListAsync();

            using var workbook =
                new ClosedXML.Excel.XLWorkbook();

            var ws =
                workbook.Worksheets.Add(
                    "Issue Reports"
                );

            var navy =
                ClosedXML.Excel.XLColor
                    .FromHtml("#16283F");

            var lightGray =
                ClosedXML.Excel.XLColor
                    .FromHtml("#F2F4F8");


            // Title

            ws.Cell(1, 1).Value =
                "GRP-03-39 - Issue Reports";

            ws.Cell(1, 1).Style.Font.Bold =
                true;

            ws.Cell(1, 1).Style.Font.FontSize =
                16;

            ws.Cell(1, 1).Style.Font.FontColor =
                navy;

            ws.Range(1, 1, 1, 7).Merge();


            // Filters

            var filterText =
                $"Filters applied: Date range " +
                $"{from?.ToString("yyyy-MM-dd") ?? "(any)"} " +
                $"to " +
                $"{to?.ToString("yyyy-MM-dd") ?? "(any)"} " +
                $" | Status: " +
                $"{(string.IsNullOrWhiteSpace(status) ? "All" : status)}";

            ws.Cell(3, 1).Value =
                filterText;

            ws.Cell(3, 1).Style.Font.Bold =
                true;

            ws.Cell(3, 1).Style.Font.FontColor =
                navy;

            ws.Cell(3, 1)
                .Style
                .Fill
                .BackgroundColor =
                lightGray;

            ws.Range(3, 1, 3, 7).Merge();


            // Headers

            string[] headers =
            {
                "Report Code",
                "Title",
                "Category",
                "Priority",
                "Status",
                "Location",
                "Reported Date"
            };

            for (int i = 0;
                 i < headers.Length;
                 i++)
            {
                var cell =
                    ws.Cell(5, i + 1);

                cell.Value =
                    headers[i];

                cell.Style.Font.Bold =
                    true;

                cell.Style.Font.FontColor =
                    ClosedXML.Excel.XLColor.White;

                cell.Style.Fill.BackgroundColor =
                    navy;
            }


            // Data

            int row = 6;

            foreach (var r in reports)
            {
                ws.Cell(row, 1).Value =
                    r.ReferenceNumber;

                ws.Cell(row, 2).Value =
                    r.Title;

                ws.Cell(row, 3).Value =
                    r.Category.DisplayName;

                ws.Cell(row, 4).Value =
                    r.Priority;

                ws.Cell(row, 5).Value =
                    StatusMapper.ToApp(
                        r.Status);

                ws.Cell(row, 6).Value =
                    r.Location
                        .AddressDescription
                    ?? string.Empty;

                ws.Cell(row, 7).Value =
                    r.CreatedAt
                        .ToString("yyyy-MM-dd");

                if (row % 2 == 0)
                {
                    ws.Range(
                        row,
                        1,
                        row,
                        7)
                        .Style
                        .Fill
                        .BackgroundColor =
                        lightGray;
                }

                row++;
            }


            // Filter / freeze

            var lastDataRow =
                row - 1;

            if (lastDataRow >= 6)
            {
                ws.Range(
                    5,
                    1,
                    lastDataRow,
                    7)
                    .SetAutoFilter();

                ws.SheetView
                    .FreezeRows(5);
            }


            // Summary

            ws.Cell(row + 1, 1).Value =
                "Reports shown:";

            ws.Cell(row + 1, 1)
                .Style
                .Font
                .Bold = true;

            ws.Cell(row + 1, 2).Value =
                reports.Count;


            ws.Cell(row + 2, 1).Value =
                "Resolved:";

            ws.Cell(row + 2, 1)
                .Style
                .Font
                .Bold = true;

            ws.Cell(row + 2, 2).Value =
                reports.Count(r =>
                    r.Status == "Resolved");


            ws.Cell(row + 3, 1).Value =
                "Generated:";

            ws.Cell(row + 3, 1)
                .Style
                .Font
                .Bold = true;

            ws.Cell(row + 3, 2).Value =
                DateTime.Now.ToString(
                    "dd MMM yyyy, HH:mm"
                );


            ws.Columns(1, 7)
                .AdjustToContents();


            using var stream =
                new MemoryStream();

            workbook.SaveAs(stream);

            stream.Position = 0;


            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"grp0339-issue-report-{DateTime.Now:yyyyMMdd-HHmm}.xlsx"
            );
        }


        // =========================================================
        // PRIVATE REPORT DTO BUILDER
        // =========================================================

        private async Task<List<IssueReadDto>>
            GetReportReadDtos(
                IQueryable<Report> query)
        {
            var reports =
                await query

                    .Include(r =>
                        r.Category)

                    .Include(r =>
                        r.Location)

                    .Include(r =>
                        r.Assignments)
                        .ThenInclude(a =>
                            a.Worker)

                    .ToListAsync();


            return reports
                .Select(r =>
                {
                    var currentAssignment =
                        r.Assignments
                            .FirstOrDefault(
                                a => a.IsActive
                            );

                    return new IssueReadDto
                    {
                        ReportID =
                            r.ReportId,

                        ReportCode =
                            r.ReferenceNumber,

                        Title =
                            r.Title,

                        Description =
                            r.Description,

                        Status =
                            StatusMapper.ToApp(
                                r.Status
                            ),

                        Priority =
                            r.Priority,

                        LocationName =
                            r.Location
                                ?.AddressDescription,

                        CreatedAt =
                            r.CreatedAt,

                        CategoryName =
                            r.Category
                                ?.DisplayName,

                        WorkerName =
                            currentAssignment != null &&
                            currentAssignment.Worker != null
                                ? $"{currentAssignment.Worker.FirstName} " +
                                  $"{currentAssignment.Worker.LastName}"
                                : null
                    };
                })
                .ToList();
        }
    }
}