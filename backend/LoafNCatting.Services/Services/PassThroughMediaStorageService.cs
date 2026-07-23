using LoafNCatting.Application.Contracts;
using LoafNCatting.Application.Interfaces.Services;

namespace LoafNCatting.Services.Services;

public sealed class PassThroughMediaStorageService : IMediaStorageService
{
    public static PassThroughMediaStorageService Instance { get; } = new();

    private PassThroughMediaStorageService()
    {
    }

    public PresignedUploadDto CreateUploadUrl(MediaAssetKind kind, PresignedUploadRequestDto request)
    {
        throw new InvalidOperationException("Media upload storage is not configured.");
    }

    public string? NormalizeStoredKey(string? value) => value?.Trim();

    public string? ResolveDisplayUrl(string? value) => value?.Trim();
}
