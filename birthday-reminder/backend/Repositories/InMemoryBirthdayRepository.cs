using backend.Models;

namespace backend.Repositories;

/**
 * =============================================================================
 * REPOSITORY: InMemoryBirthdayRepository
 * =============================================================================
 * What is this file for?
 * It stores birthday records in RAM inside a standard C# List<Birthday>.
 * 
 * Why do we have this?
 * When you first clone the project, you don't need to install or configure
 * PostgreSQL! You can run the entire app immediately with zero database setup.
 * 
 * Key Concepts for Beginners:
 * 1. Thread Safety: Multiple users/requests hit the backend concurrently.
 *    We use `lock (_lock)` to prevent data corruption when modifying the list.
 * 2. Task.FromResult: Our interface methods return `Task<T>` (async). Because
 *    in-memory operations are instant, `Task.FromResult` wraps the result into
 *    a completed Task.
 */
public class InMemoryBirthdayRepository : IBirthdayRepository
{
    // The in-memory collection holding all birthday records
    private readonly List<Birthday> _birthdays = new();

    // Lock object ensuring thread safety
    private readonly object _lock = new();

    // Auto-incrementing counter for unique birthday IDs (1, 2, 3...)
    private int _nextId = 1;

    /// <summary>
    /// 1. READ ALL FOR USER
    /// Returns all birthdays belonging to a specific user ID.
    /// </summary>
    public Task<List<Birthday>> GetAllByUserIdAsync(int userId)
    {
        lock (_lock)
        {
            List<Birthday> result = new();
            foreach (Birthday item in _birthdays)
            {
                if (item.UserId == userId)
                {
                    result.Add(item);
                }
            }
            return Task.FromResult(result);
        }
    }

    /// <summary>
    /// 2. READ BY ID
    /// Finds a single birthday by its unique integer ID.
    /// </summary>
    public Task<Birthday?> GetByIdAsync(int id)
    {
        lock (_lock)
        {
            foreach (Birthday item in _birthdays)
            {
                if (item.Id == id)
                {
                    return Task.FromResult<Birthday?>(item);
                }
            }
            return Task.FromResult<Birthday?>(null);
        }
    }

    /// <summary>
    /// 3. CREATE
    /// Assigns a new unique ID and appends the birthday to our list.
    /// </summary>
    public Task<Birthday> CreateAsync(Birthday birthday)
    {
        lock (_lock)
        {
            birthday.Id = _nextId;
            _nextId++;
            _birthdays.Add(birthday);
            return Task.FromResult(birthday);
        }
    }

    /// <summary>
    /// 4. UPDATE
    /// Replaces the existing birthday record matching the ID.
    /// </summary>
    public Task<Birthday?> UpdateAsync(Birthday birthday)
    {
        lock (_lock)
        {
            for (int i = 0; i < _birthdays.Count; i++)
            {
                if (_birthdays[i].Id == birthday.Id)
                {
                    _birthdays[i] = birthday;
                    return Task.FromResult<Birthday?>(_birthdays[i]);
                }
            }
            return Task.FromResult<Birthday?>(null);
        }
    }

    /// <summary>
    /// 5. DELETE
    /// Removes a birthday from the list by ID.
    /// </summary>
    public Task<bool> DeleteAsync(int id)
    {
        lock (_lock)
        {
            for (int i = 0; i < _birthdays.Count; i++)
            {
                if (_birthdays[i].Id == id)
                {
                    _birthdays.RemoveAt(i);
                    return Task.FromResult(true);
                }
            }
            return Task.FromResult(false);
        }
    }

    /// <summary>
    /// 6. READ ALL (SYSTEM WIDE)
    /// Used by the background ReminderBackgroundService to check upcoming alerts.
    /// </summary>
    public Task<List<Birthday>> GetAllAsync()
    {
        lock (_lock)
        {
            List<Birthday> copy = new(_birthdays);
            return Task.FromResult(copy);
        }
    }
}

