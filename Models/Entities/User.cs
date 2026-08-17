using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FixMyTownApi.Models.Entities
{
    [Table("Users")]
    public class User
    {
        [Key]
        public int UserId { get; set; }

        public required string FirstName { get; set; }
        public required string LastName { get; set; }

        public required string Email { get; set; }
        public string? PhoneNumber { get; set; }

        public required string PasswordHash { get; set; }

        
        public required string Role { get; set; }

        public bool IsActive { get; set; } = true;
        public bool IsVerified { get; set; } = false;
        public string? VerificationToken { get; set; }

        public string? ResetToken { get; set; }
        public DateTime? ResetTokenExpiry { get; set; }

        public string? EmployeeId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        public ICollection<WorkerDepartment> WorkerDepartments { get; set; } = new List<WorkerDepartment>();
    }
}
