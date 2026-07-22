using LoafNCatting.Application.Contracts;

namespace LoafNCatting.Application.Interfaces.Services;

public interface INotificationService
{
    Task<IReadOnlyList<NotificationDto>> GetForUserAsync(
        int userId,
        bool? isRead = null,
        CancellationToken cancellationToken = default);

    Task<UnreadNotificationCountDto> GetUnreadCountAsync(
        int userId,
        CancellationToken cancellationToken = default);

    Task<NotificationDto> MarkAsReadAsync(
        int userId,
        int notificationId,
        CancellationToken cancellationToken = default);

    Task<MarkNotificationsReadResultDto> MarkAllAsReadAsync(
        int userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds one notification to the current unit of work without committing it.
    /// The calling business operation owns the transaction and commit.
    /// </summary>
    Task QueueForUserAsync(
        int userId,
        NotificationDraft draft,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds one notification per active Staff account to the current unit of work
    /// without committing it, and returns the number of queued recipients.
    /// </summary>
    Task<int> QueueForActiveStaffAsync(
        NotificationDraft draft,
        CancellationToken cancellationToken = default);
}
