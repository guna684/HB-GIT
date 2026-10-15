using backend.DTOs;
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
 */
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly IEmailService _emailService;

    // Both AuthService and IEmailService are injected automatically by ASP.NET Core
    public AuthController(AuthService authService, IEmailService emailService)
    {
        _authService = authService;
        _emailService = emailService;
    }

    /// <summary>
    /// POST /api/auth/register
    /// Accepts { name, email, password }
    /// Checks if email is taken, hashes password with BCrypt, and returns a JWT token.
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

        // 3. Return 200 OK with the new user's JWT token
        return Ok(result);
    }

    /// <summary>
    /// POST /api/auth/login
    /// Accepts { email, password }
    /// Verifies credentials, returns a JWT token, and sends a login notification email.
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

        // 3. Send email notification to user alerting them of the login!
        await _emailService.SendLoginNotificationAsync(result.Email, result.Name, DateTime.Now);

        // 4. Return 200 OK with the JWT token
        return Ok(result);
    }
}


