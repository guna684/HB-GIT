using System.Security.Claims;
using System.Security.Cryptography;
using backend.DTOs;
using backend.Models;
using backend.Repositories;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

/**
 * =============================================================================
 * CONTROLLER: AuthController
 * =============================================================================
 * Handles user account operations:
 * - Registering a new account
 * - Logging in
 * - Forgot Password (OTP generation and delivery)
 * - Reset Password (OTP verification and password updating)
 * - Change Password (for logged-in users)
 */
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly IEmailService _emailService;
    private readonly IBirthdayRepository _birthdayRepository;
    private readonly IUserRepository _userRepository;
    private readonly IPasswordResetOtpRepository _otpRepository;
    private readonly OtpRateLimiter _rateLimiter;

    // Injected automatically by ASP.NET Core Dependency Injection
    public AuthController(
        AuthService authService,
        IEmailService emailService,
        IBirthdayRepository birthdayRepository,
        IUserRepository userRepository,
        IPasswordResetOtpRepository otpRepository,
        OtpRateLimiter rateLimiter)
    {
        _authService = authService;
        _emailService = emailService;
        _birthdayRepository = birthdayRepository;
        _userRepository = userRepository;
        _otpRepository = otpRepository;
        _rateLimiter = rateLimiter;
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

    /// <summary>
    /// POST /api/auth/forgot-password
    /// Generates a secure 6-digit OTP, saves its BCrypt hash to the database,
    /// and emails the OTP to the user.
    /// Rate-limited to 1 request per 60 seconds and 5 requests per hour.
    /// Always returns the same generic message to prevent email enumeration.
    /// </summary>
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        // 1. Check rate limits: 1 per 60s, 5 per hour
        var rateCheck = _rateLimiter.CheckAndRecord(request.Email);
        if (!rateCheck.Allowed)
        {
            return StatusCode(StatusCodes.Status429TooManyRequests, new { message = rateCheck.ErrorMessage });
        }

        // Generic message returned in ALL cases to protect user privacy
        const string responseMessage = "If this email is registered, an OTP has been sent.";

        // 2. Find user by email
        string normalizedEmail = request.Email.Trim().ToLowerInvariant();
        User? user = await _userRepository.GetByEmailAsync(normalizedEmail);

        if (user == null)
        {
            return Ok(new { message = responseMessage });
        }

        // 3. Invalidate any older unused OTPs for this user
        await _otpRepository.InvalidateUnusedOtpsForUserAsync(user.Id);

        // 4. Generate cryptographically secure random 6-digit number
        int randomCode = RandomNumberGenerator.GetInt32(100000, 1000000);
        string otp = randomCode.ToString("D6");

        // 5. Hash the OTP using BCrypt so raw codes are never stored in the database
        string otpHash = BCrypt.Net.BCrypt.HashPassword(otp);

        PasswordResetOtp otpRecord = new()
        {
            UserId = user.Id,
            OtpHash = otpHash,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            FailedAttempts = 0,
            IsUsed = false,
            CreatedAt = DateTime.UtcNow
        };

        await _otpRepository.CreateAsync(otpRecord);

        // 6. Deliver OTP via email
        await _emailService.SendPasswordResetOtpAsync(user.Email, user.Name, otp);

        return Ok(new { message = responseMessage });
    }

    /// <summary>
    /// POST /api/auth/reset-password
    /// Validates 6-digit OTP, limits wrong attempts to 5, verifies password complexity,
    /// hashes new password, updates user, marks OTP as used, and sends confirmation email.
    /// </summary>
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        // 1. Password complexity check (minimum 8 chars, at least one letter and one number)
        if (!IsPasswordComplex(request.NewPassword))
        {
            return BadRequest(new { message = "Password must be at least 8 characters long and contain both letters and numbers." });
        }

        const string genericError = "Invalid email, verification code, or expired request.";

        // 2. Look up user by email
        string normalizedEmail = request.Email.Trim().ToLowerInvariant();
        User? user = await _userRepository.GetByEmailAsync(normalizedEmail);
        if (user == null)
        {
            return BadRequest(new { message = genericError });
        }

        // 3. Find latest unused OTP record
        PasswordResetOtp? otpRecord = await _otpRepository.GetLatestActiveByUserIdAsync(user.Id);
        if (otpRecord == null)
        {
            return BadRequest(new { message = genericError });
        }

        // 4. Check expiration (10 minutes)
        if (DateTime.UtcNow > otpRecord.ExpiresAt)
        {
            otpRecord.IsUsed = true;
            await _otpRepository.UpdateAsync(otpRecord);
            return BadRequest(new { message = "Verification code has expired. Please request a new code." });
        }

        // 5. Check if already reached maximum wrong attempts (5)
        if (otpRecord.FailedAttempts >= 5)
        {
            otpRecord.IsUsed = true;
            await _otpRepository.UpdateAsync(otpRecord);
            return BadRequest(new { message = "Too many failed attempts. This code is now invalid. Please request a new code." });
        }

        // 6. Verify submitted OTP against BCrypt hash
        bool isMatch = BCrypt.Net.BCrypt.Verify(request.Otp.Trim(), otpRecord.OtpHash);
        if (!isMatch)
        {
            otpRecord.FailedAttempts++;
            if (otpRecord.FailedAttempts >= 5)
            {
                otpRecord.IsUsed = true;
            }
            await _otpRepository.UpdateAsync(otpRecord);

            if (otpRecord.IsUsed)
            {
                return BadRequest(new { message = "Too many failed attempts. This code is now invalid. Please request a new code." });
            }

            int remaining = 5 - otpRecord.FailedAttempts;
            return BadRequest(new { message = $"Invalid verification code. You have {remaining} attempt{(remaining > 1 ? "s" : "")} remaining." });
        }

        // 7. OTP verified! Mark as used immediately so it cannot be reused
        otpRecord.IsUsed = true;
        await _otpRepository.UpdateAsync(otpRecord);

        // 8. Hash new password and save to database
        string newHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await _userRepository.UpdatePasswordAsync(user.Id, newHash);

        // 9. Send security confirmation email
        await _emailService.SendPasswordChangedNotificationAsync(user.Email, user.Name);

        return Ok(new { message = "Your password has been reset successfully. You can now log in with your new password." });
    }

    /// <summary>
    /// POST /api/auth/change-password
    /// Allows an authenticated logged-in user to change their password by verifying their current password.
    /// </summary>
    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        // 1. Password complexity check
        if (!IsPasswordComplex(request.NewPassword))
        {
            return BadRequest(new { message = "New password must be at least 8 characters long and contain both letters and numbers." });
        }

        // 2. Extract logged-in user ID from JWT claims
        string? userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            return Unauthorized(new { message = "User identity not found in token." });
        }

        // 3. Find user
        User? user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            return NotFound(new { message = "User account not found." });
        }

        // 4. Verify current password
        bool isCurrentValid = BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash);
        if (!isCurrentValid)
        {
            return BadRequest(new { message = "Current password is incorrect." });
        }

        // 5. Hash new password and update
        string newHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await _userRepository.UpdatePasswordAsync(user.Id, newHash);

        // 6. Send security confirmation email
        await _emailService.SendPasswordChangedNotificationAsync(user.Email, user.Name);

        return Ok(new { message = "Password updated successfully." });
    }

    /// <summary>
    /// Helper: Checks password meets minimum length (8) and contains at least 1 letter and 1 digit.
    /// </summary>
    private static bool IsPasswordComplex(string password)
    {
        return !string.IsNullOrWhiteSpace(password) &&
               password.Length >= 8 &&
               password.Any(char.IsLetter) &&
               password.Any(char.IsDigit);
    }
}



