using System.Linq;
using bookShop.Data;

namespace bookShop.Service;

public class AuthService
{
    public bool Login(string username, string password)
    {
        using var context = new AppDbContext();

        var user = context.Users.FirstOrDefault(u => u.userName == username);
        return user is not null && user.password == password;
    }
}