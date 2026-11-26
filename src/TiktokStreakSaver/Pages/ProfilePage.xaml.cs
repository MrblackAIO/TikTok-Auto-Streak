using Microsoft.Maui.Controls.Shapes;
using TiktokStreakSaver.Models;
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
    private bool _suppressTimePickerChanged = false;

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

    private void OnThemeChanged(object? sender, EventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            UpdateAppearanceCard();
            UpdateAmPmPills(_radialTimerDrawable.PeriodText == "AM");
            RadialTimerGraphicsView.Invalidate();
        });
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            SessionState.Changed -= OnSessionStateChanged;
            SessionState.Changed += OnSessionStateChanged;
            ThemeService.Current.ThemeChanged -= OnThemeChanged;
            ThemeService.Current.ThemeChanged += OnThemeChanged;

            UpdateAppearanceCard();
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

            _suppressTimePickerChanged = true;
            try
            {
                ScheduleTimePicker.Time = scheduleTime;
            }
            finally
            {
                _suppressTimePickerChanged = false;
            }

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

            if (_sessionService.IsSessionValid() && string.IsNullOrEmpty(_sessionService.GetTikTokUsername()))
            {
                TriggerSilentProfileSync();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ProfilePage] OnAppearing error: {ex}");
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        SessionState.Changed -= OnSessionStateChanged;
        ThemeService.Current.ThemeChanged -= OnThemeChanged;
    }

    private void UpdateAppearanceCard()
    {
        var svc = ThemeService.Current;
        var palette = svc.CurrentPalette;
        var preset = ThemeCatalog.GetPreset(svc.CurrentPresetId);
        var modeText = svc.CurrentMode switch
        {
            AppThemeMode.Dark => "Dark Mode",
            AppThemeMode.Light => "Light Mode",
            _ => "Follow System"
        };

        ActiveThemeSummaryLabel.Text = $"{preset.Name} • {modeText}";
        ActiveThemeBadgeLabel.Text = svc.CustomColors.HasAnyOverride
            ? "Custom"
            : (svc.CurrentPresetId == ThemeCatalog.DefaultPresetId && svc.CurrentMode == AppThemeMode.Dark ? "Default" : preset.Name);

        QuickDarkPill.BackgroundColor = svc.CurrentMode == AppThemeMode.Dark ? palette.Primary : Colors.Transparent;
        QuickDarkLabel.TextColor = svc.CurrentMode == AppThemeMode.Dark ? palette.OnPrimary : palette.SecondaryText;

        QuickLightPill.BackgroundColor = svc.CurrentMode == AppThemeMode.Light ? palette.Primary : Colors.Transparent;
        QuickLightLabel.TextColor = svc.CurrentMode == AppThemeMode.Light ? palette.OnPrimary : palette.SecondaryText;

        QuickSystemPill.BackgroundColor = svc.CurrentMode == AppThemeMode.System ? palette.Primary : Colors.Transparent;
        QuickSystemLabel.TextColor = svc.CurrentMode == AppThemeMode.System ? palette.OnPrimary : palette.SecondaryText;

        QuickPresetsRow.Children.Clear();
        foreach (var item in ThemeCatalog.Presets)
        {
            var isSelected = item.Id.Equals(svc.CurrentPresetId, StringComparison.OrdinalIgnoreCase);
            var preview = palette.IsLight ? item.LightPalette : item.DarkPalette;

            var chip = new Border
            {
                StrokeThickness = isSelected ? 1.5 : 1,
                Stroke = new SolidColorBrush(isSelected ? palette.Primary : palette.Border),
                StrokeShape = new RoundRectangle { CornerRadius = 12 },
                BackgroundColor = isSelected ? palette.PrimarySubtle : palette.SecondaryBackground,
                Padding = new Thickness(10, 7)
            };

            var row = new HorizontalStackLayout { Spacing = 6, VerticalOptions = LayoutOptions.Center };
            row.Children.Add(new Border
            {
                WidthRequest = 10,
                HeightRequest = 10,
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = 5 },
                BackgroundColor = preview.Primary,
                VerticalOptions = LayoutOptions.Center
            });
            row.Children.Add(new Border
            {
                WidthRequest = 10,
                HeightRequest = 10,
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = 5 },
                BackgroundColor = preview.Secondary,
                VerticalOptions = LayoutOptions.Center
            });
            row.Children.Add(new Label
            {
                Text = item.Name,
                FontFamily = isSelected ? "InterSemiBold" : "InterMedium",
                FontSize = 11,
                TextColor = isSelected ? palette.PrimaryText : palette.SecondaryText,
                VerticalOptions = LayoutOptions.Center
            });
            chip.Content = row;

            var capturedId = item.Id;
            var tap = new TapGestureRecognizer();
            tap.Tapped += (_, _) => ThemeService.Current.SetPreset(capturedId);
            chip.GestureRecognizers.Add(tap);
            QuickPresetsRow.Children.Add(chip);
        }
    }

    private void OnQuickDarkTapped(object? sender, TappedEventArgs e) => ThemeService.Current.SetMode(AppThemeMode.Dark);
    private void OnQuickLightTapped(object? sender, TappedEventArgs e) => ThemeService.Current.SetMode(AppThemeMode.Light);
    private void OnQuickSystemTapped(object? sender, TappedEventArgs e) => ThemeService.Current.SetMode(AppThemeMode.System);

    private async void OnOpenThemeSettingsClicked(object? sender, EventArgs e)
    {
        await Navigation.PushAsync(new ThemeSettingsPage());
    }

    private async void OnQuickResetThemeClicked(object? sender, EventArgs e)
    {
        bool confirm = await DisplayAlert(
            "Reset Theme",
            "Restore the default Obsidian Dark theme and clear custom colors?",
            "Reset",
            "Cancel");
        if (confirm)
            ThemeService.Current.ResetToDefault();
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

    private bool _isSyncingProfile = false;

    private async void TriggerSilentProfileSync()
    {
        if (_isSyncingProfile) return;
        _isSyncingProfile = true;

        try
        {
            // 1. Immediate fast HTTP sync via active session cookies
            bool httpSuccess = await TikTokProfileSyncHelper.FetchAndSaveProfileViaHttpAsync(_sessionService);
            if (httpSuccess)
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await UpdateLoginButtonStateAsync();
                });
                _isSyncingProfile = false;
                return;
            }

            // 2. Fallback to WebView DOM inspection
            if (ProfileSyncWebView == null)
            {
                _isSyncingProfile = false;
                return;
            }

            var desktopUa = _sessionService.GetLoginUserAgent()
                ?? "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36";

            TikTokWebViewHelper.TryConfigureWebView(ProfileSyncWebView, desktopUa);
            ProfileSyncWebView.Source = "https://www.tiktok.com/messages?lang=en";
        }
        catch (Exception ex)
        {
            _isSyncingProfile = false;
            System.Diagnostics.Debug.WriteLine($"[ProfilePage] Silent sync init failed: {ex.Message}");
        }
    }

    private async void OnProfileSyncWebViewNavigated(object? sender, WebNavigatedEventArgs e)
    {
        if (!_isSyncingProfile) return;

        try
        {
            // Give 1.5s for SSR hydration and DOM scripts to finish
            await Task.Delay(1500);

            bool success = await TikTokProfileSyncHelper.ExtractAndSaveProfileAsync(ProfileSyncWebView, _sessionService);
            if (success)
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await UpdateLoginButtonStateAsync();
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ProfilePage] OnProfileSyncWebViewNavigated error: {ex.Message}");
        }
        finally
        {
            _isSyncingProfile = false;
        }
    }

    private async void OnProfilePhotoTapped(object? sender, TappedEventArgs e)
    {
        try
        {
            var action = await DisplayActionSheet("Profile Details & Photo", "Cancel", null,
                "Sync Profile from TikTok", "Choose Photo from Gallery");

            if (action == "Sync Profile from TikTok")
            {
                TriggerSilentProfileSync();
                await DisplayAlert("Syncing", "Syncing profile info from your TikTok session in background...", "OK");
                return;
            }

            if (action == "Choose Photo from Gallery")
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
        }
        catch (Exception ex)
        {
            await DisplayAlert("Photo", $"Could not update profile: {ex.Message}", "OK");
        }
    }

    private void OnDisplayNameChanged(object? sender, EventArgs e)
    {
        _sessionService.SetDisplayName(DisplayNameEntry.Text ?? "User");
    }

    private async Task UpdateLoginButtonStateAsync()
    {
        bool runReady = await SessionRefreshHelper.RefreshAndGetRunReadyAsync(_sessionService);
        ApplyLoginButtonState(runReady);
    }

    private void ApplyLoginButtonState(bool isSessionValid)
    {
        try
        {
            if (isSessionValid)
            {
                LoggedOutPanel.IsVisible = false;
                LoggedInProfilePanel.IsVisible = true;
                SessionCardTitle.Text = "TikTok Profile";
                SessionDot.BackgroundColor = GetThemeColor("Success", "#10B981");

                var username = _sessionService.GetTikTokUsername();
                ProfileUsernameLabel.Text = string.IsNullOrWhiteSpace(username) ? "@tiktok_user" : $"@{username}";

                var nick = _sessionService.GetTikTokNickname();
                if (string.IsNullOrWhiteSpace(nick) || nick.Equals(username, StringComparison.OrdinalIgnoreCase))
                {
                    var disp = _sessionService.GetDisplayName();
                    if (!string.IsNullOrWhiteSpace(disp) && !disp.Equals(username, StringComparison.OrdinalIgnoreCase))
                        nick = disp;
                }
                ProfileNicknameLabel.Text = string.IsNullOrWhiteSpace(nick) ? "User" : nick;

                var bio = _sessionService.GetTikTokBio();
                BioLabel.Text = string.IsNullOrWhiteSpace(bio) ? "Add a status note..." : bio;

                var lastCheck = _sessionService.GetLastCheckTime();
                SessionLastCheckLabel.Text = lastCheck.HasValue ? $"Verified {lastCheck.Value:MMM dd, h:mm tt}" : "Verified and ready";

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
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ProfilePage] ApplyLoginButtonState error: {ex.Message}");
        }
    }

    private async void OnEditNameClicked(object? sender, EventArgs e)
    {
        var currentName = ProfileNicknameLabel.Text;
        if (currentName == "User")
            currentName = "";

        var newName = await DisplayPromptAsync(
            "Display Name",
            "Enter your in-app name or TikTok nickname:",
            initialValue: currentName,
            maxLength: 50);

        if (newName != null)
        {
            var trimmed = newName.Trim();
            var finalName = string.IsNullOrWhiteSpace(trimmed) ? "User" : trimmed;
            _sessionService.SetDisplayName(finalName);
            _sessionService.SetTikTokNickname(finalName);
            ProfileNicknameLabel.Text = finalName;
            SessionState.NotifyChanged();
        }
    }

    private async void OnEditUsernameClicked(object? sender, EventArgs e)
    {
        var currentUsername = _sessionService.GetTikTokUsername();

        var newUsername = await DisplayPromptAsync(
            "TikTok Username",
            "Enter your TikTok handle (e.g. adrian.official09):",
            initialValue: currentUsername,
            placeholder: "username without @",
            maxLength: 35);

        if (newUsername != null)
        {
            var clean = newUsername.Trim().TrimStart('@');
            _sessionService.SetTikTokUsername(clean);
            ProfileUsernameLabel.Text = string.IsNullOrWhiteSpace(clean) ? "@tiktok_user" : $"@{clean}";
            SessionState.NotifyChanged();
        }
    }

    private async void OnEditBioClicked(object? sender, EventArgs e)
    {
        var currentBio = _sessionService.GetTikTokBio();
        if (string.IsNullOrWhiteSpace(currentBio))
            currentBio = "";

        var newBio = await DisplayPromptAsync(
            "Edit Status Note",
            "Shows in a popup bubble when tapping your profile icon on Home:",
            initialValue: currentBio,
            placeholder: "e.g. Protecting streaks daily 🔥",
            maxLength: 200);

        if (newBio != null)
        {
            var trimmed = newBio.Trim();
            _sessionService.SetTikTokBio(trimmed);
            _sessionService.SetStatusNoteCustomized(!string.IsNullOrWhiteSpace(trimmed));
            BioLabel.Text = string.IsNullOrWhiteSpace(trimmed) ? "Add a status note..." : trimmed;
            SessionState.NotifyChanged();
        }
    }

    private void UpdateLoginButtonState(bool isSessionValid)
    {
        ApplyLoginButtonState(isSessionValid);
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
        _settingsService.SetUseFixedTime(true);
#if ANDROID
        var context = Platform.CurrentActivity ?? Android.App.Application.Context;
        if (e.Value)
            TiktokStreakSaver.Platforms.Android.StreakScheduler.ScheduleNextRun(context);
        else
            TiktokStreakSaver.Platforms.Android.StreakScheduler.CancelSchedule(context);
#endif
        SessionState.NotifyChanged();
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
        SessionState.NotifyChanged();
    }

    private void OnTimePickerChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(TimePicker.Time) || _suppressTimePickerChanged) return;
        var time = ScheduleTimePicker.Time;
        _settingsService.SetUseFixedTime(true);
        _settingsService.SetFixedTimeHour(time.Hours);
        _settingsService.SetFixedTimeMinute(time.Minutes);
        UpdateRadialTimerDisplay(time);
#if ANDROID
        if (_settingsService.IsScheduled())
        {
            var context = Platform.CurrentActivity ?? Android.App.Application.Context;
            TiktokStreakSaver.Platforms.Android.StreakScheduler.ScheduleNextRun(context);
        }
#endif
        SessionState.NotifyChanged();
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
        if (AmPill == null || PmPill == null || AmPillLabel == null || PmPillLabel == null)
            return;

        var palette = ThemeService.Current.CurrentPalette;
        var accent = palette.Accent;
        var borderColor = palette.Border;
        var elevated = palette.SecondaryBackground;
        var muted = palette.SecondaryText;

        var accentBrush = new SolidColorBrush(accent);
        var borderBrush = new SolidColorBrush(borderColor);

        if (isAm)
        {
            AmPill.BackgroundColor = palette.AccentTint;
            AmPill.Stroke = accentBrush;
            AmPillLabel.TextColor = accent;

            PmPill.BackgroundColor = elevated;
            PmPill.Stroke = borderBrush;
            PmPillLabel.TextColor = muted;
        }
        else
        {
            PmPill.BackgroundColor = palette.AccentTint;
            PmPill.Stroke = accentBrush;
            PmPillLabel.TextColor = accent;

            AmPill.BackgroundColor = elevated;
            AmPill.Stroke = borderBrush;
            AmPillLabel.TextColor = muted;
        }
    }

    private void OnScheduleTimeInputChanged(object? sender, TextChangedEventArgs e)
    {
        if (_suppressTimeInputChanged) return;
        if (ScheduleHourEntry == null || ScheduleMinuteEntry == null) return;

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

            _suppressTimePickerChanged = true;
            try
            {
                ScheduleTimePicker.Time = newTime;
            }
            finally
            {
                _suppressTimePickerChanged = false;
            }

            _settingsService.SetUseFixedTime(true);
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
            if (_settingsService.IsScheduled())
            {
                var context = Platform.CurrentActivity ?? Android.App.Application.Context;
                TiktokStreakSaver.Platforms.Android.StreakScheduler.ScheduleNextRun(context);
            }
#endif
            SessionState.NotifyChanged();
        }
    }

    private void OnAmTapped(object? sender, TappedEventArgs e)
    {
        _settingsService.SetUseFixedTime(true);
        int h = int.TryParse(ScheduleHourEntry?.Text, out var parsedH) ? parsedH : 12;
        int m = int.TryParse(ScheduleMinuteEntry?.Text, out var parsedM) ? parsedM : 0;
        var h12 = Math.Clamp(h, 1, 12);
        int hour24 = (h12 % 12); // AM is 0..11
        int minute = Math.Clamp(m, 0, 59);

        var newTime = new TimeSpan(hour24, minute, 0);
        _suppressTimePickerChanged = true;
        try
        {
            ScheduleTimePicker.Time = newTime;
        }
        finally
        {
            _suppressTimePickerChanged = false;
        }

        _settingsService.SetFixedTimeHour(hour24);
        _settingsService.SetFixedTimeMinute(minute);
        UpdateRadialTimerDisplay(newTime);

#if ANDROID
        if (_settingsService.IsScheduled())
        {
            var context = Platform.CurrentActivity ?? Android.App.Application.Context;
            TiktokStreakSaver.Platforms.Android.StreakScheduler.ScheduleNextRun(context);
        }
#endif
        SessionState.NotifyChanged();
    }

    private void OnPmTapped(object? sender, TappedEventArgs e)
    {
        _settingsService.SetUseFixedTime(true);
        int h = int.TryParse(ScheduleHourEntry?.Text, out var parsedH) ? parsedH : 12;
        int m = int.TryParse(ScheduleMinuteEntry?.Text, out var parsedM) ? parsedM : 0;
        var h12 = Math.Clamp(h, 1, 12);
        int hour24 = (h12 % 12) + 12; // PM is 12..23
        int minute = Math.Clamp(m, 0, 59);

        var newTime = new TimeSpan(hour24, minute, 0);
        _suppressTimePickerChanged = true;
        try
        {
            ScheduleTimePicker.Time = newTime;
        }
        finally
        {
            _suppressTimePickerChanged = false;
        }

        _settingsService.SetFixedTimeHour(hour24);
        _settingsService.SetFixedTimeMinute(minute);
        UpdateRadialTimerDisplay(newTime);

#if ANDROID
        if (_settingsService.IsScheduled())
        {
            var context = Platform.CurrentActivity ?? Android.App.Application.Context;
            TiktokStreakSaver.Platforms.Android.StreakScheduler.ScheduleNextRun(context);
        }
#endif
        SessionState.NotifyChanged();
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
#if ANDROID
                var context = Platform.CurrentActivity ?? Android.App.Application.Context;
                TiktokStreakSaver.Platforms.Android.Services.UpdateNotificationHelper.PostUpdateNotification(
                    context, remoteVersion, info.ApkDownloadUrl);
#endif
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
