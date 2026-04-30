using System;
using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using bookShop.Models;
using bookShop.Service;
using bookShop.Views.Pages;

namespace bookShop.Views;

public partial class DashboardWindow : Window
{
    private readonly ObservableCollection<ToastItem> _toasts = new();

    public DashboardWindow()
    {
        InitializeComponent();

        global::bookShop.AppIcon.Apply(this);

        var toastHost = this.FindControl<ItemsControl>("ToastHost");
        if (toastHost is not null)
            toastHost.ItemsSource = _toasts;

        NotificationHub.NotificationCreated += NotificationHub_NotificationCreated;
        Closed += (_, _) => NotificationHub.NotificationCreated -= NotificationHub_NotificationCreated;

        ApplyNavigationPermissions();

        var defaultRoute = AuthorizationService.GetDefaultRoute(SessionContext.CurrentUser);
        NavigateTo(defaultRoute);
    }

    private void ApplyNavigationPermissions()
    {
        var user = SessionContext.CurrentUser;

        // If no session (e.g., window opened directly), keep existing nav visible.
        if (user is null)
            return;

        void SetVisible(string name, string route)
        {
            var nav = this.FindControl<Border>(name);
            if (nav is not null)
                nav.IsVisible = AuthorizationService.CanAccessRoute(user, route) || AuthorizationService.IsAdmin(user);
        }

        SetVisible("NavDashboard", "Dashboard");
        SetVisible("NavOrders", "Orders");
        SetVisible("NavPos", "POS Billing");
        SetVisible("NavInventory", "Book Inventory");
        SetVisible("NavCustomers", "Customers");
        SetVisible("NavSuppliers", "Suppliers");
        SetVisible("NavReports", "Reports");
        SetVisible("NavDiscountCards", "Discount Cards");

        var navCashiers = this.FindControl<Border>("NavCashiers");
        if (navCashiers is not null)
            navCashiers.IsVisible = AuthorizationService.IsAdmin(user);
    }

    private void NotificationsButton_Click(object? sender, RoutedEventArgs e)
    {
        var panel = this.FindControl<Border>("NotificationsPanel");
        if (panel is null)
            return;

        panel.IsVisible = !panel.IsVisible;
        if (panel.IsVisible)
            RefreshNotificationsList();
    }

    private void RefreshNotificationsList()
    {
        var list = this.FindControl<ItemsControl>("NotificationsList");
        var empty = this.FindControl<TextBlock>("NotificationsEmptyText");
        if (list is null || empty is null)
            return;

        var items = NotificationService.GetLatest(20);
        list.ItemsSource = items;
        empty.IsVisible = items.Count == 0;
    }

    private void NotificationHub_NotificationCreated(object? sender, Notification notification)
    {
        Dispatcher.UIThread.Post(() =>
        {
            ShowToast(notification.Title, notification.Message);
            RefreshNotificationsList();
        });
    }

    private void ShowToast(string title, string message)
    {
        var toast = new ToastItem(title, message);
        _toasts.Add(toast);

        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(4) };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            _toasts.Remove(toast);
        };
        timer.Start();
    }

    private void NavItem_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control control)
            return;

        var tag = control.Tag?.ToString();
        if (string.IsNullOrWhiteSpace(tag))
            return;

        NavigateTo(tag);
    }

    private void NavigateTo(string route)
    {
        var user = SessionContext.CurrentUser;
        if (user is not null && !AuthorizationService.CanAccessRoute(user, route) && !AuthorizationService.IsAdmin(user))
            return;

        var host = this.FindControl<ContentControl>("PageHost");
        if (host is null)
            return;

        host.Content = route switch
        {
            "Dashboard" => new DashboardPage(),
            "Orders" => new OrdersPage(),
            "Customers" => new CustomersPage(),
            "Suppliers" => new SuppliersPage(),
            "POS Billing" => new PosBillingPage(),
            "Book Inventory" => new BookInventoryPage(),
            "Reports" => new ReportsPage(),
            "Discount Cards" => new DiscountCardsPage(),
            "Cashiers" => new CashiersPage(),
            _ => host.Content
        };

        var header = this.FindControl<TextBlock>("HeaderTitle");
        if (header is not null)
            header.Text = route;

        SetSelectedNav(route);
    }

    private void SetSelectedNav(string route)
    {
        var navDashboard = this.FindControl<Border>("NavDashboard");
        var navOrders = this.FindControl<Border>("NavOrders");
        var navPos = this.FindControl<Border>("NavPos");
        var navInventory = this.FindControl<Border>("NavInventory");
        var navCustomers = this.FindControl<Border>("NavCustomers");
        var navCashiers = this.FindControl<Border>("NavCashiers");
        var navSuppliers = this.FindControl<Border>("NavSuppliers");
        var navReports = this.FindControl<Border>("NavReports");
        var navDiscountCards = this.FindControl<Border>("NavDiscountCards");

        if (navDashboard is not null) navDashboard.Classes.Set("selected", route == "Dashboard");
        if (navOrders is not null) navOrders.Classes.Set("selected", route == "Orders");
        if (navPos is not null) navPos.Classes.Set("selected", route == "POS Billing");
        if (navInventory is not null) navInventory.Classes.Set("selected", route == "Book Inventory");
        if (navCustomers is not null) navCustomers.Classes.Set("selected", route == "Customers");
        if (navCashiers is not null) navCashiers.Classes.Set("selected", route == "Cashiers");
        if (navSuppliers is not null) navSuppliers.Classes.Set("selected", route == "Suppliers");
        if (navReports is not null) navReports.Classes.Set("selected", route == "Reports");
        if (navDiscountCards is not null) navDiscountCards.Classes.Set("selected", route == "Discount Cards");
    }

    private void SignOut_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        SessionContext.Clear();
        var login = new global::bookShop.MainWindow();
        login.Show();
        Close();
    }

    private sealed record ToastItem(string Title, string Message);
}