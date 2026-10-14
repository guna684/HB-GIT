using backend.DTOs;
using backend.Models;
using backend.Repositories;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace backend.Services;

/// <summary>
/// Handles business logic for birthday management:
/// calculating upcoming dates, turning ages, photo uploads, and user isolation.
/// </summary>
public class BirthdayService
{
    private readonly IBirthdayRepository _birthdayRepository;
    private readonly IWebHostEnvironment _environment;

    // Allowed image file extensions for profile photos
    private static readonly string[] AllowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };

    // Maximum file upload size: 2 Megabytes (2 * 1024 * 1024 bytes)
    private const long MaxFileSizeBytes = 2 * 1024 * 1024;

    public BirthdayService(IBirthdayRepository birthdayRepository, IWebHostEnvironment environment)
    {
        _birthdayRepository = birthdayRepository;
        _environment = environment;
    }

    /// <summary>
    /// Retrieves all birthdays belonging to the logged-in user, mapped to response DTOs.
    /// Birthdays are sorted so the closest upcoming one comes first.
    /// </summary>
    public async Task<List<BirthdayResponse>> GetAllForUserAsync(int userId, string baseUrl)
    {
        List<Birthday> list = await _birthdayRepository.GetAllByUserIdAsync(userId);
        List<BirthdayResponse> responses = new();

        foreach (Birthday item in list)
        {
            responses.Add(MapToResponse(item, baseUrl));
        }

        // Sort by days until birthday (nearest first)
        responses.Sort((a, b) => a.DaysUntilBirthday.CompareTo(b.DaysUntilBirthday));

        return responses;
    }

    /// <summary>
    /// Retrieves upcoming birthdays within the next 30 days (0 to 30 days remaining),
    /// sorted by nearest first.
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
    /// Finds a single birthday by ID, ensuring it belongs to the logged-in user.
    /// Returns null if not found or if owned by another user.
    /// </summary>
    public async Task<BirthdayResponse?> GetByIdForUserAsync(int id, int userId, string baseUrl)
    {
        Birthday? item = await _birthdayRepository.GetByIdAsync(id);
        if (item == null || item.UserId != userId)
        {
            return null;
        }

        return MapToResponse(item, baseUrl);
    }

    /// <summary>
    /// Creates a new birthday entry for the user.
    /// Handles saving an optional photo to wwwroot/uploads.
    /// </summary>
    public async Task<BirthdayResponse> CreateForUserAsync(int userId, BirthdayRequest request, string baseUrl)
    {
        string? photoFileName = null;

        // Process photo upload if the user provided one
        if (request.Photo != null && request.Photo.Length > 0)
        {
            photoFileName = await SavePhotoAsync(request.Photo);
        }

        Birthday newBirthday = new()
        {
            UserId = userId,
            PersonName = request.PersonName.Trim(),
            DateOfBirth = request.DateOfBirth,
            Relationship = request.Relationship,
            Notes = request.Notes?.Trim(),
            RemindDaysBefore = request.RemindDaysBefore,
            PhotoFileName = photoFileName
        };

        Birthday created = await _birthdayRepository.CreateAsync(newBirthday);
        return MapToResponse(created, baseUrl);
    }

    /// <summary>
    /// Updates an existing birthday's details.
    /// If a new photo is uploaded, the old photo file is safely deleted from disk.
    /// </summary>
    public async Task<BirthdayResponse?> UpdateForUserAsync(int id, int userId, BirthdayRequest request, string baseUrl)
    {
        Birthday? existing = await _birthdayRepository.GetByIdAsync(id);
        if (existing == null || existing.UserId != userId)
        {
            return null;
        }

        // If a new photo is uploaded, delete the old one and save the new one
        if (request.Photo != null && request.Photo.Length > 0)
        {
            if (!string.IsNullOrEmpty(existing.PhotoFileName))
            {
                DeletePhotoFile(existing.PhotoFileName);
            }
            existing.PhotoFileName = await SavePhotoAsync(request.Photo);
        }

        existing.PersonName = request.PersonName.Trim();
        existing.DateOfBirth = request.DateOfBirth;
        existing.Relationship = request.Relationship;
        existing.Notes = request.Notes?.Trim();
        existing.RemindDaysBefore = request.RemindDaysBefore;

        Birthday? updated = await _birthdayRepository.UpdateAsync(existing);
        if (updated == null)
        {
            return null;
        }

        return MapToResponse(updated, baseUrl);
    }

    /// <summary>
    /// Deletes a birthday and removes its associated photo from disk if present.
    /// Ensures only the owner can delete their birthday.
    /// </summary>
    public async Task<bool> DeleteForUserAsync(int id, int userId)
    {
        Birthday? existing = await _birthdayRepository.GetByIdAsync(id);
        if (existing == null || existing.UserId != userId)
        {
            return false;
        }

        // Delete photo file from disk if one exists
        if (!string.IsNullOrEmpty(existing.PhotoFileName))
        {
            DeletePhotoFile(existing.PhotoFileName);
        }

        return await _birthdayRepository.DeleteAsync(id);
    }

    /// <summary>
    /// Validates and saves an uploaded photo to wwwroot/uploads with a unique GUID name.
    /// </summary>
    private async Task<string> SavePhotoAsync(IFormFile file)
    {
        // 1. Check file size
        if (file.Length > MaxFileSizeBytes)
        {
            throw new ArgumentException("Photo file size cannot exceed 2 MB.");
        }

        // 2. Check file extension
        string extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        bool isAllowed = false;
        foreach (string allowed in AllowedExtensions)
        {
            if (allowed == extension)
            {
                isAllowed = true;
                break;
            }
        }

        if (!isAllowed)
        {
            throw new ArgumentException("Only .jpg, .jpeg, .png, and .webp images are allowed.");
        }

        // 3. Determine upload folder path in wwwroot/uploads
        string webRootPath = _environment.WebRootPath;
        if (string.IsNullOrEmpty(webRootPath))
        {
            webRootPath = Path.Combine(_environment.ContentRootPath, "wwwroot");
        }

        string uploadsFolder = Path.Combine(webRootPath, "uploads");
        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        // 4. Generate unique file name to prevent accidental overwrites
        string uniqueFileName = $"{Guid.NewGuid()}{extension}";
        string fullFilePath = Path.Combine(uploadsFolder, uniqueFileName);

        // 5. Write the file bytes to disk
        using (FileStream stream = new(fullFilePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        return uniqueFileName;
    }

    /// <summary>
    /// Helper method to delete a photo file from wwwroot/uploads.
    /// </summary>
    private void DeletePhotoFile(string fileName)
    {
        string webRootPath = _environment.WebRootPath;
        if (string.IsNullOrEmpty(webRootPath))
        {
            webRootPath = Path.Combine(_environment.ContentRootPath, "wwwroot");
        }

        string fullFilePath = Path.Combine(webRootPath, "uploads", fileName);
        if (File.Exists(fullFilePath))
        {
            try
            {
                File.Delete(fullFilePath);
            }
            catch
            {
                // Silently ignore deletion errors if file is locked or already gone
            }
        }
    }

    /// <summary>
    /// Maps a Birthday database model to a BirthdayResponse DTO.
    /// Computes daysUntilBirthday, turningAge, and full photoUrl.
    /// </summary>
    private static BirthdayResponse MapToResponse(Birthday item, string baseUrl)
    {
        (int daysUntil, int turningAge) = CalculateBirthdayStats(item.DateOfBirth, DateTime.Today);

        string? fullPhotoUrl = null;
        if (!string.IsNullOrEmpty(item.PhotoFileName))
        {
            // Create full URL like http://localhost:5000/uploads/guid.jpg
            fullPhotoUrl = $"{baseUrl.TrimEnd('/')}/uploads/{item.PhotoFileName}";
        }

        return new BirthdayResponse
        {
            Id = item.Id,
            PersonName = item.PersonName,
            DateOfBirth = item.DateOfBirth,
            Relationship = item.Relationship,
            Notes = item.Notes,
            PhotoUrl = fullPhotoUrl,
            DaysUntilBirthday = daysUntil,
            TurningAge = turningAge,
            RemindDaysBefore = item.RemindDaysBefore
        };
    }

    /// <summary>
    /// Calculates the number of days until the person's next birthday and their upcoming age.
    /// Handles February 29 leap year babies correctly by using February 28 in non-leap years.
    /// </summary>
    public static (int daysUntil, int turningAge) CalculateBirthdayStats(DateTime dateOfBirth, DateTime today)
    {
        int birthYear = dateOfBirth.Year;
        int birthMonth = dateOfBirth.Month;
        int birthDay = dateOfBirth.Day;

        int currentYear = today.Year;

        // 1. Get the birthday for the current year (handling Feb 29 if necessary)
        DateTime birthdayThisYear = GetDateInYear(birthMonth, birthDay, currentYear);

        DateTime nextBirthdayDate;
        int turningAge;

        if (birthdayThisYear.Date >= today.Date)
        {
            // Birthday is today or later this year
            nextBirthdayDate = birthdayThisYear;
            turningAge = currentYear - birthYear;
        }
        else
        {
            // Birthday already passed earlier this year, so next one is next year
            int nextYear = currentYear + 1;
            nextBirthdayDate = GetDateInYear(birthMonth, birthDay, nextYear);
            turningAge = nextYear - birthYear;
        }

        int daysUntil = (int)(nextBirthdayDate.Date - today.Date).TotalDays;
        return (daysUntil, turningAge);
    }

    /// <summary>
    /// Returns a valid DateTime for a given month and day in a target year.
    /// If the birth date is February 29 and the target year is not a leap year,
    /// it safely falls back to February 28.
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
