using System;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace bookShop.Views.Dialogs;

public partial class AddCustomItemWindow : Window
{
    public string ItemName { get; private set; } = "";
    public decimal UnitPrice { get; private set; }
    public int Quantity { get; private set; } = 1;

    public AddCustomItemWindow()
    {
        InitializeComponent();
        global::bookShop.AppIcon.Apply(this);
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }

    private void Add_Click(object? sender, RoutedEventArgs e)
    {
        var name = (this.FindControl<TextBox>("ItemNameBox")?.Text ?? "").Trim();
        var priceText = (this.FindControl<TextBox>("PriceBox")?.Text ?? "").Trim();
        var qtyText = (this.FindControl<TextBox>("QtyBox")?.Text ?? "").Trim();

        var error = this.FindControl<TextBlock>("ErrorText");
        if (error is not null) error.Text = "";

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(priceText) || string.IsNullOrWhiteSpace(qtyText))
        {
            if (error is not null) error.Text = "All fields are required.";
            return;
        }

        if (!decimal.TryParse(priceText, NumberStyles.Number, CultureInfo.InvariantCulture, out var price) &&
            !decimal.TryParse(priceText, NumberStyles.Number, CultureInfo.CurrentCulture, out price))
        {
            if (error is not null) error.Text = "Unit price must be a valid number.";
            return;
        }

        if (price < 0)
        {
            if (error is not null) error.Text = "Unit price must be 0 or more.";
            return;
        }

        if (!int.TryParse(qtyText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var qty) &&
            !int.TryParse(qtyText, NumberStyles.Integer, CultureInfo.CurrentCulture, out qty))
        {
            if (error is not null) error.Text = "Quantity must be a valid number.";
            return;
        }

        if (qty <= 0)
        {
            if (error is not null) error.Text = "Quantity must be 1 or more.";
            return;
        }

        ItemName = name;
        UnitPrice = Math.Round(price, 2, MidpointRounding.AwayFromZero);
        Quantity = qty;

        Close(true);
    }
}
