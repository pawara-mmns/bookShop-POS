using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using bookShop.Data;
using bookShop.Models;
using bookShop.Service;
using bookShop.Views.Dialogs;
using Microsoft.EntityFrameworkCore;

namespace bookShop.Views.Pages;

public partial class PosBillingPage : UserControl
{
    private const decimal DefaultTaxRate = 0.08m;

    private readonly ObservableCollection<PosLineItem> _orderLines = new();
    private readonly List<CustomerOption> _customerOptions = new();
    private List<Books> _allBooks = new();
    private CustomerOption? _selectedCustomer;

    private string _paymentMethod = "Card";
    private string? _discountCode;
    private decimal _discountAmount;

    public PosBillingPage()
    {
        InitializeComponent();

        var orderItemsList = this.FindControl<ItemsControl>("OrderItemsList");
        if (orderItemsList is not null)
            orderItemsList.ItemsSource = _orderLines;

        LoadBooks();
        LoadCustomers();
        PopulateCustomers();
        UpdatePaymentButtons();
        RefreshTotals();
    }

    private void LoadBooks()
    {
        try
        {
            using var context = new AppDbContext();
            _allBooks = context.Books
                .OrderBy(b => b.Title)
                .ToList();
        }
        catch (Exception)
        {
            _allBooks = new List<Books>();
        }
    }

    private void LoadCustomers()
    {
        _customerOptions.Clear();
        _customerOptions.Add(new CustomerOption(null, "Walk-in Customer"));

        try
        {
            using var context = new AppDbContext();
            var customers = context.Customers
                .OrderBy(c => c.Name)
                .Select(c => new CustomerOption(c.CustomerId, c.Name))
                .ToList();

            _customerOptions.AddRange(customers);
        }
        catch (Exception)
        {
            // keep walk-in only
        }
    }

    private void PopulateCustomers()
    {
        var combo = this.FindControl<ComboBox>("CustomerCombo");
        if (combo is null)
            return;

        combo.ItemsSource = _customerOptions;
        combo.DisplayMemberBinding = new Avalonia.Data.Binding("DisplayName");
        combo.SelectedIndex = 0;
        _selectedCustomer = _customerOptions.FirstOrDefault();
    }

    private void SetError(string message)
    {
        var error = this.FindControl<TextBlock>("ErrorText");
        if (error is not null)
            error.Text = message;
    }

    private void ClearError()
    {
        SetError("");
    }

    private static string FormatMoney(decimal amount)
        => $"LKR {amount.ToString("0.00", CultureInfo.InvariantCulture)}";

    private static string FormatDiscount(decimal amount)
        => $"-LKR {amount.ToString("0.00", CultureInfo.InvariantCulture)}";

    private void RefreshTotals()
    {
        var subtotal = _orderLines.Sum(l => l.LineTotal);
        var taxAmount = Math.Round(subtotal * DefaultTaxRate, 2, MidpointRounding.AwayFromZero);
        var total = Math.Round(subtotal + taxAmount - _discountAmount, 2, MidpointRounding.AwayFromZero);

        var itemsCount = _orderLines.Sum(l => l.Quantity);

        var subtotalText = this.FindControl<TextBlock>("SubtotalText");
        var taxText = this.FindControl<TextBlock>("TaxText");
        var discountText = this.FindControl<TextBlock>("DiscountText");
        var totalText = this.FindControl<TextBlock>("TotalText");
        var itemsCountText = this.FindControl<TextBlock>("ItemsCountText");
        var taxLabel = this.FindControl<TextBlock>("TaxLabel");

        if (subtotalText is not null) subtotalText.Text = FormatMoney(subtotal);
        if (taxText is not null) taxText.Text = FormatMoney(taxAmount);
        if (discountText is not null) discountText.Text = FormatDiscount(_discountAmount);
        if (totalText is not null) totalText.Text = FormatMoney(total);
        if (itemsCountText is not null) itemsCountText.Text = $"{itemsCount} items";
        if (taxLabel is not null) taxLabel.Text = $"Tax ({DefaultTaxRate * 100:0}%)";
    }

    private void SearchBox_TextChanged(object? sender, TextChangedEventArgs e)
    {
        var searchBox = this.FindControl<TextBox>("SearchBox");
        var container = this.FindControl<Border>("SearchResultsContainer");
        var list = this.FindControl<ItemsControl>("SearchResultsList");

        if (searchBox is null || container is null || list is null)
            return;

        var q = (searchBox.Text ?? "").Trim();
        if (string.IsNullOrWhiteSpace(q))
        {
            container.IsVisible = false;
            list.ItemsSource = Array.Empty<Books>();
            return;
        }

        var results = _allBooks
            .Where(b =>
                b.ISBN.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                b.Title.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                b.Author.Contains(q, StringComparison.OrdinalIgnoreCase))
            .Take(6)
            .ToList();

        list.ItemsSource = results;
        container.IsVisible = results.Count > 0;
    }

    private void SearchBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
            return;

        var searchBox = this.FindControl<TextBox>("SearchBox");
        if (searchBox is null)
            return;

        var q = (searchBox.Text ?? "").Trim();
        if (string.IsNullOrWhiteSpace(q))
            return;

        var match = _allBooks.FirstOrDefault(b =>
            b.ISBN.Equals(q, StringComparison.OrdinalIgnoreCase) ||
            b.Title.Equals(q, StringComparison.OrdinalIgnoreCase));

        if (match is not null)
            AddBookToOrder(match);
    }

    private void AddBookToOrder_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button)
            return;

        if (button.DataContext is not Books book)
            return;

        AddBookToOrder(book);
    }

    private void AddBookToOrder(Books book)
    {
        ClearError();

        if (book.Stock <= 0)
        {
            SetError("This book is out of stock.");
            return;
        }

        var existing = _orderLines.FirstOrDefault(l => l.ISBN == book.ISBN);
        if (existing is not null)
        {
            if (existing.Quantity + 1 > existing.MaxQuantity)
            {
                SetError("Cannot add more than available stock.");
                return;
            }

            existing.Quantity += 1;
            RefreshTotals();
            return;
        }

        var line = new PosLineItem
        {
            ISBN = book.ISBN,
            ItemCode = book.ISBN,
            ItemName = book.Title,
            UnitPrice = book.Price,
            Quantity = 1,
            MaxQuantity = book.Stock
        };

        line.PropertyChanged += Line_PropertyChanged;
        _orderLines.Add(line);
        RefreshTotals();
    }

    private void Line_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(PosLineItem.Quantity) or nameof(PosLineItem.UnitPrice))
            RefreshTotals();
    }

    private void DecreaseQty_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button)
            return;

        if (button.DataContext is not PosLineItem line)
            return;

        ClearError();
        if (line.Quantity <= 1)
        {
            RemoveLine(line);
            return;
        }

        line.Quantity -= 1;
        RefreshTotals();
    }

    private void IncreaseQty_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button)
            return;

        if (button.DataContext is not PosLineItem line)
            return;

        ClearError();
        if (line.MaxQuantity > 0 && line.Quantity + 1 > line.MaxQuantity)
        {
            SetError("Cannot add more than available stock.");
            return;
        }

        line.Quantity += 1;
        RefreshTotals();
    }

    private void RemoveLine_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button)
            return;

        if (button.DataContext is not PosLineItem line)
            return;

        ClearError();
        RemoveLine(line);
    }

    private void RemoveLine(PosLineItem line)
    {
        line.PropertyChanged -= Line_PropertyChanged;
        _orderLines.Remove(line);
        RefreshTotals();
    }

    private async void AddCustomItem_Click(object? sender, RoutedEventArgs e)
    {
        ClearError();

        var owner = TopLevel.GetTopLevel(this) as Window;
        var dialog = new AddCustomItemWindow();

        if (owner is null)
        {
            dialog.Show();
            return;
        }

        bool added = await dialog.ShowDialog<bool>(owner);
        if (!added)
            return;

        var line = new PosLineItem
        {
            ISBN = null,
            ItemCode = "Custom",
            ItemName = dialog.ItemName,
            UnitPrice = dialog.UnitPrice,
            Quantity = dialog.Quantity,
            MaxQuantity = 0
        };

        line.PropertyChanged += Line_PropertyChanged;
        _orderLines.Add(line);
        RefreshTotals();
    }

    private void CustomerCombo_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is not ComboBox combo)
            return;

        _selectedCustomer = combo.SelectedItem as CustomerOption;
    }

    private void ApplyDiscount_Click(object? sender, RoutedEventArgs e)
    {
        ClearError();

        var box = this.FindControl<TextBox>("DiscountCodeBox");
        var code = (box?.Text ?? "").Trim().ToUpperInvariant();

        _discountCode = string.IsNullOrWhiteSpace(code) ? null : code;
        _discountAmount = 0m;

        if (string.IsNullOrWhiteSpace(code))
        {
            RefreshTotals();
            return;
        }

        if (_orderLines.Count == 0)
        {
            SetError("Add at least one item before applying a discount.");
            RefreshTotals();
            return;
        }

        try
        {
            using var context = new AppDbContext();
            var card = context.DiscountCards.FirstOrDefault(d => d.Code == code && d.IsActive);

            if (card is null || (card.ExpiresAt.HasValue && card.ExpiresAt.Value <= DateTime.UtcNow))
            {
                SetError("Invalid or expired discount code.");
                RefreshTotals();
                return;
            }

            var subtotal = _orderLines.Sum(l => l.LineTotal);
            decimal discount = card.Type == DiscountCardType.Percentage
                ? Math.Round(subtotal * (card.Value / 100m), 2, MidpointRounding.AwayFromZero)
                : card.Value;

            if (discount < 0) discount = 0m;
            if (discount > subtotal) discount = subtotal;

            _discountAmount = discount;
            RefreshTotals();
        }
        catch (Exception)
        {
            SetError("Failed to apply discount.");
            RefreshTotals();
        }
    }

    private void PaymentCard_Click(object? sender, RoutedEventArgs e)
    {
        _paymentMethod = "Card";
        UpdatePaymentButtons();
    }

    private void PaymentCash_Click(object? sender, RoutedEventArgs e)
    {
        _paymentMethod = "Cash";
        UpdatePaymentButtons();
    }

    private void UpdatePaymentButtons()
    {
        var card = this.FindControl<Button>("CardButton");
        var cash = this.FindControl<Button>("CashButton");

        if (card is not null) card.Classes.Set("selected", _paymentMethod == "Card");
        if (cash is not null) cash.Classes.Set("selected", _paymentMethod == "Cash");
    }

    private void CompleteSale_Click(object? sender, RoutedEventArgs e)
    {
        ClearError();

        if (_orderLines.Count == 0)
        {
            SetError("Add at least one item to complete the sale.");
            return;
        }

        var subtotal = _orderLines.Sum(l => l.LineTotal);
        var taxAmount = Math.Round(subtotal * DefaultTaxRate, 2, MidpointRounding.AwayFromZero);
        var total = Math.Round(subtotal + taxAmount - _discountAmount, 2, MidpointRounding.AwayFromZero);

        var customer = _selectedCustomer ?? _customerOptions.FirstOrDefault();
        var customerName = customer?.DisplayName ?? "Walk-in Customer";
        var customerId = customer?.CustomerId;

        try
        {
            using var context = new AppDbContext();
            using var tx = context.Database.BeginTransaction();

            var createdNotifications = new List<Notification>();
            var lowStockCrossed = new List<(string Title, string ISBN, int Stock)>();

            var order = new Orders
            {
                CreatedAt = DateTime.UtcNow,
                CustomerId = customerId,
                CustomerName = customerName,
                PaymentMethod = _paymentMethod,
                DiscountCode = string.IsNullOrWhiteSpace(_discountCode) ? null : _discountCode,
                Subtotal = subtotal,
                TaxRate = DefaultTaxRate,
                TaxAmount = taxAmount,
                DiscountAmount = _discountAmount,
                Total = total
            };

            context.Orders.Add(order);
            context.SaveChanges();

            var saleNotification = new Notification
            {
                CreatedAt = DateTime.UtcNow,
                Type = "SaleCompleted",
                Title = "Sale completed",
                Message = $"Order #{order.OrderId} • {FormatMoney(total)} • {_paymentMethod}",
                OrderId = order.OrderId
            };

            context.Notifications.Add(saleNotification);
            createdNotifications.Add(saleNotification);

            foreach (var line in _orderLines)
            {
                context.OrderDetails.Add(new OrderDetails
                {
                    OrderId = order.OrderId,
                    ISBN = line.ISBN,
                    ItemName = line.ItemName,
                    UnitPrice = line.UnitPrice,
                    Quantity = line.Quantity,
                    LineTotal = line.LineTotal
                });

                if (!string.IsNullOrWhiteSpace(line.ISBN))
                {
                    var book = context.Books.FirstOrDefault(b => b.ISBN == line.ISBN);
                    if (book is null)
                        throw new InvalidOperationException("A book in the order was not found in inventory.");

                    if (book.Stock < line.Quantity)
                        throw new InvalidOperationException($"Insufficient stock for '{book.Title}'.");

                    var oldStock = book.Stock;
                    book.Stock -= line.Quantity;

                    if (oldStock >= 8 && book.Stock < 8)
                        lowStockCrossed.Add((book.Title, book.ISBN, book.Stock));
                }
            }

            foreach (var item in lowStockCrossed)
            {
                var n = new Notification
                {
                    CreatedAt = DateTime.UtcNow,
                    Type = "LowStock",
                    Title = "Low stock alert",
                    Message = $"{item.Title} ({item.ISBN}) is low: {item.Stock} left",
                    ISBN = item.ISBN
                };

                context.Notifications.Add(n);
                createdNotifications.Add(n);
            }

            context.SaveChanges();
            tx.Commit();

            foreach (var n in createdNotifications)
                NotificationHub.Raise(n);

            // Reset UI
            foreach (var line in _orderLines.ToList())
            {
                line.PropertyChanged -= Line_PropertyChanged;
            }

            _orderLines.Clear();
            _discountCode = null;
            _discountAmount = 0m;

            var discountBox = this.FindControl<TextBox>("DiscountCodeBox");
            if (discountBox is not null) discountBox.Text = "";

            var searchBox = this.FindControl<TextBox>("SearchBox");
            if (searchBox is not null) searchBox.Text = "";

            var resultsContainer = this.FindControl<Border>("SearchResultsContainer");
            if (resultsContainer is not null) resultsContainer.IsVisible = false;

            LoadBooks();
            RefreshTotals();
        }
        catch (Exception ex)
        {
            SetError(ex.Message);
        }
    }

    private sealed record CustomerOption(int? CustomerId, string DisplayName);
}

public class PosLineItem : INotifyPropertyChanged
{
    private string? _isbn;
    private string _itemCode = "";
    private string _itemName = "";
    private decimal _unitPrice;
    private int _quantity;
    private int _maxQuantity;

    public string? ISBN
    {
        get => _isbn;
        set
        {
            if (value == _isbn) return;
            _isbn = value;
            OnPropertyChanged(nameof(ISBN));
        }
    }

    public string ItemCode
    {
        get => _itemCode;
        set
        {
            if (value == _itemCode) return;
            _itemCode = value;
            OnPropertyChanged(nameof(ItemCode));
        }
    }

    public string ItemName
    {
        get => _itemName;
        set
        {
            if (value == _itemName) return;
            _itemName = value;
            OnPropertyChanged(nameof(ItemName));
        }
    }

    public decimal UnitPrice
    {
        get => _unitPrice;
        set
        {
            if (value == _unitPrice) return;
            _unitPrice = value;
            OnPropertyChanged(nameof(UnitPrice));
            OnPropertyChanged(nameof(UnitPriceDisplay));
            OnPropertyChanged(nameof(LineTotal));
            OnPropertyChanged(nameof(LineTotalDisplay));
        }
    }

    public int Quantity
    {
        get => _quantity;
        set
        {
            if (value == _quantity) return;
            _quantity = value;
            OnPropertyChanged(nameof(Quantity));
            OnPropertyChanged(nameof(LineTotal));
            OnPropertyChanged(nameof(LineTotalDisplay));
        }
    }

    public int MaxQuantity
    {
        get => _maxQuantity;
        set
        {
            if (value == _maxQuantity) return;
            _maxQuantity = value;
            OnPropertyChanged(nameof(MaxQuantity));
        }
    }

    public decimal LineTotal => UnitPrice * Quantity;

    public string UnitPriceDisplay => $"LKR {UnitPrice.ToString("0.00", CultureInfo.InvariantCulture)}";

    public string LineTotalDisplay => $"LKR {LineTotal.ToString("0.00", CultureInfo.InvariantCulture)}";

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
