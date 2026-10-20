using System.Collections.Concurrent;

namespace backend.Services;

/**
 * =============================================================================
 * SERVICE: OtpRateLimiter
 * =============================================================================
 * Enforces rate limiting on forgot-password requests:
 * 1. Maximum 1 request per 60 seconds per email address.
 * 2. Maximum 5 requests per 60 minutes (1 hour) per email address.
 * 
 * Thread-safe and in-memory, ensuring no database table clutter.
 */
public class OtpRateLimiter
{
    private readonly ConcurrentDictionary<string, List<DateTime>> _requestHistory = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _lock = new();

    /// <summary>
    /// Checks whether an OTP request is permitted for the given email address.
    /// If permitted, records the timestamp and returns Allowed = true.
    /// </summary>
    public (bool Allowed, string? ErrorMessage) CheckAndRecord(string email)
    {
        string normalizedEmail = email.Trim().ToLowerInvariant();
        DateTime now = DateTime.UtcNow;

        lock (_lock)
        {
            if (!_requestHistory.TryGetValue(normalizedEmail, out List<DateTime>? timestamps))
            {
                timestamps = new List<DateTime>();
                _requestHistory[normalizedEmail] = timestamps;
            }

            // Remove timestamps older than 1 hour to prevent memory growth
            timestamps.RemoveAll(t => (now - t).TotalMinutes > 60);

            // Rule 1: Limit to 1 request per 60 seconds
            if (timestamps.Count > 0)
            {
                DateTime lastRequest = timestamps[^1];
                double secondsSinceLast = (now - lastRequest).TotalSeconds;
                if (secondsSinceLast < 60)
                {
                    int remainingSeconds = (int)Math.Ceiling(60 - secondsSinceLast);
                    return (false, $"Please wait {remainingSeconds} second{(remainingSeconds > 1 ? "s" : "")} before requesting another code.");
                }
            }

            // Rule 2: Limit to 5 requests per hour
            if (timestamps.Count >= 5)
            {
                return (false, "Too many password reset requests. Please try again in an hour.");
            }

            // Request is allowed, record timestamp
            timestamps.Add(now);
            return (true, null);
        }
    }
}
