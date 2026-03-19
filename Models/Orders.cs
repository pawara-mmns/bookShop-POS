using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace bookShop.Models;

public class Orders
{
    [Key]
    public int OrderId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int? CustomerId { get; set; }

    public string CustomerName { get; set; } = "Walk-in Customer";

    public string PaymentMethod { get; set; } = "Card";

    public string? DiscountCode { get; set; }

    public decimal Subtotal { get; set; }

    public decimal TaxRate { get; set; } = 0.08m;

    public decimal TaxAmount { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal Total { get; set; }

    public List<OrderDetails> Details { get; set; } = new();
}
