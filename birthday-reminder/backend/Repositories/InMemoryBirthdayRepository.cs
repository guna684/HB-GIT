using backend.Models;

namespace backend.Repositories;

/// <summary>
/// Stores birthday records in memory inside a C# List.
/// Provides basic Create, Read, Update, and Delete (CRUD) operations.
/// </summary>
public class InMemoryBirthdayRepository : IBirthdayRepository
{
    // In-memory list to hold all birthday records
    private readonly List<Birthday> _birthdays = new();

    // Lock object to make operations thread-safe
    private readonly object _lock = new();

    // Counter used to generate unique birthday IDs
    private int _nextId = 1;

    /// <summary>
    /// Returns all birthdays belonging to a specific user.
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
    /// Finds a single birthday by its unique ID.
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
    /// Assigns an incremental ID to the new birthday and adds it to the list.
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
    /// Updates fields of an existing birthday record.
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
    /// Removes a birthday from the list by its ID.
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
    /// Returns all birthdays across the entire system.
    /// Used by the background reminder service to inspect upcoming dates.
    /// </summary>
    public Task<List<Birthday>> GetAllAsync()
    {
        lock (_lock)
        {
            // Return a shallow copy of the list
            List<Birthday> copy = new(_birthdays);
            return Task.FromResult(copy);
        }
    }
}
