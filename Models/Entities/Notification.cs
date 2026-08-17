using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FixMyTownApi.Models.Entities
{
    /// <summary>Mirrors dbo.Notifications in GRP-03-39DB.sql.</summary>
    [Table("Notifications")]
    public class Notification
    {
        [Key]
        public int NotificationId { get; set; }

        public int UserId { get; set; }
        public int? ReportId { get; set; }
        public int? RelatedUserId { get; set; }

        public required string Type { get; set; }
        public required string Title { get; set; }
        public required string Message { get; set; }
        public string? Link { get; set; }

        public bool IsRead { get; set; }
        public bool IsSeen { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReadAt { get; set; }
        public string? Metadata { get; set; }
    }
}
