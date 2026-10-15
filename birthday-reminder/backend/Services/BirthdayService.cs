using backend.DTOs;
using backend.Models;
using backend.Repositories;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace backend.Services;

/**
 * =============================================================================
 * SERVICE: BirthdayService
 * =============================================================================
 * The core "Brain" (Business Logic Layer) of the Birthday Reminder app.
 * 
 * Responsibilities:
 * 1. User Isolation: Ensures users only view, edit, and delete their OWN birthdays.
 * 2. Photo Storage: Validates extension/size, saves images to wwwroot/uploads with unique GUIDs.
 * 3. Math & Date Calculations: Calculates exact countdown days and turning age,
 *    correctly handling leap years (people born on Feb 29!).
 * 4. Data Mapping: Translates database entities (Birthday) into responses (BirthdayResponse).
 */
public class BirthdayService
{
    private readonly IBirthdayRepository _birthdayRepository;

    // Maximum file upload size: 500 Kilobytes (500 * 1024 bytes)
    // Kept under 500 KB to keep PostgreSQL database backups and queries fast!
    private const long MaxFileSizeBytes = 500 * 1024;

    public BirthdayService(IBirthdayRepository birthdayRepository)
    {
        _birthdayRepository = birthdayRepository;
    }

    /// <summary>
    /// 1. GET ALL BIRTHDAYS FOR USER
    /// Fetches all birthdays belonging to the logged-in user and sorts them so
    /// the closest upcoming birthday appears first.
    /// </summary>
    public async Task<List<BirthdayResponse>> GetAllForUserAsync(int userId, string baseUrl)
    {
        // 1. Fetch raw records from repository
        List<Birthday> list = await _birthdayRepository.GetAllByUserIdAsync(userId);
        List<BirthdayResponse> responses = new();

        // 2. Map each database model into a response DTO with calculated stats
        foreach (Birthday item in list)
        {
            responses.Add(MapToResponse(item, baseUrl));
        }

        // 3. Sort ascending: closest upcoming birthday (0 days) comes first!
        responses.Sort((a, b) => a.DaysUntilBirthday.CompareTo(b.DaysUntilBirthday));

        return responses;
    }

    /// <summary>
    /// 2. GET UPCOMING BIRTHDAYS (NEXT 30 DAYS)
    /// Filters birthdays that occur between today (0 days) and 30 days from now.
    /// </summary>
    public async Task<List<BirthdayResponse>> GetUpcomingForUserAsync(int userId, string baseUrl)
    {
        List<BirthdayResponse> all = await GetAllForUserAsync(userId, baseUrl);
        List<BirthdayResponse> upcoming = new();

        foreach (BirthdayResponse item in all)
        {
            if (item.DaysUntilBirthday >= 0 && item.DaysUntilBirthday <= 30)
            {
                upcoming.Add(item);
            }
        }

        return upcoming;
    }

    /// <summary>
    /// 3. GET SINGLE BIRTHDAY BY ID
    /// Verifies the birthday exists and belongs to the requesting user.
    /// </summary>
    public async Task<BirthdayResponse?> GetByIdForUserAsync(int id, int userId, string baseUrl)
    {
        Birthday? item = await _birthdayRepository.GetByIdAsync(id);
        
        // Security check: If record doesn't exist OR belongs to another user, return null
        if (item == null || item.UserId != userId)
        {
            return null;
        }

        return MapToResponse(item, baseUrl);
    }

    /// <summary>
    /// 4. GET PHOTO BY BIRTHDAY ID
    /// Returns the binary photo bytes and MIME content type from PostgreSQL BYTEA.
    /// Returns null if the birthday doesn't exist, doesn't belong to the user, or has no photo.
    /// </summary>
    public async Task<(byte[] photoBytes, string contentType)?> GetPhotoForUserAsync(int id, int userId)
    {
        Birthday? item = await _birthdayRepository.GetByIdAsync(id);
        if (item == null || item.UserId != userId)
        {
            return null;
        }

        if (item.PhotoData == null || item.PhotoData.Length == 0)
        {
            return null;
        }

        string contentType = string.IsNullOrWhiteSpace(item.PhotoContentType)
            ? "image/jpeg"
            : item.PhotoContentType;

        return (item.PhotoData, contentType);
    }

    /// <summary>
    /// 5. CREATE BIRTHDAY
    /// Saves a new birthday with optional photo bytes stored directly in PostgreSQL BYTEA.
    /// </summary>
    public async Task<BirthdayResponse> CreateForUserAsync(int userId, BirthdayRequest request, string baseUrl)
    {
        byte[]? photoBytes = null;
        string? contentType = null;

        // If the user uploaded a photo file, validate size and read bytes into memory
        if (request.Photo != null && request.Photo.Length > 0)
        {
            (photoBytes, contentType) = await ConvertPhotoToBytesAsync(request.Photo);
        }

        // Construct the database model
        Birthday newBirthday = new()
        {
            UserId = userId,
            PersonName = request.PersonName.Trim(),
            DateOfBirth = request.DateOfBirth,
            Relationship = request.Relationship,
            Notes = request.Notes?.Trim(),
            RemindDaysBefore = request.RemindDaysBefore,
            PhotoData = photoBytes,
            PhotoContentType = contentType
        };

        // Save to repository (assigns a unique ID)
        Birthday created = await _birthdayRepository.CreateAsync(newBirthday);
        return MapToResponse(created, baseUrl);
    }

    /// <summary>
    /// 6. UPDATE BIRTHDAY
    /// Updates an existing birthday. If a new photo is uploaded, replaces photo bytes in database.
    /// </summary>
    public async Task<BirthdayResponse?> UpdateForUserAsync(int id, int userId, BirthdayRequest request, string baseUrl)
    {
        // 1. Fetch existing record and verify ownership
        Birthday? existing = await _birthdayRepository.GetByIdAsync(id);
        if (existing == null || existing.UserId != userId)
        {
            return null;
        }

        // 2. Handle photo updates: remove photo or replace with newly uploaded photo bytes
        if (request.RemovePhoto)
        {
            existing.PhotoData = null;
            existing.PhotoContentType = null;
        }
        else if (request.Photo != null && request.Photo.Length > 0)
        {
            (byte[] photoBytes, string contentType) = await ConvertPhotoToBytesAsync(request.Photo);
            existing.PhotoData = photoBytes;
            existing.PhotoContentType = contentType;
        }

        // 3. Update fields with new values
        existing.PersonName = request.PersonName.Trim();
        existing.DateOfBirth = request.DateOfBirth;
        existing.Relationship = request.Relationship;
        existing.Notes = request.Notes?.Trim();
        existing.RemindDaysBefore = request.RemindDaysBefore;

        // 4. Save changes
        Birthday? updated = await _birthdayRepository.UpdateAsync(existing);
        if (updated == null)
        {
            return null;
        }

        return MapToResponse(updated, baseUrl);
    }

    /// <summary>
    /// 7. DELETE BIRTHDAY
    /// Deletes a birthday record. The photo bytes inside PostgreSQL are removed automatically!
    /// </summary>
    public async Task<bool> DeleteForUserAsync(int id, int userId)
    {
        // 1. Verify record exists and belongs to requesting user
        Birthday? existing = await _birthdayRepository.GetByIdAsync(id);
        if (existing == null || existing.UserId != userId)
        {
            return false;
        }

        // 2. Delete from repository (deleting row automatically deletes the BYTEA photo column)
        return await _birthdayRepository.DeleteAsync(id);
    }

    // =========================================================================
    // PRIVATE HELPER METHODS
    // =========================================================================

    /// <summary>
    /// Validates photo size and extension, then converts the uploaded file into a byte array for PostgreSQL BYTEA storage.
    /// </summary>
    private static async Task<(byte[] photoBytes, string contentType)> ConvertPhotoToBytesAsync(IFormFile file)
    {
        // Check 1: File size limit (500 KB)
        if (file.Length > MaxFileSizeBytes)
        {
            throw new ArgumentException("Photo file size cannot exceed 500 KB.");
        }

        // Check 2: Extension check (.jpg, .jpeg, .png, .webp)
        string extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        bool isAllowed = extension == ".jpg" || extension == ".jpeg" || extension == ".png" || extension == ".webp";
        if (!isAllowed)
        {
            throw new ArgumentException("Only .jpg, .jpeg, .png, and .webp images are allowed.");
        }

        // Check 3: Content type (fallback based on extension if browser didn't supply one)
        string contentType = file.ContentType;
        if (string.IsNullOrWhiteSpace(contentType) || contentType == "application/octet-stream")
        {
            contentType = extension switch
            {
                ".png" => "image/png",
                ".webp" => "image/webp",
                _ => "image/jpeg"
            };
        }

        // Read file bytes into memory
        using MemoryStream memoryStream = new();
        await file.CopyToAsync(memoryStream);
        byte[] photoBytes = memoryStream.ToArray();

        return (photoBytes, contentType);
    }

    /// <summary>
    /// Maps a database Birthday entity to a client-friendly BirthdayResponse DTO.
    /// Calculates DaysUntilBirthday, TurningAge, HasPhoto boolean, and PhotoUrl endpoint.
    /// </summary>
    private static BirthdayResponse MapToResponse(Birthday item, string baseUrl)
    {
        (int daysUntil, int turningAge) = CalculateBirthdayStats(item.DateOfBirth, DateTime.Today);

        bool hasPhoto = item.PhotoData != null && item.PhotoData.Length > 0;
        string? photoUrl = hasPhoto ? $"{baseUrl.TrimEnd('/')}/api/birthdays/{item.Id}/photo" : null;

        return new BirthdayResponse
        {
            Id = item.Id,
            PersonName = item.PersonName,
            DateOfBirth = item.DateOfBirth,
            Relationship = item.Relationship,
            Notes = item.Notes,
            HasPhoto = hasPhoto,
            PhotoUrl = photoUrl,
            DaysUntilBirthday = daysUntil,
            TurningAge = turningAge,
            RemindDaysBefore = item.RemindDaysBefore
        };
    }

    /// <summary>
    /// Calculates the number of days until the person's next birthday and their upcoming age.
    /// Correctly handles February 29 leap-year babies by using February 28 in non-leap years!
    /// </summary>
    public static (int daysUntil, int turningAge) CalculateBirthdayStats(DateTime dateOfBirth, DateTime today)
    {
        int birthYear = dateOfBirth.Year;
        int birthMonth = dateOfBirth.Month;
        int birthDay = dateOfBirth.Day;

        int currentYear = today.Year;

        // Step 1: Find their birthday date in the current calendar year
        DateTime birthdayThisYear = GetDateInYear(birthMonth, birthDay, currentYear);

        DateTime nextBirthdayDate;
        int turningAge;

        if (birthdayThisYear.Date >= today.Date)
        {
            // Birthday is either TODAY or coming up later this year
            nextBirthdayDate = birthdayThisYear;
            turningAge = currentYear - birthYear;
        }
        else
        {
            // Birthday already passed earlier this year, so next one is next year!
            int nextYear = currentYear + 1;
            nextBirthdayDate = GetDateInYear(birthMonth, birthDay, nextYear);
            turningAge = nextYear - birthYear;
        }

        // Calculate days difference
        int daysUntil = (int)(nextBirthdayDate.Date - today.Date).TotalDays;
        return (daysUntil, turningAge);
    }

    /// <summary>
    /// Returns a valid DateTime for a month/day in a specific year.
    /// If someone was born on Feb 29 and target year is NOT a leap year, returns Feb 28.
    /// </summary>
    private static DateTime GetDateInYear(int month, int day, int year)
    {
        if (month == 2 && day == 29)
        {
            if (!DateTime.IsLeapYear(year))
            {
                return new DateTime(year, 2, 28);
            }
        }

        return new DateTime(year, month, day);
    }
}

