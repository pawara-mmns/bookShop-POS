using System;
using System.ComponentModel.DataAnnotations;

namespace bookShop.Models;


public class Books
{
    [Key]
    public String ISBN { get; set; } = null!;

    public string Title { get; set; } = null!;

    public string Author { get; set; } = null!;

    public string Category { get; set; } = null!;

    public int Stock { get; set; }

    public decimal Price { get; set; }

}