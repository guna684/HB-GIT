namespace backend.DTOs;

/**
 * =============================================================================
 * DTO: UpcomingBirthdayReminder
 * =============================================================================
 * A lightweight data transfer object used when summarizing upcoming birthdays
 * to include inside notification emails sent to users.
 * 
 * Why create this DTO?
 * It holds only the essential information needed to format a clear, beautiful
 * email alert without dragging unnecessary database entity fields along.
 */
public class UpcomingBirthdayReminder
{
    // The name of the friend or family member
    public string PersonName { get; set; } = string.Empty;

    // Relationship to the user (e.g. "Friend", "Family", "Colleague")
    public string Relationship { get; set; } = string.Empty;

    // Their original date of birth
    public DateTime DateOfBirth { get; set; }

    // How many days left until their next birthday (0 = TODAY!)
    public int DaysUntil { get; set; }

    // The age they will turn on their next birthday
    public int TurningAge { get; set; }

    // Optional user notes (e.g. gift ideas or party plans)
    public string? Notes { get; set; }
}
