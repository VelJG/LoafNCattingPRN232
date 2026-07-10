using System;
using System.Collections.Generic;

namespace LoafNCatting.Entity.Models;

public partial class User
{
    public int UserId { get; set; }

    public string Name { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Password { get; set; } = null!;

    public string PhoneNumber { get; set; } = null!;

    public string? Address { get; set; }

    public string? AvatarUrl { get; set; }

    public int RoleId { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool IsEmailVerified { get; set; }

    public string? EmailVerificationOtpHash { get; set; }

    public DateTime? EmailVerificationOtpExpiresAt { get; set; }

    public virtual ICollection<Cart> Carts { get; set; } = new List<Cart>();

    public virtual ICollection<Conversation> ConversationCustomerUsers { get; set; } = new List<Conversation>();

    public virtual ICollection<Conversation> ConversationStaffUsers { get; set; } = new List<Conversation>();

    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual ICollection<Order> OrderCustomerUsers { get; set; } = new List<Order>();

    public virtual ICollection<Order> OrderStaffUsers { get; set; } = new List<Order>();

    public virtual ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();

    public virtual Role Role { get; set; } = null!;
}
