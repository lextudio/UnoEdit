using Microsoft.UI.Xaml;

namespace UnoEdit.Skia.Desktop.Controls;

/// <summary>
/// Abstraction used by <see cref="SearchPanel"/> to interact with its host text editor.
/// Implemented by both the Uno (<c>UnoEdit.Skia.Desktop.Controls.TextEditor</c>) and
/// WinUI (<c>UnoEdit.WinUI.Controls.TextEditor</c>) editors.
/// </summary>
public interface ISearchPanelHost
{
    /// <summary>Gets the current caret offset inside the document.</summary>
    int CurrentOffset { get; }

    /// <summary>Returns the <see cref="SearchPanel"/> owned by this editor.</summary>
    SearchPanel SearchPanel { get; }

    /// <summary>Moves the caret to <paramref name="offset"/> and scrolls it into view.</summary>
    void ScrollToOffset(int offset);

    /// <summary>Selects the range [<paramref name="offset"/>, <paramref name="endOffset"/>).</summary>
    void SetSelection(int offset, int endOffset);

    /// <summary>Requests keyboard focus on this control.</summary>
    bool Focus(FocusState value);
}
