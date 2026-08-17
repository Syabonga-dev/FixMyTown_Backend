using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FixMyTownApi.Models.Entities
{
    [Table("Assignments")]
    public class Assignment
    {
        [Key]
        public int AssignmentId { get; set; }

        public int ReportId { get; set; }
        public int WorkerId { get; set; }
        public int AssignedById { get; set; }

        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ExpectedCompletionDate { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public bool IsActive { get; set; } = true;

        public string? Notes { get; set; }
        public string? CompletionNotes { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public int IsOverdue { get; private set; }

        public Report Report { get; set; } = null!;
        public User Worker { get; set; } = null!;
    }
}
