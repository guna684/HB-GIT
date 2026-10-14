using backend.DTOs;
using backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

/// <summary>
/// Controller for authentication operations: user registration and login.
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Registers a new user account and returns a JWT token.
    /// POST /api/auth/register
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        // AuthService checks if the email is taken, hashes the password, and creates the user
        AuthResponse? result = await _authService.RegisterAsync(request);

        if (result == null)
        {
            return BadRequest(new { message = "An account with this email already exists." });
        }

        return Ok(result);
    }

    /// <summary>
    /// Logs in an existing user by verifying credentials and returns a JWT token.
    /// POST /api/auth/login
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        // AuthService verifies the email and password hash
        AuthResponse? result = await _authService.LoginAsync(request);

        if (result == null)
        {
            return Unauthorized(new { message = "Invalid email or password." });
        }

        return Ok(result);
    }
}
