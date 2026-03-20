using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Controls;
using bookShop.Data;
using bookShop.Models;

namespace bookShop.Views.Pages;

public partial class OrdersPage : UserControl
{
    private List<OrderRow> _rows = new();

    public OrdersPage()
    {
        InitializeComponent();
        LoadOrders();
    }

    private void LoadOrders()
    {
        try
        {
            using var context = new AppDbContext();

            _rows = context.Orders
                .OrderByDescending(o => o.OrderId)
                .Select(o => new OrderRow
                {
                    OrderId = o.OrderId,
                    CustomerId = o.CustomerId,
                    CreatedAt = o.CreatedAt,
                    PaymentMethod = o.PaymentMethod,
                    Subtotal = o.Subtotal
                })
                .ToList();

            var list = this.FindControl<ItemsControl>("OrdersList");
            if (list is not null)
                list.ItemsSource = _rows;
        }
        catch (Exception)
        {
            var list = this.FindControl<ItemsControl>("OrdersList");
            if (list is not null)
                list.ItemsSource = Array.Empty<OrderRow>();
        }
    }
}

public class OrderRow
{
    public int OrderId { get; set; }
    public int? CustomerId { get; set; }
    public DateTime CreatedAt { get; set; }
    public string PaymentMethod { get; set; } = "";
    public decimal Subtotal { get; set; }

    public string DisplayOrderId => $"OR-{OrderId:000}";

    public string DisplayCustomerId => CustomerId is null ? "-" : CustomerId.Value.ToString(CultureInfo.InvariantCulture);

    public string DisplayDate
    {
        get
        {
            // stored in UTC; display as local date
            var local = DateTime.SpecifyKind(CreatedAt, DateTimeKind.Utc).ToLocalTime();
            return local.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }
    }

    public string DisplaySubtotal => $"LKR {Subtotal.ToString("0.00", CultureInfo.InvariantCulture)}";
}
