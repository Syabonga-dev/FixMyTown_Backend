using Microsoft.AspNetCore.Http;

namespace FixMyTownApi.Models.Dtos.Worker
{
    /// <summary>
    /// NOTE ON CASING: counts/priorityIssues are lowercase-first on
    /// purpose - WorkerDashboard.jsx reads data.counts and
    /// data.priorityIssues. The fields INSIDE WorkerCountsDto stay
    /// PascalCase (TotalAssigned, InProgress, Completed) because
    /// that's what the frontend reads for those.
    /// </summary>
    public class WorkerDashboardDto
    {
        public WorkerCountsDto counts { get; set; } = new();
        public List<Issues.IssueReadDto> priorityIssues { get; set; } = new();
    }

    public class WorkerCountsDto
    {
        public int TotalAssigned { get; set; }
        public int InProgress { get; set; }
        public int Completed { get; set; }
    }

    /// <summary>
    /// What a worker submits from the "Update Progress" modal - a
    /// note, an optional photo, and whether this fully resolves the
    /// issue.
    /// </summary>
    public class ProgressUpdateCreateDto
    {
        public string? Note { get; set; }
        public bool MarkResolved { get; set; }
        public IFormFile? Photo { get; set; }
    }
}
