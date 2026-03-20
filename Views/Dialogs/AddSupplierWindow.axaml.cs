using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using bookShop.Data;
using bookShop.Models;

namespace bookShop.Views.Dialogs;

public partial class AddSupplierWindow : Window
{
    public AddSupplierWindow()
    {
        InitializeComponent();
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }

    private void Add_Click(object? sender, RoutedEventArgs e)
    {
        var companyName = (this.FindControl<TextBox>("CompanyNameBox")?.Text ?? "").Trim();
        var contactName = (this.FindControl<TextBox>("ContactNameBox")?.Text ?? "").Trim();
        var email = (this.FindControl<TextBox>("EmailBox")?.Text ?? "").Trim();
        var phone = (this.FindControl<TextBox>("PhoneBox")?.Text ?? "").Trim();

        var error = this.FindControl<TextBlock>("ErrorText");
        if (error is not null) error.Text = "";

        if (string.IsNullOrWhiteSpace(companyName) || string.IsNullOrWhiteSpace(contactName) ||
            string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(phone))
        {
            if (error is not null)
                error.Text = "All fields are required.";
            return;
        }

        try
        {
            using var context = new AppDbContext();
            context.Suppliers.Add(new Suppliers
            {
                CompanyName = companyName,
                ContactName = contactName,
                Email = email,
                Phone = phone
            });

            context.SaveChanges();
            Close(true);
        }
        catch (Exception)
        {
            if (error is not null)
                error.Text = "Failed to add supplier. Please try again.";
        }
    }
}
