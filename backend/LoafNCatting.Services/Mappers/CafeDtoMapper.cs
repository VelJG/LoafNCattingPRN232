using LoafNCatting.Application.Contracts;
using LoafNCatting.Application.Interfaces.Services;
using LoafNCatting.Entity.Models;

namespace LoafNCatting.Services.Mappers;

internal static class CafeDtoMapper
{
    public static CategoryDto ToCategoryDto(Category category)
        => new(category.CategoryId, category.Name, category.Description);

    public static ProductDto ToProductDto(Product product, IMediaStorageService mediaStorage)
        => new(
            product.ProductId,
            product.Name,
            product.Description,
            product.Price,
            product.DiscountPrice,
            product.UnitInStock,
            mediaStorage.ResolveDisplayUrl(product.Picture),
            product.CategoryId,
            product.Category.Name,
            product.IsAvailable,
            product.IsAvailable && product.UnitInStock > 0,
            mediaStorage.NormalizeStoredKey(product.Picture));

    public static CatDto ToCatDto(Cat cat, IMediaStorageService mediaStorage)
        => new(
            cat.CatId,
            cat.Name,
            cat.Age,
            cat.Gender?.GenderName,
            cat.Breed,
            mediaStorage.ResolveDisplayUrl(cat.Picture),
            cat.Description,
            cat.FriendlinessRating,
            cat.CutenessRating,
            cat.PlayfulnessRating,
            cat.Status.StatusName,
            mediaStorage.NormalizeStoredKey(cat.Picture));
}
