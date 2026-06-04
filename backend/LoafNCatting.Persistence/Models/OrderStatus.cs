using System;
using System.Collections.Generic;

namespace LoafNCatting.Persistence.Models;

public partial class OrderStatus
{
    public int OrderStatusId { get; set; }

    public string OrderStatusName { get; set; } = null!;

    public string? Description { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
