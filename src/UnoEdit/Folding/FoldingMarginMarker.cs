// UnoEdit port of ICSharpCode.AvalonEdit.Folding.FoldingMarginMarker.
// WPF DrawingContext / PixelSnapHelpers rendering replaced with Uno XAML shapes.
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using UnoEdit.Skia.Desktop.Controls;
using XamlVisibility = Microsoft.UI.Xaml.Visibility;

namespace ICSharpCode.AvalonEdit.Folding;

/// <summary>
/// Renders an AvalonEdit-style fold-margin marker: a box with −/+ and connecting lines.
/// Mirrors the WPF FoldingMarginMarker but uses Canvas + Shapes instead of DrawingContext.
/// </summary>
public sealed class FoldingMarginMarker : UserControl
{
    // Dependency properties -----------------------------------------------

    public static readonly DependencyProperty FoldMarkerKindProperty =
        DependencyProperty.Register(nameof(FoldMarkerKind), typeof(FoldMarkerKind),
            typeof(FoldingMarginMarker),
            new PropertyMetadata(FoldMarkerKind.None, OnFoldMarkerKindChanged));

    public static readonly DependencyProperty MarkerBrushProperty =
        DependencyProperty.Register(nameof(MarkerBrush), typeof(Brush),
            typeof(FoldingMarginMarker),
            new PropertyMetadata(null, OnMarkerBrushChanged));

    public FoldMarkerKind FoldMarkerKind
    {
        get => (FoldMarkerKind)GetValue(FoldMarkerKindProperty);
        set => SetValue(FoldMarkerKindProperty, value);
    }

    public Brush MarkerBrush
    {
        get => (Brush)GetValue(MarkerBrushProperty);
        set => SetValue(MarkerBrushProperty, value);
    }

    // Shape elements -------------------------------------------------------

    private readonly Line      _vertLine;
    private readonly Rectangle _box;
    private readonly Line      _minusLine;
    private readonly Line      _plusLine;
    private readonly Line      _endLine;

    // Constructor ----------------------------------------------------------

    public FoldingMarginMarker()
    {
        var canvas = new Canvas { Width = 16, Height = 22 };

        _vertLine  = new Line  { StrokeThickness = 1, Visibility = XamlVisibility.Collapsed };
        _box = new Rectangle { Width = 10, Height = 10,
                               StrokeThickness = 1, Visibility = XamlVisibility.Collapsed };
        Canvas.SetLeft(_box, 3);
        Canvas.SetTop(_box, 6);
        _minusLine = new Line  { X1 = 5,  Y1 = 11, X2 = 11, Y2 = 11,
                                 StrokeThickness = 1, Visibility = XamlVisibility.Collapsed };
        _plusLine  = new Line  { X1 = 8,  Y1 = 8,  X2 = 8,  Y2 = 14,
                                 StrokeThickness = 1, Visibility = XamlVisibility.Collapsed };
        _endLine   = new Line  { X1 = 8,  Y1 = 11, X2 = 14, Y2 = 11,
                                 StrokeThickness = 1, Visibility = XamlVisibility.Collapsed };

        canvas.Children.Add(_vertLine);
        canvas.Children.Add(_box);
        canvas.Children.Add(_minusLine);
        canvas.Children.Add(_plusLine);
        canvas.Children.Add(_endLine);

        Content = canvas;
    }

    // Property-changed callbacks -------------------------------------------

    private static void OnFoldMarkerKindChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((FoldingMarginMarker)d).UpdateVisuals();

    private static void OnMarkerBrushChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((FoldingMarginMarker)d).UpdateBrushes();

    private void UpdateBrushes()
    {
        var brush = MarkerBrush;
        _vertLine.Stroke  = brush;
        _box.Stroke       = brush;
        _minusLine.Stroke = brush;
        _plusLine.Stroke  = brush;
        _endLine.Stroke   = brush;
    }

    private void UpdateVisuals()
    {
        switch (FoldMarkerKind)
        {
            case FoldMarkerKind.CanFold:
                // Box with minus; vertical line from box-bottom to cell-bottom
                Show(_box, _minusLine);
                Hide(_plusLine, _endLine);
                ShowVertLine(y1: 16, y2: 22);
                break;

            case FoldMarkerKind.CanExpand:
                // Box with plus; no connecting lines (region is collapsed)
                Show(_box, _minusLine, _plusLine);
                Hide(_vertLine, _endLine);
                break;

            case FoldMarkerKind.InsideFold:
                // Vertical line through full row height
                ShowVertLine(y1: 0, y2: 22);
                Hide(_box, _minusLine, _plusLine, _endLine);
                break;

            case FoldMarkerKind.FoldEnd:
                // Vertical line from top to center + horizontal arm pointing right
                ShowVertLine(y1: 0, y2: 11);
                Show(_endLine);
                Hide(_box, _minusLine, _plusLine);
                break;

            default: // None
                Hide(_vertLine, _box, _minusLine, _plusLine, _endLine);
                break;
        }
    }

    // Helpers --------------------------------------------------------------

    private void ShowVertLine(double y1, double y2)
    {
        _vertLine.X1 = 8; _vertLine.Y1 = y1;
        _vertLine.X2 = 8; _vertLine.Y2 = y2;
        _vertLine.Visibility = XamlVisibility.Visible;
    }

    private static void Show(params UIElement[] elements)
    {
        foreach (var e in elements) e.Visibility = XamlVisibility.Visible;
    }

    private static void Hide(params UIElement[] elements)
    {
        foreach (var e in elements) e.Visibility = XamlVisibility.Collapsed;
    }
}
