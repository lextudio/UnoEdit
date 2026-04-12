using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.TextMate;
using TextMateSharp.Grammars;
using UnoEdit.Skia.Desktop.Controls;
using ICSharpCode.AvalonEdit.CodeCompletion;
using System.Windows.Input;

namespace UnoEdit.Skia.Desktop;

public sealed partial class MainPage : Page
{
    private readonly TextDocument _document;
    private readonly FoldingManager _foldingManager;
    private readonly BraceFoldingStrategy _foldingStrategy = new();
    private readonly TextMateLineHighlighter _textMateHighlighter;
    private readonly RegistryOptions _textMateRegistryOptions;
    private bool _isDarkTheme = true;
    private CompletionWindow? _completionWindow;

    public MainPage()
    {
        this.InitializeComponent();

        _document = new TextDocument(BuildSampleText());
        _foldingManager = new FoldingManager(_document);
        _textMateRegistryOptions = new RegistryOptions(ThemeName.DarkPlus);
        _textMateHighlighter = new TextMateLineHighlighter(_textMateRegistryOptions);
        _textMateHighlighter.SetGrammarByExtension(".cs");
        // Detect OS theme and apply the matching editor theme on startup.
        if (Application.Current.RequestedTheme == ApplicationTheme.Light)
        {
            _isDarkTheme = false;
            _textMateRegistryOptions = new RegistryOptions(ThemeName.LightPlus);
            _textMateHighlighter.SetTheme(ThemeName.LightPlus);
        }
        Editor.Document = _document;
        if (!_isDarkTheme)
        {
            Editor.Theme = TextEditorTheme.Light;
            ThemeToggle.Content = "🌙 Dark";
        }
        Editor.FoldingManager = _foldingManager;
        Editor.HighlightedLineSource = _textMateHighlighter;
        // Wire search and simple completion hooks for the sample host.
        Editor.TextArea.TextEntered += OnTextAreaTextEntered;
        Editor.TextArea.TextEntering += OnTextAreaTextEntering;
        _document.TextChanged += OnDocumentTextChanged;
        UpdateFoldings();
        StatsTextBlock.Text = BuildStats(_document);
    }

    private void OnDocumentTextChanged(object? sender, EventArgs e)
    {
        UpdateFoldings();
        StatsTextBlock.Text = BuildStats(_document);
    }

    private void UpdateFoldings()
    {
        _foldingStrategy.UpdateFoldings(_foldingManager, _document);
    }

    private void OnHighlighterChanged(object sender, SelectionChangedEventArgs e)
    {
        Editor.HighlightedLineSource = HighlighterComboBox.SelectedIndex switch
        {
            0 => _textMateHighlighter,
            1 => new XshdHighlightedLineSource(HighlightingManager.Instance.GetDefinitionByExtension(".cs")),
            _ => null,
        };
    }

    private void OnThemeToggleClick(object sender, RoutedEventArgs e)
    {
        _isDarkTheme = !_isDarkTheme;
        Editor.Theme = _isDarkTheme ? TextEditorTheme.Dark : TextEditorTheme.Light;
        _textMateHighlighter.SetTheme(_isDarkTheme ? ThemeName.DarkPlus : ThemeName.LightPlus);
        ThemeToggle.Content = _isDarkTheme ? "☀ Light" : "🌙 Dark";
    }

    private static string BuildSampleText()
    {
        return """
using System;

namespace Demo;

public static class Bootstrap
{
    public static string Describe()
    {
        return "UnoEdit document core is running on Uno Skia Desktop.";
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

    private void OnFindClick(object sender, RoutedEventArgs e)
    {
        Editor.OpenSearchPanel();
    }

    private void OnCompleteClick(object sender, RoutedEventArgs e)
    {
        ShowCompletion();
    }

    private void OnTextAreaTextEntered(object? sender, TextCompositionEventArgs e)
    {
        if (e?.Text == ".")
        {
            ShowCompletion();
        }
    }

    private void OnTextAreaTextEntering(object? sender, TextCompositionEventArgs e)
    {
        if (_completionWindow != null && e is not null && !string.IsNullOrEmpty(e.Text) && !char.IsLetterOrDigit(e.Text[0]))
        {
            _completionWindow.CompletionList.RequestInsertion(e);
        }
    }

    private void ShowCompletion()
    {
        try
        {
            var textArea = Editor.TextArea;
            var doc = Editor.Document;
            if (textArea == null || doc == null)
                return;

            int caret = Editor.CurrentOffset;
            int start = caret;
            var text = doc.Text ?? string.Empty;
            while (start > 0 && (char.IsLetterOrDigit(text[start - 1]) || text[start - 1] == '_')) start--;

            var window = new CompletionWindow(textArea);
            window.StartOffset = start;
            window.EndOffset = caret;

            var list = window.CompletionList.CompletionData;
            list.Add(new SampleCompletionData("Describe()", "Return a description string"));
            list.Add(new SampleCompletionData("Bootstrap", "Sample class name"));
            list.Add(new SampleCompletionData("DescribeAsync()", "Async variant"));

            // Wire insertion handler: insert selected item's Text into document.
            window.CompletionList.InsertionRequested += (s, e) =>
            {
                var selected = window.CompletionList.SelectedItem;
                if (selected is not null)
                {
                    int length = Math.Clamp(window.EndOffset - window.StartOffset, 0, doc.TextLength - window.StartOffset);
                    doc.Replace(window.StartOffset, length, selected.Text);
                    // Move caret after inserted text
                    Editor.SetSelection(window.StartOffset, window.StartOffset + selected.Text.Length);
                }
            };

            window.Closed += (s, e) => { _completionWindow = null; };
            _completionWindow = window;
            window.Show();
        }
        catch
        {
            // best-effort sample completion; swallow exceptions to avoid breaking sample host
        }
    }
}
