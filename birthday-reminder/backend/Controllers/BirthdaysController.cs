using System.Security.Claims;
using backend.DTOs;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

/// <summary>
/// Controller for managing birthdays.
/// All endpoints require a valid JWT token in the Authorization header: "Bearer {token}".
/// Users can only access and modify their own birthday entries.
/// </summary>
[Authorize]
[ApiController]
[Route("api/birthdays")]
public class BirthdaysController : ControllerBase
{
    private readonly BirthdayService _birthdayService;

    public BirthdaysController(BirthdayService birthdayService)
    {
        _birthdayService = birthdayService;
    }

    /// <summary>
    /// Gets all birthdays belonging to the logged-in user.
    /// GET /api/birthdays
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
    /// Gets upcoming birthdays within the next 30 days, sorted by closest date first.
    /// GET /api/birthdays/upcoming
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
    /// Gets a single birthday by ID.
    /// GET /api/birthdays/{id}
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
    /// Creates a new birthday reminder. Accepts multipart/form-data for optional photo upload.
    /// POST /api/birthdays
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
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Updates an existing birthday by ID. Accepts multipart/form-data for optional new photo upload.
    /// PUT /api/birthdays/{id}
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
    /// Deletes a birthday and its associated photo file.
    /// DELETE /api/birthdays/{id}
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

    /// <summary>
    /// Extracts the logged-in user's integer ID from the JWT token's claims.
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
    /// Generates the base URL of the currently running backend server (e.g., http://localhost:5000).
    /// Used to build full URLs for uploaded profile photos.
    /// </summary>
    private string GetBaseUrl()
    {
        return $"{Request.Scheme}://{Request.Host}";
    }
}
