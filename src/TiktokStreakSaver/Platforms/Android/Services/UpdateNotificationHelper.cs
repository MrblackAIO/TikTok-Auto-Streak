using Android.App;
using Android.Content;
using AndroidX.Core.App;
using TiktokStreakSaver.Services;

namespace TiktokStreakSaver.Platforms.Android.Services;

[Microsoft.Maui.Controls.Internals.Preserve(AllMembers = true)]
public static class UpdateNotificationHelper
{
    private const string ChannelId = "app_updates_channel";
    private const int NotificationId = 9001;

    public static async Task CheckAndNotifyUpdateAsync(Context context)
    {
        try
        {
            var updateService = new UpdateService();
            var updateInfo = await updateService.CheckForUpdatesAsync();

            if (updateInfo?.HasUpdate == true && !string.IsNullOrEmpty(updateInfo.ApkDownloadUrl))
            {
                var intent = new Intent(context, typeof(MainActivity));
                intent.PutExtra("trigger_update", true);
                intent.PutExtra("update_version", updateInfo.LatestVersion);
                intent.PutExtra("update_url", updateInfo.ApkDownloadUrl);
                intent.SetFlags(ActivityFlags.SingleTop | ActivityFlags.ClearTop);

                var pendingIntent = PendingIntent.GetActivity(
                    context,
                    1001,
                    intent,
                    PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

                var notification = new NotificationCompat.Builder(context, ChannelId)
                    .SetSmallIcon(Resource.Mipmap.appicon)
                    .SetContentTitle("New Update Available")
                    .SetContentText($"Version v{updateInfo.LatestVersion} is available to install.")
                    .SetPriority(NotificationCompat.PriorityHigh)
                    .SetContentIntent(pendingIntent)
                    .SetAutoCancel(true)
                    .AddAction(0, "Install Now", pendingIntent)
                    .Build();

                var manager = NotificationManagerCompat.From(context);
                manager.Notify(NotificationId, notification);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[UpdateCheck] Background check error: {ex.Message}");
        }
    }
}
