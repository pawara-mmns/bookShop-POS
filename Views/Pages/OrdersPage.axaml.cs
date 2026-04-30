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
                    Subtotal = o.Subtotal,
                    DiscountAmount = o.DiscountAmount
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
    public decimal DiscountAmount { get; set; }

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

    private decimal NetSubtotal
    {
        get
        {
            var net = Math.Round(Subtotal - DiscountAmount, 2, MidpointRounding.AwayFromZero);
            return net < 0 ? 0m : net;
        }
    }

    public string DisplayDiscount => $"LKR {DiscountAmount.ToString("0.00", CultureInfo.InvariantCulture)}";

    // Requirement: show subtotal after discount in the table
    public string DisplaySubtotal => $"LKR {NetSubtotal.ToString("0.00", CultureInfo.InvariantCulture)}";
}
