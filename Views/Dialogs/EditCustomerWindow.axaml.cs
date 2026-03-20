using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using bookShop.Data;
using bookShop.Models;

namespace bookShop.Views.Dialogs;

public partial class EditCustomerWindow : Window
{
    private int _customerId;

    public EditCustomerWindow()
    {
        InitializeComponent();
        global::bookShop.AppIcon.Apply(this);
        _customerId = 0;
    }

    public EditCustomerWindow(Customers customer)
    {
        InitializeComponent();

        _customerId = customer.CustomerId;

        var customerIdText = this.FindControl<TextBlock>("CustomerIdText");
        if (customerIdText is not null)
            customerIdText.Text = $"Customer ID: {customer.CustomerId}";

        var nameBox = this.FindControl<TextBox>("NameBox");
        var emailBox = this.FindControl<TextBox>("EmailBox");
        var phoneBox = this.FindControl<TextBox>("PhoneBox");
        var addressBox = this.FindControl<TextBox>("AddressBox");

        if (nameBox is not null) nameBox.Text = customer.Name;
        if (emailBox is not null) emailBox.Text = customer.Email;
        if (phoneBox is not null) phoneBox.Text = customer.Phone;
        if (addressBox is not null) addressBox.Text = customer.Address;
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }

    private void Save_Click(object? sender, RoutedEventArgs e)
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
            var customer = context.Customers.FirstOrDefault(c => c.CustomerId == _customerId);
            if (customer is null)
            {
                if (error is not null)
                    error.Text = "Customer not found.";
                return;
            }

            customer.Name = name;
            customer.Email = email;
            customer.Phone = phone;
            customer.Address = address;

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
