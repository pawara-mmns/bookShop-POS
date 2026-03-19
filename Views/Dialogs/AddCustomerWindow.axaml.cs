using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using bookShop.Data;
using bookShop.Models;

namespace bookShop.Views.Dialogs;

public partial class AddCustomerWindow : Window
{
    public AddCustomerWindow()
    {
        InitializeComponent();
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }

    private void Add_Click(object? sender, RoutedEventArgs e)
    {
        var name = (this.FindControl<TextBox>("NameBox")?.Text ?? "").Trim();
        var email = (this.FindControl<TextBox>("EmailBox")?.Text ?? "").Trim();
        var phone = (this.FindControl<TextBox>("PhoneBox")?.Text ?? "").Trim();
        var address = (this.FindControl<TextBox>("AddressBox")?.Text ?? "").Trim();

        var error = this.FindControl<TextBlock>("ErrorText");
        if (error is not null) error.Text = "";

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(phone) || string.IsNullOrWhiteSpace(address))
        {
            if (error is not null)
                error.Text = "All fields are required.";
            return;
        }

        try
        {
            using var context = new AppDbContext();
            context.Customers.Add(new Customers
            {
                Name = name,
                Email = email,
                Phone = phone,
                Address = address
            });

            context.SaveChanges();
            Close(true);
        }
        catch (Exception)
        {
            if (error is not null)
                error.Text = "Failed to add customer. Please try again.";
        }
    }
}
