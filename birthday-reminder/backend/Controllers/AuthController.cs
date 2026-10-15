using backend.DTOs;
using backend.Models;
using backend.Repositories;
using backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

/**
 * =============================================================================
 * CONTROLLER: AuthController
 * =============================================================================
 * Handles user account operations: Registering a new account and Logging in.
 * 
 * Key Concepts for Beginners:
 * - [ApiController]: Automatically checks if the incoming request model is valid.
 *   If required fields are missing, it returns 400 Bad Request automatically.
 * - [Route("api/auth")]: The base URL path. Endpoints inside this controller
 *   will be accessed at: http://localhost:5000/api/auth/...
 * - ControllerBase: Provides helper methods like Ok(data), BadRequest(error),
 *   Unauthorized(error), NotFound(error).
 * - IEmailService: Injected to send automatic email alerts whenever a user logs in!
 * - IBirthdayRepository: Injected so we can check the user's upcoming birthdays
 *   and include a helpful reminder list right inside their login notification email!
 */
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly IEmailService _emailService;
    private readonly IBirthdayRepository _birthdayRepository;

    // Injected automatically by ASP.NET Core Dependency Injection
    public AuthController(
        AuthService authService,
        IEmailService emailService,
        IBirthdayRepository birthdayRepository)
    {
        _authService = authService;
        _emailService = emailService;
        _birthdayRepository = birthdayRepository;
    }

    /// <summary>
    /// POST /api/auth/register
    /// Accepts { name, email, password }
    /// Checks if email is taken, hashes password with BCrypt, returns a JWT token,
    /// and sends a warm welcome email.
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        // 1. Ask AuthService to register the user
        AuthResponse? result = await _authService.RegisterAsync(request);

        // 2. If null, the email is already in use by another user
        if (result == null)
        {
            return BadRequest(new { message = "An account with this email already exists." });
        }

        // 3. Send friendly welcome email to the newly registered user
        await _emailService.SendWelcomeEmailAsync(result.Email, result.Name);

        // 4. Return 200 OK with the new user's JWT token
        return Ok(result);
    }

    /// <summary>
    /// POST /api/auth/login
    /// Accepts { email, password }
    /// Verifies credentials, retrieves the user's upcoming birthdays, sends a notification
    /// email directly to this user's email address, and returns their JWT token.
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        // 1. Ask AuthService to verify credentials
        AuthResponse? result = await _authService.LoginAsync(request);

        // 2. If null, either email doesn't exist or password didn't match
        if (result == null)
        {
            return Unauthorized(new { message = "Invalid email or password." });
        }

        // 3. Find this user's upcoming birthdays to include inside their email reminder!
        List<Birthday> userBirthdays = await _birthdayRepository.GetAllByUserIdAsync(result.UserId);
        DateTime today = DateTime.Today;

        List<UpcomingBirthdayReminder> upcomingList = new();
        foreach (Birthday b in userBirthdays)
        {
            (int daysUntil, int turningAge) = BirthdayService.CalculateBirthdayStats(b.DateOfBirth, today);

            // Filter for birthdays happening in the next 30 days (or today)
            if (daysUntil >= 0 && daysUntil <= 30)
            {
                upcomingList.Add(new UpcomingBirthdayReminder
                {
                    PersonName = b.PersonName,
                    Relationship = b.Relationship,
                    DateOfBirth = b.DateOfBirth,
                    DaysUntil = daysUntil,
                    TurningAge = turningAge,
                    Notes = b.Notes
                });
            }
        }

        // Sort ascending: closest birthday (0 = TODAY) comes first!
        upcomingList.Sort((a, b) => a.DaysUntil.CompareTo(b.DaysUntil));

        // 4. Send email notification directly to THIS user's email address with upcoming birthdays!
        await _emailService.SendLoginNotificationAsync(result.Email, result.Name, DateTime.Now, upcomingList);

        // 5. Return 200 OK with the JWT token
        return Ok(result);
    }
}



