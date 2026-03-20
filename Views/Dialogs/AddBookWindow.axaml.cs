using System;
using System.Globalization;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using bookShop.Data;
using bookShop.Models;

namespace bookShop.Views.Dialogs;

public partial class AddBookWindow : Window
{
    public AddBookWindow()
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
        var isbn = (this.FindControl<TextBox>("IsbnBox")?.Text ?? "").Trim();
        var title = (this.FindControl<TextBox>("TitleBox")?.Text ?? "").Trim();
        var author = (this.FindControl<TextBox>("AuthorBox")?.Text ?? "").Trim();
        var category = (this.FindControl<TextBox>("CategoryBox")?.Text ?? "").Trim();
        var stockText = (this.FindControl<TextBox>("StockBox")?.Text ?? "").Trim();
        var priceText = (this.FindControl<TextBox>("PriceBox")?.Text ?? "").Trim();

        var error = this.FindControl<TextBlock>("ErrorText");
        if (error is not null) error.Text = "";

        if (string.IsNullOrWhiteSpace(isbn) || string.IsNullOrWhiteSpace(title) ||
            string.IsNullOrWhiteSpace(author) || string.IsNullOrWhiteSpace(category) ||
            string.IsNullOrWhiteSpace(stockText) || string.IsNullOrWhiteSpace(priceText))
        {
            if (error is not null) error.Text = "All fields are required.";
            return;
        }

        if (!int.TryParse(stockText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var stock) &&
            !int.TryParse(stockText, NumberStyles.Integer, CultureInfo.CurrentCulture, out stock))
        {
            if (error is not null) error.Text = "Stock must be a valid number.";
            return;
        }

        if (!decimal.TryParse(priceText, NumberStyles.Number, CultureInfo.InvariantCulture, out var price) &&
            !decimal.TryParse(priceText, NumberStyles.Number, CultureInfo.CurrentCulture, out price))
        {
            if (error is not null) error.Text = "Price must be a valid number.";
            return;
        }

        try
        {
            using var context = new AppDbContext();
            bool exists = context.Books.Any(b => b.ISBN == isbn);
            if (exists)
            {
                if (error is not null) error.Text = "A book with this ISBN already exists.";
                return;
            }

            context.Books.Add(new Books
            {
                ISBN = isbn,
                Title = title,
                Author = author,
                Category = category,
                Stock = stock,
                Price = price
            });

            context.SaveChanges();
            Close(true);
        }
        catch (Exception)
        {
            if (error is not null) error.Text = "Failed to add book. Please try again.";
        }
    }
}
