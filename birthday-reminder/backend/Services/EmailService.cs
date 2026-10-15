using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace backend.Services;

/**
 * =============================================================================
 * SERVICE: EmailService
 * =============================================================================
 * Handles sending email notifications (like login alerts and birthday reminders).
 * 
 * Key Features for Beginners:
 * 1. Zero Setup Required in Development:
 *    If you haven't configured SMTP credentials yet in appsettings.json,
 *    it gracefully prints the formatted email directly in the terminal console!
 * 2. Real SMTP Sending:
 *    When you add your SMTP credentials (like Gmail App Password, Outlook, or
 *    SendGrid), it sends real emails over the internet using SSL/TLS.
 * 3. Safe & Non-Blocking:
 *    If an email fails to deliver (e.g., SMTP down or no internet), the error
 *    is caught and logged without crashing the user's login experience!
 */
public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Sends a security notification email when a user logs in.
    /// </summary>
    public async Task SendLoginNotificationAsync(string toEmail, string userName, DateTime loginTime)
    {
        string subject = "🔔 Login Notification - Birthday Reminder App";

        // HTML Email Template with clean inline styling
        string bodyHtml = $@"
<!DOCTYPE html>
<html>
<head>
  <meta charset=""utf-8"">
  <style>
    body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f8fafc; margin: 0; padding: 20px; }}
    .container {{ max-width: 580px; margin: 0 auto; background-color: #ffffff; border-radius: 14px; padding: 32px; box-shadow: 0 4px 6px rgba(0,0,0,0.05); border: 1px solid #e2e8f0; }}
    .header {{ text-align: center; margin-bottom: 24px; }}
    .header h1 {{ color: #6366F1; margin: 8px 0; font-size: 24px; }}
    .alert-box {{ background-color: #eff6ff; border-left: 4px solid #3b82f6; padding: 16px; border-radius: 8px; margin: 20px 0; }}
    .details {{ background-color: #f8fafc; border-radius: 8px; padding: 16px; margin: 20px 0; }}
    .details-row {{ display: flex; justify-content: space-between; padding: 6px 0; border-bottom: 1px solid #e2e8f0; }}
    .details-row:last-child {{ border-bottom: none; }}
    .label {{ color: #64748b; font-size: 14px; font-weight: 500; }}
    .value {{ color: #0f172a; font-size: 14px; font-weight: 600; }}
    .warning {{ color: #ef4444; font-size: 13px; margin-top: 16px; }}
    .footer {{ text-align: center; color: #94a3b8; font-size: 12px; margin-top: 24px; border-top: 1px solid #e2e8f0; padding-top: 16px; }}
  </style>
</head>
<body>
  <div class=""container"">
    <div class=""header"">
      <div style=""font-size: 40px;"">🎂</div>
      <h1>Birthday Reminder</h1>
    </div>

    <p style=""font-size: 16px; color: #0f172a;"">Hi <strong>{userName}</strong>,</p>

    <div class=""alert-box"">
      <p style=""margin: 0; color: #1e40af; font-weight: 600;"">
        A new login was detected on your account.
      </p>
    </div>

    <div class=""details"">
      <div style=""padding: 4px 0;""><strong>Account:</strong> {toEmail}</div>
      <div style=""padding: 4px 0;""><strong>Date & Time:</strong> {loginTime:dddd, MMMM dd, yyyy h:mm tt}</div>
    </div>

    <p style=""color: #475569; font-size: 14px; line-height: 1.5;"">
      If this was you, you can safely ignore this email.
    </p>

    <p class=""warning"">
      ⚠️ <strong>Didn't log in?</strong> If you did not sign in recently, someone else may have access to your credentials. Please change your password immediately.
    </p>

    <div class=""footer"">
      <p>This is an automated notification from Birthday Reminder App.</p>
    </div>
  </div>
</body>
</html>";

        await SendEmailAsync(toEmail, subject, bodyHtml);
    }

    /// <summary>
    /// Sends an email via SMTP or outputs it to the console if SMTP is not configured.
    /// </summary>
    public async Task SendEmailAsync(string toEmail, string subject, string bodyHtml)
    {
        // 1. Read SMTP configuration from appsettings.json
        string? smtpServer = _configuration["EmailSettings:SmtpServer"];
        int port = 587;
        if (int.TryParse(_configuration["EmailSettings:Port"], out int configuredPort))
        {
            port = configuredPort;
        }

        string senderEmail = _configuration["EmailSettings:SenderEmail"] ?? "noreply@birthdayreminder.com";
        string senderName = _configuration["EmailSettings:SenderName"] ?? "Birthday Reminder App";
        string? username = _configuration["EmailSettings:Username"];
        string? password = _configuration["EmailSettings:Password"];
        bool enableSsl = true;
        if (bool.TryParse(_configuration["EmailSettings:EnableSsl"], out bool configuredSsl))
        {
            enableSsl = configuredSsl;
        }

        // 2. Check if real SMTP credentials are provided
        bool hasSmtpConfigured = !string.IsNullOrWhiteSpace(smtpServer) &&
                                 !string.IsNullOrWhiteSpace(username) &&
                                 !string.IsNullOrWhiteSpace(password) &&
                                 !password.Contains("YOUR_PASSWORD", StringComparison.OrdinalIgnoreCase);

        if (!hasSmtpConfigured)
        {
            // Development Mode: Display friendly simulation in the terminal logs!
            _logger.LogInformation(
                "\n============================================================\n" +
                "📧 [EMAIL SERVICE - LOGIN NOTIFICATION SIMULATOR]\n" +
                "To:      {ToEmail}\n" +
                "Subject: {Subject}\n" +
                "Status:  Sent successfully (Simulated Console Delivery)\n" +
                "------------------------------------------------------------\n" +
                "💡 Note for Beginners: To send real emails to your Gmail/Outlook inbox,\n" +
                "simply configure 'EmailSettings' with your SMTP details in appsettings.json!\n" +
                "============================================================",
                toEmail,
                subject
            );
            return;
        }

        // 3. Real SMTP Delivery
        try
        {
            using var mailMessage = new MailMessage
            {
                From = new MailAddress(senderEmail, senderName),
                Subject = subject,
                Body = bodyHtml,
                IsBodyHtml = true
            };
            mailMessage.To.Add(toEmail);

            using var smtpClient = new SmtpClient(smtpServer, port)
            {
                Credentials = new NetworkCredential(username, password),
                EnableSsl = enableSsl
            };

            await smtpClient.SendMailAsync(mailMessage);
            _logger.LogInformation("Successfully sent email to {ToEmail} via SMTP ({SmtpServer}:{Port})", toEmail, smtpServer, port);
        }
        catch (Exception ex)
        {
            // Log the error but DO NOT throw, so the user can still log in smoothly!
            _logger.LogError(ex, "Failed to send email to {ToEmail} via SMTP ({SmtpServer}). Please verify EmailSettings in appsettings.json.", toEmail, smtpServer);
        }
    }
}
