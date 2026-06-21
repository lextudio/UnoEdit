using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
using UnoEdit.Skia.Desktop.Controls;

namespace ICSharpCode.AvalonEdit.Rendering
{
    /// <summary>
    /// Paints syntax-colored text lines onto a Win2D <see cref="CanvasDrawingSession"/> using real
    /// glyph advances (via <see cref="CanvasTextLayout"/>) — the redesign's replacement for the
    /// TextBlock-per-line surface. Pure logic with no XAML dependency so it can be exercised
    /// offscreen in tests. Run X positions come from a single per-line layout so they stay
    /// consistent with what is actually rendered (no <c>fontSize * 0.6</c> approximation).
    /// </summary>
    internal static class CanvasLinePainter
    {
        // Small cache so repeated prefix measurements (e.g. the caret-column binary search) don't
        // rebuild a layout every call. Keyed by (text, font, size); bounded to avoid unbounded growth.
        static readonly Dictionary<(string, string?, double), double> WidthCache = new();
        const int WidthCacheLimit = 4096;

        /// <summary>
        /// Measures the rendered width (in DIPs) of <paramref name="text"/> using real glyph advances
        /// via <see cref="CanvasTextLayout"/> — the single source of truth that replaces the
        /// TextBlock-probe / <c>fontSize * 0.6</c> approximations for caret/selection/hit-testing.
        /// </summary>
        public static double MeasureTextWidth(string? text, string? fontFamilySource, double fontSize)
        {
            if (string.IsNullOrEmpty(text))
                return 0d;

            var key = (text, fontFamilySource, fontSize);
            if (WidthCache.TryGetValue(key, out double cached))
                return cached;

            double width;
            try
            {
                using var format = new CanvasTextFormat { FontFamily = fontFamilySource, FontSize = (float)fontSize };
                using var layout = new CanvasTextLayout(CanvasDevice.GetSharedDevice(), text, format, float.PositiveInfinity, float.PositiveInfinity);
                width = layout.LayoutBounds.Width;
            }
            catch
            {
                width = text.Length * fontSize * 0.6;
            }

            if (WidthCache.Count >= WidthCacheLimit)
                WidthCache.Clear();
            WidthCache[key] = width;
            return width;
        }

        /// <summary>
        /// Returns the font-derived line height (ascent + descent + leading) for the given font,
        /// replacing the hardcoded 22px row height.
        /// </summary>
        public static double GetLineHeight(string? fontFamilySource, double fontSize)
        {
            try
            {
                using var format = new CanvasTextFormat { FontFamily = fontFamilySource, FontSize = (float)fontSize };
                // "Mg" spans a typical ascender + descender so the metrics reflect the full line box.
                using var layout = new CanvasTextLayout(CanvasDevice.GetSharedDevice(), "Mg", format, float.PositiveInfinity, float.PositiveInfinity);
                double height = layout.LayoutBounds.Height;
                return height > 0 ? height : fontSize * 1.4;
            }
            catch
            {
                return fontSize * 1.4;
            }
        }

        /// <summary>
        /// Draws the given lines top-down starting at <paramref name="firstRowTop"/>, advancing by
        /// <paramref name="lineHeight"/> per line and shifting left by <paramref name="horizontalOffset"/>
        /// to honor horizontal scrolling.
        /// </summary>
        public static void Paint(
            CanvasDrawingSession session,
            IReadOnlyList<IReadOnlyList<TextRun>> lines,
            string? fontFamilySource,
            double fontSize,
            double lineHeight,
            double horizontalOffset,
            double firstRowTop)
        {
            ArgumentNullException.ThrowIfNull(session);
            if (lines is null)
                return;

            double y = firstRowTop;
            foreach (var runs in lines)
            {
                if (runs is { Count: > 0 })
                    PaintLine(session, runs, fontFamilySource, fontSize, lineHeight, horizontalOffset, y);
                y += lineHeight;
            }
        }

        /// <summary>
        /// Paints a single line's runs centered within a row of <paramref name="rowHeight"/> at the
        /// given <paramref name="top"/>. Used by the TextView canvas surface, which advances by each
        /// row's individual height (rows are not uniform).
        /// </summary>
        public static void PaintLineAt(
            CanvasDrawingSession session,
            IReadOnlyList<TextRun> runs,
            string? fontFamilySource,
            double fontSize,
            double rowHeight,
            double horizontalOffset,
            double top)
        {
            ArgumentNullException.ThrowIfNull(session);
            if (runs is { Count: > 0 })
                PaintLine(session, runs, fontFamilySource, fontSize, rowHeight, horizontalOffset, top);
        }

        static void PaintLine(
            CanvasDrawingSession session,
            IReadOnlyList<TextRun> runs,
            string? fontFamilySource,
            double fontSize,
            double lineHeight,
            double horizontalOffset,
            double y)
        {
            // Center the glyph box within the (taller) row, matching the TextBlock path's
            // VerticalAlignment=Center so text aligns with the caret/selection overlays.
            double glyphHeight = GetLineHeight(fontFamilySource, fontSize);
            float top = (float)(y + (lineHeight - glyphHeight) / 2);

            using var format = new CanvasTextFormat { FontFamily = fontFamilySource, FontSize = (float)fontSize };

            // Position each run by accumulating cached run-width measurements rather than building a
            // CanvasTextLayout per line on every Draw. Selection drag triggers a full redraw per
            // pointer-move; re-shaping every line each frame made the selection visibly lag the
            // mouse. MeasureTextWidth is cached and is the same measurement caret/selection use, so
            // positions stay consistent.
            double x = 0;
            foreach (var run in runs)
            {
                if (run.Text.Length == 0)
                    continue;

                float drawX = (float)(x - horizontalOffset);
                double w = MeasureTextWidth(run.Text, fontFamilySource, fontSize);

                if (run.Background.HasValue)
                    session.FillRectangle(drawX, top, (float)w, (float)glyphHeight, run.Background.Value);

                session.DrawText(run.Text, drawX, top, run.Foreground, format);
                x += w;
            }
        }
    }
}
