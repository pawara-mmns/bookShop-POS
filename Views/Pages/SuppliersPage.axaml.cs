using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using bookShop.Data;
using bookShop.Models;
using bookShop.Views.Dialogs;

namespace bookShop.Views.Pages;

public partial class SuppliersPage : UserControl
{
    private List<Suppliers> _suppliers = new();

    public SuppliersPage()
    {
        InitializeComponent();
        LoadSuppliers();
    }

    private void LoadSuppliers()
    {
        try
        {
            using var context = new AppDbContext();
            _suppliers = context.Suppliers
                .OrderBy(s => s.CompanyName)
                .ToList();

            var list = this.FindControl<ItemsControl>("SuppliersList");
            if (list is not null)
                list.ItemsSource = _suppliers;
        }
        catch (Exception)
        {
            var list = this.FindControl<ItemsControl>("SuppliersList");
            if (list is not null)
                list.ItemsSource = Array.Empty<Suppliers>();
        }
    }

    private async void AddSupplier_Click(object? sender, RoutedEventArgs e)
    {
        var owner = TopLevel.GetTopLevel(this) as Window;
        var dialog = new AddSupplierWindow();

        if (owner is null)
        {
            dialog.Show();
            return;
        }

        bool added = await dialog.ShowDialog<bool>(owner);
        if (added)
            LoadSuppliers();
    }

    private async void EditSupplier_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button)
            return;

        if (button.DataContext is not Suppliers supplier)
            return;

        var owner = TopLevel.GetTopLevel(this) as Window;
        var dialog = new EditSupplierWindow(supplier);

        if (owner is null)
        {
            dialog.Show();
            return;
        }

        bool saved = await dialog.ShowDialog<bool>(owner);
        if (saved)
            LoadSuppliers();
    }

    private async void DeleteSupplier_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button)
            return;

        if (button.DataContext is not Suppliers supplier)
            return;

        var owner = TopLevel.GetTopLevel(this) as Window;
        var confirm = new ConfirmDeleteWindow("Delete Supplier", $"Are you sure you want to delete '{supplier.CompanyName}'?");

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
            var existing = context.Suppliers.FirstOrDefault(s => s.SupplierId == supplier.SupplierId);
            if (existing is null)
                return;

            context.Suppliers.Remove(existing);
            context.SaveChanges();
            LoadSuppliers();
        }
        catch (Exception)
        {
            // minimal behavior
        }
    }
}
