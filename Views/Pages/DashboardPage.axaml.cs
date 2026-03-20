using System;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Threading;

namespace bookShop.Views.Pages;

public partial class DashboardPage : UserControl
{
    private DispatcherTimer? _clockTimer;

    public DashboardPage()
    {
        InitializeComponent();

        AttachedToLogicalTree += (_, _) => StartClock();
        DetachedFromLogicalTree += (_, _) => StopClock();

        UpdateClockText();
    }

    private void StartClock()
    {
        if (_clockTimer is not null)
            return;

        _clockTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };

        _clockTimer.Tick += (_, _) => UpdateClockText();
        _clockTimer.Start();
    }

    private void StopClock()
    {
        if (_clockTimer is null)
            return;

        _clockTimer.Stop();
        _clockTimer = null;
    }

    private void UpdateClockText()
    {
        var text = this.FindControl<TextBlock>("LiveDateTimeText");
        if (text is null)
            return;

        var now = DateTime.Now;
        text.Text = now.ToString("ddd, MMM dd, yyyy • h:mm:ss tt", CultureInfo.InvariantCulture);
    }
}
