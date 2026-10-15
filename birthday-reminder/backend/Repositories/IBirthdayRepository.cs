using backend.Models;

namespace backend.Repositories;

/**
 * =============================================================================
 * INTERFACE: IBirthdayRepository
 * =============================================================================
 * What is an Interface in C#?
 * Think of an interface like a contract or blueprint.
 * It lists the methods that MUST exist, but doesn't write the implementation code.
 * 
 * Why is this awesome for beginners to learn?
 * Because our services only care about `IBirthdayRepository`.
 * - In local dev with no database? -> Use InMemoryBirthdayRepository.
 * - In production with PostgreSQL?  -> Use PostgresBirthdayRepository.
 * The rest of the app doesn't have to change a single line of code!
 */
public interface IBirthdayRepository
{
    /// <summary>
    /// Gets all birthdays belonging to a specific logged-in user.
    /// </summary>
    Task<List<Birthday>> GetAllByUserIdAsync(int userId);

    /// <summary>
    /// Finds a single birthday by its unique ID.
    /// </summary>
    Task<Birthday?> GetByIdAsync(int id);

    /// <summary>
    /// Saves a new birthday and returns the created record with its assigned ID.
    /// </summary>
    Task<Birthday> CreateAsync(Birthday birthday);

    /// <summary>
    /// Updates an existing birthday's details.
    /// </summary>
    Task<Birthday?> UpdateAsync(Birthday birthday);

    /// <summary>
    /// Deletes a birthday by its ID. Returns true if removed, false if not found.
    /// </summary>
    Task<bool> DeleteAsync(int id);

    /// <summary>
    /// Gets all birthdays across all users (used by background reminder worker).
    /// </summary>
    Task<List<Birthday>> GetAllAsync();
}

