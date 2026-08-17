using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FixMyTownApi.Services
{
    /// <summary>
    /// Sends the two emails Fix MyTown needs: a welcome/confirmation
    /// email right after registration, and the "forgot password" OTP
    /// code.
    ///
    /// SETUP NEEDED: fill in the "Smtp" section of appsettings.json with
    /// real credentials before this can send actual emails - see the
    /// comments there for how to get a free Gmail "App Password".
    ///
    /// Until you do that (or if sending fails for any reason, e.g. no
    /// internet), every email is ALWAYS also written to this project's
    /// console/terminal output, so you can keep testing without needing
    /// real email delivery working yet.
    /// </summary>
    public class EmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration config, ILogger<EmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        /// <summary>Sent right after a citizen successfully registers.</summary>
        public Task SendWelcomeEmailAsync(string toEmail, string fullName)
        {
            var subject = "Welcome to Fix MyTown!";
            var body = $"Hi {fullName},\n\n" +
                       $"Your Fix MyTown citizen account has been created successfully " +
                       $"with this email address ({toEmail}).\n\n" +
                       $"You can now log in to report issues in your community - potholes, " +
                       $"broken street lights, illegal dumping, and more - and track their " +
                       $"progress until they're resolved.\n\n" +
                       $"If you didn't create this account, please ignore this email.";

            return SendEmailAsync(toEmail, subject, body, logLabel: $"Welcome email for {toEmail}");
        }

        /// <summary>Sent during the Forgot Password flow, Step 2 -> Step 3.</summary>
        public Task SendOtpEmailAsync(string toEmail, string fullName, string otpCode)
        {
            var subject = "Your Fix MyTown password reset code";
            var body = $"Hi {fullName},\n\n" +
                       $"Your one-time code to reset your Fix MyTown password is:\n\n" +
                       $"    {otpCode}\n\n" +
                       $"This code expires in 10 minutes. If you didn't request this, you can safely ignore this email.";

            return SendEmailAsync(toEmail, subject, body, logLabel: $"Password reset OTP for {toEmail}: {otpCode}");
        }

        /// <summary>
        /// Shared sending logic for both email types above. Always logs
        /// first (your safety net while SMTP isn't configured yet, or if
        /// sending ever fails), then attempts a real send only if the
        /// Smtp section in appsettings.json has actually been filled in.
        /// </summary>
        private async Task SendEmailAsync(string toEmail, string subject, string body, string logLabel)
        {
            _logger.LogWarning("{Label}", logLabel);

            var host = _config["Smtp:Host"];
            var fromAddress = _config["Smtp:From"];
            var username = _config["Smtp:Username"];

            if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(fromAddress) || string.IsNullOrWhiteSpace(username))
            {
                _logger.LogWarning("Smtp:Username/Smtp:From not configured in appsettings.json - email was only logged above, not actually sent.");
                return;
            }

            try
            {
                using var client = new SmtpClient(host, int.Parse(_config["Smtp:Port"] ?? "587"))
                {
                    Credentials = new NetworkCredential(_config["Smtp:Username"], _config["Smtp:Password"]),
                    EnableSsl = bool.Parse(_config["Smtp:EnableSsl"] ?? "true")
                };

                var message = new MailMessage
                {
                    From = new MailAddress(fromAddress, "Fix MyTown"),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = false
                };
                message.To.Add(toEmail);

                await client.SendMailAsync(message);
                _logger.LogInformation("Email sent successfully to {Email}: {Subject}", toEmail, subject);
            }
            catch (Exception ex)
            {
                // Don't let an email failure block registration or the OTP
                // flow - the content is already in the console log above.
                _logger.LogError(ex, "Failed to send email to {Email}", toEmail);
            }
        }
    }
}
