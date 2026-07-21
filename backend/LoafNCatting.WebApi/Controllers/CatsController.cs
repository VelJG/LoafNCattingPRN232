using LoafNCatting.Application.Contracts;
using LoafNCatting.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace LoafNCatting.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class CatsController : ControllerBase
{
    private readonly ICatService _service;

    public CatsController(ICatService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<List<CatDto>>> GetCats([FromQuery] string? search)
    {
        return Ok(await _service.GetCatsAsync(search));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CatDto>> GetCat(int id)
    {
        var cat = await _service.GetCatAsync(id);
        return cat is null ? NotFound() : Ok(cat);
    }
}
