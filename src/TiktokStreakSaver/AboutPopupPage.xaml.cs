using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls.Shapes;
using TiktokStreakSaver.Models;
using TiktokStreakSaver.Services;

namespace TiktokStreakSaver;

[Microsoft.Maui.Controls.Internals.Preserve(AllMembers = true)]
public partial class AboutPopupPage : ContentPage
{
    private sealed record ManualSettingEntry(
        string Emoji,
        string Title,
        string Category,
        string OnLabel,
        string OnDescription,
        string OffLabel,
        string OffDescription);

    private static readonly ManualSettingEntry[] ManualEntries =
    [
        new(
            "🔄",
            "Auto-Retry (Fail-safe skip)",
            "Automation Config",
            "ON",
            "Skips a failing chat so other friends still receive messages, and auto-disables repeatedly unreachable targets after 3 failed runs.",
            "OFF",
            "Immediately halts the entire automation run on the first target failure so no further messages are attempted."),
        new(
            "🎲",
            "Custom Messages (Randomizer)",
            "Automation Config",
            "ON",
            "Selects varied lines randomly from 50 built-in streak messages and locks custom message editors on Home.",
            "OFF",
            "Sends your main Automation Text and applies any per-chat Personalized Messages you set on Home."),
        new(
            "🔀",
            "Chat Queue Priority",
            "Automation Config",
            "Mixed / Friends / Groups",
            "Controls queue order: Mixed follows your Friends list, while Friends First or Groups First processes one group before the other.",
            "Friends Tab",
            "Choose exactly which direct buddies or group chats are active or paused from the Friends tab."),
        new(
            "⏰",
            "Background Run & Schedule",
            "Schedule",
            "ON",
            "Arms exact daily alarms at your 12-hour AM/PM time, retries hourly (up to 3x), and auto-resumes today as soon as internet returns.",
            "OFF",
            "Cancels all background alarms and offline recovery jobs; automation only runs when started manually."),
        new(
            "🔋",
            "Low Battery Saver",
            "Schedule",
            "ON",
            "Anticipates low battery (<=15%) and sends today's pending streaks early before the device powers down (once per day).",
            "OFF",
            "Ignores low-battery warnings and waits for your regular scheduled run time."),
        new(
            "💬",
            "Personalized Messages",
            "Home Screen",
            "Custom Set",
            "Sends the recipient's individual custom message during streak runs while keeping all changes locally on-device.",
            "Default",
            "Leaves the recipient using your shared Automation Text automatically."),
        new(
            "🎨",
            "Appearance & Theme",
            "Profile Theme",
            "Custom / Preset",
            "Updates colors, progress rings, Status Bubble, and system bars live across Dark, Light, or System mode with 5 presets and 12 tokens.",
            "Reset",
            "One-tap Reset restores the default Obsidian Dark theme."),
        new(
            "💡",
            "Status Note Bubble",
            "Profile & Home",
            "Tap Avatar",
            "Shows your Status Note in an expanding theme-adaptive speech bubble on Home for 5 seconds and stays saved across updates.",
            "Empty",
            "Tap Status Note in Profile anytime to set or update your message.")
    ];

    private readonly string _assignedVersion;
    private readonly bool _isUpdate;
    private readonly string? _apkDownloadUrl;
    private bool _isClosing = false;
    private bool _isDownloading = false;
    private CancellationTokenSource? _downloadCts;
    private bool _isFallbackMode = false;

    public AboutPopupPage(string title, string version, string changelog, bool isUpdate, string? apkDownloadUrl = null)
    {
        InitializeComponent();

        _assignedVersion = version;
        _isUpdate = isUpdate;
        _apkDownloadUrl = apkDownloadUrl;

        HeaderTitle.Text = title;
        string displayVer = string.IsNullOrWhiteSpace(version) ? AppInfo.Current.VersionString : version;
        string cleanVersion = displayVer.TrimStart('v', 'V');
        VersionLabel.Text = $"v{cleanVersion} (Premium) • Signed Version";

        if (isUpdate)
        {
            // Update popup: show changelog, hide the tagline subtitle
            SubHeaderLabel.IsVisible = false;
            if (string.IsNullOrWhiteSpace(changelog))
                ChangelogBorder.IsVisible = false;
            else
                LoadChangelogHtml(changelog);

            bool isMandatory = !string.IsNullOrWhiteSpace(changelog) &&
                (changelog.Contains("[Mandatory]", StringComparison.OrdinalIgnoreCase) ||
                 changelog.Contains("[Required]", StringComparison.OrdinalIgnoreCase));

            CloseButton.IsVisible = false;
            UpdateButtonGrid.IsVisible = true;

            if (isMandatory)
            {
                LaterButton.IsVisible = false; // Block user from dismissing mandatory updates
            }
        }
        else
        {
            // Welcome popup: hide changelog entirely, show tagline subtitle
            ChangelogBorder.IsVisible = false;
            SubHeaderLabel.IsVisible = true;

            CloseButton.IsVisible = true;
            UpdateButtonGrid.IsVisible = false;
        }

        PopulateManualEntries();
        PopulateEngineStatusRows(cleanVersion);
    }

    private void PopulateManualEntries()
    {
        var p = ThemeService.Current.CurrentPalette;
        ManualEntriesContainer.Children.Clear();

        foreach (var item in ManualEntries)
        {
            var card = new Border
            {
                StrokeThickness = 1,
                Stroke = new SolidColorBrush(p.Border),
                StrokeShape = new RoundRectangle { CornerRadius = 14 },
                BackgroundColor = p.SecondaryBackground,
                Padding = new Thickness(12, 10)
            };

            var root = new VerticalStackLayout { Spacing = 8 };

            var headerGrid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto }
                },
                ColumnSpacing = 8
            };

            var emojiLabel = new Label
            {
                Text = item.Emoji,
                FontSize = 16,
                VerticalOptions = LayoutOptions.Center
            };
            Grid.SetColumn(emojiLabel, 0);
            headerGrid.Children.Add(emojiLabel);

            var titleLabel = new Label
            {
                Text = item.Title,
                FontFamily = "InterSemiBold",
                FontSize = 13,
                TextColor = p.PrimaryText,
                VerticalOptions = LayoutOptions.Center
            };
            Grid.SetColumn(titleLabel, 1);
            headerGrid.Children.Add(titleLabel);

            var categoryChip = new Border
            {
                StrokeThickness = 1,
                Stroke = new SolidColorBrush(p.Border),
                StrokeShape = new RoundRectangle { CornerRadius = 8 },
                BackgroundColor = p.CardBackground,
                Padding = new Thickness(7, 3),
                VerticalOptions = LayoutOptions.Center,
                Content = new Label
                {
                    Text = item.Category,
                    FontFamily = "InterMedium",
                    FontSize = 9.5,
                    TextColor = p.SecondaryText
                }
            };
            Grid.SetColumn(categoryChip, 2);
            headerGrid.Children.Add(categoryChip);
            root.Children.Add(headerGrid);

            root.Children.Add(BuildStateRow(
                badgeText: item.OnLabel,
                description: item.OnDescription,
                badgeColor: p.Success,
                badgeTint: p.SuccessTint,
                p: p));

            root.Children.Add(BuildStateRow(
                badgeText: item.OffLabel,
                description: item.OffDescription,
                badgeColor: item.OffLabel == "OFF" ? p.Primary : p.Secondary,
                badgeTint: item.OffLabel == "OFF" ? p.PrimaryTint : p.SecondaryTint,
                p: p));

            card.Content = root;
            ManualEntriesContainer.Children.Add(card);
        }
    }

    private static Grid BuildStateRow(
        string badgeText,
        string description,
        Color badgeColor,
        Color badgeTint,
        ThemePalette p)
    {
        var row = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Star }
            },
            ColumnSpacing = 8
        };

        var pill = new Border
        {
            StrokeThickness = 1,
            Stroke = new SolidColorBrush(badgeColor.WithAlpha(0.45f)),
            StrokeShape = new RoundRectangle { CornerRadius = 7 },
            BackgroundColor = badgeTint,
            Padding = new Thickness(7, 2),
            MinimumWidthRequest = 42,
            VerticalOptions = LayoutOptions.Start,
            Content = new Label
            {
                Text = badgeText,
                FontFamily = "InterBold",
                FontSize = 10,
                TextColor = badgeColor,
                HorizontalTextAlignment = TextAlignment.Center
            }
        };
        Grid.SetColumn(pill, 0);
        row.Children.Add(pill);

        var descLabel = new Label
        {
            Text = description,
            FontFamily = "InterRegular",
            FontSize = 11.5,
            TextColor = p.SecondaryText,
            LineBreakMode = LineBreakMode.WordWrap,
            VerticalOptions = LayoutOptions.Center
        };
        Grid.SetColumn(descLabel, 1);
        row.Children.Add(descLabel);

        return row;
    }

    private void PopulateEngineStatusRows(string cleanVersion)
    {
        var themeSvc = ThemeService.Current;
        var p = themeSvc.CurrentPalette;
        var preset = ThemeCatalog.GetPreset(themeSvc.CurrentPresetId);
        var modeText = themeSvc.CurrentMode switch
        {
            AppThemeMode.Dark => "Dark",
            AppThemeMode.Light => "Light",
            _ => "System"
        };

        var settings = new SettingsService();
        bool scheduled = settings.IsScheduled();
        bool offlinePending = settings.HasPendingOfflineRecoveryToday();
        string scheduleState = scheduled
            ? (offlinePending ? "Active • Offline recovery armed for today" : "Active • Background & network recovery ready")
            : "Paused • Manual runs only";

        EngineBuildBadgeLabel.Text = $"✓ v{cleanVersion}";
        EngineStatusRowsContainer.Children.Clear();

        (string Icon, string Title, string Detail, Color Accent)[] rows =
        [
            ("⚙️", "On-Device WebView Engine", "Runs automation locally with zero credential telemetry.", p.Secondary),
            ("📡", "Scheduler & Recovery", scheduleState, scheduled ? p.Success : p.Warning),
            ("🎨", $"Theme • {preset.Name}", $"{modeText} mode{(themeSvc.CustomColors.HasAnyOverride ? " • Custom tokens active" : string.Empty)}", p.Accent)
        ];

        foreach (var item in rows)
        {
            var box = new Border
            {
                StrokeThickness = 1,
                Stroke = new SolidColorBrush(p.Border),
                StrokeShape = new RoundRectangle { CornerRadius = 12 },
                BackgroundColor = p.SecondaryBackground,
                Padding = new Thickness(11, 9)
            };

            var rowGrid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = GridLength.Star }
                },
                ColumnSpacing = 10
            };

            var iconLabel = new Label
            {
                Text = item.Icon,
                FontSize = 15,
                VerticalOptions = LayoutOptions.Center
            };
            Grid.SetColumn(iconLabel, 0);
            rowGrid.Children.Add(iconLabel);

            var textStack = new VerticalStackLayout { Spacing = 1, VerticalOptions = LayoutOptions.Center };
            textStack.Children.Add(new Label
            {
                Text = item.Title,
                FontFamily = "InterSemiBold",
                FontSize = 12.5,
                TextColor = p.PrimaryText
            });
            textStack.Children.Add(new Label
            {
                Text = item.Detail,
                FontFamily = "InterRegular",
                FontSize = 11,
                TextColor = item.Accent,
                LineBreakMode = LineBreakMode.WordWrap
            });
            Grid.SetColumn(textStack, 1);
            rowGrid.Children.Add(textStack);

            box.Content = rowGrid;
            EngineStatusRowsContainer.Children.Add(box);
        }
    }

    private void LoadChangelogHtml(string markdown)
    {
        var p = ThemeService.Current.CurrentPalette;

        // ── Color palette driven by active ThemeService ────────────────────
        string bg        = ColorContrastHelper.ToHex(p.CardBackground);
        string fg        = ColorContrastHelper.ToHex(p.PrimaryText);
        string fgMuted   = ColorContrastHelper.ToHex(p.SecondaryText);
        string accent    = ColorContrastHelper.ToHex(p.Primary);
        string cardBg    = ColorContrastHelper.ToHex(p.CardBackground);
        string divider   = ColorContrastHelper.ToHex(p.Border);
        string utilBg    = ColorContrastHelper.ToHex(p.SecondaryBackground);
        string bulletCol = ColorContrastHelper.ToHex(p.Border);
        string onAccent  = ColorContrastHelper.ToHex(p.OnPrimary);

        string bodyHtml = ConvertMarkdownToStructuredHtml(markdown);

        string html = $@"<!DOCTYPE html>
<html><head><meta name='viewport' content='width=device-width,initial-scale=1,maximum-scale=1'>
<style>
*{{margin:0;padding:0;box-sizing:border-box;}}
body{{
  background:{bg};color:{fg};
  font-family:-apple-system,'SF Pro Text',Roboto,'Segoe UI',sans-serif;
  font-size:14px;line-height:1.6;
  padding:16px;
  -webkit-text-size-adjust:100%;
}}

/* ── Section cards ── */
.section{{
  margin-bottom:16px;
}}
.section:last-child{{margin-bottom:0;}}

.section-title{{
  font-size:15px;font-weight:700;
  color:{fg};
  margin:0 0 10px 0;
  padding-bottom:8px;
  border-bottom:1px solid {divider};
  letter-spacing:-0.2px;
}}

.section-body p{{
  margin:6px 0;
  color:{fgMuted};
  font-size:13px;
  line-height:1.55;
}}

/* ── Bullet lists ── */
.section-body ul,.section-body ol{{
  list-style:none;
  padding:0;margin:0;
}}
.section-body ul li{{
  position:relative;
  padding:8px 0 8px 18px;
  font-size:13.5px;
  line-height:1.5;
  border-bottom:1px solid {divider};
}}
.section-body ul li:last-child{{border-bottom:none;}}
.section-body ul li::before{{
  content:'';
  position:absolute;left:0;top:15px;
  width:6px;height:6px;
  border-radius:50%;
  background:{accent};
}}
.section-body ol{{
  counter-reset:step;
}}
.section-body ol li{{
  position:relative;
  padding:8px 0 8px 24px;
  font-size:13.5px;
  line-height:1.5;
  border-bottom:1px solid {divider};
  counter-increment:step;
}}
.section-body ol li:last-child{{border-bottom:none;}}
.section-body ol li::before{{
  content:counter(step);
  position:absolute;left:0;top:8px;
  width:18px;height:18px;
  border-radius:50%;
  background:{accent};
  color:{onAccent};
  font-size:11px;font-weight:700;
  display:flex;align-items:center;justify-content:center;
}}

/* ── Inline elements ── */
strong{{font-weight:600;color:{fg};}}
em{{font-style:italic;}}
code{{
  background:{divider};
  padding:1px 5px;
  border-radius:4px;
  font-size:12px;
  font-family:'SF Mono',Menlo,monospace;
}}

/* ── Utility sections (Installation, Requirements) ── */
.util-section{{
  background:{utilBg};
  border-radius:10px;
  padding:12px 14px;
  margin-bottom:10px;
}}
.util-section:last-child{{margin-bottom:0;}}
.util-title{{
  font-size:13px;font-weight:700;
  color:{fgMuted};
  text-transform:uppercase;
  letter-spacing:0.5px;
  margin:0 0 8px 0;
}}
.util-section ul li,.util-section ol li{{
  font-size:13px;
  color:{fgMuted};
  border-bottom-color:{divider};
}}
.util-section ul li::before{{
  background:{bulletCol};
}}
.util-section ol li::before{{
  background:{bulletCol};
}}

/* ── Divider ── */
.section-divider{{
  border:none;
  border-top:1px solid {divider};
  margin:4px 0;
}}

/* ── Blockquote ── */
blockquote{{
  border-left:3px solid {accent};
  padding:6px 12px;
  margin:8px 0;
  color:{fgMuted};
  font-size:13px;
  background:{utilBg};
  border-radius:0 6px 6px 0;
}}
</style></head><body>{bodyHtml}</body></html>";

        ChangelogWebView.Source = new HtmlWebViewSource { Html = html };

        // Auto-size WebView to content
        ChangelogWebView.Navigated += (s, e) =>
        {
            ChangelogWebView.EvaluateJavaScriptAsync("document.body.scrollHeight")
                .ContinueWith(t =>
                {
                    if (t.Result is string result && double.TryParse(result, out double height) && height > 0)
                    {
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            ChangelogWebView.HeightRequest = height + 16;
                        });
                    }
                });
        };
    }

    // ── Section-aware Markdown-to-HTML converter ────────────────────────────

    /// <summary>
    /// Utility sections (Installation, Requirements) get distinct styling.
    /// </summary>
    private static readonly HashSet<string> UtilitySections = new(StringComparer.OrdinalIgnoreCase)
    {
        "Installation", "Requirements", "About"
    };

    /// <summary>
    /// Converts Markdown into structured HTML with section grouping.
    /// H2 and H3 headers start new sections; lists and paragraphs fill sections.
    /// The first H2 matching "What's new in this release" is stripped because
    /// the dialog header already shows the title and version.
    /// </summary>
    private static string ConvertMarkdownToStructuredHtml(string markdown)
    {
        var lines = markdown.Split('\n');
        var sb = new System.Text.StringBuilder();
        bool inSection = false;
        bool inSectionBody = false;
        bool inUl = false;
        bool inOl = false;
        bool firstH2Stripped = false;

        foreach (var rawLine in lines)
        {
            string line = rawLine.TrimEnd('\r');

            // ── Horizontal rules → divider ──
            if (System.Text.RegularExpressions.Regex.IsMatch(line.Trim(), @"^-{3,}$|^\*{3,}$|^_{3,}$"))
            {
                CloseList(sb, ref inUl, ref inOl);
                CloseSectionBody(sb, ref inSectionBody);
                CloseSection(sb, ref inSection);
                sb.AppendLine("<hr class='section-divider'/>");
                continue;
            }

            // ── H2 headers → new top-level section ──
            if (line.StartsWith("## "))
            {
                string title = line[3..].Trim();

                // Strip "What's new in this release (vX.X.X)" — redundant with dialog header
                if (!firstH2Stripped && title.StartsWith("What", StringComparison.OrdinalIgnoreCase))
                {
                    firstH2Stripped = true;
                    continue;
                }

                CloseList(sb, ref inUl, ref inOl);
                CloseSectionBody(sb, ref inSectionBody);
                CloseSection(sb, ref inSection);

                bool isUtil = UtilitySections.Contains(title.Trim());
                sb.AppendLine(isUtil ? "<div class='section util-section'>" : "<div class='section'>");
                sb.AppendLine(isUtil
                    ? $"<div class='util-title'>{FormatInline(title)}</div>"
                    : $"<div class='section-title'>{FormatInline(title)}</div>");
                inSection = true;

                sb.AppendLine("<div class='section-body'>");
                inSectionBody = true;
                continue;
            }

            // ── H3 headers → new sub-section ──
            if (line.StartsWith("### "))
            {
                string title = line[4..].Trim();

                CloseList(sb, ref inUl, ref inOl);
                CloseSectionBody(sb, ref inSectionBody);
                CloseSection(sb, ref inSection);

                bool isUtil = UtilitySections.Contains(title.Trim());
                sb.AppendLine(isUtil ? "<div class='section util-section'>" : "<div class='section'>");
                sb.AppendLine(isUtil
                    ? $"<div class='util-title'>{FormatInline(title)}</div>"
                    : $"<div class='section-title'>{FormatInline(title)}</div>");
                inSection = true;

                sb.AppendLine("<div class='section-body'>");
                inSectionBody = true;
                continue;
            }

            // ── H1 headers (rare, treat as section title) ──
            if (line.StartsWith("# "))
            {
                CloseList(sb, ref inUl, ref inOl);
                CloseSectionBody(sb, ref inSectionBody);
                CloseSection(sb, ref inSection);
                // Skip — redundant top-level title
                continue;
            }

            // ── Ensure content is inside a section ──
            if (!inSection)
            {
                sb.AppendLine("<div class='section'>");
                inSection = true;
                sb.AppendLine("<div class='section-body'>");
                inSectionBody = true;
            }

            // ── Blockquote ──
            if (line.StartsWith("> "))
            {
                CloseList(sb, ref inUl, ref inOl);
                sb.AppendLine($"<blockquote>{FormatInline(line[2..])}</blockquote>");
                continue;
            }

            // ── Unordered list ──
            var ulMatch = System.Text.RegularExpressions.Regex.Match(line, @"^\s*[-*]\s+(.+)");
            if (ulMatch.Success)
            {
                if (inOl) { sb.AppendLine("</ol>"); inOl = false; }
                if (!inUl) { sb.AppendLine("<ul>"); inUl = true; }
                sb.AppendLine($"<li>{FormatInline(ulMatch.Groups[1].Value)}</li>");
                continue;
            }

            // ── Ordered list ──
            var olMatch = System.Text.RegularExpressions.Regex.Match(line, @"^\s*\d+\.\s+(.+)");
            if (olMatch.Success)
            {
                if (inUl) { sb.AppendLine("</ul>"); inUl = false; }
                if (!inOl) { sb.AppendLine("<ol>"); inOl = true; }
                sb.AppendLine($"<li>{FormatInline(olMatch.Groups[1].Value)}</li>");
                continue;
            }

            // ── Empty line ──
            if (string.IsNullOrWhiteSpace(line))
            {
                CloseList(sb, ref inUl, ref inOl);
                continue;
            }

            // ── Plain paragraph ──
            CloseList(sb, ref inUl, ref inOl);
            sb.AppendLine($"<p>{FormatInline(line)}</p>");
        }

        CloseList(sb, ref inUl, ref inOl);
        CloseSectionBody(sb, ref inSectionBody);
        CloseSection(sb, ref inSection);
        return sb.ToString();
    }

    private static void CloseList(System.Text.StringBuilder sb, ref bool inUl, ref bool inOl)
    {
        if (inUl) { sb.AppendLine("</ul>"); inUl = false; }
        if (inOl) { sb.AppendLine("</ol>"); inOl = false; }
    }

    private static void CloseSectionBody(System.Text.StringBuilder sb, ref bool inSectionBody)
    {
        if (inSectionBody) { sb.AppendLine("</div>"); inSectionBody = false; }
    }

    private static void CloseSection(System.Text.StringBuilder sb, ref bool inSection)
    {
        if (inSection) { sb.AppendLine("</div>"); inSection = false; }
    }

    /// <summary>Formats inline Markdown: bold, italic, inline code.</summary>
    private static string FormatInline(string text)
    {
        // Inline code: `code`
        text = System.Text.RegularExpressions.Regex.Replace(text, @"`([^`]+)`", "<code>$1</code>");
        // Bold: **text** or __text__
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\*\*(.+?)\*\*", "<strong>$1</strong>");
        text = System.Text.RegularExpressions.Regex.Replace(text, @"__(.+?)__", "<strong>$1</strong>");
        // Italic: *text* or _text_
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\*(.+?)\*", "<em>$1</em>");
        text = System.Text.RegularExpressions.Regex.Replace(text, @"(?<!\w)_(.+?)_(?!\w)", "<em>$1</em>");
        return text;
    }

    // ── Onboarding / Welcome: Continue button ──────────────────────────────
    private async void OnCloseClicked(object sender, EventArgs e)
    {
        if (_isClosing) return;
        _isClosing = true;

        Preferences.Default.Set("LastAppVersionSeen", _assignedVersion);

        try { await Navigation.PopModalAsync(); } catch { }
    }

    // ── Update: Later button ───────────────────────────────────────────────
    private async void OnLaterClicked(object sender, EventArgs e)
    {
        if (_isClosing || _isDownloading) return;
        _isClosing = true;

        Preferences.Default.Set("LastRemoteVersionSeen", _assignedVersion);

        try { await Navigation.PopModalAsync(); } catch { }
    }

    // ── Update: Install button ─────────────────────────────────────────────
    private async void OnInstallClicked(object? sender, EventArgs e)
    {
        if (_isDownloading) return;

#if IOS
        var url = _apkDownloadUrl ?? "https://github.com/MrblackAIO/TikTok-Auto-Streak/releases/latest";
        await Launcher.OpenAsync(url);
        Preferences.Default.Set("LastRemoteVersionSeen", _assignedVersion);
        try { await Navigation.PopModalAsync(); } catch { }
        return;
#endif

        // If no APK asset found, fall back to opening GitHub releases page
        if (string.IsNullOrEmpty(_apkDownloadUrl))
        {
            await FallbackToGitHubAsync();
            return;
        }

        _isDownloading = true;
        InstallButton.IsEnabled = false;
        LaterButton.IsEnabled = false;
        InstallButton.Text = "Downloading...";

        _downloadCts = new CancellationTokenSource();

        var progress = new Progress<double>(percent =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                int pct = (int)Math.Round(percent * 100);
                ProgressLabel.IsVisible = true;
                ProgressLabel.Text = $"Downloading update... {pct}%";
            });
        });

        var updateService = new UpdateService();
        string? apkPath = await updateService.DownloadApkAsync(
            _apkDownloadUrl,
            _assignedVersion,
            progress,
            _downloadCts.Token);

        if (apkPath == null || !File.Exists(apkPath))
        {
            ShowDownloadError();
            return;
        }

        // Pre-mark version state BEFORE handing off to installer.
        // When Android restarts the app after install, CheckStartupPopupAsync will
        // detect UpdateJustInstalled=true and skip straight past the Welcome popup.
        Preferences.Default.Set("UpdateJustInstalled", true);
        Preferences.Default.Set("LastRemoteVersionSeen", _assignedVersion);
        Preferences.Default.Set("LastAppVersionSeen", _assignedVersion);

        ProgressLabel.Text = "Starting installer...";

        TriggerApkInstall(apkPath);

        // Close popup after handing off to the system installer
        try { await Navigation.PopModalAsync(); } catch { }
    }

    private void ShowDownloadError()
    {
        _isDownloading = false;
        InstallButton.IsEnabled = true;
        LaterButton.IsEnabled = true;
        InstallButton.Text = "Install";
        ProgressLabel.IsVisible = true;
        ProgressLabel.Text = "Download failed. Please try again.";
        ProgressLabel.TextColor = ThemeService.Current.CurrentPalette.Error;

        SwitchToFallbackMode();
    }

    private void SwitchToFallbackMode()
    {
        if (_isFallbackMode) return;
        _isFallbackMode = true;

        InstallButton.Text = "Open GitHub Page";
        InstallButton.Clicked -= OnInstallClicked;
        InstallButton.Clicked += OnFallbackClicked;
    }

    private async void OnFallbackClicked(object? sender, EventArgs e)
    {
        await FallbackToGitHubAsync();
    }

    private async Task FallbackToGitHubAsync()
    {
        try
        {
            await Launcher.Default.OpenAsync("https://github.com/MrblackAIO/TikTok-Auto-Streak/releases/latest");
        }
        catch { }
    }

    private async void OnFeenerCreditTapped(object? sender, TappedEventArgs e)
    {
        try
        {
            await Launcher.Default.OpenAsync("https://github.com/MrblackAIO/TikTok-Auto-Streak");
        }
        catch { }
    }

    private async void OnRusdiCreditTapped(object? sender, TappedEventArgs e)
    {
        try
        {
            await Launcher.Default.OpenAsync("https://github.com/MrblackAIO/TikTok-Auto-Streak");
        }
        catch { }
    }

    private void TriggerApkInstall(string apkPath)
    {
#if ANDROID
        try
        {
            var context = Android.App.Application.Context;
            var file = new Java.IO.File(apkPath);
            var uri = AndroidX.Core.Content.FileProvider.GetUriForFile(
                context,
                $"{context.PackageName}.fileProvider",
                file);

            // Intent.ActionView with the APK MIME type is the correct cross-version approach.
            // ActionInstallPackage is deprecated on API 29+ and triggers CA1422.
            var intent = new Android.Content.Intent(Android.Content.Intent.ActionView);
            intent.SetDataAndType(uri, "application/vnd.android.package-archive");
            intent.AddFlags(Android.Content.ActivityFlags.GrantReadUriPermission);
            intent.AddFlags(Android.Content.ActivityFlags.NewTask);
            context.StartActivity(intent);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[StreakSaver] APK install intent failed: {ex.Message}");

            // Show user-friendly explanation for the most common failure cause.
            // INSTALL_FAILED_UPDATE_INCOMPATIBLE means the update APK was signed with
            // a different keystore than the installed version — Android blocks this.
            // The user must uninstall the existing app first, then reinstall from GitHub.
            MainThread.BeginInvokeOnMainThread(() =>
            {
                ProgressLabel.IsVisible = true;
                ProgressLabel.Text = "Install failed. If you see a 'conflicting package' error, " +
                                     "uninstall this app first, then reinstall from GitHub.";
                ProgressLabel.TextColor = ThemeService.Current.CurrentPalette.Error;
                _isDownloading = false;
                InstallButton.IsEnabled = true;
                LaterButton.IsEnabled = true;
                SwitchToFallbackMode();
            });
        }
#endif
    }

    /// <summary>Intercept Android hardware back button to behave consistently with Later.</summary>
    protected override bool OnBackButtonPressed()
    {
        if (_isDownloading)
        {
            _downloadCts?.Cancel();
        }
        if (_isUpdate)
            OnLaterClicked(this, EventArgs.Empty);
        else
            OnCloseClicked(this, EventArgs.Empty);
        return true;
    }
}
