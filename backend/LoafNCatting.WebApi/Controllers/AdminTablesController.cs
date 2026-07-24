using LoafNCatting.Application.Contracts;
using LoafNCatting.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LoafNCatting.WebApi.Controllers;

[ApiController]
[Authorize(Roles = "Admin,Staff")]
[Route("api/admin/tables")]
public sealed class AdminTablesController : ApiControllerBase
{
    private readonly IAdminTableService _tableService;

    public AdminTablesController(IAdminTableService tableService)
    {
        _tableService = tableService;
    }

    [HttpGet]
    public Task<IActionResult> GetAll(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] string? area,
        CancellationToken cancellationToken)
        => HandleAsync(() => _tableService.GetAllAsync(
            search,
            status,
            area,
            cancellationToken));

    [HttpGet("options")]
    public Task<IActionResult> GetOptions(CancellationToken cancellationToken)
        => HandleAsync(() => _tableService.GetOptionsAsync(cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var table = await _tableService.GetByIdAsync(id, cancellationToken);
        return table is null ? NotFound() : Ok(table);
    }

    [HttpPost]
    public Task<IActionResult> Create(
        [FromBody] AdminTableUpsertRequest request,
        CancellationToken cancellationToken)
        => HandleAsync(
            () => _tableService.CreateAsync(request, cancellationToken),
            table => CreatedAtAction(nameof(GetById), new { id = table.TableId }, table));

    [HttpPut("{id:int}")]
    public Task<IActionResult> Update(
        int id,
        [FromBody] AdminTableUpsertRequest request,
        CancellationToken cancellationToken)
        => HandleAsync(
            () => _tableService.UpdateAsync(id, request, cancellationToken),
            table => table is null ? NotFound() : Ok(table));

    [HttpDelete("{id:int}")]
    public Task<IActionResult> Delete(
        int id,
        CancellationToken cancellationToken)
        => HandleAsync(
            () => _tableService.DeleteAsync(id, cancellationToken),
            deleted => deleted ? NoContent() : NotFound());
}