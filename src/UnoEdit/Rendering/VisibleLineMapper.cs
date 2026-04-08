// VisibleLineMapper: maps document lines to visual rows considering folded sections.
namespace ICSharpCode.AvalonEdit.Rendering
{
    using System;
    using System.Collections.Generic;
    using ICSharpCode.AvalonEdit.Document;
    using ICSharpCode.AvalonEdit.Folding;

    /// <summary>
    /// Helper that maps document line numbers to visual rows taking folded sections into account.
    /// This is a small, testable replacement for the visible-line logic embedded in TextView.
    /// </summary>
    public class VisibleLineMapper : IDisposable
    {
        readonly TextDocument _document;
        readonly FoldingManager? _foldingManager;
        readonly List<int> _visibleLines = new List<int>();

        public VisibleLineMapper(TextDocument document, FoldingManager? foldingManager)
        {
            _document = document ?? throw new ArgumentNullException(nameof(document));
            _foldingManager = foldingManager;
            if (_foldingManager != null)
                _foldingManager.FoldingsChanged += OnFoldingsChanged;
            Rebuild();
        }

        void OnFoldingsChanged(object? s, EventArgs e) => Rebuild();

        public IReadOnlyList<int> VisibleLines => _visibleLines;

        public void Rebuild()
        {
            _visibleLines.Clear();
            if (_document == null) return;
            for (int ln = 1; ln <= _document.LineCount; ln++)
            {
                if (!IsLineHidden(ln))
                    _visibleLines.Add(ln);
            }
        }

        public bool IsLineHidden(int lineNumber)
        {
            if (_foldingManager is null || _document is null) return false;
            foreach (var section in _foldingManager.AllFoldings)
            {
                if (!section.IsFolded) continue;
                int foldStartLine = _document.GetLineByOffset(section.StartOffset).LineNumber;
                int foldEndLine   = _document.GetLineByOffset(section.EndOffset).LineNumber;
                if (lineNumber > foldStartLine && lineNumber <= foldEndLine)
                    return true;
            }
            return false;
        }

        /// <summary>Returns 0-based visual row index for the given document line, or -1 if hidden.</summary>
        public int GetVisualRow(int docLineNumber)
        {
            if (_visibleLines.Count == 0) return -1;
            int idx = _visibleLines.BinarySearch(docLineNumber);
            return idx >= 0 ? idx : -1;
        }

        public void Dispose()
        {
            if (_foldingManager != null)
                _foldingManager.FoldingsChanged -= OnFoldingsChanged;
        }
    }
}
