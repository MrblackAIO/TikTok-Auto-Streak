# Changelog

All notable changes to Streak Saver are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project follows [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [2.0.19] - 2026-11-26

### Added
- **Once-Per-Launch Home Status Note Auto-Pop**: Automatically displays a user's non-empty Status Note bubble roughly `1.5 seconds` after reaching the Home screen (`DashboardPage`), firing once per app launch while skipping tab switches, early exits, and active modal dialogs.

## [2.0.18] - 2026-11-26

### Fixed
- **Home Status Note Bubble Sizing**: Replaced unconstrained `Fill` sizing on `StatusBubbleGraphicsView` with explicit content-measured bounds (`130–240dp` wide × `56–180dp` tall via `StatusBubbleContent.Measure` and `OnStatusBubbleContentSizeChanged`) and tightened compact `StatusBubbleDrawable` proportions so the popup cleanly frames short and multi-line notes beneath the profile avatar.

## [2.0.17] - 2026-11-26

### Fixed
- **Status Note (formerly Bio) Persistence Across Updates & Syncs**: Fixed the bug where passive TikTok profile syncs (`TikTokProfileSyncHelper.ParseAndSaveJsonAsync`) on `/messages` overwrote user-saved notes with empty strings after updates or automation runs; added `session_status_note_customized` in `SessionService` while preserving `session_tiktok_bio` for seamless migration.
- **Same-Day Android Internet Restoration Recovery**: Added persistent `OfflineRecoveryJobService` (`JobScheduler`, `NetworkType.Any`, `SetPersisted(true)`) alongside `NetworkChangeMonitor` with an `AppTime.Today`-anchored recovery state (`pending_offline_recovery_date`) and persisted 20-minute throttle (`last_network_recovery_utc_ticks`) so internet restoration triggers streak recovery the same day even after the 3 hourly `AlarmManager` retries are exhausted or the app process is killed.
- **Home Profile Avatar Accent Border**: Matched `ProfilePage`'s `2dp` themed `CoralRedBrush` border ring and inset circular clip (`40x40`) on `DashboardPage`.

### Added
- **Home Status Note Bubble Popup**: Renamed `"Bio"` to `"Status Note"` and added a theme-adaptive expanding/collapsing speech bubble (`StatusBubbleDrawable`) on the Home screen when tapping the profile avatar, auto-dismissing after 5 seconds with the reverse animation.
- **Automation Manual & Engine Status**: Renamed `"Automation Guide & Tips"` to `"Automation Manual & Tips"`, added the emoji-styled **Automation Manual** section explaining ON/OFF behavior across Automation Configs and settings, and replaced the legacy **Verified Release** block with a live **Engine & Build Status** card.

## [2.0.16] - 2026-11-26

### Fixed
- **Personalized Messages Card-Stack & Dropdown UI**: Refined the collapsed layered card-stack peek offsets (`7dp`/`14dp` with elevated contrast borders) without negative-margin clipping, fixed the expanded dropdown to render an aligned, upright vertical list of 2-section recipient cards with zero lingering rotation/offset transforms, added `AutoSize` multi-line message editors, preserved focused editor state during background refreshes, and dynamically adjusted Dashboard bottom scroll clearance (`BottomScrollSpacer`) above `ControlDockBorder` and `FloatingNavBar`.

## [2.0.15] - 2026-11-26

### Fixed
- **Timezone & History Timestamp Accuracy**: Fixed UTC/local deserialization mismatch in `AppJsonSerialization` and introduced centralized `AppTime` with automatic fallback to **PHT (`Asia/Manila`, UTC+8)** when the device timezone cannot be distinguished, ensuring History logs, Dashboard `Sent Today`/`Last Run`, and daily cooldown checks align with local clock time.

### Added
- **Global Theme Customization System**: Added `ThemeService`, `ThemeCatalog`, `ColorContrastHelper`, and `ThemeSettingsPage` accessible from `ProfilePage` with Dark / Light / Follow System modes, 11 coordinated built-in presets (`Obsidian`, `TikTok Neon`, `AMOLED Black`, `Midnight Blue`, `Ocean`, `Emerald`, `Dracula`, `Nord`, `Sunset`, `Rose`, `Minimal Light`), quick accent picker, 12-token color customization with WCAG contrast validation, live preview, persistent storage, and dynamic Android system bar styling.
- **Personalized Messages per Friend / Group**: Added `PersonalizedMessagesCardStack` on the Dashboard (`Automation Text`) with animated card-stack unfolding/collapsing, search and filter chips, per-recipient override editors and reset actions keyed by `FriendConfig.Id`, and conditional visibility when Custom Message Randomizer is OFF across Android (`StreakService`) and iOS (`StreakEngine`).

## [2.0.14] - 2026-11-15

### Fixed
- **Post-Update Schedule Recovery**: Resolved the bug where scheduled alarms failed to trigger after updating the APK. Added `ACTION_MY_PACKAGE_REPLACED` in `BootReceiver` so Android OS automatically re-arms the `AlarmManager` schedule the moment an update finishes installing.
- **App Startup Schedule Re-Sync**: `MainActivity` and `DashboardPage` now verify and re-arm the OS alarm on startup if scheduling is enabled, preventing "ghost" schedule labels in the UI.
- **Alarm Chain Resilience**: Protected `AlarmReceiver` with a `try/catch` and fallback rescheduling logic so that foreground service startup errors on Android 12+ do not terminate the recurring daily schedule.

### Added
- **Allow Background Auto-Start Prompt**: Added a dedicated `BtnAutoStart` button inside `PermissionsPanel` on the Dashboard matching existing permission styles.
- **Generic Brand-Agnostic Settings Integration**: Uses standard Android 9+ `ActivityManager.IsBackgroundRestricted` detection and launches native `App Info` (`Settings.ActionApplicationDetailsSettings`), working reliably across all phone models (TECNO, Xiaomi, Samsung, Google Pixel, OnePlus, Vivo, Oppo) without fragile OEM-specific intents.

### Changed
- **Official Package ID Migration**: Migrated Android `ApplicationId`, FileProvider, and iOS identifiers to `com.mrblackaio.tiktokautostreak`.
- **System-Wide Rebranding**: Completely removed legacy `Jon2G` references across constants, bridge files, project configurations, and documentation in favor of `Mrblack` / `MrblackAIO`.

### Fixed
- **Chat Automation Speed & Reliability**: Fixed the regression where messaging failed and looped through 50 scrolls. Grounded chat selection in upstream logic and removed the broken `isInsideChatList` selector which falsely disqualified TikTok's `ConversationHeader`.
- **Top Conversation False-Positive**: Fixed the bug where the first friend in the inbox failed on missing input. The automation now requires a live message input box before considering an open chat active, and explicitly clicks conversation items in the inbox.
- **Ultra-Fast Inbox Scanning**: Added pre-click matching for both profile links (`a[href*="/@"]`) and inline `@username` text in the conversation sidebar, eliminating 1.5s per-item delays and reducing messaging to ~2-3 seconds per friend.
- **Profile Handle Extraction (@tiktok_user)**: Solved the root cause of `@tiktok_user` displaying after login. Replaced unawaited WebView Promises with direct C# authenticated HTTP retrieval via `passport/web/account/info/` using session cookies, instantly extracting the real TikTok `unique_id`.
- **Background Profile Sync**: Automatically syncs profile details and avatar on ProfilePage appearance and streak execution if the handle is not yet populated.

## [2.0.10] - 2026-11-11

### Fixed
- **Profile Username Extraction**: Restored authenticated `passport/web/account/info/` API inspection inside WebView to automatically extract the true TikTok account handle (`unique_id`), eliminating the fallback `@tiktok_user` display.
- **Manual Handle Override**: Enabled tap gestures on `@username` on the Profile page allowing users to directly edit, set, or correct their TikTok handle via prompt dialog.
- **History Last Run 12H Time Display**: Replaced the persistent `"Last run: N/A"` label with the actual 12-hour AM/PM execution timestamp (e.g. `Last run: Today at 6:46 PM` or `Last run: Yesterday at 8:30 AM`).
- **Immediate Execution Time Sync**: Guaranteed that triggering "RUN AUTOMATION NOW" from the Dashboard or background service executions instantly updates `_settingsService.SetLastRunTime(DateTime.Now)`.

## [2.0.9] - 2026-11-11

### Added
- **Privacy & Transparency Hub Overhaul**: Completely redesigned the in-app Privacy Hub (`PrivacyPage.xaml`) into a futuristic Obsidian dashboard featuring an illuminated shield emblem (`🛡️`), architectural trust guarantees, and direct interactive action cards.
- **Direct GitHub Resource Cards**: Added dedicated redirect cards and buttons for:
  - Official Repository: `https://github.com/MrblackAIO/TikTok-Auto-Streak`
  - Issue Tracker & Bug Reports: `https://github.com/MrblackAIO/TikTok-Auto-Streak/issues`
  - Releases & Signed APK Downloads: `https://github.com/MrblackAIO/TikTok-Auto-Streak/releases`
  - Official Privacy Policy Document: `https://github.com/MrblackAIO/TikTok-Auto-Streak/blob/main/PRIVACY.md`
  - Open Source License: `https://github.com/MrblackAIO/TikTok-Auto-Streak/blob/main/LICENSE`
- **In-Hub Local Data Purge**: Added a `"Purge Session & Local Data"` button directly inside the Privacy Hub to wipe stored session cookies, cache, and history with confirmation.

### Changed
- **Complete Rebranding to Mrblack**: Systematically replaced all legacy `Jon2G` references, documentation links, and author credits throughout the app, `PRIVACY.md`, `LICENSE`, web showcase (`src/Web`), and documentation with `Mrblack` and `MrblackAIO/TikTok-Auto-Streak`.

## [2.0.8] - 2026-11-11

### Added
- **Friends Streak Day Count Input**: Added a prompt sheet to friend "Edit" action allowing users to directly adjust and enter their current consecutive streak day count (`friend.StreakCount`).
- **Streak Count Auto-Increment**: Guaranteed automatic streak day counter increment (+1) after each successfully sent message, even when TikTok DM headers lack OCR scrape markers.
- **Editable Profile Name & Bio**: Added interactive pencil icon (`✏️`) beside display name and edit button on Bio with default `"Add your bio"` placeholder.
- **Camera Overlay Badge**: Added an avatar camera badge overlay (`📷`) at the bottom-right of the user profile photo signaling photo customization.
- **Immediate 12-Hour Schedule Arming**: Scheduling now defaults to fixed-time 12-hour AM/PM mode. Adjusting hour, minute, or AM/PM automatically arms the Android `AlarmManager` immediately for today or tomorrow and updates Home status.
- **Run Automation Now Schedule Sync**: Triggering "RUN AUTOMATION NOW" from the Home page captures the current device clock time, sets it as the new daily recurring schedule, and schedules the next automated run for tomorrow at that exact time.

### Changed
- **12-Hour AM/PM Time Format**: Standardized all user-facing schedules, last run timestamps, and verified session checks to 12-hour format with AM/PM (`h:mm tt`) instead of 24-hour military time.
- **Profile Layout Polish**: Removed Following and Followers metric capsules, resolved duplicate username display by prioritizing actual TikTok nickname / display name over handle.

### Added
- **Instant Custom Notification Sound (`new_notification.mp3`)**: Bundled custom audio directly into Android package raw resources (`res/raw/new_notification.mp3`).
- **Dedicated High-Priority Sound Channel (`streak_alerts_custom_v2`)**: Configured a dedicated NotificationChannel with `NotificationImportance.High` and hardware-level `AudioAttributes` (`Notification`, `Sonification`, `AudibilityEnforced`) for zero-lag heads-up alerts.
- **Instant Alert Dispatching**: Wired `StreakSoundNotificationHelper` into `StreakService` and `MainActivity` for immediate completion and retry notifications.

## [2.0.6] - 2026-11-11

### Fixed
- **StreakService Compilation Error**: Declared and initialized `_sessionService` inside `StreakService.cs`, resolving compile error `CS0103`.
- **Compiler Warnings Cleanup**: Converted `ApplyLoginButtonState` in `ProfilePage.xaml.cs` to a clean synchronous method eliminating warning CS1998, and suppressed Android 35+ obsolete API warning CA1422 in `MainActivity.cs`.

## [2.0.5] - 2026-11-11

### Added
- **Passive TikTok Profile Extraction**: Extracted account avatar, display name, handle, bio, following and followers counts via zero-network-signature SSR hydration inspection (`__UNIVERSAL_DATA_FOR_REHYDRATION__`) avoiding anti-bot detection.
- **Existing Session Auto-Sync**: Automatically syncs profile details in the background on `ProfilePage` for already authenticated sessions, plus on-demand tap-to-sync.
- **Live Streak Synchronization**: Automation dynamically scrapes the true streak value from active TikTok chat room headers and updates each friend's real streak count.

### Fixed
- **Arc Gauge Text Overlap**: Rephrased the progress gauge subtitle from `"Daily Streak Saver active"` to `"Streaks Active"` with optimized font sizing and vertical alignment, preventing edge collisions.
- **Friends Streak Display**: Replaced the static `"🔥 12 days"` placeholder with the friend's actual verified live streak count.

## [2.0.4] - 2026-11-11

### Fixed
- **Profile Navigation Crash**: Resolved fatal `System.InvalidCastException` thrown during `ProfilePage.OnAppearing` due to an invalid cast of `ObsidianBorder` color resource to `Brush` in `UpdateAmPmPills`.
- **Safe Dynamic Colors**: Standardized dynamic theme color resolution using `GetThemeColor` with fallback defaults, preventing UI thread unhandled exceptions.
- **Event Recursion Safeguard**: Prevented potential event loops between native `TimePicker` and the HH:MM input fields using `_suppressTimePickerChanged`.

## [2.0.3] - 2026-11-11

### Added
- **TikTok Account Profile Card**: When logged in, the Session Settings card renders as a full TikTok profile displaying user avatar, display name, handle (`@username`), following & follower stats, and bio.
- **HH:MM Schedule Input Fields**: Replaced the 1440-minute swipe slider with precise side-by-side numeric input boxes (`HH` and `MM`) with AM/PM selection capsules.

### Changed
- **Home Page Avatar**: Replaced default lightning (`⚡`) with fire (`🔥`). Shows circular TikTok profile picture when logged in.
- **Generic Defaults**: Removed hardcoded "Mrblack" operator placeholder and default name on Profile and Home pages, defaulting to generic user or TikTok nickname.
- **Attribution Cleanup**: Changed "Custom Engineered for Mrblack" to "Engineered by Mrblack" in the release documentation and About screens.

## [2.0.2] - 2026-11-11

### Fixed

- **Navigation Bar Tap Targets**: Replaced narrow 72px column layouts with evenly divided 80x64 full-cell star columns (`*,*,*,*`) and explicit transparent backdrops, eliminating dead zones and accidental mis-taps between tabs.
- **Debounced Route Switching**: Added an asynchronous navigation re-entrancy lock (`_isNavigating`) and non-animated route dispatch to prevent MAUI Shell route collisions and incorrect page routing when tapping tabs in rapid succession.
- **Active Tab Synchronization**: Fixed visual state desynchronization so the navbar indicator strictly matches the current active page, with clean visual restoration if navigation is interrupted.
- **Tactile Touch Feedback**: Added lightweight non-blocking micro-scale animations (0.88x scale on press) for instant tactile interactivity.

## [2.2.0] - 2026-10-17

### Added

- Live activity on the Dashboard while a streak run is in progress.
- Retry Failed for today's failed friends and groups, without changing the daily schedule.
- Daily progress that counts each chat once and lets a later successful retry replace an earlier failure.
- Completion notifications that name up to five failed chats, with a count when there are more.
- Green, yellow, and red health indicators based on consecutive send failures.
- A chat priority setting: mixed order, friends first, or groups first.

### Fixed

- Login no longer follows TikTok app links such as `snssdk://` or `intent://`, which closed the page with an unknown URL scheme.
- The desktop browser identity is applied before the first login request, and login cookies are saved before the session is marked valid.
- Background automation starts its browser on the UI thread, uses a screen-density-scaled inbox, and resumes browser timers after login.
- Inbox lookup waits longer and uses tighter chat selectors.
- A missing chat is disabled only after three failed runs.
- Exact alarms fall back to inexact alarms when Android blocks exact scheduling.
- A low-battery run starts only when an enabled chat still needs a message, and it is recorded only after the run actually starts.

[2.2.0]: https://github.com/MrblackAIO/TikTok-Auto-Streak/releases
