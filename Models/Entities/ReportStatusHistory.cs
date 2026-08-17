using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FixMyTownApi.Models.Entities
{
    /// <summary>Mirrors dbo.ReportStatusHistory in GRP-03-39DB.sql.</summary>
    [Table("ReportStatusHistory")]
    public class ReportStatusHistory
    {
        [Key]
        public int StatusHistoryId { get; set; }

        public int ReportId { get; set; }

        public string? OldStatus { get; set; }
        public required string NewStatus { get; set; }

        public int ChangedById { get; set; }
        public string? Comment { get; set; }

        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

        public Report Report { get; set; } = null!;
    }
}
