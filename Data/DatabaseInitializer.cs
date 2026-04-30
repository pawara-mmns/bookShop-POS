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
                password = "1234",
                role = "Admin",
                canUsePosBilling = true,
                canViewDashboard = true,
                canViewOrders = true,
                canViewInventory = true,
                canViewCustomers = true,
                canViewSuppliers = true,
                canViewReports = true,
                canManageDiscountCards = true
            });

            context.SaveChanges();
        }

        // Backfill after migrations for existing databases
        var admin = context.Users.FirstOrDefault(u => u.userName == "admin");
        if (admin is not null && admin.role != "Admin")
        {
            admin.role = "Admin";
            admin.canUsePosBilling = true;
            admin.canViewDashboard = true;
            admin.canViewOrders = true;
            admin.canViewInventory = true;
            admin.canViewCustomers = true;
            admin.canViewSuppliers = true;
            admin.canViewReports = true;
            admin.canManageDiscountCards = true;
            context.SaveChanges();
        }
    }
}
