using FixMyTownApi.Data;
using FixMyTownApi.Models.Dtos.Auth;
using FixMyTownApi.Models.Dtos.Common;
using FixMyTownApi.Models.Entities;
using FixMyTownApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace FixMyTownApi.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly PasswordService _passwords;
        private readonly JwtService _jwt;
        private readonly EmailService _email;
        private readonly IMemoryCache _cache;

        public AuthController(AppDbContext db, PasswordService passwords, JwtService jwt, EmailService email, IMemoryCache cache)
        {
            _db = db;
            _passwords = passwords;
            _jwt = jwt;
            _email = email;
            _cache = cache;
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<ActionResult<AuthResponseDto>> Register(RegisterDto dto)
        {
            var emailTaken = await _db.Users.AnyAsync(u => u.Email == dto.Email);
            if (emailTaken)
                return Conflict(new ApiMessageDto("This email already exists. Please try logging in instead, or use a different email."));

            var passwordError = ValidatePasswordStrength(dto.Password);
            if (passwordError != null)
                return BadRequest(new ApiMessageDto(passwordError));

            var (firstName, lastName) = SplitName(dto.FullName);

            var user = new User
            {
                FirstName = firstName,
                LastName = lastName,
                Email = dto.Email,
                PasswordHash = _passwords.Hash(dto.Password),
                PhoneNumber = dto.Phone,
                Role = RoleMapper.ToDb("citizen"),
                IsVerified = true
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            var fullName = $"{user.FirstName} {user.LastName}";
            await _email.SendWelcomeEmailAsync(user.Email, fullName);

            var token = _jwt.GenerateToken(user);

            return Created(string.Empty, new AuthResponseDto
            {
                message = "Account created successfully.",
                token = token,
                user = new UserSummaryDto { id = user.UserId, fullName = fullName, email = user.Email, role = RoleMapper.ToApp(user.Role) }
            });
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<AuthResponseDto>> Login(LoginDto dto)
        {
            var dbRole = RoleMapper.ToDb(dto.Role);

            var user = await _db.Users.FirstOrDefaultAsync(u =>
                u.Email == dto.Email && u.Role == dbRole && u.IsActive);

            if (user == null)
                return Unauthorized(new ApiMessageDto("Invalid credentials or wrong role selected."));

            if (!_passwords.Verify(dto.Password, user.PasswordHash))
                return Unauthorized(new ApiMessageDto("Invalid credentials."));

            user.LastLoginAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            var token = _jwt.GenerateToken(user);
            var fullName = $"{user.FirstName} {user.LastName}";

            return Ok(new AuthResponseDto
            {
                message = "Login successful.",
                token = token,
                user = new UserSummaryDto { id = user.UserId, fullName = fullName, email = user.Email, role = RoleMapper.ToApp(user.Role) }
            });
        }

        [HttpPost("forgot-password/check-email")]
        [AllowAnonymous]
        public async Task<IActionResult> CheckEmail(ForgotPasswordCheckEmailDto dto)
        {
            var exists = await _db.Users.AnyAsync(u => u.Email == dto.Email && u.IsActive);
            return Ok(new { exists });
        }

        [HttpPost("forgot-password/request-otp")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiMessageDto>> RequestOtp(ForgotPasswordRequestOtpDto dto)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email && u.IsActive);
            if (user == null)
                return NotFound(new ApiMessageDto("We couldn't find an account with that email."));

            var passwordError = ValidatePasswordStrength(dto.NewPassword);
            if (passwordError != null)
                return BadRequest(new ApiMessageDto(passwordError));

            var otp = Random.Shared.Next(0, 1000000).ToString("D6");

            user.ResetToken = otp;
            user.ResetTokenExpiry = DateTime.UtcNow.AddMinutes(10);
            await _db.SaveChangesAsync();

            _cache.Set($"pending-password:{dto.Email}", _passwords.Hash(dto.NewPassword), TimeSpan.FromMinutes(10));

            var fullName = $"{user.FirstName} {user.LastName}";
            await _email.SendOtpEmailAsync(user.Email, fullName, otp);

            return Ok(new ApiMessageDto("A verification code has been sent to your email."));
        }

        [HttpPost("forgot-password/verify-otp")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiMessageDto>> VerifyOtp(ForgotPasswordVerifyOtpDto dto)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email && u.IsActive);
            if (user == null)
                return NotFound(new ApiMessageDto("We couldn't find an account with that email."));

            if (string.IsNullOrEmpty(user.ResetToken))
                return BadRequest(new ApiMessageDto("Please request a new code first."));

            if (user.ResetTokenExpiry == null || user.ResetTokenExpiry < DateTime.UtcNow)
                return BadRequest(new ApiMessageDto("This code has expired. Please request a new one."));

            if (user.ResetToken != dto.Otp)
                return BadRequest(new ApiMessageDto("Incorrect code. Please check your email and try again."));

            if (!_cache.TryGetValue($"pending-password:{dto.Email}", out string? pendingHash) || pendingHash == null)
                return BadRequest(new ApiMessageDto("Your session expired - please start over and request a new code."));

            user.PasswordHash = pendingHash;
            user.ResetToken = null;
            user.ResetTokenExpiry = null;
            await _db.SaveChangesAsync();

            _cache.Remove($"pending-password:{dto.Email}");

            return Ok(new ApiMessageDto("Your password has been reset successfully. You can now log in."));
        }

        private static (string FirstName, string LastName) SplitName(string fullName)
        {
            var trimmed = fullName.Trim();
            var spaceIndex = trimmed.IndexOf(' ');
            return spaceIndex < 0
                ? (trimmed, string.Empty)
                : (trimmed[..spaceIndex], trimmed[(spaceIndex + 1)..]);
        }

 
        private static string? ValidatePasswordStrength(string password)
        {
            if (password.Length < 6 || password.Length > 15)
                return "Password must be 6-15 characters long.";
            if (!password.Any(char.IsUpper))
                return "Password must include at least one uppercase letter.";
            if (!password.Any(char.IsDigit))
                return "Password must include at least one number.";
            if (password.All(char.IsLetterOrDigit))
                return "Password must include at least one special character (e.g. ! @ # $).";

            return null;
        }
    }
}
