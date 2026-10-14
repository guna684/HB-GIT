using backend.Models;
using backend.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace backend.Services;

/// <summary>
/// A background worker that runs periodically (every 1 hour) to check for upcoming birthdays
/// and logs reminder alerts.
/// </summary>
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
    /// Executes the background loop while the application is running.
    /// Runs immediately on startup, then pauses for 1 hour before checking again.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ReminderBackgroundService started.");

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

            // Wait 1 hour before checking again
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }

        _logger.LogInformation("ReminderBackgroundService is stopping.");
    }

    /// <summary>
    /// Loads all birthdays and logs alerts for any that fall within their reminder threshold.
    /// </summary>
    private async Task CheckBirthdaysAsync()
    {
        // Using an IServiceScope ensures this code will work without modifications
        // even when IBirthdayRepository is changed from InMemory (Singleton) to EF Core (Scoped).
        using (IServiceScope scope = _serviceProvider.CreateScope())
        {
            IBirthdayRepository repository = scope.ServiceProvider.GetRequiredService<IBirthdayRepository>();

            List<Birthday> allBirthdays = await repository.GetAllAsync();
            DateTime today = DateTime.Today;

            foreach (Birthday birthday in allBirthdays)
            {
                (int daysUntil, int turningAge) = BirthdayService.CalculateBirthdayStats(birthday.DateOfBirth, today);

                // Check if the birthday falls within the user's reminder window
                if (daysUntil <= birthday.RemindDaysBefore)
                {
                    string message;
                    if (daysUntil == 0)
                    {
                        message = $"Reminder: {birthday.PersonName}'s birthday is TODAY! (turning {turningAge})";
                    }
                    else if (daysUntil == 1)
                    {
                        message = $"Reminder: {birthday.PersonName}'s birthday is TOMORROW (turning {turningAge})";
                    }
                    else
                    {
                        message = $"Reminder: {birthday.PersonName}'s birthday is in {daysUntil} days (turning {turningAge})";
                    }

                    _logger.LogInformation(message);

                    // TODO: In the future, inject IEmailService and send an email or push notification to the user here.
                    // Example: await emailService.SendReminderEmailAsync(user.Email, birthday.PersonName, daysUntil);
                }
            }
        }
    }
}
