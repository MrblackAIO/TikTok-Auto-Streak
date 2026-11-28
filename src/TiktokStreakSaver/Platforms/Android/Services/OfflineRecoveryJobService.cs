using Android.App;
using Android.App.Job;
using Android.Content;
using Android.OS;
using TiktokStreakSaver.Services;

namespace TiktokStreakSaver.Platforms.Android.Services;

/// <summary>
/// Persistent <see cref="JobService"/> that wakes the app when internet connectivity
/// (<see cref="NetworkType.Any"/>) is restored after same-day <c>no_network</c> failures,
/// even if the OS killed the app process during a multi-hour outage or exhausted the
/// 3 hourly <see cref="AlarmManager"/> retries.
/// </summary>
[Service(
    Name = AppConstants.PackageName + ".Services.OfflineRecoveryJobService",
    Permission = "android.permission.BIND_JOB_SERVICE",
    Exported = false)]
[Microsoft.Maui.Controls.Internals.Preserve(AllMembers = true)]
public class OfflineRecoveryJobService : JobService
{
    public const int JobId = 2002;
    private const long BackoffMillis = 15L * 60L * 1000L;
    private readonly object _jobLock = new();
    private CancellationTokenSource? _jobCts;

    /// <summary>
    /// Arm the persistent connectivity job if one is not already pending.
    /// Avoiding re-scheduling an existing pending job preserves exponential backoff
    /// during captive portals or flapping networks.
    /// </summary>
    public static void Schedule(Context context)
    {
        try
        {
            var scheduler = (JobScheduler?)context.GetSystemService(Context.JobSchedulerService);
            if (scheduler == null)
            {
                System.Diagnostics.Debug.WriteLine("OfflineRecoveryJobService: JobScheduler unavailable");
                return;
            }

            if (Build.VERSION.SdkInt >= BuildVersionCodes.N && scheduler.GetPendingJob(JobId) != null)
            {
                System.Diagnostics.Debug.WriteLine("OfflineRecoveryJobService: pending job already scheduled");
                return;
            }

            var component = new ComponentName(context, Java.Lang.Class.FromType(typeof(OfflineRecoveryJobService)));
            var builder = new JobInfo.Builder(JobId, component)
                .SetRequiredNetworkType(NetworkType.Any)
                .SetPersisted(true)
                .SetBackoffCriteria(BackoffMillis, BackoffPolicy.Exponential);

            var result = scheduler.Schedule(builder.Build());
            System.Diagnostics.Debug.WriteLine($"OfflineRecoveryJobService: Schedule result = {result}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"OfflineRecoveryJobService.Schedule failed: {ex.Message}");
        }
    }

    /// <summary>Cancel any pending same-day offline recovery job.</summary>
    public static void Cancel(Context context)
    {
        try
        {
            var scheduler = (JobScheduler?)context.GetSystemService(Context.JobSchedulerService);
            scheduler?.Cancel(JobId);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"OfflineRecoveryJobService.Cancel failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Ensures the persistent job state matches current scheduled & same-day recovery flags,
    /// cleaning up stale jobs from previous calendar days.
    /// </summary>
    internal static void SyncWithPendingState(Context context, SettingsService settings)
    {
        if (settings.IsScheduled() && settings.HasPendingOfflineRecoveryToday())
        {
            Schedule(context);
        }
        else
        {
            if (!settings.HasPendingOfflineRecoveryToday())
                settings.ClearOfflineRecoveryPending();
            Cancel(context);
        }
    }

    public override bool OnStartJob(JobParameters? @params)
    {
        CancellationTokenSource cts;
        lock (_jobLock)
        {
            _jobCts?.Cancel();
            _jobCts?.Dispose();
            _jobCts = new CancellationTokenSource();
            cts = _jobCts;
        }

        var ct = cts.Token;
        _ = Task.Run(async () =>
        {
            bool wantsReschedule = false;
            try
            {
                // NetworkType.Any often fires slightly before Android completes its captive-portal
                // / DNS validation probe. Poll briefly so real Wi-Fi/data isn't forced into 15m backoff.
                await NetworkConnectivity.WaitForValidatedInternetAsync(
                    this, maxWaitMs: 3000, pollIntervalMs: 500, cancellationToken: ct);

                if (ct.IsCancellationRequested)
                    return;

                var outcome = NetworkChangeMonitor.TryTriggerOfflineRecovery(this, "JobScheduler");
                if (outcome == NetworkChangeMonitor.RecoveryTriggerOutcome.Triggered)
                {
                    // Hold JobScheduler's wakelock briefly so StreakService.OnCreate() acquires
                    // its own foreground notification and WakeLock cleanly.
                    await Task.Delay(3000, ct);
                }
                else if (outcome == NetworkChangeMonitor.RecoveryTriggerOutcome.ThrottledOrOffline)
                {
                    var settings = new SettingsService();
                    if (settings.IsScheduled() && settings.HasPendingOfflineRecoveryToday())
                    {
                        wantsReschedule = true;
                    }
                    else
                    {
                        settings.ClearOfflineRecoveryPending();
                        Cancel(this);
                    }
                }
            }
            catch (System.OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OfflineRecoveryJobService.OnStartJob error: {ex.Message}");
            }

            if (!ct.IsCancellationRequested)
            {
                try
                {
                    JobFinished(@params, wantsReschedule);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"OfflineRecoveryJobService.JobFinished failed: {ex.Message}");
                }
            }
        });

        return true;
    }

    public override bool OnStopJob(JobParameters? @params)
    {
        lock (_jobLock)
        {
            _jobCts?.Cancel();
        }

        var settings = new SettingsService();
        return settings.IsScheduled() && settings.HasPendingOfflineRecoveryToday();
    }
}
