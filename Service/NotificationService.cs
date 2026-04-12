using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using bookShop.Data;
using Microsoft.EntityFrameworkCore;

namespace bookShop.Service;

public static class NotificationService
{
    public static IReadOnlyList<NotificationListItem> GetLatest(int take = 20)
    {
        try
        {
            using var context = new AppDbContext();

            var items = context.Notifications
                .AsNoTracking()
                .OrderByDescending(n => n.CreatedAt)
                .Take(take)
                .Select(n => new
                {
                    n.NotificationId,
                    n.CreatedAt,
                    n.Title,
                    n.Message,
                    n.Type,
                    n.IsRead
                })
                .ToList();

            return items
                .Select(n => new NotificationListItem(
                    Id: n.NotificationId,
                    Title: n.Title,
                    Message: n.Message,
                    Time: DateTime.SpecifyKind(n.CreatedAt, DateTimeKind.Utc)
                        .ToLocalTime()
                        .ToString("ddd, MMM dd • h:mm tt", CultureInfo.InvariantCulture),
                    Type: n.Type,
                    IsRead: n.IsRead))
                .ToList();
        }
        catch
        {
            return Array.Empty<NotificationListItem>();
        }
    }
}

public sealed record NotificationListItem(int Id, string Title, string Message, string Time, string Type, bool IsRead);
