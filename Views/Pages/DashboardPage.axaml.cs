using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using bookShop.Data;
using bookShop.Models;
using bookShop.Service;
using Microsoft.EntityFrameworkCore;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;

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
                        Date: localTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
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

    private sealed record RecentSaleRow(string OrderId, string Date, string Time, string Customer, string Total, string PaymentMethod);

    private async void GeneratePo_Click(object? sender, RoutedEventArgs e)
    {
        List<LowStockRow> items;

        try
        {
            using var context = new AppDbContext();
            items = context.Books
                .AsNoTracking()
                .Where(b => b.Stock < 8)
                .OrderBy(b => b.Stock)
                .ThenBy(b => b.Title)
                .Select(b => new LowStockRow(b.Title, b.ISBN, b.Stock))
                .ToList();
        }
        catch
        {
            items = new List<LowStockRow>();
        }

        if (items.Count == 0)
        {
            CreateAndRaiseNotification(new Notification
            {
                CreatedAt = DateTime.UtcNow,
                Type = "PurchaseOrder",
                Title = "Generate PO",
                Message = "No low-stock items found."
            });
            return;
        }

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.StorageProvider is not { } storage)
            return;

        var file = await storage.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Generate Purchase Order",
            SuggestedFileName = $"purchase-order-{DateTime.Now:yyyyMMdd-HHmm}.pdf",
            DefaultExtension = "pdf",
            FileTypeChoices = new[]
            {
                new FilePickerFileType("PDF") { Patterns = new[] { "*.pdf" } }
            }
        });

        if (file is null)
            return;

        try
        {
            await using var stream = await file.OpenWriteAsync();
            WritePurchaseOrderPdf(stream, items);

            CreateAndRaiseNotification(new Notification
            {
                CreatedAt = DateTime.UtcNow,
                Type = "PurchaseOrder",
                Title = "PO generated",
                Message = $"Purchase order exported ({items.Count} items)."
            });
        }
        catch
        {
            CreateAndRaiseNotification(new Notification
            {
                CreatedAt = DateTime.UtcNow,
                Type = "PurchaseOrder",
                Title = "PO export failed",
                Message = "Could not write the PDF file."
            });
        }
    }

    private static void WritePurchaseOrderPdf(Stream stream, List<LowStockRow> items)
    {
        const int targetStock = 10;

        static PdfPage AddA4Page(PdfDocument doc)
        {
            var p = doc.AddPage();
            p.Size = PdfSharpCore.PageSize.A4;
            return p;
        }

        var document = new PdfDocument();
        var page = AddA4Page(document);
        XGraphics gfx = XGraphics.FromPdfPage(page);

        try
        {
            var fontOptions = new XPdfFontOptions(PdfFontEncoding.Unicode);
            var titleFont = new XFont("DejaVu Sans", 18, XFontStyle.Bold, fontOptions);
            var subFont = new XFont("DejaVu Sans", 10, XFontStyle.Regular, fontOptions);
            var headerFont = new XFont("DejaVu Sans", 10, XFontStyle.Bold, fontOptions);
            var rowFont = new XFont("DejaVu Sans", 10, XFontStyle.Regular, fontOptions);

            double margin = 40;
            double x = margin;
            double y = 50;

            void DrawTableHeader()
            {
                gfx.DrawString("Title", headerFont, XBrushes.Black, new XPoint(x, y));
                gfx.DrawString("ISBN", headerFont, XBrushes.Black, new XPoint(x + 270, y));
                gfx.DrawString("Stock", headerFont, XBrushes.Black, new XPoint(x + 400, y));
                gfx.DrawString("Qty", headerFont, XBrushes.Black, new XPoint(x + 460, y));
                y += 12;
                gfx.DrawLine(XPens.LightGray, x, y, page.Width - margin, y);
                y += 14;
            }

            gfx.DrawString("Purchase Order (Low Stock)", titleFont, XBrushes.Black, new XPoint(x, y));
            y += 22;
            gfx.DrawString($"Generated: {DateTime.Now:ddd, MMM dd, yyyy • h:mm tt}", subFont, XBrushes.Gray, new XPoint(x, y));
            y += 26;

            DrawTableHeader();

            foreach (var item in items)
            {
                if (y > page.Height - 60)
                {
                    gfx.Dispose();
                    page = AddA4Page(document);
                    gfx = XGraphics.FromPdfPage(page);
                    y = 50;
                    DrawTableHeader();
                }

                var qty = Math.Max(0, targetStock - item.Stock);

                gfx.DrawString(TrimTo(item.Title, 40), rowFont, XBrushes.Black, new XPoint(x, y));
                gfx.DrawString(item.ISBN, rowFont, XBrushes.Black, new XPoint(x + 270, y));
                gfx.DrawString(item.Stock.ToString(CultureInfo.InvariantCulture), rowFont, XBrushes.Black, new XPoint(x + 400, y));
                gfx.DrawString(qty.ToString(CultureInfo.InvariantCulture), rowFont, XBrushes.Black, new XPoint(x + 460, y));

                y += 18;
            }
        }
        finally
        {
            gfx.Dispose();
        }

        document.Save(stream, closeStream: false);
    }

    private static string TrimTo(string value, int max)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= max)
            return value;

        return value.Substring(0, max - 1) + "…";
    }

    private static void CreateAndRaiseNotification(Notification notification)
    {
        try
        {
            using var context = new AppDbContext();
            context.Notifications.Add(notification);
            context.SaveChanges();
        }
        catch
        {
            // ignore persistence failure
        }

        NotificationHub.Raise(notification);
    }
}
