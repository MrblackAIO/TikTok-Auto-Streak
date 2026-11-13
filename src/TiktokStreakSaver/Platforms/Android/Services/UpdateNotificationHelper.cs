using Android.App;
using Android.Content;
using Android.Media;
using Android.OS;
using AndroidX.Core.App;
using TiktokStreakSaver.Services;

namespace TiktokStreakSaver.Platforms.Android.Services;

[Microsoft.Maui.Controls.Internals.Preserve(AllMembers = true)]
public static class UpdateNotificationHelper
{
    public const string ChannelId = "app_updates_sound_v1";
    public const string ChannelName = "App Updates";
    private const int NotificationId = 9001;

    /// <summary>
    /// Registers the dedicated custom sound NotificationChannel for updates (API 26+).
    /// Bound to raw audio resource new_notification.mp3 with high importance.
    /// </summary>
    public static void CreateUpdateNotificationChannel(Context context)
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.O)
            return;

        var notificationManager = (NotificationManager?)context.GetSystemService(Context.NotificationService);
        if (notificationManager == null) return;

        var existingChannel = notificationManager.GetNotificationChannel(ChannelId);
        if (existingChannel != null) return;

        var channel = new NotificationChannel(
            ChannelId,
            ChannelName,
            NotificationImportance.High)
        {
            Description = "Instant alerts with custom sound when a new app update is available."
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
    /// Posts a high-priority update notification with custom sound (new_notification.mp3),
    /// target version, and direct "Install Now" tap action.
    /// </summary>
    public static void PostUpdateNotification(Context context, string version, string? downloadUrl)
    {
        try
        {
            CreateUpdateNotificationChannel(context);

            string cleanVersion = version.TrimStart('v', 'V');
            string title = $"New Update Available: v{cleanVersion}";
            string message = $"TikTok AutoStreak v{cleanVersion} is now available! Tap to review what's new and update.";

            var intent = new Intent(context, typeof(MainActivity));
            intent.PutExtra("trigger_update", true);
            intent.PutExtra("update_version", cleanVersion);
            intent.PutExtra("update_url", downloadUrl);
            intent.SetFlags(ActivityFlags.SingleTop | ActivityFlags.ClearTop);

            var pendingIntent = PendingIntent.GetActivity(
                context,
                1001,
                intent,
                PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

            var builder = new NotificationCompat.Builder(context, ChannelId)
                .SetSmallIcon(Resource.Drawable.ic_notification)
                .SetContentTitle(title)
                .SetContentText(message)
                .SetStyle(new NotificationCompat.BigTextStyle().BigText(message))
                .SetPriority(NotificationCompat.PriorityHigh)
                .SetCategory(NotificationCompat.CategoryStatus)
                .SetContentIntent(pendingIntent)
                .SetAutoCancel(true)
                .AddAction(0, "Install Now", pendingIntent);

            if (Build.VERSION.SdkInt < BuildVersionCodes.O)
            {
                var soundUri = global::Android.Net.Uri.Parse(
                    $"{ContentResolver.SchemeAndroidResource}://{context.PackageName}/raw/new_notification");
                builder.SetSound(soundUri);
                builder.SetDefaults((int)NotificationDefaults.Vibrate);
            }

            var manager = NotificationManagerCompat.From(context);
            manager.Notify(NotificationId, builder.Build());
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[UpdateNotification] Failed to post update notification: {ex.Message}");
        }
    }

    /// <summary>
    /// Background check helper to query GitHub and post an update notification if a new version is available.
    /// </summary>
    public static async Task CheckAndNotifyUpdateAsync(Context context)
    {
        try
        {
            var updateService = new UpdateService();
            var updateInfo = await updateService.CheckForUpdatesAsync();

            if (updateInfo?.HasUpdate == true && !string.IsNullOrEmpty(updateInfo.ApkDownloadUrl))
            {
                PostUpdateNotification(context, updateInfo.LatestVersion, updateInfo.ApkDownloadUrl);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[UpdateCheck] Background check error: {ex.Message}");
        }
    }
}
