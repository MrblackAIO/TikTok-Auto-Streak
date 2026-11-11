using Microsoft.Maui.Graphics;

namespace TiktokStreakSaver.Views;

/// <summary>
/// Custom drawable for the Radial Neon Progress Arc Gauge on the Dashboard.
/// Renders an obsidian track and a glowing neon accent arc with center ratio text.
/// </summary>
[Microsoft.Maui.Controls.Internals.Preserve(AllMembers = true)]
public class NormalProgressDrawable : IDrawable
{
    public float Progress { get; set; }
    public bool IsDarkTheme { get; set; } = true;
    public string RatioText { get; set; } = "0/0";
    public string Subtitle { get; set; } = "Streaks Active";

    private static readonly Color TrackColor = Color.FromArgb("#161B24");
    private static readonly Color ArcStartColor = Color.FromArgb("#8B5CF6"); // Violet
    private static readonly Color ArcEndColor = Color.FromArgb("#FF3B5C");   // Coral-Red
    private static readonly Color GlowColor = Color.FromArgb("#40FF3B5C");
    private static readonly Color TextWhite = Color.FromArgb("#FFFFFF");
    private static readonly Color TextMuted = Color.FromArgb("#8E95A5");

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        var cx = dirtyRect.Width / 2f;
        var cy = dirtyRect.Height / 2f - 4f;
        var radius = Math.Min(cx, cy) - 14f;
        if (radius <= 0) return;

        var stroke = 9f;
        var startAngle = 140f;
        var sweepRange = 260f; // 260 degree arc gauge

        // 1. Draw Background Track Arc
        canvas.StrokeColor = TrackColor;
        canvas.StrokeSize = stroke;
        canvas.StrokeLineCap = LineCap.Round;
        canvas.DrawArc(
            cx - radius, cy - radius,
            radius * 2, radius * 2,
            startAngle,
            startAngle - sweepRange,
            true, false);

        // 2. Draw Active Neon Progress Arc
        var safeProgress = Math.Clamp(Progress, 0f, 1f);
        if (safeProgress > 0.01f)
        {
            var activeSweep = sweepRange * safeProgress;
            canvas.SetShadow(new SizeF(0, 0), 10, GlowColor);
            canvas.StrokeColor = ArcEndColor;
            canvas.StrokeSize = stroke;
            canvas.StrokeLineCap = LineCap.Round;

            canvas.DrawArc(
                cx - radius, cy - radius,
                radius * 2, radius * 2,
                startAngle,
                startAngle - activeSweep,
                true, false);

            canvas.SetShadow(SizeF.Zero, 0, Colors.Transparent);
        }

        // 3. Draw Center Metrics (Ratio & Subtitle)
        canvas.FontColor = TextWhite;
        canvas.FontSize = 26f;
        canvas.Font = Microsoft.Maui.Graphics.Font.Default;
        canvas.DrawString(
            RatioText,
            0, cy - 20f,
            dirtyRect.Width, 32f,
            HorizontalAlignment.Center, VerticalAlignment.Center);

        canvas.FontColor = TextMuted;
        canvas.FontSize = 10.5f;
        canvas.DrawString(
            Subtitle,
            0, cy + 12f,
            dirtyRect.Width, 18f,
            HorizontalAlignment.Center, VerticalAlignment.Center);
    }
}
