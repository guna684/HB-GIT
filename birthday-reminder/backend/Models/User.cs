namespace backend.Models;

/// <summary>
/// Represents a registered user in our application.
/// Each user has an account with a name, email, and securely hashed password.
/// </summary>
public class User
{
    // Unique identifier for the user
    public int Id { get; set; }

    // Full name of the user
    public string Name { get; set; } = string.Empty;

    // Email address used to log in (must be unique)
    public string Email { get; set; } = string.Empty;

    // BCrypt hashed password (we NEVER store plain text passwords)
    public string PasswordHash { get; set; } = string.Empty;
}
