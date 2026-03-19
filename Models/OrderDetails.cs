using System.ComponentModel.DataAnnotations;

namespace bookShop.Models;

public class OrderDetails
{
    [Key]
    public int OrderDetailId { get; set; }

    public int OrderId { get; set; }

    public Orders? Order { get; set; }

    public string? ISBN { get; set; }

    public string ItemName { get; set; } = null!;

    public decimal UnitPrice { get; set; }

    public int Quantity { get; set; }

    public decimal LineTotal { get; set; }
}
