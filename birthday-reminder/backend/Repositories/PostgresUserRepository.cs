using backend.Data;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Repositories;

/**
 * =============================================================================
 * REPOSITORY: PostgresUserRepository
 * =============================================================================
 * Implements IUserRepository using PostgreSQL and Entity Framework Core.
 * Handles user account lookup by ID, lookup by Email, and inserting new users.
 */
public class PostgresUserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    // AppDbContext is injected automatically by ASP.NET Core Dependency Injection
    public PostgresUserRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// 1. READ BY ID
    /// Finds a user by their unique database ID.
    /// SQL equivalent:
    ///   SELECT * FROM "Users" WHERE "Id" = @id LIMIT 1;
    /// </summary>
    public async Task<User?> GetByIdAsync(int id)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    /// <summary>
    /// 2. READ BY EMAIL
    /// Finds a user by their email address (case-insensitive).
    /// Used during login and registration.
    /// SQL equivalent:
    ///   SELECT * FROM "Users" WHERE LOWER("Email") = LOWER(@email) LIMIT 1;
    /// </summary>
    public async Task<User?> GetByEmailAsync(string email)
    {
        string normalizedEmail = email.Trim().ToLowerInvariant();

        return await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);
    }

    /// <summary>
    /// 3. CREATE USER
    /// Inserts a new user record into the PostgreSQL database.
    /// SQL equivalent:
    ///   INSERT INTO "Users" ("Name", "Email", "PasswordHash") VALUES (@name, @email, @hash);
    /// </summary>
    public async Task<User> CreateAsync(User user)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    /// <summary>
    /// 4. UPDATE PASSWORD
    /// Updates the user's password hash in the database.
    /// SQL equivalent:
    ///   UPDATE "Users" SET "PasswordHash" = @newPasswordHash WHERE "Id" = @userId;
    /// </summary>
    public async Task UpdatePasswordAsync(int userId, string newPasswordHash)
    {
        User? user = await _context.Users.FindAsync(userId);
        if (user != null)
        {
            user.PasswordHash = newPasswordHash;
            await _context.SaveChangesAsync();
        }
    }
}

