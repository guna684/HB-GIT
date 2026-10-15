namespace backend.Models;

/**
 * =============================================================================
 * ENTITY MODEL: User
 * =============================================================================
 * Represents a registered user in our database.
 * 
 * Notice:
 * - We store 'PasswordHash', NOT the user's real password!
 * - When a user registers with "secret123", BCrypt converts it into:
 *   "$2a$11$N9qo8uLOickgx2ZMRZoMye.I...".
 */
public class User
{
    // Unique Primary Key (auto-incremented integer: 1, 2, 3...)
    public int Id { get; set; }

    // Full name of the user (e.g. "Ravi Kumar")
    public string Name { get; set; } = string.Empty;

    // Email address used for logging in (must be unique)
    public string Email { get; set; } = string.Empty;

    // Secure BCrypt hashed password
    public string PasswordHash { get; set; } = string.Empty;
}

