using Avalonia.Controls;
using Avalonia.Interactivity;

namespace bookShop.Views.Dialogs;

public partial class ConfirmDeleteWindow : Window
{
    public ConfirmDeleteWindow() : this("Delete Customer", "Are you sure you want to delete this customer?")
    {
    }

    public ConfirmDeleteWindow(string message) : this("Delete Customer", message)
    {
    }

    public ConfirmDeleteWindow(string heading, string message)
    {
        InitializeComponent();

        var headingText = this.FindControl<TextBlock>("HeadingText");
        if (headingText is not null)
            headingText.Text = heading;

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
