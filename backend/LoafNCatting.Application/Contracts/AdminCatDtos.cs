namespace LoafNCatting.Application.Contracts;

public sealed class AdminCatDto
{
    public int CatId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? Age { get; set; }
    public string? Gender { get; set; }
    public string? Breed { get; set; }
    public string? Picture { get; set; }
    public string? Description { get; set; }
    public int? FriendlinessRating { get; set; }
    public int? CutenessRating { get; set; }
    public int? PlayfulnessRating { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public sealed class AdminCatUpsertRequest
{
    public string Name { get; set; } = string.Empty;
    public int? Age { get; set; }
    public string? Gender { get; set; }
    public string? Breed { get; set; }
    public string? Picture { get; set; }
    public string? Description { get; set; }
    public int? FriendlinessRating { get; set; }
    public int? CutenessRating { get; set; }
    public int? PlayfulnessRating { get; set; }
    public string Status { get; set; } = string.Empty;
}

public sealed class AdminCatOptionsDto
{
    public IReadOnlyList<string> Statuses { get; set; } = [];
    public IReadOnlyList<string> Genders { get; set; } = [];
}
