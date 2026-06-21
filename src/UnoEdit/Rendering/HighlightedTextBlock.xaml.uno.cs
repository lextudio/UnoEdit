using Microsoft.UI.Xaml.Media;
#if WINDOWS_APP_SDK
using Microsoft.UI.Xaml.Documents;
#endif

namespace UnoEdit.Skia.Desktop.Controls;

public sealed partial class HighlightedTextBlock : UserControl
{
    public static readonly DependencyProperty LineViewModelProperty =
        DependencyProperty.Register(
            nameof(LineViewModel),
            typeof(TextLineViewModel),
            typeof(HighlightedTextBlock),
            new PropertyMetadata(null, OnLineViewModelChanged));

    public static readonly DependencyProperty EditorFontFamilyProperty =
        DependencyProperty.Register(
            nameof(EditorFontFamily),
            typeof(FontFamily),
            typeof(HighlightedTextBlock),
            new PropertyMetadata(EditorTextMetrics.CreateFontFamily(), OnEditorFontChanged));

    public static readonly DependencyProperty EditorFontSizeProperty =
        DependencyProperty.Register(
            nameof(EditorFontSize),
            typeof(double),
            typeof(HighlightedTextBlock),
            new PropertyMetadata(EditorTextMetrics.FontSize, OnEditorFontChanged));

    public static readonly DependencyProperty EditorFontWeightProperty =
        DependencyProperty.Register(
            nameof(EditorFontWeight),
            typeof(Windows.UI.Text.FontWeight),
            typeof(HighlightedTextBlock),
            new PropertyMetadata(new Windows.UI.Text.FontWeight { Weight = 400 }, OnEditorFontChanged));

    public static readonly DependencyProperty EditorFontStyleProperty =
        DependencyProperty.Register(
            nameof(EditorFontStyle),
            typeof(Windows.UI.Text.FontStyle),
            typeof(HighlightedTextBlock),
            new PropertyMetadata(Windows.UI.Text.FontStyle.Normal, OnEditorFontChanged));

    // Last rendered Runs/refs — used to skip unnecessary Inlines rebuilds when only
    // caret/selection state changed (text content is the same object reference).
    private System.Collections.Generic.IReadOnlyList<TextRun>? _lastRuns;
    private System.Collections.Generic.IReadOnlyList<ICSharpCode.AvalonEdit.Rendering.ReferenceSegment>? _lastRefSegs;

    // The inner text control hosted by PART_Host. On the WinUI target this is the native
    // TextBlock, which renders embedded InlineUIContainers (the control-character boxes).
    // On the Uno desktop target the native TextBlock silently drops InlineUIContainer, so we
    // host a LeXtudio RichTextBlock instead — it renders the boxes the same way WPF AvalonEdit
    // does via SingleCharacterElementGenerator. The two controls take different inline type
    // systems (Microsoft.UI.Xaml.Documents vs the System.Windows.Documents shim), so the
    // inline-building code below is split per platform.
#if WINDOWS_APP_SDK
    private readonly Microsoft.UI.Xaml.Controls.TextBlock _text = new()
    {
        TextWrapping = TextWrapping.NoWrap,
        VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Center,
    };
#else
    private readonly LeXtudio.UI.Xaml.Controls.RichTextBlock _text = new()
    {
        TextWrapping = TextWrapping.NoWrap,
        VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Center,
    };
#endif

    public HighlightedTextBlock()
    {
        this.InitializeComponent();
        PART_Host.Child = _text;
        ApplyEditorFont();
    }

    public TextLineViewModel? LineViewModel
    {
        get => (TextLineViewModel?)GetValue(LineViewModelProperty);
        set => SetValue(LineViewModelProperty, value);
    }

    public FontFamily EditorFontFamily
    {
        get => (FontFamily)GetValue(EditorFontFamilyProperty);
        set => SetValue(EditorFontFamilyProperty, value);
    }

    public double EditorFontSize
    {
        get => (double)GetValue(EditorFontSizeProperty);
        set => SetValue(EditorFontSizeProperty, value);
    }

    public Windows.UI.Text.FontWeight EditorFontWeight
    {
        get => (Windows.UI.Text.FontWeight)GetValue(EditorFontWeightProperty);
        set => SetValue(EditorFontWeightProperty, value);
    }

    public Windows.UI.Text.FontStyle EditorFontStyle
    {
        get => (Windows.UI.Text.FontStyle)GetValue(EditorFontStyleProperty);
        set => SetValue(EditorFontStyleProperty, value);
    }

    private static void OnEditorFontChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((HighlightedTextBlock)d).ApplyEditorFont();
    }

    private void ApplyEditorFont()
    {
        _text.FontFamily = EditorFontFamily;
        _text.FontSize = EditorFontSize;
        _text.FontWeight = EditorFontWeight;
        _text.FontStyle = EditorFontStyle;
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

        ApplyInlines(newRuns, newRefSegs, vm);
    }

    private void ApplyWrapping(TextLineViewModel? vm)
    {
        _text.TextWrapping = vm?.WrapText == true
            ? TextWrapping.WrapWholeWords
            : TextWrapping.NoWrap;
    }

#if WINDOWS_APP_SDK
    private void ApplyInlines(
        System.Collections.Generic.IReadOnlyList<TextRun>? newRuns,
        System.Collections.Generic.IReadOnlyList<ICSharpCode.AvalonEdit.Rendering.ReferenceSegment>? newRefSegs,
        TextLineViewModel? vm)
    {
        var inlines = _text.Inlines;
        bool hasRefs = newRefSegs is { Count: > 0 };

        // Lines containing control-character boxes mix Run and InlineUIContainer inlines; the
        // positional in-place updater below only handles plain Runs, so rebuild them wholesale.
        if (HasControlCharacterBox(newRuns))
        {
            RebuildInlinesWithBoxes(newRuns!, newRefSegs, vm);
            return;
        }

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

    private static bool HasControlCharacterBox(System.Collections.Generic.IReadOnlyList<TextRun>? runs)
    {
        if (runs is null)
            return false;
        for (int i = 0; i < runs.Count; i++)
            if (runs[i].IsControlCharacterBox)
                return true;
        return false;
    }

    // Builds a fresh inline collection where control-character runs render as a gray rounded box
    // (InlineUIContainer → Border → TextBlock) and the rest as ordinary colored Runs, matching
    // WPF AvalonEdit's SingleCharacterElementGenerator.
    private void RebuildInlinesWithBoxes(
        System.Collections.Generic.IReadOnlyList<TextRun> runs,
        System.Collections.Generic.IReadOnlyList<ICSharpCode.AvalonEdit.Rendering.ReferenceSegment>? refSegs,
        TextLineViewModel? vm)
    {
        var inlines = _text.Inlines;
        inlines.Clear();
        bool hasRefs = refSegs is { Count: > 0 };
        int vPos = 0;

        foreach (var textRun in runs)
        {
            if (textRun.IsControlCharacterBox)
            {
                inlines.Add(CreateControlCharacterInline(textRun.Text));
                vPos += 1; // a control character occupies a single visual column
                continue;
            }

            int rStart = vPos;
            int rEnd = vPos + textRun.Text.Length;
            vPos = rEnd;
            bool isRef = hasRefs && IsInReference(refSegs!, rStart, rEnd, vm?.Text ?? string.Empty);
            inlines.Add(new Run
            {
                Text = textRun.Text,
                Foreground = new SolidColorBrush(textRun.Foreground),
                TextDecorations = isRef
                    ? Windows.UI.Text.TextDecorations.Underline
                    : Windows.UI.Text.TextDecorations.None,
            });
        }
    }

    private InlineUIContainer CreateControlCharacterInline(string name)
        => new() { Child = CreateControlCharacterBox(name) };
#else
    // Uno desktop: rebuild the RichTextBlock's inline collection in one pass. Plain runs become
    // System.Windows.Documents.Run, control-character runs become a boxed Border (added as an
    // implicit InlineUIContainer). RichTextBlock re-lays-out on every Inlines mutation, and the
    // caret/line-selection are drawn by sibling overlay elements in TextView's DataTemplate, so
    // a clean rebuild here is both correct and cheap (only runs on actual text changes — the
    // RefreshInlines early-out skips caret/selection-only updates).
    private void ApplyInlines(
        System.Collections.Generic.IReadOnlyList<TextRun>? newRuns,
        System.Collections.Generic.IReadOnlyList<ICSharpCode.AvalonEdit.Rendering.ReferenceSegment>? newRefSegs,
        TextLineViewModel? vm)
    {
        // RichTextBlock's top-level Inlines collection is owned by a FlowDocument, whose WPF
        // schema rejects inlines as direct children — content must hang off a Block. So build a
        // single Paragraph and add the runs to its Inlines (the same path the RichTextBlock
        // samples use); the renderer falls through to the Blocks loop when Inlines is empty.
        _text.Blocks.Clear();

        if (newRuns is null || newRuns.Count == 0)
            return;

        var paragraph = new System.Windows.Documents.Paragraph();
        var inlines = paragraph.Inlines;
        bool hasRefs = newRefSegs is { Count: > 0 };
        int vPos = 0;

        foreach (var textRun in newRuns)
        {
            if (textRun.IsControlCharacterBox)
            {
                // Add(UIElement) wraps the box in an implicit InlineUIContainer.
                inlines.Add(CreateControlCharacterBox(textRun.Text));
                vPos += 1; // a control character occupies a single visual column
                continue;
            }

            int rStart = vPos;
            int rEnd = vPos + textRun.Text.Length;
            vPos = rEnd;
            bool isRef = hasRefs && IsInReference(newRefSegs!, rStart, rEnd, vm?.Text ?? string.Empty);

            var run = new System.Windows.Documents.Run
            {
                Text = textRun.Text,
                Foreground = new SolidColorBrush(textRun.Foreground),
                Background = textRun.Background.HasValue ? new SolidColorBrush(textRun.Background.Value) : null,
            };
            if (isRef)
                run.TextDecorations = System.Windows.Media.TextDecorations.Underline;
            inlines.Add(run);
        }

        _text.Blocks.Add(paragraph);
    }
#endif

    // Gray rounded box bearing the control character's name (NUL, ETX, …), matching WPF
    // AvalonEdit's SingleCharacterElementGenerator. Shared by both platforms' inline builders.
    private Microsoft.UI.Xaml.Controls.Border CreateControlCharacterBox(string name)
    {
        var label = new Microsoft.UI.Xaml.Controls.TextBlock
        {
            Text = name,
            FontFamily = EditorFontFamily,
            FontSize = EditorFontSize * 0.7,
            Foreground = new SolidColorBrush(Microsoft.UI.Colors.White),
            VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Center,
            TextAlignment = Microsoft.UI.Xaml.TextAlignment.Center,
        };
        return new Microsoft.UI.Xaml.Controls.Border
        {
            Background = new SolidColorBrush(Windows.UI.Color.FromArgb(200, 128, 128, 128)),
            CornerRadius = new Microsoft.UI.Xaml.CornerRadius(2.5),
            Padding = new Microsoft.UI.Xaml.Thickness(2, 0, 2, 0),
            Margin = new Microsoft.UI.Xaml.Thickness(0.5, 0, 0.5, 0),
            VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Center,
            Child = label,
        };
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
