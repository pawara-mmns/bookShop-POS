using System;
using bookShop.Models;

namespace bookShop.Service;

public static class NotificationHub
{
    public static event EventHandler<Notification>? NotificationCreated;

    public static void Raise(Notification notification)
        => NotificationCreated?.Invoke(null, notification);
}
