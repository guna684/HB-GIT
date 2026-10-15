using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using backend.DTOs;
using backend.Models;
using backend.Repositories;
using Microsoft.IdentityModel.Tokens;

namespace backend.Services;

/**
 * =============================================================================
 * SERVICE: AuthService
 * =============================================================================
 * Handles user authentication logic:
 * 1. User Registration: Validates uniqueness, hashes password, generates JWT.
 * 2. User Login: Verifies BCrypt password hash, generates JWT.
 * 3. JWT Token Generation: Creates signed tokens with user claims.
 * 
 * Why do we use BCrypt?
 * We NEVER store plain text passwords in the database! If an attacker breaches the
 * database, they cannot reverse a BCrypt hash to read real passwords.
 */
public class AuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IConfiguration _configuration;

    public AuthService(IUserRepository userRepository, IConfiguration configuration)
    {
        _userRepository = userRepository;
        _configuration = configuration;
    }

    /// <summary>
    /// Registers a new user.
    /// Returns null if an account with that email already exists.
    /// </summary>
    public async Task<AuthResponse?> RegisterAsync(RegisterRequest request)
    {
        // Step 1: Check if this email is already registered
        string normalizedEmail = request.Email.Trim().ToLowerInvariant();
        User? existingUser = await _userRepository.GetByEmailAsync(normalizedEmail);
        if (existingUser != null)
        {
            return null; // Email taken
        }

        // Step 2: Securely hash the password using BCrypt
        string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        // Step 3: Build the new User database model
        User newUser = new()
        {
            Name = request.Name.Trim(),
            Email = normalizedEmail,
            PasswordHash = passwordHash
        };

        // Step 4: Save the user to database / repository
        User createdUser = await _userRepository.CreateAsync(newUser);

        // Step 5: Generate a signed JWT token so they are logged in right away
        string token = GenerateJwtToken(createdUser);

        return new AuthResponse
        {
            Token = token,
            UserId = createdUser.Id,
            Name = createdUser.Name,
            Email = createdUser.Email
        };
    }

    /// <summary>
    /// Validates user login credentials.
    /// Returns null if the email is not found or password doesn't match.
    /// </summary>
    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        // Step 1: Look up user by email
        string normalizedEmail = request.Email.Trim().ToLowerInvariant();
        User? user = await _userRepository.GetByEmailAsync(normalizedEmail);
        if (user == null)
        {
            return null; // Email not registered
        }

        // Step 2: Compare plain text password against the stored BCrypt hash
        bool isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
        if (!isPasswordValid)
        {
            return null; // Wrong password
        }

        // Step 3: Password matched! Generate a new JWT token
        string token = GenerateJwtToken(user);

        return new AuthResponse
        {
            Token = token,
            UserId = user.Id,
            Name = user.Name,
            Email = user.Email
        };
    }

    /// <summary>
    /// Generates a cryptographically signed JSON Web Token (JWT).
    /// </summary>
    private string GenerateJwtToken(User user)
    {
        // 1. Read secret settings from appsettings.json
        string secretKey = _configuration["Jwt:Key"] ?? "DefaultFallbackSecretKey12345678901234567890";
        string issuer = _configuration["Jwt:Issuer"] ?? "BirthdayReminderApi";
        string audience = _configuration["Jwt:Audience"] ?? "BirthdayReminderApp";
        int expiryDays = 7;
        if (int.TryParse(_configuration["Jwt:ExpiryDays"], out int parsedDays))
        {
            expiryDays = parsedDays;
        }

        // 2. Claims: Data embedded inside the token that travels to the frontend
        Claim[] claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), // Logged-in user's ID
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Email, user.Email)
        };

        // 3. Cryptographic signature with HMAC-SHA256
        SymmetricSecurityKey key = new(Encoding.UTF8.GetBytes(secretKey));
        SigningCredentials credentials = new(key, SecurityAlgorithms.HmacSha256);

        // 4. Build the token descriptor
        JwtSecurityToken tokenDescriptor = new(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(expiryDays),
            signingCredentials: credentials
        );

        // 5. Serialize into string (e.g. "eyJhbGciOiJIUzI1NiIsIn...")
        return new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);
    }
}

