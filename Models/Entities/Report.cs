using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FixMyTownApi.Models.Entities
{

    [Table("Reports")]
    public class Report
    {
        [Key]
        public int ReportId { get; set; }

        public required string ReferenceNumber { get; set; }

        public int CitizenId { get; set; }
        public int CategoryId { get; set; }
        public int LocationId { get; set; }

        public required string Title { get; set; }
        public required string Description { get; set; }

        public string Priority { get; set; } = "Medium";

        /// <summary>Stored as one of: Reported, UnderReview, Assigned, InProgress, Resolved, Closed, Rejected.</summary>
        public string Status { get; set; } = "Reported";

        public bool IsAnonymous { get; set; }
        public bool IsPublic { get; set; } = true;
        public bool IsEmergency { get; set; }

        public int? PriorityOverrideBy { get; set; }
        public string? PriorityOverrideReason { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public int? ResolutionTimeMinutes { get; private set; }

        public int? UpdatedBy { get; set; }

        public Category Category { get; set; } = null!;
        public Location Location { get; set; } = null!;
        public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
        public ICollection<ReportStatusHistory> StatusHistory { get; set; } = new List<ReportStatusHistory>();
        public ICollection<ReportPhoto> Photos { get; set; } = new List<ReportPhoto>();
        public ICollection<ProgressUpdate> ProgressUpdates { get; set; } = new List<ProgressUpdate>();
    }
}
