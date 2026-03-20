using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using bookShop.Data;
using bookShop.Models;

namespace bookShop.Views.Dialogs;

public partial class EditSupplierWindow : Window
{
    private int _supplierId;

    public EditSupplierWindow()
    {
        InitializeComponent();
        global::bookShop.AppIcon.Apply(this);
        _supplierId = 0;
    }

    public EditSupplierWindow(Suppliers supplier)
    {
        InitializeComponent();

        global::bookShop.AppIcon.Apply(this);

        _supplierId = supplier.SupplierId;

        var supplierIdText = this.FindControl<TextBlock>("SupplierIdText");
        if (supplierIdText is not null)
            supplierIdText.Text = $"Supplier ID: SUP-{supplier.SupplierId:000}";

        var companyNameBox = this.FindControl<TextBox>("CompanyNameBox");
        var contactNameBox = this.FindControl<TextBox>("ContactNameBox");
        var emailBox = this.FindControl<TextBox>("EmailBox");
        var phoneBox = this.FindControl<TextBox>("PhoneBox");

        if (companyNameBox is not null) companyNameBox.Text = supplier.CompanyName;
        if (contactNameBox is not null) contactNameBox.Text = supplier.ContactName;
        if (emailBox is not null) emailBox.Text = supplier.Email;
        if (phoneBox is not null) phoneBox.Text = supplier.Phone;
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }

    private void Save_Click(object? sender, RoutedEventArgs e)
    {
        var companyName = (this.FindControl<TextBox>("CompanyNameBox")?.Text ?? "").Trim();
        var contactName = (this.FindControl<TextBox>("ContactNameBox")?.Text ?? "").Trim();
        var email = (this.FindControl<TextBox>("EmailBox")?.Text ?? "").Trim();
        var phone = (this.FindControl<TextBox>("PhoneBox")?.Text ?? "").Trim();

        var error = this.FindControl<TextBlock>("ErrorText");
        if (error is not null) error.Text = "";

        if (_supplierId <= 0 || string.IsNullOrWhiteSpace(companyName) || string.IsNullOrWhiteSpace(contactName) ||
            string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(phone))
        {
            if (error is not null)
                error.Text = "All fields are required.";
            return;
        }

        try
        {
            using var context = new AppDbContext();
            var existing = context.Suppliers.FirstOrDefault(s => s.SupplierId == _supplierId);
            if (existing is null)
            {
                if (error is not null)
                    error.Text = "Supplier not found.";
                return;
            }

            existing.CompanyName = companyName;
            existing.ContactName = contactName;
            existing.Email = email;
            existing.Phone = phone;

            context.SaveChanges();
            Close(true);
        }
        catch (Exception)
        {
            if (error is not null)
                error.Text = "Failed to save changes. Please try again.";
        }
    }
}
