using LoafNCatting.Application.Contracts;
using LoafNCatting.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace LoafNCatting.WebApi.Controllers;

[ApiController]
[Route("api/store-location")]
public sealed class StoreLocationController : ControllerBase
{
    private readonly IStoreLocationService _service;

    public StoreLocationController(IStoreLocationService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<StoreLocationDto>> GetPrimaryLocation(CancellationToken cancellationToken)
    {
        var location = await _service.GetPrimaryLocationAsync(cancellationToken);
        return location is null ? NotFound() : Ok(location);
    }
}
