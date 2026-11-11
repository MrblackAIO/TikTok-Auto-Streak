using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;
using AndroidX.Core.Content;

namespace TiktokStreakSaver
{
    [Activity(Theme = "@style/App.StartingTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    [Microsoft.Maui.Controls.Internals.Preserve(AllMembers = true)]
    public class MainActivity : MauiAppCompatActivity
    {
        private const int NotificationPermissionRequestCode = 1001;

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            if (Window != null)
            {
                var darkColor = Android.Graphics.Color.ParseColor("#0A0D12");
                Window.SetStatusBarColor(darkColor);
                Window.SetNavigationBarColor(darkColor);

                AndroidX.Core.View.WindowCompat.SetDecorFitsSystemWindows(Window, false);

                var controller = AndroidX.Core.View.WindowCompat.GetInsetsController(Window, Window.DecorView);
                if (controller != null)
                {
                    controller.AppearanceLightStatusBars = false;
                    controller.AppearanceLightNavigationBars = false;
                }
            }
            
            // Create notification channel on app start
            CreateNotificationChannel();
            CreateUpdateNotificationChannel();

            // Handle direct update intent from notification tap
            HandleUpdateIntent(Intent);
            
            // Request notification permission for Android 13+
            RequestNotificationPermission();
        }

        protected override void OnNewIntent(Intent? intent)
        {
            base.OnNewIntent(intent);
            HandleUpdateIntent(intent);
        }

        private void HandleUpdateIntent(Intent? intent)
        {
            if (intent?.GetBooleanExtra("trigger_update", false) == true)
            {
                var version = intent.GetStringExtra("update_version") ?? "";
                var url = intent.GetStringExtra("update_url");

                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    var modal = new AboutPopupPage(
                        "App Update",
                        version,
                        "",
                        isUpdate: true,
                        apkDownloadUrl: url);

                    if (Microsoft.Maui.Controls.Application.Current?.Windows.Count > 0 &&
                        Microsoft.Maui.Controls.Application.Current.Windows[0].Page != null)
                    {
                        await Microsoft.Maui.Controls.Application.Current.Windows[0].Page!.Navigation.PushModalAsync(modal);
                    }
                });
            }
        }

        private void CreateUpdateNotificationChannel()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                var channelId = "app_updates_channel";
                var channelName = "App Updates";
                var channelDescription = "Notifies when a new version of TikTok AutoStreak is released";
                var importance = NotificationImportance.High;

                var channel = new NotificationChannel(channelId, channelName, importance)
                {
                    Description = channelDescription
                };

                var notificationManager = (NotificationManager?)GetSystemService(NotificationService);
                notificationManager?.CreateNotificationChannel(channel);
            }
        }

        private void CreateNotificationChannel()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                var channelId = "streak_service_channel";
                var channelName = "Streak Service";
                var channelDescription = "Notifications for TikTok streak automation";
                var importance = NotificationImportance.Low;
                
                var channel = new NotificationChannel(channelId, channelName, importance)
                {
                    Description = channelDescription
                };
                
                var notificationManager = (NotificationManager?)GetSystemService(NotificationService);
                notificationManager?.CreateNotificationChannel(channel);
            }
        }

        private void RequestNotificationPermission()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu)
            {
                if (ContextCompat.CheckSelfPermission(this, Android.Manifest.Permission.PostNotifications) 
                    != Permission.Granted)
                {
                    ActivityCompat.RequestPermissions(this, 
                        new[] { Android.Manifest.Permission.PostNotifications }, 
                        NotificationPermissionRequestCode);
                }
            }
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }
}
