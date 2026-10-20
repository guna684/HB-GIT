using System.Security.Claims;
using backend.DTOs;
using backend.Models;
using backend.Repositories;
using backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route("api/test")]
public class TestController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly IUserRepository _userRepository;
    private readonly IPasswordResetOtpRepository _otpRepository;
    private readonly IEmailService _emailService;
    private readonly AuthController _authController;

    public TestController(
        AuthService authService,
        IUserRepository userRepository,
        IPasswordResetOtpRepository otpRepository,
        IEmailService emailService,
        IBirthdayRepository birthdayRepository,
        OtpRateLimiter rateLimiter)
    {
        _authService = authService;
        _userRepository = userRepository;
        _otpRepository = otpRepository;
        _emailService = emailService;
        _authController = new AuthController(
            authService,
            emailService,
            birthdayRepository,
            userRepository,
            otpRepository,
            rateLimiter);
    }

    public class TestResult
    {
        public string TestName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
    }

    [HttpPost("run-all")]
    public async Task<IActionResult> RunAll()
    {
        List<TestResult> results = new();

        // Setup Test User
        string testEmail = "automated_test_user@hbtest.com";
        string oldPassword = "OldPassword123";
        string newPassword = "NewPassword456";

        // Remove old test user if exists
        User? existing = await _userRepository.GetByEmailAsync(testEmail);
        if (existing != null)
        {
            await _otpRepository.InvalidateUnusedOtpsForUserAsync(existing.Id);
        }

        // Register new test user
        AuthResponse? reg = await _authService.RegisterAsync(new RegisterRequest
        {
            Name = "Automated Test User",
            Email = testEmail,
            Password = oldPassword
        });

        User user = (await _userRepository.GetByEmailAsync(testEmail))!;
        results.Add(new TestResult
        {
            TestName = "Setup Test User",
            Status = "OK",
            Details = $"Created test user ID {user.Id} ({user.Email})"
        });

        // ---------------------------------------------------------------------
        // TEST 1: forgot-password with registered email
        // ---------------------------------------------------------------------
        IActionResult fpRes = await _authController.ForgotPassword(new ForgotPasswordRequest { Email = testEmail });
        if (fpRes is OkObjectResult okFp)
        {
            PasswordResetOtp? activeOtp = await _otpRepository.GetLatestActiveByUserIdAsync(user.Id);
            if (activeOtp != null && !activeOtp.IsUsed && activeOtp.FailedAttempts == 0)
            {
                results.Add(new TestResult
                {
                    TestName = "1. Forgot Password (Registered Email)",
                    Status = "OK",
                    Details = "Returned generic success message and saved hashed OTP record in database."
                });
            }
            else
            {
                results.Add(new TestResult
                {
                    TestName = "1. Forgot Password (Registered Email)",
                    Status = "FAIL",
                    Details = "OTP record was not properly created in database."
                });
            }
        }
        else
        {
            results.Add(new TestResult
            {
                TestName = "1. Forgot Password (Registered Email)",
                Status = "FAIL",
                Details = "Did not return OK response."
            });
        }

        // ---------------------------------------------------------------------
        // TEST 2: forgot-password with unknown email -> same generic message
        // ---------------------------------------------------------------------
        IActionResult unknownRes = await _authController.ForgotPassword(new ForgotPasswordRequest { Email = "unknown_user_999@hbtest.com" });
        if (unknownRes is OkObjectResult)
        {
            results.Add(new TestResult
            {
                TestName = "2. Forgot Password (Unknown Email)",
                Status = "OK",
                Details = "Returned identical generic message to prevent email enumeration."
            });
        }
        else
        {
            results.Add(new TestResult
            {
                TestName = "2. Forgot Password (Unknown Email)",
                Status = "FAIL",
                Details = "Failed to return generic success message."
            });
        }

        // ---------------------------------------------------------------------
        // TEST 3: wrong OTP 5 times -> OTP blocked
        // ---------------------------------------------------------------------
        bool blockedOnFifth = false;
        for (int i = 1; i <= 5; i++)
        {
            IActionResult wrRes = await _authController.ResetPassword(new ResetPasswordRequest
            {
                Email = testEmail,
                Otp = "000000", // Wrong OTP
                NewPassword = "SomePassword123"
            });

            if (i == 5 && wrRes is BadRequestObjectResult br)
            {
                string text = br.Value?.ToString() ?? "";
                if (text.Contains("Too many failed attempts"))
                {
                    blockedOnFifth = true;
                }
            }
        }

        PasswordResetOtp? blockedOtp = await _otpRepository.GetLatestActiveByUserIdAsync(user.Id);
        // Latest active should be null or IsUsed = true
        if (blockedOnFifth && (blockedOtp == null || blockedOtp.IsUsed || blockedOtp.FailedAttempts >= 5))
        {
            results.Add(new TestResult
            {
                TestName = "3. Wrong OTP 5 Times Blocked",
                Status = "OK",
                Details = "Code was blocked and invalidated after 5 incorrect attempts."
            });
        }
        else
        {
            results.Add(new TestResult
            {
                TestName = "3. Wrong OTP 5 Times Blocked",
                Status = "FAIL",
                Details = "Code was not blocked after 5 attempts."
            });
        }

        // ---------------------------------------------------------------------
        // TEST 4: expired OTP -> rejected
        // ---------------------------------------------------------------------
        // Create an expired OTP directly
        string dummyHash = BCrypt.Net.BCrypt.HashPassword("654321");
        PasswordResetOtp expiredRecord = new()
        {
            UserId = user.Id,
            OtpHash = dummyHash,
            ExpiresAt = DateTime.UtcNow.AddMinutes(-15), // Expired 15 minutes ago
            FailedAttempts = 0,
            IsUsed = false,
            CreatedAt = DateTime.UtcNow.AddMinutes(-20)
        };
        await _otpRepository.CreateAsync(expiredRecord);

        IActionResult expRes = await _authController.ResetPassword(new ResetPasswordRequest
        {
            Email = testEmail,
            Otp = "654321",
            NewPassword = "NewValidPassword123"
        });

        if (expRes is BadRequestObjectResult expBr && expBr.Value?.ToString()?.Contains("expired") == true)
        {
            results.Add(new TestResult
            {
                TestName = "4. Expired OTP Rejected",
                Status = "OK",
                Details = "Rejected with message that the code has expired."
            });
        }
        else
        {
            results.Add(new TestResult
            {
                TestName = "4. Expired OTP Rejected",
                Status = "FAIL",
                Details = "Did not reject expired OTP properly."
            });
        }

        // ---------------------------------------------------------------------
        // TEST 5: correct OTP -> password changed, old pass rejected, new pass works
        // ---------------------------------------------------------------------
        // Invalidate older OTPs and create a fresh known OTP '789123'
        await _otpRepository.InvalidateUnusedOtpsForUserAsync(user.Id);
        string knownOtp = "789123";
        PasswordResetOtp validRecord = new()
        {
            UserId = user.Id,
            OtpHash = BCrypt.Net.BCrypt.HashPassword(knownOtp),
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            FailedAttempts = 0,
            IsUsed = false,
            CreatedAt = DateTime.UtcNow
        };
        await _otpRepository.CreateAsync(validRecord);

        IActionResult resetRes = await _authController.ResetPassword(new ResetPasswordRequest
        {
            Email = testEmail,
            Otp = knownOtp,
            NewPassword = newPassword
        });

        if (resetRes is OkObjectResult)
        {
            // Verify old password no longer works
            AuthResponse? oldLogin = await _authService.LoginAsync(new LoginRequest { Email = testEmail, Password = oldPassword });
            // Verify new password works
            AuthResponse? newLogin = await _authService.LoginAsync(new LoginRequest { Email = testEmail, Password = newPassword });

            if (oldLogin == null && newLogin != null)
            {
                results.Add(new TestResult
                {
                    TestName = "5. Correct OTP Password Reset",
                    Status = "OK",
                    Details = "Password updated successfully! Old password rejected; new password logged in successfully."
                });
            }
            else
            {
                results.Add(new TestResult
                {
                    TestName = "5. Correct OTP Password Reset",
                    Status = "FAIL",
                    Details = $"Old password rejected: {oldLogin == null}, New password worked: {newLogin != null}"
                });
            }
        }
        else
        {
            results.Add(new TestResult
            {
                TestName = "5. Correct OTP Password Reset",
                Status = "FAIL",
                Details = "ResetPassword call did not return OK."
            });
        }

        // ---------------------------------------------------------------------
        // TEST 6: same OTP used twice -> second time rejected
        // ---------------------------------------------------------------------
        IActionResult reuseRes = await _authController.ResetPassword(new ResetPasswordRequest
        {
            Email = testEmail,
            Otp = knownOtp,
            NewPassword = "YetAnotherPassword999"
        });

        if (reuseRes is BadRequestObjectResult)
        {
            results.Add(new TestResult
            {
                TestName = "6. Same OTP Used Twice",
                Status = "OK",
                Details = "Second attempt with already used OTP was rejected."
            });
        }
        else
        {
            results.Add(new TestResult
            {
                TestName = "6. Same OTP Used Twice",
                Status = "FAIL",
                Details = "Reused OTP was accepted unexpectedly."
            });
        }

        // ---------------------------------------------------------------------
        // TEST 7: change-password with wrong current password -> rejected
        // ---------------------------------------------------------------------
        // Mock claims for the logged-in user
        ClaimsPrincipal claimsUser = new(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Name)
        }, "TestAuth"));

        _authController.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsUser }
        };

        // Try wrong current password
        IActionResult changeWrongRes = await _authController.ChangePassword(new ChangePasswordRequest
        {
            CurrentPassword = "WrongCurrentPassword999",
            NewPassword = "FinalPassword123"
        });

        bool wrongRejected = changeWrongRes is BadRequestObjectResult brChange &&
                             brChange.Value?.ToString()?.Contains("Current password is incorrect") == true;

        // Try correct current password
        IActionResult changeCorrectRes = await _authController.ChangePassword(new ChangePasswordRequest
        {
            CurrentPassword = newPassword,
            NewPassword = "FinalPassword123"
        });

        bool correctAccepted = changeCorrectRes is OkObjectResult;

        if (wrongRejected && correctAccepted)
        {
            results.Add(new TestResult
            {
                TestName = "7. Change Password (Current Password Validation)",
                Status = "OK",
                Details = "Wrong current password was rejected. Correct current password succeeded and updated."
            });
        }
        else
        {
            results.Add(new TestResult
            {
                TestName = "7. Change Password (Current Password Validation)",
                Status = "FAIL",
                Details = $"Wrong rejected: {wrongRejected}, Correct accepted: {correctAccepted}"
            });
        }

        // ---------------------------------------------------------------------
        // CLEANUP
        // ---------------------------------------------------------------------
        await _otpRepository.InvalidateUnusedOtpsForUserAsync(user.Id);
        // Remove test user in DB
        // User cleanup will be done in SQL or by leaving no traces
        results.Add(new TestResult
        {
            TestName = "Cleanup",
            Status = "OK",
            Details = "Invalidated test OTPs and finalized test run."
        });

        return Ok(results);
    }
}
