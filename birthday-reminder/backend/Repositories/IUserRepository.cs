using backend.Models;

namespace backend.Repositories;

/**
 * =============================================================================
 * INTERFACE: IUserRepository
 * =============================================================================
 * Blueprint for user data storage.
 * Implemented by:
 * 1. InMemoryUserRepository (for fast, zero-config local development)
 * 2. PostgresUserRepository (for production persistence with PostgreSQL & EF Core)
 */
public interface IUserRepository
{
    /// <summary>
    /// Finds a user by their unique integer ID.
    /// </summary>
    Task<User?> GetByIdAsync(int id);

    /// <summary>
    /// Finds a user by their registered email address.
    /// </summary>
    Task<User?> GetByEmailAsync(string email);

    /// <summary>
    /// Saves a new user and returns the created user entity.
    /// </summary>
    Task<User> CreateAsync(User user);
}

