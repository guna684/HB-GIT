using System.Net;
using System.Net.Mail;
using System.Text;
using backend.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace backend.Services;

/**
 * =============================================================================
 * SERVICE: EmailService
 * =============================================================================
 * Handles sending email notifications (login alerts with upcoming birthday reminders,
 * welcome emails, and scheduled birthday alerts).
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
    /// Sends a sign-in notification to the user who logged in, automatically including
    /// a list of their upcoming birthdays so they are immediately reminded of celebrations!
    /// </summary>
    public async Task SendLoginNotificationAsync(
        string toEmail,
        string userName,
        DateTime loginTime,
        List<UpcomingBirthdayReminder>? upcomingBirthdays = null)
    {
        upcomingBirthdays ??= new List<UpcomingBirthdayReminder>();

        int count = upcomingBirthdays.Count;
        string subject = count > 0
            ? $"🔔 Login Alert - You have {count} Upcoming Birthday{(count > 1 ? "s" : "")} to Remember!"
            : "🔔 Login Notification - Birthday Reminder App";

        // Build HTML for the list of upcoming birthdays
        StringBuilder birthdaysHtml = new();
        if (count > 0)
        {
            birthdaysHtml.Append(@"
    <div style=""background-color: #fdf2f8; border: 1px solid #fbcfe8; border-radius: 10px; padding: 18px; margin: 20px 0;"">
      <h3 style=""color: #be185d; margin: 0 0 12px 0; font-size: 16px; display: flex; align-items: center;"">
        🎂 Upcoming Birthdays to Remember:
      </h3>
      <table style=""width: 100%; border-collapse: collapse;"">");

            foreach (var b in upcomingBirthdays)
            {
                string badgeBg = b.DaysUntil == 0 ? "#fce7f3" : (b.DaysUntil == 1 ? "#ffedd5" : "#e0e7ff");
                string badgeColor = b.DaysUntil == 0 ? "#be185d" : (b.DaysUntil == 1 ? "#c2410c" : "#4338ca");
                string badgeText = b.DaysUntil == 0 ? "🎉 TODAY!" : (b.DaysUntil == 1 ? "🎈 TOMORROW" : $"🗓 In {b.DaysUntil} days");

                birthdaysHtml.Append($@"
        <tr style=""border-bottom: 1px solid #fce7f3;"">
          <td style=""padding: 10px 0;"">
            <span style=""background-color: {badgeBg}; color: {badgeColor}; padding: 3px 10px; border-radius: 9999px; font-weight: 700; font-size: 11px; text-transform: uppercase; letter-spacing: 0.5px;"">
              {badgeText}
            </span>
            <div style=""font-size: 15px; font-weight: 650; color: #0f172a; margin-top: 5px;"">
              {b.PersonName} <span style=""font-weight: 400; color: #64748b; font-size: 13px;"">({b.Relationship})</span>
            </div>
            <div style=""font-size: 13px; color: #475569; margin-top: 2px;"">
              Turning <strong>{b.TurningAge}</strong> on {b.DateOfBirth:MMMM dd}
              {(string.IsNullOrWhiteSpace(b.Notes) ? "" : $" &bull; <span style=\"color: #64748b; font-style: italic;\">{b.Notes}</span>")}
            </div>
          </td>
        </tr>");
            }

            birthdaysHtml.Append(@"
      </table>
      <p style=""margin: 12px 0 0 0; font-size: 13px; color: #9d174d; font-weight: 500;"">
        Don't forget to send them your warmest wishes! 🎁
      </p>
    </div>");
        }
        else
        {
            birthdaysHtml.Append(@"
    <div style=""background-color: #f8fafc; border: 1px solid #e2e8f0; border-radius: 10px; padding: 16px; margin: 20px 0;"">
      <p style=""margin: 0; color: #475569; font-size: 14px;"">
        ✨ <strong>No immediate birthdays</strong> coming up in the next 30 days. You're all caught up!
      </p>
      <p style=""margin: 6px 0 0 0; color: #64748b; font-size: 13px;"">
        Tip: Remember to add new friends and family on your dashboard to never miss another special day.
      </p>
    </div>");
        }

        // Full Responsive HTML Template
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
    .alert-box {{ background-color: #eff6ff; border-left: 4px solid #3b82f6; padding: 14px; border-radius: 8px; margin: 16px 0; }}
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
      <p style=""margin: 0; color: #1e40af; font-size: 14px;"">
        <strong>Sign-in detected:</strong> Logged into your account on <strong>{loginTime:dddd, MMMM dd, yyyy h:mm tt}</strong>.
      </p>
    </div>

    {birthdaysHtml}

    <p style=""color: #475569; font-size: 13px; line-height: 1.5;"">
      If this was you, you can safely ignore this email.
    </p>

    <p style=""color: #ef4444; font-size: 12px; background-color: #fef2f2; padding: 10px; border-radius: 6px; margin-top: 16px;"">
      ⚠️ <strong>Didn't sign in?</strong> If you did not sign in recently, someone else may have access to your account. Please change your password immediately.
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
    /// Sends a welcome email to a newly registered user.
    /// </summary>
    public async Task SendWelcomeEmailAsync(string toEmail, string userName)
    {
        string subject = "🎉 Welcome to Birthday Reminder!";

        string bodyHtml = $@"
<!DOCTYPE html>
<html>
<head>
  <meta charset=""utf-8"">
  <style>
    body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f8fafc; margin: 0; padding: 20px; }}
    .container {{ max-width: 580px; margin: 0 auto; background-color: #ffffff; border-radius: 14px; padding: 32px; border: 1px solid #e2e8f0; }}
    .header {{ text-align: center; margin-bottom: 24px; }}
    .header h1 {{ color: #6366F1; margin: 8px 0; font-size: 24px; }}
    .card {{ background-color: #f0fdf4; border: 1px solid #bbf7d0; padding: 16px; border-radius: 8px; margin: 16px 0; }}
    .footer {{ text-align: center; color: #94a3b8; font-size: 12px; margin-top: 24px; border-top: 1px solid #e2e8f0; padding-top: 16px; }}
  </style>
</head>
<body>
  <div class=""container"">
    <div class=""header"">
      <div style=""font-size: 40px;"">🎈</div>
      <h1>Welcome to Birthday Reminder!</h1>
    </div>

    <p style=""font-size: 16px; color: #0f172a;"">Hi <strong>{userName}</strong>,</p>

    <div class=""card"">
      <p style=""margin: 0; color: #166534; font-weight: 600;"">
        Your account has been successfully created!
      </p>
      <p style=""margin: 6px 0 0 0; color: #15803d; font-size: 14px;"">
        You can now track birthdays of your friends, family, and colleagues, and receive automated reminders so you never forget another celebration.
      </p>
    </div>

    <h3 style=""color: #334155; font-size: 16px;"">Quick Tips to Get Started:</h3>
    <ul style=""color: #475569; font-size: 14px; line-height: 1.6;"">
      <li><strong>Add a Birthday:</strong> Click ""+ Add Birthday"" on your dashboard.</li>
      <li><strong>Set Reminder Days:</strong> Choose how many days in advance you want an alert (e.g. 1 day or 7 days).</li>
      <li><strong>Attach Photos:</strong> Upload a picture to make each card special!</li>
    </ul>

    <div class=""footer"">
      <p>Birthday Reminder App &bull; Automated notification</p>
    </div>
  </div>
</body>
</html>";

        await SendEmailAsync(toEmail, subject, bodyHtml);
    }

    /// <summary>
    /// Sends a reminder alert for a specific friend or family member's upcoming birthday.
    /// Used by the background worker service.
    /// </summary>
    public async Task SendBirthdayReminderAlertAsync(
        string toEmail,
        string userName,
        string personName,
        string relationship,
        int turningAge,
        int daysUntil,
        DateTime dateOfBirth,
        string? notes)
    {
        string subject = daysUntil == 0
            ? $"🎉 TODAY is {personName}'s Birthday!"
            : $"🎂 Reminder: {personName}'s Birthday is in {daysUntil} day{(daysUntil > 1 ? "s" : "")}!";

        string timeText = daysUntil == 0 ? "TODAY! 🎉" : (daysUntil == 1 ? "TOMORROW 🎈" : $"In {daysUntil} days");

        string bodyHtml = $@"
<!DOCTYPE html>
<html>
<head>
  <meta charset=""utf-8"">
  <style>
    body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f8fafc; margin: 0; padding: 20px; }}
    .container {{ max-width: 580px; margin: 0 auto; background-color: #ffffff; border-radius: 14px; padding: 32px; border: 1px solid #e2e8f0; }}
    .header {{ text-align: center; margin-bottom: 24px; }}
    .header h1 {{ color: #6366F1; margin: 8px 0; font-size: 24px; }}
    .card {{ background-color: #fdf2f8; border: 1px solid #fbcfe8; padding: 20px; border-radius: 10px; margin: 20px 0; }}
    .footer {{ text-align: center; color: #94a3b8; font-size: 12px; margin-top: 24px; border-top: 1px solid #e2e8f0; padding-top: 16px; }}
  </style>
</head>
<body>
  <div class=""container"">
    <div class=""header"">
      <div style=""font-size: 40px;"">🎂</div>
      <h1>Birthday Reminder Alert</h1>
    </div>

    <p style=""font-size: 16px; color: #0f172a;"">Hi <strong>{userName}</strong>,</p>

    <div class=""card"">
      <h2 style=""color: #be185d; margin: 0 0 10px 0; font-size: 20px;"">{personName}</h2>
      <p style=""margin: 4px 0; color: #334155; font-size: 15px;""><strong>Relationship:</strong> {relationship}</p>
      <p style=""margin: 4px 0; color: #334155; font-size: 15px;""><strong>Turning Age:</strong> {turningAge}</p>
      <p style=""margin: 4px 0; color: #334155; font-size: 15px;""><strong>Date:</strong> {dateOfBirth:MMMM dd} ({timeText})</p>
      {(string.IsNullOrWhiteSpace(notes) ? "" : $"<p style=\"margin: 6px 0 0 0; color: #64748b; font-size: 14px;\"><strong>Notes:</strong> {notes}</p>")}
    </div>

    <p style=""color: #475569; font-size: 14px;"">
      Don't forget to send a message, call, or prepare a gift to celebrate!
    </p>

    <div class=""footer"">
      <p>Birthday Reminder App &bull; Automated notification</p>
    </div>
  </div>
</body>
</html>";

        await SendEmailAsync(toEmail, subject, bodyHtml);
    }

    /// <summary>
    /// Sends a 6-digit OTP verification code to a user who requested a password reset.
    /// </summary>
    public async Task SendPasswordResetOtpAsync(string toEmail, string userName, string otp)
    {
        string subject = "🔐 Your Password Reset Verification Code";

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
    .otp-box {{ background-color: #eef2ff; border: 2px dashed #6366f1; border-radius: 12px; padding: 20px; text-align: center; margin: 24px 0; }}
    .otp-code {{ font-size: 36px; font-weight: 800; letter-spacing: 8px; color: #4338ca; font-family: monospace; }}
    .notice {{ background-color: #fef3c7; border-left: 4px solid #f59e0b; padding: 12px; border-radius: 6px; margin: 16px 0; color: #92400e; font-size: 13px; }}
    .footer {{ text-align: center; color: #94a3b8; font-size: 12px; margin-top: 24px; border-top: 1px solid #e2e8f0; padding-top: 16px; }}
  </style>
</head>
<body>
  <div class=""container"">
    <div class=""header"">
      <div style=""font-size: 40px;"">🔐</div>
      <h1>Password Reset Request</h1>
    </div>

    <p style=""font-size: 16px; color: #0f172a;"">Hi <strong>{userName}</strong>,</p>

    <p style=""color: #475569; font-size: 14px; line-height: 1.6;"">
      We received a request to reset your password for your Birthday Reminder account. Use the 6-digit verification code below to complete the reset:
    </p>

    <div class=""otp-box"">
      <div style=""font-size: 12px; text-transform: uppercase; color: #6366f1; font-weight: 700; margin-bottom: 6px;"">Your Verification Code</div>
      <div class=""otp-code"">{otp}</div>
    </div>

    <div class=""notice"">
      ⏳ <strong>This code expires in 10 minutes</strong> and can only be used once.
    </div>

    <p style=""color: #64748b; font-size: 13px; line-height: 1.5;"">
      If you did not request a password reset, you can safely ignore this email. Your current password remains secure.
    </p>

    <div class=""footer"">
      <p>Birthday Reminder App &bull; Automated security notification</p>
    </div>
  </div>
</body>
</html>";

        await SendEmailAsync(toEmail, subject, bodyHtml);
    }

    /// <summary>
    /// Sends a security confirmation email after a user's password has been successfully updated.
    /// </summary>
    public async Task SendPasswordChangedNotificationAsync(string toEmail, string userName)
    {
        string subject = "🔒 Your Password Was Successfully Changed";

        string bodyHtml = $@"
<!DOCTYPE html>
<html>
<head>
  <meta charset=""utf-8"">
  <style>
    body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f8fafc; margin: 0; padding: 20px; }}
    .container {{ max-width: 580px; margin: 0 auto; background-color: #ffffff; border-radius: 14px; padding: 32px; border: 1px solid #e2e8f0; }}
    .header {{ text-align: center; margin-bottom: 24px; }}
    .header h1 {{ color: #10b981; margin: 8px 0; font-size: 24px; }}
    .card {{ background-color: #f0fdf4; border: 1px solid #bbf7d0; padding: 16px; border-radius: 8px; margin: 16px 0; }}
    .warning {{ background-color: #fef2f2; border: 1px solid #fecaca; padding: 14px; border-radius: 8px; margin: 16px 0; color: #991b1b; font-size: 13px; }}
    .footer {{ text-align: center; color: #94a3b8; font-size: 12px; margin-top: 24px; border-top: 1px solid #e2e8f0; padding-top: 16px; }}
  </style>
</head>
<body>
  <div class=""container"">
    <div class=""header"">
      <div style=""font-size: 40px;"">🔒</div>
      <h1>Password Changed</h1>
    </div>

    <p style=""font-size: 16px; color: #0f172a;"">Hi <strong>{userName}</strong>,</p>

    <div class=""card"">
      <p style=""margin: 0; color: #166534; font-weight: 600;"">
        Your password has been changed successfully.
      </p>
      <p style=""margin: 6px 0 0 0; color: #15803d; font-size: 14px;"">
        You can now use your new password to sign into your Birthday Reminder account.
      </p>
    </div>

    <div class=""warning"">
      ⚠️ <strong>Didn't make this change?</strong> If you did not change your password, someone may have unauthorized access to your account. Please reset your password immediately or contact support.
    </div>

    <div class=""footer"">
      <p>Birthday Reminder App &bull; Automated security notification</p>
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
                "📧 [EMAIL SERVICE - NOTIFICATION SIMULATOR]\n" +
                "To:      {ToEmail}\n" +
                "Subject: {Subject}\n" +
                "Status:  Sent successfully (Simulated Console Delivery)\n" +
                "Content Preview:\n{BodyPreview}\n" +
                "------------------------------------------------------------\n" +
                "💡 Note for Beginners: To send real emails via Brevo SMTP,\n" +
                "configure 'EmailSettings' with your SMTP details in appsettings.json!\n" +
                "============================================================",
                toEmail,
                subject,
                bodyHtml
            );
            return;
        }

        // 3. Real SMTP Delivery (e.g. Brevo SMTP relay)
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
            // Log the error clearly (without exposing password) and DO NOT throw, so the API & background worker never crash!
            _logger.LogError(ex, "Failed to send email to {ToEmail} via SMTP ({SmtpServer}:{Port}). Please verify EmailSettings in appsettings.json.", toEmail, smtpServer, port);
        }
    }
}
