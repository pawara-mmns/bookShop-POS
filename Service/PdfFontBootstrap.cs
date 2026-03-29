using System;
using PdfSharpCore.Fonts;

namespace bookShop.Service;

public static class PdfFontBootstrap
{
    private static bool _initialized;

    public static void Initialize()
    {
        if (_initialized)
            return;

        try
        {
            GlobalFontSettings.FontResolver ??= new SystemTtfFontResolver();
            _initialized = true;
        }
        catch (InvalidOperationException)
        {
            // PdfSharpCore allows setting resolver only once.
            _initialized = true;
        }
    }
}
