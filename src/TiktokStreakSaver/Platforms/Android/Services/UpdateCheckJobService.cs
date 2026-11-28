using Android.App;
using Android.App.Job;
using Android.Content;
using Android.OS;

namespace TiktokStreakSaver.Platforms.Android.Services;

/// <summary>
/// Standalone periodic <see cref="JobService"/> (every 6 hours) that checks GitHub Releases
/// for newer APK updates in the background, even when the app UI is closed or between daily
/// streak runs.
/// </summary>
[Service(
    Name = AppConstants.PackageName + ".Services.UpdateCheckJobService",
    Permission = "android.permission.BIND_JOB_SERVICE",
    Exported = false)]
[Microsoft.Maui.Controls.Internals.Preserve(AllMembers = true)]
public class UpdateCheckJobService : JobService
{
    public const int JobId = 2003;
    private const long IntervalMillis = 6L * 60L * 60L * 1000L; // 6 hours
    private readonly object _jobLock = new();
    private CancellationTokenSource? _jobCts;

    /// <summary>
    /// Registers the 6-hour persisted update check job if not already scheduled.
    /// </summary>
    public static void Schedule(Context context)
    {
        try
        {
            var scheduler = (JobScheduler?)context.GetSystemService(Context.JobSchedulerService);
            if (scheduler == null)
            {
                System.Diagnostics.Debug.WriteLine("UpdateCheckJobService: JobScheduler unavailable");
                return;
            }

            if (Build.VERSION.SdkInt >= BuildVersionCodes.N && scheduler.GetPendingJob(JobId) != null)
                return;

            var component = new ComponentName(context, Java.Lang.Class.FromType(typeof(UpdateCheckJobService)));
            var builder = new JobInfo.Builder(JobId, component)
                .SetRequiredNetworkType(NetworkType.Any)
                .SetPeriodic(IntervalMillis)
                .SetPersisted(true);

            var job = builder.Build();
            if (job == null)
            {
                System.Diagnostics.Debug.WriteLine("UpdateCheckJobService: JobInfo.Builder.Build() returned null");
                return;
            }

            var result = scheduler.Schedule(job);
            System.Diagnostics.Debug.WriteLine($"UpdateCheckJobService: Schedule result = {result}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"UpdateCheckJobService.Schedule failed: {ex.Message}");
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
            try
            {
                bool validated = await NetworkConnectivity.WaitForValidatedInternetAsync(
                    this, maxWaitMs: 2500, pollIntervalMs: 500, cancellationToken: ct);

                if (validated && !ct.IsCancellationRequested)
                {
                    await UpdateNotificationHelper.CheckAndNotifyUpdateAsync(this, ct);
                }
            }
            catch (System.OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateCheckJobService.OnStartJob error: {ex.Message}");
            }

            if (!ct.IsCancellationRequested)
            {
                try
                {
                    JobFinished(@params, wantsReschedule: false);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"UpdateCheckJobService.JobFinished failed: {ex.Message}");
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
        return true;
    }
}
