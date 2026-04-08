namespace UnoEdit.Skia.Desktop.Controls;

public sealed class TextLineViewModel
{
    public TextLineViewModel(
        int number,
        string text,
        double caretOpacity,
        double highlightOpacity,
        Thickness caretMargin,
        Thickness selectionMargin,
        double selectionWidth,
        double selectionOpacity)
    {
        Number = number.ToString();
        Text = text;
        CaretOpacity = caretOpacity;
        HighlightOpacity = highlightOpacity;
        CaretMargin = caretMargin;
        SelectionMargin = selectionMargin;
        SelectionWidth = selectionWidth;
        SelectionOpacity = selectionOpacity;
    }

    public string Number { get; }

    public string Text { get; }

    public double CaretOpacity { get; }

    public double HighlightOpacity { get; }

    public Thickness CaretMargin { get; }

    public Thickness SelectionMargin { get; }

    public double SelectionWidth { get; }

    public double SelectionOpacity { get; }
}
