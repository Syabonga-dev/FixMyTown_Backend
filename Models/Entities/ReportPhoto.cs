using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FixMyTownApi.Models.Entities
{
    /// <summary>Mirrors dbo.ReportPhotos in GRP-03-39DB.sql.</summary>
    [Table("ReportPhotos")]
    public class ReportPhoto
    {
        [Key]
        public int PhotoId { get; set; }

        public int ReportId { get; set; }
        public int UploadedById { get; set; }

        public required string FileName { get; set; }
        public required string FileUrl { get; set; }
        public int? FileSize { get; set; }
        public string? MimeType { get; set; }

        public string PhotoType { get; set; } = "Report";

        public bool IsPublic { get; set; }
        public bool IsVerified { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        public Report Report { get; set; } = null!;
    }
}
