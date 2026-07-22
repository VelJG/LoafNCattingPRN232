using System.ComponentModel.DataAnnotations;

namespace LoafNCatting.Application.Contracts;

public sealed record ConversationDto(
    int ConversationId,
    int CustomerUserId,
    string CustomerName,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

public sealed record CustomerConversationDto(ConversationDto? Conversation);

public sealed record StoreConversationDto(
    int ConversationId,
    int CustomerUserId,
    string CustomerName,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    string? LastMessageContent,
    DateTime? LastMessageAtUtc,
    string? LastMessageSenderRole,
    int UnreadCustomerMessageCount);

public sealed record MessageDto(
    int MessageId,
    int ConversationId,
    int SenderUserId,
    string SenderName,
    string SenderRole,
    string Content,
    DateTime SentAtUtc,
    bool IsRead);

public sealed record MarkMessagesReadResultDto(int UpdatedCount);

public sealed record MessageCreatedRealtimeDto(
    int CustomerUserId,
    MessageDto Message);

public sealed record MessagesReadRealtimeDto(
    int ConversationId,
    int ReaderUserId,
    string ReaderRole,
    int UpdatedCount);

public static class MessageRealtimeEvents
{
    public const string MessageCreated = "MessageCreated";
    public const string MessagesRead = "MessagesRead";
}

public sealed class SendMessageRequest
{
    [Required]
    public string? Content { get; init; }
}
