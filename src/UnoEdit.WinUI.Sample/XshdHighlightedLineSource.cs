using System;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;

namespace UnoEdit.WinUI.Sample;

/// <summary>
/// Wraps the classic XSHD/DocumentHighlighter as an IHighlightedLineSource.
/// Mirrors the same class in the Uno sample.
/// </summary>
internal sealed class XshdHighlightedLineSource : IHighlightedLineSource
{
    private readonly IHighlightingDefinition _definition;
    private DocumentHighlighter _highlighter;

    public event EventHandler HighlightingInvalidated;

    public XshdHighlightedLineSource(IHighlightingDefinition definition)
    {
        _definition = definition ?? throw new ArgumentNullException(nameof(definition));
    }

    public void SetDocument(TextDocument document)
    {
        _highlighter?.Dispose();
        _highlighter = null;
        if (document != null)
            _highlighter = new DocumentHighlighter(document, _definition);
        HighlightingInvalidated?.Invoke(this, EventArgs.Empty);
    }

    public HighlightedLine HighlightLine(int lineNumber)
        => _highlighter?.HighlightLine(lineNumber);

    public void Dispose()
    {
        _highlighter?.Dispose();
        _highlighter = null;
    }
}
