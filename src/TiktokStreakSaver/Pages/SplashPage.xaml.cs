namespace TiktokStreakSaver.Pages;

[Microsoft.Maui.Controls.Internals.Preserve(AllMembers = true)]
public partial class SplashPage : ContentPage
{
    private bool _hasAnimated;

    public SplashPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_hasAnimated) return;
        _hasAnimated = true;

        // Initial setup for ultra-smooth entrance
        LogoImage.Opacity = 0;
        LogoImage.Scale = 0.82;
        LogoImage.TranslationY = 12;

        TitleStack.Opacity = 0;
        TitleStack.TranslationY = 16;

        FooterStack.Opacity = 0;
        FooterStack.TranslationY = 10;

        // Stage 1: Logo smooth scale and fade-in (600ms cubic-out)
        await Task.WhenAll(
            LogoImage.FadeTo(1.0, 600, Easing.CubicOut),
            LogoImage.ScaleTo(1.0, 600, Easing.CubicOut),
            LogoImage.TranslateTo(0, 0, 600, Easing.CubicOut));

        // Stage 2: Staggered title entrance (500ms cubic-out)
        await Task.WhenAll(
            TitleStack.FadeTo(1.0, 500, Easing.CubicOut),
            TitleStack.TranslateTo(0, 0, 500, Easing.CubicOut));

        // Stage 3: Footer branding reveal
        _ = Task.WhenAll(
            FooterStack.FadeTo(1.0, 500, Easing.CubicOut),
            FooterStack.TranslateTo(0, 0, 500, Easing.CubicOut));

        // Stage 4: Premium loading presentation interval
        await Task.Delay(1300);

        // Stage 5: Buttery smooth cross-fade exit into AppShell
        await MainContentGrid.FadeTo(0, 260, Easing.SinInOut);

        if (this.Window != null)
        {
            this.Window.Page = new AppShell();
        }
        else if (Application.Current?.Windows.Count > 0)
        {
            Application.Current.Windows[0].Page = new AppShell();
        }
    }
}
