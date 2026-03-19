using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using bookShop.Data;
using bookShop.Models;

namespace bookShop.Views.Pages;

public partial class CustomersPage : UserControl
{
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
            List<Customers> customers = context.Customers
                .OrderBy(c => c.CustomerId)
                .ToList();

            var list = this.FindControl<ItemsControl>("CustomersList");
            if (list is not null)
                list.ItemsSource = customers;
        }
        catch (Exception)
        {
            var list = this.FindControl<ItemsControl>("CustomersList");
            if (list is not null)
                list.ItemsSource = Array.Empty<Customers>();
        }
    }
}
