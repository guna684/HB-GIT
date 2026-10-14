using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using backend.DTOs;
using backend.Models;
using backend.Repositories;
using Microsoft.IdentityModel.Tokens;

namespace backend.Services;

/// <summary>
/// Handles user registration, password verification, and JWT token generation.
/// </summary>
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
    /// Registers a new user. Hashes their password using BCrypt and returns an auth response with a JWT.
    /// Returns null if an account with that email already exists.
    /// </summary>
    public async Task<AuthResponse?> RegisterAsync(RegisterRequest request)
    {
        // Check if a user with this email already exists
        User? existingUser = await _userRepository.GetByEmailAsync(request.Email);
        if (existingUser != null)
        {
            // Email is already taken
            return null;
        }

        // Hash the plain text password using BCrypt
        string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        // Build the new user entity
        User newUser = new()
        {
            Name = request.Name.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            PasswordHash = passwordHash
        };

        // Save the user to the repository
        User createdUser = await _userRepository.CreateAsync(newUser);

        // Generate a JWT token for the new user so they are immediately logged in
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
    /// Validates user credentials and returns an auth response with a JWT token.
    /// Returns null if the email is not found or the password does not match.
    /// </summary>
    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        // Find the user by email
        User? user = await _userRepository.GetByEmailAsync(request.Email.Trim().ToLowerInvariant());
        if (user == null)
        {
            // User not found
            return null;
        }

        // Verify the plain text password against the stored BCrypt hash
        bool isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
        if (!isPasswordValid)
        {
            // Password does not match
            return null;
        }

        // Generate a new JWT token
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
    /// Helper method to create and sign a JSON Web Token (JWT) containing user claims.
    /// Reads secret key, issuer, audience, and expiry settings from appsettings.json.
    /// </summary>
    private string GenerateJwtToken(User user)
    {
        // Read configuration values
        string secretKey = _configuration["Jwt:Key"] ?? "DefaultFallbackSecretKey12345678901234567890";
        string issuer = _configuration["Jwt:Issuer"] ?? "BirthdayReminderApi";
        string audience = _configuration["Jwt:Audience"] ?? "BirthdayReminderApp";
        int expiryDays = 7;
        if (int.TryParse(_configuration["Jwt:ExpiryDays"], out int parsedDays))
        {
            expiryDays = parsedDays;
        }

        // Claims are pieces of data embedded inside the token (User ID, Name, Email)
        Claim[] claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Email, user.Email)
        };

        // Create the symmetric security key using UTF8 bytes
        SymmetricSecurityKey key = new(Encoding.UTF8.GetBytes(secretKey));
        SigningCredentials credentials = new(key, SecurityAlgorithms.HmacSha256);

        // Define token options
        JwtSecurityToken tokenDescriptor = new(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(expiryDays),
            signingCredentials: credentials
        );

        // Convert the token object to a serialized string
        return new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);
    }
}
