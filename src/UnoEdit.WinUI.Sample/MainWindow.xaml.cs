using System;
using System.Linq;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit.Highlighting;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace UnoEdit.WinUI.Sample;

public sealed partial class MainWindow : Window
{
    private readonly TextDocument _document;
    private readonly FoldingManager _foldingManager;
    private readonly BraceFoldingStrategy _folding = new();
    private bool _isDark = true;

    public MainWindow()
    {
        this.InitializeComponent();

        // Resize the window to something comfortable for an editor sample.
        if (AppWindow is AppWindow aw)
            aw.Resize(new Windows.Graphics.SizeInt32(1100, 720));

        // Build the initial document with sample C# text.
        _document = new TextDocument(BuildSampleText());
        _foldingManager = new FoldingManager(_document);
        _document.Changed += OnDocumentChanged;

        // Wire the editor control to the document model.
        Editor.Document = _document;

        // Populate the syntax combo with all registered XSHD highlighters.
        foreach (var def in HighlightingManager.Instance.HighlightingDefinitions.OrderBy(d => d.Name))
            SyntaxComboBox.Items.Add(def.Name);

        // Select C# by default.
        var csIndex = SyntaxComboBox.Items.IndexOf("C#");
        if (csIndex >= 0)
        {
            SyntaxComboBox.SelectedIndex = csIndex;
        }
        else
        {
            // Fall back to first entry if C# is not registered.
            if (SyntaxComboBox.Items.Count > 0)
                SyntaxComboBox.SelectedIndex = 0;
        }

        UpdateStats();
    }

    // ── Event handlers ────────────────────────────────────────────────────
    private void OnDocumentChanged(object sender, DocumentChangeEventArgs e) => UpdateStats();

    private void OnEditorTextChanged(object sender, EventArgs e)
    {
        _folding.UpdateFoldings(_foldingManager, _document);
        UpdateStats();
    }

    private void OnSyntaxChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SyntaxComboBox.SelectedItem is string name)
        {
            Editor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition(name);
            StatusText.Text = $"Syntax: {name}";
        }
    }

    private void OnThemeButtonClick(object sender, RoutedEventArgs e)
    {
        _isDark = !_isDark;
        ThemeButton.Content = _isDark ? "☀ Light" : "🌙 Dark";
        // Apply WinUI theme to the window content.
        if (Content is FrameworkElement fe)
            fe.RequestedTheme = _isDark ? ElementTheme.Dark : ElementTheme.Light;
        StatusText.Text = $"Theme: {(_isDark ? "Dark" : "Light")}";
    }

    // ── Helpers ───────────────────────────────────────────────────────────
    private void UpdateStats()
    {
        if (_document == null) return;
        int lines = _document.LineCount;
        int chars = _document.TextLength;
        DocumentLine first = _document.GetLineByNumber(1);
        DocumentLine last  = _document.GetLineByNumber(lines);
        StatsText.Text = $"Lines: {lines}  |  Chars: {chars}" +
                         $"  |  First-line len: {first.Length}" +
                         $"  |  Last-line offset: {last.Offset}";
    }

    private static string BuildSampleText() => """
        using System;

        namespace Demo;

        /// <summary>Sample class exercising the UnoEdit document model on WinUI 3.</summary>
        public static class Bootstrap
        {
            public static string Describe()
            {
                return "UnoEdit document core is running on WinUI 3.";
            }

            public static void PrintInfo()
            {
                Console.WriteLine(Describe());
                for (int i = 0; i < 5; i++)
                {
                    Console.WriteLine($"  Line {i}");
                }
            }
        }
        """;
}
