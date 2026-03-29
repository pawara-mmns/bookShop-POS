using System;
using System.Collections.Generic;
using System.IO;
using PdfSharpCore.Fonts;

namespace bookShop.Service;

public sealed class SystemTtfFontResolver : IFontResolver
{
    private const string RegularKey = "DejaVuSans#Regular";
    private const string BoldKey = "DejaVuSans#Bold";

    public string DefaultFontName => "DejaVu Sans";

    private static readonly Lazy<byte[]> RegularFontBytes = new(LoadRegular);
    private static readonly Lazy<byte[]> BoldFontBytes = new(LoadBold);

    public byte[] GetFont(string faceName)
    {
        return faceName switch
        {
            RegularKey => RegularFontBytes.Value,
            BoldKey => BoldFontBytes.Value,
            _ => RegularFontBytes.Value
        };
    }

    public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
    {
        // Map common families to DejaVu Sans, then embed the TTF.
        familyName = (familyName ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(familyName))
            return new FontResolverInfo(isBold ? BoldKey : RegularKey);

        var normalized = familyName.Replace("_", " ", StringComparison.Ordinal).ToLowerInvariant();

        if (normalized is "dejavu sans" or "dejavusans" or "arial" or "helvetica" or "sans" or "sans serif" or "inter")
            return new FontResolverInfo(isBold ? BoldKey : RegularKey);

        // Fallback to DejaVu for everything else.
        return new FontResolverInfo(isBold ? BoldKey : RegularKey);
    }

    private static byte[] LoadRegular()
        => TryLoadFromPaths(GetRegularFontPaths());

    private static byte[] LoadBold()
        => TryLoadFromPaths(GetBoldFontPaths());

    private static byte[] TryLoadFromPaths(IEnumerable<string> paths)
    {
        foreach (var path in paths)
        {
            try
            {
                if (File.Exists(path))
                    return File.ReadAllBytes(path);
            }
            catch
            {
                // try next
            }
        }

        throw new FileNotFoundException(
            "Could not find a usable TTF font for PDF export. Install DejaVu Sans or Liberation Sans on this system.");
    }

    private static IEnumerable<string> GetRegularFontPaths()
    {
        // Linux
        yield return "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf";
        yield return "/usr/share/fonts/truetype/liberation/LiberationSans-Regular.ttf";
        yield return "/usr/share/fonts/truetype/liberation2/LiberationSans-Regular.ttf";

        // macOS
        yield return "/System/Library/Fonts/Supplemental/Arial Unicode.ttf";
        yield return "/Library/Fonts/Arial Unicode.ttf";

        // Windows
        var win = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
        if (!string.IsNullOrWhiteSpace(win))
        {
            yield return Path.Combine(win, "arial.ttf");
            yield return Path.Combine(win, "segoeui.ttf");
        }
    }

    private static IEnumerable<string> GetBoldFontPaths()
    {
        // Linux
        yield return "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf";
        yield return "/usr/share/fonts/truetype/liberation/LiberationSans-Bold.ttf";
        yield return "/usr/share/fonts/truetype/liberation2/LiberationSans-Bold.ttf";

        // macOS
        yield return "/System/Library/Fonts/Supplemental/Arial Bold.ttf";
        yield return "/Library/Fonts/Arial Bold.ttf";

        // Windows
        var win = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
        if (!string.IsNullOrWhiteSpace(win))
        {
            yield return Path.Combine(win, "arialbd.ttf");
            yield return Path.Combine(win, "segoeuib.ttf");
        }
    }
}
