using LoafNCatting.Application.Contracts;
using LoafNCatting.Application.Interfaces.Services;
using LoafNCatting.WebApi.Filters;
using Microsoft.AspNetCore.Mvc;

namespace LoafNCatting.WebApi.Controllers;

[ApiController]
[AdminOnly]
[Route("api/admin/products")]
public sealed class AdminProductsController : ControllerBase
{
    private readonly IAdminProductService _productService;

    public AdminProductsController(IAdminProductService productService)
    {
        _productService = productService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AdminProductDto>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] int? categoryId,
        [FromQuery] bool? isAvailable)
        => Ok(await _productService.GetAllAsync(search, categoryId, isAvailable));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AdminProductDto>> GetById(int id)
    {
        var product = await _productService.GetByIdAsync(id);
        return product is null ? NotFound() : Ok(product);
    }

    [HttpPost]
    public async Task<ActionResult<AdminProductDto>> Create(AdminProductUpsertRequest request)
    {
        try
        {
            var product = await _productService.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = product.ProductId }, product);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<AdminProductDto>> Update(int id, AdminProductUpsertRequest request)
    {
        try
        {
            var product = await _productService.UpdateAsync(id, request);
            return product is null ? NotFound() : Ok(product);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
        => await _productService.DeleteAsync(id) ? NoContent() : NotFound();
}

