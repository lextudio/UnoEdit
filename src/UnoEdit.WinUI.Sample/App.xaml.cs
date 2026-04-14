using System;
using System.IO;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Markup;

namespace UnoEdit.WinUI.Sample;

public partial class App : Application
{
    private MainWindow? _window;

    public App()
    {
        this.InitializeComponent();
        this.UnhandledException += OnUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnDomainException;
    }

    private void LoadUnoEditorResources()
    {
        string resourcePath = Path.Combine(AppContext.BaseDirectory, "UnoEditorResources.xaml");
        string resourceXaml = File.ReadAllText(resourcePath);
        var dict = (ResourceDictionary)XamlReader.Load(resourceXaml);

        this.Resources.MergedDictionaries.Add(dict);
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
        System.Diagnostics.Debug.WriteLine($"[DOMAIN] {e.ExceptionObject}");
        Console.Error.WriteLine($"Fatal: {e.ExceptionObject}");
    }

    private static void ShowFatal(Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"[FATAL] {ex}");
        Console.Error.WriteLine($"Fatal during launch: {ex}");
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
