using LoafNCatting.Application.Contracts;
using LoafNCatting.Application.Interfaces.Repositories;
using LoafNCatting.Application.Interfaces.Services;
using LoafNCatting.Entity.Models;
using Microsoft.EntityFrameworkCore;

namespace LoafNCatting.Services.Services;

public sealed class NotificationService : INotificationService
{
    private const string StaffRoleName = "Staff";
    private const int MaximumTitleLength = 255;
    private const int MaximumTypeLength = 100;

    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public NotificationService(IUnitOfWork unitOfWork, TimeProvider timeProvider)
    {
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task QueueForUserAsync(
        int userId,
        NotificationDraft draft,
        CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(userId),
                "Notification recipient id must be greater than zero.");
        }

        var normalized = NormalizeDraft(draft);
        var recipientExists = await _unitOfWork.Repository<User>()
            .Entities
            .AsNoTracking()
            .AnyAsync(
                user => user.UserId == userId && user.IsActive,
                cancellationToken);
        if (!recipientExists)
        {
            throw new KeyNotFoundException("Active notification recipient was not found.");
        }

        await _unitOfWork.Repository<Notification>().InsertAsync(
            CreateNotification(userId, normalized),
            saveChanges: false);
    }

    public async Task<int> QueueForActiveStaffAsync(
        NotificationDraft draft,
        CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeDraft(draft);
        var staffUserIds = await _unitOfWork.Repository<User>()
            .Entities
            .AsNoTracking()
            .Where(user =>
                user.IsActive &&
                user.Role.RoleName == StaffRoleName)
            .OrderBy(user => user.UserId)
            .Select(user => user.UserId)
            .ToListAsync(cancellationToken);

        if (staffUserIds.Count == 0)
        {
            return 0;
        }

        var notifications = staffUserIds
            .Select(userId => CreateNotification(userId, normalized))
            .ToList();
        await _unitOfWork.Repository<Notification>().InsertRangeAsync(
            notifications,
            saveChanges: false);
        return notifications.Count;
    }

    private Notification CreateNotification(
        int userId,
        NotificationDraft draft)
        => new()
        {
            UserId = userId,
            Title = draft.Title,
            Content = draft.Content,
            Type = draft.Type,
            IsRead = false,
            CreatedAt = _timeProvider.GetUtcNow().UtcDateTime
        };

    private static NotificationDraft NormalizeDraft(NotificationDraft draft)
    {
        ArgumentNullException.ThrowIfNull(draft);

        var title = RequiredTrimmed(draft.Title, nameof(draft.Title));
        var content = RequiredTrimmed(draft.Content, nameof(draft.Content));
        var type = RequiredTrimmed(draft.Type, nameof(draft.Type));

        if (title.Length > MaximumTitleLength)
        {
            throw new ArgumentException(
                $"Notification title cannot exceed {MaximumTitleLength} characters.",
                nameof(draft));
        }

        if (type.Length > MaximumTypeLength)
        {
            throw new ArgumentException(
                $"Notification type cannot exceed {MaximumTypeLength} characters.",
                nameof(draft));
        }

        return new NotificationDraft(title, content, type);
    }

    private static string RequiredTrimmed(string value, string parameterName)
        => string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException(
                $"{parameterName} is required.",
                parameterName)
            : value.Trim();
}
