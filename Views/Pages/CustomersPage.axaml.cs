using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using bookShop.Data;
using bookShop.Models;
using bookShop.Views.Dialogs;

namespace bookShop.Views.Pages;

public partial class CustomersPage : UserControl
{
    private List<Customers> _customers = new();

    public CustomersPage()
    {
        InitializeComponent();
        LoadCustomers();
    }

    private void LoadCustomers()
    {
        try
        {
            using var context = new AppDbContext();
            _customers = context.Customers
                .OrderBy(c => c.CustomerId)
                .ToList();

            var list = this.FindControl<ItemsControl>("CustomersList");
            if (list is not null)
                list.ItemsSource = _customers;
        }
        catch (Exception)
        {
            var list = this.FindControl<ItemsControl>("CustomersList");
            if (list is not null)
                list.ItemsSource = Array.Empty<Customers>();
        }
    }

    private async void EditCustomer_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button)
            return;

        if (button.DataContext is not Customers customer)
            return;

        var owner = TopLevel.GetTopLevel(this) as Window;
        var dialog = new EditCustomerWindow(customer);

        if (owner is null)
        {
            dialog.Show();
            return;
        }

        bool saved = await dialog.ShowDialog<bool>(owner);

        if (saved)
            LoadCustomers();
    }

    private async void DeleteCustomer_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button)
            return;

        if (button.DataContext is not Customers customer)
            return;

        var owner = TopLevel.GetTopLevel(this) as Window;
        var confirm = new ConfirmDeleteWindow($"Are you sure you want to delete '{customer.Name}'?");

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
            var existing = context.Customers.FirstOrDefault(c => c.CustomerId == customer.CustomerId);
            if (existing is null)
                return;

            context.Customers.Remove(existing);
            context.SaveChanges();
            LoadCustomers();
        }
        catch (Exception)
        {
            // Minimal behavior: silently ignore for now
        }
    }
}
