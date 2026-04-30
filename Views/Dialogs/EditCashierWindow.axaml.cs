using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using bookShop.Data;
using bookShop.Service;

namespace bookShop.Views.Dialogs;

public partial class EditCashierWindow : Window
{
    private int _userId;

    public EditCashierWindow()
    {
        InitializeComponent();
        global::bookShop.AppIcon.Apply(this);
    }

    public EditCashierWindow(int userId)
        : this()
    {
        _userId = userId;
        LoadUser();
    }

    private void LoadUser()
    {
        var error = this.FindControl<TextBlock>("ErrorText");
        if (error is not null) error.Text = "";

        if (!AuthorizationService.IsAdmin(SessionContext.CurrentUser))
        {
            if (error is not null)
                error.Text = "Only admin can edit cashiers.";
            return;
        }

        try
        {
            using var context = new AppDbContext();
            var user = context.Users.FirstOrDefault(u => u.Id == _userId && u.role == AuthorizationService.RoleCashier);
            if (user is null)
            {
                if (error is not null)
                    error.Text = "Cashier not found.";
                return;
            }

            var usernameBox = this.FindControl<TextBox>("UsernameBox");
            if (usernameBox is not null)
                usernameBox.Text = user.userName;

            void SetCheck(string name, bool value)
            {
                var cb = this.FindControl<CheckBox>(name);
                if (cb is not null)
                    cb.IsChecked = value;
            }

            SetCheck("DashboardCheck", user.canViewDashboard);
            SetCheck("OrdersCheck", user.canViewOrders);
            SetCheck("InventoryCheck", user.canViewInventory);
            SetCheck("CustomersCheck", user.canViewCustomers);
            SetCheck("SuppliersCheck", user.canViewSuppliers);
            SetCheck("ReportsCheck", user.canViewReports);
            SetCheck("DiscountCardsCheck", user.canManageDiscountCards);
        }
        catch (Exception)
        {
            if (error is not null)
                error.Text = "Failed to load cashier.";
        }
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }

    private void Save_Click(object? sender, RoutedEventArgs e)
    {
        var error = this.FindControl<TextBlock>("ErrorText");
        if (error is not null) error.Text = "";

        if (!AuthorizationService.IsAdmin(SessionContext.CurrentUser))
        {
            if (error is not null)
                error.Text = "Only admin can edit cashiers.";
            return;
        }

        var newPassword = (this.FindControl<TextBox>("PasswordBox")?.Text ?? "").Trim();

        try
        {
            using var context = new AppDbContext();
            var user = context.Users.FirstOrDefault(u => u.Id == _userId && u.role == AuthorizationService.RoleCashier);
            if (user is null)
            {
                if (error is not null)
                    error.Text = "Cashier not found.";
                return;
            }

            if (!string.IsNullOrWhiteSpace(newPassword))
                user.password = newPassword;

            bool GetCheck(string name)
                => this.FindControl<CheckBox>(name)?.IsChecked ?? false;

            user.canUsePosBilling = true;
            user.canViewDashboard = GetCheck("DashboardCheck");
            user.canViewOrders = GetCheck("OrdersCheck");
            user.canViewInventory = GetCheck("InventoryCheck");
            user.canViewCustomers = GetCheck("CustomersCheck");
            user.canViewSuppliers = GetCheck("SuppliersCheck");
            user.canViewReports = GetCheck("ReportsCheck");
            user.canManageDiscountCards = GetCheck("DiscountCardsCheck");

            context.SaveChanges();
            Close(true);
        }
        catch (Exception)
        {
            if (error is not null)
                error.Text = "Failed to save cashier.";
        }
    }
}
