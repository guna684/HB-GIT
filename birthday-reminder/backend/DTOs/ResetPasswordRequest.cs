using System.ComponentModel.DataAnnotations;

namespace backend.DTOs;

/**
 * =============================================================================
 * DTO: ResetPasswordRequest
 * =============================================================================
 * Payload sent when submitting the 6-digit OTP and new password.
 */
public class ResetPasswordRequest
{
    [Required(ErrorMessage = "Email address is required.")]
    [EmailAddress(ErrorMessage = "Please provide a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "6-digit OTP code is required.")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "OTP code must be exactly 6 digits.")]
    public string Otp { get; set; } = string.Empty;

    [Required(ErrorMessage = "New password is required.")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters long.")]
    public string NewPassword { get; set; } = string.Empty;
}
