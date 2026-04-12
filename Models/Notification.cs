using System;
using System.ComponentModel.DataAnnotations;

namespace bookShop.Models;

public class Notification
{
    [Key]
    public int NotificationId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string Type { get; set; } = "Info";

    public string Title { get; set; } = "";

    public string Message { get; set; } = "";

    public bool IsRead { get; set; }

    public int? OrderId { get; set; }

    public string? ISBN { get; set; }
}
