# Changelog

All notable changes to Streak Saver are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project follows [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

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

[2.2.0]: https://github.com/Jon2G/TiktokStreakSaver/compare/v2.1.4...v2.2.0
