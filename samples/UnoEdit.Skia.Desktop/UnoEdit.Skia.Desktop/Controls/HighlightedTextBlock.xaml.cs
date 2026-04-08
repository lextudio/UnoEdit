using Microsoft.UI.Xaml.Documents;

namespace UnoEdit.Skia.Desktop.Controls;

public sealed partial class HighlightedTextBlock : UserControl
{
    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(
            nameof(ViewModel),
            typeof(TextLineViewModel),
            typeof(HighlightedTextBlock),
            new PropertyMetadata(null, OnViewModelChanged));

    public HighlightedTextBlock()
    {
        this.InitializeComponent();
    }

    public TextLineViewModel? ViewModel
    {
        get => (TextLineViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    private static void OnViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((HighlightedTextBlock)d).RefreshInlines((TextLineViewModel?)e.NewValue);
    }

    private void RefreshInlines(TextLineViewModel? vm)
    {
        PART_Text.Inlines.Clear();

        if (vm is null)
            return;

        var runs = vm.Runs;
        if (runs is null || runs.Count == 0)
            return;

        // Single run with default color — use simple Text binding for performance
        if (runs.Count == 1)
        {
            PART_Text.Inlines.Add(new Run
            {
                Text = runs[0].Text,
                Foreground = new SolidColorBrush(runs[0].Foreground)
            });
            return;
        }

        foreach (var textRun in runs)
        {
            PART_Text.Inlines.Add(new Run
            {
                Text = textRun.Text,
                Foreground = new SolidColorBrush(textRun.Foreground)
            });
        }
    }
}
