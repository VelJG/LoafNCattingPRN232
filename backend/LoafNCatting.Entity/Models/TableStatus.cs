using System;
using System.Collections.Generic;

namespace LoafNCatting.Entity.Models;

public partial class TableStatus
{
    public int TableStatusId { get; set; }

    public string StatusName { get; set; } = null!;

    public string? Description { get; set; }

    public virtual ICollection<RestaurantTable> RestaurantTables { get; set; } = new List<RestaurantTable>();
}
