using System;
using System.Globalization;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using bookShop.Data;
using bookShop.Models;

namespace bookShop.Views.Dialogs;

public partial class EditBookWindow : Window
{
    private string _isbn = "";

    public EditBookWindow()
    {
        InitializeComponent();

        global::bookShop.AppIcon.Apply(this);
    }

    public EditBookWindow(Books book)
    {
        InitializeComponent();

        global::bookShop.AppIcon.Apply(this);

        _isbn = book.ISBN;

        var isbnText = this.FindControl<TextBlock>("IsbnText");
        if (isbnText is not null)
            isbnText.Text = $"ISBN: {book.ISBN}";

        var titleBox = this.FindControl<TextBox>("TitleBox");
        var authorBox = this.FindControl<TextBox>("AuthorBox");
        var categoryBox = this.FindControl<TextBox>("CategoryBox");
        var stockBox = this.FindControl<TextBox>("StockBox");
        var priceBox = this.FindControl<TextBox>("PriceBox");

        if (titleBox is not null) titleBox.Text = book.Title;
        if (authorBox is not null) authorBox.Text = book.Author;
        if (categoryBox is not null) categoryBox.Text = book.Category;
        if (stockBox is not null) stockBox.Text = book.Stock.ToString(CultureInfo.InvariantCulture);
        if (priceBox is not null) priceBox.Text = book.Price.ToString(CultureInfo.InvariantCulture);
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }

    private void Save_Click(object? sender, RoutedEventArgs e)
    {
        var title = (this.FindControl<TextBox>("TitleBox")?.Text ?? "").Trim();
        var author = (this.FindControl<TextBox>("AuthorBox")?.Text ?? "").Trim();
        var category = (this.FindControl<TextBox>("CategoryBox")?.Text ?? "").Trim();
        var stockText = (this.FindControl<TextBox>("StockBox")?.Text ?? "").Trim();
        var priceText = (this.FindControl<TextBox>("PriceBox")?.Text ?? "").Trim();

        var error = this.FindControl<TextBlock>("ErrorText");
        if (error is not null) error.Text = "";

        if (string.IsNullOrWhiteSpace(_isbn) || string.IsNullOrWhiteSpace(title) ||
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
            var existing = context.Books.FirstOrDefault(b => b.ISBN == _isbn);
            if (existing is null)
            {
                if (error is not null) error.Text = "Book not found.";
                return;
            }

            existing.Title = title;
            existing.Author = author;
            existing.Category = category;
            existing.Stock = stock;
            existing.Price = price;

            context.SaveChanges();
            Close(true);
        }
        catch (Exception)
        {
            if (error is not null) error.Text = "Failed to save changes. Please try again.";
        }
    }
}
