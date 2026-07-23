using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using LoafNCatting.Application.Contracts;
using LoafNCatting.Application.Interfaces.Services;
using Microsoft.Extensions.Configuration;

namespace LoafNCatting.Services.Services;

public sealed class S3MediaStorageService : IMediaStorageService
{
    private const long MaxUploadBytes = 1024 * 1024;
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png"
    };

    private readonly IAmazonS3 _client;
    private readonly string _bucketName;
    private readonly int _uploadExpiryMinutes;
    private readonly int _downloadExpiryMinutes;

    public S3MediaStorageService(IConfiguration configuration)
    {
        _bucketName = configuration["S3:BucketName"]?.Trim() ?? string.Empty;
        var regionName = configuration["S3:Region"]?.Trim() ?? string.Empty;
        _uploadExpiryMinutes = ReadInt(configuration["S3:UploadUrlExpiresInMinutes"], 10);
        _downloadExpiryMinutes = ReadInt(configuration["S3:DownloadUrlExpiresInMinutes"], 60);

        if (string.IsNullOrWhiteSpace(_bucketName) || string.IsNullOrWhiteSpace(regionName))
        {
            throw new InvalidOperationException("S3 BucketName/Region are not configured.");
        }

        var region = RegionEndpoint.GetBySystemName(regionName);
        var accessKey = configuration["S3:AccessKey"]?.Trim();
        var secretKey = configuration["S3:SecretKey"]?.Trim();
        var sessionToken = configuration["S3:SessionToken"]?.Trim();

        _client = !string.IsNullOrWhiteSpace(accessKey) && !string.IsNullOrWhiteSpace(secretKey)
            ? new AmazonS3Client(
                string.IsNullOrWhiteSpace(sessionToken)
                    ? new BasicAWSCredentials(accessKey, secretKey)
                    : new SessionAWSCredentials(accessKey, secretKey, sessionToken),
                region)
            : new AmazonS3Client(region);
    }

    public PresignedUploadDto CreateUploadUrl(MediaAssetKind kind, PresignedUploadRequestDto request)
    {
        ValidateRequest(request);

        var normalizedContentType = NormalizeContentType(request.ContentType);
        var extension = GetExtension(request.FileName, normalizedContentType);
        var key = $"{GetPrefix(kind)}/{Guid.NewGuid():N}{extension}";
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(_uploadExpiryMinutes);
        var uploadUrl = _client.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = _bucketName,
            Key = key,
            Verb = HttpVerb.PUT,
            ContentType = normalizedContentType,
            Expires = expiresAtUtc
        });

        return new PresignedUploadDto(
            uploadUrl,
            key,
            CreatePresignedDownloadUrl(key),
            expiresAtUtc);
    }

    public string? NormalizeStoredKey(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var raw = value.Trim();
        if (HasScheme(raw) || IsManagedKey(raw))
        {
            return raw;
        }

        return raw switch
        {
            _ when raw.StartsWith("/Images/Cats/", StringComparison.OrdinalIgnoreCase)
                => $"cat/{ExtractFileName(raw)}",
            _ when raw.StartsWith("/Images/Beverages/", StringComparison.OrdinalIgnoreCase)
                => $"product/{ExtractFileName(raw)}",
            _ when raw.StartsWith("/Images/CatFood/", StringComparison.OrdinalIgnoreCase)
                => $"product/{ExtractFileName(raw)}",
            _ when raw.StartsWith("/Images/Avatars/", StringComparison.OrdinalIgnoreCase)
                => $"avatar/{ExtractFileName(raw)}",
            _ when raw.StartsWith("/Images/Users/", StringComparison.OrdinalIgnoreCase)
                => $"avatar/{ExtractFileName(raw)}",
            _ => raw
        };
    }

    public string? ResolveDisplayUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = NormalizeStoredKey(value);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        if (HasScheme(normalized) || !IsManagedKey(normalized))
        {
            return normalized;
        }

        return CreatePresignedDownloadUrl(normalized);
    }

    private static int ReadInt(string? rawValue, int defaultValue)
        => int.TryParse(rawValue, out var parsed) && parsed > 0
            ? parsed
            : defaultValue;

    private static void ValidateRequest(PresignedUploadRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.FileName))
        {
            throw new InvalidOperationException("File name is required.");
        }

        if (request.FileSizeBytes <= 0 || request.FileSizeBytes > MaxUploadBytes)
        {
            throw new InvalidOperationException("Only JPG/PNG images up to 1 MB are allowed.");
        }

        if (!AllowedContentTypes.Contains(request.ContentType.Trim()))
        {
            throw new InvalidOperationException("Only JPG/PNG images are allowed.");
        }
    }

    private string CreatePresignedDownloadUrl(string key)
        => _client.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = _bucketName,
            Key = key,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.AddMinutes(_downloadExpiryMinutes)
        });

    private static string NormalizeContentType(string contentType)
        => contentType.Trim().Equals("image/png", StringComparison.OrdinalIgnoreCase)
            ? "image/png"
            : "image/jpeg";

    private static string GetExtension(string fileName, string contentType)
    {
        var extension = Path.GetExtension(fileName)?.Trim().ToLowerInvariant();
        if (extension is ".jpg" or ".jpeg")
        {
            return ".jpg";
        }

        if (extension == ".png")
        {
            return ".png";
        }

        return contentType.Equals("image/png", StringComparison.OrdinalIgnoreCase)
            ? ".png"
            : ".jpg";
    }

    private static string GetPrefix(MediaAssetKind kind) => kind switch
    {
        MediaAssetKind.Avatar => "avatar",
        MediaAssetKind.Product => "product",
        MediaAssetKind.Cat => "cat",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
    };

    private static string ExtractFileName(string value)
    {
        var slashIndex = value.LastIndexOf('/');
        return slashIndex >= 0 ? value[(slashIndex + 1)..] : value;
    }

    private static bool HasScheme(string value)
        => Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
           !string.IsNullOrWhiteSpace(uri.Scheme);

    private static bool IsManagedKey(string value)
        => value.StartsWith("avatar/", StringComparison.OrdinalIgnoreCase) ||
           value.StartsWith("product/", StringComparison.OrdinalIgnoreCase) ||
           value.StartsWith("cat/", StringComparison.OrdinalIgnoreCase);
}
