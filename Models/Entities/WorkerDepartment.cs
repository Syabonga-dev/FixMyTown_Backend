using System.ComponentModel.DataAnnotations.Schema;

namespace FixMyTownApi.Models.Entities
{
    /// <summary>
    /// Mirrors dbo.WorkerDepartments - a many-to-many link between a
    /// worker (User with Role="Worker") and their department(s). Our
    /// app treats the row with IsPrimary=1 as "the" worker's department,
    /// matching the simpler single-department model the frontend expects.
    /// </summary>
    [Table("WorkerDepartments")]
    public class WorkerDepartment
    {
        public int UserId { get; set; }
        public int DepartmentId { get; set; }

        public bool IsPrimary { get; set; }
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

        public User User { get; set; } = null!;
        public Department Department { get; set; } = null!;
    }
}
