using backend.Data;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Repositories;

/// <summary>
/// PostgreSQL implementation of IUserRepository using Entity Framework Core.
/// 
/// How EF Core works here:
/// - Instead of writing raw SQL like "SELECT * FROM Users WHERE Email = ...",
///   we write clean C# LINQ queries.
/// - EF Core automatically converts our C# methods into optimized PostgreSQL SQL queries.
/// </summary>
public class PostgresUserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    // EF Core DbContext is injected automatically by ASP.NET Core Dependency Injection
    public PostgresUserRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Finds a user by their unique database ID.
    /// SQL equivalent: SELECT * FROM "Users" WHERE "Id" = @id LIMIT 1;
    /// </summary>
    public async Task<User?> GetByIdAsync(int id)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    /// <summary>
    /// Finds a user by their email address (case-insensitive).
    /// Used during login and registration to check if an account exists.
    /// SQL equivalent: SELECT * FROM "Users" WHERE LOWER("Email") = LOWER(@email) LIMIT 1;
    /// </summary>
    public async Task<User?> GetByEmailAsync(string email)
    {
        string normalizedEmail = email.Trim().ToLower();

        return await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);
    }

    /// <summary>
    /// Saves a new user to the PostgreSQL database.
    /// 
    /// How it works:
    /// 1. '_context.Users.Add(user)' tells EF Core: "Prepare to insert this user".
    /// 2. 'await _context.SaveChangesAsync()' sends the INSERT command to PostgreSQL.
    /// 3. PostgreSQL assigns a new auto-incremented ID (e.g. 1, 2, 3) which EF Core
    ///    automatically puts back into 'user.Id'.
    /// </summary>
    public async Task<User> CreateAsync(User user)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }
}
