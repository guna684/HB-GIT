using backend.Models;

namespace backend.Repositories;

/**
 * =============================================================================
 * REPOSITORY: InMemoryPasswordResetOtpRepository
 * =============================================================================
 * In-memory list implementation of IPasswordResetOtpRepository for local testing.
 */
public class InMemoryPasswordResetOtpRepository : IPasswordResetOtpRepository
{
    private readonly List<PasswordResetOtp> _otps = new();
    private readonly object _lock = new();
    private int _nextId = 1;

    public Task<PasswordResetOtp> CreateAsync(PasswordResetOtp otp)
    {
        lock (_lock)
        {
            otp.Id = _nextId++;
            _otps.Add(otp);
            return Task.FromResult(otp);
        }
    }

    public Task<PasswordResetOtp?> GetLatestActiveByUserIdAsync(int userId)
    {
        lock (_lock)
        {
            PasswordResetOtp? otp = _otps
                .Where(o => o.UserId == userId && !o.IsUsed)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefault();

            return Task.FromResult(otp);
        }
    }

    public Task InvalidateUnusedOtpsForUserAsync(int userId)
    {
        lock (_lock)
        {
            foreach (PasswordResetOtp otp in _otps.Where(o => o.UserId == userId && !o.IsUsed))
            {
                otp.IsUsed = true;
            }
            return Task.CompletedTask;
        }
    }

    public Task UpdateAsync(PasswordResetOtp otp)
    {
        lock (_lock)
        {
            int index = _otps.FindIndex(o => o.Id == otp.Id);
            if (index >= 0)
            {
                _otps[index] = otp;
            }
            return Task.CompletedTask;
        }
    }
}
