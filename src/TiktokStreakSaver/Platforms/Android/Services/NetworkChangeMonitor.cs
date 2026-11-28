using Android.App;
using Android.Content;
using Android.Net;
using Android.OS;
using AndroidX.Core.App;
using TiktokStreakSaver.Services;

namespace TiktokStreakSaver.Platforms.Android.Services;

/// <summary>
/// Listens for connectivity changes via <see cref="ConnectivityManager"/> and, when the
/// device regains Wi‑Fi or cellular internet AND same-day offline recovery (or an active
/// in-day retry) is pending, triggers a recovery run through <see cref="StreakScheduler.RunNow"/>.
///
/// Registered once from <see cref="MainApplication.OnCreate"/> for instant in-process detection;
/// <see cref="OfflineRecoveryJobService"/> handles waking the app across process death.
/// </summary>
[Microsoft.Maui.Controls.Internals.Preserve(AllMembers = true)]
internal static class NetworkChangeMonitor
{
    internal enum RecoveryTriggerOutcome
    {
        Triggered,
        AlreadyRunning,
        ThrottledOrOffline,
        NotNeeded
    }

    /// <summary>
    /// Minimum time between two network-triggered recovery attempts. Persisted across
    /// process restarts so captive portals or flapping networks cannot cause retry loops.
    /// </summary>
    private static readonly TimeSpan RecoveryThrottle = TimeSpan.FromMinutes(20);

    private static readonly object _lock = new();
    private static DateTime _lastRecoveryAttemptUtc = DateTime.MinValue;
    private static ConnectivityManager.NetworkCallback? _callback;
    private static bool _registered;

    /// <summary>
    /// Register the default-network callback. Safe to call multiple times; only the
    /// first call has an effect.
    /// </summary>
    public static void Register(Context context)
    {
        lock (_lock)
        {
            if (_registered) return;

            var cm = (ConnectivityManager?)context.GetSystemService(Context.ConnectivityService);
            if (cm == null)
            {
                System.Diagnostics.Debug.WriteLine("NetworkChangeMonitor: ConnectivityManager unavailable, skipping registration");
                return;
            }

            try
            {
                _callback = new RecoveryNetworkCallback(context.ApplicationContext ?? context);
                cm.RegisterDefaultNetworkCallback(_callback);
                _registered = true;
                System.Diagnostics.Debug.WriteLine("NetworkChangeMonitor: registered default-network callback");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"NetworkChangeMonitor: registration failed — {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Shared recovery entry point used by both <see cref="RecoveryNetworkCallback"/> and
    /// <see cref="OfflineRecoveryJobService"/>. Enforces schedule, same-day anchor,
    /// connectivity, and persisted throttle guards before launching <see cref="StreakService"/>.
    /// </summary>
    internal static RecoveryTriggerOutcome TryTriggerOfflineRecovery(Context context, string source)
    {
        try
        {
            var settings = new SettingsService();

            if (!settings.IsScheduled())
            {
                settings.ClearOfflineRecoveryPending();
                OfflineRecoveryJobService.Cancel(context);
                return RecoveryTriggerOutcome.NotNeeded;
            }

            bool offlineRecoveryPending = settings.HasPendingOfflineRecoveryToday();
            bool standardRetryPending = settings.GetLastRunFailedToday() &&
                                        settings.GetTodayRetryCount() < SettingsService.MaxRetriesPerDay;

            if (!offlineRecoveryPending)
            {
                // Clear stale or previous-day markers and ensure no dangling JobScheduler job stays armed.
                settings.ClearOfflineRecoveryPending();
                OfflineRecoveryJobService.Cancel(context);

                if (!standardRetryPending)
                    return RecoveryTriggerOutcome.NotNeeded;
            }

            if (StreakService.IsRunning)
                return RecoveryTriggerOutcome.AlreadyRunning;

            // Require OS-validated internet so captive portals / dead routers don't burn the throttle.
            if (!NetworkConnectivity.HasWifiOrCellularValidatedInternet(context))
                return RecoveryTriggerOutcome.ThrottledOrOffline;

            lock (_lock)
            {
                var nowUtc = DateTime.UtcNow;
                var persistedLastUtc = settings.GetLastNetworkRecoveryUtc();
                var lastAttemptUtc = persistedLastUtc > _lastRecoveryAttemptUtc
                    ? persistedLastUtc
                    : _lastRecoveryAttemptUtc;

                if (nowUtc - lastAttemptUtc < RecoveryThrottle)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"NetworkChangeMonitor ({source}): throttled — last attempt too recent ({(nowUtc - lastAttemptUtc).TotalMinutes:F1}m ago)");
                    return RecoveryTriggerOutcome.ThrottledOrOffline;
                }

                _lastRecoveryAttemptUtc = nowUtc;
                settings.SetLastNetworkRecoveryUtc(nowUtc);
            }

            var reason = offlineRecoveryPending
                ? SettingsService.FailureReasonNoNetwork
                : (settings.GetLastRunFailureReason() ?? "unknown");
            System.Diagnostics.Debug.WriteLine(
                $"NetworkChangeMonitor ({source}): network restored, starting recovery run (reason: {reason})");

            return StreakScheduler.RunNow(context)
                ? RecoveryTriggerOutcome.Triggered
                : RecoveryTriggerOutcome.AlreadyRunning;
        }
        catch (ForegroundServiceStartNotAllowedException ex) when (Build.VERSION.SdkInt >= BuildVersionCodes.S)
        {
            System.Diagnostics.Debug.WriteLine(
                $"NetworkChangeMonitor ({source}): foreground service blocked from background — {ex.Message}");
            PostBlockedRecoveryNotificationOnceToday(context);
            return RecoveryTriggerOutcome.ThrottledOrOffline;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"NetworkChangeMonitor ({source}): {ex.Message}");
            return RecoveryTriggerOutcome.ThrottledOrOffline;
        }
    }

    private static void PostBlockedRecoveryNotificationOnceToday(Context context)
    {
        try
        {
            var settings = new SettingsService();
            if (settings.WasBlockedRecoveryAlertShownToday())
                return;

            var intent = new Intent(context, typeof(MainActivity));
            intent.SetFlags(ActivityFlags.NewTask | ActivityFlags.ClearTop);
            var pendingIntent = PendingIntent.GetActivity(
                context,
                1003,
                intent,
                PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

            var notification = StreakSoundNotificationHelper.CreateAlertBuilder(
                    context,
                    "TikTok AutoStreak — background start blocked",
                    "Internet returned, but Android blocked background recovery. Open the app or disable battery restrictions to enable automatic recovery.")
                .SetContentIntent(pendingIntent)
                .Build()!;

            NotificationManagerCompat.From(context).Notify(1003, notification);
            settings.MarkBlockedRecoveryAlertShownToday();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"PostBlockedRecoveryNotificationOnceToday error: {ex.Message}");
        }
    }

    private sealed class RecoveryNetworkCallback : ConnectivityManager.NetworkCallback
    {
        private readonly Context _context;

        public RecoveryNetworkCallback(Context context)
        {
            _context = context;
        }

        public override void OnAvailable(Network network)
        {
            base.OnAvailable(network);
            if (!NetworkConnectivity.HasWifiOrCellularValidatedInternet(_context))
                return;

            TryTriggerOfflineRecovery(_context, "NetworkCallback.OnAvailable");
        }

        public override void OnCapabilitiesChanged(Network network, NetworkCapabilities networkCapabilities)
        {
            base.OnCapabilitiesChanged(network, networkCapabilities);

            // Require Internet + Validated capabilities so captive portals don't trigger premature runs.
            if (!networkCapabilities.HasCapability(NetCapability.Internet)) return;
            if (!networkCapabilities.HasCapability(NetCapability.Validated)) return;

            var onWifi = networkCapabilities.HasTransport(TransportType.Wifi);
            var onCellular = networkCapabilities.HasTransport(TransportType.Cellular);
            if (!onWifi && !onCellular) return;

            TryTriggerOfflineRecovery(_context, "NetworkCallback.OnCapabilitiesChanged");
        }
    }
}
