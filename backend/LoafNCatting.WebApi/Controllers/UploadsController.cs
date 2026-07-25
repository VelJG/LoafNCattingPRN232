using LoafNCatting.Application.Contracts;
using LoafNCatting.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LoafNCatting.WebApi.Controllers;

[ApiController]
[Authorize]
[Route("api/uploads")]
public sealed class UploadsController(IMediaStorageService mediaStorage) : ApiControllerBase
{
    [HttpPost("avatar")]
    public Task<IActionResult> CreateAvatarUploadUrl(PresignedUploadRequestDto request)
        => HandleAsync(() => Task.FromResult(
            mediaStorage.CreateUploadUrl(MediaAssetKind.Avatar, request)));

    [HttpPost("product")]
    [Authorize(Roles = "Admin,Staff")]
    public Task<IActionResult> CreateProductUploadUrl(PresignedUploadRequestDto request)
        => HandleAsync(() => Task.FromResult(
            mediaStorage.CreateUploadUrl(MediaAssetKind.Product, request)));

    [HttpPost("product/file")]
    [Authorize(Roles = "Admin,Staff")]
    public Task<IActionResult> UploadProduct(IFormFile file, CancellationToken cancellationToken)
        => UploadFile(MediaAssetKind.Product, file, cancellationToken);

    [HttpPost("cat")]
    [Authorize(Roles = "Admin,Staff")]
    public Task<IActionResult> CreateCatUploadUrl(PresignedUploadRequestDto request)
        => HandleAsync(() => Task.FromResult(
            mediaStorage.CreateUploadUrl(MediaAssetKind.Cat, request)));

    [HttpPost("cat/file")]
    [Authorize(Roles = "Admin,Staff")]
    public Task<IActionResult> UploadCat(IFormFile file, CancellationToken cancellationToken)
        => UploadFile(MediaAssetKind.Cat, file, cancellationToken);

    private Task<IActionResult> UploadFile(
        MediaAssetKind kind,
        IFormFile file,
        CancellationToken cancellationToken)
        => HandleAsync(async () =>
        {
            await using var content = file.OpenReadStream();
            return await mediaStorage.UploadAsync(
                kind,
                new PresignedUploadRequestDto(
                    file.FileName,
                    file.ContentType,
                    file.Length),
                content,
                cancellationToken);
        });
}
