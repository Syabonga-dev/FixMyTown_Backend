namespace FixMyTownApi.Models.Dtos.Admin
{
    /// <summary>
    /// NOTE ON CASING: Counts/RecentReports/WorkerOverview are
    /// lowercase-first on purpose - AdminDashboard.jsx reads
    /// data.counts / data.recentReports / data.workerOverview.
    /// The fields INSIDE DashboardCountsDto stay PascalCase
    /// (TotalReports, Pending, ...) because that's what the
    /// frontend reads for those.
    /// </summary>
    public class AdminDashboardDto
    {
        public DashboardCountsDto counts { get; set; } = new();
        public List<Issues.IssueReadDto> recentReports { get; set; } = new();
        public List<WorkerOverviewDto> workerOverview { get; set; } = new();
    }

    public class DashboardCountsDto
    {
        public int TotalReports { get; set; }
        public int Pending { get; set; }
        public int InProgress { get; set; }
        public int Resolved { get; set; }
    }

    public class WorkerOverviewDto
    {
        public string WorkerName { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public int ActiveIssues { get; set; }
    }

    /// <summary>What an admin sends to assign an unassigned report to a worker.</summary>
    public class AssignReportDto
    {
        public int DepartmentId { get; set; }
        public int WorkerId { get; set; }
        public string? Note { get; set; }
    }

    /// <summary>What an admin sends to register a new worker account.</summary>
    public class WorkerCreateDto
    {
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public int DepartmentId { get; set; }

        // If left blank, the API generates one and returns it in
        // the response, matching the prototype's "Auto-Generated
        // Password" panel with a "Generate New" button.
        public string? Password { get; set; }
    }

    /// <summary>What an admin sends to edit an existing worker's details.</summary>
    public class WorkerUpdateDto
    {
        public string FullName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public int DepartmentId { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class WorkerReadDto
    {
        public int WorkerID { get; set; }
        public int UserID { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public bool IsActive { get; set; }
        public int DepartmentID { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public int ActiveIssues { get; set; }
    }

    public class DepartmentReadDto
    {
        public int DepartmentID { get; set; }
        public string Name { get; set; } = string.Empty;
        public int OpenIssues { get; set; }
        public int WorkerCount { get; set; }
    }

    /// <summary>
    /// NOTE ON CASING: ByCategory/ByLocation are lowercase-first
    /// ("byCategory"/"byLocation") on purpose - Analytics.jsx reads
    /// data.byCategory and data.byLocation. Everything else on this
    /// DTO stays PascalCase (TotalIssues, ResolutionRate, ...) to
    /// match how the frontend reads those.
    /// </summary>
    public class AnalyticsDto
    {
        public int TotalIssues { get; set; }
        public int TotalWorkers { get; set; }
        public int ResolutionRate { get; set; }
        public double AvgResponseHours { get; set; }
        public double AvgResolutionDays { get; set; }
        public List<CategoryCountDto> byCategory { get; set; } = new();
        public List<LocationCountDto> byLocation { get; set; } = new();
    }

    public class CategoryCountDto
    {
        public string CategoryName { get; set; } = string.Empty;
        public int IssueCount { get; set; }
    }

    public class LocationCountDto
    {
        public string? LocationName { get; set; }
        public int IssueCount { get; set; }
    }
}
