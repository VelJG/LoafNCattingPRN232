using LoafNCatting.Application.Contracts;
using LoafNCatting.Application.Interfaces.Repositories;
using LoafNCatting.Application.Interfaces.Services;
using LoafNCatting.Entity.Models;
using Microsoft.EntityFrameworkCore;

namespace LoafNCatting.Services.Services;

public sealed class AdminCatService : IAdminCatService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMediaStorageService _mediaStorage;

    public AdminCatService(
        IUnitOfWork unitOfWork,
        IMediaStorageService? mediaStorage = null)
    {
        _unitOfWork = unitOfWork;
        _mediaStorage = mediaStorage ?? PassThroughMediaStorageService.Instance;
    }

    public async Task<IReadOnlyList<AdminCatDto>> GetAllAsync(string? search, string? status, string? gender)
    {
        var query = _unitOfWork.Repository<Cat>().Entities.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(cat =>
                cat.Name.Contains(term) ||
                cat.Breed != null && cat.Breed.Contains(term) ||
                cat.Description != null && cat.Description.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            var statusName = status.Trim();
            query = query.Where(cat => cat.Status.StatusName == statusName);
        }

        if (!string.IsNullOrWhiteSpace(gender))
        {
            var genderName = gender.Trim();
            query = query.Where(cat => cat.Gender != null && cat.Gender.GenderName == genderName);
        }

        var items = await query
            .OrderBy(cat => cat.Name)
            .Select(cat => new AdminCatDto
            {
                CatId = cat.CatId,
                Name = cat.Name,
                Age = cat.Age,
                Gender = cat.Gender == null ? null : cat.Gender.GenderName,
                Breed = cat.Breed,
                Picture = cat.Picture,
                PictureKey = cat.Picture,
                Description = cat.Description,
                FriendlinessRating = cat.FriendlinessRating,
                CutenessRating = cat.CutenessRating,
                PlayfulnessRating = cat.PlayfulnessRating,
                Status = cat.Status.StatusName,
                CreatedAt = cat.CreatedAt,
                UpdatedAt = cat.UpdatedAt
            })
            .ToListAsync();

        foreach (var item in items)
        {
            ResolveMedia(item);
        }

        return items;
    }

    public async Task<AdminCatDto?> GetByIdAsync(int catId)
    {
        var item = await _unitOfWork.Repository<Cat>().Entities
            .AsNoTracking()
            .Where(cat => cat.CatId == catId)
            .Select(cat => new AdminCatDto
            {
                CatId = cat.CatId,
                Name = cat.Name,
                Age = cat.Age,
                Gender = cat.Gender == null ? null : cat.Gender.GenderName,
                Breed = cat.Breed,
                Picture = cat.Picture,
                PictureKey = cat.Picture,
                Description = cat.Description,
                FriendlinessRating = cat.FriendlinessRating,
                CutenessRating = cat.CutenessRating,
                PlayfulnessRating = cat.PlayfulnessRating,
                Status = cat.Status.StatusName,
                CreatedAt = cat.CreatedAt,
                UpdatedAt = cat.UpdatedAt
            })
            .FirstOrDefaultAsync();

        return item is null ? null : ResolveMedia(item);
    }

    public async Task<AdminCatOptionsDto> GetOptionsAsync()
        => new()
        {
            Statuses = await _unitOfWork.Repository<CatStatus>().Entities
                .AsNoTracking()
                .OrderBy(status => status.StatusName)
                .Select(status => status.StatusName)
                .ToListAsync(),
            Genders = await _unitOfWork.Repository<Gender>().Entities
                .AsNoTracking()
                .OrderBy(gender => gender.GenderName)
                .Select(gender => gender.GenderName)
                .ToListAsync()
        };

    public async Task<AdminCatDto> CreateAsync(AdminCatUpsertRequest request)
    {
        var references = await ValidateAndResolveAsync(request);

        var cat = new Cat
        {
            Name = request.Name.Trim(),
            Age = request.Age,
            GenderId = references.GenderId,
            Breed = Clean(request.Breed),
            Picture = Clean(request.Picture),
            Description = Clean(request.Description),
            FriendlinessRating = request.FriendlinessRating,
            CutenessRating = request.CutenessRating,
            PlayfulnessRating = request.PlayfulnessRating,
            StatusId = references.StatusId,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Repository<Cat>().InsertAsync(cat, saveChanges: false);
        await _unitOfWork.SaveChangesAsync();

        return (await GetByIdAsync(cat.CatId))!;
    }

    public async Task<AdminCatDto?> UpdateAsync(int catId, AdminCatUpsertRequest request)
    {
        var references = await ValidateAndResolveAsync(request);

        var cat = await _unitOfWork.Repository<Cat>().FindAsync(catId);
        if (cat is null)
        {
            return null;
        }

        cat.Name = request.Name.Trim();
        cat.Age = request.Age;
        cat.GenderId = references.GenderId;
        cat.Breed = Clean(request.Breed);
        cat.Picture = Clean(request.Picture);
        cat.Description = Clean(request.Description);
        cat.FriendlinessRating = request.FriendlinessRating;
        cat.CutenessRating = request.CutenessRating;
        cat.PlayfulnessRating = request.PlayfulnessRating;
        cat.StatusId = references.StatusId;
        cat.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync();

        return await GetByIdAsync(catId);
    }

    public async Task<bool> DeleteAsync(int catId)
    {
        var cat = await _unitOfWork.Repository<Cat>().FindAsync(catId);
        if (cat is null)
        {
            return false;
        }

        await _unitOfWork.Repository<Cat>().DeleteAsync(cat, saveChanges: false);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    private async Task<(int StatusId, int? GenderId)> ValidateAndResolveAsync(AdminCatUpsertRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("Cat name is required.");
        }

        if (request.Age is < 0)
        {
            throw new ArgumentException("Age must be greater than or equal to 0.");
        }

        ValidateRating(request.FriendlinessRating, "Friendliness rating");
        ValidateRating(request.CutenessRating, "Cuteness rating");
        ValidateRating(request.PlayfulnessRating, "Playfulness rating");

        if (string.IsNullOrWhiteSpace(request.Status))
        {
            throw new ArgumentException("Cat status is required.");
        }

        var statusName = request.Status.Trim();
        var statusId = await _unitOfWork.Repository<CatStatus>().Entities
            .AsNoTracking()
            .Where(status => status.StatusName == statusName)
            .Select(status => (int?)status.StatusId)
            .FirstOrDefaultAsync();

        if (statusId is null)
        {
            throw new ArgumentException("Cat status does not exist.");
        }

        int? genderId = null;
        if (!string.IsNullOrWhiteSpace(request.Gender))
        {
            var genderName = request.Gender.Trim();
            genderId = await _unitOfWork.Repository<Gender>().Entities
                .AsNoTracking()
                .Where(gender => gender.GenderName == genderName)
                .Select(gender => (int?)gender.GenderId)
                .FirstOrDefaultAsync();

            if (genderId is null)
            {
                throw new ArgumentException("Gender does not exist.");
            }
        }

        return (statusId.Value, genderId);
    }

    private AdminCatDto ResolveMedia(AdminCatDto item)
    {
        item.PictureKey = _mediaStorage.NormalizeStoredKey(item.PictureKey);
        item.Picture = _mediaStorage.ResolveDisplayUrl(item.Picture);
        return item;
    }

    private static string? Clean(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void ValidateRating(int? rating, string name)
    {
        if (rating is < 1 or > 5)
        {
            throw new ArgumentException($"{name} must be between 1 and 5.");
        }
    }
}
