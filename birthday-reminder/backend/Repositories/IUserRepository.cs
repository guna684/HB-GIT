using backend.Models;

namespace backend.Repositories;

/// <summary>
/// Defines the data access contract for User operations.
/// Controllers and services depend only on this interface, never on the concrete storage class.
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Finds a user by their unique ID.
    /// </summary>
    Task<User?> GetByIdAsync(int id);

    /// <summary>
    /// Finds a user by their email address.
    /// </summary>
    Task<User?> GetByEmailAsync(string email);

    /// <summary>
    /// Saves a new user to storage and assigns a unique ID.
    /// </summary>
    Task<User> CreateAsync(User user);
}
