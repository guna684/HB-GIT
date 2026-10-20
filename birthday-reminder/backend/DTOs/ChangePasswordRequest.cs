using System.ComponentModel.DataAnnotations;

namespace backend.DTOs;

/**
 * =============================================================================
 * DTO: ChangePasswordRequest
 * =============================================================================
 * Payload sent by a logged-in user to update their current password.
 */
public class ChangePasswordRequest
{
    [Required(ErrorMessage = "Current password is required.")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "New password is required.")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters long.")]
    public string NewPassword { get; set; } = string.Empty;
}
