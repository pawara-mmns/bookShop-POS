using bookShop.Models;

namespace bookShop.Service;

public static class SessionContext
{
    public static User? CurrentUser { get; private set; }

    public static void Set(User user)
    {
        CurrentUser = user;
    }

    public static void Clear()
    {
        CurrentUser = null;
    }
}
