using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FixMyTownApi.Models.Entities
{
    /// <summary>
    /// Mirrors dbo.ProgressUpdates in GRP-03-39DB.sql. Note this schema
    /// has no PhotoURL or StatusAtUpdate column like the old one - just
    /// a note and time spent. Any photo from a worker's progress update
    /// is saved into ReportPhotos instead (PhotoType = "Progress").
    /// </summary>
    [Table("ProgressUpdates")]
    public class ProgressUpdate
    {
        [Key]
        public int ProgressUpdateId { get; set; }

        public int ReportId { get; set; }
        public int WorkerId { get; set; }

        public required string ProgressNote { get; set; }
        public int TimeSpentMinutes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public Report Report { get; set; } = null!;
    }
}
