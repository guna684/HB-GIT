using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace backend.DTOs;

/// <summary>
/// Custom validation rule to ensure a date of birth is not set to a future date.
/// </summary>
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

/// <summary>
/// Data received when creating or updating a birthday.
/// Sent using multipart/form-data to support an optional image file upload.
/// </summary>
public class BirthdayRequest
{
    // Full name of the person whose birthday is being saved
    [Required(ErrorMessage = "Person name is required")]
    [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
    public string PersonName { get; set; } = string.Empty;

    // The date they were born (cannot be in the future)
    [Required(ErrorMessage = "Date of birth is required")]
    [NotFutureDate]
    public DateTime DateOfBirth { get; set; }

    // Relationship category: Family, Friend, Work, or Other
    [Required(ErrorMessage = "Relationship is required")]
    public string Relationship { get; set; } = "Friend";

    // Optional notes, gift ideas, or interests
    [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
    public string? Notes { get; set; }

    // Number of days in advance to remind the user (0 for day of, 1 for 1 day before, etc.)
    [Range(0, 30, ErrorMessage = "Remind days before must be between 0 and 30")]
    public int RemindDaysBefore { get; set; } = 1;

    // Optional photo file uploaded from the user's device (.jpg, .jpeg, .png, .webp)
    public IFormFile? Photo { get; set; }
}
