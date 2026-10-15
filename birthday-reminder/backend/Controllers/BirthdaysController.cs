using System.Security.Claims;
using backend.DTOs;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

/**
 * =============================================================================
 * CONTROLLER: BirthdaysController
 * =============================================================================
 * Handles all CRUD (Create, Read, Update, Delete) actions for Birthday reminders.
 * 
 * Key Concepts for Beginners:
 * - [Authorize]: Enforces that every endpoint in this controller requires a valid
 *   JWT token in the "Authorization: Bearer <token>" request header.
 *   If someone tries to access this without a token, ASP.NET returns 401 Unauthorized!
 * - User Isolation: Every request extracts the logged-in user's unique ID from the
 *   JWT token. This guarantees that User A can NEVER see or modify User B's birthdays!
 */
[Authorize]
[ApiController]
[Route("api/birthdays")]
public class BirthdaysController : ControllerBase
{
    private readonly BirthdayService _birthdayService;

    // BirthdayService is injected automatically by ASP.NET Core Dependency Injection
    public BirthdaysController(BirthdayService birthdayService)
    {
        _birthdayService = birthdayService;
    }

    /// <summary>
    /// GET /api/birthdays
    /// Retrieves all birthdays belonging to the logged-in user, sorted by upcoming first.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        int userId = GetLoggedInUserId();
        string baseUrl = GetBaseUrl();

        List<BirthdayResponse> birthdays = await _birthdayService.GetAllForUserAsync(userId, baseUrl);
        return Ok(birthdays);
    }

    /// <summary>
    /// GET /api/birthdays/upcoming
    /// Retrieves birthdays happening within the next 30 days (0 to 30 days away).
    /// </summary>
    [HttpGet("upcoming")]
    public async Task<IActionResult> GetUpcoming()
    {
        int userId = GetLoggedInUserId();
        string baseUrl = GetBaseUrl();

        List<BirthdayResponse> upcoming = await _birthdayService.GetUpcomingForUserAsync(userId, baseUrl);
        return Ok(upcoming);
    }

    /// <summary>
    /// GET /api/birthdays/{id}
    /// Retrieves a single birthday by ID. Returns 404 if not found or owned by someone else.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        int userId = GetLoggedInUserId();
        string baseUrl = GetBaseUrl();

        BirthdayResponse? birthday = await _birthdayService.GetByIdForUserAsync(id, userId, baseUrl);
        if (birthday == null)
        {
            return NotFound(new { message = "Birthday not found." });
        }

        return Ok(birthday);
    }

    /// <summary>
    /// POST /api/birthdays
    /// Creates a new birthday reminder.
    /// Uses [FromForm] and [Consumes("multipart/form-data")] to accept an optional photo file upload.
    /// </summary>
    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Create([FromForm] BirthdayRequest request)
    {
        int userId = GetLoggedInUserId();
        string baseUrl = GetBaseUrl();

        try
        {
            BirthdayResponse created = await _birthdayService.CreateForUserAsync(userId, request, baseUrl);
            
            // CreatedAtAction returns HTTP 201 Created with a "Location" header pointing to the new resource
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (ArgumentException ex)
        {
            // If the photo was too big or had an invalid extension, return 400 Bad Request
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// PUT /api/birthdays/{id}
    /// Updates an existing birthday reminder by ID.
    /// </summary>
    [HttpPut("{id}")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Update(int id, [FromForm] BirthdayRequest request)
    {
        int userId = GetLoggedInUserId();
        string baseUrl = GetBaseUrl();

        try
        {
            BirthdayResponse? updated = await _birthdayService.UpdateForUserAsync(id, userId, request, baseUrl);
            if (updated == null)
            {
                return NotFound(new { message = "Birthday not found." });
            }

            return Ok(updated);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// DELETE /api/birthdays/{id}
    /// Deletes a birthday and safely deletes its photo file from disk.
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        int userId = GetLoggedInUserId();

        bool deleted = await _birthdayService.DeleteForUserAsync(id, userId);
        if (!deleted)
        {
            return NotFound(new { message = "Birthday not found." });
        }

        return Ok(new { message = "Birthday deleted successfully." });
    }

    // =========================================================================
    // PRIVATE CONTROLLER HELPERS
    // =========================================================================

    /// <summary>
    /// Helper: GetLoggedInUserId
    /// Reads the integer user ID embedded in the JWT token's claims.
    /// Because the JWT is cryptographically signed by the server, this value
    /// cannot be faked or tampered with by the user.
    /// </summary>
    private int GetLoggedInUserId()
    {
        string? claimValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(claimValue, out int userId))
        {
            return userId;
        }

        throw new UnauthorizedAccessException("User ID is missing or invalid in JWT token.");
    }

    /// <summary>
    /// Helper: GetBaseUrl
    /// Detects the server's current address (e.g. "http://localhost:5000").
    /// We use this to turn "uploads/photo.jpg" into a complete URL:
    /// "http://localhost:5000/uploads/photo.jpg" so the frontend can display it!
    /// </summary>
    private string GetBaseUrl()
    {
        return $"{Request.Scheme}://{Request.Host}";
    }
}

