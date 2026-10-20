using System.ComponentModel.DataAnnotations;

namespace backend.DTOs;

/**
 * =============================================================================
 * DTO: ForgotPasswordRequest
 * =============================================================================
 * Payload sent when a user requests an OTP to reset their forgotten password.
 */
public class ForgotPasswordRequest
{
    [Required(ErrorMessage = "Email address is required.")]
    [EmailAddress(ErrorMessage = "Please provide a valid email address.")]
    public string Email { get; set; } = string.Empty;
}
