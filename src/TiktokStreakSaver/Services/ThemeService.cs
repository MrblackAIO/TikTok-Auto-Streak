using System.Text.Json;
using TiktokStreakSaver.Models;
using TiktokStreakSaver.Services.Storage;

namespace TiktokStreakSaver.Services;

/// <summary>
/// Centralized runtime theme engine.
/// Resolves active mode (Dark/Light/System), preset, and 12 custom color overrides,
/// updates Application.Current.Resources dynamically without restart, synchronizes
/// native system bars, and notifies code-behind views and GraphicsView drawables.
/// </summary>
[Microsoft.Maui.Controls.Internals.Preserve(AllMembers = true)]
public sealed class ThemeService
{
    private const string ThemeModeKey = "app_theme_mode";
    private const string ThemePresetIdKey = "app_theme_preset_id";
    private const string ThemeCustomColorsKey = "app_theme_custom_colors";

    private static readonly Lazy<ThemeService> LazyInstance = new(() => new ThemeService());
    public static ThemeService Current => LazyInstance.Value;

    private readonly IAppStorage _storage = AppStorageProvider.Current;
    private bool _subscribedToSystemTheme;

    public AppThemeMode CurrentMode { get; private set; } = AppThemeMode.Dark;
    public string CurrentPresetId { get; private set; } = ThemeCatalog.DefaultPresetId;
    public ThemeCustomColors CustomColors { get; private set; } = new();
    public ThemePalette CurrentPalette { get; private set; } = ThemeCatalog.GetPreset(ThemeCatalog.DefaultPresetId).DarkPalette;

    public event EventHandler? ThemeChanged;

    private ThemeService()
    {
        LoadSavedConfiguration();
    }

    public void Initialize()
    {
        EnsureSystemThemeSubscription();
        ApplyCurrentTheme();
    }

    private void EnsureSystemThemeSubscription()
    {
        if (_subscribedToSystemTheme || Application.Current == null)
            return;

        _subscribedToSystemTheme = true;
        Application.Current.RequestedThemeChanged += OnSystemRequestedThemeChanged;
    }

    private void OnSystemRequestedThemeChanged(object? sender, AppThemeChangedEventArgs e)
    {
        if (CurrentMode == AppThemeMode.System)
        {
            MainThread.BeginInvokeOnMainThread(() => ApplyCurrentTheme(e.RequestedTheme));
        }
    }

    public void LoadSavedConfiguration()
    {
        var modeRaw = _storage.GetString(ThemeModeKey, nameof(AppThemeMode.Dark));
        CurrentMode = Enum.TryParse<AppThemeMode>(modeRaw, true, out var parsedMode)
            ? parsedMode
            : AppThemeMode.Dark;

        var presetId = _storage.GetString(ThemePresetIdKey, ThemeCatalog.DefaultPresetId);
        CurrentPresetId = ThemeCatalog.GetPreset(presetId).Id;

        var customJson = _storage.GetString(ThemeCustomColorsKey, string.Empty);
        if (!string.IsNullOrWhiteSpace(customJson))
        {
            try
            {
                CustomColors = JsonSerializer.Deserialize(customJson, AppJsonContext.Default.ThemeCustomColors)
                    ?? new ThemeCustomColors();
            }
            catch
            {
                CustomColors = new ThemeCustomColors();
            }
        }
        else
        {
            CustomColors = new ThemeCustomColors();
        }
    }

    public void SaveAndApply(AppThemeMode mode, string presetId, ThemeCustomColors? customColors)
    {
        CurrentMode = mode;
        CurrentPresetId = ThemeCatalog.GetPreset(presetId).Id;
        CustomColors = customColors?.Clone() ?? new ThemeCustomColors();

        _storage.SetString(ThemeModeKey, CurrentMode.ToString());
        _storage.SetString(ThemePresetIdKey, CurrentPresetId);
        var json = JsonSerializer.Serialize(CustomColors, AppJsonContext.Default.ThemeCustomColors);
        _storage.SetString(ThemeCustomColorsKey, json);

        EnsureSystemThemeSubscription();
        ApplyCurrentTheme();
    }

    public void SetMode(AppThemeMode mode) => SaveAndApply(mode, CurrentPresetId, CustomColors);

    public void SetPreset(string presetId, bool preserveCustomOverrides = false)
    {
        var preset = ThemeCatalog.GetPreset(presetId);
        var nextMode = CurrentMode;
        if (preset.Id == "minimal_light" && CurrentMode == AppThemeMode.Dark)
            nextMode = AppThemeMode.Light;
        else if (preset.Id != "minimal_light" && CurrentMode == AppThemeMode.Light && !preserveCustomOverrides)
            nextMode = preset.RecommendedMode;

        SaveAndApply(nextMode, preset.Id, preserveCustomOverrides ? CustomColors : new ThemeCustomColors());
    }

    public void ResetToDefault()
    {
        _storage.Remove(ThemeModeKey);
        _storage.Remove(ThemePresetIdKey);
        _storage.Remove(ThemeCustomColorsKey);

        CurrentMode = AppThemeMode.Dark;
        CurrentPresetId = ThemeCatalog.DefaultPresetId;
        CustomColors = new ThemeCustomColors();

        ApplyCurrentTheme();
    }

    public ThemePalette ResolvePalette(
        AppThemeMode mode,
        string presetId,
        ThemeCustomColors? customColors,
        AppTheme? systemOverride = null)
    {
        var preset = ThemeCatalog.GetPreset(presetId);
        bool useLight = ResolveIsLight(mode, systemOverride);
        var basePalette = useLight ? preset.LightPalette : preset.DarkPalette;

        if (customColors == null || !customColors.HasAnyOverride)
            return basePalette;

        static Color Pick(string? hex, Color fallback) =>
            ColorContrastHelper.TryParseHex(hex, out var parsed) ? parsed : fallback;

        var mainBg = Pick(customColors.MainBackgroundHex, basePalette.MainBackground);
        var secBg = Pick(customColors.SecondaryBackgroundHex, basePalette.SecondaryBackground);
        var cardBg = Pick(customColors.CardBackgroundHex, basePalette.CardBackground);
        var primaryText = Pick(customColors.PrimaryTextHex, basePalette.PrimaryText);
        var secondaryText = Pick(customColors.SecondaryTextHex, basePalette.SecondaryText);

        if (customColors.AutoContrast)
        {
            bool bgChanged = !string.IsNullOrWhiteSpace(customColors.MainBackgroundHex) ||
                             !string.IsNullOrWhiteSpace(customColors.CardBackgroundHex);

            if (bgChanged && string.IsNullOrWhiteSpace(customColors.PrimaryTextHex))
                primaryText = ColorContrastHelper.EnsureReadableText(primaryText, cardBg);
            else if (!string.IsNullOrWhiteSpace(customColors.PrimaryTextHex))
                primaryText = ColorContrastHelper.EnsureReadableText(primaryText, cardBg, 3.0);

            if (bgChanged && string.IsNullOrWhiteSpace(customColors.SecondaryTextHex))
                secondaryText = ColorContrastHelper.EnsureReadableSecondaryText(secondaryText, cardBg);
        }

        bool isLightEffective = ColorContrastHelper.IsLightColor(mainBg);

        return new ThemePalette(
            Primary: Pick(customColors.PrimaryHex, basePalette.Primary),
            Secondary: Pick(customColors.SecondaryHex, basePalette.Secondary),
            Accent: Pick(customColors.AccentHex, basePalette.Accent),
            MainBackground: mainBg,
            SecondaryBackground: secBg,
            CardBackground: cardBg,
            PrimaryText: primaryText,
            SecondaryText: secondaryText,
            Border: Pick(customColors.BorderHex, basePalette.Border),
            Success: Pick(customColors.SuccessHex, basePalette.Success),
            Warning: Pick(customColors.WarningHex, basePalette.Warning),
            Error: Pick(customColors.ErrorHex, basePalette.Error),
            IsLight: isLightEffective);
    }

    private static bool ResolveIsLight(AppThemeMode mode, AppTheme? systemOverride = null)
    {
        return mode switch
        {
            AppThemeMode.Light => true,
            AppThemeMode.Dark => false,
            _ => (systemOverride ?? Application.Current?.PlatformAppTheme ?? AppTheme.Dark) == AppTheme.Light
        };
    }

    public void ApplyCurrentTheme(AppTheme? systemOverride = null)
    {
        var palette = ResolvePalette(CurrentMode, CurrentPresetId, CustomColors, systemOverride);
        CurrentPalette = palette;

        var app = Application.Current;
        if (app != null)
        {
            app.UserAppTheme = CurrentMode switch
            {
                AppThemeMode.Dark => AppTheme.Dark,
                AppThemeMode.Light => AppTheme.Light,
                _ => AppTheme.Unspecified
            };

            ApplyPaletteToResources(app.Resources, palette);
        }

        ApplyPlatformSystemBars();
        ThemeChanged?.Invoke(this, EventArgs.Empty);
    }

    private static void ApplyPaletteToResources(ResourceDictionary resources, ThemePalette p)
    {
        SetColorAndBrush(resources, "ObsidianBlack", p.MainBackground);
        SetColorAndBrush(resources, "ObsidianSurface", p.CardBackground);
        SetColorAndBrush(resources, "ObsidianSurfaceElevated", p.SecondaryBackground);
        SetColorAndBrush(resources, "ObsidianBorder", p.Border);
        SetColorAndBrush(resources, "BorderSubtle", ColorContrastHelper.WithAlpha(p.PrimaryText, 0.08f));
        SetColorAndBrush(resources, "BorderGlow", ColorContrastHelper.WithAlpha(p.Primary, 0.28f));

        SetColorAndBrush(resources, "TextPrimary", p.PrimaryText);
        SetColorAndBrush(resources, "TextSecondary", p.SecondaryText);
        SetColorAndBrush(resources, "BrandSubtitle", p.MutedText);

        SetColorAndBrush(resources, "MintGreen", p.Success);
        SetColorAndBrush(resources, "MintGreenNeon", p.Success);
        SetColorAndBrush(resources, "CoralRed", p.Primary);
        SetColorAndBrush(resources, "VioletPurple", p.Accent);
        SetColorAndBrush(resources, "TealCyan", p.Secondary);
        SetColorAndBrush(resources, "WarmAmber", p.Warning);
        SetColorAndBrush(resources, "DarkSlate", p.SecondaryBackground);
        SetColorAndBrush(resources, "MutedIndigo", p.CardBackground);

        SetColorAndBrush(resources, "Primary", p.Primary);
        SetColorAndBrush(resources, "PrimaryDark", p.Primary);
        SetColorAndBrush(resources, "PrimaryDarkText", p.OnPrimary);
        SetColorAndBrush(resources, "OnPrimary", p.OnPrimary);
        SetColorAndBrush(resources, "Secondary", p.Secondary);
        SetColorAndBrush(resources, "SecondaryDarkText", p.OnSecondary);
        SetColorAndBrush(resources, "OnSecondary", p.OnSecondary);
        SetColorAndBrush(resources, "Tertiary", p.Accent);
        SetColorAndBrush(resources, "OnAccent", p.OnAccent);
        SetColorAndBrush(resources, "OnSuccess", p.OnSuccess);
        SetColorAndBrush(resources, "OnWarning", p.OnWarning);
        SetColorAndBrush(resources, "OnError", p.OnError);

        SetColorAndBrush(resources, "PrimaryLight", p.PrimaryTint);
        SetColorAndBrush(resources, "PrimarySubtle", p.PrimarySubtle);
        SetColorAndBrush(resources, "AccentTint", p.AccentTint);
        SetColorAndBrush(resources, "SecondaryTint", p.SecondaryTint);

        SetColorAndBrush(resources, "Magenta", p.Primary);
        SetColorAndBrush(resources, "MidnightBlue", p.MainBackground);
        SetColorAndBrush(resources, "OffBlack", p.MainBackground);
        SetColorAndBrush(resources, "PageBackgroundLight", p.MainBackground);

        SetColorAndBrush(resources, "Success", p.Success);
        SetColorAndBrush(resources, "SuccessDark", p.Success);
        SetColorAndBrush(resources, "Warning", p.Warning);
        SetColorAndBrush(resources, "WarningDark", p.Warning);
        SetColorAndBrush(resources, "Error", p.Error);
        SetColorAndBrush(resources, "ErrorDark", p.Error);
        SetColorAndBrush(resources, "Info", p.Secondary);
        SetColorAndBrush(resources, "InfoDark", p.Secondary);

        SetColorAndBrush(resources, "Gray100", p.CardBackground);
        SetColorAndBrush(resources, "Gray200", p.SecondaryBackground);
        SetColorAndBrush(resources, "Gray300", p.MutedText);
        SetColorAndBrush(resources, "Gray400", p.SecondaryText);
        SetColorAndBrush(resources, "Gray500", p.MutedText);
        SetColorAndBrush(resources, "Gray600", p.Border);
        SetColorAndBrush(resources, "Gray700", p.Border);
        SetColorAndBrush(resources, "Gray800", p.SecondaryBackground);
        SetColorAndBrush(resources, "Gray900", p.CardBackground);
        SetColorAndBrush(resources, "Gray950", p.MainBackground);

        SetColorAndBrush(resources, "OverlayLight", p.OverlayBackground);
        SetColorAndBrush(resources, "OverlayDark", p.OverlayBackground);

        SetColorAndBrush(resources, "CardBackgroundLight", p.CardBackground);
        SetColorAndBrush(resources, "CardBackgroundDark", p.CardBackground);
        SetColorAndBrush(resources, "SurfaceElevated", p.SecondaryBackground);
        SetColorAndBrush(resources, "BorderColorLight", p.Border);
        SetColorAndBrush(resources, "BorderColorDark", p.Border);
        SetColorAndBrush(resources, "DeleteColor", p.Error);
        SetColorAndBrush(resources, "ListItemLight", p.CardBackground);
        SetColorAndBrush(resources, "ListItemDark", p.CardBackground);
        SetColorAndBrush(resources, "ListItemBorderLight", p.Border);
        SetColorAndBrush(resources, "ListItemBorderDark", p.Border);

        SetColorAndBrush(resources, "NavBarBackground", p.NavBarBackground);
        SetColorAndBrush(resources, "ControlDockBackground", p.ControlDockBackground);
        SetColorAndBrush(resources, "ThemeShadowColor", p.ShadowColor);
        SetColorAndBrush(resources, "SwitchOffThumb", p.SwitchOffThumb);
        SetColorAndBrush(resources, "DisabledSurface", p.DisabledSurface);
        SetColorAndBrush(resources, "DisabledText", p.DisabledText);

        SetColorAndBrush(resources, "WarningBannerBackground", p.WarningBannerBackground);
        SetColorAndBrush(resources, "WarningBannerBorder", p.WarningBannerBorder);
        SetColorAndBrush(resources, "WarningBannerTitle", p.WarningBannerTitle);
    }

    private static void SetColorAndBrush(ResourceDictionary resources, string key, Color color)
    {
        resources[key] = color;
        resources[key + "Brush"] = new SolidColorBrush(color);
    }

    public void ApplyPlatformSystemBars()
    {
        var palette = CurrentPalette;
#if ANDROID
        try
        {
            var activity = Platform.CurrentActivity;
            var window = activity?.Window;
            if (window == null)
                return;

            var bg = palette.MainBackground;
            var androidColor = Android.Graphics.Color.Argb(
                (int)(bg.Alpha * 255),
                (int)(bg.Red * 255),
                (int)(bg.Green * 255),
                (int)(bg.Blue * 255));

#pragma warning disable CA1422
            window.SetStatusBarColor(androidColor);
            window.SetNavigationBarColor(androidColor);
#pragma warning restore CA1422

            var controller = AndroidX.Core.View.WindowCompat.GetInsetsController(window, window.DecorView);
            if (controller != null)
            {
                bool lightIcons = palette.IsLight;
                controller.AppearanceLightStatusBars = lightIcons;
                controller.AppearanceLightNavigationBars = lightIcons;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ThemeService] ApplyPlatformSystemBars failed: {ex.Message}");
        }
#endif
    }
}
