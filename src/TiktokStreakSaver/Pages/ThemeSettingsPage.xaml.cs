using Microsoft.Maui.Controls.Shapes;
using TiktokStreakSaver.Models;
using TiktokStreakSaver.Services;

namespace TiktokStreakSaver.Pages;

[Microsoft.Maui.Controls.Internals.Preserve(AllMembers = true)]
public partial class ThemeSettingsPage : ContentPage
{
    private enum TokenSlot
    {
        Primary,
        Secondary,
        Accent,
        MainBackground,
        SecondaryBackground,
        CardBackground,
        PrimaryText,
        SecondaryText,
        Border,
        Success,
        Warning,
        Error
    }

    private sealed record TokenDescriptor(TokenSlot Slot, string Label, string Subtitle);

    private static readonly TokenDescriptor[] TokenDescriptors =
    {
        new(TokenSlot.Primary, "Primary Color", "Main CTA buttons and active navigation"),
        new(TokenSlot.Secondary, "Secondary Color", "Secondary badges, handles, and highlights"),
        new(TokenSlot.Accent, "Accent Color", "Timer arc, editor badges, and pills"),
        new(TokenSlot.MainBackground, "Main Background", "Root page canvas and system bars"),
        new(TokenSlot.SecondaryBackground, "Secondary Background", "Inputs, elevated chips, and tracks"),
        new(TokenSlot.CardBackground, "Card & Surface", "Section cards and list item surfaces"),
        new(TokenSlot.PrimaryText, "Primary Text", "Headings, titles, and primary values"),
        new(TokenSlot.SecondaryText, "Secondary Text", "Subtitles, hints, and timestamps"),
        new(TokenSlot.Border, "Borders & Dividers", "Card strokes and subtle separators"),
        new(TokenSlot.Success, "Success Indicator", "Sent badges, active dots, and gauges"),
        new(TokenSlot.Warning, "Warning Indicator", "Caution banners and retry notices"),
        new(TokenSlot.Error, "Error Indicator", "Failure alerts, stop, and delete actions")
    };

    private static readonly string[] QuickAccentHexes =
    {
        "#FF3B5C", "#FE2C55", "#06B6D4", "#8B5CF6", "#10B981", "#38BDF8", "#F59E0B", "#EC4899"
    };

    private AppThemeMode _draftMode;
    private string _draftPresetId;
    private ThemeCustomColors _draftCustom;
    private TokenSlot? _editingSlot;
    private bool _suppressPickerEvents;
    private bool _isAdvancedExpanded;

    public ThemeSettingsPage()
    {
        InitializeComponent();

        var svc = ThemeService.Current;
        _draftMode = svc.CurrentMode;
        _draftPresetId = svc.CurrentPresetId;
        _draftCustom = svc.CustomColors.Clone();

        AutoContrastSwitch.IsToggled = _draftCustom.AutoContrast;

        BuildAccentSwatches();
        BuildPickerQuickSwatches();
        RefreshAllDraftUi();
    }

    private ThemePalette GetDraftPalette() =>
        ThemeService.Current.ResolvePalette(_draftMode, _draftPresetId, _draftCustom);

    private void RefreshAllDraftUi()
    {
        var palette = GetDraftPalette();
        UpdateModePills(palette);
        UpdateLivePreview(palette);
        BuildPresetsGrid();
        BuildAdvancedTokenRows(palette);
    }

    private void UpdateModePills(ThemePalette palette)
    {
        var activeBg = palette.Primary;
        var activeFg = palette.OnPrimary;
        var inactiveBg = Colors.Transparent;
        var inactiveFg = palette.SecondaryText;

        ModeDarkPill.BackgroundColor = _draftMode == AppThemeMode.Dark ? activeBg : inactiveBg;
        ModeDarkLabel.TextColor = _draftMode == AppThemeMode.Dark ? activeFg : inactiveFg;

        ModeLightPill.BackgroundColor = _draftMode == AppThemeMode.Light ? activeBg : inactiveBg;
        ModeLightLabel.TextColor = _draftMode == AppThemeMode.Light ? activeFg : inactiveFg;

        ModeSystemPill.BackgroundColor = _draftMode == AppThemeMode.System ? activeBg : inactiveBg;
        ModeSystemLabel.TextColor = _draftMode == AppThemeMode.System ? activeFg : inactiveFg;
    }

    private void UpdateLivePreview(ThemePalette p)
    {
        var preset = ThemeCatalog.GetPreset(_draftPresetId);
        var modeLabel = _draftMode switch
        {
            AppThemeMode.Dark => "Dark",
            AppThemeMode.Light => "Light",
            _ => "System"
        };
        var customTag = _draftCustom.HasAnyOverride ? " • Custom" : string.Empty;

        PreviewOuterCanvas.BackgroundColor = p.MainBackground;
        PreviewOuterCanvas.Stroke = new SolidColorBrush(p.Border);
        PreviewHeadingLabel.TextColor = p.SecondaryText;
        PreviewPresetTitle.Text = $"{preset.Name} • {modeLabel}{customTag}";
        PreviewPresetTitle.TextColor = p.PrimaryText;

        PreviewCard.BackgroundColor = p.CardBackground;
        PreviewCard.Stroke = new SolidColorBrush(p.Border);
        PreviewCardTitle.TextColor = p.PrimaryText;
        PreviewCardSub.TextColor = p.SecondaryText;

        PreviewAccentChip.BackgroundColor = p.AccentTint;
        PreviewAccentChipText.TextColor = p.Accent;

        PreviewSuccessBox.BackgroundColor = p.SuccessTint;
        PreviewSuccessBox.Stroke = new SolidColorBrush(p.Success.WithAlpha(0.4f));
        PreviewSuccessText.TextColor = p.Success;

        PreviewWarningBox.BackgroundColor = p.WarningTint;
        PreviewWarningBox.Stroke = new SolidColorBrush(p.Warning.WithAlpha(0.4f));
        PreviewWarningText.TextColor = p.Warning;

        PreviewErrorBox.BackgroundColor = p.ErrorTint;
        PreviewErrorBox.Stroke = new SolidColorBrush(p.Error.WithAlpha(0.4f));
        PreviewErrorText.TextColor = p.Error;

        PreviewPrimaryBtn.BackgroundColor = p.Primary;
        PreviewPrimaryBtnText.TextColor = p.OnPrimary;

        PreviewSecondaryBtn.BackgroundColor = p.SecondaryBackground;
        PreviewSecondaryBtn.Stroke = new SolidColorBrush(p.Secondary);
        PreviewSecondaryBtnText.TextColor = p.Secondary;

        // WCAG contrast calculation between PrimaryText and CardBackground
        var cardRatio = ColorContrastHelper.GetContrastRatio(p.PrimaryText, p.CardBackground);
        var bgRatio = ColorContrastHelper.GetContrastRatio(p.PrimaryText, p.MainBackground);
        var minRatio = Math.Min(cardRatio, bgRatio);
        var rating = ColorContrastHelper.GetContrastRating(minRatio);

        PreviewContrastLabel.Text = $"{rating} {minRatio:F1}:1";
        var isReadable = minRatio >= 4.5;
        PreviewContrastBadge.BackgroundColor = isReadable ? p.SuccessTint : p.WarningTint;
        PreviewContrastBadge.Stroke = new SolidColorBrush((isReadable ? p.Success : p.Warning).WithAlpha(0.5f));
        PreviewContrastLabel.TextColor = isReadable ? p.Success : p.Warning;

        ContrastFixBanner.IsVisible = !isReadable;
        if (!isReadable)
            ContrastWarningText.TextColor = p.Warning;

        QuickAccentHexEntry.Placeholder = ColorContrastHelper.ToHex(p.Primary);
    }

    private void BuildPresetsGrid()
    {
        PresetsContainer.Children.Clear();
        var currentActive = ThemeService.Current.CurrentPalette;

        foreach (var preset in ThemeCatalog.Presets)
        {
            var isSelected = preset.Id.Equals(_draftPresetId, StringComparison.OrdinalIgnoreCase);
            var previewPalette = _draftMode == AppThemeMode.Light ||
                                 (_draftMode == AppThemeMode.System && Application.Current?.PlatformAppTheme == AppTheme.Light)
                ? preset.LightPalette
                : preset.DarkPalette;

            var card = new Border
            {
                Margin = new Thickness(0, 0, 0, 10),
                Padding = new Thickness(12, 10),
                StrokeThickness = isSelected ? 2 : 1,
                Stroke = new SolidColorBrush(isSelected ? currentActive.Primary : currentActive.Border),
                StrokeShape = new RoundRectangle { CornerRadius = 14 },
                BackgroundColor = isSelected ? currentActive.PrimarySubtle : currentActive.SecondaryBackground
            };
            FlexLayout.SetBasis(card, new Microsoft.Maui.Layouts.FlexBasis(0.485f, true));

            var stack = new VerticalStackLayout { Spacing = 6 };
            stack.Children.Add(new Label
            {
                Text = preset.Name,
                FontFamily = "InterSemiBold",
                FontSize = 13,
                TextColor = currentActive.PrimaryText
            });
            stack.Children.Add(new Label
            {
                Text = preset.Description,
                FontFamily = "InterRegular",
                FontSize = 10,
                TextColor = currentActive.SecondaryText,
                MaxLines = 1,
                LineBreakMode = LineBreakMode.TailTruncation
            });

            var swatchRow = new HorizontalStackLayout { Spacing = 5 };
            foreach (var c in new[] { previewPalette.MainBackground, previewPalette.CardBackground, previewPalette.Primary, previewPalette.Secondary, previewPalette.Accent })
            {
                swatchRow.Children.Add(new Border
                {
                    WidthRequest = 16,
                    HeightRequest = 16,
                    StrokeThickness = 1,
                    Stroke = new SolidColorBrush(currentActive.Border),
                    StrokeShape = new RoundRectangle { CornerRadius = 8 },
                    BackgroundColor = c
                });
            }
            stack.Children.Add(swatchRow);
            card.Content = stack;

            var capturedId = preset.Id;
            var capturedRecMode = preset.RecommendedMode;
            var tap = new TapGestureRecognizer();
            tap.Tapped += (_, _) =>
            {
                _draftPresetId = capturedId;
                if (capturedId == "minimal_light" && _draftMode == AppThemeMode.Dark)
                    _draftMode = AppThemeMode.Light;
                else if (capturedId != "minimal_light" && _draftMode == AppThemeMode.Light && !_draftCustom.HasAnyOverride)
                    _draftMode = capturedRecMode;

                RefreshAllDraftUi();
            };
            card.GestureRecognizers.Add(tap);
            PresetsContainer.Children.Add(card);
        }
    }

    private void BuildAccentSwatches()
    {
        AccentSwatchesRow.Children.Clear();
        var border = ThemeService.Current.CurrentPalette.Border;
        foreach (var hex in QuickAccentHexes)
        {
            var swatch = new Border
            {
                WidthRequest = 28,
                HeightRequest = 28,
                StrokeThickness = 1.5,
                Stroke = new SolidColorBrush(border),
                StrokeShape = new RoundRectangle { CornerRadius = 14 },
                BackgroundColor = Color.FromArgb(hex)
            };
            var capturedHex = hex;
            var tap = new TapGestureRecognizer();
            tap.Tapped += (_, _) =>
            {
                _draftCustom.PrimaryHex = capturedHex;
                QuickAccentHexEntry.Text = capturedHex;
                RefreshAllDraftUi();
            };
            swatch.GestureRecognizers.Add(tap);
            AccentSwatchesRow.Children.Add(swatch);
        }
    }

    private void BuildPickerQuickSwatches()
    {
        PickerQuickSwatchesRow.Children.Clear();
        foreach (var hex in new[] { "#FF3B5C", "#06B6D4", "#8B5CF6", "#10B981", "#F59E0B", "#FFFFFF", "#0A0D12", "#F8FAFC" })
        {
            var swatch = new Border
            {
                WidthRequest = 26,
                HeightRequest = 26,
                StrokeThickness = 1,
                Stroke = new SolidColorBrush(ThemeService.Current.CurrentPalette.Border),
                StrokeShape = new RoundRectangle { CornerRadius = 13 },
                BackgroundColor = Color.FromArgb(hex)
            };
            var capturedHex = hex;
            var tap = new TapGestureRecognizer();
            tap.Tapped += (_, _) => ApplyHexToPickerControls(capturedHex);
            swatch.GestureRecognizers.Add(tap);
            PickerQuickSwatchesRow.Children.Add(swatch);
        }
    }

    private void BuildAdvancedTokenRows(ThemePalette palette)
    {
        AdvancedTokensContainer.Children.Clear();
        var activePalette = ThemeService.Current.CurrentPalette;

        foreach (var desc in TokenDescriptors)
        {
            var color = GetColorForSlot(palette, desc.Slot);
            var hex = ColorContrastHelper.ToHex(color);
            var isOverridden = !string.IsNullOrWhiteSpace(GetOverrideHex(desc.Slot));

            var row = new Border
            {
                StrokeThickness = 1,
                Stroke = new SolidColorBrush(isOverridden ? activePalette.Primary : activePalette.Border),
                StrokeShape = new RoundRectangle { CornerRadius = 12 },
                BackgroundColor = activePalette.SecondaryBackground,
                Padding = new Thickness(12, 10)
            };

            var grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto }
                },
                ColumnSpacing = 12
            };

            var swatch = new Border
            {
                WidthRequest = 30,
                HeightRequest = 30,
                StrokeThickness = 1,
                Stroke = new SolidColorBrush(activePalette.Border),
                StrokeShape = new RoundRectangle { CornerRadius = 8 },
                BackgroundColor = color,
                VerticalOptions = LayoutOptions.Center
            };
            Grid.SetColumn(swatch, 0);
            grid.Children.Add(swatch);

            var textStack = new VerticalStackLayout { Spacing = 1, VerticalOptions = LayoutOptions.Center };
            textStack.Children.Add(new Label
            {
                Text = isOverridden ? $"{desc.Label} • Custom" : desc.Label,
                FontFamily = "InterSemiBold",
                FontSize = 13,
                TextColor = activePalette.PrimaryText
            });
            textStack.Children.Add(new Label
            {
                Text = desc.Subtitle,
                FontFamily = "InterRegular",
                FontSize = 10,
                TextColor = activePalette.SecondaryText
            });
            Grid.SetColumn(textStack, 1);
            grid.Children.Add(textStack);

            var hexLabel = new Label
            {
                Text = hex,
                FontFamily = "monospace",
                FontSize = 11,
                TextColor = activePalette.SecondaryText,
                VerticalOptions = LayoutOptions.Center
            };
            Grid.SetColumn(hexLabel, 2);
            grid.Children.Add(hexLabel);

            row.Content = grid;
            var captured = desc;
            var tap = new TapGestureRecognizer();
            tap.Tapped += (_, _) => OpenTokenColorPicker(captured, color);
            row.GestureRecognizers.Add(tap);
            AdvancedTokensContainer.Children.Add(row);
        }
    }

    private void OpenTokenColorPicker(TokenDescriptor desc, Color initialColor)
    {
        _editingSlot = desc.Slot;
        PickerTokenTitleLabel.Text = desc.Label;
        ApplyColorToPickerControls(initialColor);
        ColorPickerOverlay.IsVisible = true;
    }

    private void ApplyColorToPickerControls(Color color)
    {
        _suppressPickerEvents = true;
        try
        {
            int r = (int)Math.Round(color.Red * 255);
            int g = (int)Math.Round(color.Green * 255);
            int b = (int)Math.Round(color.Blue * 255);
            SliderRed.Value = r;
            SliderGreen.Value = g;
            SliderBlue.Value = b;
            LabelRedVal.Text = r.ToString();
            LabelGreenVal.Text = g.ToString();
            LabelBlueVal.Text = b.ToString();
            PickerHexEntry.Text = $"#{r:X2}{g:X2}{b:X2}";
            PickerSwatchPreview.BackgroundColor = color;
        }
        finally
        {
            _suppressPickerEvents = false;
        }
    }

    private void ApplyHexToPickerControls(string hex)
    {
        if (ColorContrastHelper.TryParseHex(hex, out var c))
            ApplyColorToPickerControls(c);
    }

    private void OnRgbSliderChanged(object? sender, ValueChangedEventArgs e)
    {
        if (_suppressPickerEvents)
            return;

        int r = (int)Math.Round(SliderRed.Value);
        int g = (int)Math.Round(SliderGreen.Value);
        int b = (int)Math.Round(SliderBlue.Value);
        var c = Color.FromRgb(r, g, b);
        ApplyColorToPickerControls(c);
    }

    private void OnPickerHexTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_suppressPickerEvents)
            return;

        if (ColorContrastHelper.TryParseHex(e.NewTextValue, out var c))
        {
            _suppressPickerEvents = true;
            try
            {
                int r = (int)Math.Round(c.Red * 255);
                int g = (int)Math.Round(c.Green * 255);
                int b = (int)Math.Round(c.Blue * 255);
                SliderRed.Value = r;
                SliderGreen.Value = g;
                SliderBlue.Value = b;
                LabelRedVal.Text = r.ToString();
                LabelGreenVal.Text = g.ToString();
                LabelBlueVal.Text = b.ToString();
                PickerSwatchPreview.BackgroundColor = c;
            }
            finally
            {
                _suppressPickerEvents = false;
            }
        }
    }

    private void OnPickerClearTokenClicked(object? sender, EventArgs e)
    {
        if (_editingSlot.HasValue)
            SetOverrideHex(_editingSlot.Value, null);
        ColorPickerOverlay.IsVisible = false;
        _editingSlot = null;
        RefreshAllDraftUi();
    }

    private void OnPickerCancelClicked(object? sender, EventArgs e)
    {
        ColorPickerOverlay.IsVisible = false;
        _editingSlot = null;
    }

    private void OnPickerConfirmClicked(object? sender, EventArgs e)
    {
        if (_editingSlot.HasValue)
        {
            int r = (int)Math.Round(SliderRed.Value);
            int g = (int)Math.Round(SliderGreen.Value);
            int b = (int)Math.Round(SliderBlue.Value);
            var hex = ColorContrastHelper.TryParseHex(PickerHexEntry.Text, out var parsed)
                ? ColorContrastHelper.ToHex(parsed)
                : $"#{r:X2}{g:X2}{b:X2}";
            SetOverrideHex(_editingSlot.Value, hex);
        }
        ColorPickerOverlay.IsVisible = false;
        _editingSlot = null;
        RefreshAllDraftUi();
    }

    private void OnModeDarkTapped(object? sender, TappedEventArgs e)
    {
        _draftMode = AppThemeMode.Dark;
        RefreshAllDraftUi();
    }

    private void OnModeLightTapped(object? sender, TappedEventArgs e)
    {
        _draftMode = AppThemeMode.Light;
        RefreshAllDraftUi();
    }

    private void OnModeSystemTapped(object? sender, TappedEventArgs e)
    {
        _draftMode = AppThemeMode.System;
        RefreshAllDraftUi();
    }

    private async void OnQuickAccentHexApplied(object? sender, EventArgs e)
    {
        var raw = QuickAccentHexEntry.Text?.Trim();
        if (string.IsNullOrWhiteSpace(raw))
            return;

        if (!ColorContrastHelper.TryParseHex(raw, out var color))
        {
            await DisplayAlert("Invalid Color", "Enter a valid hex color such as #FF3B5C.", "OK");
            return;
        }

        _draftCustom.PrimaryHex = ColorContrastHelper.ToHex(color);
        RefreshAllDraftUi();
    }

    private void OnToggleAdvancedClicked(object? sender, EventArgs e)
    {
        _isAdvancedExpanded = !_isAdvancedExpanded;
        AdvancedTokensContainer.IsVisible = _isAdvancedExpanded;
        ToggleAdvancedButton.Text = _isAdvancedExpanded ? "Hide" : "Show";
    }

    private void OnAutoContrastToggled(object? sender, ToggledEventArgs e)
    {
        _draftCustom.AutoContrast = e.Value;
        RefreshAllDraftUi();
    }

    private void OnAutoFixContrastClicked(object? sender, EventArgs e)
    {
        var p = GetDraftPalette();
        var readablePrimary = ColorContrastHelper.GetReadableForeground(p.CardBackground);
        var readableSecondary = ColorContrastHelper.EnsureReadableSecondaryText(p.SecondaryText, p.CardBackground, 4.5);
        _draftCustom.PrimaryTextHex = ColorContrastHelper.ToHex(readablePrimary);
        _draftCustom.SecondaryTextHex = ColorContrastHelper.ToHex(readableSecondary);
        RefreshAllDraftUi();
    }

    private async void OnResetClicked(object? sender, EventArgs e)
    {
        bool confirm = await DisplayAlert(
            "Reset Theme",
            "Restore the default Obsidian Dark theme and clear all custom colors?",
            "Reset",
            "Cancel");
        if (!confirm)
            return;

        ThemeService.Current.ResetToDefault();
        _draftMode = ThemeService.Current.CurrentMode;
        _draftPresetId = ThemeService.Current.CurrentPresetId;
        _draftCustom = ThemeService.Current.CustomColors.Clone();
        AutoContrastSwitch.IsToggled = _draftCustom.AutoContrast;
        QuickAccentHexEntry.Text = string.Empty;
        RefreshAllDraftUi();
    }

    private async void OnApplyThemeClicked(object? sender, EventArgs e)
    {
        ThemeService.Current.SaveAndApply(_draftMode, _draftPresetId, _draftCustom);
        await Navigation.PopAsync();
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private static Color GetColorForSlot(ThemePalette p, TokenSlot slot) => slot switch
    {
        TokenSlot.Primary => p.Primary,
        TokenSlot.Secondary => p.Secondary,
        TokenSlot.Accent => p.Accent,
        TokenSlot.MainBackground => p.MainBackground,
        TokenSlot.SecondaryBackground => p.SecondaryBackground,
        TokenSlot.CardBackground => p.CardBackground,
        TokenSlot.PrimaryText => p.PrimaryText,
        TokenSlot.SecondaryText => p.SecondaryText,
        TokenSlot.Border => p.Border,
        TokenSlot.Success => p.Success,
        TokenSlot.Warning => p.Warning,
        TokenSlot.Error => p.Error,
        _ => p.Primary
    };

    private string? GetOverrideHex(TokenSlot slot) => slot switch
    {
        TokenSlot.Primary => _draftCustom.PrimaryHex,
        TokenSlot.Secondary => _draftCustom.SecondaryHex,
        TokenSlot.Accent => _draftCustom.AccentHex,
        TokenSlot.MainBackground => _draftCustom.MainBackgroundHex,
        TokenSlot.SecondaryBackground => _draftCustom.SecondaryBackgroundHex,
        TokenSlot.CardBackground => _draftCustom.CardBackgroundHex,
        TokenSlot.PrimaryText => _draftCustom.PrimaryTextHex,
        TokenSlot.SecondaryText => _draftCustom.SecondaryTextHex,
        TokenSlot.Border => _draftCustom.BorderHex,
        TokenSlot.Success => _draftCustom.SuccessHex,
        TokenSlot.Warning => _draftCustom.WarningHex,
        TokenSlot.Error => _draftCustom.ErrorHex,
        _ => null
    };

    private void SetOverrideHex(TokenSlot slot, string? hex)
    {
        switch (slot)
        {
            case TokenSlot.Primary: _draftCustom.PrimaryHex = hex; break;
            case TokenSlot.Secondary: _draftCustom.SecondaryHex = hex; break;
            case TokenSlot.Accent: _draftCustom.AccentHex = hex; break;
            case TokenSlot.MainBackground: _draftCustom.MainBackgroundHex = hex; break;
            case TokenSlot.SecondaryBackground: _draftCustom.SecondaryBackgroundHex = hex; break;
            case TokenSlot.CardBackground: _draftCustom.CardBackgroundHex = hex; break;
            case TokenSlot.PrimaryText: _draftCustom.PrimaryTextHex = hex; break;
            case TokenSlot.SecondaryText: _draftCustom.SecondaryTextHex = hex; break;
            case TokenSlot.Border: _draftCustom.BorderHex = hex; break;
            case TokenSlot.Success: _draftCustom.SuccessHex = hex; break;
            case TokenSlot.Warning: _draftCustom.WarningHex = hex; break;
            case TokenSlot.Error: _draftCustom.ErrorHex = hex; break;
        }
    }
}
