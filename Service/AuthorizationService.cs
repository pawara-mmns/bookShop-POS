using bookShop.Models;

namespace bookShop.Service;

public static class AuthorizationService
{
    public const string RoleAdmin = "Admin";
    public const string RoleCashier = "Cashier";

    public static bool IsAdmin(User? user)
        => user is not null && user.role == RoleAdmin;

    public static bool CanAccessRoute(User? user, string route)
    {
        if (user is null)
            return false;

        if (IsAdmin(user))
            return true;

        // Cashier
        return route switch
        {
            "POS Billing" => user.canUsePosBilling,
            "Dashboard" => user.canViewDashboard,
            "Orders" => user.canViewOrders,
            "Book Inventory" => user.canViewInventory,
            "Customers" => user.canViewCustomers,
            "Suppliers" => user.canViewSuppliers,
            "Reports" => user.canViewReports,
            "Discount Cards" => user.canManageDiscountCards,
            "Cashiers" => false,
            _ => false
        };
    }

    public static string GetDefaultRoute(User? user)
    {
        if (user is null)
            return "Dashboard";

        if (IsAdmin(user))
            return "Dashboard";

        // Cashier default
        if (user.canUsePosBilling)
            return "POS Billing";

        // Fallback to first allowed feature
        if (user.canViewDashboard) return "Dashboard";
        if (user.canViewOrders) return "Orders";
        if (user.canViewInventory) return "Book Inventory";
        if (user.canViewCustomers) return "Customers";
        if (user.canViewSuppliers) return "Suppliers";
        if (user.canViewReports) return "Reports";
        if (user.canManageDiscountCards) return "Discount Cards";
        return "POS Billing";
    }
}
