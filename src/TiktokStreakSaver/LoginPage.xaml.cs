using TiktokStreakSaver.Services;
using TiktokStreakSaver.Services.Storage;

namespace TiktokStreakSaver;

[Microsoft.Maui.Controls.Internals.Preserve(AllMembers = true)]
public partial class LoginPage : ContentPage
{
    private readonly SessionService _sessionService;
    private readonly SettingsService _settingsService;
    private bool _isLoggedIn;
    private bool _webViewTornDown;
    private bool _completionInProgress;
    private EventHandler? _webViewHandlerChanged;

    public LoginPage()
    {
        InitializeComponent();
        _sessionService = new SessionService();
        _settingsService = new SettingsService();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _webViewTornDown = false;
        _isLoggedIn = false;
        _completionInProgress = false;
        LoadTikTok();
        await TryCompleteExistingSessionAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        TearDownLoginWebView();
    }

    private void LoadTikTok()
    {
        LoadingOverlay.IsVisible = true;

        var desktopUa = AppConstants.DesktopChromeUserAgent;
#if ANDROID || IOS
        _sessionService.SetLoginUserAgent(desktopUa);

        if (TikTokWebViewHelper.TryConfigureWebView(TikTokWebView, desktopUa))
        {
            TikTokWebView.Source = TikTokWebViewHelper.LoginUrl;
            return;
        }

        _webViewHandlerChanged = (_, _) =>
        {
            if (_webViewTornDown ||
                !TikTokWebViewHelper.TryConfigureWebView(TikTokWebView, desktopUa))
                return;

            TikTokWebView.HandlerChanged -= _webViewHandlerChanged;
            _webViewHandlerChanged = null;
            TikTokWebView.Source = TikTokWebViewHelper.LoginUrl;
        };
        TikTokWebView.HandlerChanged += _webViewHandlerChanged;
#else
        TikTokWebView.Source = TikTokWebViewHelper.LoginUrl;
#endif
    }

    private void OnWebViewNavigating(object? sender, WebNavigatingEventArgs e)
    {
        // http(s) navigations are real pages. Custom schemes (snssdk://, intent://)
        // crash or white-screen the Android WebView with ERR_UNKNOWN_URL_SCHEME.
        if (e.Url != null && e.Url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            return;

        e.Cancel = true;

        const string key = "params_url=";
        var idx = e.Url?.IndexOf(key, StringComparison.Ordinal) ?? -1;
        if (idx < 0 || e.Url == null)
            return;

        var rest = e.Url.Substring(idx + key.Length);
        var amp = rest.IndexOf('&');
        if (amp > 0)
            rest = rest.Substring(0, amp);

        var decoded = Uri.UnescapeDataString(rest);
        if (decoded.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            TikTokWebView.Source = decoded;
    }

    private async Task TryCompleteExistingSessionAsync()
    {
        await Task.Delay(800);
        if (_isLoggedIn || _completionInProgress)
            return;

#if IOS
        Platforms.iOS.Services.IosWebViewConfigurator.AttachWebView(TikTokWebView);
        if (!await TikTokWebViewHelper.HasValidSessionCookieAsync(TikTokWebView))
            return;

        _isLoggedIn = true;
        await Platforms.iOS.Services.IosWebViewConfigurator.ExportCookiesFromCurrentWebViewAsync();
        await SessionRefreshHelper.RefreshAndGetRunReadyAsync(_sessionService);
        await Done(showSuccessAlert: false);
#elif ANDROID
        if (!TikTokWebViewHelper.HasValidSessionCookie())
            return;

        _isLoggedIn = true;
        await Done(showSuccessAlert: false);
#endif
    }

    private async void OnWebViewNavigated(object? sender, WebNavigatedEventArgs e)
    {
        if (_isLoggedIn || _completionInProgress) return;
        LoadingOverlay.IsVisible = false;

        bool hasSession;
#if IOS
        Platforms.iOS.Services.IosWebViewConfigurator.AttachWebView(TikTokWebView);
        hasSession = await TikTokWebViewHelper.HasValidSessionCookieAsync(TikTokWebView);
        if (!hasSession && TikTokWebViewHelper.CheckLoginStatus(e.Url).IsLoggedIn)
        {
            await Task.Delay(400);
            hasSession = await TikTokWebViewHelper.HasValidSessionCookieAsync(TikTokWebView);
        }
#else
        hasSession = TikTokWebViewHelper.HasValidSessionCookie();
#endif

        if (!hasSession)
            return;

        _isLoggedIn = true;
#if IOS
        await Platforms.iOS.Services.IosWebViewConfigurator.ExportCookiesFromCurrentWebViewAsync();
#endif
        await Done();
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        if (TikTokWebView.CanGoBack)
            TikTokWebView.GoBack();
        else
            await LeaveLoginPageAsync();
    }

    private void OnRefreshClicked(object? sender, EventArgs e)
    {
        LoadingOverlay.IsVisible = true;
        TikTokWebView.Reload();
    }

    private void TearDownLoginWebView()
    {
        if (_webViewTornDown)
            return;
        _webViewTornDown = true;
        if (_webViewHandlerChanged != null)
        {
            TikTokWebView.HandlerChanged -= _webViewHandlerChanged;
            _webViewHandlerChanged = null;
        }
        TikTokWebViewHelper.TearDownLoginWebView(TikTokWebView);
    }

    private async Task Done(bool showSuccessAlert = true)
    {
        if (_completionInProgress)
            return;
        _completionInProgress = true;

        if (_isLoggedIn)
        {
#if ANDROID
            TikTokWebViewHelper.FlushCookies();
#endif
            if (!_sessionService.TrySetSessionValid(true, out var persistError))
            {
                _completionInProgress = false;
                await DisplayAlert("Could Not Save Session",
                    persistError ?? "Login succeeded but session state did not persist on this device.", "OK");
                return;
            }

            // Extract TikTok profile info (avatar, nickname, username, bio, stats)
            await TryExtractTikTokProfileAsync();

            AppStorageProvider.Current.SetBool(AppConstants.AuthRequiredKey, false);

            bool justEnabled = false;
            if (!_settingsService.IsScheduled())
            {
#if ANDROID
                var context = Platform.CurrentActivity ?? Android.App.Application.Context;
                TiktokStreakSaver.Platforms.Android.StreakScheduler.ScheduleNextRun(context);
#endif
                justEnabled = true;
            }

            SessionState.NotifyChanged();

            if (showSuccessAlert)
            {
                var body = justEnabled
#if IOS
                    ? "You're logged in to TikTok! Set up a daily Shortcut (see Profile) to run streaks automatically."
#else
                    ? "You're logged in to TikTok! Background automation has been enabled — your streaks will run on the schedule set in Profile."
#endif
                    : "You're logged in to TikTok! The app will use this session for streak messaging.";
                await DisplayAlert("Logged In", body, "OK");
            }

            await LeaveLoginPageAsync();
        }
        else
        {
            _sessionService.TrySetSessionValid(false, out _);
            await DisplayAlert("Not Logged In",
                "Please login to TikTok first before continuing.", "OK");
            _completionInProgress = false;
        }
    }

    private async Task LeaveLoginPageAsync()
    {
        if (Navigation.NavigationStack.Count > 1)
            await Navigation.PopAsync();
        else if (Navigation.ModalStack.Count > 0)
            await Navigation.PopModalAsync();
        else if (Shell.Current != null)
            await Shell.Current.GoToAsync("//DashboardPage");
    }

    private async Task TryExtractTikTokProfileAsync()
    {
        try
        {
            const string extractScript = @"
(async function() {
    try {
        let res = await fetch('https://www.tiktok.com/passport/web/account/info/', { credentials: 'include' });
        let data = await res.json();
        if (data && data.data) {
            let uniqueId = data.data.unique_id || '';
            let avatarUrl = data.data.avatar_url || '';
            let nickname = data.data.nickname || uniqueId;
            let bio = '';
            let followers = 0;
            let following = 0;

            if (uniqueId) {
                try {
                    let detailRes = await fetch('https://www.tiktok.com/api/user/detail/?uniqueId=' + encodeURIComponent(uniqueId) + '&aid=1988', { credentials: 'include' });
                    let detailData = await detailRes.json();
                    if (detailData && detailData.userInfo) {
                        bio = detailData.userInfo.user?.signature || '';
                        nickname = detailData.userInfo.user?.nickname || nickname;
                        avatarUrl = detailData.userInfo.user?.avatarLarger || detailData.userInfo.user?.avatarMedium || avatarUrl;
                        followers = detailData.userInfo.stats?.followerCount || 0;
                        following = detailData.userInfo.stats?.followingCount || 0;
                    }
                } catch(e) {}
            }

            return JSON.stringify({
                username: uniqueId,
                nickname: nickname,
                avatarUrl: avatarUrl,
                bio: bio,
                followers: followers,
                following: following
            });
        }
    } catch(e) {}
    return '';
})();";

            var rawJson = await TikTokWebView.EvaluateJavaScriptAsync(extractScript);
            if (!string.IsNullOrWhiteSpace(rawJson) && rawJson != "\"\"" && rawJson != "null")
            {
                string jsonString = rawJson;
                if (jsonString.StartsWith("\"") && jsonString.EndsWith("\""))
                {
                    try
                    {
                        jsonString = System.Text.Json.JsonSerializer.Deserialize<string>(rawJson) ?? rawJson;
                    }
                    catch { }
                }

                using var doc = System.Text.Json.JsonDocument.Parse(jsonString);
                var root = doc.RootElement;
                if (root.TryGetProperty("username", out var uProp) && !string.IsNullOrEmpty(uProp.GetString()))
                {
                    var username = uProp.GetString()!;
                    _sessionService.SetTikTokUsername(username);

                    if (root.TryGetProperty("nickname", out var nProp) && !string.IsNullOrEmpty(nProp.GetString()))
                    {
                        var nick = nProp.GetString()!;
                        _sessionService.SetTikTokNickname(nick);
                        _sessionService.SetDisplayName(nick);
                    }

                    if (root.TryGetProperty("bio", out var bProp))
                        _sessionService.SetTikTokBio(bProp.GetString() ?? "");

                    if (root.TryGetProperty("followers", out var fProp))
                        _sessionService.SetTikTokFollowers(fProp.GetInt32());

                    if (root.TryGetProperty("following", out var fgProp))
                        _sessionService.SetTikTokFollowing(fgProp.GetInt32());

                    if (root.TryGetProperty("avatarUrl", out var aProp) && !string.IsNullOrEmpty(aProp.GetString()))
                    {
                        var avatarUrl = aProp.GetString()!;
                        await DownloadAndSaveAvatarAsync(avatarUrl);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LoginPage] Failed to extract TikTok profile: {ex.Message}");
        }
    }

    private async Task DownloadAndSaveAvatarAsync(string avatarUrl)
    {
        try
        {
            using var client = new System.Net.Http.HttpClient();
            client.Timeout = TimeSpan.FromSeconds(10);
            var imageBytes = await client.GetByteArrayAsync(avatarUrl);
            var localPath = System.IO.Path.Combine(FileSystem.AppDataDirectory, "tiktok_avatar.jpg");
            await System.IO.File.WriteAllBytesAsync(localPath, imageBytes);
            _sessionService.SetProfileImagePath(localPath);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LoginPage] Failed to download avatar image: {ex.Message}");
        }
    }
}
