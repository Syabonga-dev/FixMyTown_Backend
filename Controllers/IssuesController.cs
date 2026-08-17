using FixMyTownApi.Data;
using FixMyTownApi.Models.Dtos.Common;
using FixMyTownApi.Models.Dtos.Issues;
using FixMyTownApi.Models.Entities;
using FixMyTownApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FixMyTownApi.Controllers
{
    [Route("api/issues")]
    [ApiController]
    public class IssuesController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _env;
        private readonly ReportNumberGenerator _refNumbers;

        public IssuesController(
            AppDbContext db,
            IWebHostEnvironment env,
            ReportNumberGenerator refNumbers)
        {
            _db = db;
            _env = env;
            _refNumbers = refNumbers;
        }

        [HttpGet("recent")]
        [AllowAnonymous]
        public async Task<IActionResult> GetRecentIssues()
        {
            var oneHourAgo = DateTime.UtcNow.AddHours(-1);

            var query = _db.Reports
                .AsNoTracking()
                .Select(r => new
                {
                    ReportId = r.ReportId,
                    ReferenceNumber = r.ReferenceNumber,
                    Title = r.Title,
                    Status = r.Status,
                    Priority = r.Priority,
                    CreatedAt = r.CreatedAt,

                    Category = r.Category.CategoryName,

                    Latitude = r.Location.Latitude,
                    Longitude = r.Location.Longitude,

                    Photos = r.Photos
                        .Where(p => p.IsPublic)
                        .Select(p => p.FileUrl)
                        .ToList(),

                    Location =
                        !string.IsNullOrEmpty(r.Location.StreetAddress)
                            ? r.Location.StreetAddress
                            : !string.IsNullOrEmpty(r.Location.AddressDescription)
                                ? r.Location.AddressDescription
                                : !string.IsNullOrEmpty(r.Location.Suburb)
                                    ? r.Location.Suburb
                                    : !string.IsNullOrEmpty(r.Location.Ward)
                                        ? r.Location.Ward
                                        : !string.IsNullOrEmpty(r.Location.District)
                                            ? r.Location.District
                                            : r.Location.Province
                });

            // Reports from the last hour
            var recentIssues = await query
                .Where(r => r.CreatedAt >= oneHourAgo)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            // If none exist, return the latest 20 reports
            if (!recentIssues.Any())
            {
                recentIssues = await query
                    .OrderByDescending(r => r.CreatedAt)
                    .Take(20)
                    .ToListAsync();
            }

            return Ok(recentIssues);
        }

        [HttpGet("public-stats")]
        [AllowAnonymous]
        public async Task<ActionResult<PublicStatsDto>> GetPublicStats()
        {
            var today = DateTime.UtcNow.Date;

            var stats = new PublicStatsDto
            {
                TotalReports = await _db.Reports
                    .CountAsync(r => r.DeletedAt == null),

                InProgress = await _db.Reports
                    .CountAsync(r =>
                        r.Status == "InProgress" &&
                        r.DeletedAt == null),

                Resolved = await _db.Reports
                    .CountAsync(r =>
                        r.Status == "Resolved" &&
                        r.DeletedAt == null),

                NewToday = await _db.Reports
                    .CountAsync(r =>
                        r.CreatedAt.Date == today &&
                        r.DeletedAt == null)
            };

            return Ok(stats);
        }

        [HttpGet("map")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<IssueMapPinDto>>> GetMapIssues()
        {
            var pins = await _db.Reports
                .Include(r => r.Category)
                .Include(r => r.Location)
                .Where(r => r.DeletedAt == null)
                .Select(r => new IssueMapPinDto
                {
                    ReportID = r.ReportId,
                    Title = r.Title,
                    Status = r.Status,
                    Priority = r.Priority,
                    Latitude = r.Location.Latitude ?? 0,
                    Longitude = r.Location.Longitude ?? 0,
                    CategoryName = r.Category.DisplayName
                })
                .ToListAsync();

            foreach (var pin in pins)
            {
                pin.Status = StatusMapper.ToApp(pin.Status);
            }

            return Ok(pins);
        }

        [HttpPost]
        [Authorize(Roles = "citizen")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<ApiMessageDto>> Create(
            [FromForm] IssueCreateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Title) ||
                string.IsNullOrWhiteSpace(dto.Description))
            {
                return BadRequest(
                    new ApiMessageDto(
                        "Title and description are both required."
                    )
                );
            }

            var category = await _db.Categories.FindAsync(dto.CategoryId);

            if (category == null)
            {
                return BadRequest(
                    new ApiMessageDto(
                        "Please choose a valid category."
                    )
                );
            }

            var location = new Location
            {
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                AddressDescription = dto.LocationName
            };

            _db.Locations.Add(location);
            await _db.SaveChangesAsync();

            var report = new Report
            {
                ReferenceNumber = await _refNumbers.GenerateAsync(),
                CitizenId = User.CurrentUserId(),
                CategoryId = dto.CategoryId,
                LocationId = location.LocationId,
                Title = dto.Title,
                Description = dto.Description,
                Priority = string.IsNullOrWhiteSpace(dto.Priority)
                    ? "Medium"
                    : dto.Priority,
                Status = "Reported"
            };

            _db.Reports.Add(report);
            await _db.SaveChangesAsync();

            // Save uploaded photos
            if (dto.Photos != null && dto.Photos.Count > 0)
            {
                var webRootPath = _env.WebRootPath;

                if (string.IsNullOrWhiteSpace(webRootPath))
                {
                    webRootPath = Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot"
                    );
                }

                var uploadsFolder = Path.Combine(
                    webRootPath,
                    "uploads"
                );

                Directory.CreateDirectory(uploadsFolder);

                Console.WriteLine(
                    $"WEB ROOT: {webRootPath}"
                );

                Console.WriteLine(
                    $"UPLOADS FOLDER: {uploadsFolder}"
                );

                foreach (var file in dto.Photos.Take(5))
                {
                    if (file == null || file.Length == 0)
                    {
                        continue;
                    }

                    var extension = Path.GetExtension(
                        file.FileName
                    );

                    var fileName =
                        $"{Guid.NewGuid()}{extension}";

                    var filePath = Path.Combine(
                        uploadsFolder,
                        fileName
                    );

                    using (var stream = new FileStream(
                        filePath,
                        FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    _db.ReportPhotos.Add(
                        new ReportPhoto
                        {
                            ReportId = report.ReportId,
                            UploadedById = User.CurrentUserId(),
                            FileName = fileName,
                            FileUrl = $"/uploads/{fileName}",
                            FileSize = (int?)file.Length,
                            MimeType = file.ContentType,
                            PhotoType = "Report",
                            IsPublic = true,
                            IsVerified = false,
                            UploadedAt = DateTime.UtcNow
                        }
                    );
                }

                await _db.SaveChangesAsync();
            }

            return CreatedAtAction(
                nameof(GetById),
                new { id = report.ReportId },
                new ApiMessageDto(
                    "Report submitted successfully."
                )
            );
        }

        [HttpGet("mine")]
        [Authorize(Roles = "citizen")]
        public async Task<ActionResult<IEnumerable<IssueReadDto>>> GetMine()
        {
            try
            {
                var userIdClaim = User.FindFirst(
                    System.Security.Claims.ClaimTypes.NameIdentifier
                )?.Value;

                if (string.IsNullOrEmpty(userIdClaim))
                {
                    return Unauthorized(new
                    {
                        message = "User ID not found in token"
                    });
                }

                if (!int.TryParse(userIdClaim, out int myId))
                {
                    return BadRequest(new
                    {
                        message = "Invalid user ID in token"
                    });
                }

                var reports = await _db.Reports
                    .Include(r => r.Category)
                    .Include(r => r.Location)
                    .Include(r => r.Assignments)
                        .ThenInclude(a => a.Worker)
                    .Where(r =>
                        r.CitizenId == myId &&
                        r.DeletedAt == null)
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync();

                var result = reports.Select(r =>
                {
                    var currentAssignment =
                        r.Assignments?
                            .FirstOrDefault(a => a.IsActive);

                    return new IssueReadDto
                    {
                        ReportID = r.ReportId,
                        ReportCode =
                            r.ReferenceNumber ?? "N/A",
                        Title =
                            r.Title ?? "Untitled",
                        Description =
                            r.Description ?? "",
                        Status =
                            StatusMapper.ToApp(
                                r.Status ?? "Reported"),
                        Priority =
                            r.Priority ?? "Medium",
                        LocationName =
                            r.Location?.AddressDescription
                            ?? "Unknown Location",
                        CreatedAt = r.CreatedAt,
                        CategoryName =
                            r.Category?.DisplayName
                            ?? "Uncategorized",
                        WorkerName =
                            currentAssignment?.Worker != null
                                ? $"{currentAssignment.Worker.FirstName} {currentAssignment.Worker.LastName}"
                                : null
                    };
                }).ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                var errorMessage =
                    $"Error in GetMine: {ex.Message}";

                if (ex.InnerException != null)
                {
                    errorMessage +=
                        $" | Inner: {ex.InnerException.Message}";
                }

                Console.WriteLine(errorMessage);
                Console.WriteLine(
                    $"Stack: {ex.StackTrace}"
                );

                return StatusCode(
                    500,
                    new
                    {
                        message =
                            "An error occurred while fetching your reports",
                        error = ex.Message,
                        innerError =
                            ex.InnerException?.Message
                    }
                );
            }
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<IssueDetailDto>> GetById(
            int id)
        {
            var myId = User.CurrentUserId();

            var report = await _db.Reports
                .Include(r => r.Category)
                    .ThenInclude(c => c.Department)
                .Include(r => r.Location)
                .Include(r => r.Photos)
                .Include(r => r.ProgressUpdates)
                .FirstOrDefaultAsync(r =>
                    r.ReportId == id &&
                    r.CitizenId == myId &&
                    r.DeletedAt == null);

            if (report == null)
            {
                return NotFound(
                    new ApiMessageDto(
                        "Report not found."
                    )
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

            return Ok(
                new IssueDetailDto
                {
                    ReportID = report.ReportId,
                    ReportCode = report.ReferenceNumber,
                    Title = report.Title,
                    Description = report.Description,
                    Status =
                        StatusMapper.ToApp(report.Status),
                    Priority = report.Priority,
                    Latitude =
                        report.Location.Latitude ?? 0,
                    Longitude =
                        report.Location.Longitude ?? 0,
                    LocationName =
                        report.Location.AddressDescription,
                    CreatedAt = report.CreatedAt,
                    UpdatedAt =
                        report.UpdatedAt ??
                        report.CreatedAt,
                    CategoryName =
                        report.Category.DisplayName,
                    DepartmentName =
                        report.Category.Department
                            ?.DisplayName,

                    Photos = report.Photos
                        .Select(p => p.FileUrl)
                        .ToList(),

                    Updates =
                        report.ProgressUpdates
                            .OrderBy(u => u.CreatedAt)
                            .Select(u =>
                                new ProgressUpdateReadDto
                                {
                                    Note =
                                        u.ProgressNote,
                                    PhotoURL = null,
                                    StatusAtUpdate =
                                        StatusMapper.ToApp(
                                            report.Status),
                                    CreatedAt =
                                        u.CreatedAt,
                                    WorkerName =
                                        workerNames.GetValueOrDefault(
                                            u.WorkerId,
                                            "Unknown"
                                        )
                                })
                            .ToList()
                }
            );
        }

        [HttpGet("search")]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult<IssueDetailDto>> SearchByCode(
            [FromQuery] string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return BadRequest(
                    new ApiMessageDto(
                        "Please provide a report code."
                    )
                );
            }

            code = code.Trim();

            var report = await _db.Reports
                .Include(r => r.Category)
                    .ThenInclude(c => c.Department)
                .Include(r => r.Location)
                .Include(r => r.Photos)
                .Include(r => r.ProgressUpdates)
                .FirstOrDefaultAsync(r =>
                    r.ReferenceNumber == code &&
                    r.DeletedAt == null);

            if (report == null)
            {
                return NotFound(
                    new ApiMessageDto(
                        "Report not found."
                    )
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

            return Ok(
                new IssueDetailDto
                {
                    ReportID = report.ReportId,
                    ReportCode =
                        report.ReferenceNumber,
                    Title = report.Title,
                    Description =
                        report.Description,
                    Status =
                        StatusMapper.ToApp(
                            report.Status),
                    Priority =
                        report.Priority,
                    Latitude =
                        report.Location.Latitude ?? 0,
                    Longitude =
                        report.Location.Longitude ?? 0,
                    LocationName =
                        report.Location.AddressDescription,
                    CreatedAt =
                        report.CreatedAt,
                    UpdatedAt =
                        report.UpdatedAt ??
                        report.CreatedAt,
                    CategoryName =
                        report.Category.DisplayName,
                    DepartmentName =
                        report.Category.Department
                            ?.DisplayName,

                    Photos = report.Photos
                        .Select(p => p.FileUrl)
                        .ToList(),

                    Updates =
                        report.ProgressUpdates
                            .OrderBy(u => u.CreatedAt)
                            .Select(u =>
                                new ProgressUpdateReadDto
                                {
                                    Note =
                                        u.ProgressNote,
                                    PhotoURL = null,
                                    StatusAtUpdate =
                                        StatusMapper.ToApp(
                                            report.Status),
                                    CreatedAt =
                                        u.CreatedAt,
                                    WorkerName =
                                        workerNames.GetValueOrDefault(
                                            u.WorkerId,
                                            "Unknown"
                                        )
                                })
                            .ToList()
                }
            );
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "citizen")]
        public async Task<ActionResult<ApiMessageDto>> Update(
            int id,
            IssueUpdateDto dto)
        {
            var myId = User.CurrentUserId();

            var report = await _db.Reports
                .FirstOrDefaultAsync(r =>
                    r.ReportId == id &&
                    r.CitizenId == myId &&
                    r.DeletedAt == null);

            if (report == null)
            {
                return NotFound(
                    new ApiMessageDto(
                        "Report not found."
                    )
                );
            }

            if (report.Status != "Reported")
            {
                return BadRequest(
                    new ApiMessageDto(
                        "This report is already being worked on and can no longer be edited."
                    )
                );
            }

            report.Title = dto.Title;
            report.Description = dto.Description;
            report.Priority = dto.Priority;
            report.UpdatedAt = DateTime.UtcNow;
            report.UpdatedBy = myId;

            await _db.SaveChangesAsync();

            return Ok(
                new ApiMessageDto(
                    "Report updated successfully."
                )
            );
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "citizen")]
        public async Task<ActionResult<ApiMessageDto>> Delete(
            int id)
        {
            var myId = User.CurrentUserId();

            var report = await _db.Reports
                .FirstOrDefaultAsync(r =>
                    r.ReportId == id &&
                    r.CitizenId == myId &&
                    r.DeletedAt == null);

            if (report == null)
            {
                return NotFound(
                    new ApiMessageDto(
                        "Report not found."
                    )
                );
            }

            if (report.Status != "Reported")
            {
                return BadRequest(
                    new ApiMessageDto(
                        "This report is already being worked on and can no longer be deleted."
                    )
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
    }
}