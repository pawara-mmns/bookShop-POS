using System.Linq;
using bookShop.Data;
using bookShop.Models;

namespace bookShop.Service;

public class AuthService
{
    public bool TryLogin(string username, string password, out User? user, out string error)
    {
        using var context = new AppDbContext();

        user = context.Users.FirstOrDefault(u => u.userName == username);
        if (user is null)
        {
            error = "Invalid username or password.";
            return false;
        }

        if (!user.isActive)
        {
            error = "This account is disabled.";
            return false;
        }

        if (user.password != password)
        {
            error = "Invalid username or password.";
            return false;
        }

        SessionContext.Set(user);
        error = "";
        return true;
    }
}