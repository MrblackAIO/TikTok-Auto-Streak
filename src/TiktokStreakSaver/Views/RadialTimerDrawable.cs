using Microsoft.Maui.Graphics;

namespace TiktokStreakSaver.Views;

/// <summary>
/// Custom drawable for the futuristic glowing digital arc dial timer on Preferences &amp; Schedule.
/// Displays an obsidian track, a glowing cyan/violet arc, and center digital time.
/// </summary>
[Microsoft.Maui.Controls.Internals.Preserve(AllMembers = true)]
public class RadialTimerDrawable : IDrawable
{
    public float Progress { get; set; } = 0.5f; // 12:00 = 0.5
    public string TimeText { get; set; } = "12:00";
    public string PeriodText { get; set; } = "PM";

    private static readonly Color TrackColor = Color.FromArgb("#161B24");
    private static readonly Color ArcColor = Color.FromArgb("#8B5CF6"); // Violet-Blue
    private static readonly Color GlowColor = Color.FromArgb("#508B5CF6");
    private static readonly Color TextWhite = Color.FromArgb("#FFFFFF");
    private static readonly Color TextMuted = Color.FromArgb("#8E95A5");

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        var cx = dirtyRect.Width / 2f;
        var cy = dirtyRect.Height / 2f;
        var radius = Math.Min(cx, cy) - 12f;
        if (radius <= 0) return;

        var stroke = 8f;

        // Background track circle
        canvas.StrokeColor = TrackColor;
        canvas.StrokeSize = stroke;
        canvas.StrokeLineCap = LineCap.Round;
        canvas.DrawCircle(cx, cy, radius);

        // Glowing Active Arc
        var safeProgress = Math.Clamp(Progress, 0.02f, 1f);
        var sweepAngle = safeProgress * 360f;

        canvas.SetShadow(new SizeF(0, 0), 12, GlowColor);
        canvas.StrokeColor = ArcColor;
        canvas.StrokeSize = stroke;
        canvas.StrokeLineCap = LineCap.Round;

        canvas.DrawArc(
            cx - radius, cy - radius,
            radius * 2, radius * 2,
            90f,
            90f - sweepAngle,
            true, false);

        canvas.SetShadow(SizeF.Zero, 0, Colors.Transparent);

        // Period Indicator (e.g. "PM" or "AM")
        canvas.FontColor = TextMuted;
        canvas.FontSize = 11f;
        canvas.Font = Microsoft.Maui.Graphics.Font.Default;
        canvas.DrawString(PeriodText, 0, cy - 28f, dirtyRect.Width, 18f,
            HorizontalAlignment.Center, VerticalAlignment.Center);

        // Digital Time Display (e.g. "12:00")
        canvas.FontColor = TextWhite;
        canvas.FontSize = 26f;
        canvas.Font = Microsoft.Maui.Graphics.Font.Default;
        canvas.DrawString(TimeText, 0, cy - 8f, dirtyRect.Width, 32f,
            HorizontalAlignment.Center, VerticalAlignment.Center);
    }
}
