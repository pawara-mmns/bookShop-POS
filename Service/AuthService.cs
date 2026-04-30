using System.Linq;
using bookShop.Data;
using bookShop.Models;

namespace bookShop.Service;

public class AuthService
{
    public User? Login(string username, string password)
    {
        using var context = new AppDbContext();

        var user = context.Users.FirstOrDefault(u => u.userName == username);
        if (user is null || user.password != password)
            return null;

        SessionContext.Set(user);
        return user;
    }
}