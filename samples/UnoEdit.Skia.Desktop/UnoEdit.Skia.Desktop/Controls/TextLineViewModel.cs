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
        if (line == null || line.Sections.Count == 0 || text.Length == 0)
            return new[] { new TextRun(text, DefaultForeground) };

        int lineStart = line.DocumentLine.Offset;
        int len = text.Length;

        // Per-character foreground color array (null = use default)
        var colors = new Windows.UI.Color?[len];

        // Apply highlighting sections. Inner (nested) sections follow outer ones in the
        // list, so later iterations overwrite earlier ones — innermost color wins.
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

            int start = Math.Max(0, section.Offset - lineStart);
            int end   = Math.Min(len, section.Offset + section.Length - lineStart);
            for (int i = start; i < end; i++)
                colors[i] = uiColor;
        }

        // Collapse adjacent same-color chars into runs
        var runs = new List<TextRun>();
        int pos = 0;
        while (pos < len)
        {
            var c = colors[pos] ?? DefaultForeground;
            int end = pos + 1;
            while (end < len && (colors[end] ?? DefaultForeground) == c)
                end++;
            runs.Add(new TextRun(text.Substring(pos, end - pos), c));
            pos = end;
        }
        return runs;
    }
}
