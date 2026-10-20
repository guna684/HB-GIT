using backend.Models;

namespace backend.Repositories;

/**
 * =============================================================================
 * INTERFACE: IPasswordResetOtpRepository
 * =============================================================================
 * Blueprint for managing Password Reset OTP records.
 * Implemented by:
 * 1. PostgresPasswordResetOtpRepository (PostgreSQL + EF Core)
 * 2. InMemoryPasswordResetOtpRepository (In-Memory List for local testing)
 */
public interface IPasswordResetOtpRepository
{
    /// <summary>
    /// Saves a newly generated OTP record.
    /// </summary>
    Task<PasswordResetOtp> CreateAsync(PasswordResetOtp otp);

    /// <summary>
    /// Gets the most recent unused OTP record for a given user.
    /// </summary>
    Task<PasswordResetOtp?> GetLatestActiveByUserIdAsync(int userId);

    /// <summary>
    /// Marks all older unused OTP records for a user as used/invalidated.
    /// </summary>
    Task InvalidateUnusedOtpsForUserAsync(int userId);

    /// <summary>
    /// Updates an existing OTP record (e.g. incrementing failed attempts or marking as used).
    /// </summary>
    Task UpdateAsync(PasswordResetOtp otp);
}
