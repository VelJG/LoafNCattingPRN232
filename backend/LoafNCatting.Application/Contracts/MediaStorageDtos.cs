using System.ComponentModel.DataAnnotations;

namespace LoafNCatting.Application.Contracts;

public sealed record PresignedUploadRequestDto(
    [param: Required, MaxLength(255)] string FileName,
    [param: Required, MaxLength(100)] string ContentType,
    [param: Range(1, long.MaxValue)] long FileSizeBytes);

public sealed record PresignedUploadDto(
    string UploadUrl,
    string S3Key,
    string FileUrl,
    DateTime ExpiresAtUtc);
