using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace backend.DTOs;

/**
 * =============================================================================
 * CUSTOM VALIDATION ATTRIBUTE: NotFutureDateAttribute
 * =============================================================================
 * Prevents users from selecting a date of birth that is in the future.
 * ASP.NET Core runs this check automatically whenever a request arrives!
 */
public class NotFutureDateAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is DateTime date && date.Date > DateTime.Today)
        {
            return new ValidationResult("Date of birth cannot be in the future.");
        }
        return ValidationResult.Success;
    }
}

/**
 * =============================================================================
 * DTO: BirthdayRequest
 * =============================================================================
 * What is a DTO (Data Transfer Object)?
 * A DTO is a lightweight C# class designed specifically for carrying data
 * across the network between the React frontend and C# backend.
 * 
 * Notice:
 * It contains `IFormFile? Photo` to receive the binary profile photo file
 * uploaded from the user's browser.
 */
public class BirthdayRequest
{
    // Full name of the person (required, max 100 characters)
    [Required(ErrorMessage = "Person name is required")]
    [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
    public string PersonName { get; set; } = string.Empty;

    // Date of birth (cannot be in the future)
    [Required(ErrorMessage = "Date of birth is required")]
    [NotFutureDate]
    public DateTime DateOfBirth { get; set; }

    // Relationship category: "Family", "Friend", "Work", or "Other"
    [Required(ErrorMessage = "Relationship is required")]
    public string Relationship { get; set; } = "Friend";

    // Optional gift ideas, memories, or notes (up to 500 characters)
    [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
    public string? Notes { get; set; }

    // Reminder days before the birthday (0 = day of, 1 = 1 day before, etc.)
    [Range(0, 30, ErrorMessage = "Remind days before must be between 0 and 30")]
    public int RemindDaysBefore { get; set; } = 1;

    // The optional binary image file sent from <input type="file" />
    public IFormFile? Photo { get; set; }

    // If true, removes the current photo without uploading a replacement
    public bool RemovePhoto { get; set; } = false;
}

