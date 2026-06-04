using System;
using System.Collections.Generic;

namespace LoafNCatting.Persistence.Models;

public partial class Payment
{
    public int PaymentId { get; set; }

    public decimal PaymentAmount { get; set; }

    public int MethodId { get; set; }

    public string PaymentStatus { get; set; } = null!;

    public string? TransactionCode { get; set; }

    public DateTime PaymentDate { get; set; }

    public DateTime? PaidAt { get; set; }

    public int OrderId { get; set; }

    public virtual PaymentMethod Method { get; set; } = null!;

    public virtual Order Order { get; set; } = null!;
}
