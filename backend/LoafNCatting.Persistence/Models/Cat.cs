using System;
using System.Collections.Generic;

namespace LoafNCatting.Persistence.Models;

public partial class Cat
{
    public int CatId { get; set; }

    public string Name { get; set; } = null!;

    public int? Age { get; set; }

    public int? GenderId { get; set; }

    public string? Breed { get; set; }

    public string? Picture { get; set; }

    public string? Description { get; set; }

    public int? FriendlinessRating { get; set; }

    public int? CutenessRating { get; set; }

    public int? PlayfulnessRating { get; set; }

    public int StatusId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Gender? Gender { get; set; }

    public virtual CatStatus Status { get; set; } = null!;
}
