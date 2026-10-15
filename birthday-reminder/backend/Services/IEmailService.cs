namespace backend.Services;

/**
 * =============================================================================
 * INTERFACE: IEmailService
 * =============================================================================
 * Blueprint for sending email notifications in our application.
 * 
 * Why use an interface for email?
 * 1. Flexibility: We can swap between local development logging, SMTP (Gmail/Outlook),
 *    SendGrid, or Amazon SES without changing any controller code!
 * 2. Testability: Easy to test login and reminder flows.
 */
public interface IEmailService
{
    /// <summary>
    /// Sends a security notification email whenever a user logs into their account.
    /// </summary>
    /// <param name="toEmail">The user's registered email address.</param>
    /// <param name="userName">The user's full name.</param>
    /// <param name="loginTime">The date and time of the login event.</param>
    Task SendLoginNotificationAsync(string toEmail, string userName, DateTime loginTime);

    /// <summary>
    /// Generic method to send an HTML email with a custom subject and body.
    /// </summary>
    /// <param name="toEmail">Recipient's email address.</param>
    /// <param name="subject">Email subject line.</param>
    /// <param name="bodyHtml">Email content in HTML format.</param>
    Task SendEmailAsync(string toEmail, string subject, string bodyHtml);
}
