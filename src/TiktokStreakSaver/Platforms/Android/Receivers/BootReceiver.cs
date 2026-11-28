using Android.App;
using Android.Content;
using TiktokStreakSaver;
using TiktokStreakSaver.Platforms.Android;
using TiktokStreakSaver.Platforms.Android.Services;
using TiktokStreakSaver.Services;

namespace TiktokStreakSaver.Platforms.Android.Receivers;

[Microsoft.Maui.Controls.Internals.Preserve(AllMembers = true)]
[BroadcastReceiver(Name = AppConstants.PackageName + ".Receivers.BootReceiver", Enabled = true, Exported = true)]
[IntentFilter(new[] { Intent.ActionBootCompleted, "android.intent.action.QUICKBOOT_POWERON", Intent.ActionMyPackageReplaced })]
public class BootReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent)
    {
        if (context == null || intent == null) return;

        if (intent.Action == Intent.ActionBootCompleted || 
            intent.Action == "android.intent.action.QUICKBOOT_POWERON" ||
            intent.Action == Intent.ActionMyPackageReplaced)
        {
            UpdateCheckJobService.Schedule(context);

            var settingsService = new SettingsService();
            
            // Only reschedule if it was previously scheduled
            if (settingsService.IsScheduled())
            {
                StreakScheduler.ScheduleNextRun(context);
            }
            else
            {
                OfflineRecoveryJobService.Cancel(context);
            }
        }
    }
}



