using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace bookShop.Models;

public enum DiscountCardType
{
    Percentage = 0,
    FixedAmount = 1
}

public class DiscountCards
{
    [Key]
    public int DiscountCardId { get; set; }

    public string Code { get; set; } = "";

    public DiscountCardType Type { get; set; } = DiscountCardType.Percentage;

    public decimal Value { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ExpiresAt { get; set; }

    [NotMapped]
    public bool IsExpired => ExpiresAt.HasValue && ExpiresAt.Value <= DateTime.UtcNow;
}
