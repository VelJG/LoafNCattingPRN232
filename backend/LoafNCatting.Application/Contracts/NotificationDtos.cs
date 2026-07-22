namespace LoafNCatting.Application.Contracts;

public sealed record NotificationDraft(
    string Title,
    string Content,
    string Type);

public static class NotificationTypes
{
    public const string ReservationCreated = "ReservationCreated";
    public const string ReservationConfirmed = "ReservationConfirmed";
    public const string ReservationCancelled = "ReservationCancelled";
    public const string ReservationNoShow = "ReservationNoShow";
    public const string ReservationExpired = "ReservationExpired";
    public const string ReservationEndingSoon = "ReservationEndingSoon";
    public const string ReservationLifecycleConflict = "ReservationLifecycleConflict";
    public const string NewCustomerMessage = "NewCustomerMessage";
    public const string NewStaffReply = "NewStaffReply";
}
