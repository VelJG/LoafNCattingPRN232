namespace LoafNCatting.Application.Contracts;

public sealed class AdminProductDto
{
    public int ProductId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public decimal? DiscountPrice { get; set; }

    public int UnitInStock { get; set; }

    public string? Picture { get; set; }

    public string? PictureKey { get; set; }

    public int CategoryId { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    public bool IsAvailable { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}

public sealed class AdminProductUpsertRequest
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public decimal? DiscountPrice { get; set; }

    public int UnitInStock { get; set; }

    public string? Picture { get; set; }

    public int CategoryId { get; set; }

    public bool IsAvailable { get; set; } = true;
}
