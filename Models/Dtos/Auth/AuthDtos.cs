using System.ComponentModel.DataAnnotations;

namespace FixMyTownApi.Models.Dtos.Auth
{
    /// <summary>What a new citizen sends when signing up.</summary>
    public class RegisterDto
    {
        [Required] public string FullName { get; set; } = string.Empty;
        [Required, EmailAddress] public string Email { get; set; } = string.Empty;
        [Required] public string Password { get; set; } = string.Empty;
        public string? Phone { get; set; }
    }

    /// <summary>What any user (citizen/admin/worker) sends to log in.</summary>
    public class LoginDto
    {
        [Required, EmailAddress] public string Email { get; set; } = string.Empty;
        [Required] public string Password { get; set; } = string.Empty;

        // "citizen" | "admin" | "worker" - must match the role tab
        // the person picked on the login screen
        [Required] public string Role { get; set; } = string.Empty;
    }

    /// <summary>
    /// What we hand back after a successful login/register.
    ///
    /// NOTE ON CASING: these property names are lowercase-first on
    /// purpose ("message", "token", "user") - the frontend's
    /// AuthContext.jsx reads response.data.token and response.data.user
    /// directly, so the JSON keys must match exactly.
    /// </summary>
    public class AuthResponseDto
    {
        public string message { get; set; } = string.Empty;
        public string token { get; set; } = string.Empty;
        public UserSummaryDto user { get; set; } = null!;
    }

    /// <summary>
    /// The small, safe slice of a user's info the frontend needs
    /// (never the password hash). Lowercase-first property names to
    /// match user.fullName / user.email / user.role usage across the
    /// frontend (e.g. Sidebar.jsx, ProtectedRoute.jsx).
    /// </summary>
    public class UserSummaryDto
    {
        public int id { get; set; }
        public string fullName { get; set; } = string.Empty;
        public string email { get; set; } = string.Empty;
        public string role { get; set; } = string.Empty;
    }

    // ---------- Forgot Password flow ----------
    // Step 1: citizen types their email -> CheckEmailDto
    // Step 2: if it exists, they type a new password -> RequestOtpDto (OTP gets emailed)
    // Step 3: they type the OTP from their email -> VerifyOtpDto (password actually changes here)

    /// <summary>Step 1 - just the email, to check the account exists before continuing.</summary>
    public class ForgotPasswordCheckEmailDto
    {
        [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    }

    /// <summary>Step 2 - the new password the citizen wants, before it's confirmed by OTP.</summary>
    public class ForgotPasswordRequestOtpDto
    {
        [Required, EmailAddress] public string Email { get; set; } = string.Empty;
        [Required] public string NewPassword { get; set; } = string.Empty;
    }

    /// <summary>Step 3 - the 6-digit code from their email, which finalizes the password change.</summary>
    public class ForgotPasswordVerifyOtpDto
    {
        [Required, EmailAddress] public string Email { get; set; } = string.Empty;
        [Required] public string Otp { get; set; } = string.Empty;
    }
}
