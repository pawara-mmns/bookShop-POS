using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using bookShop.Data;
using bookShop.Models;
using bookShop.Service;

namespace bookShop.Views.Dialogs;

public partial class AddCashierWindow : Window
{
    public AddCashierWindow()
    {
        InitializeComponent();
        global::bookShop.AppIcon.Apply(this);
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }

    private void Add_Click(object? sender, RoutedEventArgs e)
    {
        var error = this.FindControl<TextBlock>("ErrorText");
        if (error is not null) error.Text = "";

        if (!AuthorizationService.IsAdmin(SessionContext.CurrentUser))
        {
            if (error is not null)
                error.Text = "Only admin can create cashiers.";
            return;
        }

        var username = (this.FindControl<TextBox>("UsernameBox")?.Text ?? "").Trim();
        var password = (this.FindControl<TextBox>("PasswordBox")?.Text ?? "").Trim();

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            if (error is not null)
                error.Text = "Username and password are required.";
            return;
        }

        try
        {
            using var context = new AppDbContext();

            var exists = context.Users.Any(u => u.userName == username);
            if (exists)
            {
                if (error is not null)
                    error.Text = "Username already exists.";
                return;
            }

            var user = new User
            {
                userName = username,
                password = password,
                role = AuthorizationService.RoleCashier,
                isActive = this.FindControl<CheckBox>("ActiveCheck")?.IsChecked ?? true,
                canUsePosBilling = true,
                canViewDashboard = this.FindControl<CheckBox>("DashboardCheck")?.IsChecked ?? false,
                canViewOrders = this.FindControl<CheckBox>("OrdersCheck")?.IsChecked ?? false,
                canViewInventory = this.FindControl<CheckBox>("InventoryCheck")?.IsChecked ?? false,
                canViewCustomers = this.FindControl<CheckBox>("CustomersCheck")?.IsChecked ?? false,
                canViewSuppliers = this.FindControl<CheckBox>("SuppliersCheck")?.IsChecked ?? false,
                canViewReports = this.FindControl<CheckBox>("ReportsCheck")?.IsChecked ?? false,
                canManageDiscountCards = this.FindControl<CheckBox>("DiscountCardsCheck")?.IsChecked ?? false
            };

            context.Users.Add(user);
            context.SaveChanges();
            Close(true);
        }
        catch (Exception)
        {
            if (error is not null)
                error.Text = "Failed to add cashier. Please try again.";
        }
    }
}
