namespace FixMyTownApi.Models.Dtos.Admin
{
    /// <summary>
    /// The one true dynamic report for FixMyTown: reports filtered by
    /// date range, category, department and status - live from the
    /// database, never a static snapshot. Matches the report's own
    /// heading/filters/line-details/footer structure required by the
    /// Dynamic Reporting brief.
    /// </summary>
    public class DynamicReportDto
    {
        public DynamicReportFiltersDto filtersApplied { get; set; } = new();
        public List<Issues.IssueReadDto> lineItems { get; set; } = new();
        public DynamicReportSummaryDto summary { get; set; } = new();
    }

    /// <summary>Echoes back exactly what was filtered on, so the report heading can show it plainly.</summary>
    public class DynamicReportFiltersDto
    {
        public string? From { get; set; }
        public string? To { get; set; }
        public string CategoryName { get; set; } = "All categories";
        public string DepartmentName { get; set; } = "All departments";
        public string StatusName { get; set; } = "All statuses";
    }

    /// <summary>The report's footer - totals for whatever's currently filtered.</summary>
    public class DynamicReportSummaryDto
    {
        public int TotalReports { get; set; }
        public int Resolved { get; set; }
        public int InProgress { get; set; }
        public int Reported { get; set; }
        public int Assigned { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }
}
