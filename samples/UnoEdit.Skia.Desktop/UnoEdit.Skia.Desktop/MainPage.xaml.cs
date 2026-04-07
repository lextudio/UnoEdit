using ICSharpCode.AvalonEdit.Document;

namespace UnoEdit.Skia.Desktop;

public sealed partial class MainPage : Page
{
    private readonly TextDocument _document;

    public MainPage()
    {
        this.InitializeComponent();

        _document = new TextDocument(BuildSampleText());
        DocumentTextBox.Text = _document.Text;
        StatsTextBlock.Text = BuildStats(_document);
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
}
