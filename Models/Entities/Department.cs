using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FixMyTownApi.Models.Entities
{
    /// <summary>Mirrors dbo.Departments in GRP-03-39DB.sql.</summary>
    [Table("Departments")]
    public class Department
    {
        [Key]
        public int DepartmentId { get; set; }

        public required string DepartmentName { get; set; }
        public required string DisplayName { get; set; }
        public string? Description { get; set; }

        public int? ManagerId { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }

        public string? Color { get; set; }
        public string? Icon { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public ICollection<Category> Categories { get; set; } = new List<Category>();
        public ICollection<WorkerDepartment> WorkerDepartments { get; set; } = new List<WorkerDepartment>();
    }
}
