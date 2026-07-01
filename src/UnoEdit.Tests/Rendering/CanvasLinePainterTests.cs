using System.Collections.Generic;
using System.Linq;
using ICSharpCode.AvalonEdit.Rendering;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
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

    // ── Text/selection/hit-test X-alignment invariants ──────────────────────────
    // Regression guard for the Output-pad selection offset: the glyph painter positions
    // runs by SUMMING per-run measurements, the selection/caret compute X from a WHOLE-prefix
    // measurement, and hit-testing reads the shim's CanvasTextLayout caret positions. If any of
    // these three decompositions disagree, the selection box drifts away from the rendered text.

    const string SampleLine = "ERROR: No startup project to run.";

    [Test]
    public void MeasureTextWidth_IsAdditiveAcrossRunSplits_Proportional()
    {
        double whole = CanvasLinePainter.MeasureTextWidth(SampleLine, Font, 13);

        // An arbitrary run decomposition (as syntax highlighting would produce).
        string[] runs = { "ERROR", ": ", "No ", "startup ", "project ", "to ", "run." };
        Assert.That(string.Concat(runs), Is.EqualTo(SampleLine));
        double summed = runs.Sum(r => CanvasLinePainter.MeasureTextWidth(r, Font, 13));

        Assert.That(summed, Is.EqualTo(whole).Within(0.5),
            "per-run summed width (text draw) must equal whole-string width (selection): the two must not drift");
    }

    [Test]
    public void MeasureTextWidth_IsAdditiveAcrossRunSplits_Monospace()
    {
        const string cascadia = "file://Assets/CascadiaCode-Regular.ttf";
        double whole = CanvasLinePainter.MeasureTextWidth(SampleLine, cascadia, 13);
        string[] runs = { "ERROR:", " No startup ", "project to run." };
        double summed = runs.Sum(r => CanvasLinePainter.MeasureTextWidth(r, cascadia, 13));

        Assert.That(summed, Is.EqualTo(whole).Within(0.5));
    }

    [Test]
    public void MeasureTextWidth_PrefixesAreMonotonicAndReachFullWidth()
    {
        double full = CanvasLinePainter.MeasureTextWidth(SampleLine, Font, 13);
        double prev = 0;
        for (int i = 1; i <= SampleLine.Length; i++)
        {
            double w = CanvasLinePainter.MeasureTextWidth(SampleLine[..i], Font, 13);
            Assert.That(w, Is.GreaterThanOrEqualTo(prev - 0.001), $"prefix width must not shrink at index {i}");
            prev = w;
        }
        Assert.That(prev, Is.EqualTo(full).Within(0.5), "the full-length prefix must equal the whole-string width");
    }

    [Test]
    public void CanvasTextLayout_CaretPositions_MatchPrefixMeasurement()
    {
        // Hit-testing maps a click X back to a column via CanvasTextLayout.GetCaretPosition; the
        // rendered caret/selection X comes from MeasureTextWidth. They must agree so clicking and
        // the caret/selection land on the same glyph boundary.
        using var format = new CanvasTextFormat { FontFamily = Font, FontSize = 13 };
        using var layout = new CanvasTextLayout(
            CanvasDevice.GetSharedDevice(), SampleLine, format,
            float.PositiveInfinity, float.PositiveInfinity);

        for (int i = 0; i <= SampleLine.Length; i++)
        {
            double caretX = layout.GetCaretPosition(i, trailingSideOfCharacter: false).X;
            double prefixWidth = CanvasLinePainter.MeasureTextWidth(SampleLine[..i], Font, 13);
            Assert.That(caretX, Is.EqualTo(prefixWidth).Within(0.5),
                $"caret X at column {i} must match the measured prefix width (hit-test vs render alignment)");
        }
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
