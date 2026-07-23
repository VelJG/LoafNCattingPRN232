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

    string? NormalizeStoredKey(string? value);

    string? ResolveDisplayUrl(string? value);
}
