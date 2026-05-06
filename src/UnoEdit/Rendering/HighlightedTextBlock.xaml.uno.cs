using Microsoft.UI.Xaml.Documents;

namespace UnoEdit.Skia.Desktop.Controls;

public sealed partial class HighlightedTextBlock : UserControl
{
    public static readonly DependencyProperty LineViewModelProperty =
        DependencyProperty.Register(
            nameof(LineViewModel),
            typeof(TextLineViewModel),
            typeof(HighlightedTextBlock),
            new PropertyMetadata(null, OnLineViewModelChanged));

    // Last rendered Runs/refs — used to skip unnecessary Inlines rebuilds when only
    // caret/selection state changed (text content is the same object reference).
    private System.Collections.Generic.IReadOnlyList<TextRun>? _lastRuns;
    private System.Collections.Generic.IReadOnlyList<ICSharpCode.AvalonEdit.Rendering.ReferenceSegment>? _lastRefSegs;

    public HighlightedTextBlock()
    {
        this.InitializeComponent();
        PART_Text.FontFamily = EditorTextMetrics.CreateFontFamily();
        PART_Text.FontSize = EditorTextMetrics.FontSize;
    }

    public TextLineViewModel? LineViewModel
    {
        get => (TextLineViewModel?)GetValue(LineViewModelProperty);
        set => SetValue(LineViewModelProperty, value);
    }

    private static void OnLineViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var block = (HighlightedTextBlock)d;
        // Unsubscribe from old VM's INPC.
        if (e.OldValue is TextLineViewModel oldVm)
            oldVm.PropertyChanged -= block.OnVmPropertyChanged;
        // Subscribe to new VM's INPC (for in-place UpdateFrom mutations).
        if (e.NewValue is TextLineViewModel newVm)
            newVm.PropertyChanged += block.OnVmPropertyChanged;
        block.RefreshInlines((TextLineViewModel?)e.NewValue);
    }

    private void OnVmPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(TextLineViewModel.Runs)
            or nameof(TextLineViewModel.ReferenceSegments))
            RefreshInlines(LineViewModel);
        else if (e.PropertyName is nameof(TextLineViewModel.WrapText))
            ApplyWrapping(LineViewModel);
    }

    private void RefreshInlines(TextLineViewModel? vm)
    {
        ApplyWrapping(vm);
        var newRuns    = vm?.Runs;
        var newRefSegs = vm?.ReferenceSegments;
        // Same Runs + RefSegs reference → text content unchanged, skip entirely.
        if (ReferenceEquals(newRuns, _lastRuns) && ReferenceEquals(newRefSegs, _lastRefSegs))
            return;

        _lastRuns    = newRuns;
        _lastRefSegs = newRefSegs;

        var inlines = PART_Text.Inlines;
        bool hasRefs = newRefSegs is { Count: > 0 };

        if (newRuns is null || newRuns.Count == 0)
        {
            // Use empty Run rather than Clear() so the TextBlock never shows blank.
            if (inlines.Count == 1 && inlines[0] is Run r0)
            {
                r0.Text = string.Empty;
            }
            else
            {
                // Trim excess from the tail, then set the first to empty.
                while (inlines.Count > 1) inlines.RemoveAt(inlines.Count - 1);
                if (inlines.Count == 1) ((Run)inlines[0]).Text = string.Empty;
                else inlines.Add(new Run { Text = string.Empty });
            }
            return;
        }

        int vPos = 0;

        // Update or create Run objects positionally — never Clear().
        // 1. Update existing runs in-place up to min(old, new) count.
        int commonCount = Math.Min(inlines.Count, newRuns.Count);
        for (int j = 0; j < commonCount; j++)
        {
            if (inlines[j] is not Run run)
            {
                // Unexpected inline type — replace it.
                run = new Run();
                inlines[j] = run;   // ObservableCollection Replace
            }
            var textRun = newRuns[j];
            int rStart  = vPos;
            int rEnd    = vPos + textRun.Text.Length;
            vPos        = rEnd;
            bool isRef  = hasRefs && IsInReference(newRefSegs!, rStart, rEnd, vm!.Text);
            if (run.Text != textRun.Text)
                run.Text = textRun.Text;
            var curBrush = run.Foreground as SolidColorBrush;
            if (curBrush is null || curBrush.Color != textRun.Foreground)
                run.Foreground = new SolidColorBrush(textRun.Foreground);
            var wantDeco = isRef
                ? Windows.UI.Text.TextDecorations.Underline
                : Windows.UI.Text.TextDecorations.None;
            if (run.TextDecorations != wantDeco)
                run.TextDecorations = wantDeco;
        }

        // 2. Add new runs at the tail if new count > old.
        for (int j = commonCount; j < newRuns.Count; j++)
        {
            var textRun = newRuns[j];
            int rStart  = vPos;
            int rEnd    = vPos + textRun.Text.Length;
            vPos        = rEnd;
            bool isRef  = hasRefs && IsInReference(newRefSegs!, rStart, rEnd, vm!.Text);
            var run = new Run
            {
                Text            = textRun.Text,
                Foreground      = new SolidColorBrush(textRun.Foreground),
                TextDecorations = isRef
                    ? Windows.UI.Text.TextDecorations.Underline
                    : Windows.UI.Text.TextDecorations.None,
            };
            inlines.Add(run);
        }

        // 3. Remove excess runs from the tail if old count > new.
        while (inlines.Count > newRuns.Count)
            inlines.RemoveAt(inlines.Count - 1);
    }

    private void ApplyWrapping(TextLineViewModel? vm)
    {
        PART_Text.TextWrapping = vm?.WrapText == true
            ? TextWrapping.WrapWholeWords
            : TextWrapping.NoWrap;
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
