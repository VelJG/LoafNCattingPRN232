using LoafNCatting.Application.Contracts;
using LoafNCatting.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LoafNCatting.WebApi.Controllers;

[ApiController]
[Authorize(Roles = "Admin,Staff")]
[Route("api/admin/cats")]
public sealed class AdminCatsController : ControllerBase
{
    private readonly IAdminCatService _catService;

    public AdminCatsController(IAdminCatService catService)
    {
        _catService = catService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AdminCatDto>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] string? gender)
        => Ok(await _catService.GetAllAsync(search, status, gender));

    [HttpGet("options")]
    public async Task<ActionResult<AdminCatOptionsDto>> GetOptions()
        => Ok(await _catService.GetOptionsAsync());

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AdminCatDto>> GetById(int id)
    {
        var cat = await _catService.GetByIdAsync(id);
        return cat is null ? NotFound() : Ok(cat);
    }

    [HttpPost]
    public async Task<ActionResult<AdminCatDto>> Create(AdminCatUpsertRequest request)
    {
        try
        {
            var cat = await _catService.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = cat.CatId }, cat);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<AdminCatDto>> Update(int id, AdminCatUpsertRequest request)
    {
        try
        {
            var cat = await _catService.UpdateAsync(id, request);
            return cat is null ? NotFound() : Ok(cat);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
        => await _catService.DeleteAsync(id) ? NoContent() : NotFound();
}
