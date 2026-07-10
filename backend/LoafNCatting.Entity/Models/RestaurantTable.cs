using System;
using System.Collections.Generic;

namespace LoafNCatting.Entity.Models;

public partial class RestaurantTable
{
    public int TableId { get; set; }

    public string TableName { get; set; } = null!;

    public int Capacity { get; set; }

    public string? Area { get; set; }

    public string? Description { get; set; }

    public int TableStatusId { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();

    public virtual TableStatus TableStatus { get; set; } = null!;
}
