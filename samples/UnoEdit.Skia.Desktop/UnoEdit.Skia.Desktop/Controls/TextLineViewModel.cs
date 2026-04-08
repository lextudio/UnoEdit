using System.Collections.Generic;
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

public sealed class TextLineViewModel
{
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
        CaretOpacity = caretOpacity;
        HighlightOpacity = highlightOpacity;
        CaretMargin = caretMargin;
        SelectionMargin = selectionMargin;
        SelectionWidth = selectionWidth;
        SelectionOpacity = selectionOpacity;
        LineHighlightBrush   = new SolidColorBrush(theme.LineHighlight);
        SelectionBrush       = new SolidColorBrush(theme.SelectionColor);
        CaretBrush           = new SolidColorBrush(theme.CaretColor);
        GutterForegroundBrush = new SolidColorBrush(theme.GutterForeground);
        _defaultForeground   = theme.DefaultForeground;
        ReferenceSegments    = referenceSegments ?? System.Array.Empty<ReferenceSegment>();
        Runs = BuildRuns(text, highlightedLine, theme.DefaultForeground);
    }

    public string Number { get; }

    public FoldMarkerKind FoldMarker { get; }

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

    public string Text { get; }

    public double CaretOpacity { get; }

    public double HighlightOpacity { get; }

    public Thickness CaretMargin { get; }

    public Thickness SelectionMargin { get; }

    public double SelectionWidth { get; }

    public double SelectionOpacity { get; }

    public SolidColorBrush LineHighlightBrush   { get; }
    public SolidColorBrush SelectionBrush       { get; }
    public SolidColorBrush CaretBrush           { get; }
    public SolidColorBrush GutterForegroundBrush { get; }

    /// <summary>Reference segments that fall within this line (for underline rendering).</summary>
    public IReadOnlyList<ReferenceSegment> ReferenceSegments { get; }

    /// <summary>Pre-computed color runs for syntax-highlighted rendering.</summary>
    public IReadOnlyList<TextRun> Runs { get; }

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
    private static string ExpandTabs(string text)
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
