using System.Text.Json.Serialization;

namespace TiktokStreakSaver.Models;

[JsonConverter(typeof(JsonStringEnumConverter<AppThemeMode>))]
public enum AppThemeMode
{
    Dark,
    Light,
    System
}

/// <summary>
/// Complete resolved color palette consumed across all pages, drawables, popups, and system bars.
/// </summary>
[Microsoft.Maui.Controls.Internals.Preserve(AllMembers = true)]
public sealed record ThemePalette(
    Color Primary,
    Color Secondary,
    Color Accent,
    Color MainBackground,
    Color SecondaryBackground,
    Color CardBackground,
    Color PrimaryText,
    Color SecondaryText,
    Color Border,
    Color Success,
    Color Warning,
    Color Error,
    bool IsLight)
{
    public Color MutedText => Services.ColorContrastHelper.WithAlpha(SecondaryText, 0.8f);
    public Color OnPrimary => Services.ColorContrastHelper.GetReadableForeground(Primary);
    public Color OnSecondary => Services.ColorContrastHelper.GetReadableForeground(Secondary);
    public Color OnAccent => Services.ColorContrastHelper.GetReadableForeground(Accent);
    public Color OnSuccess => Services.ColorContrastHelper.GetReadableForeground(Success);
    public Color OnWarning => Services.ColorContrastHelper.GetReadableForeground(Warning);
    public Color OnError => Services.ColorContrastHelper.GetReadableForeground(Error);

    public Color PrimaryTint => Primary.WithAlpha(0.15f);
    public Color PrimarySubtle => Primary.WithAlpha(0.10f);
    public Color SecondaryTint => Secondary.WithAlpha(0.15f);
    public Color AccentTint => Accent.WithAlpha(0.16f);
    public Color SuccessTint => Success.WithAlpha(0.14f);
    public Color WarningTint => Warning.WithAlpha(0.14f);
    public Color ErrorTint => Error.WithAlpha(0.14f);

    public Color NavBarBackground => IsLight
        ? Services.ColorContrastHelper.Blend(CardBackground, MainBackground, 0.94f).WithAlpha(0.95f)
        : Services.ColorContrastHelper.Blend(CardBackground, MainBackground, 0.70f).WithAlpha(0.92f);

    public Color ControlDockBackground => CardBackground.WithAlpha(IsLight ? 0.96f : 0.92f);
    public Color OverlayBackground => MainBackground.WithAlpha(0.90f);
    public Color ShadowColor => IsLight ? Color.FromArgb("#18000000") : Color.FromArgb("#70000000");
    public Color DisabledSurface => Services.ColorContrastHelper.Blend(SecondaryBackground, Border, 0.5f);
    public Color DisabledText => Services.ColorContrastHelper.WithAlpha(SecondaryText, 0.55f);
    public Color SwitchOffThumb => IsLight ? SecondaryText : Color.FromArgb("#8E95A5");

    public Color WarningBannerBackground => Services.ColorContrastHelper.Blend(Warning, MainBackground, IsLight ? 0.12f : 0.18f);
    public Color WarningBannerBorder => Services.ColorContrastHelper.Blend(Warning, Border, 0.45f);
    public Color WarningBannerTitle => IsLight
        ? Services.ColorContrastHelper.Darken(Warning, 0.35f)
        : Services.ColorContrastHelper.Lighten(Warning, 0.25f);
}

/// <summary>
/// Built-in coordinated theme preset with both dark and light palette variants.
/// </summary>
[Microsoft.Maui.Controls.Internals.Preserve(AllMembers = true)]
public sealed record ThemePreset(
    string Id,
    string Name,
    string Description,
    AppThemeMode RecommendedMode,
    ThemePalette DarkPalette,
    ThemePalette LightPalette);

/// <summary>
/// User-defined overrides for the 12 customizable theme color tokens.
/// Null or empty strings fall back to the active preset's color.
/// </summary>
[Microsoft.Maui.Controls.Internals.Preserve(AllMembers = true)]
public sealed class ThemeCustomColors
{
    public string? PrimaryHex { get; set; }
    public string? SecondaryHex { get; set; }
    public string? AccentHex { get; set; }
    public string? MainBackgroundHex { get; set; }
    public string? SecondaryBackgroundHex { get; set; }
    public string? CardBackgroundHex { get; set; }
    public string? PrimaryTextHex { get; set; }
    public string? SecondaryTextHex { get; set; }
    public string? BorderHex { get; set; }
    public string? SuccessHex { get; set; }
    public string? WarningHex { get; set; }
    public string? ErrorHex { get; set; }
    public bool AutoContrast { get; set; } = true;

    public bool HasAnyOverride =>
        !string.IsNullOrWhiteSpace(PrimaryHex) ||
        !string.IsNullOrWhiteSpace(SecondaryHex) ||
        !string.IsNullOrWhiteSpace(AccentHex) ||
        !string.IsNullOrWhiteSpace(MainBackgroundHex) ||
        !string.IsNullOrWhiteSpace(SecondaryBackgroundHex) ||
        !string.IsNullOrWhiteSpace(CardBackgroundHex) ||
        !string.IsNullOrWhiteSpace(PrimaryTextHex) ||
        !string.IsNullOrWhiteSpace(SecondaryTextHex) ||
        !string.IsNullOrWhiteSpace(BorderHex) ||
        !string.IsNullOrWhiteSpace(SuccessHex) ||
        !string.IsNullOrWhiteSpace(WarningHex) ||
        !string.IsNullOrWhiteSpace(ErrorHex);

    public ThemeCustomColors Clone() => new()
    {
        PrimaryHex = PrimaryHex,
        SecondaryHex = SecondaryHex,
        AccentHex = AccentHex,
        MainBackgroundHex = MainBackgroundHex,
        SecondaryBackgroundHex = SecondaryBackgroundHex,
        CardBackgroundHex = CardBackgroundHex,
        PrimaryTextHex = PrimaryTextHex,
        SecondaryTextHex = SecondaryTextHex,
        BorderHex = BorderHex,
        SuccessHex = SuccessHex,
        WarningHex = WarningHex,
        ErrorHex = ErrorHex,
        AutoContrast = AutoContrast
    };
}
