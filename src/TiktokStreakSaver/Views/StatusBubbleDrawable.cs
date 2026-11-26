using Microsoft.Maui.Graphics;
using TiktokStreakSaver.Services;

namespace TiktokStreakSaver.Views;

/// <summary>
/// Custom drawable for the Home profile Status Note popup bubble.
/// Draws a seamless speech-bubble silhouette with an upward tail aligned to the
/// profile avatar, subtle frosted glass sheen, glow shadow, and accent outline
/// synchronized with <see cref="ThemeService.Current"/>.
/// </summary>
[Microsoft.Maui.Controls.Internals.Preserve(AllMembers = true)]
public class StatusBubbleDrawable : IDrawable
{
    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        if (dirtyRect.Width < 36f || dirtyRect.Height < 28f)
            return;

        var palette = ThemeService.Current.CurrentPalette;
        const float padX = 5f;
        const float bottomPad = 6f;
        const float topY = 12f;
        const float tailTipY = 2f;

        float left = padX;
        float right = dirtyRect.Width - padX;
        float bottom = dirtyRect.Height - bottomPad;
        float cornerRadius = Math.Min(16f, Math.Min((right - left) / 4f, (bottom - topY) / 4f));

        // 44dp avatar inside matching 20dp right margin puts avatar center at width - 22dp.
        float tailTipX = Math.Clamp(dirtyRect.Width - 22f, left + cornerRadius + 14f, right - cornerRadius - 12f);
        const float tailHalfWidth = 7.5f;
        float tailLeftX = tailTipX - tailHalfWidth;
        float tailRightX = Math.Min(right - cornerRadius - 2f, tailTipX + tailHalfWidth);

        var bubblePath = new PathF();
        bubblePath.MoveTo(left + cornerRadius, topY);
        bubblePath.LineTo(tailLeftX, topY);
        bubblePath.LineTo(tailTipX, tailTipY);
        bubblePath.LineTo(tailRightX, topY);
        bubblePath.LineTo(right - cornerRadius, topY);
        bubblePath.QuadTo(right, topY, right, topY + cornerRadius);
        bubblePath.LineTo(right, bottom - cornerRadius);
        bubblePath.QuadTo(right, bottom, right - cornerRadius, bottom);
        bubblePath.LineTo(left + cornerRadius, bottom);
        bubblePath.QuadTo(left, bottom, left, bottom - cornerRadius);
        bubblePath.LineTo(left, topY + cornerRadius);
        bubblePath.QuadTo(left, topY, left + cornerRadius, topY);
        bubblePath.Close();

        // 1. Soft theme-tinted outer glow + translucent glass fill
        var glowColor = ColorContrastHelper.Blend(palette.Primary, palette.Secondary, 0.35f)
            .WithAlpha(palette.IsLight ? 0.24f : 0.42f);
        var fillStart = ColorContrastHelper.Blend(palette.CardBackground, palette.SecondaryBackground, 0.55f)
            .WithAlpha(palette.IsLight ? 0.96f : 0.92f);
        var fillEnd = ColorContrastHelper.Blend(palette.CardBackground, palette.Primary, palette.IsLight ? 0.06f : 0.14f)
            .WithAlpha(palette.IsLight ? 0.96f : 0.92f);

        canvas.SetShadow(new SizeF(0, 3), 12f, glowColor);
        canvas.SetFillPaint(new LinearGradientPaint
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(1, 1),
            GradientStops =
            [
                new PaintGradientStop(0f, fillStart),
                new PaintGradientStop(1f, fillEnd)
            ]
        }, dirtyRect);
        canvas.FillPath(bubblePath);
        canvas.SetShadow(SizeF.Zero, 0, Colors.Transparent);

        // 2. Subtle upper-left glass highlight sheen clipped to bubble silhouette
        canvas.SaveState();
        canvas.ClipPath(bubblePath);
        var sheenPath = new PathF();
        sheenPath.MoveTo(left, tailTipY);
        sheenPath.LineTo(left + (right - left) * 0.60f, tailTipY);
        sheenPath.LineTo(left + (right - left) * 0.32f, bottom);
        sheenPath.LineTo(left, bottom);
        sheenPath.Close();

        canvas.SetFillPaint(new LinearGradientPaint
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(0.7, 0.8),
            GradientStops =
            [
                new PaintGradientStop(0f, Colors.White.WithAlpha(palette.IsLight ? 0.45f : 0.12f)),
                new PaintGradientStop(1f, Colors.White.WithAlpha(0f))
            ]
        }, dirtyRect);
        canvas.FillPath(sheenPath);
        canvas.RestoreState();

        // 3. Crisp themed accent border outline
        canvas.StrokeColor = palette.IsLight
            ? ColorContrastHelper.Blend(palette.Primary, palette.Border, 0.45f)
            : ColorContrastHelper.Blend(palette.Primary, palette.Secondary, 0.40f).WithAlpha(0.88f);
        canvas.StrokeSize = 1.5f;
        canvas.DrawPath(bubblePath);
    }
}
