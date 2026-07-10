using System;
using System.Collections.Generic;

namespace LoafNCatting.Entity.Models;

public partial class Reservation
{
    public int ReservationId { get; set; }

    public int? UserId { get; set; }

    public DateOnly Date { get; set; }

    public TimeOnly Time { get; set; }

    public string GuestName { get; set; } = null!;

    public string GuestPhoneNumber { get; set; } = null!;

    public int NumberOfGuests { get; set; }

    public string? Note { get; set; }

    public int StatusId { get; set; }

    public int TableId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ReservationStatus Status { get; set; } = null!;

    public virtual RestaurantTable Table { get; set; } = null!;

    public virtual User? User { get; set; }
}
