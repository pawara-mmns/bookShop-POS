using Avalonia.Controls;
using bookShop.Service;
using bookShop.Views;

namespace bookShop;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        global::bookShop.AppIcon.Apply(this);

        var usernameBox = this.FindControl<TextBox>("UsernameBox");
        var passwordBox = this.FindControl<TextBox>("PasswordBox");
        var loginButton = this.FindControl<Button>("LoginButton");
        var resultText = this.FindControl<TextBlock>("ResultText");

        var authService = new AuthService();

        if (usernameBox != null && passwordBox != null && loginButton != null && resultText != null)
        {
            loginButton.Click += (_, _) =>
            {
                var username = usernameBox.Text ?? "";
                var password = passwordBox.Text ?? "";

                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                {
                    resultText.Text = "Please enter username and password.";
                    return;
                }

                var user = authService.Login(username, password);

                if (user is not null)
                {
                    var dashboard = new DashboardWindow();
                    dashboard.Show();

                    this.Close();
                }
                else
                {
                    resultText.Text = "Invalid username or password.";
                }
            };
        }
    }
}