using Microsoft.AspNetCore.Http;

namespace FixMyTownApi.Models.Dtos.Issues
{
    /// <summary>
    /// What a citizen submits from the 4-step Report Wizard.
    /// Sent as multipart/form-data because photos travel alongside it.
    /// </summary>
    public class IssueCreateDto
    {
        public int CategoryId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Priority { get; set; } = "Medium";
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public string? LocationName { get; set; }
        public List<IFormFile>? Photos { get; set; }
    }

    /// <summary>
    /// What can be edited after a report exists. Citizens may only
    /// edit their own report while it's still "Reported" (before a
    /// worker has started on it); admins can edit any report, any time
    /// - see IssuesController for exactly where that rule is enforced.
    /// </summary>
    public class IssueUpdateDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Priority { get; set; } = "Medium";
    }

    /// <summary>One row in a reports table (My Reports, All Reports, Worker Assignments, etc.)</summary>
    public class IssueReadDto
    {
        public int ReportID { get; set; }
        public string ReportCode { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string Status { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string? LocationName { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string? WorkerName { get; set; }

        // Photos belonging to this report
        public List<string> Photos { get; set; } = new();
    }

    /// <summary>Full detail view - one report plus its photos and progress timeline.</summary>
    public class IssueDetailDto
    {
        public int ReportID { get; set; }
        public string ReportCode { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public string? LocationName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string? DepartmentName { get; set; }
        public List<string> Photos { get; set; } = new();
        public List<ProgressUpdateReadDto> Updates { get; set; } = new();
    }

    public class ProgressUpdateReadDto
    {
        public string? Note { get; set; }
        public string? PhotoURL { get; set; }
        public string StatusAtUpdate { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string WorkerName { get; set; } = string.Empty;
    }

    /// <summary>Pins shown on the public map view.</summary>
    public class IssueMapPinDto
    {
        public int ReportID { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public string CategoryName { get; set; } = string.Empty;
    }

    /// <summary>The "1,247 / 318 / 892 / 37" counters on the citizen dashboard.</summary>
    public class PublicStatsDto
    {
        public int TotalReports { get; set; }
        public int InProgress { get; set; }
        public int Resolved { get; set; }
        public int NewToday { get; set; }
    }
}
