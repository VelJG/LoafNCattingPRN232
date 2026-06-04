using System;
using System.Collections.Generic;

namespace LoafNCatting.Persistence.Models;

public partial class Conversation
{
    public int ConversationId { get; set; }

    public int CustomerUserId { get; set; }

    public int? StaffUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual User CustomerUser { get; set; } = null!;

    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();

    public virtual User? StaffUser { get; set; }
}
