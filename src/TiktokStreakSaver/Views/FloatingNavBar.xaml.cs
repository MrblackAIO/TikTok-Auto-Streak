namespace TiktokStreakSaver.Views;

/// <summary>
/// Custom floating pill navigation bar.
/// Handles tab switching via Shell routing, debounces navigation, and manages visual active-state indicators.
/// </summary>
[Microsoft.Maui.Controls.Internals.Preserve(AllMembers = true)]
public partial class FloatingNavBar : ContentView
{
    private static bool _isNavigating;
    private bool _subscribedTheme;

    /// <summary>
    /// Bindable property to set which tab is currently selected.
    /// Values: "Dashboard", "History", "Friends", "Profile"
    /// </summary>
    public static readonly BindableProperty CurrentTabProperty =
        BindableProperty.Create(
            nameof(CurrentTab),
            typeof(string),
            typeof(FloatingNavBar),
            "Dashboard",
            propertyChanged: OnCurrentTabChanged);

    public string CurrentTab
    {
        get => (string)GetValue(CurrentTabProperty);
        set => SetValue(CurrentTabProperty, value);
    }

    public FloatingNavBar()
    {
        InitializeComponent();
        Loaded += OnNavbarLoaded;
        Unloaded += OnNavbarUnloaded;
    }

    private void OnNavbarLoaded(object? sender, EventArgs e)
    {
        _isNavigating = false;
        if (!_subscribedTheme)
        {
            Services.ThemeService.Current.ThemeChanged += OnThemeChanged;
            _subscribedTheme = true;
        }
        UpdateVisualState(CurrentTab);
    }

    private void OnNavbarUnloaded(object? sender, EventArgs e)
    {
        if (_subscribedTheme)
        {
            Services.ThemeService.Current.ThemeChanged -= OnThemeChanged;
            _subscribedTheme = false;
        }
    }

    private void OnThemeChanged(object? sender, EventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() => UpdateVisualState(CurrentTab));
    }

    protected override void OnParentSet()
    {
        base.OnParentSet();
        _isNavigating = false;
        UpdateVisualState(CurrentTab);
    }

    private static void OnCurrentTabChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is FloatingNavBar nav && newValue is string tab)
            nav.UpdateVisualState(tab);
    }

    public void UpdateVisualState(string activeTab)
    {
        var palette = Services.ThemeService.Current.CurrentPalette;
        var activeColor = palette.Primary;
        var activeBg = palette.PrimaryTint;
        var inactiveColor = palette.SecondaryText;

        // Reset all indicator backgrounds
        DashboardIndicator.BackgroundColor = Colors.Transparent;
        HistoryIndicator.BackgroundColor = Colors.Transparent;
        FriendsIndicator.BackgroundColor = Colors.Transparent;
        ProfileIndicator.BackgroundColor = Colors.Transparent;

        // Reset all icon fills
        DashboardIcon.Fill = new SolidColorBrush(inactiveColor);
        HistoryIcon.Fill = new SolidColorBrush(inactiveColor);
        FriendsIcon.Fill = new SolidColorBrush(inactiveColor);
        ProfileIcon.Fill = new SolidColorBrush(inactiveColor);

        // Reset all label colors
        DashboardLabel.TextColor = inactiveColor;
        HistoryLabel.TextColor = inactiveColor;
        FriendsLabel.TextColor = inactiveColor;
        ProfileLabel.TextColor = inactiveColor;

        // Apply active accent
        switch (activeTab)
        {
            case "History":
                HistoryIndicator.BackgroundColor = activeBg;
                HistoryIcon.Fill = new SolidColorBrush(activeColor);
                HistoryLabel.TextColor = activeColor;
                break;
            case "Friends":
                FriendsIndicator.BackgroundColor = activeBg;
                FriendsIcon.Fill = new SolidColorBrush(activeColor);
                FriendsLabel.TextColor = activeColor;
                break;
            case "Profile":
                ProfileIndicator.BackgroundColor = activeBg;
                ProfileIcon.Fill = new SolidColorBrush(activeColor);
                ProfileLabel.TextColor = activeColor;
                break;
            default:
                DashboardIndicator.BackgroundColor = activeBg;
                DashboardIcon.Fill = new SolidColorBrush(activeColor);
                DashboardLabel.TextColor = activeColor;
                break;
        }
    }

    private void OnDashboardTapped(object? sender, TappedEventArgs e) =>
        NavigateTo("Dashboard", "//DashboardPage", DashboardIndicator);

    private void OnHistoryTapped(object? sender, TappedEventArgs e) =>
        NavigateTo("History", "//HistoryPage", HistoryIndicator);

    private void OnFriendsTapped(object? sender, TappedEventArgs e) =>
        NavigateTo("Friends", "//FriendsPage", FriendsIndicator);

    private void OnProfileTapped(object? sender, TappedEventArgs e) =>
        NavigateTo("Profile", "//ProfilePage", ProfileIndicator);

    private async void NavigateTo(string targetTab, string route, View indicator)
    {
        if (CurrentTab == targetTab || _isNavigating)
            return;

        _isNavigating = true;
        UpdateVisualState(targetTab);

        // Immediate tactile micro-feedback
        PlayPressAnimation(indicator);

        try
        {
            if (Shell.Current != null)
            {
                var navTask = Shell.Current.GoToAsync(route, false);
                await Task.WhenAny(navTask, Task.Delay(2000));
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[FloatingNavBar] Navigation to {route} failed: {ex.Message}");
        }
        finally
        {
            // Restore visual state back to this page's own CurrentTab in case navigation failed
            // or when this page is navigated back to later.
            UpdateVisualState(CurrentTab);
            _isNavigating = false;
        }
    }

    private static async void PlayPressAnimation(View indicator)
    {
        try
        {
            await indicator.ScaleTo(0.88, 45, Easing.CubicIn);
            await indicator.ScaleTo(1.0, 65, Easing.CubicOut);
        }
        catch
        {
            indicator.Scale = 1.0;
        }
    }
}
