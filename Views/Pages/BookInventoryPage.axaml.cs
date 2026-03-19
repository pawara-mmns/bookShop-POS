using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using bookShop.Data;
using bookShop.Models;
using bookShop.Views.Dialogs;

namespace bookShop.Views.Pages;

public partial class BookInventoryPage : UserControl
{
    private List<Books> _books = new();

    public BookInventoryPage()
    {
        InitializeComponent();
        LoadBooks();
    }

    private void LoadBooks()
    {
        try
        {
            using var context = new AppDbContext();
            _books = context.Books
                .OrderBy(b => b.Title)
                .ToList();

            var list = this.FindControl<ItemsControl>("BooksList");
            if (list is not null)
                list.ItemsSource = _books;
        }
        catch (Exception)
        {
            var list = this.FindControl<ItemsControl>("BooksList");
            if (list is not null)
                list.ItemsSource = Array.Empty<Books>();
        }
    }

    private async void AddBook_Click(object? sender, RoutedEventArgs e)
    {
        var owner = TopLevel.GetTopLevel(this) as Window;
        var dialog = new AddBookWindow();

        if (owner is null)
        {
            dialog.Show();
            return;
        }

        bool added = await dialog.ShowDialog<bool>(owner);
        if (added)
            LoadBooks();
    }

    private async void EditBook_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button)
            return;

        if (button.DataContext is not Books book)
            return;

        var owner = TopLevel.GetTopLevel(this) as Window;
        var dialog = new EditBookWindow(book);

        if (owner is null)
        {
            dialog.Show();
            return;
        }

        bool saved = await dialog.ShowDialog<bool>(owner);
        if (saved)
            LoadBooks();
    }

    private async void DeleteBook_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button)
            return;

        if (button.DataContext is not Books book)
            return;

        var owner = TopLevel.GetTopLevel(this) as Window;
        var confirm = new ConfirmDeleteWindow("Delete Book", $"Are you sure you want to delete '{book.Title}'?");

        if (owner is null)
        {
            confirm.Show();
            return;
        }

        bool shouldDelete = await confirm.ShowDialog<bool>(owner);
        if (!shouldDelete)
            return;

        try
        {
            using var context = new AppDbContext();
            var existing = context.Books.FirstOrDefault(b => b.ISBN == book.ISBN);
            if (existing is null)
                return;

            context.Books.Remove(existing);
            context.SaveChanges();
            LoadBooks();
        }
        catch (Exception)
        {
            // minimal behavior
        }
    }
}
