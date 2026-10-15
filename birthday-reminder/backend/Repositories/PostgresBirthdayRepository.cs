using backend.Data;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Repositories;

/**
 * =============================================================================
 * REPOSITORY: PostgresBirthdayRepository
 * =============================================================================
 * Implements IBirthdayRepository using Entity Framework Core (EF Core).
 * 
 * How EF Core works:
 * - We write pure C# LINQ methods: `.Where()`, `.OrderBy()`, `.FirstOrDefaultAsync()`.
 * - EF Core automatically converts our C# into SQL commands and runs them in PostgreSQL!
 */
public class PostgresBirthdayRepository : IBirthdayRepository
{
    private readonly AppDbContext _context;

    // AppDbContext is provided automatically by ASP.NET Core Dependency Injection
    public PostgresBirthdayRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// 1. READ ALL FOR USER
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
    /// 2. READ BY ID
    /// SQL equivalent:
    ///   SELECT * FROM "Birthdays" WHERE "Id" = @id LIMIT 1;
    /// </summary>
    public async Task<Birthday?> GetByIdAsync(int id)
    {
        return await _context.Birthdays
            .FirstOrDefaultAsync(b => b.Id == id);
    }

    /// <summary>
    /// 3. CREATE
    /// SQL equivalent:
    ///   INSERT INTO "Birthdays" (...) VALUES (...);
    /// </summary>
    public async Task<Birthday> CreateAsync(Birthday birthday)
    {
        _context.Birthdays.Add(birthday);
        await _context.SaveChangesAsync();
        return birthday;
    }

    /// <summary>
    /// 4. UPDATE
    /// SQL equivalent:
    ///   UPDATE "Birthdays" SET "PersonName" = ..., ... WHERE "Id" = @id;
    /// </summary>
    public async Task<Birthday?> UpdateAsync(Birthday birthday)
    {
        var existing = await _context.Birthdays
            .FirstOrDefaultAsync(b => b.Id == birthday.Id);

        if (existing == null)
        {
            return null; // Record not found
        }

        // Copy modified values over to the tracked database entity
        existing.PersonName = birthday.PersonName;
        existing.DateOfBirth = birthday.DateOfBirth;
        existing.Relationship = birthday.Relationship;
        existing.Notes = birthday.Notes;
        existing.PhotoData = birthday.PhotoData;
        existing.PhotoContentType = birthday.PhotoContentType;
        existing.RemindDaysBefore = birthday.RemindDaysBefore;

        // Commit changes to PostgreSQL
        await _context.SaveChangesAsync();
        return existing;
    }

    /// <summary>
    /// 5. DELETE
    /// SQL equivalent:
    ///   DELETE FROM "Birthdays" WHERE "Id" = @id;
    /// </summary>
    public async Task<bool> DeleteAsync(int id)
    {
        var existing = await _context.Birthdays
            .FirstOrDefaultAsync(b => b.Id == id);

        if (existing == null)
        {
            return false;
        }

        _context.Birthdays.Remove(existing);
        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// 6. READ ALL (SYSTEM-WIDE)
    /// Used by ReminderBackgroundService to calculate upcoming alert notifications.
    /// SQL equivalent:
    ///   SELECT * FROM "Birthdays";
    /// </summary>
    public async Task<List<Birthday>> GetAllAsync()
    {
        return await _context.Birthdays.ToListAsync();
    }
}

