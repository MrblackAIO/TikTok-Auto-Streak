using Microsoft.Maui.Graphics;

namespace TiktokStreakSaver.Views;

/// <summary>
/// Custom drawable for the circular performance gauge on Analytics &amp; Log.
/// </summary>
[Microsoft.Maui.Controls.Internals.Preserve(AllMembers = true)]
public class SuccessRateDrawable : IDrawable
{
    public float SuccessRate { get; set; }
    public string RateText { get; set; } = "—";
    public string SubText { get; set; } = "Success Rate";
    public bool IsDarkTheme { get; set; } = true;

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        var palette = Services.ThemeService.Current.CurrentPalette;
        var trackColor = palette.SecondaryBackground;
        var arcColor = palette.Success;
        var glowColor = palette.Success.WithAlpha(0.28f);
        var textPrimary = palette.PrimaryText;
        var textMuted = palette.SecondaryText;

        var cx = dirtyRect.Width / 2f;
        var cy = dirtyRect.Height / 2f;
        var radius = Math.Min(cx, cy) - 10f;
        if (radius <= 0) return;

        var stroke = 8f;

        // Draw track circle
        canvas.StrokeColor = trackColor;
        canvas.StrokeSize = stroke;
        canvas.StrokeLineCap = LineCap.Round;
        canvas.DrawCircle(cx, cy, radius);

        // Draw active arc with neon glow
        if (SuccessRate > 0.005f)
        {
            canvas.SetShadow(new SizeF(0, 0), 8, glowColor);
            canvas.StrokeColor = arcColor;
            canvas.StrokeSize = stroke;
            canvas.StrokeLineCap = LineCap.Round;

            if (SuccessRate >= 0.999f)
            {
                canvas.DrawCircle(cx, cy, radius);
            }
            else
            {
                var sweepAngle = SuccessRate * 360f;
                canvas.DrawArc(
                    cx - radius, cy - radius,
                    radius * 2, radius * 2,
                    90f,
                    90f - sweepAngle,
                    true, false);
            }
            canvas.SetShadow(SizeF.Zero, 0, Colors.Transparent);
        }

        // Draw Center Text
        canvas.FontColor = textPrimary;
        canvas.FontSize = 24f;
        canvas.Font = Microsoft.Maui.Graphics.Font.Default;

        var textY = string.IsNullOrEmpty(SubText) ? cy : cy - 8f;
        canvas.DrawString(RateText, 0, textY - 12f, dirtyRect.Width, 26f,
            HorizontalAlignment.Center, VerticalAlignment.Center);

        if (!string.IsNullOrEmpty(SubText))
        {
            canvas.FontSize = 10f;
            canvas.FontColor = textMuted;
            canvas.DrawString(SubText, 0, textY + 12f, dirtyRect.Width, 18f,
                HorizontalAlignment.Center, VerticalAlignment.Center);
        }
    }
}
