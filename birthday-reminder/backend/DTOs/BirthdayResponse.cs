namespace backend.DTOs;

/**
 * =============================================================================
 * DTO: BirthdayResponse
 * =============================================================================
 * Why not return the database entity 'Birthday' directly?
 * 
 * 1. Computed Fields:
 *    Age and countdown change every day! The database doesn't store them.
 *    Instead, the backend computes DaysUntilBirthday and TurningAge fresh on
 *    every request and includes them here.
 * 
 * 2. Photo URL:
 *    The database only stores the filename (e.g. "guid.jpg").
 *    This DTO converts it into a complete web URL:
 *    "http://localhost:5000/uploads/guid.jpg" so React can display it directly!
 */
public class BirthdayResponse
{
    // Unique identifier for the birthday
    public int Id { get; set; }

    // Name of the person
    public string PersonName { get; set; } = string.Empty;

    // The birth date (day, month, year)
    public DateTime DateOfBirth { get; set; }

    // Relationship category: "Family", "Friend", "Work", or "Other"
    public string Relationship { get; set; } = string.Empty;

    // Optional gift ideas, memories, or notes
    public string? Notes { get; set; }

    // True if a profile photo is saved in PostgreSQL for this person
    public bool HasPhoto { get; set; }

    // URL to fetch photo from API: /api/birthdays/{id}/photo (null if no photo)
    public string? PhotoUrl { get; set; }

    // Number of days until their next birthday (0 = TODAY!)
    public int DaysUntilBirthday { get; set; }

    // The age they will turn on their upcoming birthday
    public int TurningAge { get; set; }

    // How many days in advance the user wants reminder alerts
    public int RemindDaysBefore { get; set; }
}

