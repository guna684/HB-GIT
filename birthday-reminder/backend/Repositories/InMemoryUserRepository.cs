using backend.Models;

namespace backend.Repositories;

/// <summary>
/// Stores user accounts in memory inside a C# List.
/// This allows us to build and test the app without needing a database installed.
/// Later, you can replace this with a PostgreSQL repository using EF Core.
/// </summary>
public class InMemoryUserRepository : IUserRepository
{
    // In-memory list to hold all user records
    private readonly List<User> _users = new();

    // Lock object to make operations safe when multiple requests arrive at the same time
    private readonly object _lock = new();

    // Counter used to generate unique IDs (1, 2, 3...)
    private int _nextId = 1;

    /// <summary>
    /// Searches for a user matching the provided ID.
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
    /// Searches for a user matching the given email address (case-insensitive).
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
    /// Assigns an incremental ID to the new user and adds them to our in-memory list.
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
}
