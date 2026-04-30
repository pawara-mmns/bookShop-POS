using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using bookShop.Data;
using bookShop.Models;
using bookShop.Views.Dialogs;

namespace bookShop.Views.Pages;

public partial class DiscountCardsPage : UserControl
{
    private List<DiscountCards> _cards = new();

    public DiscountCardsPage()
    {
        InitializeComponent();
        LoadCards();
    }

    private void LoadCards()
    {
        try
        {
            using var context = new AppDbContext();
            _cards = context.DiscountCards
                .OrderByDescending(d => d.DiscountCardId)
                .ToList();

            var list = this.FindControl<ItemsControl>("DiscountCardsList");
            if (list is not null)
                list.ItemsSource = _cards;
        }
        catch (Exception)
        {
            var list = this.FindControl<ItemsControl>("DiscountCardsList");
            if (list is not null)
                list.ItemsSource = Array.Empty<DiscountCards>();
        }
    }

    private async void AddDiscountCard_Click(object? sender, RoutedEventArgs e)
    {
        var owner = TopLevel.GetTopLevel(this) as Window;
        var dialog = new AddDiscountCardWindow();

        if (owner is null)
        {
            dialog.Show();
            return;
        }

        bool added = await dialog.ShowDialog<bool>(owner);
        if (added)
            LoadCards();
    }

    private async void DeleteDiscountCard_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button)
            return;

        if (button.DataContext is not DiscountCards card)
            return;

        var owner = TopLevel.GetTopLevel(this) as Window;
        var confirm = new ConfirmDeleteWindow("Delete Discount Card", $"Are you sure you want to delete '{card.Code}'?");

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
            var existing = context.DiscountCards.FirstOrDefault(d => d.DiscountCardId == card.DiscountCardId);
            if (existing is null)
                return;

            context.DiscountCards.Remove(existing);
            context.SaveChanges();
            LoadCards();
        }
        catch (Exception)
        {
            // minimal behavior
        }
    }
}
