namespace backend.Models;

/// <summary>
/// Represents a birthday entry saved by a user.
/// </summary>
public class Birthday
{
    // Unique identifier for this birthday record
    public int Id { get; set; }

    // Id of the user who owns this birthday reminder (for security and privacy)
    public int UserId { get; set; }

    // Name of the friend, family member, or colleague
    public string PersonName { get; set; } = string.Empty;

    // Date of birth (day, month, year)
    public DateTime DateOfBirth { get; set; }

    // Category: "Family", "Friend", "Work", or "Other"
    public string Relationship { get; set; } = "Friend";

    // Optional notes like gift ideas or memories
    public string? Notes { get; set; }

    // Name of the photo file saved in wwwroot/uploads (e.g. "guid.jpg")
    public string? PhotoFileName { get; set; }

    // How many days before the birthday to show reminders (default 1 day before)
    public int RemindDaysBefore { get; set; } = 1;
}
