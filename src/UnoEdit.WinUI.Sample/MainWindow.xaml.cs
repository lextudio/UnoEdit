using System;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.TextMate;
using TextMateSharp.Grammars;
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
    private readonly TextMateLineHighlighter _textMateHighlighter;
    private readonly RegistryOptions _textMateRegistryOptions;
    private bool _isDarkTheme = true;
    private CompletionWindow? _completionWindow;

    public MainWindow()
    {
        this.InitializeComponent();

        if (AppWindow is AppWindow aw)
            aw.Resize(new SizeInt32(1100, 720));

        _document = new TextDocument(BuildSampleText());
        _foldingManager = new FoldingManager(_document);

        _textMateRegistryOptions = new RegistryOptions(ThemeName.DarkPlus);
        _textMateHighlighter = new TextMateLineHighlighter(_textMateRegistryOptions);
        _textMateHighlighter.SetGrammarByExtension(".cs");

        // Detect OS theme on startup and configure TextMate theme.
        if (Application.Current.RequestedTheme == ApplicationTheme.Light)
        {
            _isDarkTheme = false;
            _textMateRegistryOptions = new RegistryOptions(ThemeName.LightPlus);
            _textMateHighlighter.SetTheme(ThemeName.LightPlus);
            ThemeToggle.Content = "\U0001F319 Dark";
        }

        Editor.Document = _document;
        if (!_isDarkTheme)
            Editor.Theme = TextEditorTheme.Light;

        Editor.FoldingManager = _foldingManager;

        // Set initial selection and apply highlighting now that Editor is ready.
        HighlighterComboBox.SelectedIndex = 1;
        ApplyHighlighter(1);

        PropertyObjectComboBox.SelectedIndex = 0;
        PropertyGrid.SelectedObject = Editor;

        // Force editor refresh by re-assigning Options when changes are detected
        var origOptions = Editor.Options;
        origOptions.PropertyChanged += (s, e) =>
        {
            Console.WriteLine($"[Sample] Options.{e.PropertyName} changed");
            var options = Editor.Options;
            Editor.Options = null;
            Editor.Options = options;
        };

        // Wire events in code-behind (not in XAML) to avoid XAML parser failures
        // with non-standard event delegate types on the custom control.
        Editor.TextArea.TextEntered += OnTextAreaTextEntered;
        Editor.TextArea.TextEntering += OnTextAreaTextEntering;
        _document.TextChanged += OnDocumentTextChanged;

        UpdateFoldings();
        StatsTextBlock.Text = BuildStats(_document);
    }

    // --- Document events ---

    private void OnDocumentTextChanged(object? sender, EventArgs e)
    {
        UpdateFoldings();
        StatsTextBlock.Text = BuildStats(_document);
    }

    private void UpdateFoldings()
    {
        _foldingStrategy.UpdateFoldings(_foldingManager, _document);
    }

    // --- Toolbar handlers ---

    private void OnPropertyObjectChanged(object sender, SelectionChangedEventArgs e)
    {
        if (PropertyGrid == null || Editor == null) return;
        PropertyGrid.SelectedObject = PropertyObjectComboBox.SelectedIndex switch
        {
            1 => Editor.TextArea,
            2 => Editor.Options,
            _ => Editor,
        };
    }

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
            0 => _textMateHighlighter,
            1 when def != null => new XshdHighlightedLineSource(def),
            _ => null,
        };
    }

    private void OnThemeToggleClick(object sender, RoutedEventArgs e)
    {
        _isDarkTheme = !_isDarkTheme;
        Editor.Theme = _isDarkTheme ? TextEditorTheme.Dark : TextEditorTheme.Light;
        _textMateHighlighter.SetTheme(_isDarkTheme ? ThemeName.DarkPlus : ThemeName.LightPlus);
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
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Demo.Workspace;

public sealed class WorkspaceDocument
{
    public WorkspaceDocument(string name, IReadOnlyList<ProjectNode> projects, DateTimeOffset generatedAt)
    {
        Name = name;
        Projects = projects;
        GeneratedAt = generatedAt;
    }

    public string Name { get; }
    public IReadOnlyList<ProjectNode> Projects { get; }
    public DateTimeOffset GeneratedAt { get; }
}

public sealed class ProjectNode
{
    public ProjectNode(string id, string language, IReadOnlyList<SourceFile> files, IDictionary<string, string> metadata)
    {
        Id = id;
        Language = language;
        Files = files;
        Metadata = metadata;
    }

    public string Id { get; }
    public string Language { get; }
    public IReadOnlyList<SourceFile> Files { get; }
    public IDictionary<string, string> Metadata { get; }
}

public sealed class SourceFile
{
    public SourceFile(string path, int lineCount, bool isGenerated, string checksum)
    {
        Path = path;
        LineCount = lineCount;
        IsGenerated = isGenerated;
        Checksum = checksum;
    }

    public string Path { get; }
    public int LineCount { get; }
    public bool IsGenerated { get; }
    public string Checksum { get; }
}

public interface IWorkspaceFormatter
{
    ValueTask<string> RenderAsync(WorkspaceDocument document, CancellationToken cancellationToken = default);
}

public sealed class WorkspaceFormatter : IWorkspaceFormatter
{
    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public async ValueTask<string> RenderAsync(WorkspaceDocument document, CancellationToken cancellationToken = default)
    {
        var buffer = new List<string>();
        var watch = Stopwatch.StartNew();

        buffer.Add("// Longer sample used to stress UnoEdit highlighter switching.");
        buffer.Add($"// Generated: {document.GeneratedAt:O}");
        buffer.Add($"// Culture: {CultureInfo.InvariantCulture.Name}");
        buffer.Add(string.Empty);
        buffer.Add("namespace Demo.Workspace.Generated;");
        buffer.Add(string.Empty);
        buffer.Add("public static partial class Snapshot");
        buffer.Add("{");
        buffer.Add($"    public const string Name = \"{document.Name}\";");
        buffer.Add($"    public const int ProjectCount = {document.Projects.Length};");
        buffer.Add(string.Empty);
        buffer.Add("    public static string ExportJson()");
        buffer.Add("    {");

        string payload = JsonSerializer.Serialize(document, _serializerOptions).Replace("\"", "\"\"");
        buffer.Add("        return \"\"\"");
        buffer.Add(payload);
        buffer.Add("        \"\"\";");
        buffer.Add("    }");
        buffer.Add("}");
        buffer.Add(string.Empty);

        foreach (ProjectNode project in document.Projects)
        {
            cancellationToken.ThrowIfCancellationRequested();
            buffer.Add($"public sealed class {project.Id}Summary");
            buffer.Add("{");
            buffer.Add($"    public string Language {{ get; }} = \"{project.Language}\";");
            buffer.Add($"    public int FileCount {{ get; }} = {project.Files.Length};");
            buffer.Add("    public IEnumerable<string> EnumerateFiles()");
            buffer.Add("    {");

            foreach (SourceFile file in project.Files)
            {
                buffer.Add($"        yield return $\"{file.Path} ({file.LineCount} lines, generated={file.IsGenerated.ToString().ToLowerInvariant()})\";");
            }

            buffer.Add("    }");
            buffer.Add("}");
            buffer.Add(string.Empty);
        }

        await Task.Delay(1, cancellationToken);
        watch.Stop();

        buffer.Add("file static class Metrics");
        buffer.Add("{");
        buffer.Add($"    public static long ElapsedMilliseconds => {watch.ElapsedMilliseconds};");
        buffer.Add("}");

        return string.Join(Environment.NewLine, buffer);
    }
}

public static class WorkspaceFactory
{
    public static WorkspaceDocument Create()
    {
        return new WorkspaceDocument(
            "DemoWorkspace",
            new[]
            {
                new ProjectNode(
                    "EditorCore",
                    "CSharp",
                    new[]
                    {
                        new SourceFile("src/Editor/DocumentModel.cs", 214, false, "A1B2C3"),
                        new SourceFile("src/Editor/ViewportLayout.cs", 387, false, "A1B2C4"),
                        new SourceFile("src/Editor/GeneratedTheme.g.cs", 122, true, "A1B2C5"),
                    },
                    new Dictionary<string, string>
                    {
                        ["owner"] = "platform",
                        ["tier"] = "hot"
                    }),
                new ProjectNode(
                    "EditorTests",
                    "CSharp",
                    new[]
                    {
                        new SourceFile("tests/Highlighting/TextMateSwitchTests.cs", 146, false, "D4E5F6"),
                        new SourceFile("tests/Highlighting/ViewportTokenizationTests.cs", 173, false, "D4E5F7"),
                    },
                    new Dictionary<string, string>
                    {
                        ["owner"] = "qa",
                        ["tier"] = "validation"
                    }),
                new ProjectNode(
                    "Docs",
                    "Markdown",
                    new[]
                    {
                        new SourceFile("docs/textmate.md", 188, false, "FF1122"),
                        new SourceFile("docs/unification.md", 141, false, "FF1123"),
                    },
                    new Dictionary<string, string>
                    {
                        ["owner"] = "docs",
                        ["tier"] = "reference"
                    })
            },
            DateTimeOffset.Parse("2026-04-21T18:30:00Z", CultureInfo.InvariantCulture));
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
