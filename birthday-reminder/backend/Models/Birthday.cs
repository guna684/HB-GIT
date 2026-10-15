namespace backend.Models;

/**
 * =============================================================================
 * ENTITY MODEL: Birthday
 * =============================================================================
 * What is an Entity Model?
 * This C# class represents a single row in our PostgreSQL "Birthdays" table
 * (or an item in our In-Memory list).
 * 
 * Every property corresponds to a column in the database table.
 */
public class Birthday
{
    // Unique Primary Key (auto-incremented integer: 1, 2, 3...)
    public int Id { get; set; }

    // Foreign Key: ID of the User who created this birthday reminder
    // This allows us to keep each user's data completely private!
    public int UserId { get; set; }

    // Name of the friend, family member, or coworker
    public string PersonName { get; set; } = string.Empty;

    // Date of birth (Year, Month, Day)
    public DateTime DateOfBirth { get; set; }

    // Category: "Family", "Friend", "Work", or "Other"
    public string Relationship { get; set; } = "Friend";

    // Optional notes like gift ideas or favorite hobbies
    public string? Notes { get; set; }

    // Profile photo binary bytes stored directly in PostgreSQL (BYTEA column)
    public byte[]? PhotoData { get; set; }

    // Image MIME type (e.g. "image/jpeg", "image/png", "image/webp")
    public string? PhotoContentType { get; set; }

    // How many days in advance to remind the user (0 = day of, 1 = 1 day before, etc.)
    public int RemindDaysBefore { get; set; } = 1;
}

