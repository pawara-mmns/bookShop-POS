using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using bookShop.Data;
using bookShop.Service;
using bookShop.Views.Dialogs;

namespace bookShop.Views.Pages;

public partial class CashiersPage : UserControl
{
    private List<CashierRow> _rows = new();

    public CashiersPage()
    {
        InitializeComponent();

        // Extra safety: only admin should see this page.
        if (!AuthorizationService.IsAdmin(SessionContext.CurrentUser))
        {
            var list = this.FindControl<ItemsControl>("CashiersList");
            if (list is not null)
                list.ItemsSource = Array.Empty<CashierRow>();
            return;
        }

        LoadCashiers();
    }

    private void LoadCashiers()
    {
        try
        {
            using var context = new AppDbContext();
            _rows = context.Users
                .Where(u => u.role == AuthorizationService.RoleCashier)
                .OrderBy(u => u.Id)
                .Select(u => new CashierRow
                {
                    Id = u.Id,
                    UserName = u.userName,
                    CanViewDashboard = u.canViewDashboard,
                    CanViewOrders = u.canViewOrders,
                    CanViewInventory = u.canViewInventory,
                    CanViewCustomers = u.canViewCustomers,
                    CanViewSuppliers = u.canViewSuppliers,
                    CanViewReports = u.canViewReports,
                    CanManageDiscountCards = u.canManageDiscountCards
                })
                .ToList();

            var list = this.FindControl<ItemsControl>("CashiersList");
            if (list is not null)
                list.ItemsSource = _rows;
        }
        catch (Exception)
        {
            var list = this.FindControl<ItemsControl>("CashiersList");
            if (list is not null)
                list.ItemsSource = Array.Empty<CashierRow>();
        }
    }

    private async void AddCashier_Click(object? sender, RoutedEventArgs e)
    {
        var owner = TopLevel.GetTopLevel(this) as Window;
        var dialog = new AddCashierWindow();

        if (owner is null)
        {
            dialog.Show();
            return;
        }

        bool added = await dialog.ShowDialog<bool>(owner);
        if (added)
            LoadCashiers();
    }

    private async void EditCashier_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button btn)
            return;

        if (btn.DataContext is not CashierRow row)
            return;

        var owner = TopLevel.GetTopLevel(this) as Window;
        var dialog = new EditCashierWindow(row.Id);

        if (owner is null)
        {
            dialog.Show();
            return;
        }

        bool saved = await dialog.ShowDialog<bool>(owner);
        if (saved)
            LoadCashiers();
    }

    private async void DeleteCashier_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button btn)
            return;

        if (btn.DataContext is not CashierRow row)
            return;

        var owner = TopLevel.GetTopLevel(this) as Window;
        var confirm = new ConfirmDeleteWindow("Delete Cashier", $"Are you sure you want to delete '{row.UserName}'?");

        if (owner is null)
        {
            confirm.Show();
            return;
        }

        bool shouldDelete = await confirm.ShowDialog<bool>(owner);
        if (!shouldDelete)
            return;

        try
        {
            using var context = new AppDbContext();
            var user = context.Users.FirstOrDefault(u => u.Id == row.Id && u.role == AuthorizationService.RoleCashier);
            if (user is null)
                return;

            context.Users.Remove(user);
            context.SaveChanges();
            LoadCashiers();
        }
        catch (Exception)
        {
            // minimal behavior
        }
    }
}

public class CashierRow
{
    public int Id { get; set; }
    public string UserName { get; set; } = "";

    public bool CanViewDashboard { get; set; }
    public bool CanViewOrders { get; set; }
    public bool CanViewInventory { get; set; }
    public bool CanViewCustomers { get; set; }
    public bool CanViewSuppliers { get; set; }
    public bool CanViewReports { get; set; }
    public bool CanManageDiscountCards { get; set; }

    public string AccessSummary
    {
        get
        {
            var parts = new List<string> { "POS" };
            if (CanViewDashboard) parts.Add("Dashboard");
            if (CanViewOrders) parts.Add("Orders");
            if (CanViewInventory) parts.Add("Inventory");
            if (CanViewCustomers) parts.Add("Customers");
            if (CanViewSuppliers) parts.Add("Suppliers");
            if (CanViewReports) parts.Add("Reports");
            if (CanManageDiscountCards) parts.Add("DiscountCards");
            return string.Join(" • ", parts);
        }
    }
}
