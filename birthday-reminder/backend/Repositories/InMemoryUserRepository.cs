using backend.Models;

namespace backend.Repositories;

/**
 * =============================================================================
 * REPOSITORY: InMemoryUserRepository
 * =============================================================================
 * Stores user account records in RAM inside a C# List<User>.
 * 
 * Allows instant testing of registration, BCrypt password verification,
 * and login without requiring a PostgreSQL database running locally.
 */
public class InMemoryUserRepository : IUserRepository
{
    // The in-memory list holding registered users
    private readonly List<User> _users = new();

    // Lock object ensuring thread safety
    private readonly object _lock = new();

    // Auto-incrementing counter for unique user IDs (1, 2, 3...)
    private int _nextId = 1;

    /// <summary>
    /// 1. READ BY ID
    /// Finds a user by their unique integer ID.
    /// </summary>
    public Task<User?> GetByIdAsync(int id)
    {
        lock (_lock)
        {
            foreach (User user in _users)
            {
                if (user.Id == id)
                {
                    return Task.FromResult<User?>(user);
                }
            }
            return Task.FromResult<User?>(null);
        }
    }

    /// <summary>
    /// 2. READ BY EMAIL
    /// Finds a user by their email address (case-insensitive).
    /// </summary>
    public Task<User?> GetByEmailAsync(string email)
    {
        lock (_lock)
        {
            foreach (User user in _users)
            {
                if (string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
                {
                    return Task.FromResult<User?>(user);
                }
            }
            return Task.FromResult<User?>(null);
        }
    }

    /// <summary>
    /// 3. CREATE USER
    /// Assigns an incremental user ID and appends them to our list.
    /// </summary>
    public Task<User> CreateAsync(User user)
    {
        lock (_lock)
        {
            user.Id = _nextId;
            _nextId++;
            _users.Add(user);
            return Task.FromResult(user);
        }
    }

    /// <summary>
    /// 4. UPDATE PASSWORD
    /// Updates the password hash in the in-memory user list.
    /// </summary>
    public Task UpdatePasswordAsync(int userId, string newPasswordHash)
    {
        lock (_lock)
        {
            User? user = _users.FirstOrDefault(u => u.Id == userId);
            if (user != null)
            {
                user.PasswordHash = newPasswordHash;
            }
            return Task.CompletedTask;
        }
    }
}

