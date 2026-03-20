using Avalonia.Controls;
using Avalonia.Input;
using bookShop.Views.Pages;

namespace bookShop.Views;

public partial class DashboardWindow : Window
{
    public DashboardWindow()
    {
        InitializeComponent();

        NavigateTo("Dashboard");
    }

    private void NavItem_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control control)
            return;

        var tag = control.Tag?.ToString();
        if (string.IsNullOrWhiteSpace(tag))
            return;

        NavigateTo(tag);
    }

    private void NavigateTo(string route)
    {
        var host = this.FindControl<ContentControl>("PageHost");
        if (host is null)
            return;

        host.Content = route switch
        {
            "Dashboard" => new DashboardPage(),
                "Orders" => new OrdersPage(),
            "Customers" => new CustomersPage(),
            "Suppliers" => new SuppliersPage(),
            "POS Billing" => new PosBillingPage(),
            "Book Inventory" => new BookInventoryPage(),
            _ => host.Content
        };

        var header = this.FindControl<TextBlock>("HeaderTitle");
        if (header is not null)
            header.Text = route;

        SetSelectedNav(route);
    }

    private void SetSelectedNav(string route)
    {
        var navDashboard = this.FindControl<Border>("NavDashboard");
        var navPos = this.FindControl<Border>("NavPos");
        var navInventory = this.FindControl<Border>("NavInventory");
        var navCustomers = this.FindControl<Border>("NavCustomers");
        var navSuppliers = this.FindControl<Border>("NavSuppliers");
            var navOrders = this.FindControl<Border>("NavOrders");

        if (navDashboard is not null) navDashboard.Classes.Set("selected", route == "Dashboard");
            if (navOrders is not null) navOrders.Classes.Set("selected", route == "Orders");
        if (navPos is not null) navPos.Classes.Set("selected", route == "POS Billing");
        if (navInventory is not null) navInventory.Classes.Set("selected", route == "Book Inventory");
        if (navCustomers is not null) navCustomers.Classes.Set("selected", route == "Customers");
        if (navSuppliers is not null) navSuppliers.Classes.Set("selected", route == "Suppliers");
    }

    private void SignOut_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var login = new global::bookShop.MainWindow();
        login.Show();
        Close();
    }
}