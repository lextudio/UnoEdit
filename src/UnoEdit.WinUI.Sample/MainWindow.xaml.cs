using System;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit.Highlighting;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Windows.Input;
using UnoEdit.Skia.Desktop.Controls;
using UnoEdit.WinUI.Controls;
using Windows.Graphics;

namespace UnoEdit.WinUI.Sample;

public sealed partial class MainWindow : Window
{
    private readonly TextDocument _document;
    private readonly FoldingManager _foldingManager;
    private readonly BraceFoldingStrategy _foldingStrategy = new();
    private bool _isDarkTheme = true;
    private CompletionWindow? _completionWindow;

    public MainWindow()
    {
        this.InitializeComponent();

        if (AppWindow is AppWindow aw)
            aw.Resize(new SizeInt32(1100, 720));

        _document = new TextDocument(BuildSampleText());
        _foldingManager = new FoldingManager(_document);

        // Detect OS theme on startup.
        if (Application.Current.RequestedTheme == ApplicationTheme.Light)
        {
            _isDarkTheme = false;
            ThemeToggle.Content = "\U0001F319 Dark";
        }

        Editor.Document = _document;
        if (!_isDarkTheme)
            Editor.Theme = TextEditorTheme.Light;

        Editor.FoldingManager = _foldingManager;

        // Set initial selection and apply highlighting now that Editor is ready.
        HighlighterComboBox.SelectedIndex = 0;
        ApplyHighlighter(0);

        // Wire events in code-behind (not in XAML) to avoid XAML parser failures
        // with non-standard event delegate types on the custom control.
        Editor.TextArea.TextEntered += OnTextAreaTextEntered;
        Editor.TextArea.TextEntering += OnTextAreaTextEntering;
        _document.TextChanged += OnDocumentTextChanged;

        UpdateFoldings();
        StatsTextBlock.Text = BuildStats(_document);
    }

    // --- Document events ---

    private void OnDocumentTextChanged(object sender, EventArgs e)
    {
        UpdateFoldings();
        StatsTextBlock.Text = BuildStats(_document);
    }

    private void UpdateFoldings()
    {
        _foldingStrategy.UpdateFoldings(_foldingManager, _document);
    }

    // --- Toolbar handlers ---

    private void OnHighlighterChanged(object sender, SelectionChangedEventArgs e)
    {
        ApplyHighlighter(HighlighterComboBox.SelectedIndex);
    }

    private void ApplyHighlighter(int index)
    {
        if (Editor == null) return;
        var def = HighlightingManager.Instance.GetDefinitionByExtension(".cs");
        Editor.HighlightedLineSource = index switch
        {
            0 when def != null => new XshdHighlightedLineSource(def),
            _ => null,
        };
    }

    private void OnThemeToggleClick(object sender, RoutedEventArgs e)
    {
        _isDarkTheme = !_isDarkTheme;
        Editor.Theme = _isDarkTheme ? TextEditorTheme.Dark : TextEditorTheme.Light;
        ThemeToggle.Content = _isDarkTheme ? "\u2600 Light" : "\U0001F319 Dark";
    }

    private void OnFindClick(object sender, RoutedEventArgs e)
    {
        Editor.OpenSearchPanel();
    }

    private void OnCompleteClick(object sender, RoutedEventArgs e)
    {
        ShowCompletion();
    }

    // --- TextArea proxy events (mirrors the Uno sample) ---

    private void OnTextAreaTextEntered(object? sender, TextCompositionEventArgs e)
    {
        if (e?.Text == ".")
            ShowCompletion();
    }

    private void OnTextAreaTextEntering(object? sender, TextCompositionEventArgs e)
    {
        if (_completionWindow is not null && e?.Text?.Length > 0)
        {
            if (!char.IsLetterOrDigit(e.Text[0]) && e.Text[0] != '_')
                _completionWindow.Close();
        }
    }

    // --- Completion ---

    private void ShowCompletion()
    {
        _completionWindow?.Close();
        _completionWindow = new CompletionWindow(Editor.TextArea);
        var data = _completionWindow.CompletionList.CompletionData;
        data.Add(new SampleCompletionData("Describe()", "Return a description string"));
        data.Add(new SampleCompletionData("Bootstrap", "Sample class name"));
        data.Add(new SampleCompletionData("DescribeAsync()", "Async variant"));
        _completionWindow.Show();
        _completionWindow.Closed += (_, _) => _completionWindow = null;
    }

    // --- Helpers ---

    private static string BuildSampleText()
    {
        return """
using System;

namespace Demo;

public static class Bootstrap
{
    public static string Describe()
    {
        return "UnoEdit document core is running on WinUI 3.";
    }
}
""";
    }

    private static string BuildStats(TextDocument document)
    {
        DocumentLine firstLine = document.GetLineByNumber(1);
        DocumentLine lastLine = document.GetLineByNumber(document.LineCount);

        return $"Length: {document.TextLength}\n" +
               $"Lines: {document.LineCount}\n" +
               $"First line length: {firstLine.Length}\n" +
               $"Last line starts at offset: {lastLine.Offset}";
    }
}
