namespace TiktokStreakSaver.Views;

/// <summary>
/// Custom floating pill navigation bar.
/// Handles tab switching via Shell routing and manages visual active-state indicators.
/// </summary>
[Microsoft.Maui.Controls.Internals.Preserve(AllMembers = true)]
public partial class FloatingNavBar : ContentView
{
    /// <summary>
    /// Bindable property to set which tab is currently selected.
    /// Values: "Dashboard", "History", "Friends", "Profile"
    /// </summary>
    public static readonly BindableProperty CurrentTabProperty =
        BindableProperty.Create(nameof(CurrentTab), typeof(string), typeof(FloatingNavBar), "Dashboard",
            propertyChanged: OnCurrentTabChanged);

    public string CurrentTab
    {
        get => (string)GetValue(CurrentTabProperty);
        set => SetValue(CurrentTabProperty, value);
    }

    public FloatingNavBar()
    {
        InitializeComponent();
    }

    protected override void OnParentSet()
    {
        base.OnParentSet();
        UpdateVisualState(CurrentTab);
    }

    private static void OnCurrentTabChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is FloatingNavBar nav && newValue is string tab)
            nav.UpdateVisualState(tab);
    }

    private Color GetThemeColor(string key, string fallbackHex)
    {
        if (Application.Current != null && Application.Current.Resources.TryGetValue(key, out var resource) && resource is Color color)
            return color;
        return Color.FromArgb(fallbackHex);
    }

    private void UpdateVisualState(string activeTab)
    {
        var activeBg = Color.FromArgb("#25FF3B5C"); // Soft Coral-Red ambient glow
        var activeText = Color.FromArgb("#FF3B5C");  // Vivid Coral-Red
        var inactiveText = Color.FromArgb("#8E95A5");// Discreet inactive gray

        DashboardIndicator.BackgroundColor = Colors.Transparent;
        HistoryIndicator.BackgroundColor = Colors.Transparent;
        FriendsIndicator.BackgroundColor = Colors.Transparent;
        ProfileIndicator.BackgroundColor = Colors.Transparent;

        DashboardIcon.Fill = new SolidColorBrush(inactiveText);
        HistoryIcon.Fill = new SolidColorBrush(inactiveText);
        FriendsIcon.Fill = new SolidColorBrush(inactiveText);
        ProfileIcon.Fill = new SolidColorBrush(inactiveText);

        DashboardLabel.TextColor = inactiveText;
        HistoryLabel.TextColor = inactiveText;
        FriendsLabel.TextColor = inactiveText;
        ProfileLabel.TextColor = inactiveText;

        Border activeIndicator;
        Microsoft.Maui.Controls.Shapes.Path activeIcon;
        Label activeLabel;
        switch (activeTab)
        {
            case "History":
                activeIndicator = HistoryIndicator; activeIcon = HistoryIcon; activeLabel = HistoryLabel; break;
            case "Friends":
                activeIndicator = FriendsIndicator; activeIcon = FriendsIcon; activeLabel = FriendsLabel; break;
            case "Profile":
                activeIndicator = ProfileIndicator; activeIcon = ProfileIcon; activeLabel = ProfileLabel; break;
            default:
                activeIndicator = DashboardIndicator; activeIcon = DashboardIcon; activeLabel = DashboardLabel; break;
        }
        activeIndicator.BackgroundColor = activeBg;
        activeIcon.Fill = new SolidColorBrush(activeText);
        activeLabel.TextColor = activeText;
    }

    private void OnDashboardTapped(object? sender, TappedEventArgs e)
    {
        if (CurrentTab == "Dashboard") return;
        UpdateVisualState("Dashboard");
        _ = AnimatePress(DashboardIndicator);
        _ = Shell.Current.GoToAsync("//DashboardPage", false);
    }

    private void OnHistoryTapped(object? sender, TappedEventArgs e)
    {
        if (CurrentTab == "History") return;
        UpdateVisualState("History");
        _ = AnimatePress(HistoryIndicator);
        _ = Shell.Current.GoToAsync("//HistoryPage", false);
    }

    private void OnFriendsTapped(object? sender, TappedEventArgs e)
    {
        if (CurrentTab == "Friends") return;
        UpdateVisualState("Friends");
        _ = AnimatePress(FriendsIndicator);
        _ = Shell.Current.GoToAsync("//FriendsPage", false);
    }

    private void OnProfileTapped(object? sender, TappedEventArgs e)
    {
        if (CurrentTab == "Profile") return;
        UpdateVisualState("Profile");
        _ = AnimatePress(ProfileIndicator);
        _ = Shell.Current.GoToAsync("//ProfilePage", false);
    }

    private static async Task AnimatePress(View target)
    {
        await target.ScaleTo(0.85, 60, Easing.CubicIn);
        await target.ScaleTo(1.0, 100, Easing.CubicOut);
    }
}
