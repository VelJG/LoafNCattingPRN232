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

    [HttpPost("cat")]
    [Authorize(Roles = "Admin,Staff")]
    public Task<IActionResult> CreateCatUploadUrl(PresignedUploadRequestDto request)
        => HandleAsync(() => Task.FromResult(
            mediaStorage.CreateUploadUrl(MediaAssetKind.Cat, request)));
}
