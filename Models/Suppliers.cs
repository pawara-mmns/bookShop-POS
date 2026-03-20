using System.ComponentModel.DataAnnotations;

namespace bookShop.Models;

public class Suppliers
{
    [Key]
    public int SupplierId { get; set; }

    public string CompanyName { get; set; } = null!;

    public string ContactName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Phone { get; set; } = null!;
}
