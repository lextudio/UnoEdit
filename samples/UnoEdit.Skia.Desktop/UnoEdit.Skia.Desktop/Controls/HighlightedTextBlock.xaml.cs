using Microsoft.UI.Xaml.Documents;

namespace UnoEdit.Skia.Desktop.Controls;

public sealed partial class HighlightedTextBlock : UserControl
{
    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(
            nameof(ViewModel),
            typeof(TextLineViewModel),
            typeof(HighlightedTextBlock),
            new PropertyMetadata(null, OnViewModelChanged));

    public HighlightedTextBlock()
    {
        this.InitializeComponent();
    }

    public TextLineViewModel? ViewModel
    {
        get => (TextLineViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    private static void OnViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((HighlightedTextBlock)d).RefreshInlines((TextLineViewModel?)e.NewValue);
    }

    private void RefreshInlines(TextLineViewModel? vm)
    {
        PART_Text.Inlines.Clear();

        if (vm is null) return;

        var runs = vm.Runs;
        if (runs is null || runs.Count == 0) return;

        var refSegments = vm.ReferenceSegments;
        bool hasRefs    = refSegments is { Count: > 0 };

        // Track cumulative visual-column position to detect reference overlaps.
        int visualPos = 0;

        foreach (var textRun in runs)
        {
            int runStart = visualPos;
            int runEnd   = visualPos + textRun.Text.Length;
            visualPos    = runEnd;

            bool isRef = hasRefs && IsInReference(refSegments, runStart, runEnd, vm.Text);

            var inline = new Run
            {
                Text       = textRun.Text,
                Foreground = new SolidColorBrush(textRun.Foreground),
            };

            if (isRef)
            {
                inline.TextDecorations = Windows.UI.Text.TextDecorations.Underline;
            }

            PART_Text.Inlines.Add(inline);
        }
    }

    /// <summary>
    /// Returns true if any character in [<paramref name="visualStart"/>, <paramref name="visualEnd"/>)
    /// falls inside one of the reference segments (which use logical offsets from the line start).
    /// We compare using visual positions of the expanded text vs. logical offsets mapped to visual.
    /// </summary>
    private static bool IsInReference(
        System.Collections.Generic.IReadOnlyList<ICSharpCode.AvalonEdit.Rendering.ReferenceSegment> segments,
        int visualStart,
        int visualEnd,
        string logicalText)
    {
        // Build logical→visual map once per call if needed
        // For simplicity use a lightweight per-char scan since refSegments are rare
        foreach (var seg in segments)
        {
            // seg offsets are absolute document offsets; the vm.Text corresponds to the line.
            // We don't have the line start here — segments stored in vm are already relative
            // (relative start/end stored in ReferenceSegment when passed to vm).
            // So use seg.StartOffset / seg.EndOffset as visual-column boundaries directly
            // (they are pre-converted before constructing vm).
            if (seg.StartOffset < visualEnd && seg.EndOffset > visualStart)
                return true;
        }
        return false;
    }
}
