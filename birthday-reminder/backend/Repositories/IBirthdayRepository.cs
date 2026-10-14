using backend.Models;

namespace backend.Repositories;

/// <summary>
/// Defines the data access contract for Birthday operations.
/// Keeps database / storage details completely separated from business logic.
/// </summary>
public interface IBirthdayRepository
{
    /// <summary>
    /// Gets all birthdays belonging to a specific user.
    /// </summary>
    Task<List<Birthday>> GetAllByUserIdAsync(int userId);

    /// <summary>
    /// Finds a single birthday by its unique ID.
    /// </summary>
    Task<Birthday?> GetByIdAsync(int id);

    /// <summary>
    /// Saves a new birthday and assigns a unique ID.
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
    /// Gets all birthdays across all users (used by background reminder service).
    /// </summary>
    Task<List<Birthday>> GetAllAsync();
}
