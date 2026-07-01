using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI.Xaml;
using UnoEdit.Skia.Desktop.Controls;

namespace ICSharpCode.AvalonEdit.Rendering;

// Win2D/Skia rendering surfaces, AvalonEdit-style. Layered so selection drag is cheap:
//   * content surfaces — glyphs (text), line numbers, fold markers — repaint only when their
//     content/scroll/theme actually changes (the expensive glyph shaping);
//   * a single overlay surface — current-line highlight, selection, caret, preedit underline —
//     repaints on every refresh (rectangles only, no shaping), so dragging a selection re-renders
//     just this layer instead of re-shaping every visible glyph per pointer-move.
// Every surface positions rows by the single shared LineHeight, so nothing can drift.
public sealed partial class TextView
{
    private CanvasControl? _highlightSurface;
    private CanvasControl? _canvasTextSurface;
    private CanvasControl? _lineNumberSurface;
    private CanvasControl? _breakpointSurface;
    private CanvasControl? _foldMarginSurface;
    private CanvasControl? _overlaySurface;

    private const double CaretWidth = 2d;
    private const double OverlayHeight = 16d;
    private const double LineNumberRightPadding = 2d;
    private static readonly Windows.UI.Color Transparent = Windows.UI.Color.FromArgb(0, 0, 0, 0);

    private void InitializeCanvasTextSurface()
    {
        if (HighlightHost is not null)
        {
            _highlightSurface = CreateSurface();
            _highlightSurface.Draw += OnHighlightSurfaceDraw;
            HighlightHost.Children.Add(_highlightSurface);
        }

        if (TextContentGrid is not null)
        {
            _canvasTextSurface = CreateSurface();
            _canvasTextSurface.Draw += OnCanvasTextSurfaceDraw;
            int insertAt = System.Math.Min(1, TextContentGrid.Children.Count);
            TextContentGrid.Children.Insert(insertAt, _canvasTextSurface);
        }

        if (LineNumberHost is not null)
        {
            _lineNumberSurface = CreateSurface();
            _lineNumberSurface.Draw += OnLineNumberSurfaceDraw;
            LineNumberHost.Children.Add(_lineNumberSurface);
        }

        if (BreakpointHost is not null)
        {
            _breakpointSurface = CreateSurface();
            _breakpointSurface.Draw += OnBreakpointSurfaceDraw;
            BreakpointHost.Children.Add(_breakpointSurface);
            if (_breakpointLines.Count > 0)
                _breakpointSurface.Invalidate();
        }

        if (FoldMarginHost is not null)
        {
            _foldMarginSurface = CreateSurface();
            _foldMarginSurface.Draw += OnFoldMarginSurfaceDraw;
            FoldMarginHost.Children.Add(_foldMarginSurface);
        }

        if (OverlayHost is not null)
        {
            _overlaySurface = CreateSurface();
            _overlaySurface.Draw += OnOverlaySurfaceDraw;
            OverlayHost.Children.Add(_overlaySurface);
        }
    }

    private static CanvasControl CreateSurface() => new()
    {
        HorizontalAlignment = HorizontalAlignment.Stretch,
        VerticalAlignment = VerticalAlignment.Top,
        IsHitTestVisible = false,
    };

    private double CanvasHeight => _lines.Count * LineHeight;

    // Repaints the expensive content surfaces (glyphs / line numbers / fold markers).
    private void InvalidateCanvasContent()
    {
        double height = CanvasHeight;

        if (_canvasTextSurface is not null)
        {
            _canvasTextSurface.Height = System.Math.Max(0, height);
            _canvasTextSurface.Width = System.Math.Max(0, EditorGrid?.ActualWidth ?? ActualWidth);
            _canvasTextSurface.Invalidate();
        }

        if (_lineNumberSurface is not null && LineNumberHost?.Visibility == Visibility.Visible)
        {
            _lineNumberSurface.Height = System.Math.Max(0, height);
            _lineNumberSurface.Width = System.Math.Max(0, LineNumberHost.ActualWidth);
            _lineNumberSurface.Invalidate();
        }

        if (_breakpointSurface is not null)
        {
            _breakpointSurface.Height = System.Math.Max(0, height);
            _breakpointSurface.Width = System.Math.Max(0, BreakpointHost?.ActualWidth ?? 18);
            _breakpointSurface.Invalidate();
        }

        if (_foldMarginSurface is not null)
        {
            _foldMarginSurface.Height = System.Math.Max(0, height);
            _foldMarginSurface.Width = System.Math.Max(0, FoldMarginHost?.ActualWidth ?? 16);
            _foldMarginSurface.Invalidate();
        }
    }

    // Repaints the cheap rectangle-only layers that follow the caret/selection: the background
    // line-highlight (behind text) and the foreground overlay (selection / caret / preedit).
    private void InvalidateCanvasOverlay()
    {
        double width = System.Math.Max(0, EditorGrid?.ActualWidth ?? ActualWidth);
        double height = System.Math.Max(0, CanvasHeight);

        if (_highlightSurface is not null)
        {
            _highlightSurface.Height = height;
            _highlightSurface.Width = width;
            _highlightSurface.Invalidate();
        }

        if (_overlaySurface is not null)
        {
            _overlaySurface.Height = height;
            _overlaySurface.Width = width;
            _overlaySurface.Invalidate();
        }
    }

    // Background current-line highlight (behind the glyph/line-number/fold surfaces), full width.
    private void OnHighlightSurfaceDraw(CanvasControl sender, CanvasDrawEventArgs args)
    {
        args.DrawingSession.Clear(Transparent);
        if (_lines.Count == 0)
            return;

        var session = args.DrawingSession;
        double rowHeight = LineHeight;
        double width = _highlightSurface?.Width ?? ActualWidth;
        double y = 0;
        foreach (TextLineViewModel vm in _lines)
        {
            if (vm.HighlightOpacity > 0.001)
                session.FillRectangle(0, (float)y, (float)width, (float)rowHeight, WithOpacity(vm.LineHighlightBrush, vm.HighlightOpacity));
            y += rowHeight;
        }
    }

    private void OnCanvasTextSurfaceDraw(CanvasControl sender, CanvasDrawEventArgs args)
    {
        args.DrawingSession.Clear(Transparent);
        if (_lines.Count == 0)
            return;

        var session = args.DrawingSession;
        string? font = EditorFontFamily?.Source;
        double fontSize = EditorFontSize;
        double rowHeight = LineHeight;
        double y = 0;
        foreach (TextLineViewModel vm in _lines)
        {
            CanvasLinePainter.PaintLineAt(session, vm.Runs, font, fontSize, rowHeight, horizontalOffset: 0, top: y);
            y += rowHeight;
        }
    }

    private void OnLineNumberSurfaceDraw(CanvasControl sender, CanvasDrawEventArgs args)
    {
        args.DrawingSession.Clear(Transparent);
        if (_lines.Count == 0)
            return;

        var session = args.DrawingSession;
        double rowHeight = LineHeight;
        double width = _lineNumberSurface?.Width ?? 0;
        if (width <= 0)
            return;

        using var format = new CanvasTextFormat
        {
            FontFamily = EditorFontFamily?.Source,
            FontSize = (float)EditorFontSize,
            HorizontalAlignment = CanvasHorizontalAlignment.Right,
            VerticalAlignment = CanvasVerticalAlignment.Center,
        };

        double y = 0;
        foreach (TextLineViewModel vm in _lines)
        {
            if (!string.IsNullOrEmpty(vm.Number))
            {
                var bounds = new Windows.Foundation.Rect(0, y, System.Math.Max(0, width - LineNumberRightPadding), rowHeight);
                session.DrawText(vm.Number, bounds, vm.GutterForegroundBrush.Color, format);
            }
            y += rowHeight;
        }
    }

    private static readonly Windows.UI.Color BreakpointRed = Windows.UI.Color.FromArgb(255, 228, 20, 0);

    private void OnBreakpointSurfaceDraw(CanvasControl sender, CanvasDrawEventArgs args)
    {
        args.DrawingSession.Clear(Transparent);
        if (_lines.Count == 0)
            return;

        var session = args.DrawingSession;
        double rowHeight = LineHeight;
        double width = _breakpointSurface?.Width ?? 18;
        if (width <= 0) return;

        float cx = (float)(width / 2);
        float radius = 5f;

        double y = 0;
        foreach (TextLineViewModel vm in _lines)
        {
            if (int.TryParse(vm.Number, out int docLine) && _breakpointLines.Contains(docLine))
            {
                float cy = (float)(y + rowHeight / 2);
                session.FillCircle(cx, cy, radius, BreakpointRed);
            }
            y += rowHeight;
        }
    }

    private void OnFoldMarginSurfaceDraw(CanvasControl sender, CanvasDrawEventArgs args)
    {
        args.DrawingSession.Clear(Transparent);
        if (_lines.Count == 0)
            return;

        var session = args.DrawingSession;
        double rowHeight = LineHeight;
        double y = 0;
        foreach (TextLineViewModel vm in _lines)
        {
            DrawFoldMarker(session, vm.FoldMarker, (float)y, (float)rowHeight, vm.GutterForegroundBrush.Color);
            y += rowHeight;
        }
    }

    // Cheap overlay: current-line highlight (full width), selection, caret and preedit underline.
    // Selection/caret X are in text-content space, so they are shifted right by the gutter width.
    private void OnOverlaySurfaceDraw(CanvasControl sender, CanvasDrawEventArgs args)
    {
        args.DrawingSession.Clear(Transparent);
        if (_lines.Count == 0)
            return;

        var session = args.DrawingSession;
        double rowHeight = LineHeight;
        // The overlay canvas spans all columns from the editor's left edge, while the glyphs are
        // drawn in the text content column. Shift by the live width of every preceding column so
        // selection/caret/preedit line up with the text regardless of which margins are shown.
        double gutter = CurrentGutterWidth;

        double y = 0;
        foreach (TextLineViewModel vm in _lines)
        {
            double boxTop = y + (rowHeight - OverlayHeight) / 2;

            // Line highlight is drawn on the background layer (behind text); the overlay only paints
            // selection / caret / preedit, which sit above the glyphs.
            if (vm.SelectionOpacity > 0.001 && vm.SelectionWidth > 0)
            {
                float sx = (float)(gutter + vm.SelectionMargin.Left);
                float sw = (float)vm.SelectionWidth;
                float radius = (float)vm.SelectionCornerRadius;
                session.FillRoundedRectangle(sx, (float)boxTop, sw, (float)OverlayHeight, radius, radius, WithOpacity(vm.SelectionBrush, vm.SelectionOpacity));
                if (vm.SelectionBorderBrush is not null)
                    session.DrawRoundedRectangle(sx, (float)boxTop, sw, (float)OverlayHeight, radius, radius, WithOpacity(vm.SelectionBorderBrush, vm.SelectionOpacity), 1f);
            }

            if (vm.PreeditUnderlineOpacity > 0.001 && vm.PreeditUnderlineWidth > 0)
            {
                float ux = (float)(gutter + vm.PreeditUnderlineMargin.Left);
                session.FillRectangle(ux, (float)(y + rowHeight - 1.5), (float)vm.PreeditUnderlineWidth, 1.5f, WithOpacity(vm.PreeditUnderlineBrush, vm.PreeditUnderlineOpacity));
            }

            if (vm.CaretOpacity > 0.001)
            {
                float cx = (float)(gutter + vm.CaretMargin.Left);
                session.FillRectangle(cx, (float)boxTop, (float)CaretWidth, (float)OverlayHeight, WithOpacity(vm.CaretBrush, vm.CaretOpacity));
            }

            y += rowHeight;
        }
    }

    // Fold margin marker drawn directly, scaled to LineHeight (AvalonEdit FoldingMargin.OnRender analog).
    private static void DrawFoldMarker(CanvasDrawingSession session, FoldMarkerKind kind, float top, float rowHeight, Windows.UI.Color color)
    {
        if (kind == FoldMarkerKind.None)
            return;

        const float cx = 8f;
        const float boxHalf = 5f;
        float cy = top + rowHeight / 2f;
        float boxTop = cy - boxHalf;
        float boxBottom = cy + boxHalf;
        float boxLeft = cx - boxHalf;

        switch (kind)
        {
            case FoldMarkerKind.CanFold:
                session.DrawRectangle(boxLeft, boxTop, 10f, 10f, color, 1f);
                session.DrawLine(cx - 3f, cy, cx + 3f, cy, color, 1f);
                session.DrawLine(cx, boxBottom, cx, top + rowHeight, color, 1f);
                break;
            case FoldMarkerKind.CanExpand:
                session.DrawRectangle(boxLeft, boxTop, 10f, 10f, color, 1f);
                session.DrawLine(cx - 3f, cy, cx + 3f, cy, color, 1f);
                session.DrawLine(cx, cy - 3f, cx, cy + 3f, color, 1f);
                break;
            case FoldMarkerKind.InsideFold:
                session.DrawLine(cx, top, cx, top + rowHeight, color, 1f);
                break;
            case FoldMarkerKind.FoldEnd:
                session.DrawLine(cx, top, cx, cy, color, 1f);
                session.DrawLine(cx, cy, cx + 6f, cy, color, 1f);
                break;
        }
    }

    private static Windows.UI.Color WithOpacity(Microsoft.UI.Xaml.Media.SolidColorBrush brush, double opacity)
    {
        Windows.UI.Color c = brush.Color;
        double a = (c.A / 255.0) * System.Math.Clamp(opacity, 0, 1);
        return Windows.UI.Color.FromArgb((byte)System.Math.Round(a * 255), c.R, c.G, c.B);
    }
}
