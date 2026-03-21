using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Threading;
using bookShop.Data;
using Microsoft.EntityFrameworkCore;

namespace bookShop.Views.Pages;

public partial class DashboardPage : UserControl
{
    private DispatcherTimer? _clockTimer;

    public DashboardPage()
    {
        InitializeComponent();

        AttachedToLogicalTree += (_, _) =>
        {
            StartClock();
            RefreshDashboardData();
        };
        DetachedFromLogicalTree += (_, _) => StopClock();

        UpdateClockText();
    }

    private void StartClock()
    {
        if (_clockTimer is not null)
            return;

        _clockTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };

        _clockTimer.Tick += (_, _) => UpdateClockText();
        _clockTimer.Start();
    }

    private void StopClock()
    {
        if (_clockTimer is null)
            return;

        _clockTimer.Stop();
        _clockTimer = null;
    }

    private void UpdateClockText()
    {
        var text = this.FindControl<TextBlock>("LiveDateTimeText");
        if (text is null)
            return;

        var now = DateTime.Now;
        text.Text = now.ToString("ddd, MMM dd, yyyy • h:mm:ss tt", CultureInfo.InvariantCulture);
    }

    private static string FormatMoney(decimal amount)
        => $"LKR {amount.ToString("0.00", CultureInfo.InvariantCulture)}";

    private void RefreshDashboardData()
    {
        try
        {
            using var context = new AppDbContext();

            var nowLocal = DateTime.Now;
            var startLocal = nowLocal.Date;
            var startUtc = startLocal.ToUniversalTime();
            var endUtc = nowLocal.ToUniversalTime();

            var todayOrdersQuery = context.Orders
                .AsNoTracking()
                .Where(o => o.CreatedAt >= startUtc && o.CreatedAt <= endUtc);

            var todaySales = todayOrdersQuery.Sum(o => (decimal?)o.Total) ?? 0m;
            var todayOrderIds = todayOrdersQuery.Select(o => o.OrderId).ToList();
            var todayBooksSold = todayOrderIds.Count == 0
                ? 0
                : context.OrderDetails
                    .AsNoTracking()
                    .Where(d => todayOrderIds.Contains(d.OrderId))
                    .Sum(d => (int?)d.Quantity) ?? 0;

            var customersCount = context.Customers.AsNoTracking().Count();

            var lowStockBooks = context.Books
                .AsNoTracking()
                .Where(b => b.Stock < 8)
                .OrderBy(b => b.Stock)
                .ThenBy(b => b.Title)
                .Select(b => new LowStockRow(b.Title, b.ISBN, b.Stock))
                .ToList();

            var lowStockCount = lowStockBooks.Count;

            var recentSinceUtc = DateTime.UtcNow.AddDays(-1);
            var recentOrders = context.Orders
                .AsNoTracking()
                .Where(o => o.CreatedAt >= recentSinceUtc)
                .OrderByDescending(o => o.CreatedAt)
                .Take(5)
                .Select(o => new
                {
                    o.OrderId,
                    o.CreatedAt,
                    o.CustomerName,
                    o.Total,
                    o.PaymentMethod
                })
                .ToList();

            var recentRows = recentOrders
                .Select(o =>
                {
                    var localTime = DateTime.SpecifyKind(o.CreatedAt, DateTimeKind.Utc).ToLocalTime();
                    return new RecentSaleRow(
                        OrderId: o.OrderId.ToString(CultureInfo.InvariantCulture),
                        Time: localTime.ToString("h:mm tt", CultureInfo.InvariantCulture),
                        Customer: string.IsNullOrWhiteSpace(o.CustomerName) ? "Walk-in Customer" : o.CustomerName,
                        Total: FormatMoney(o.Total),
                        PaymentMethod: string.IsNullOrWhiteSpace(o.PaymentMethod) ? "-" : o.PaymentMethod);
                })
                .ToList();

            var todaySalesValue = this.FindControl<TextBlock>("TodaySalesValue");
            var booksSoldValue = this.FindControl<TextBlock>("BooksSoldValue");
            var activeCustomersValue = this.FindControl<TextBlock>("ActiveCustomersValue");
            var lowStockItemsValue = this.FindControl<TextBlock>("LowStockItemsValue");
            var lowStockList = this.FindControl<ItemsControl>("LowStockList");
            var recentSalesList = this.FindControl<ItemsControl>("RecentSalesList");

            if (todaySalesValue is not null) todaySalesValue.Text = FormatMoney(todaySales);
            if (booksSoldValue is not null) booksSoldValue.Text = todayBooksSold.ToString(CultureInfo.InvariantCulture);
            if (activeCustomersValue is not null) activeCustomersValue.Text = customersCount.ToString(CultureInfo.InvariantCulture);
            if (lowStockItemsValue is not null) lowStockItemsValue.Text = lowStockCount.ToString(CultureInfo.InvariantCulture);

            if (lowStockList is not null) lowStockList.ItemsSource = lowStockBooks;
            if (recentSalesList is not null) recentSalesList.ItemsSource = recentRows;
        }
        catch
        {
            var todaySalesValue = this.FindControl<TextBlock>("TodaySalesValue");
            var booksSoldValue = this.FindControl<TextBlock>("BooksSoldValue");
            var activeCustomersValue = this.FindControl<TextBlock>("ActiveCustomersValue");
            var lowStockItemsValue = this.FindControl<TextBlock>("LowStockItemsValue");
            var lowStockList = this.FindControl<ItemsControl>("LowStockList");
            var recentSalesList = this.FindControl<ItemsControl>("RecentSalesList");

            if (todaySalesValue is not null) todaySalesValue.Text = "LKR 0.00";
            if (booksSoldValue is not null) booksSoldValue.Text = "0";
            if (activeCustomersValue is not null) activeCustomersValue.Text = "0";
            if (lowStockItemsValue is not null) lowStockItemsValue.Text = "0";
            if (lowStockList is not null) lowStockList.ItemsSource = Array.Empty<LowStockRow>();
            if (recentSalesList is not null) recentSalesList.ItemsSource = Array.Empty<RecentSaleRow>();
        }
    }

    private sealed record LowStockRow(string Title, string ISBN, int Stock);

    private sealed record RecentSaleRow(string OrderId, string Time, string Customer, string Total, string PaymentMethod);
}
