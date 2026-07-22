using LoafNCatting.Application.Contracts;
using LoafNCatting.Entity.Models;

namespace LoafNCatting.Services.Mappers;

internal static class CafeDtoMapper
{
    public static CategoryDto ToCategoryDto(Category category)
        => new(category.CategoryId, category.Name, category.Description);

    public static ProductDto ToProductDto(Product product)
        => new(
            product.ProductId,
            product.Name,
            product.Description,
            product.Price,
            product.DiscountPrice,
            product.UnitInStock,
            product.Picture,
            product.CategoryId,
            product.Category.Name,
            product.IsAvailable,
            product.IsAvailable && product.UnitInStock > 0,
            product.Picture);

    public static CatDto ToCatDto(Cat cat)
        => new(
            cat.CatId,
            cat.Name,
            cat.Age,
            cat.Gender?.GenderName,
            cat.Breed,
            cat.Picture,
            cat.Description,
            cat.FriendlinessRating,
            cat.CutenessRating,
            cat.PlayfulnessRating,
            cat.Status.StatusName,
            cat.Picture);
}
