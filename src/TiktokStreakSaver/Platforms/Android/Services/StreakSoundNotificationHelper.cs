using Android.App;
using Android.Content;
using Android.Media;
using Android.OS;
using AndroidX.Core.App;

namespace TiktokStreakSaver.Platforms.Android.Services;

/// <summary>
/// Helper to configure and dispatch notifications with instant custom sound playback (new_notification.mp3).
/// Configures a dedicated high-priority NotificationChannel bound to the raw audio resource.
/// </summary>
[Microsoft.Maui.Controls.Internals.Preserve(AllMembers = true)]
public static class StreakSoundNotificationHelper
{
    public const string AlertChannelId = "streak_alerts_custom_v2";
    public const string AlertChannelName = "Streak Alerts & Completion";

    /// <summary>
    /// Registers the dedicated custom sound NotificationChannel with Android OS (API 26+).
    /// </summary>
    public static void CreateStreakNotificationChannel(Context context)
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.O)
            return;

        var notificationManager = (NotificationManager?)context.GetSystemService(Context.NotificationService);
        if (notificationManager == null) return;

        var existingChannel = notificationManager.GetNotificationChannel(AlertChannelId);
        if (existingChannel != null) return;

        var channel = new NotificationChannel(
            AlertChannelId,
            AlertChannelName,
            NotificationImportance.High)
        {
            Description = "Instant alerts with custom sound when streak automations finish or require attention."
        };

        var soundUri = global::Android.Net.Uri.Parse(
            $"{ContentResolver.SchemeAndroidResource}://{context.PackageName}/raw/new_notification");

        var audioAttributes = new AudioAttributes.Builder()!
            .SetUsage(AudioUsageKind.Notification)!
            .SetContentType(AudioContentType.Sonification)!
            .SetFlags(AudioFlags.AudibilityEnforced)!
            .Build()!;

        channel.SetSound(soundUri, audioAttributes);
        channel.EnableVibration(true);
        channel.SetVibrationPattern(new long[] { 0, 180, 80, 180 });

        notificationManager.CreateNotificationChannel(channel);
    }

    /// <summary>
    /// Constructs a NotificationCompat.Builder pre-configured with the custom sound channel,
    /// high priority, auto-cancel, and backwards-compatible sound for Android 7.x.
    /// </summary>
    public static NotificationCompat.Builder CreateAlertBuilder(Context context, string title, string message)
    {
        CreateStreakNotificationChannel(context);

        var builder = new NotificationCompat.Builder(context, AlertChannelId)
            .SetSmallIcon(Resource.Drawable.ic_notification)
            .SetContentTitle(title)
            .SetContentText(message)
            .SetStyle(new NotificationCompat.BigTextStyle().BigText(message))
            .SetPriority(NotificationCompat.PriorityHigh)
            .SetCategory(NotificationCompat.CategoryStatus)
            .SetAutoCancel(true);

        if (Build.VERSION.SdkInt < BuildVersionCodes.O)
        {
            var soundUri = global::Android.Net.Uri.Parse(
                $"{ContentResolver.SchemeAndroidResource}://{context.PackageName}/raw/new_notification");
            builder.SetSound(soundUri);
            builder.SetDefaults((int)NotificationDefaults.Vibrate);
        }

        return builder;
    }

    /// <summary>
    /// Convenience helper to dispatch an instant sound completion alert.
    /// </summary>
    public static void PostCompletionAlert(Context context, string title, string message, PendingIntent? pendingIntent = null, int notificationId = 1002)
    {
        var builder = CreateAlertBuilder(context, title, message);
        if (pendingIntent != null)
        {
            builder.SetContentIntent(pendingIntent);
        }

        var notificationManager = NotificationManagerCompat.From(context);
        notificationManager.Notify(notificationId, builder.Build());
    }
}
