# Changelog

All notable changes to Streak Saver are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project follows [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

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
