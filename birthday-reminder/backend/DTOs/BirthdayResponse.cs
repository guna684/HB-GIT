namespace backend.DTOs;

/// <summary>
/// Data returned to the client when viewing a birthday.
/// Includes calculated fields like daysUntilBirthday and turningAge,
/// and a full URL for the photo so the frontend can display it directly.
/// </summary>
public class BirthdayResponse
{
    // Unique identifier for the birthday
    public int Id { get; set; }

    // Name of the person
    public string PersonName { get; set; } = string.Empty;

    // The birth date
    public DateTime DateOfBirth { get; set; }

    // Relationship category (Family, Friend, Work, Other)
    public string Relationship { get; set; } = string.Empty;

    // Optional notes
    public string? Notes { get; set; }

    // Complete URL to access the photo (e.g., http://localhost:5000/uploads/xyz.jpg)
    // Will be null if no photo was uploaded
    public string? PhotoUrl { get; set; }

    // Number of days until their next birthday (0 means today!)
    public int DaysUntilBirthday { get; set; }

    // The age they will turn on their upcoming birthday
    public int TurningAge { get; set; }

    // How many days before their birthday the user requested a reminder
    public int RemindDaysBefore { get; set; }
}
