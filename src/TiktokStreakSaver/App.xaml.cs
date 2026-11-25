namespace TiktokStreakSaver;

[Microsoft.Maui.Controls.Internals.Preserve(AllMembers = true)]
public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        Services.ThemeService.Current.Initialize();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
#if IOS
        if (!Services.Storage.AppStorageProvider.Current.GetBool(AppConstants.IosOnboardingCompleteKey))
            return new Window(new NavigationPage(new Pages.IosOnboardingPage()));
#endif
        return new Window(new Pages.SplashPage());
    }
}
