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

    public async Task<IReadOnlyList<NotificationDto>> GetForUserAsync(
        int userId,
        bool? isRead = null,
        CancellationToken cancellationToken = default)
    {
        ValidateUserId(userId);
        await EnsureActiveUserAsync(userId, cancellationToken);

        var query = _unitOfWork.Repository<Notification>()
            .Entities
            .AsNoTracking()
            .Where(notification => notification.UserId == userId);
        if (isRead.HasValue)
        {
            query = query.Where(notification => notification.IsRead == isRead.Value);
        }

        return await query
            .OrderByDescending(notification => notification.CreatedAt)
            .ThenByDescending(notification => notification.NotificationId)
            .Select(notification => new NotificationDto(
                notification.NotificationId,
                notification.Title,
                notification.Content,
                notification.Type,
                notification.IsRead,
                notification.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<UnreadNotificationCountDto> GetUnreadCountAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        ValidateUserId(userId);
        await EnsureActiveUserAsync(userId, cancellationToken);

        var count = await _unitOfWork.Repository<Notification>()
            .Entities
            .AsNoTracking()
            .CountAsync(
                notification =>
                    notification.UserId == userId &&
                    !notification.IsRead,
                cancellationToken);
        return new UnreadNotificationCountDto(count);
    }

    public async Task<NotificationDto> MarkAsReadAsync(
        int userId,
        int notificationId,
        CancellationToken cancellationToken = default)
    {
        ValidateUserId(userId);
        ValidateNotificationId(notificationId);
        await EnsureActiveUserAsync(userId, cancellationToken);

        var repository = _unitOfWork.Repository<Notification>();
        var notification = await repository
            .Entities
            .SingleOrDefaultAsync(
                current =>
                    current.NotificationId == notificationId &&
                    current.UserId == userId,
                cancellationToken)
            ?? throw new KeyNotFoundException("Notification was not found.");

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            await repository.UpdateAsync(notification, saveChanges: false);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return ToDto(notification);
    }

    public async Task<MarkNotificationsReadResultDto> MarkAllAsReadAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        ValidateUserId(userId);
        await EnsureActiveUserAsync(userId, cancellationToken);

        var repository = _unitOfWork.Repository<Notification>();
        var unreadNotifications = await repository
            .Entities
            .Where(notification =>
                notification.UserId == userId &&
                !notification.IsRead)
            .ToListAsync(cancellationToken);
        if (unreadNotifications.Count == 0)
        {
            return new MarkNotificationsReadResultDto(0);
        }

        foreach (var notification in unreadNotifications)
        {
            notification.IsRead = true;
        }

        await repository.UpdateRangeAsync(
            unreadNotifications,
            saveChanges: false);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return new MarkNotificationsReadResultDto(unreadNotifications.Count);
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

    private async Task EnsureActiveUserAsync(
        int userId,
        CancellationToken cancellationToken)
    {
        var isActive = await _unitOfWork.Repository<User>()
            .Entities
            .AsNoTracking()
            .AnyAsync(
                user => user.UserId == userId && user.IsActive,
                cancellationToken);
        if (!isActive)
        {
            throw new UnauthorizedAccessException(
                "The authenticated user account is not active or valid.");
        }
    }

    private static NotificationDto ToDto(Notification notification)
        => new(
            notification.NotificationId,
            notification.Title,
            notification.Content,
            notification.Type,
            notification.IsRead,
            notification.CreatedAt);

    private static void ValidateUserId(int userId)
    {
        if (userId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(userId),
                "Notification user id must be greater than zero.");
        }
    }

    private static void ValidateNotificationId(int notificationId)
    {
        if (notificationId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(notificationId),
                "Notification id must be greater than zero.");
        }
    }

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
