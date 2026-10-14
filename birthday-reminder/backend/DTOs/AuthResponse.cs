namespace backend.DTOs;

/// <summary>
/// Data returned to the client after successful registration or login.
/// Contains the JWT token and basic user details.
/// </summary>
public class AuthResponse
{
    // The JSON Web Token (JWT) string used to authenticate future requests
    public string Token { get; set; } = string.Empty;

    // The user's ID
    public int UserId { get; set; }

    // The user's full name
    public string Name { get; set; } = string.Empty;

    // The user's email address
    public string Email { get; set; } = string.Empty;
}
