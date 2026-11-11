using TiktokStreakSaver.Services;

namespace TiktokStreakSaver.Pages;

[Microsoft.Maui.Controls.Internals.Preserve(AllMembers = true)]
public partial class PrivacyPage : ContentPage
{
    private const string RepoUrl = "https://github.com/MrblackAIO/TikTok-Auto-Streak";
    private const string PolicyUrl = "https://github.com/MrblackAIO/TikTok-Auto-Streak/blob/main/PRIVACY.md";
    private const string IssuesUrl = "https://github.com/MrblackAIO/TikTok-Auto-Streak/issues";
    private const string ReleasesUrl = "https://github.com/MrblackAIO/TikTok-Auto-Streak/releases";
    private const string LicenseUrl = "https://github.com/MrblackAIO/TikTok-Auto-Streak/blob/main/LICENSE";

    private readonly SessionService _sessionService;
    private readonly SettingsService _settingsService;

    public PrivacyPage()
    {
        InitializeComponent();
        _sessionService = new SessionService();
        _settingsService = new SettingsService();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        this.Opacity = 0;
        this.TranslationY = 12;
        await Task.WhenAll(
            this.FadeTo(1, 280, Easing.SinInOut),
            this.TranslateTo(0, 0, 280, Easing.SinInOut));
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private async void OnViewOnGitHubClicked(object? sender, EventArgs e)
    {
        await OpenExternalUrlAsync(RepoUrl);
    }

    private async void OnIssuesClicked(object? sender, EventArgs e)
    {
        await OpenExternalUrlAsync(IssuesUrl);
    }

    private async void OnContactGitHubClicked(object? sender, EventArgs e)
    {
        await OpenExternalUrlAsync(IssuesUrl);
    }

    private async void OnReleasesClicked(object? sender, EventArgs e)
    {
        await OpenExternalUrlAsync(ReleasesUrl);
    }

    private async void OnPolicyDocClicked(object? sender, EventArgs e)
    {
        await OpenExternalUrlAsync(PolicyUrl);
    }

    private async void OnLicenseClicked(object? sender, EventArgs e)
    {
        await OpenExternalUrlAsync(LicenseUrl);
    }

    private async Task OpenExternalUrlAsync(string url)
    {
        try
        {
            await Launcher.OpenAsync(url);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Open Link", $"Could not open link: {ex.Message}", "OK");
        }
    }

    private async void OnClearDataClicked(object? sender, EventArgs e)
    {
        bool confirm = await DisplayAlert(
            "Purge Session & Local Data",
            "This will log you out, clear TikTok session cookies, and reset stored configuration on this device. Continue?",
            "Purge", "Cancel");

        if (confirm)
        {
            try
            {
                _sessionService.ClearSession();
                _settingsService.ClearRunHistory();
                SessionState.NotifyChanged();
                await DisplayAlert("Data Cleared", "All local session cookies and run history have been purged.", "OK");
                await Navigation.PopAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Could not purge data: {ex.Message}", "OK");
            }
        }
    }
}
