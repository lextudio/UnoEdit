using System;
using Microsoft.UI.Xaml;

namespace UnoEdit.WinUI.Sample;

public partial class App : Application
{
    private MainWindow _window;

    public App()
    {
        this.InitializeComponent();
        this.UnhandledException += OnUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnDomainException;
    }

    private void LoadUnoEditorResources()
    {
        var dict = new Microsoft.UI.Xaml.ResourceDictionary();

        // Scalar resources
        dict["UnoTextEditorBorderThickness"] = new Microsoft.UI.Xaml.Thickness(1);
        dict["UnoTextEditorCornerRadius"] = new Microsoft.UI.Xaml.CornerRadius(10);
        dict["UnoSearchPanelCornerRadius"] = new Microsoft.UI.Xaml.CornerRadius(8);
        dict["UnoSearchPanelMargin"] = new Microsoft.UI.Xaml.Thickness(12, 12, 12, 0);
        dict["UnoSearchPanelPadding"] = new Microsoft.UI.Xaml.Thickness(10);
        dict["UnoEditorLineHeight"] = 22.0;
        dict["UnoEditorTextFontSize"] = 13.0;
        dict["UnoEditorGlyphFontSize"] = 10.0;
        dict["UnoEditorOverlayHeight"] = 16.0;
        dict["UnoEditorCaretWidth"] = 2.0;

        // Dark theme
        var dark = new Microsoft.UI.Xaml.ResourceDictionary();
        dark["UnoEditorBackgroundBrush"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(0xFF, 0x0F, 0x17, 0x2A));
        dark["UnoEditorBorderBrush"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(0xFF, 0x33, 0x41, 0x55));
        dark["UnoEditorTitleBarBackgroundBrush"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(0xFF, 0x0F, 0x17, 0x2A));
        dark["UnoEditorTitleBarForegroundBrush"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(0xFF, 0xE5, 0xE7, 0xEB));
        dark["UnoEditorSummaryForegroundBrush"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(0xFF, 0x94, 0xA3, 0xB8));
        dark["UnoEditorGutterForegroundBrush"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(0xFF, 0x64, 0x74, 0x8B));
        dark["UnoSearchPanelBackgroundBrush"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(0xFF, 0x17, 0x20, 0x33));
        dark["UnoSearchPanelBorderBrush"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(0xFF, 0x33, 0x41, 0x55));
        dark["UnoSearchPanelResultsBrush"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(0xFF, 0x94, 0xA3, 0xB8));
        dict.ThemeDictionaries["Dark"] = dark;

        // Light theme
        var light = new Microsoft.UI.Xaml.ResourceDictionary();
        light["UnoEditorBackgroundBrush"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(0xFF, 0xFF, 0xFF, 0xFF));
        light["UnoEditorBorderBrush"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(0xFF, 0xD1, 0xD5, 0xDB));
        light["UnoEditorTitleBarBackgroundBrush"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(0xFF, 0xF9, 0xFA, 0xFB));
        light["UnoEditorTitleBarForegroundBrush"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(0xFF, 0x11, 0x18, 0x27));
        light["UnoEditorSummaryForegroundBrush"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(0xFF, 0x6E, 0x76, 0x81));
        light["UnoEditorGutterForegroundBrush"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(0xFF, 0x6E, 0x76, 0x81));
        light["UnoSearchPanelBackgroundBrush"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(0xFF, 0xF3, 0xF4, 0xF6));
        light["UnoSearchPanelBorderBrush"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(0xFF, 0xD1, 0xD5, 0xDB));
        light["UnoSearchPanelResultsBrush"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(0xFF, 0x6E, 0x76, 0x81));
        dict.ThemeDictionaries["Light"] = light;

        this.Resources.MergedDictionaries.Add(dict);

        // Styles (must be added after theme resources so ThemeResource refs resolve)
        var btnStyle = new Microsoft.UI.Xaml.Style(typeof(Microsoft.UI.Xaml.Controls.Button));
        btnStyle.Setters.Add(new Microsoft.UI.Xaml.Setter(Microsoft.UI.Xaml.Controls.Button.MinWidthProperty, 44.0));
        btnStyle.Setters.Add(new Microsoft.UI.Xaml.Setter(Microsoft.UI.Xaml.Controls.Button.MinHeightProperty, 32.0));
        this.Resources["UnoSearchActionButtonStyle"] = btnStyle;

        var cbStyle = new Microsoft.UI.Xaml.Style(typeof(Microsoft.UI.Xaml.Controls.CheckBox));
        cbStyle.Setters.Add(new Microsoft.UI.Xaml.Setter(Microsoft.UI.Xaml.FrameworkElement.VerticalAlignmentProperty, Microsoft.UI.Xaml.VerticalAlignment.Center));
        cbStyle.Setters.Add(new Microsoft.UI.Xaml.Setter(Microsoft.UI.Xaml.FrameworkElement.MarginProperty, new Microsoft.UI.Xaml.Thickness(0, 2, 0, 0)));
        cbStyle.Setters.Add(new Microsoft.UI.Xaml.Setter(Microsoft.UI.Xaml.FrameworkElement.MinHeightProperty, 0.0));
        cbStyle.Setters.Add(new Microsoft.UI.Xaml.Setter(Microsoft.UI.Xaml.FrameworkElement.MinWidthProperty, 0.0));
        cbStyle.Setters.Add(new Microsoft.UI.Xaml.Setter(Microsoft.UI.Xaml.Controls.Control.PaddingProperty, new Microsoft.UI.Xaml.Thickness(6, 0, 0, 0)));
        this.Resources["UnoSearchOptionCheckBoxStyle"] = cbStyle;
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
            LoadUnoEditorResources();
            _window = new MainWindow();
            _window.Activate();
        }
        catch (Exception ex)
        {
            ShowFatal(ex);
        }
    }

    private static void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        System.Diagnostics.Debug.WriteLine($"[UNHANDLED] {e.Exception}");
        Console.Error.WriteLine($"Unhandled: {e.Exception}");
        e.Handled = true;
    }

    private static void OnDomainException(object sender, System.UnhandledExceptionEventArgs e)
    {
        var msg = $"[DOMAIN] {e.ExceptionObject}";
        System.Diagnostics.Debug.WriteLine(msg);
        Console.Error.WriteLine(msg);
        System.IO.File.AppendAllText("crash.log", msg + "\n");
    }

    private static void ShowFatal(Exception ex)
    {
        var msg = $"Fatal during launch: {ex}";
        System.Diagnostics.Debug.WriteLine(msg);
        Console.Error.WriteLine(msg);
        System.IO.File.AppendAllText("crash.log", msg + "\n");
        // Show a simple WinUI dialog — create a minimal window just for the message.
        var w = new Microsoft.UI.Xaml.Window();
        var tb = new Microsoft.UI.Xaml.Controls.TextBlock
        {
            Text = $"Startup error:\n{ex.GetType().Name}: {ex.Message}\n\n{ex.StackTrace}",
            TextWrapping = Microsoft.UI.Xaml.TextWrapping.WrapWholeWords,
            Margin = new Microsoft.UI.Xaml.Thickness(16),
            FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas"),
            FontSize = 12,
        };
        var scroll = new Microsoft.UI.Xaml.Controls.ScrollViewer { Content = tb };
        w.Content = scroll;
        w.Title = "UnoEdit WinUI Sample — Startup Error";
        w.Activate();
    }
}
