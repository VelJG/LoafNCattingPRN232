using LoafNCatting.Application.Contracts;

namespace LoafNCatting.Application.Interfaces.Services;

public enum MediaAssetKind
{
    Avatar,
    Product,
    Cat
}

public interface IMediaStorageService
{
    PresignedUploadDto CreateUploadUrl(MediaAssetKind kind, PresignedUploadRequestDto request);

    Task<MediaUploadResultDto> UploadAsync(
        MediaAssetKind kind,
        PresignedUploadRequestDto request,
        Stream content,
        CancellationToken cancellationToken = default);

    string? NormalizeStoredKey(string? value);

    string? ResolveDisplayUrl(string? value);
}
