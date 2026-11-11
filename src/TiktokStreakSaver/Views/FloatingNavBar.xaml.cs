namespace TiktokStreakSaver.Views;

/// <summary>
/// Custom floating pill navigation bar.
/// Handles tab switching via Shell routing, debounces navigation, and manages visual active-state indicators.
/// </summary>
[Microsoft.Maui.Controls.Internals.Preserve(AllMembers = true)]
public partial class FloatingNavBar : ContentView
{
    private static bool _isNavigating;
    private static readonly Color ActiveBg = Color.FromArgb("#25FF3B5C"); // Soft Coral-Red ambient glow
    private static readonly Color ActiveColor = Color.FromArgb("#FF3B5C"); // Vivid Coral-Red
    private static readonly Color InactiveColor = Color.FromArgb("#8E95A5"); // Discreet inactive gray

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
    }

    private void OnNavbarLoaded(object? sender, EventArgs e)
    {
        _isNavigating = false;
        UpdateVisualState(CurrentTab);
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
        // Reset all indicator backgrounds
        DashboardIndicator.BackgroundColor = Colors.Transparent;
        HistoryIndicator.BackgroundColor = Colors.Transparent;
        FriendsIndicator.BackgroundColor = Colors.Transparent;
        ProfileIndicator.BackgroundColor = Colors.Transparent;

        // Reset all icon fills
        DashboardIcon.Fill = new SolidColorBrush(InactiveColor);
        HistoryIcon.Fill = new SolidColorBrush(InactiveColor);
        FriendsIcon.Fill = new SolidColorBrush(InactiveColor);
        ProfileIcon.Fill = new SolidColorBrush(InactiveColor);

        // Reset all label colors
        DashboardLabel.TextColor = InactiveColor;
        HistoryLabel.TextColor = InactiveColor;
        FriendsLabel.TextColor = InactiveColor;
        ProfileLabel.TextColor = InactiveColor;

        // Apply active accent
        switch (activeTab)
        {
            case "History":
                HistoryIndicator.BackgroundColor = ActiveBg;
                HistoryIcon.Fill = new SolidColorBrush(ActiveColor);
                HistoryLabel.TextColor = ActiveColor;
                break;
            case "Friends":
                FriendsIndicator.BackgroundColor = ActiveBg;
                FriendsIcon.Fill = new SolidColorBrush(ActiveColor);
                FriendsLabel.TextColor = ActiveColor;
                break;
            case "Profile":
                ProfileIndicator.BackgroundColor = ActiveBg;
                ProfileIcon.Fill = new SolidColorBrush(ActiveColor);
                ProfileLabel.TextColor = ActiveColor;
                break;
            default:
                DashboardIndicator.BackgroundColor = ActiveBg;
                DashboardIcon.Fill = new SolidColorBrush(ActiveColor);
                DashboardLabel.TextColor = ActiveColor;
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
