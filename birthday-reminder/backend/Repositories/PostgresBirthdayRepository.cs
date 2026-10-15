using backend.Data;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Repositories;

/// <summary>
/// PostgreSQL implementation of IBirthdayRepository using Entity Framework Core.
/// 
/// Handles CRUD operations (Create, Read, Update, Delete) against the PostgreSQL "Birthdays" table.
/// </summary>
public class PostgresBirthdayRepository : IBirthdayRepository
{
    private readonly AppDbContext _context;

    public PostgresBirthdayRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Gets all birthdays belonging to a specific logged-in user.
    /// Results are sorted alphabetically by person name.
    /// 
    /// SQL equivalent:
    ///   SELECT * FROM "Birthdays" WHERE "UserId" = @userId ORDER BY "PersonName";
    /// </summary>
    public async Task<List<Birthday>> GetAllByUserIdAsync(int userId)
    {
        return await _context.Birthdays
            .Where(b => b.UserId == userId)
            .OrderBy(b => b.PersonName)
            .ToListAsync();
    }

    /// <summary>
    /// Finds a single birthday record by its unique database ID.
    /// 
    /// SQL equivalent:
    ///   SELECT * FROM "Birthdays" WHERE "Id" = @id LIMIT 1;
    /// </summary>
    public async Task<Birthday?> GetByIdAsync(int id)
    {
        return await _context.Birthdays
            .FirstOrDefaultAsync(b => b.Id == id);
    }

    /// <summary>
    /// Inserts a new birthday record into the PostgreSQL database.
    /// 
    /// How it works:
    /// 1. '_context.Birthdays.Add(birthday)' queues the new birthday for insertion.
    /// 2. 'SaveChangesAsync()' generates: INSERT INTO "Birthdays" (...) VALUES (...);
    /// 3. PostgreSQL generates the unique Id, and EF Core updates 'birthday.Id'.
    /// </summary>
    public async Task<Birthday> CreateAsync(Birthday birthday)
    {
        _context.Birthdays.Add(birthday);
        await _context.SaveChangesAsync();
        return birthday;
    }

    /// <summary>
    /// Updates the details of an existing birthday in PostgreSQL.
    /// 
    /// How it works:
    /// 1. Finds the existing record in the database.
    /// 2. If found, updates its properties with the new values.
    /// 3. Calling 'SaveChangesAsync()' generates an UPDATE SQL statement:
    ///    UPDATE "Birthdays" SET "PersonName" = ..., "DateOfBirth" = ... WHERE "Id" = @id;
    /// </summary>
    public async Task<Birthday?> UpdateAsync(Birthday birthday)
    {
        var existing = await _context.Birthdays
            .FirstOrDefaultAsync(b => b.Id == birthday.Id);

        if (existing == null)
        {
            return null; // Record not found
        }

        // Copy updated values over to the tracked entity
        existing.PersonName = birthday.PersonName;
        existing.DateOfBirth = birthday.DateOfBirth;
        existing.Relationship = birthday.Relationship;
        existing.Notes = birthday.Notes;
        existing.PhotoFileName = birthday.PhotoFileName;
        existing.RemindDaysBefore = birthday.RemindDaysBefore;

        // Persist modifications to PostgreSQL
        await _context.SaveChangesAsync();
        return existing;
    }

    /// <summary>
    /// Deletes a birthday record by its unique database ID.
    /// 
    /// SQL equivalent:
    ///   DELETE FROM "Birthdays" WHERE "Id" = @id;
    /// </summary>
    public async Task<bool> DeleteAsync(int id)
    {
        var existing = await _context.Birthdays
            .FirstOrDefaultAsync(b => b.Id == id);

        if (existing == null)
        {
            return false; // Record not found
        }

        _context.Birthdays.Remove(existing);
        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Gets all birthdays across all users in the system.
    /// Used exclusively by 'ReminderBackgroundService' to check which birthdays are approaching today/tomorrow.
    /// 
    /// SQL equivalent:
    ///   SELECT * FROM "Birthdays";
    /// </summary>
    public async Task<List<Birthday>> GetAllAsync()
    {
        return await _context.Birthdays.ToListAsync();
    }
}
