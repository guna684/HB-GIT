namespace backend.Models;

/**
 * =============================================================================
 * ENTITY MODEL: PasswordResetOtp
 * =============================================================================
 * Represents a temporary one-time password (OTP) requested by a user to reset
 * their forgotten password.
 * 
 * Security Features:
 * - Stores OtpHash (hashed using BCrypt), NEVER plain-text OTP digits!
 * - ExpiresAt limits code validity to 10 minutes.
 * - FailedAttempts prevents brute-force guessing (max 5 attempts).
 * - IsUsed ensures each OTP can only be used once.
 */
public class PasswordResetOtp
{
    // Unique Primary Key
    public int Id { get; set; }

    // Foreign Key referencing the user who requested the reset
    public int UserId { get; set; }

    // Secure BCrypt hash of the 6-digit OTP
    public string OtpHash { get; set; } = string.Empty;

    // Time when this OTP becomes invalid (10 minutes after generation)
    public DateTime ExpiresAt { get; set; }

    // Counter for invalid guesses (blocked after 5 failed tries)
    public int FailedAttempts { get; set; } = 0;

    // Set to true once the OTP is redeemed or superseded
    public bool IsUsed { get; set; } = false;

    // Timestamp when this reset request was created
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
