using System;
using System.Collections.Generic;

namespace LoafNCatting.Persistence.Models;

public partial class Order
{
    public int OrderId { get; set; }

    public DateTime OrderDate { get; set; }

    public decimal TotalPrice { get; set; }

    public int? CustomerUserId { get; set; }

    public int? StaffUserId { get; set; }

    public int? TableId { get; set; }

    public int? ReservationId { get; set; }

    public string? OrderType { get; set; }

    public string? Note { get; set; }

    public int OrderStatusId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual User? CustomerUser { get; set; }

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

    public virtual OrderStatus OrderStatus { get; set; } = null!;

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual Reservation? Reservation { get; set; }

    public virtual User? StaffUser { get; set; }

    public virtual RestaurantTable? Table { get; set; }
}
