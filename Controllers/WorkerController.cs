using FixMyTownApi.Data;
using FixMyTownApi.Models.Dtos.Common;
using FixMyTownApi.Models.Dtos.Issues;
using FixMyTownApi.Models.Dtos.Worker;
using FixMyTownApi.Models.Entities;
using FixMyTownApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FixMyTownApi.Controllers
{
    [Route("api/worker")]
    [ApiController]
    [Authorize(Roles = "worker")]
    public class WorkerController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _env;

        public WorkerController(
            AppDbContext db,
            IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        [HttpGet("dashboard")]
        public async Task<ActionResult<WorkerDashboardDto>> GetDashboard()
        {
            var workerId = User.CurrentUserId();

            var counts = new WorkerCountsDto
            {
                TotalAssigned = await _db.Assignments
                    .CountAsync(a => a.WorkerId == workerId),

                InProgress = await _db.Assignments
                    .CountAsync(a =>
                        a.WorkerId == workerId &&
                        a.Report.Status == "InProgress"),

                Completed = await _db.Assignments
                    .CountAsync(a =>
                        a.WorkerId == workerId &&
                        a.Report.Status == "Resolved")
            };

            var priorityReports = await _db.Assignments
                .AsNoTracking()
                .Include(a => a.Report)
                    .ThenInclude(r => r.Category)
                .Include(a => a.Report)
                    .ThenInclude(r => r.Location)
                .Include(a => a.Report)
                    .ThenInclude(r => r.Photos)
                .Where(a =>
                    a.WorkerId == workerId &&
                    a.Report.Status != "Resolved" &&
                    (
                        a.Report.Priority == "High" ||
                        a.Report.Priority == "Critical"
                    ))
                .OrderBy(a =>
                    a.Report.Priority == "Critical" ? 0 : 1)
                .ThenBy(a => a.Report.CreatedAt)
                .Take(5)
                .Select(a => a.Report)
                .ToListAsync();

            var priorityIssues = priorityReports
                .Select(MapReportToIssueDto)
                .ToList();

            return Ok(new WorkerDashboardDto
            {
                counts = counts,
                priorityIssues = priorityIssues
            });
        }

        [HttpGet("assignments")]
        public async Task<ActionResult<IEnumerable<IssueReadDto>>> GetAssignments(
            [FromQuery] string? status)
        {
            var workerId = User.CurrentUserId();

            var query = _db.Assignments
                .AsNoTracking()
                .Include(a => a.Report)
                    .ThenInclude(r => r.Category)
                .Include(a => a.Report)
                    .ThenInclude(r => r.Location)
                .Include(a => a.Report)
                    .ThenInclude(r => r.Photos)
                .Where(a => a.WorkerId == workerId);

            if (!string.IsNullOrWhiteSpace(status))
            {
                var dbStatus = StatusMapper.ToDb(status);

                query = query.Where(
                    a => a.Report.Status == dbStatus
                );
            }

            var reports = await query
                .OrderByDescending(a => a.Report.CreatedAt)
                .Select(a => a.Report)
                .ToListAsync();

            var result = reports
                .Select(MapReportToIssueDto)
                .ToList();

            return Ok(result);
        }

        [HttpGet("reports/{id:int}")]
        public async Task<ActionResult<IssueDetailDto>> GetAssignedReport(int id)
        {
            var workerId = User.CurrentUserId();

            var assignment = await _db.Assignments
                .AsNoTracking()
                .Include(a => a.Report)
                    .ThenInclude(r => r.Category)
                .Include(a => a.Report)
                    .ThenInclude(r => r.Location)
                .Include(a => a.Report)
                    .ThenInclude(r => r.Photos)
                .Include(a => a.Report)
                    .ThenInclude(r => r.ProgressUpdates)
                .FirstOrDefaultAsync(a =>
                    a.ReportId == id &&
                    a.WorkerId == workerId);

            if (assignment == null)
            {
                return NotFound(
                    new ApiMessageDto(
                        "This report is not assigned to you."
                    )
                );
            }

            var report = assignment.Report;

            var result = new IssueDetailDto
            {
                ReportID = report.ReportId,
                ReportCode = report.ReferenceNumber,
                Title = report.Title,
                Description = report.Description,
                Status = StatusMapper.ToApp(report.Status),
                Priority = report.Priority,

                Latitude = report.Location.Latitude ?? 0m,
                Longitude = report.Location.Longitude ?? 0m,

                LocationName = report.Location.AddressDescription,
                CreatedAt = report.CreatedAt,
                UpdatedAt = report.UpdatedAt ?? report.CreatedAt,

                CategoryName = report.Category.DisplayName,

                Photos = report.Photos
                    .OrderBy(p => p.UploadedAt)
                    .Select(p => p.FileUrl)
                    .ToList()
            };

            return Ok(result);
        }

        [HttpPost("reports/{id:int}/progress")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<ApiMessageDto>> UpdateProgress(
            int id,
            [FromForm] ProgressUpdateCreateDto dto)
        {
            var workerId = User.CurrentUserId();

            var assignment = await _db.Assignments
                .Include(a => a.Report)
                .FirstOrDefaultAsync(a =>
                    a.ReportId == id &&
                    a.WorkerId == workerId);

            if (assignment == null)
            {
                return NotFound(
                    new ApiMessageDto(
                        "This report is not assigned to you."
                    )
                );
            }

            var report = assignment.Report;

            var oldStatus = report.Status;

            var newStatus =
                dto.MarkResolved
                    ? "Resolved"
                    : "InProgress";

            if (dto.Photo != null &&
                dto.Photo.Length > 0)
            {
                var uploadsFolder = Path.Combine(
                    _env.WebRootPath,
                    "uploads"
                );

                Directory.CreateDirectory(uploadsFolder);

                var extension =
                    Path.GetExtension(dto.Photo.FileName);

                var fileName =
                    $"{Guid.NewGuid()}{extension}";

                var filePath =
                    Path.Combine(
                        uploadsFolder,
                        fileName
                    );

                await using var stream =
                    new FileStream(
                        filePath,
                        FileMode.Create
                    );

                await dto.Photo.CopyToAsync(stream);

                _db.ReportPhotos.Add(
                    new ReportPhoto
                    {
                        ReportId = id,
                        UploadedById = workerId,
                        FileName = fileName,
                        FileUrl = $"/uploads/{fileName}",
                        FileSize = (int)dto.Photo.Length,
                        MimeType = dto.Photo.ContentType,
                        PhotoType = dto.MarkResolved
                            ? "Resolution"
                            : "Progress",
                        IsPublic = true,
                        IsVerified = false,
                        UploadedAt = DateTime.UtcNow
                    }
                );
            }

            report.Status = newStatus;
            report.UpdatedAt = DateTime.UtcNow;
            report.UpdatedBy = workerId;

            _db.ProgressUpdates.Add(
                new ProgressUpdate
                {
                    ReportId = id,
                    WorkerId = workerId,
                    ProgressNote =
                        string.IsNullOrWhiteSpace(dto.Note)
                            ? "Progress updated."
                            : dto.Note
                }
            );

            _db.ReportStatusHistories.Add(
                new ReportStatusHistory
                {
                    ReportId = id,
                    OldStatus = oldStatus,
                    NewStatus = newStatus,
                    ChangedById = workerId,
                    Comment = dto.Note,
                    ChangedAt = DateTime.UtcNow
                }
            );

            if (dto.MarkResolved)
            {
                report.ResolvedAt = DateTime.UtcNow;

                assignment.CompletedAt =
                    DateTime.UtcNow;

                assignment.IsActive = false;

                assignment.CompletionNotes =
                    dto.Note;
            }

            var notificationMessage =
                dto.MarkResolved
                    ? $"Good news - your report \"{report.Title}\" ({report.ReferenceNumber}) has been resolved."
                    : $"There's an update on your report \"{report.Title}\" ({report.ReferenceNumber}).";

            _db.Notifications.Add(
                new Notification
                {
                    UserId = report.CitizenId,
                    ReportId = id,
                    Type = dto.MarkResolved
                        ? "Resolution"
                        : "StatusUpdate",
                    Title = dto.MarkResolved
                        ? "Report Resolved"
                        : "Report Updated",
                    Message = notificationMessage,
                    CreatedAt = DateTime.UtcNow
                }
            );

            await _db.SaveChangesAsync();

            return Ok(
                new
                {
                    message = "Progress updated successfully.",
                    status = StatusMapper.ToApp(newStatus)
                }
            );
        }

        private static IssueReadDto MapReportToIssueDto(
            Report report)
        {
            return new IssueReadDto
            {
                ReportID = report.ReportId,

                ReportCode = report.ReferenceNumber,

                Title = report.Title,

                Description = report.Description,

                Status = StatusMapper.ToApp(
                    report.Status
                ),

                Priority = report.Priority,

                LocationName =
                    report.Location?.AddressDescription,

                CreatedAt = report.CreatedAt,

                CategoryName =
                    report.Category?.DisplayName
                    ?? string.Empty,

                WorkerName = null,

                Photos =
                    report.Photos?
                        .OrderBy(p => p.UploadedAt)
                        .Select(p => p.FileUrl)
                        .ToList()
                    ?? new List<string>()
            };
        }
    }
}