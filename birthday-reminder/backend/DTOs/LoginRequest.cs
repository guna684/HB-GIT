using System.ComponentModel.DataAnnotations;

namespace backend.DTOs;

/// <summary>
/// Data sent from the client when logging into an existing account.
/// </summary>
public class LoginRequest
{
    // The user's registered email
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email address format")]
    public string Email { get; set; } = string.Empty;

    // The user's password
    [Required(ErrorMessage = "Password is required")]
    public string Password { get; set; } = string.Empty;
}
