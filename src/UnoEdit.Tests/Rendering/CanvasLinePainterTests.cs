using System.Collections.Generic;
using ICSharpCode.AvalonEdit.Rendering;
using Microsoft.Graphics.Canvas;
using NUnit.Framework;
using SkiaSharp;
using UnoEdit.Skia.Desktop.Controls;

namespace UnoEdit.Tests.Rendering;

[TestFixture]
public class CanvasLinePainterTests
{
    static readonly Windows.UI.Color Black = Windows.UI.Color.FromArgb(255, 0, 0, 0);
    static readonly Windows.UI.Color Red = Windows.UI.Color.FromArgb(255, 220, 30, 30);

    // The OpenSans font Uno bundles, vendored into the test output so metrics resolve the same
    // typeface across Windows/macOS/Linux rather than depending on system-installed fonts.
    const string Font = "file://Assets/OpenSans-Regular.ttf";

    [Test]
    public void GetLineHeight_IsPositive_AndScalesWithFontSize()
    {
        double small = CanvasLinePainter.GetLineHeight(Font, 12);
        double large = CanvasLinePainter.GetLineHeight(Font, 24);

        Assert.That(small, Is.GreaterThan(0));
        Assert.That(large, Is.GreaterThan(small), "line height must grow with font size (no hardcoded 22px)");
    }

    [Test]
    public void Paint_DrawsVisibleGlyphs_OntoSurface()
    {
        using var bitmap = new SKBitmap(200, 60, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.White);

        var lines = new List<IReadOnlyList<TextRun>>
        {
            new[] { new TextRun("var", Red), new TextRun(" x = 1;", Black) },
        };

        using (var session = new CanvasDrawingSession(canvas))
        {
            CanvasLinePainter.Paint(session, lines, Font, 16, lineHeight: 20, horizontalOffset: 0, firstRowTop: 0);
        }

        Assert.That(HasNonWhitePixel(bitmap), Is.True, "painter should have rendered glyphs onto the surface");
    }

    [Test]
    public void MeasureTextWidth_CascadiaIsMonospace()
    {
        const string cascadia = "file://Assets/CascadiaCode-Regular.ttf";
        // In a monospaced font, narrow and wide glyphs share the same advance.
        double narrow = CanvasLinePainter.MeasureTextWidth("iiiiiiii", cascadia, 16);
        double wide = CanvasLinePainter.MeasureTextWidth("MMMMMMMM", cascadia, 16);

        Assert.That(narrow, Is.GreaterThan(0));
        Assert.That(narrow, Is.EqualTo(wide).Within(0.5), "Cascadia Code must render with uniform (monospace) advances");
    }

    [Test]
    public void MeasureTextWidth_OpenSansIsProportional()
    {
        double narrow = CanvasLinePainter.MeasureTextWidth("iiiiiiii", Font, 16);
        double wide = CanvasLinePainter.MeasureTextWidth("MMMMMMMM", Font, 16);

        Assert.That(wide, Is.GreaterThan(narrow + 1), "OpenSans is proportional: 'M' must be wider than 'i'");
    }

    [Test]
    public void Paint_WithNoLines_DoesNotThrow()
    {
        using var bitmap = new SKBitmap(50, 20, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(bitmap);
        using var session = new CanvasDrawingSession(canvas);

        Assert.DoesNotThrow(() =>
            CanvasLinePainter.Paint(session, new List<IReadOnlyList<TextRun>>(), Font, 16, 20, 0, 0));
    }

    static bool HasNonWhitePixel(SKBitmap bitmap)
    {
        for (int y = 0; y < bitmap.Height; y++)
            for (int x = 0; x < bitmap.Width; x++)
            {
                SKColor c = bitmap.GetPixel(x, y);
                if (c.Red != 255 || c.Green != 255 || c.Blue != 255)
                    return true;
            }
        return false;
    }
}
