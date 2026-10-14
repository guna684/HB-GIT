using System.ComponentModel.DataAnnotations;

namespace backend.DTOs;

/// <summary>
/// Data sent from the client when creating a new user account.
/// </summary>
public class RegisterRequest
{
    // User's full name is required
    [Required(ErrorMessage = "Name is required")]
    [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
    public string Name { get; set; } = string.Empty;

    // A valid email is required to sign in
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email address format")]
    public string Email { get; set; } = string.Empty;

    // Minimum password length of 6 characters for security
    [Required(ErrorMessage = "Password is required")]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters long")]
    public string Password { get; set; } = string.Empty;
}
