using backend.Models;
using backend.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace backend.Services;

/**
 * =============================================================================
 * BACKGROUND SERVICE: ReminderBackgroundService
 * =============================================================================
 * What is a BackgroundService?
 * Most code in an ASP.NET Core API only runs when a user clicks something in their
 * browser (an HTTP request arrives).
 * 
 * But what if nobody visits the website for 3 days? How do reminder emails or
 * notifications get triggered?
 * 
 * That's why BackgroundService exists! It is a background worker loop that runs
 * continuously on the server (e.g. once every hour), independent of user visits.
 */
public class ReminderBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ReminderBackgroundService> _logger;

    public ReminderBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<ReminderBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// The main background execution loop. Runs when the server starts and stops
    /// when the server shuts down.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ReminderBackgroundService started.");

        // Loop runs as long as the backend server is alive
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckBirthdaysAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while checking birthday reminders.");
            }

            // Sleep for 1 hour before checking again
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }

        _logger.LogInformation("ReminderBackgroundService is stopping.");
    }

    /// <summary>
    /// Inspects all birthdays in the database to see if any occur today or within
    /// their configured reminder threshold (e.g. 1 day before).
    /// </summary>
    private async Task CheckBirthdaysAsync()
    {
        // WHY CREATE A SCOPE?
        // ReminderBackgroundService is a Singleton (lives forever).
        // Database contexts and repositories are Scoped (created per request).
        // Creating an IServiceScope here lets us safely request the Scoped repository!
        using (IServiceScope scope = _serviceProvider.CreateScope())
        {
            IBirthdayRepository birthdayRepository = scope.ServiceProvider.GetRequiredService<IBirthdayRepository>();
            IUserRepository userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            IEmailService emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

            List<Birthday> allBirthdays = await birthdayRepository.GetAllAsync();
            DateTime today = DateTime.Today;

            foreach (Birthday birthday in allBirthdays)
            {
                (int daysUntil, int turningAge) = BirthdayService.CalculateBirthdayStats(birthday.DateOfBirth, today);

                // Check if the upcoming birthday is within the user's alert window
                if (daysUntil <= birthday.RemindDaysBefore)
                {
                    string message;
                    if (daysUntil == 0)
                    {
                        message = $"🎂 Alert: {birthday.PersonName}'s birthday is TODAY! (turning {turningAge})";
                    }
                    else if (daysUntil == 1)
                    {
                        message = $"🎂 Alert: {birthday.PersonName}'s birthday is TOMORROW (turning {turningAge})";
                    }
                    else
                    {
                        message = $"🎂 Alert: {birthday.PersonName}'s birthday is in {daysUntil} days (turning {turningAge})";
                    }

                    _logger.LogInformation(message);

                    // Send reminder email to the owner of this birthday
                    User? owner = await userRepository.GetByIdAsync(birthday.UserId);
                    if (owner != null && !string.IsNullOrWhiteSpace(owner.Email))
                    {
                        string emailSubject = daysUntil == 0
                            ? $"🎉 Today is {birthday.PersonName}'s Birthday!"
                            : $"🎂 Reminder: {birthday.PersonName}'s Birthday is in {daysUntil} day(s)!";

                        string emailHtml = $@"
<div style=""font-family: Arial, sans-serif; max-width: 580px; margin: 0 auto; padding: 24px; border: 1px solid #e2e8f0; border-radius: 12px;"">
  <h2 style=""color: #6366F1; margin-top: 0;"">🎂 Birthday Reminder Alert</h2>
  <p>Hi <strong>{owner.Name}</strong>,</p>
  <p>Don't forget to celebrate!</p>
  <div style=""background-color: #f1f5f9; padding: 16px; border-radius: 8px; margin: 16px 0;"">
    <p style=""margin: 4px 0; font-size: 16px;""><strong>Name:</strong> {birthday.PersonName}</p>
    <p style=""margin: 4px 0;""><strong>Relationship:</strong> {birthday.Relationship}</p>
    <p style=""margin: 4px 0;""><strong>Turning Age:</strong> {turningAge}</p>
    <p style=""margin: 4px 0;""><strong>When:</strong> {(daysUntil == 0 ? "TODAY! 🎉" : $"In {daysUntil} days")}</p>
    {(string.IsNullOrWhiteSpace(birthday.Notes) ? "" : $"<p style=\"margin: 4px 0;\"><strong>Notes:</strong> {birthday.Notes}</p>")}
  </div>
  <p style=""color: #64748b; font-size: 13px;"">Birthday Reminder App &bull; Automated notification</p>
</div>";

                        await emailService.SendEmailAsync(owner.Email, emailSubject, emailHtml);
                    }
                }
            }
        }
    }
}


