using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;
using Microsoft.Maui.DevFlow.Agent.Core;
using Microsoft.UI.Dispatching;

namespace UnoEdit.Skia.Desktop;

public static class UnoEditHighlightTests
{
    [DevFlowAction("unoedit.highlight.test", Description = "Load a file with syntax highlighting and return colored sections. Args: [filePath, languageExtension]")]
    public static string TestHighlight(string filePath, string languageExtension)
    {
        try
        {
            var text = File.ReadAllText(filePath);
            var doc = new TextDocument(text);
            var def = HighlightingManager.Instance.GetDefinitionByExtension(languageExtension);
            if (def == null)
                return JsonSerializer.Serialize(new { success = false, error = $"No highlighting definition for '{languageExtension}'" });

            using var highlighter = new DocumentHighlighter(doc, def);
            int coloredLines = 0;
            int totalColoredSections = 0;

            for (int i = 1; i <= doc.LineCount; i++)
            {
                var line = highlighter.HighlightLine(i);
                var sections = line.Sections
                    .Where(s => s.Color?.Foreground != null)
                    .ToList();

                if (sections.Count > 0)
                {
                    coloredLines++;
                    totalColoredSections += sections.Count;
                }
            }

            return JsonSerializer.Serialize(new
            {
                success = true,
                language = def.Name,
                totalLines = doc.LineCount,
                coloredLines,
                totalColoredSections
            });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { success = false, error = ex.Message });
        }
    }

    [DevFlowAction("editor.load.file", Description = "Load a file into the sample editor window visually. Args: [filePath, languageExtension]")]
    public static string LoadFile(string filePath, string languageExtension)
    {
        try
        {
            var mainWindow = MainWindow.Instance;
            if (mainWindow == null)
                return JsonSerializer.Serialize(new { success = false, error = "No MainWindow instance available" });

            if (!File.Exists(filePath))
                return JsonSerializer.Serialize(new { success = false, error = $"File not found: {filePath}" });

            mainWindow.DispatcherQueue.TryEnqueue(() =>
            {
                mainWindow.LoadFile(filePath, languageExtension);
            });

            var text = File.ReadAllText(filePath);
            var doc = new TextDocument(text);
            return JsonSerializer.Serialize(new
            {
                success = true,
                file = filePath,
                extension = languageExtension,
                totalLines = doc.LineCount,
                totalChars = text.Length
            });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { success = false, error = ex.Message });
        }
    }
}
