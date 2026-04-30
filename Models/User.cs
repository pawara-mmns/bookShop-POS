namespace bookShop.Models;
public class User
{
    public int Id { get; set; }
    public string userName { get; set; } = null!;
    public string password { get; set; } = null!;

    public bool isActive { get; set; } = true;

    // Roles: "Admin", "Cashier"
    public string role { get; set; } = "Cashier";

    // POS is the default feature for all cashiers
    public bool canUsePosBilling { get; set; } = true;

    // Admin can grant these features to cashiers
    public bool canViewDashboard { get; set; }
    public bool canViewOrders { get; set; }
    public bool canViewInventory { get; set; }
    public bool canViewCustomers { get; set; }
    public bool canViewSuppliers { get; set; }
    public bool canViewReports { get; set; }
    public bool canManageDiscountCards { get; set; }
}