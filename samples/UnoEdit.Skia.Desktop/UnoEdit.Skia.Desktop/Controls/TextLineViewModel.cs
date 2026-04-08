using System.Collections.Generic;
using ICSharpCode.AvalonEdit.Highlighting;

namespace UnoEdit.Skia.Desktop.Controls;

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
    // Default foreground color (#E2E8F0)
    private static readonly Windows.UI.Color DefaultForeground =
        Windows.UI.Color.FromArgb(255, 226, 232, 240);

    /// <summary>Tab width in spaces. Must match the rendering constant in <see cref="TextView"/>.</summary>
    public const int TabWidth = 4;

    /// <summary>
    /// Compute the visual (display) column for a given logical column in a line's text,
    /// expanding tab characters to the next tab stop (multiple of <see cref="TabWidth"/>).
    /// Columns are 0-based here (unlike AvalonEdit's 1-based TextLocation).
    /// </summary>
    public static int LogicalToVisualColumn(string text, int logicalColumn)
    {
        int visual = 0;
        for (int i = 0; i < logicalColumn && i < text.Length; i++)
        {
            if (text[i] == '\t')
                visual = ((visual / TabWidth) + 1) * TabWidth;
            else
                visual++;
        }
        return visual;
    }

    /// <summary>
    /// Compute the logical column for a given visual (pixel-column) position in a line.
    /// Returns the 0-based logical column index closest to the given visual column.
    /// </summary>
    public static int VisualToLogicalColumn(string text, int visualColumn)
    {
        int visual = 0;
        for (int i = 0; i < text.Length; i++)
        {
            int nextVisual = text[i] == '\t'
                ? ((visual / TabWidth) + 1) * TabWidth
                : visual + 1;
            // Snap to nearest boundary
            if (visualColumn <= (visual + nextVisual) / 2)
                return i;
            visual = nextVisual;
        }
        return text.Length;
    }

    public TextLineViewModel(
        int number,
        string text,
        double caretOpacity,
        double highlightOpacity,
        Thickness caretMargin,
        Thickness selectionMargin,
        double selectionWidth,
        double selectionOpacity,
        HighlightedLine? highlightedLine = null)
    {
        Number = number.ToString();
        Text = text;
        CaretOpacity = caretOpacity;
        HighlightOpacity = highlightOpacity;
        CaretMargin = caretMargin;
        SelectionMargin = selectionMargin;
        SelectionWidth = selectionWidth;
        SelectionOpacity = selectionOpacity;
        Runs = BuildRuns(text, highlightedLine);
    }

    public string Number { get; }

    public string Text { get; }

    public double CaretOpacity { get; }

    public double HighlightOpacity { get; }

    public Thickness CaretMargin { get; }

    public Thickness SelectionMargin { get; }

    public double SelectionWidth { get; }

    public double SelectionOpacity { get; }

    /// <summary>Pre-computed color runs for syntax-highlighted rendering.</summary>
    public IReadOnlyList<TextRun> Runs { get; }

    private static IReadOnlyList<TextRun> BuildRuns(string text, HighlightedLine? line)
    {
        // Expand tabs to spaces so the visual width matches the column math in TextView.
        string expanded = ExpandTabs(text);

        if (line == null || line.Sections.Count == 0 || expanded.Length == 0)
            return new[] { new TextRun(expanded, DefaultForeground) };

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
            var c = colors[pos] ?? DefaultForeground;
            int end = pos + 1;
            while (end < visualLen && (colors[end] ?? DefaultForeground) == c)
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
