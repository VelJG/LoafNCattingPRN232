namespace LoafNCatting.Application.Contracts;

public sealed record CategoryDto(int CategoryId, string Name, string? Description);

public sealed record ProductDto(
    int ProductId,
    string Name,
    string? Description,
    decimal Price,
    decimal? DiscountPrice,
    int UnitInStock,
    string? Picture,
    int CategoryId,
    string CategoryName,
    bool IsAvailable,
    bool CanOrder,
    string? PictureKey = null);

public sealed record CatDto(
    int CatId,
    string Name,
    int? Age,
    string? GenderName,
    string? Breed,
    string? Picture,
    string? Description,
    int? FriendlinessRating,
    int? CutenessRating,
    int? PlayfulnessRating,
    string StatusName,
    string? PictureKey = null);
