using Microsoft.Maui.Controls.Shapes;
using TiktokStreakSaver.Models;
using TiktokStreakSaver.Services;

namespace TiktokStreakSaver.Views;

/// <summary>
/// Interactive, animated card-stack dropdown on the Dashboard allowing users to
/// assign per-recipient automation message overrides when Custom Message Randomizer is OFF.
/// </summary>
[Microsoft.Maui.Controls.Internals.Preserve(AllMembers = true)]
public partial class PersonalizedMessagesCardStack : ContentView
{
    private enum RecipientFilter
    {
        All,
        Friends,
        Groups,
        Custom
    }

    private readonly SettingsService _settingsService = new();
    private readonly Dictionary<string, Editor> _activeEditors = new(StringComparer.Ordinal);
    private List<FriendConfig> _allRecipients = new();
    private RecipientFilter _activeFilter = RecipientFilter.All;
    private bool _isExpanded;
    private bool _isAnimating;
    private bool _isDisabledByRun;
    private bool _subscribedTheme;
    private CancellationTokenSource? _animCts;

    public PersonalizedMessagesCardStack()
    {
        InitializeComponent();
        Loaded += OnViewLoaded;
        Unloaded += OnViewUnloaded;
    }

    private void OnViewLoaded(object? sender, EventArgs e)
    {
        if (!_subscribedTheme)
        {
            ThemeService.Current.ThemeChanged += OnThemeChanged;
            _subscribedTheme = true;
        }
        Refresh();
    }

    private void OnViewUnloaded(object? sender, EventArgs e)
    {
        if (_subscribedTheme)
        {
            ThemeService.Current.ThemeChanged -= OnThemeChanged;
            _subscribedTheme = false;
        }
        _animCts?.Cancel();
    }

    private void OnThemeChanged(object? sender, EventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            BuildFilterChips();
            if (_isExpanded)
                RebuildRecipientCards(animateUnfold: false);
            UpdateHeaderBadge();
        });
    }

    /// <summary>
    /// Synchronizes recipients and custom message counts from SettingsService.
    /// Safe to call on OnAppearing, OnRefreshing, and when default message or run state changes.
    /// </summary>
    public void Refresh(bool isRunning = false)
    {
        _isDisabledByRun = isRunning;

        var raw = _settingsService.GetFriendsList();
        var deduped = new List<FriendConfig>();
        var seenIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var entry in raw)
        {
            if (string.IsNullOrWhiteSpace(entry.Id) || !seenIds.Add(entry.Id))
                continue;
            deduped.Add(entry);
        }

        _allRecipients = deduped;
        UpdateHeaderBadge();
        BuildFilterChips();

        SearchBoxBorder.IsVisible = _allRecipients.Count > 3;
        FilterChipsScroll.IsVisible = _allRecipients.Count > 0;

        if (_isExpanded)
            RebuildRecipientCards(animateUnfold: false);

        Opacity = isRunning ? 0.65 : 1.0;
    }

    public void SetRunningState(bool isRunning)
    {
        if (_isDisabledByRun == isRunning)
            return;

        _isDisabledByRun = isRunning;
        Opacity = isRunning ? 0.65 : 1.0;
        foreach (var editor in _activeEditors.Values)
            editor.IsEnabled = !isRunning;

        if (_isExpanded)
            RebuildRecipientCards(animateUnfold: false);
    }

    public void UpdateDefaultMessagePreview()
    {
        if (!_isExpanded)
            return;

        var defaultPreview = BuildDefaultPlaceholder();
        foreach (var editor in _activeEditors.Values)
            editor.Placeholder = defaultPreview;
    }

    public void PersistPendingEdits()
    {
        foreach (var pair in _activeEditors)
        {
            _settingsService.SetCustomMessageForRecipient(pair.Key, pair.Value.Text);
        }
        UpdateHeaderBadge();
    }

    private void UpdateHeaderBadge()
    {
        var customCount = _settingsService.GetCustomizedRecipientCount(_allRecipients);
        var total = _allRecipients.Count;

        CustomizedCountLabel.Text = customCount == 1 ? "1 custom" : $"{customCount} custom";
        HeaderSubtitleLabel.Text = total == 0
            ? "No friends or groups added yet"
            : $"{total} recipient{(total == 1 ? string.Empty : "s")} • Tap to personalize";

        var palette = ThemeService.Current.CurrentPalette;
        CustomizedCountBadge.BackgroundColor = customCount > 0 ? palette.PrimarySubtle : palette.SecondaryBackground;
        CustomizedCountLabel.TextColor = customCount > 0 ? palette.Primary : palette.SecondaryText;
    }

    private async void OnHeaderTapped(object? sender, TappedEventArgs e)
    {
        if (_isAnimating)
            return;

        _isExpanded = !_isExpanded;
        if (_isExpanded)
            await ExpandWithAnimationAsync();
        else
            await CollapseWithAnimationAsync();
    }

    private async Task ExpandWithAnimationAsync()
    {
        _isAnimating = true;
        _animCts?.Cancel();
        _animCts = new CancellationTokenSource();
        var ct = _animCts.Token;

        try
        {
            var reducedMotion = IsReducedMotionEnabled();
            RebuildRecipientCards(animateUnfold: !reducedMotion);

            ExpandedPanel.IsVisible = true;
            ExpandedPanel.Opacity = 1;

            if (reducedMotion)
            {
                ChevronIcon.Rotation = 180;
                StackPeekLayer1.IsVisible = false;
                StackPeekLayer2.IsVisible = false;
                return;
            }

            // Disintegrate/separate the stacked peek layers as cards unfold
            _ = Task.WhenAll(
                ChevronIcon.RotateTo(180, 220, Easing.CubicOut),
                StackPeekLayer1.TranslateTo(0, 10, 180, Easing.CubicOut),
                StackPeekLayer1.FadeTo(0, 160, Easing.CubicOut),
                StackPeekLayer2.TranslateTo(0, 16, 180, Easing.CubicOut),
                StackPeekLayer2.FadeTo(0, 160, Easing.CubicOut));

            var cards = CardsStackContainer.Children.OfType<View>().ToList();
            var animatedCount = Math.Min(cards.Count, 8);
            var animTasks = new List<Task>(animatedCount);

            for (int i = 0; i < animatedCount; i++)
            {
                var card = cards[i];
                var delayMs = i * 28;
                animTasks.Add(AnimateCardUnfoldAsync(card, delayMs, ct));
            }

            if (animTasks.Count > 0)
                await Task.WhenAll(animTasks);

            if (!ct.IsCancellationRequested)
            {
                StackPeekLayer1.IsVisible = false;
                StackPeekLayer2.IsVisible = false;
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
            _isAnimating = false;
        }
    }

    private static async Task AnimateCardUnfoldAsync(View card, int delayMs, CancellationToken ct)
    {
        if (delayMs > 0)
            await Task.Delay(delayMs, ct);

        if (ct.IsCancellationRequested)
            return;

        await Task.WhenAll(
            card.FadeTo(1.0, 220, Easing.CubicOut),
            card.TranslateTo(0, 0, 240, Easing.CubicOut),
            card.ScaleTo(1.0, 220, Easing.CubicOut),
            card.RotateTo(0, 220, Easing.CubicOut));
    }

    private async Task CollapseWithAnimationAsync()
    {
        _isAnimating = true;
        _animCts?.Cancel();
        _animCts = new CancellationTokenSource();

        try
        {
            PersistPendingEdits();

            var reducedMotion = IsReducedMotionEnabled();
            if (!reducedMotion)
            {
                var cards = CardsStackContainer.Children.OfType<View>().Take(8).ToList();
                var collapseTasks = new List<Task>(cards.Count + 1);
                foreach (var card in cards)
                {
                    collapseTasks.Add(Task.WhenAll(
                        card.FadeTo(0, 150, Easing.CubicIn),
                        card.TranslateTo(0, -10, 150, Easing.CubicIn),
                        card.ScaleTo(0.95, 150, Easing.CubicIn)));
                }

                collapseTasks.Add(ChevronIcon.RotateTo(0, 200, Easing.CubicInOut));
                await Task.WhenAll(collapseTasks);
            }
            else
            {
                ChevronIcon.Rotation = 0;
            }

            ExpandedPanel.IsVisible = false;
            CardsStackContainer.Children.Clear();
            _activeEditors.Clear();

            StackPeekLayer1.IsVisible = true;
            StackPeekLayer2.IsVisible = true;
            if (reducedMotion)
            {
                StackPeekLayer1.TranslationY = 0;
                StackPeekLayer1.Opacity = 0.72;
                StackPeekLayer2.TranslationY = 0;
                StackPeekLayer2.Opacity = 0.45;
            }
            else
            {
                StackPeekLayer1.TranslationY = 8;
                StackPeekLayer1.Opacity = 0;
                StackPeekLayer2.TranslationY = 12;
                StackPeekLayer2.Opacity = 0;
                await Task.WhenAll(
                    StackPeekLayer1.TranslateTo(0, 0, 160, Easing.CubicOut),
                    StackPeekLayer1.FadeTo(0.72, 160, Easing.CubicOut),
                    StackPeekLayer2.TranslateTo(0, 0, 180, Easing.CubicOut),
                    StackPeekLayer2.FadeTo(0.45, 180, Easing.CubicOut));
            }
        }
        finally
        {
            _isAnimating = false;
        }
    }

    private void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_isExpanded)
            RebuildRecipientCards(animateUnfold: false);
    }

    private void BuildFilterChips()
    {
        FilterChipsRow.Children.Clear();
        var palette = ThemeService.Current.CurrentPalette;
        var map = _settingsService.GetCustomFriendMessages();
        var customCount = _allRecipients.Count(r => map.TryGetValue(r.Id, out var m) && !string.IsNullOrWhiteSpace(m));
        var friendsCount = _allRecipients.Count(r => !r.IsGroup);
        var groupsCount = _allRecipients.Count(r => r.IsGroup);

        var items = new (RecipientFilter Filter, string Label)[]
        {
            (RecipientFilter.All, $"All ({_allRecipients.Count})"),
            (RecipientFilter.Friends, $"Friends ({friendsCount})"),
            (RecipientFilter.Groups, $"Groups ({groupsCount})"),
            (RecipientFilter.Custom, $"Customized ({customCount})")
        };

        foreach (var (filter, label) in items)
        {
            var isSelected = _activeFilter == filter;
            var chip = new Border
            {
                StrokeThickness = 1,
                Stroke = new SolidColorBrush(isSelected ? palette.Primary : palette.Border),
                StrokeShape = new RoundRectangle { CornerRadius = 10 },
                BackgroundColor = isSelected ? palette.PrimarySubtle : palette.SecondaryBackground,
                Padding = new Thickness(10, 5)
            };

            chip.Content = new Label
            {
                Text = label,
                FontFamily = isSelected ? "InterSemiBold" : "InterMedium",
                FontSize = 11,
                TextColor = isSelected ? palette.Primary : palette.SecondaryText
            };

            var capturedFilter = filter;
            var tap = new TapGestureRecognizer();
            tap.Tapped += (_, _) =>
            {
                if (_activeFilter == capturedFilter)
                    return;
                _activeFilter = capturedFilter;
                BuildFilterChips();
                if (_isExpanded)
                    RebuildRecipientCards(animateUnfold: false);
            };
            chip.GestureRecognizers.Add(tap);
            FilterChipsRow.Children.Add(chip);
        }
    }

    private void RebuildRecipientCards(bool animateUnfold)
    {
        CardsStackContainer.Children.Clear();
        _activeEditors.Clear();

        var customMap = _settingsService.GetCustomFriendMessages();
        var search = SearchEntry.Text?.Trim() ?? string.Empty;

        var filtered = _allRecipients.Where(r =>
        {
            var hasCustom = customMap.TryGetValue(r.Id, out var msg) && !string.IsNullOrWhiteSpace(msg);
            bool matchesFilter = _activeFilter switch
            {
                RecipientFilter.Friends => !r.IsGroup,
                RecipientFilter.Groups => r.IsGroup,
                RecipientFilter.Custom => hasCustom,
                _ => true
            };
            if (!matchesFilter)
                return false;

            if (string.IsNullOrEmpty(search))
                return true;

            return (!string.IsNullOrWhiteSpace(r.DisplayName) && r.DisplayName.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                   (!string.IsNullOrWhiteSpace(r.Username) && r.Username.Contains(search, StringComparison.OrdinalIgnoreCase));
        }).ToList();

        if (filtered.Count == 0)
        {
            EmptyStateLabel.Text = _allRecipients.Count == 0
                ? "Add friends or groups in the Friends tab to assign personalized messages."
                : "No recipients match the current filter.";
            EmptyStateLabel.IsVisible = true;
            return;
        }

        EmptyStateLabel.IsVisible = false;
        var defaultPlaceholder = BuildDefaultPlaceholder();

        for (int i = 0; i < filtered.Count; i++)
        {
            var recipient = filtered[i];
            customMap.TryGetValue(recipient.Id, out var savedMessage);
            var card = CreateRecipientCard(recipient, savedMessage ?? string.Empty, defaultPlaceholder);

            if (animateUnfold && i < 8)
            {
                card.Opacity = 0;
                card.TranslationY = -16;
                card.TranslationX = i % 2 == 0 ? -10 : 10;
                card.Scale = 0.93;
                card.Rotation = i % 2 == 0 ? -1.8 : 1.8;
            }

            CardsStackContainer.Children.Add(card);
        }
    }

    private string BuildDefaultPlaceholder()
    {
        var defaultText = _settingsService.GetMessageText();
        if (string.IsNullOrWhiteSpace(defaultText))
            return $"Default: {SettingsService.DefaultMessage}";

        var singleLine = defaultText.Replace('\r', ' ').Replace('\n', ' ').Trim();
        if (singleLine.Length > 42)
            singleLine = singleLine[..42] + "…";

        return $"Using default: {singleLine}";
    }

    private View CreateRecipientCard(FriendConfig recipient, string initialCustomMessage, string defaultPlaceholder)
    {
        var palette = ThemeService.Current.CurrentPalette;
        bool hasCustom = !string.IsNullOrWhiteSpace(initialCustomMessage);

        var cardBorder = new Border
        {
            StrokeThickness = hasCustom ? 1.4 : 1,
            Stroke = new SolidColorBrush(hasCustom ? palette.Accent.WithAlpha(0.65f) : palette.Border),
            StrokeShape = new RoundRectangle { CornerRadius = 16 },
            BackgroundColor = palette.SecondaryBackground,
            Padding = new Thickness(12, 10),
            Shadow = new Shadow
            {
                Brush = new SolidColorBrush(palette.ShadowColor),
                Offset = new Point(0, 3),
                Radius = 10,
                Opacity = 0.5f
            }
        };

        var rootStack = new VerticalStackLayout { Spacing = 8 };

        // Top Row: Avatar, Name/Handle, Status Badge, Reset Button
        var headerGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Auto }
            },
            ColumnSpacing = 8,
            VerticalOptions = LayoutOptions.Center
        };

        var displayName = string.IsNullOrWhiteSpace(recipient.DisplayName) ? recipient.Username : recipient.DisplayName;
        var initial = string.IsNullOrWhiteSpace(displayName) ? "?" : displayName.Substring(0, 1).ToUpperInvariant();
        var avatarSeeds = new[] { palette.Primary, palette.Secondary, palette.Accent, palette.Success };
        var avatarColor = avatarSeeds[Math.Abs((displayName ?? string.Empty).GetHashCode()) % avatarSeeds.Length];

        var avatar = new Border
        {
            WidthRequest = 34,
            HeightRequest = 34,
            StrokeThickness = 1,
            Stroke = new SolidColorBrush(avatarColor.WithAlpha(0.6f)),
            StrokeShape = new RoundRectangle { CornerRadius = 17 },
            BackgroundColor = avatarColor.WithAlpha(0.18f),
            VerticalOptions = LayoutOptions.Center
        };
        avatar.Content = new Label
        {
            Text = recipient.IsGroup ? "👥" : initial,
            FontFamily = "InterBold",
            FontSize = 13,
            TextColor = palette.PrimaryText,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        };
        Grid.SetColumn(avatar, 0);
        headerGrid.Children.Add(avatar);

        var nameStack = new VerticalStackLayout { Spacing = 1, VerticalOptions = LayoutOptions.Center };
        nameStack.Children.Add(new Label
        {
            Text = displayName,
            FontFamily = "InterSemiBold",
            FontSize = 13,
            TextColor = palette.PrimaryText,
            LineBreakMode = LineBreakMode.TailTruncation
        });
        nameStack.Children.Add(new Label
        {
            Text = recipient.IsGroup ? "Group Chat" : $"@{recipient.Username}",
            FontFamily = "InterRegular",
            FontSize = 11,
            TextColor = recipient.IsGroup ? palette.Secondary : palette.SecondaryText,
            LineBreakMode = LineBreakMode.TailTruncation
        });
        Grid.SetColumn(nameStack, 1);
        headerGrid.Children.Add(nameStack);

        var statusLabel = new Label
        {
            Text = hasCustom ? "✓ Custom" : "Default",
            FontFamily = "InterSemiBold",
            FontSize = 10,
            TextColor = hasCustom ? palette.Success : palette.SecondaryText
        };
        var statusBadge = new Border
        {
            StrokeThickness = 1,
            Stroke = new SolidColorBrush(hasCustom ? palette.Success.WithAlpha(0.45f) : palette.Border),
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            BackgroundColor = hasCustom ? palette.SuccessTint : palette.CardBackground,
            Padding = new Thickness(7, 3),
            VerticalOptions = LayoutOptions.Center,
            Content = statusLabel
        };
        Grid.SetColumn(statusBadge, 2);
        headerGrid.Children.Add(statusBadge);

        var resetButton = new Button
        {
            Text = "Reset",
            BackgroundColor = Colors.Transparent,
            TextColor = palette.Error,
            FontFamily = "InterSemiBold",
            FontSize = 11,
            Padding = new Thickness(6, 2),
            MinimumHeightRequest = 28,
            MinimumWidthRequest = 32,
            HeightRequest = 28,
            IsVisible = hasCustom,
            IsEnabled = !_isDisabledByRun,
            VerticalOptions = LayoutOptions.Center
        };
        Grid.SetColumn(resetButton, 3);
        headerGrid.Children.Add(resetButton);

        rootStack.Children.Add(headerGrid);

        // Message Editor Container
        var editorBorder = new Border
        {
            StrokeThickness = 1,
            Stroke = new SolidColorBrush(palette.Border),
            StrokeShape = new RoundRectangle { CornerRadius = 12 },
            BackgroundColor = palette.CardBackground
        };

        var editor = new Editor
        {
            Text = initialCustomMessage,
            Placeholder = defaultPlaceholder,
            PlaceholderColor = palette.SecondaryText,
            TextColor = palette.PrimaryText,
            BackgroundColor = Colors.Transparent,
            FontFamily = "InterRegular",
            FontSize = 13,
            HeightRequest = 58,
            Margin = new Thickness(10, 4),
            IsEnabled = !_isDisabledByRun
        };
        _activeEditors[recipient.Id] = editor;

        void SyncCardState(string? text, bool isFocused)
        {
            var p = ThemeService.Current.CurrentPalette;
            bool customNow = !string.IsNullOrWhiteSpace(text);

            statusLabel.Text = customNow ? "✓ Custom" : "Default";
            statusLabel.TextColor = customNow ? p.Success : p.SecondaryText;
            statusBadge.BackgroundColor = customNow ? p.SuccessTint : p.CardBackground;
            statusBadge.Stroke = new SolidColorBrush(customNow ? p.Success.WithAlpha(0.45f) : p.Border);
            resetButton.IsVisible = customNow;

            if (isFocused)
            {
                cardBorder.Stroke = new SolidColorBrush(p.Primary);
                cardBorder.StrokeThickness = 1.6;
                editorBorder.Stroke = new SolidColorBrush(p.Primary);
            }
            else
            {
                cardBorder.Stroke = new SolidColorBrush(customNow ? p.Accent.WithAlpha(0.65f) : p.Border);
                cardBorder.StrokeThickness = customNow ? 1.4 : 1;
                editorBorder.Stroke = new SolidColorBrush(p.Border);
            }
        }

        editor.TextChanged += (_, e) =>
        {
            _settingsService.SetCustomMessageForRecipient(recipient.Id, e.NewTextValue);
            SyncCardState(e.NewTextValue, editor.IsFocused);
            UpdateHeaderBadge();
            BuildFilterChips();
        };

        editor.Focused += async (_, _) =>
        {
            SyncCardState(editor.Text, isFocused: true);
            if (!IsReducedMotionEnabled())
                await cardBorder.ScaleTo(1.01, 90, Easing.CubicOut);
        };

        editor.Unfocused += async (_, _) =>
        {
            _settingsService.SetCustomMessageForRecipient(recipient.Id, editor.Text);
            SyncCardState(editor.Text, isFocused: false);
            UpdateHeaderBadge();
            if (!IsReducedMotionEnabled())
                await cardBorder.ScaleTo(1.0, 90, Easing.CubicOut);
        };

        resetButton.Clicked += (_, _) =>
        {
            _settingsService.ResetCustomMessageForRecipient(recipient.Id);
            editor.Text = string.Empty;
            SyncCardState(string.Empty, editor.IsFocused);
            UpdateHeaderBadge();
            BuildFilterChips();
        };

        editorBorder.Content = editor;
        rootStack.Children.Add(editorBorder);
        cardBorder.Content = rootStack;
        return cardBorder;
    }

    private static bool IsReducedMotionEnabled()
    {
#if ANDROID
        try
        {
            var resolver = Android.App.Application.Context.ContentResolver;
            if (resolver != null)
            {
                var scale = Android.Provider.Settings.Global.GetFloat(
                    resolver,
                    Android.Provider.Settings.Global.AnimatorDurationScale,
                    1.0f);
                return scale <= 0.01f;
            }
        }
        catch { }
#endif
        return false;
    }
}
