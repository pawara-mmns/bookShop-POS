using System;
using System.Globalization;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using bookShop.Data;
using bookShop.Models;
using bookShop.Service;

namespace bookShop.Views.Dialogs;

public partial class AddDiscountCardWindow : Window
{
    public AddDiscountCardWindow()
    {
        InitializeComponent();
        global::bookShop.AppIcon.Apply(this);

        var typeCombo = this.FindControl<ComboBox>("TypeCombo");
        if (typeCombo is not null)
        {
            typeCombo.ItemsSource = new[] { DiscountCardType.Percentage, DiscountCardType.FixedAmount };
            typeCombo.SelectedIndex = 0;
        }

        var codeBox = this.FindControl<TextBox>("CodeBox");
        if (codeBox is not null)
            codeBox.Text = DiscountCodeGenerator.Generate();
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }

    private void Generate_Click(object? sender, RoutedEventArgs e)
    {
        var codeBox = this.FindControl<TextBox>("CodeBox");
        if (codeBox is not null)
            codeBox.Text = DiscountCodeGenerator.Generate();
    }

    private void Add_Click(object? sender, RoutedEventArgs e)
    {
        var error = this.FindControl<TextBlock>("ErrorText");
        if (error is not null) error.Text = "";

        var code = (this.FindControl<TextBox>("CodeBox")?.Text ?? "").Trim().ToUpperInvariant();
        var valueText = (this.FindControl<TextBox>("ValueBox")?.Text ?? "").Trim();
        var type = (this.FindControl<ComboBox>("TypeCombo")?.SelectedItem as DiscountCardType?) ?? DiscountCardType.Percentage;
        var isActive = this.FindControl<CheckBox>("ActiveCheck")?.IsChecked ?? true;
        var expires = this.FindControl<DatePicker>("ExpiresPicker")?.SelectedDate;

        if (!DiscountCodeGenerator.IsValidFormat(code))
        {
            if (error is not null)
                error.Text = "Code format must be XXX-XXX-XXX (A-Z/2-9).";
            return;
        }

        if (!decimal.TryParse(valueText, NumberStyles.Number, CultureInfo.InvariantCulture, out var value) || value <= 0)
        {
            if (error is not null)
                error.Text = "Value must be a positive number (use dot as decimal separator).";
            return;
        }

        if (type == DiscountCardType.Percentage && value > 100)
        {
            if (error is not null)
                error.Text = "Percentage cannot be greater than 100.";
            return;
        }

        try
        {
            using var context = new AppDbContext();

            var exists = context.DiscountCards.Any(d => d.Code == code);
            if (exists)
            {
                if (error is not null)
                    error.Text = "This discount code already exists. Generate a new one.";
                return;
            }

            context.DiscountCards.Add(new DiscountCards
            {
                Code = code,
                Type = type,
                Value = value,
                IsActive = isActive,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = expires?.DateTime
            });

            context.SaveChanges();
            Close(true);
        }
        catch (Exception)
        {
            if (error is not null)
                error.Text = "Failed to add discount card. Please try again.";
        }
    }
}
