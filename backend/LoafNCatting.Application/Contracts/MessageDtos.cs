using System.ComponentModel.DataAnnotations;

namespace LoafNCatting.Application.Contracts;

public sealed record ConversationDto(
    int ConversationId,
    int CustomerUserId,
    string CustomerName,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

public sealed record CustomerConversationDto(ConversationDto? Conversation);

public sealed record MessageDto(
    int MessageId,
    int ConversationId,
    int SenderUserId,
    string SenderName,
    string SenderRole,
    string Content,
    DateTime SentAtUtc,
    bool IsRead);

public sealed class SendMessageRequest
{
    [Required]
    public string? Content { get; init; }
}
