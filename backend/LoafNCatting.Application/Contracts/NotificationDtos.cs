namespace LoafNCatting.Application.Contracts;

public sealed record NotificationDraft(
    string Title,
    string Content,
    string Type);

public sealed record NotificationDto(
    int NotificationId,
    string Title,
    string Content,
    string? Type,
    bool IsRead,
    DateTime CreatedAtUtc);

public sealed record UnreadNotificationCountDto(int Count);

public sealed record MarkNotificationsReadResultDto(int UpdatedCount);

public sealed record NotificationChangedRealtimeDto(
    string ChangeType,
    NotificationDto? Notification,
    int UnreadCount);

public static class NotificationRealtimeChangeTypes
{
    public const string Created = "Created";
    public const string Read = "Read";
    public const string ReadAll = "ReadAll";
}

public static class NotificationRealtimeEvents
{
    public const string Changed = "NotificationChanged";
}

public static class NotificationTypes
{
    public const string ReservationCreated = "ReservationCreated";
    public const string ReservationConfirmed = "ReservationConfirmed";
    public const string ReservationCancelled = "ReservationCancelled";
    public const string ReservationCheckedIn = "ReservationCheckedIn";
    public const string ReservationCompleted = "ReservationCompleted";
    public const string ReservationNoShow = "ReservationNoShow";
    public const string ReservationExpired = "ReservationExpired";
    public const string ReservationEndingSoon = "ReservationEndingSoon";
    public const string ReservationLifecycleConflict = "ReservationLifecycleConflict";
    public const string NewCustomerMessage = "NewCustomerMessage";
    public const string NewStaffReply = "NewStaffReply";
}
