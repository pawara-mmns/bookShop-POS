using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using bookShop.Data;
using Microsoft.EntityFrameworkCore;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;

namespace bookShop.Views.Pages;

public partial class ReportsPage : UserControl
{
    private ReportSnapshot? _snapshot;

    private sealed record RangeOption(string Key, string Display)
    {
        public override string ToString() => Display;
    }

    public ReportsPage()
    {
        InitializeComponent();

        var rangeCombo = this.FindControl<ComboBox>("RangeCombo");
        if (rangeCombo is not null)
        {
            rangeCombo.ItemsSource = new RangeOption[]
            {
                new("Last7Days", "Last 7 Days"),
                new("ThisMonth", "This Month"),
                new("ThisQuarter", "This Quarter"),
                new("YearToDate", "Year to Date")
            };
            rangeCombo.SelectedIndex = 0;
            rangeCombo.SelectionChanged += (_, _) => Refresh();
        }

        AttachedToLogicalTree += (_, _) => Refresh();
    }

    private static string FormatMoney(decimal amount)
        => $"LKR {amount.ToString("0.00", CultureInfo.InvariantCulture)}";

    private void Refresh()
    {
        var rangeCombo = this.FindControl<ComboBox>("RangeCombo");
        var option = rangeCombo?.SelectedItem as RangeOption;

        var nowLocal = DateTime.Now;
        var (label, startLocal) = GetRange(option?.Key, option?.Display, nowLocal);
        LoadReport(label, startLocal, nowLocal);
    }

    private static (string Label, DateTime StartLocal) GetRange(string? key, string? display, DateTime nowLocal)
    {
        key ??= "Last7Days";
        display ??= "Last 7 Days";

        DateTime startLocal = key switch
        {
            "ThisMonth" => new DateTime(nowLocal.Year, nowLocal.Month, 1),
            "ThisQuarter" =>
                new DateTime(nowLocal.Year, (((nowLocal.Month - 1) / 3) * 3) + 1, 1),
            "YearToDate" => new DateTime(nowLocal.Year, 1, 1),
            _ => nowLocal.Date.AddDays(-6)
        };

        return (display, startLocal);
    }

    private void LoadReport(string periodLabel, DateTime startLocal, DateTime nowLocal)
    {
        try
        {
            using var context = new AppDbContext();

            startLocal = startLocal.Date;
            var startUtc = startLocal.ToUniversalTime();
            var endUtc = nowLocal.ToUniversalTime();

            if (endUtc < startUtc)
                endUtc = startUtc;

            var duration = endUtc - startUtc;
            if (duration < TimeSpan.FromDays(1))
                duration = TimeSpan.FromDays(1);

            var prevStartUtc = startUtc - duration;
            var prevEndUtc = startUtc;

            var currentOrders = context.Orders
                .AsNoTracking()
                .Where(o => o.CreatedAt >= startUtc && o.CreatedAt <= endUtc)
                .Select(o => new
                {
                    o.OrderId,
                    o.CreatedAt,
                    o.Subtotal,
                    o.TaxAmount,
                    o.DiscountAmount,
                    o.Total
                })
                .ToList();

            var previousOrders = context.Orders
                .AsNoTracking()
                .Where(o => o.CreatedAt >= prevStartUtc && o.CreatedAt < prevEndUtc)
                .Select(o => new
                {
                    o.Subtotal,
                    o.TaxAmount,
                    o.DiscountAmount,
                    o.Total
                })
                .ToList();

            decimal currentGross = currentOrders.Sum(o => o.Total);
            decimal currentNet = currentOrders.Sum(o => o.Total - o.TaxAmount);
            int currentCount = currentOrders.Count;
            decimal currentAvg = currentCount == 0 ? 0m : Math.Round(currentGross / currentCount, 2, MidpointRounding.AwayFromZero);

            decimal prevGross = previousOrders.Sum(o => o.Total);
            decimal prevNet = previousOrders.Sum(o => o.Total - o.TaxAmount);
            int prevCount = previousOrders.Count;
            decimal prevAvg = prevCount == 0 ? 0m : Math.Round(prevGross / prevCount, 2, MidpointRounding.AwayFromZero);

            SetValue("GrossRevenueValue", FormatMoney(currentGross));
            SetValue("NetProfitValue", FormatMoney(currentNet));
            SetValue("TotalOrdersValue", currentCount.ToString(CultureInfo.InvariantCulture));
            SetValue("AvgOrderValueValue", FormatMoney(currentAvg));

            SetDelta("GrossRevenueDeltaPill", "GrossRevenueDeltaText", currentGross, prevGross);
            SetDelta("NetProfitDeltaPill", "NetProfitDeltaText", currentNet, prevNet);
            SetDelta("TotalOrdersDeltaPill", "TotalOrdersDeltaText", currentCount, prevCount);
            SetDelta("AvgOrderValueDeltaPill", "AvgOrderValueDeltaText", currentAvg, prevAvg);

            // Revenue trend (group by local date)
            var totalsByDay = currentOrders
                .GroupBy(o => DateTime.SpecifyKind(o.CreatedAt, DateTimeKind.Utc).ToLocalTime().Date)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Total));

            var trendEndLocal = nowLocal.Date;
            var trendStartLocal = trendEndLocal.AddDays(-6);
            if (trendStartLocal < startLocal.Date)
                trendStartLocal = startLocal.Date;

            var trendDays = (trendEndLocal - trendStartLocal).Days + 1;
            if (trendDays < 1)
                trendDays = 1;

            var trend = new List<TrendBarRow>(capacity: trendDays);
            for (int i = 0; i < trendDays; i++)
            {
                var day = trendStartLocal.AddDays(i);
                totalsByDay.TryGetValue(day, out var amount);
                trend.Add(new TrendBarRow(day.ToString("ddd", CultureInfo.InvariantCulture), amount));
            }

            decimal max = trend.Count == 0 ? 0m : trend.Max(t => t.Amount);
            foreach (var item in trend)
                item.RecomputeHeight(max);

            var trendBars = this.FindControl<ItemsControl>("TrendBars");
            if (trendBars is not null)
                trendBars.ItemsSource = trend;

            // Category breakdown
            var categoryRows = LoadCategoryRows(context, startUtc, endUtc);
            var categoryList = this.FindControl<ItemsControl>("CategoryList");
            if (categoryList is not null)
                categoryList.ItemsSource = categoryRows;

            // Top selling
            var topRows = LoadTopSellingRows(context, startUtc, endUtc);
            var topList = this.FindControl<ItemsControl>("TopBooksList");
            if (topList is not null)
                topList.ItemsSource = topRows;

            _snapshot = new ReportSnapshot(
                PeriodLabel: periodLabel,
                GrossRevenue: currentGross,
                NetProfit: currentNet,
                TotalOrders: currentCount,
                AvgOrderValue: currentAvg,
                Trend: trend,
                Categories: categoryRows,
                TopBooks: topRows);
        }
        catch
        {
            SetValue("GrossRevenueValue", "LKR 0.00");
            SetValue("NetProfitValue", "LKR 0.00");
            SetValue("TotalOrdersValue", "0");
            SetValue("AvgOrderValueValue", "LKR 0.00");

            SetDeltaUnknown("GrossRevenueDeltaPill", "GrossRevenueDeltaText");
            SetDeltaUnknown("NetProfitDeltaPill", "NetProfitDeltaText");
            SetDeltaUnknown("TotalOrdersDeltaPill", "TotalOrdersDeltaText");
            SetDeltaUnknown("AvgOrderValueDeltaPill", "AvgOrderValueDeltaText");

            var trendBars = this.FindControl<ItemsControl>("TrendBars");
            if (trendBars is not null)
                trendBars.ItemsSource = Array.Empty<TrendBarRow>();

            var categoryList = this.FindControl<ItemsControl>("CategoryList");
            if (categoryList is not null)
                categoryList.ItemsSource = Array.Empty<CategoryRow>();

            var topList = this.FindControl<ItemsControl>("TopBooksList");
            if (topList is not null)
                topList.ItemsSource = Array.Empty<TopBookRow>();

            _snapshot = null;
        }
    }

    private List<CategoryRow> LoadCategoryRows(AppDbContext context, DateTime startUtc, DateTime endUtc)
    {
        var rows = (from d in context.OrderDetails.AsNoTracking()
                    join o in context.Orders.AsNoTracking() on d.OrderId equals o.OrderId
                    join b in context.Books.AsNoTracking() on d.ISBN equals b.ISBN
                    where d.ISBN != null && o.CreatedAt >= startUtc && o.CreatedAt <= endUtc
                    group d by b.Category
            into g
                    select new
                    {
                        Category = g.Key,
                        Revenue = g.Sum(x => x.LineTotal)
                    })
            .ToList();

        var total = rows.Sum(r => r.Revenue);
        if (total <= 0)
            return new List<CategoryRow>();

        var colors = new[]
        {
            Color.Parse("#4F46E5"),
            Color.Parse("#3B82F6"),
            Color.Parse("#10B981"),
            Color.Parse("#EF4444")
        };

        int colorIndex = 0;
        return rows
            .OrderByDescending(r => r.Revenue)
            .Select(r =>
            {
                var pct = (int)Math.Round((double)(r.Revenue / total) * 100d, MidpointRounding.AwayFromZero);
                var color = colors[colorIndex++ % colors.Length];
                return new CategoryRow(r.Category, pct, new SolidColorBrush(color));
            })
            .ToList();
    }

    private List<TopBookRow> LoadTopSellingRows(AppDbContext context, DateTime startUtc, DateTime endUtc)
    {
        var rows = (from d in context.OrderDetails.AsNoTracking()
                    join o in context.Orders.AsNoTracking() on d.OrderId equals o.OrderId
                    join b in context.Books.AsNoTracking() on d.ISBN equals b.ISBN
                    where d.ISBN != null && o.CreatedAt >= startUtc && o.CreatedAt <= endUtc
                    group d by new { b.ISBN, b.Title, b.Author }
            into g
                    select new
                    {
                        g.Key.Title,
                        g.Key.Author,
                        UnitsSold = g.Sum(x => x.Quantity),
                        Revenue = g.Sum(x => x.LineTotal)
                    })
            .OrderByDescending(r => r.UnitsSold)
            .ThenByDescending(r => r.Revenue)
            .Take(10)
            .ToList();

        var result = new List<TopBookRow>(rows.Count);
        int rank = 1;
        foreach (var r in rows)
        {
            result.Add(new TopBookRow(
                Rank: $"#{rank}",
                Title: r.Title,
                Author: r.Author,
                UnitsSold: r.UnitsSold.ToString(CultureInfo.InvariantCulture),
                Revenue: FormatMoney(r.Revenue)));
            rank++;
        }

        return result;
    }

    private void SetValue(string name, string value)
    {
        var tb = this.FindControl<TextBlock>(name);
        if (tb is not null)
            tb.Text = value;
    }

    private void SetDelta(string pillName, string textName, decimal current, decimal previous)
        => SetDeltaInternal(pillName, textName, current, previous);

    private void SetDelta(string pillName, string textName, int current, int previous)
        => SetDeltaInternal(pillName, textName, current, previous);

    private void SetDeltaInternal(string pillName, string textName, decimal current, decimal previous)
    {
        var pill = this.FindControl<Border>(pillName);
        var text = this.FindControl<TextBlock>(textName);
        if (pill is null || text is null)
            return;

        if (previous == 0m)
        {
            pill.Background = new SolidColorBrush(Color.Parse("#F3F4F6"));
            text.Foreground = new SolidColorBrush(Color.Parse("#6B7280"));
            text.Text = "—";
            return;
        }

        var delta = (current - previous) / previous * 100m;
        var abs = Math.Abs(delta);
        var prefix = delta >= 0 ? "+" : "-";
        text.Text = $"{prefix}{abs.ToString("0.0", CultureInfo.InvariantCulture)}%";

        if (delta >= 0)
        {
            pill.Background = new SolidColorBrush(Color.Parse("#D1FAE5"));
            text.Foreground = new SolidColorBrush(Color.Parse("#10B981"));
        }
        else
        {
            pill.Background = new SolidColorBrush(Color.Parse("#FEE2E2"));
            text.Foreground = new SolidColorBrush(Color.Parse("#EF4444"));
        }
    }

    private void SetDeltaUnknown(string pillName, string textName)
    {
        var pill = this.FindControl<Border>(pillName);
        var text = this.FindControl<TextBlock>(textName);
        if (pill is null || text is null)
            return;

        pill.Background = new SolidColorBrush(Color.Parse("#F3F4F6"));
        text.Foreground = new SolidColorBrush(Color.Parse("#6B7280"));
        text.Text = "—";
    }

    private async void ExportPdf_Click(object? sender, RoutedEventArgs e)
    {
        if (_snapshot is null)
            return;

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.StorageProvider is not { } storage)
            return;

        var file = await storage.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export PDF",
            SuggestedFileName = "report.pdf",
            DefaultExtension = "pdf",
            FileTypeChoices = new[]
            {
                new FilePickerFileType("PDF") { Patterns = new[] { "*.pdf" } }
            }
        });

        if (file is null)
            return;

        await using var stream = await file.OpenWriteAsync();
        WritePdf(stream, _snapshot);
    }

    private void WritePdf(Stream stream, ReportSnapshot snapshot)
    {
        var document = new PdfDocument();
        var page = document.AddPage();
        page.Size = PdfSharpCore.PageSize.A4;

        using var gfx = XGraphics.FromPdfPage(page);

        const string fontFamily = "DejaVu Sans";
        var titleFont = new XFont(fontFamily, 18, XFontStyle.Bold);
        var headerFont = new XFont(fontFamily, 12, XFontStyle.Bold);
        var bodyFont = new XFont(fontFamily, 11, XFontStyle.Regular);

        double x = 40;
        double y = 40;

        gfx.DrawString("Analytics & Reports", titleFont, XBrushes.Black, new XPoint(x, y));
        y += 24;
        gfx.DrawString(snapshot.PeriodLabel, bodyFont, XBrushes.Gray, new XPoint(x, y));
        y += 22;

        gfx.DrawString($"Gross Revenue: {FormatMoney(snapshot.GrossRevenue)}", headerFont, XBrushes.Black, new XPoint(x, y));
        y += 18;
        gfx.DrawString($"Net Profit: {FormatMoney(snapshot.NetProfit)}", headerFont, XBrushes.Black, new XPoint(x, y));
        y += 18;
        gfx.DrawString($"Total Orders: {snapshot.TotalOrders}", headerFont, XBrushes.Black, new XPoint(x, y));
        y += 18;
        gfx.DrawString($"Avg. Order Value: {FormatMoney(snapshot.AvgOrderValue)}", headerFont, XBrushes.Black, new XPoint(x, y));
        y += 28;

        gfx.DrawString("Top Selling Books", headerFont, XBrushes.Black, new XPoint(x, y));
        y += 18;

        gfx.DrawString("Rank", headerFont, XBrushes.Black, new XPoint(x, y));
        gfx.DrawString("Title", headerFont, XBrushes.Black, new XPoint(x + 60, y));
        gfx.DrawString("Author", headerFont, XBrushes.Black, new XPoint(x + 300, y));
        gfx.DrawString("Units", headerFont, XBrushes.Black, new XPoint(x + 420, y));
        y += 14;

        foreach (var row in snapshot.TopBooks.Take(12))
        {
            gfx.DrawString(row.Rank, bodyFont, XBrushes.Gray, new XPoint(x, y));
            gfx.DrawString(row.Title, bodyFont, XBrushes.Black, new XPoint(x + 60, y));
            gfx.DrawString(row.Author, bodyFont, XBrushes.Gray, new XPoint(x + 300, y));
            gfx.DrawString(row.UnitsSold, bodyFont, XBrushes.Black, new XPoint(x + 420, y));
            y += 14;
            if (y > page.Height - 60)
                break;
        }

        document.Save(stream, closeStream: false);
    }

    private sealed record ReportSnapshot(
        string PeriodLabel,
        decimal GrossRevenue,
        decimal NetProfit,
        int TotalOrders,
        decimal AvgOrderValue,
        List<TrendBarRow> Trend,
        List<CategoryRow> Categories,
        List<TopBookRow> TopBooks);

    private sealed class TrendBarRow
    {
        public string DayLabel { get; }
        public decimal Amount { get; }
        public double BarHeight { get; private set; }

        public TrendBarRow(string dayLabel, decimal amount)
        {
            DayLabel = dayLabel;
            Amount = amount;
        }

        public void RecomputeHeight(decimal max)
        {
            const double minHeight = 22;
            const double maxHeight = 160;

            if (max <= 0m)
            {
                BarHeight = minHeight;
                return;
            }

            var pct = (double)(Amount / max);
            BarHeight = minHeight + (maxHeight - minHeight) * pct;
        }
    }

    private sealed record CategoryRow(string Category, int Percent, IBrush BarColor)
    {
        public string PercentText => $"{Percent.ToString(CultureInfo.InvariantCulture)}%";
    }

    private sealed record TopBookRow(string Rank, string Title, string Author, string UnitsSold, string Revenue);
}
