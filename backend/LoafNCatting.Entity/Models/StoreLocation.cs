using System;
using System.Collections.Generic;

namespace LoafNCatting.Entity.Models;

public partial class StoreLocation
{
    public int StoreLocationId { get; set; }

    public string StoreName { get; set; } = null!;

    public string Address { get; set; } = null!;

    public string? PhoneNumber { get; set; }

    public string? OpeningHours { get; set; }

    public double Latitude { get; set; }

    public double Longitude { get; set; }
}
