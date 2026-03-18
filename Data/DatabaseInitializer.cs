using System.Linq;
using Microsoft.EntityFrameworkCore;
using bookShop.Models;

namespace bookShop.Data;

public static class DatabaseInitializer
{
    public static void Initialize()
    {
        using var context = new AppDbContext();

        context.Database.Migrate();

        if (!context.Users.Any())
        {
            context.Users.Add(new User
            {
                userName = "admin",
                password = "1234"
            });

            context.SaveChanges();
        }
    }
}
