namespace TiktokStreakSaver.Services;

/// <summary>
/// WCAG 2.1 color contrast, luminance, parsing, and color blending utilities.
/// </summary>
[Microsoft.Maui.Controls.Internals.Preserve(AllMembers = true)]
public static class ColorContrastHelper
{
    private static readonly Color LightInk = Color.FromArgb("#FFFFFF");
    private static readonly Color DarkInk = Color.FromArgb("#0F172A");

    public static bool TryParseHex(string? hex, out Color color)
    {
        color = Colors.Transparent;
        if (string.IsNullOrWhiteSpace(hex))
            return false;

        var clean = hex.Trim();
        if (!clean.StartsWith('#'))
            clean = "#" + clean;

        // Support #RGB, #RRGGBB, #AARRGGBB
        if (clean.Length is not (4 or 7 or 9))
            return false;

        try
        {
            color = Color.FromArgb(clean);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static string ToHex(Color color, bool includeAlpha = false)
    {
        int r = (int)Math.Round(Math.Clamp(color.Red, 0f, 1f) * 255);
        int g = (int)Math.Round(Math.Clamp(color.Green, 0f, 1f) * 255);
        int b = (int)Math.Round(Math.Clamp(color.Blue, 0f, 1f) * 255);
        if (!includeAlpha)
            return $"#{r:X2}{g:X2}{b:X2}";

        int a = (int)Math.Round(Math.Clamp(color.Alpha, 0f, 1f) * 255);
        return $"#{a:X2}{r:X2}{g:X2}{b:X2}";
    }

    public static double GetRelativeLuminance(Color c)
    {
        static double Linearize(double channel) =>
            channel <= 0.03928 ? channel / 12.92 : Math.Pow((channel + 0.055) / 1.055, 2.4);

        var r = Linearize(Math.Clamp(c.Red, 0f, 1f));
        var g = Linearize(Math.Clamp(c.Green, 0f, 1f));
        var b = Linearize(Math.Clamp(c.Blue, 0f, 1f));
        return 0.2126 * r + 0.7152 * g + 0.0722 * b;
    }

    public static double GetContrastRatio(Color a, Color b)
    {
        var l1 = GetRelativeLuminance(a);
        var l2 = GetRelativeLuminance(b);
        var lighter = Math.Max(l1, l2);
        var darker = Math.Min(l1, l2);
        return (lighter + 0.05) / (darker + 0.05);
    }

    public static bool IsLightColor(Color c) => GetRelativeLuminance(c) > 0.36;

    public static Color GetReadableForeground(Color background)
    {
        var lightRatio = GetContrastRatio(LightInk, background);
        var darkRatio = GetContrastRatio(DarkInk, background);
        return lightRatio >= darkRatio ? LightInk : DarkInk;
    }

    public static Color EnsureReadableText(Color preferredText, Color background, double minRatio = 4.5)
    {
        if (GetContrastRatio(preferredText, background) >= minRatio)
            return preferredText;

        return GetReadableForeground(background);
    }

    public static Color EnsureReadableSecondaryText(Color preferredSecondary, Color background, double minRatio = 3.2)
    {
        if (GetContrastRatio(preferredSecondary, background) >= minRatio)
            return preferredSecondary;

        return IsLightColor(background)
            ? Color.FromArgb("#475569")
            : Color.FromArgb("#CBD5E1");
    }

    public static string GetContrastRating(double ratio) => ratio switch
    {
        >= 7.0 => "AAA",
        >= 4.5 => "AA",
        >= 3.0 => "AA Large",
        _ => "Low"
    };

    public static Color WithAlpha(Color c, float alpha) =>
        new(c.Red, c.Green, c.Blue, Math.Clamp(alpha, 0f, 1f));

    public static Color Blend(Color fg, Color bg, float fgWeight)
    {
        var w = Math.Clamp(fgWeight, 0f, 1f);
        var inv = 1f - w;
        return new Color(
            fg.Red * w + bg.Red * inv,
            fg.Green * w + bg.Green * inv,
            fg.Blue * w + bg.Blue * inv,
            1f);
    }

    public static Color Darken(Color c, float amount) => Blend(Colors.Black, c, amount);

    public static Color Lighten(Color c, float amount) => Blend(Colors.White, c, amount);
}
