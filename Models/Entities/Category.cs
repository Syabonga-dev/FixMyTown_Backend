using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FixMyTownApi.Models.Entities
{
    /// <summary>Mirrors dbo.Categories in GRP-03-39DB.sql.</summary>
    [Table("Categories")]
    public class Category
    {
        [Key]
        public int CategoryId { get; set; }

        public required string CategoryName { get; set; }
        public required string DisplayName { get; set; }
        public string? Description { get; set; }

        public string? Icon { get; set; }
        public string? Color { get; set; }

        public string DefaultPriority { get; set; } = "Medium";
        public int? EstimatedResolutionHours { get; set; } = 48;

        public int? DepartmentId { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Department? Department { get; set; }
        public ICollection<Report> Reports { get; set; } = new List<Report>();
    }
}
