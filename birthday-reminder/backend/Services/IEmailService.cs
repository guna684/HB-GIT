using backend.DTOs;

namespace backend.Services;

/**
 * =============================================================================
 * INTERFACE: IEmailService
 * =============================================================================
 * Blueprint for sending email notifications in our application.
 * 
 * Why use an interface for email?
 * 1. Flexibility: We can swap between local development console logging, SMTP
 *    (Gmail/Outlook), SendGrid, or Amazon SES without changing any controller code!
 * 2. Testability: Easy to test login and reminder flows.
 */
public interface IEmailService
{
    /// <summary>
    /// Sends a sign-in / login notification email to the user who just logged in.
    /// Automatically includes a list of upcoming birthdays so the user is immediately reminded!
    /// </summary>
    Task SendLoginNotificationAsync(string toEmail, string userName, DateTime loginTime, List<UpcomingBirthdayReminder>? upcomingBirthdays = null);

    /// <summary>
    /// Sends a friendly welcome email when a user registers a new account.
    /// </summary>
    Task SendWelcomeEmailAsync(string toEmail, string userName);

    /// <summary>
    /// Sends an upcoming birthday reminder alert for a specific friend or family member.
    /// (Used by the background worker service).
    /// </summary>
    Task SendBirthdayReminderAlertAsync(string toEmail, string userName, string personName, string relationship, int turningAge, int daysUntil, DateTime dateOfBirth, string? notes);

    /// <summary>
    /// Sends a 6-digit OTP code to a user who requested a password reset.
    /// </summary>
    Task SendPasswordResetOtpAsync(string toEmail, string userName, string otp);

    /// <summary>
    /// Sends a security confirmation email after a user's password has been successfully updated.
    /// </summary>
    Task SendPasswordChangedNotificationAsync(string toEmail, string userName);

    /// <summary>
    /// Generic method to send an HTML email with a custom subject and body.
    /// </summary>
    Task SendEmailAsync(string toEmail, string subject, string bodyHtml);
}

