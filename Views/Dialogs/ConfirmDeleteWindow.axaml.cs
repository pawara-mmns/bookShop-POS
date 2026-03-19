using Avalonia.Controls;
using Avalonia.Interactivity;

namespace bookShop.Views.Dialogs;

public partial class ConfirmDeleteWindow : Window
{
    public ConfirmDeleteWindow() : this("Are you sure you want to delete this customer?")
    {
    }

    public ConfirmDeleteWindow(string message)
    {
        InitializeComponent();

        var msg = this.FindControl<TextBlock>("MessageText");
        if (msg is not null)
            msg.Text = message;
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }

    private void Delete_Click(object? sender, RoutedEventArgs e)
    {
        Close(true);
    }
}
