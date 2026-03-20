using System;
using Avalonia.Controls;
using Avalonia.Platform;

namespace bookShop;

public static class AppIcon
{
    private static WindowIcon? _cached;

    private static readonly Uri[] CandidateUris =
    [
        new Uri("avares://bookShop/Assets/app-icon.ico"),
        new Uri("avares://bookShop/Assets/app-icon.png"),
    ];

    public static WindowIcon? Get()
    {
        if (_cached is not null)
            return _cached;

        foreach (var uri in CandidateUris)
        {
            try
            {
                if (!AssetLoader.Exists(uri))
                    continue;

                using var stream = AssetLoader.Open(uri);
                _cached = new WindowIcon(stream);
                return _cached;
            }
            catch
            {
                // Ignore icon load failures; app should still run.
            }
        }

        return null;
    }

    public static void Apply(Window window)
    {
        var icon = Get();
        if (icon is not null)
            window.Icon = icon;
    }
}
