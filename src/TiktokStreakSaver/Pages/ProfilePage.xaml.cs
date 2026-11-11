using Microsoft.Maui.Controls.Shapes;
using TiktokStreakSaver.Services;
using TiktokStreakSaver.Views;

namespace TiktokStreakSaver.Pages;

[Microsoft.Maui.Controls.Internals.Preserve(AllMembers = true)]
public partial class ProfilePage : ContentPage
{
    private readonly SessionService _sessionService;
    private readonly SettingsService _settingsService;
    private bool _suppressIntervalChanged = false;
    private bool _suppressRandomizeToggle = false;
    private bool _suppressChatPriorityChanged = false;
#if ANDROID
    private readonly UpdateService _updateService;
    private bool _isCheckingUpdates = false;
#endif
    private readonly RadialTimerDrawable _radialTimerDrawable = new RadialTimerDrawable();
    private bool _suppressTimeInputChanged = false;

    public ProfilePage()
    {
        InitializeComponent();
        _sessionService = new SessionService();
        _settingsService = new SettingsService();
        RadialTimerGraphicsView.Drawable = _radialTimerDrawable;
#if ANDROID
        _updateService = new UpdateService();
#endif
    }

    private void OnSessionStateChanged(object? sender, EventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
            await UpdateLoginButtonStateAsync());
    }

    private Color GetThemeColor(string key, string fallbackHex = "#92979E")
    {
        if (Application.Current != null && Application.Current.Resources.TryGetValue(key, out var resource) && resource is Color color)
            return color;
        return Color.FromArgb(fallbackHex);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        SessionState.Changed -= OnSessionStateChanged;
        SessionState.Changed += OnSessionStateChanged;

        LoadProfilePhoto();

        DisplayNameEntry.Text = _sessionService.GetDisplayName();

        ScheduleSwitch.IsToggled = _settingsService.IsScheduled();
        SkipUnreachableSwitch.IsToggled = _settingsService.GetSkipUnreachableUsers();
        _suppressRandomizeToggle = true;
        try
        {
            RandomizeMessagesSwitch.IsToggled = _settingsService.GetRandomizeNormalMessages();
        }
        finally
        {
            _suppressRandomizeToggle = false;
        }
        SendOnBatteryLowSwitch.IsToggled = _settingsService.GetSendOnBatteryLow();

        _suppressChatPriorityChanged = true;
        ChatPriorityPicker.SelectedIndex = _settingsService.GetChatPriority() switch
        {
            SettingsService.ChatPriorityFriendsFirst => 1,
            SettingsService.ChatPriorityGroupsFirst => 2,
            _ => 0
        };
        _suppressChatPriorityChanged = false;

        FixedTimeSwitch.IsToggled = _settingsService.GetUseFixedTime();
        var scheduleTime = new TimeSpan(
            _settingsService.GetFixedTimeHour(),
            _settingsService.GetFixedTimeMinute(), 0);
        ScheduleTimePicker.Time = scheduleTime;
        UpdateRadialTimerDisplay(scheduleTime);
        ScheduleOptionsPanel.IsVisible = ScheduleSwitch.IsToggled;
        TimePickerRow.IsVisible = FixedTimeSwitch.IsToggled;
        IntervalPanel.IsVisible = !FixedTimeSwitch.IsToggled;

        _suppressIntervalChanged = true;
        try
        {
            var hours = _settingsService.GetIntervalHours();
            IntervalHoursStepper.Value = hours;
            UpdateIntervalLabels();
        }
        finally
        {
            _suppressIntervalChanged = false;
        }

        VersionLabel.Text = $"v{AppInfo.Current.VersionString} (Premium) • Signed Version";

#if IOS
        IosShortcutsSettingsPanel.IsVisible = true;
        AndroidSchedulingPanel.IsVisible = false;
        CheckUpdatesButton.IsVisible = false;
        AltStoreUpdatesLabel.IsVisible = true;
#else
        IosShortcutsSettingsPanel.IsVisible = false;
        AndroidSchedulingPanel.IsVisible = true;
#endif

        await UpdateLoginButtonStateAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        SessionState.Changed -= OnSessionStateChanged;
    }

    private void LoadProfilePhoto()
    {
        var photoPath = _sessionService.GetProfileImagePath();
        if (!string.IsNullOrEmpty(photoPath) && System.IO.File.Exists(photoPath))
        {
            ProfilePhoto.Source = ImageSource.FromFile(photoPath);
            ProfilePhoto.IsVisible = true;
            ProfileEmoji.IsVisible = false;
            ProfilePhoto.Clip = new EllipseGeometry
            {
                Center = new Point(36, 36),
                RadiusX = 36,
                RadiusY = 36
            };
        }
        else
        {
            ProfilePhoto.IsVisible = false;
            ProfileEmoji.Text = "🔥";
            ProfileEmoji.IsVisible = true;
        }
    }

    private async void OnProfilePhotoTapped(object? sender, TappedEventArgs e)
    {
        try
        {
            var result = await MediaPicker.Default.PickPhotoAsync(new MediaPickerOptions
            {
                Title = "Please pick a photo"
            });

            if (result != null)
            {
                var newFile = System.IO.Path.Combine(FileSystem.AppDataDirectory, result.FileName);
                var oldPath = _sessionService.GetProfileImagePath();
                if (!string.IsNullOrEmpty(oldPath) && oldPath != newFile && System.IO.File.Exists(oldPath))
                {
                    try { System.IO.File.Delete(oldPath); } catch { }
                }

                using (var stream = await result.OpenReadAsync())
                using (var newStream = System.IO.File.Create(newFile))
                    await stream.CopyToAsync(newStream);

                _sessionService.SetProfileImagePath(newFile);
                LoadProfilePhoto();
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Photo", $"Could not pick photo: {ex.Message}", "OK");
        }
    }

    private void OnDisplayNameChanged(object? sender, EventArgs e)
    {
        _sessionService.SetDisplayName(DisplayNameEntry.Text ?? "User");
    }

    private async Task UpdateLoginButtonStateAsync()
    {
        bool runReady = await SessionRefreshHelper.RefreshAndGetRunReadyAsync(_sessionService);
        await ApplyLoginButtonStateAsync(runReady);
    }

    private async Task ApplyLoginButtonStateAsync(bool isSessionValid)
    {
        if (isSessionValid)
        {
            LoggedOutPanel.IsVisible = false;
            LoggedInProfilePanel.IsVisible = true;
            SessionCardTitle.Text = "TikTok Profile";
            SessionDot.BackgroundColor = GetThemeColor("Success", "#10B981");

            var nick = _sessionService.GetTikTokNickname();
            if (string.IsNullOrWhiteSpace(nick))
                nick = _sessionService.GetDisplayName();
            ProfileNicknameLabel.Text = string.IsNullOrWhiteSpace(nick) ? "User" : nick;

            var username = _sessionService.GetTikTokUsername();
            ProfileUsernameLabel.Text = string.IsNullOrWhiteSpace(username) ? "@tiktok_user" : $"@{username}";

            var bio = _sessionService.GetTikTokBio();
            BioLabel.Text = string.IsNullOrWhiteSpace(bio) ? "TikTok Streak Automation Operator 🔥" : bio;

            int followers = _sessionService.GetTikTokFollowers();
            FollowersCountLabel.Text = FormatMetricCount(followers);

            int following = _sessionService.GetTikTokFollowing();
            FollowingCountLabel.Text = FormatMetricCount(following);

            var lastCheck = _sessionService.GetLastCheckTime();
            SessionLastCheckLabel.Text = lastCheck.HasValue ? $"Verified {lastCheck.Value:MMM dd, HH:mm}" : "Verified and ready";

            LoadProfilePhoto();
        }
        else
        {
            LoggedOutPanel.IsVisible = true;
            LoggedInProfilePanel.IsVisible = false;
            SessionCardTitle.Text = "Session Settings";
            SessionDot.BackgroundColor = GetThemeColor("CoralRed", "#FF3B5C");
            DisplayNameEntry.Text = _sessionService.GetDisplayName();
        }
    }

    private static string FormatMetricCount(int count)
    {
        if (count >= 1_000_000)
            return $"{(count / 1_000_000.0):0.#}M";
        if (count >= 1_000)
            return $"{(count / 1_000.0):0.#}K";
        return count > 0 ? count.ToString() : "--";
    }

    private async void UpdateLoginButtonState(bool isSessionValid)
    {
        await ApplyLoginButtonStateAsync(isSessionValid);
    }

    private async void OnLoginClicked(object? sender, EventArgs e)
    {
        await Navigation.PushAsync(new LoginPage());
        await UpdateLoginButtonStateAsync();
    }

    private async void OnShortcutTutorialClicked(object? sender, EventArgs e)
    {
        await Navigation.PushAsync(new IosShortcutTutorialPage());
    }

    private async void OnIosLimitationsClicked(object? sender, EventArgs e)
    {
        await Navigation.PushAsync(new IosLimitationsPage());
    }

    private void OnScheduleToggled(object? sender, ToggledEventArgs e)
    {
        ScheduleOptionsPanel.IsVisible = e.Value;
        _settingsService.SetScheduled(e.Value);
#if ANDROID
        var context = Platform.CurrentActivity ?? Android.App.Application.Context;
        if (e.Value)
            TiktokStreakSaver.Platforms.Android.StreakScheduler.ScheduleNextRun(context);
        else
            TiktokStreakSaver.Platforms.Android.StreakScheduler.CancelSchedule(context);
#endif
    }

    private void OnFixedTimeToggled(object? sender, ToggledEventArgs e)
    {
        _settingsService.SetUseFixedTime(e.Value);
        TimePickerRow.IsVisible = e.Value;
        IntervalPanel.IsVisible = !e.Value;
#if ANDROID
        if (_settingsService.IsScheduled())
        {
            var context = Platform.CurrentActivity ?? Android.App.Application.Context;
            TiktokStreakSaver.Platforms.Android.StreakScheduler.ScheduleNextRun(context);
        }
#endif
    }

    private void OnTimePickerChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(TimePicker.Time)) return;
        var time = ScheduleTimePicker.Time;
        _settingsService.SetFixedTimeHour(time.Hours);
        _settingsService.SetFixedTimeMinute(time.Minutes);
        UpdateRadialTimerDisplay(time);
#if ANDROID
        if (_settingsService.IsScheduled() && _settingsService.GetUseFixedTime())
        {
            var context = Platform.CurrentActivity ?? Android.App.Application.Context;
            TiktokStreakSaver.Platforms.Android.StreakScheduler.ScheduleNextRun(context);
        }
#endif
    }

    private void UpdateRadialTimerDisplay(TimeSpan time)
    {
        var hour = time.Hours;
        var minute = time.Minutes;
        var period = hour >= 12 ? "PM" : "AM";
        var displayHour = hour % 12;
        if (displayHour == 0) displayHour = 12;

        _radialTimerDrawable.TimeText = $"{displayHour:D2}:{minute:D2}";
        _radialTimerDrawable.PeriodText = period;
        _radialTimerDrawable.Progress = (float)(time.TotalMinutes / 1440.0);
        RadialTimerGraphicsView.Invalidate();

        _suppressTimeInputChanged = true;
        try
        {
            ScheduleHourEntry.Text = $"{displayHour:D2}";
            ScheduleMinuteEntry.Text = $"{minute:D2}";
            UpdateAmPmPills(period == "AM");
        }
        finally
        {
            _suppressTimeInputChanged = false;
        }
    }

    private void UpdateAmPmPills(bool isAm)
    {
        if (Application.Current?.Resources == null) return;
        var purple = (Color)Application.Current.Resources["VioletPurple"];
        var border = (Brush)Application.Current.Resources["ObsidianBorder"];
        var elevated = (Color)Application.Current.Resources["ObsidianSurfaceElevated"];
        var muted = (Color)Application.Current.Resources["TextSecondary"];

        if (isAm)
        {
            AmPill.BackgroundColor = Color.FromArgb("#268B5CF6");
            AmPill.Stroke = new SolidColorBrush(purple);
            AmPillLabel.TextColor = purple;

            PmPill.BackgroundColor = elevated;
            PmPill.Stroke = border;
            PmPillLabel.TextColor = muted;
        }
        else
        {
            PmPill.BackgroundColor = Color.FromArgb("#268B5CF6");
            PmPill.Stroke = new SolidColorBrush(purple);
            PmPillLabel.TextColor = purple;

            AmPill.BackgroundColor = elevated;
            AmPill.Stroke = border;
            AmPillLabel.TextColor = muted;
        }
    }

    private void OnScheduleTimeInputChanged(object? sender, TextChangedEventArgs e)
    {
        if (_suppressTimeInputChanged) return;

        if (int.TryParse(ScheduleHourEntry.Text, out var h) &&
            int.TryParse(ScheduleMinuteEntry.Text, out var m))
        {
            bool isPm = _radialTimerDrawable.PeriodText == "PM";
            int hour24;
            if (h >= 13)
            {
                hour24 = Math.Clamp(h, 0, 23);
                isPm = hour24 >= 12;
            }
            else
            {
                var h12 = Math.Clamp(h, 1, 12);
                hour24 = (h12 % 12) + (isPm ? 12 : 0);
            }

            int minute = Math.Clamp(m, 0, 59);
            var newTime = new TimeSpan(hour24, minute, 0);

            ScheduleTimePicker.Time = newTime;
            _settingsService.SetFixedTimeHour(hour24);
            _settingsService.SetFixedTimeMinute(minute);

            var displayHour = hour24 % 12;
            if (displayHour == 0) displayHour = 12;

            _radialTimerDrawable.TimeText = $"{displayHour:D2}:{minute:D2}";
            _radialTimerDrawable.PeriodText = isPm ? "PM" : "AM";
            _radialTimerDrawable.Progress = (float)(newTime.TotalMinutes / 1440.0);
            RadialTimerGraphicsView.Invalidate();
            UpdateAmPmPills(!isPm);

#if ANDROID
            if (_settingsService.IsScheduled() && _settingsService.GetUseFixedTime())
            {
                var context = Platform.CurrentActivity ?? Android.App.Application.Context;
                TiktokStreakSaver.Platforms.Android.StreakScheduler.ScheduleNextRun(context);
            }
#endif
        }
    }

    private void OnAmTapped(object? sender, TappedEventArgs e)
    {
        var time = ScheduleTimePicker.Time;
        if (time.Hours >= 12)
        {
            var newTime = new TimeSpan(time.Hours - 12, time.Minutes, 0);
            ScheduleTimePicker.Time = newTime;
            _settingsService.SetFixedTimeHour(newTime.Hours);
            UpdateRadialTimerDisplay(newTime);
        }
    }

    private void OnPmTapped(object? sender, TappedEventArgs e)
    {
        var time = ScheduleTimePicker.Time;
        if (time.Hours < 12)
        {
            var newTime = new TimeSpan(time.Hours + 12, time.Minutes, 0);
            ScheduleTimePicker.Time = newTime;
            _settingsService.SetFixedTimeHour(newTime.Hours);
            UpdateRadialTimerDisplay(newTime);
        }
    }

    private void OnIntervalHoursChanged(object? sender, ValueChangedEventArgs e)
    {
        if (_suppressIntervalChanged) return;
        var hours = (int)Math.Round(e.NewValue);
        _settingsService.SetIntervalHours(hours);
        UpdateIntervalLabels();
#if ANDROID
        if (_settingsService.IsScheduled())
        {
            var ctx = Platform.CurrentActivity ?? Android.App.Application.Context;
            TiktokStreakSaver.Platforms.Android.StreakScheduler.ScheduleNextRun(ctx);
        }
#endif
    }

    private void UpdateIntervalLabels()
    {
        var hours = _settingsService.GetIntervalHours();
        IntervalHoursValueLabel.Text = hours.ToString();
        IntervalSummaryLabel.Text = hours == 1
            ? "Every hour between automatic runs."
            : $"Every {hours} hours between automatic runs.";
    }

    private void OnSkipUnreachableToggled(object? sender, ToggledEventArgs e)
    {
        _settingsService.SetSkipUnreachableUsers(e.Value);
    }

    private void OnRandomizeMessagesToggled(object? sender, ToggledEventArgs e)
    {
        if (_suppressRandomizeToggle)
            return;

        _settingsService.SetRandomizeNormalMessages(e.Value);
        SessionState.NotifyChanged();
    }

    private void OnChatPriorityChanged(object? sender, EventArgs e)
    {
        if (_suppressChatPriorityChanged)
            return;

        var value = ChatPriorityPicker.SelectedIndex switch
        {
            1 => SettingsService.ChatPriorityFriendsFirst,
            2 => SettingsService.ChatPriorityGroupsFirst,
            _ => SettingsService.ChatPriorityMixed
        };
        _settingsService.SetChatPriority(value);
    }

    private void OnSendOnBatteryLowToggled(object? sender, ToggledEventArgs e)
    {
        _settingsService.SetSendOnBatteryLow(e.Value);
    }

    private async void OnAboutClicked(object? sender, EventArgs e)
    {
        string currentVersion = AppInfo.Current.VersionString;
        await Navigation.PushModalAsync(new AboutPopupPage(
            "TikTok AutoStreak", currentVersion, string.Empty, false));
    }

#if ANDROID
    private async void OnCheckUpdatesClicked(object? sender, EventArgs e)
    {
        if (_isCheckingUpdates) return;
        _isCheckingUpdates = true;
        var originalText = CheckUpdatesButton.Text;
        try
        {
            CheckUpdatesButton.IsEnabled = false;
            CheckUpdatesButton.Text = "Checking…";

            var info = await _updateService.CheckForUpdatesAsync();
            if (info == null)
            {
                await DisplayAlert("Update Check", "Could not reach the update server. Please try again later.", "OK");
                return;
            }

            string currentVersion = AppInfo.Current.VersionString;
            string remoteVersion = info.LatestVersion.StartsWith("v", StringComparison.OrdinalIgnoreCase)
                ? info.LatestVersion.Substring(1)
                : info.LatestVersion;

            if (info.HasUpdate)
            {
                await Navigation.PushModalAsync(new AboutPopupPage(
                    "Update Available!", remoteVersion, info.Changelog, true, info.ApkDownloadUrl));
            }
            else
            {
                await DisplayAlert("Up to Date", $"You are already on the latest version (v{currentVersion}).", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Update Check", $"Update check failed: {ex.Message}", "OK");
        }
        finally
        {
            CheckUpdatesButton.Text = originalText;
            CheckUpdatesButton.IsEnabled = true;
            _isCheckingUpdates = false;
        }
    }
#else
    private void OnCheckUpdatesClicked(object? sender, EventArgs e) { }
#endif

    private async void OnPrivacyPolicyClicked(object? sender, EventArgs e)
    {
        await Navigation.PushAsync(new PrivacyPage());
    }

    private async void OnLogoutClicked(object? sender, EventArgs e)
    {
        bool confirm = await DisplayAlert("Logout", "This will clear your TikTok session. You'll need to login again before running automations.", "Logout", "Cancel");
        if (confirm)
        {
            _sessionService.ClearSession();
            UpdateLoginButtonState(false);
        }
    }
}
