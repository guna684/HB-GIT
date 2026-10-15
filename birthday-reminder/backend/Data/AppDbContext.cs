using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Data;

/// <summary>
/// AppDbContext is the bridge between our C# application and the PostgreSQL database.
/// 
/// Think of AppDbContext like a manager for your database:
/// 1. Each 'DbSet<T>' represents a table in the database (e.g. Users table, Birthdays table).
/// 2. When you query a DbSet (like context.Users.Where(...)), EF Core translates your C#
///    code into SQL queries (SELECT * FROM "Users" WHERE ...) and runs it on PostgreSQL.
/// 3. When you add, edit, or delete items and call 'SaveChangesAsync()', EF Core generates
///    INSERT, UPDATE, or DELETE SQL statements automatically.
/// </summary>
public class AppDbContext : DbContext
{
    // The constructor receives configuration options (such as the connection string)
    // from ASP.NET Core's Dependency Injection system in Program.cs.
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // =========================================================================
    // Database Tables (DbSets)
    // =========================================================================
    
    // Maps to the "Users" table in PostgreSQL
    public DbSet<User> Users { get; set; } = null!;

    // Maps to the "Birthdays" table in PostgreSQL
    public DbSet<Birthday> Birthdays { get; set; } = null!;

    // =========================================================================
    // Table Rules & Relationships (OnModelCreating)
    // =========================================================================
    // This method lets us define rules for our tables:
    // - Which column is the Primary Key
    // - Which fields are required (NOT NULL) and their maximum text length
    // - Unique constraints (e.g. no two users can register with the same email)
    // - Foreign key relationships (e.g. every birthday belongs to a specific user)
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ---------------------------------------------------------------------
        // 1. Rules for "Users" Table
        // ---------------------------------------------------------------------
        modelBuilder.Entity<User>(entity =>
        {
            // Table name in PostgreSQL
            entity.ToTable("Users");

            // Primary Key: Unique ID for each user (auto-increments 1, 2, 3...)
            entity.HasKey(u => u.Id);

            // Name: Required text, up to 100 characters
            entity.Property(u => u.Name)
                  .IsRequired()
                  .HasMaxLength(100);

            // Email: Required text, up to 255 characters
            entity.Property(u => u.Email)
                  .IsRequired()
                  .HasMaxLength(255);

            // PasswordHash: Stored as secure BCrypt string
            entity.Property(u => u.PasswordHash)
                  .IsRequired();

            // UNIQUE constraint: Prevents duplicate registrations with the same email
            entity.HasIndex(u => u.Email)
                  .IsUnique();
        });

        // ---------------------------------------------------------------------
        // 2. Rules for "Birthdays" Table
        // ---------------------------------------------------------------------
        modelBuilder.Entity<Birthday>(entity =>
        {
            // Table name in PostgreSQL
            entity.ToTable("Birthdays");

            // Primary Key: Unique ID for each birthday record
            entity.HasKey(b => b.Id);

            // Person's Name: Required text, up to 150 characters
            entity.Property(b => b.PersonName)
                  .IsRequired()
                  .HasMaxLength(150);

            // Relationship: "Friend", "Family", "Work", or "Other"
            entity.Property(b => b.Relationship)
                  .IsRequired()
                  .HasMaxLength(50);

            // Notes: Optional notes (gift ideas, hobbies), up to 1000 characters
            entity.Property(b => b.Notes)
                  .HasMaxLength(1000);

            // PhotoFileName: Name of image saved in wwwroot/uploads (e.g. "guid.jpg")
            entity.Property(b => b.PhotoFileName)
                  .HasMaxLength(300);

            // RemindDaysBefore: Default reminder is 1 day before the birthday
            entity.Property(b => b.RemindDaysBefore)
                  .HasDefaultValue(1);

            // Store DateOfBirth without timezone complexity (local calendar date)
            entity.Property(b => b.DateOfBirth)
                  .HasColumnType("timestamp without time zone");

            // Foreign Key Relationship:
            // - Each Birthday belongs to ONE User (via UserId).
            // - DeleteBehavior.Cascade: If a user deletes their account,
            //   all birthdays created by that user are automatically cleaned up!
            entity.HasOne<User>()
                  .WithMany()
                  .HasForeignKey(b => b.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Index on UserId so queries like "get all birthdays for user #5" run super fast
            entity.HasIndex(b => b.UserId);
        });
    }
}
