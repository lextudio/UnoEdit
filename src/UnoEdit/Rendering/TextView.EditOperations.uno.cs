using System.Windows.Documents;
using ICSharpCode.AvalonEdit.Document;
using Windows.ApplicationModel.DataTransfer;

namespace UnoEdit.Skia.Desktop.Controls;

public sealed partial class TextView
{
    internal static string? TranslateKeyToText(Windows.System.VirtualKey key, bool shiftPressed)
    {
        if (key >= Windows.System.VirtualKey.A && key <= Windows.System.VirtualKey.Z)
        {
            int delta = key - Windows.System.VirtualKey.A;
            char character = (char)('a' + delta);
            return shiftPressed ? char.ToUpperInvariant(character).ToString() : character.ToString();
        }

        if (key >= Windows.System.VirtualKey.Number0 && key <= Windows.System.VirtualKey.Number9)
        {
            int delta = key - Windows.System.VirtualKey.Number0;
            return shiftPressed ? ShiftedDigit(delta) : ((char)('0' + delta)).ToString();
        }

        if (key >= Windows.System.VirtualKey.NumberPad0 && key <= Windows.System.VirtualKey.NumberPad9)
        {
            int delta = key - Windows.System.VirtualKey.NumberPad0;
            return ((char)('0' + delta)).ToString();
        }

        return key switch
        {
            Windows.System.VirtualKey.Space => " ",
            Windows.System.VirtualKey.Multiply => "*",
            Windows.System.VirtualKey.Add => "+",
            Windows.System.VirtualKey.Subtract => "-",
            Windows.System.VirtualKey.Decimal => ".",
            Windows.System.VirtualKey.Divide => "/",
            (Windows.System.VirtualKey)186 => shiftPressed ? ":" : ";",
            (Windows.System.VirtualKey)187 => shiftPressed ? "+" : "=",
            (Windows.System.VirtualKey)188 => shiftPressed ? "<" : ",",
            (Windows.System.VirtualKey)189 => shiftPressed ? "_" : "-",
            (Windows.System.VirtualKey)190 => shiftPressed ? ">" : ".",
            (Windows.System.VirtualKey)191 => shiftPressed ? "?" : "/",
            (Windows.System.VirtualKey)192 => shiftPressed ? "~" : "`",
            (Windows.System.VirtualKey)219 => shiftPressed ? "{" : "[",
            (Windows.System.VirtualKey)220 => shiftPressed ? "|" : "\\",
            (Windows.System.VirtualKey)221 => shiftPressed ? "}" : "]",
            (Windows.System.VirtualKey)222 => shiftPressed ? "\"" : "'",
            _ => null
        };
    }

    internal static string ShiftedDigit(int digit) => digit switch
    {
        0 => ")",
        1 => "!",
        2 => "@",
        3 => "#",
        4 => "$",
        5 => "%",
        6 => "^",
        7 => "&",
        8 => "*",
        9 => "(",
        _ => string.Empty
    };

    internal bool InsertPrintableKey(Windows.System.VirtualKey key, bool shiftPressed)
    {
        string? text = TranslateKeyToText(key, shiftPressed);
        return text is not null && InsertText(text);
    }

    internal bool MoveHorizontal(int delta, bool extendSelection)
    {
        if (_document is null)
        {
            return false;
        }

        int targetOffset = Math.Clamp(CurrentOffset + delta, 0, _document.TextLength);
        UpdateCaretAndSelection(targetOffset, extendSelection);
        return true;
    }

    internal bool MoveVertical(int delta, bool extendSelection)
    {
        if (_document is null)
        {
            return false;
        }

        TextLocation location = _document.GetLocation(CurrentOffset);
        int currentVisualRow = GetVisualRow(location.Line);
        if (currentVisualRow < 0)
        {
            currentVisualRow = 0;
        }

        int targetVisualRow = Math.Clamp(currentVisualRow + delta, 0, _visibleDocLines.Count - 1);
        int targetLineNumber = _visibleDocLines.Count > 0 ? _visibleDocLines[targetVisualRow] : location.Line;
        DocumentLine targetLine = _document.GetLineByNumber(targetLineNumber);
        int targetColumn = ClampColumn(targetLine, _desiredColumn);
        int targetOffset = _document.GetOffset(targetLineNumber, targetColumn);
        UpdateCaretAndSelection(targetOffset, extendSelection);
        return true;
    }

    internal bool MoveToLineBoundary(bool moveToStart, bool extendSelection)
    {
        if (_document is null)
        {
            return false;
        }

        TextLocation location = _document.GetLocation(CurrentOffset);
        DocumentLine line = _document.GetLineByNumber(location.Line);
        int targetColumn = moveToStart ? 1 : line.Length + 1;
        int targetOffset = _document.GetOffset(location.Line, targetColumn);
        UpdateCaretAndSelection(targetOffset, extendSelection);
        return true;
    }

    internal bool MoveToDocumentBoundary(bool moveToStart, bool extendSelection)
    {
        if (_document is null)
        {
            return false;
        }

        int targetOffset = moveToStart ? 0 : _document.TextLength;
        UpdateCaretAndSelection(targetOffset, extendSelection);
        return true;
    }

    internal bool MovePageVertical(int direction, bool extendSelection)
    {
        if (_document is null)
        {
            return false;
        }

        double viewportHeight = TextScrollViewer.ViewportHeight > 0 ? TextScrollViewer.ViewportHeight : ActualHeight;
        int pageLines = Math.Max(1, (int)(viewportHeight / LineHeight) - 1);
        TextLocation location = _document.GetLocation(CurrentOffset);
        int currentVisualRow = GetVisualRow(location.Line);
        if (currentVisualRow < 0)
        {
            currentVisualRow = 0;
        }

        int targetVisualRow = Math.Clamp(currentVisualRow + direction * pageLines, 0, _visibleDocLines.Count - 1);
        int targetLineNumber = _visibleDocLines.Count > 0 ? _visibleDocLines[targetVisualRow] : location.Line;
        DocumentLine targetLine = _document.GetLineByNumber(targetLineNumber);
        int targetColumn = ClampColumn(targetLine, _desiredColumn);
        int targetOffset = _document.GetOffset(targetLineNumber, targetColumn);
        UpdateCaretAndSelection(targetOffset, extendSelection);
        return true;
    }

    internal bool MoveWordBoundary(bool backward, bool extendSelection)
    {
        if (_document is null)
        {
            return false;
        }

        LogicalDirection direction = backward ? LogicalDirection.Backward : LogicalDirection.Forward;
        int nextOffset = TextUtilities.GetNextCaretPosition(_document, CurrentOffset, direction, CaretPositioningMode.WordStartOrSymbol);
        if (nextOffset < 0)
        {
            nextOffset = backward ? 0 : _document.TextLength;
        }

        UpdateCaretAndSelection(nextOffset, extendSelection);
        return true;
    }

    internal bool DeleteWord(bool backward)
    {
        if (_document is null)
        {
            return false;
        }

        if (HasSelection())
        {
            DeleteSelectedText();
            return true;
        }

        LogicalDirection direction = backward ? LogicalDirection.Backward : LogicalDirection.Forward;
        int boundary = TextUtilities.GetNextCaretPosition(_document, CurrentOffset, direction, CaretPositioningMode.WordStartOrSymbol);
        if (boundary < 0)
        {
            boundary = backward ? 0 : _document.TextLength;
        }

        int startOffset = backward ? boundary : CurrentOffset;
        int length = Math.Abs(boundary - CurrentOffset);
        if (length == 0)
        {
            return false;
        }

        BatchRefresh(() =>
        {
            using (_document.RunUpdate())
            {
                _document.Remove(startOffset, length);
            }

            CollapseSelection(startOffset);
        });
        return true;
    }

    internal bool Backspace()
    {
        if (_document is null)
        {
            return false;
        }

        if (HasSelection())
        {
            DeleteSelectedText();
            return true;
        }

        if (CurrentOffset == 0)
        {
            return false;
        }

        int newOffset = CurrentOffset - 1;
        BatchRefresh(() =>
        {
            using (_document.RunUpdate())
            {
                _document.Remove(newOffset, 1);
            }

            CollapseSelection(newOffset);
        });
        return true;
    }

    internal bool Delete()
    {
        if (_document is null)
        {
            return false;
        }

        if (HasSelection())
        {
            DeleteSelectedText();
            return true;
        }

        if (CurrentOffset >= _document.TextLength)
        {
            return false;
        }

        int offset = CurrentOffset;
        BatchRefresh(() =>
        {
            using (_document.RunUpdate())
            {
                _document.Remove(offset, 1);
            }

            CollapseSelection(offset);
        });
        return true;
    }

    internal bool InsertText(string text)
    {
        if (_document is null)
        {
            return false;
        }

        int insertionOffset = CurrentOffset;
        BatchRefresh(() =>
        {
            using (_document.RunUpdate())
            {
                if (HasSelection())
                {
                    insertionOffset = DeleteSelectedTextInternal();
                }

                _document.Insert(insertionOffset, text);
            }

            CollapseSelection(insertionOffset + text.Length);
        });
        return true;
    }

    internal bool SelectAll()
    {
        if (_document is null)
        {
            return false;
        }

        BatchRefresh(() =>
        {
            _selectionAnchorOffset = 0;
            CurrentOffset = _document.TextLength;
            SelectionStartOffset = 0;
            SelectionEndOffset = _document.TextLength;
        });
        return true;
    }

    internal bool CopySelection()
    {
        if (_document is null || !HasSelection())
        {
            return false;
        }

        string selectedText = GetSelectedText();
        if (selectedText.Length == 0)
        {
            return false;
        }

        var package = new DataPackage();
        package.SetText(selectedText);
        Clipboard.SetContent(package);
        return true;
    }

    internal bool Undo()
    {
        if (_document is null || !_document.UndoStack.CanUndo)
        {
            return false;
        }

        BatchRefresh(() =>
        {
            _document.UndoStack.Undo();
            CollapseSelection(Math.Min(CurrentOffset, _document.TextLength));
        });
        return true;
    }

    internal bool Redo()
    {
        if (_document is null || !_document.UndoStack.CanRedo)
        {
            return false;
        }

        BatchRefresh(() =>
        {
            _document.UndoStack.Redo();
            CollapseSelection(Math.Min(CurrentOffset, _document.TextLength));
        });
        return true;
    }

    internal bool CutSelection()
    {
        if (!CopySelection())
        {
            return false;
        }

        DeleteSelectedText();
        return true;
    }

    internal async Task<bool> PasteAsync()
    {
        if (_document is null)
        {
            return false;
        }

        DataPackageView package = Clipboard.GetContent();
        if (!package.Contains(StandardDataFormats.Text))
        {
            return false;
        }

        string text = await package.GetTextAsync();
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        return InsertText(text);
    }

    internal void UpdateCaretAndSelection(int targetOffset, bool extendSelection)
    {
        BatchRefresh(() =>
        {
            CurrentOffset = targetOffset;

            if (extendSelection)
            {
                SelectionStartOffset = Math.Min(_selectionAnchorOffset, targetOffset);
                SelectionEndOffset = Math.Max(_selectionAnchorOffset, targetOffset);
            }
            else
            {
                _selectionAnchorOffset = targetOffset;
                SelectionStartOffset = targetOffset;
                SelectionEndOffset = targetOffset;
            }

            _desiredColumn = _document?.GetLocation(CurrentOffset).Column ?? 1;
        });
    }

    internal bool HasSelection()
    {
        return SelectionStartOffset != SelectionEndOffset;
    }

    internal string GetSelectedText()
    {
        if (_document is null || !HasSelection())
        {
            return string.Empty;
        }

        int startOffset = Math.Min(SelectionStartOffset, SelectionEndOffset);
        int endOffset = Math.Max(SelectionStartOffset, SelectionEndOffset);
        return _document.GetText(startOffset, endOffset - startOffset);
    }

    internal void DeleteSelectedText()
    {
        if (_document is null || !HasSelection())
        {
            return;
        }

        int startOffset;
        BatchRefresh(() =>
        {
            using (_document.RunUpdate())
            {
                startOffset = DeleteSelectedTextInternal();
            }

            CollapseSelection(startOffset);
        });
    }

    internal int DeleteSelectedTextInternal()
    {
        if (_document is null)
        {
            return 0;
        }

        int startOffset = Math.Min(SelectionStartOffset, SelectionEndOffset);
        int endOffset = Math.Max(SelectionStartOffset, SelectionEndOffset);
        if (endOffset > startOffset)
        {
            _document.Remove(startOffset, endOffset - startOffset);
        }

        return startOffset;
    }

    internal void CollapseSelection(int offset)
    {
        BatchRefresh(() =>
        {
            _selectionAnchorOffset = offset;
            CurrentOffset = offset;
            SelectionStartOffset = offset;
            SelectionEndOffset = offset;
        });
    }
}
