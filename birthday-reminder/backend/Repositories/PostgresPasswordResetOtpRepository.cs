using backend.Data;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Repositories;

/**
 * =============================================================================
 * REPOSITORY: PostgresPasswordResetOtpRepository
 * =============================================================================
 * Implements IPasswordResetOtpRepository using PostgreSQL and Entity Framework Core.
 */
public class PostgresPasswordResetOtpRepository : IPasswordResetOtpRepository
{
    private readonly AppDbContext _context;

    public PostgresPasswordResetOtpRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Inserts a new OTP record into the PasswordResetOtp table.
    /// </summary>
    public async Task<PasswordResetOtp> CreateAsync(PasswordResetOtp otp)
    {
        _context.PasswordResetOtps.Add(otp);
        await _context.SaveChangesAsync();
        return otp;
    }

    /// <summary>
    /// Finds the newest unused OTP for the specified user.
    /// </summary>
    public async Task<PasswordResetOtp?> GetLatestActiveByUserIdAsync(int userId)
    {
        return await _context.PasswordResetOtps
            .Where(o => o.UserId == userId && !o.IsUsed)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Marks all older active OTP records for this user as used (so only the newest OTP is valid).
    /// </summary>
    public async Task InvalidateUnusedOtpsForUserAsync(int userId)
    {
        List<PasswordResetOtp> activeOtps = await _context.PasswordResetOtps
            .Where(o => o.UserId == userId && !o.IsUsed)
            .ToListAsync();

        foreach (PasswordResetOtp otp in activeOtps)
        {
            otp.IsUsed = true;
        }

        if (activeOtps.Count > 0)
        {
            await _context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Saves updates made to an OTP record (such as incrementing failed attempts or setting IsUsed = true).
    /// </summary>
    public async Task UpdateAsync(PasswordResetOtp otp)
    {
        _context.PasswordResetOtps.Update(otp);
        await _context.SaveChangesAsync();
    }
}
