using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Rendering;

namespace UnoEdit.Skia.Desktop.Controls;

/// <summary>Indicates whether a line has a fold marker and in what state.</summary>
public enum FoldMarkerKind { None, CanFold, CanExpand }

/// <summary>A single colored text segment for a line.</summary>
public readonly struct TextRun
{
    public TextRun(string text, Windows.UI.Color foreground)
    {
        Text = text;
        Foreground = foreground;
    }

    public string Text { get; }
    public Windows.UI.Color Foreground { get; }
}

public sealed class TextLineViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    private void Notify([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    // Default foreground color (#E2E8F0) — used only when no theme is provided (static builds/tests).
    private static readonly Windows.UI.Color DefaultForeground =
        Windows.UI.Color.FromArgb(255, 226, 232, 240);

    // Per-instance default foreground from the active theme.
    private readonly Windows.UI.Color _defaultForeground;

    /// <summary>Tab width in spaces. Must match <see cref="ICSharpCode.AvalonEdit.Rendering.TabColumnHelper.TabWidth"/>.</summary>
    public const int TabWidth = ICSharpCode.AvalonEdit.Rendering.TabColumnHelper.TabWidth;

    /// <summary>
    /// Compute the visual (display) column for a given logical column in a line's text,
    /// expanding tab characters to the next tab stop (multiple of <see cref="TabWidth"/>).
    /// Columns are 0-based here (unlike AvalonEdit's 1-based TextLocation).
    /// </summary>
    public static int LogicalToVisualColumn(string text, int logicalColumn)
        => ICSharpCode.AvalonEdit.Rendering.TabColumnHelper.LogicalToVisualColumn(text, logicalColumn);

    /// <summary>
    /// Compute the logical column for a given visual (pixel-column) position in a line.
    /// Returns the 0-based logical column index closest to the given visual column.
    /// </summary>
    public static int VisualToLogicalColumn(string text, int visualColumn)
        => ICSharpCode.AvalonEdit.Rendering.TabColumnHelper.VisualToLogicalColumn(text, visualColumn);

    public TextLineViewModel(
        int number,
        string text,
        double caretOpacity,
        double highlightOpacity,
        Thickness caretMargin,
        Thickness selectionMargin,
        double selectionWidth,
        double selectionOpacity,
        TextEditorTheme theme,
        FoldMarkerKind foldMarker = FoldMarkerKind.None,
        HighlightedLine? highlightedLine = null,
        IReadOnlyList<ReferenceSegment>? referenceSegments = null)
    {
        FoldMarker = foldMarker;
        Number = number.ToString();
        Text = text;
        _caretOpacity = caretOpacity;
        _highlightOpacity = highlightOpacity;
        _caretMargin = caretMargin;
        _selectionMargin = selectionMargin;
        _selectionWidth = selectionWidth;
        _selectionOpacity = selectionOpacity;
        LineHighlightBrush   = new SolidColorBrush(theme.LineHighlight);
        SelectionBrush       = new SolidColorBrush(theme.SelectionColor);
        CaretBrush           = new SolidColorBrush(theme.CaretColor);
        GutterForegroundBrush = new SolidColorBrush(theme.GutterForeground);
        _defaultForeground   = theme.DefaultForeground;
        ReferenceSegments    = referenceSegments ?? System.Array.Empty<ReferenceSegment>();
        Runs = BuildRuns(text, highlightedLine, theme.DefaultForeground);
    }

    // Copy constructor that reuses all text/run data and only updates transient visual state.
    private TextLineViewModel(
        TextLineViewModel source,
        double caretOpacity,
        double highlightOpacity,
        Thickness caretMargin,
        Thickness selectionMargin,
        double selectionWidth,
        double selectionOpacity)
    {
        FoldMarker            = source.FoldMarker;
        Number                = source.Number;
        Text                  = source.Text;
        _caretOpacity         = caretOpacity;
        _highlightOpacity     = highlightOpacity;
        _caretMargin          = caretMargin;
        _selectionMargin      = selectionMargin;
        _selectionWidth       = selectionWidth;
        _selectionOpacity     = selectionOpacity;
        LineHighlightBrush    = source.LineHighlightBrush;
        SelectionBrush        = source.SelectionBrush;
        CaretBrush            = source.CaretBrush;
        GutterForegroundBrush = source.GutterForegroundBrush;
        _defaultForeground    = source._defaultForeground;
        ReferenceSegments     = source.ReferenceSegments;
        Runs                  = source.Runs;  // reuse pre-computed syntax-highlighted runs
    }

    /// <summary>
    /// Create a copy of this view-model with updated caret and selection state only.
    /// Reuses pre-computed text runs so syntax highlighting is not recomputed.
    /// Returns <c>this</c> when all values are unchanged to avoid ObservableCollection Replace churn.
    /// </summary>
    internal TextLineViewModel WithCaretAndSelection(
        double caretOpacity, double highlightOpacity,
        Thickness caretMargin,
        Thickness selectionMargin, double selectionWidth, double selectionOpacity)
    {
        if (Math.Abs(CaretOpacity - caretOpacity) < 0.001
            && Math.Abs(HighlightOpacity - highlightOpacity) < 0.001
            && CaretMargin.Left == caretMargin.Left
            && Math.Abs(SelectionOpacity - selectionOpacity) < 0.001
            && SelectionMargin.Left == selectionMargin.Left
            && Math.Abs(SelectionWidth - selectionWidth) < 0.001)
        {
            return this;
        }
        return new(this, caretOpacity, highlightOpacity, caretMargin, selectionMargin, selectionWidth, selectionOpacity);
    }

    /// <summary>
    /// Mutate this view-model in-place from <paramref name="source"/>. Raises PropertyChanged
    /// only for properties whose values actually differ, so the XAML binding engine updates
    /// only the affected UI elements. The ObservableCollection item is never replaced, which
    /// avoids tearing down and recreating the DataTemplate visual tree (the root cause of flash).
    /// </summary>
    internal void UpdateFrom(TextLineViewModel source)
    {
        // Caret / selection — use property setters (they check for change).
        CaretOpacity     = source._caretOpacity;
        HighlightOpacity = source._highlightOpacity;
        CaretMargin      = source._caretMargin;
        SelectionMargin  = source._selectionMargin;
        SelectionWidth   = source._selectionWidth;
        SelectionOpacity = source._selectionOpacity;

        // Text / runs — only change when line content was re-highlighted.
        if (Text != source.Text)
        {
            Text = source.Text;
            Notify(nameof(Text));
        }
        if (!ReferenceEquals(Runs, source.Runs))
        {
            Runs = source.Runs;
            Notify(nameof(Runs));
        }
        if (!ReferenceEquals(ReferenceSegments, source.ReferenceSegments))
        {
            ReferenceSegments = source.ReferenceSegments;
            Notify(nameof(ReferenceSegments));
        }
        if (FoldMarker != source.FoldMarker)
        {
            FoldMarker = source.FoldMarker;
            Notify(nameof(FoldMarker));
            Notify(nameof(FoldMarkerGlyph));
            Notify(nameof(FoldMarkerAutomationName));
        }
        // Brushes and Number don't change within a theme / line-number.
    }

    public string Number { get; private set; }

    public FoldMarkerKind FoldMarker { get; private set; }

    public string FoldMarkerGlyph => FoldMarker switch
    {
        FoldMarkerKind.CanFold   => "▼",
        FoldMarkerKind.CanExpand => "▶",
        _                        => "",
    };

    public string FoldMarkerAutomationName => FoldMarker switch
    {
        FoldMarkerKind.CanFold   => $"Collapse line {Number}",
        FoldMarkerKind.CanExpand => $"Expand line {Number}",
        _                        => string.Empty,
    };

    public string Text { get; private set; }

    private double _caretOpacity;
    public double CaretOpacity { get => _caretOpacity; internal set { if (Math.Abs(_caretOpacity - value) > 0.001) { _caretOpacity = value; Notify(); } } }

    private double _highlightOpacity;
    public double HighlightOpacity { get => _highlightOpacity; internal set { if (Math.Abs(_highlightOpacity - value) > 0.001) { _highlightOpacity = value; Notify(); } } }

    private Thickness _caretMargin;
    public Thickness CaretMargin { get => _caretMargin; internal set { if (_caretMargin.Left != value.Left) { _caretMargin = value; Notify(); } } }

    private Thickness _selectionMargin;
    public Thickness SelectionMargin { get => _selectionMargin; internal set { if (_selectionMargin.Left != value.Left) { _selectionMargin = value; Notify(); } } }

    private double _selectionWidth;
    public double SelectionWidth { get => _selectionWidth; internal set { if (Math.Abs(_selectionWidth - value) > 0.001) { _selectionWidth = value; Notify(); } } }

    private double _selectionOpacity;
    public double SelectionOpacity { get => _selectionOpacity; internal set { if (Math.Abs(_selectionOpacity - value) > 0.001) { _selectionOpacity = value; Notify(); } } }

    public SolidColorBrush LineHighlightBrush   { get; private set; }
    public SolidColorBrush SelectionBrush       { get; private set; }
    public SolidColorBrush CaretBrush           { get; private set; }
    public SolidColorBrush GutterForegroundBrush { get; private set; }

    /// <summary>Reference segments that fall within this line (for underline rendering).</summary>
    public IReadOnlyList<ReferenceSegment> ReferenceSegments { get; private set; }

    /// <summary>Pre-computed color runs for syntax-highlighted rendering.</summary>
    public IReadOnlyList<TextRun> Runs { get; private set; }

    private static IReadOnlyList<TextRun> BuildRuns(string text, HighlightedLine? line, Windows.UI.Color defaultForeground)
    {
        // Expand tabs to spaces so the visual width matches the column math in TextView.
        string expanded = ExpandTabs(text);

        if (line == null || line.Sections.Count == 0 || expanded.Length == 0)
            return new[] { new TextRun(expanded, defaultForeground) };

        int lineStart = line.DocumentLine.Offset;
        // Map from logical offsets to per-expanded-character color, handling tab expansion.
        // First build a logical→visual offset map.
        int logicalLen = text.Length;
        int visualLen  = expanded.Length;
        var logToVis = new int[logicalLen + 1]; // logToVis[i] = visual start of logical char i
        {
            int vis = 0;
            for (int i = 0; i < logicalLen; i++)
            {
                logToVis[i] = vis;
                vis = text[i] == '\t' ? ((vis / TabWidth) + 1) * TabWidth : vis + 1;
            }
            logToVis[logicalLen] = vis;
        }

        var colors = new Windows.UI.Color?[visualLen];

        foreach (var section in line.Sections)
        {
            if (section.Color?.Foreground == null) continue;
            var mediaColor = section.Color.Foreground.GetColor();
            if (mediaColor == null) continue;

            var uiColor = Windows.UI.Color.FromArgb(
                mediaColor.Value.A,
                mediaColor.Value.R,
                mediaColor.Value.G,
                mediaColor.Value.B);

            int logStart = Math.Max(0, section.Offset - lineStart);
            int logEnd   = Math.Min(logicalLen, section.Offset + section.Length - lineStart);
            int visStart = logToVis[logStart];
            int visEnd   = logToVis[logEnd];
            for (int v = visStart; v < visEnd; v++)
                colors[v] = uiColor;
        }

        var runs = new List<TextRun>();
        int pos = 0;
        while (pos < visualLen)
        {
            var c = colors[pos] ?? defaultForeground;
            int end = pos + 1;
            while (end < visualLen && (colors[end] ?? defaultForeground) == c)
                end++;
            runs.Add(new TextRun(expanded.Substring(pos, end - pos), c));
            pos = end;
        }
        return runs;
    }

    /// <summary>Expand tab characters to the next multiple-of-<see cref="TabWidth"/> space boundary.</summary>
    internal static string ExpandTabs(string text)
    {
        if (!text.Contains('\t'))
            return text;
        var sb = new System.Text.StringBuilder(text.Length + 8);
        int col = 0;
        foreach (char ch in text)
        {
            if (ch == '\t')
            {
                int spaces = TabWidth - (col % TabWidth);
                sb.Append(' ', spaces);
                col += spaces;
            }
            else
            {
                sb.Append(ch);
                col++;
            }
        }
        return sb.ToString();
    }
}
