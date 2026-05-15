using System;
using System.IO;
#if !WINDOWS_APP_SDK
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Uno.Resizetizer;
using DevToolsUno;
using DevToolsUno.Diagnostics;
#else
using Microsoft.UI.Xaml.Markup;
#endif

namespace UnoEdit.Skia.Desktop;

public partial class App : Application
{
    public App()
    {
        UnoEdit.Logging.HighlightLogger.Reset();
        UnoEdit.Logging.HighlightLogger.Enabled = true;
        System.Diagnostics.Debug.WriteLine($"[UnoEdit] click-debug log: {UnoEdit.Logging.HighlightLogger.LogPath}");

        this.InitializeComponent();
        LoadUnoEditTheme();
    }

    private void LoadUnoEditTheme()
    {
        try
        {
            // Try different URI formats that work with Uno Platform library resources
            Uri? themeUri = null;

            try
            {
                // First try: using fully qualified assembly name
                themeUri = new Uri("ms-appx:///ICSharpCode.AvalonEdit/Themes/generic.xaml");
                var themeDictionary = new ResourceDictionary { Source = themeUri };
                this.Resources.MergedDictionaries.Add(themeDictionary);
                return;
            }
            catch { }

            try
            {
                // Fallback: try with UnoEdit assembly name
                themeUri = new Uri("ms-appx:///UnoEdit/Themes/generic.xaml");
                var themeDictionary = new ResourceDictionary { Source = themeUri };
                this.Resources.MergedDictionaries.Add(themeDictionary);
                return;
            }
            catch { }

            Console.WriteLine($"Warning: Could not load UnoEdit theme from library. Tried: ms-appx:///ICSharpCode.AvalonEdit/Themes/generic.xaml and ms-appx:///UnoEdit/Themes/generic.xaml");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load UnoEdit theme: {ex.Message}");
        }
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
#if DEBUG
        UnoPropertyGrid.PropertyGridLogger.Enabled = true;
        UnoPropertyGrid.PropertyGridLogger.Reset();
        UnoEdit.Logging.HighlightLogger.Enabled = true;
        UnoEdit.Logging.HighlightLogger.Reset();
#endif

#if WINDOWS_APP_SDK
        try
        {
            LoadUnoEditorResources();
            var window = new MainWindow();
            window.Activate();
        }
        catch (Exception ex)
        {
            ShowFatal(ex);
        }
#else
        var runTestsEnv = System.Environment.GetEnvironmentVariable("UNO_RUNTIME_TESTS_RUN_TESTS");
        if (!string.IsNullOrEmpty(runTestsEnv)
            && !runTestsEnv.TrimStart().StartsWith('{')
            && !bool.TryParse(runTestsEnv, out _))
        {
            System.Environment.SetEnvironmentVariable("UNO_RUNTIME_TESTS_RUN_TESTS", "true");
            runTestsEnv = "true";
        }
        bool runTests = !string.IsNullOrEmpty(runTestsEnv) && runTestsEnv != "false" && runTestsEnv != "0";

        if (runTests)
        {
            var outputPath = System.Environment.GetEnvironmentVariable("UNO_RUNTIME_TESTS_OUTPUT_PATH");
            if (string.IsNullOrEmpty(outputPath))
            {
                var dir = Path.Combine(AppContext.BaseDirectory, "test-results");
                Directory.CreateDirectory(dir);
                outputPath = Path.Combine(dir, "runtime-tests.xml");
                System.Environment.SetEnvironmentVariable("UNO_RUNTIME_TESTS_OUTPUT_PATH", outputPath);
            }

            var testWindow = new Window();
            var frame = new Frame();
            testWindow.Content = frame;
            frame.Navigate(typeof(RuntimeTestsPage), args.Arguments);
            testWindow.Activate();
        }
        else
        {
            var mainWindow = new MainWindow();
#if DEBUG
            mainWindow.UseStudio();
            var devTools = mainWindow.AttachDevTools(new DevToolsOptions
            {
                LaunchView = DevToolsViewKind.VisualTree,
                ShowAsChildWindow = false,
            });
            mainWindow.Closed += (_, _) => { devTools?.Dispose(); };
#endif
            mainWindow.SetWindowIcon();
            mainWindow.Activate();
        }
#endif
    }

#if WINDOWS_APP_SDK
    private void LoadUnoEditorResources()
    {
        string resourcePath = Path.Combine(AppContext.BaseDirectory, "UnoEditorResources.xaml");
        string resourceXaml = File.ReadAllText(resourcePath);
        var dict = (ResourceDictionary)XamlReader.Load(resourceXaml);
        this.Resources.MergedDictionaries.Add(dict);
    }

    private static void ShowFatal(Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"[FATAL] {ex}");
        Console.Error.WriteLine($"Fatal during launch: {ex}");
        var w = new Microsoft.UI.Xaml.Window();
        var tb = new Microsoft.UI.Xaml.Controls.TextBlock
        {
            Text = $"Startup error:\n{ex.GetType().Name}: {ex.Message}\n\n{ex.StackTrace}",
            TextWrapping = Microsoft.UI.Xaml.TextWrapping.WrapWholeWords,
            Margin = new Microsoft.UI.Xaml.Thickness(16),
            FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas"),
            FontSize = 12,
        };
        w.Content = new Microsoft.UI.Xaml.Controls.ScrollViewer { Content = tb };
        w.Title = "UnoEdit Sample — Startup Error";
        w.Activate();
    }
#endif

#if !WINDOWS_APP_SDK
    public static void InitializeLogging()
    {
#if DEBUG
        // Logging is disabled by default for release builds, as it incurs a significant
        // initialization cost from Microsoft.Extensions.Logging setup. If startup performance
        // is a concern for your application, keep this disabled. If you're running on the web or
        // desktop targets, you can use URL or command line parameters to enable it.
        //
        // For more performance documentation: https://platform.uno/docs/articles/Uno-UI-Performance.html

        var factory = LoggerFactory.Create(builder =>
        {
#if __WASM__
            builder.AddProvider(new global::Uno.Extensions.Logging.WebAssembly.WebAssemblyConsoleLoggerProvider());
#elif __IOS__
            builder.AddProvider(new global::Uno.Extensions.Logging.OSLogLoggerProvider());

            // Log to the Visual Studio Debug console
            builder.AddConsole();
#else
            builder.AddConsole();
#endif

            // Exclude logs below this level
            builder.SetMinimumLevel(LogLevel.Information);

            // Default filters for Uno Platform namespaces
            builder.AddFilter("Uno", LogLevel.Warning);
            builder.AddFilter("Windows", LogLevel.Warning);
            builder.AddFilter("Microsoft", LogLevel.Warning);

            // Generic Xaml events
            // builder.AddFilter("Microsoft.UI.Xaml", LogLevel.Debug );
            // builder.AddFilter("Microsoft.UI.Xaml.VisualStateGroup", LogLevel.Debug );
            // builder.AddFilter("Microsoft.UI.Xaml.StateTriggerBase", LogLevel.Debug );
            // builder.AddFilter("Microsoft.UI.Xaml.UIElement", LogLevel.Debug );
            // builder.AddFilter("Microsoft.UI.Xaml.FrameworkElement", LogLevel.Trace );

            // Layouter specific messages
            // builder.AddFilter("Microsoft.UI.Xaml.Controls", LogLevel.Debug );
            // builder.AddFilter("Microsoft.UI.Xaml.Controls.Layouter", LogLevel.Debug );
            // builder.AddFilter("Microsoft.UI.Xaml.Controls.Panel", LogLevel.Debug );

            // builder.AddFilter("Windows.Storage", LogLevel.Debug );

            // Binding related messages
            // builder.AddFilter("Microsoft.UI.Xaml.Data", LogLevel.Debug );
            // builder.AddFilter("Microsoft.UI.Xaml.Data", LogLevel.Debug );

            // Binder memory references tracking
            // builder.AddFilter("Uno.UI.DataBinding.BinderReferenceHolder", LogLevel.Debug );

            // DevServer and HotReload related
            // builder.AddFilter("Uno.UI.RemoteControl", LogLevel.Information);

            // Debug JS interop
            // builder.AddFilter("Uno.Foundation.WebAssemblyRuntime", LogLevel.Debug );
        });

        global::Uno.Extensions.LogExtensionPoint.AmbientLoggerFactory = factory;

#if HAS_UNO
        global::Uno.UI.Adapter.Microsoft.Extensions.Logging.LoggingAdapter.Initialize();
#endif
#endif
    }
#endif
}
