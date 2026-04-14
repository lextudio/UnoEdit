using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Uno.Resizetizer;
using System.IO;

using DevToolsUno;
using DevToolsUno.Diagnostics;

namespace UnoEdit.Skia.Desktop;

public partial class App : Application
{
    /// <summary>
    /// Initializes the singleton application object. This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        this.InitializeComponent();

        // Load UnoEdit theme from library assembly
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

    protected Window? MainWindow { get; private set; }
    private IDisposable? _devTools;

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        MainWindow = new Window();
#if DEBUG
        MainWindow.UseStudio();
#endif

        // Do not repeat app initialization when the Window already has content,
        // just ensure that the window is active
        if (MainWindow.Content is not Frame rootFrame)
        {
            // Create a Frame to act as the navigation context and navigate to the first page
            rootFrame = new Frame();

            // Place the frame in the current Window
            MainWindow.Content = rootFrame;

            rootFrame.NavigationFailed += OnNavigationFailed;
        }
#if DEBUG
        // AttachDevTools requires the window to have a FrameworkElement content root,
        // so it must be called after MainWindow.Content is set.
        _devTools = MainWindow.AttachDevTools(new DevToolsOptions
        {
            LaunchView = DevToolsViewKind.VisualTree,
            ShowAsChildWindow = false,
        });

        MainWindow.Closed += (_, _) =>
        {
            _devTools?.Dispose();
            _devTools = null;
        };
#endif

        if (rootFrame.Content == null)
        {
            // When UNO_RUNTIME_TESTS_RUN_TESTS is set (CI headless mode), navigate to
            // the runtime-tests host page so the engine can discover and run tests.
            var runTestsEnv = System.Environment.GetEnvironmentVariable("UNO_RUNTIME_TESTS_RUN_TESTS");
            // Normalize "1" / "yes" / any non-JSON non-bool truthy value so the engine doesn't
            // treat it as a test-name filter.  The engine only clears the value when it parses
            // as boolean "true"; any other non-JSON string becomes a filter expression.
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
                // Diagnostic: report whether test attributes are present on types
                try
                {
                    var entryAsm = typeof(App).GetTypeInfo().Assembly;
                    Console.WriteLine($"[RuntimeTests-Diag] Entry assembly: {entryAsm.GetName().Name}");
                    var allTypes = entryAsm.GetTypes();
                    var classesWithTestClass = allTypes.Count(t => t.GetCustomAttributes(false).Any(a => a.GetType().Name.Contains("TestClass")));
                    var methodsWithTestMethod = allTypes.Sum(t => t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static).Count(m => m.GetCustomAttributes(false).Any(a => a.GetType().Name.Contains("TestMethod"))));
                    Console.WriteLine($"[RuntimeTests-Diag] Types={allTypes.Length}, ClassesWithTestClass={classesWithTestClass}, MethodsWithTestMethod={methodsWithTestMethod}");

                    var loaded = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetName().Name + "@" + (a.GetName().Version?.ToString() ?? "n/a"));
                    Console.WriteLine("[RuntimeTests-Diag] LoadedAssemblies: " + string.Join(", ", loaded));

                        // Show the TestMethodAttribute type that UnitTestsControl would use
                        try
                        {
                            var tmType = typeof(Microsoft.VisualStudio.TestTools.UnitTesting.TestMethodAttribute);
                            Console.WriteLine($"[RuntimeTests-Diag] TMAttr Type: {tmType.FullName} from {tmType.Assembly.FullName}");

                            var sampleTestType = allTypes.FirstOrDefault(t => t.GetCustomAttributes(false).Any(a => a.GetType().Name.Contains("TestClass")));
                            if (sampleTestType is not null)
                            {
                                var m = sampleTestType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                                    .FirstOrDefault(m2 => m2.GetCustomAttributes(false).Any(a => a.GetType().Name.Contains("TestMethod")));
                                if (m is not null)
                                {
                                    foreach (var a in m.GetCustomAttributes(false))
                                    {
                                        Console.WriteLine($"[RuntimeTests-Diag] MethodAttr: {a.GetType().FullName} from {a.GetType().Assembly.FullName}");
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[RuntimeTests-Diag] Failed to inspect TestMethodAttribute types: {ex}");
                        }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[RuntimeTests-Diag] Failed to inspect assembly: {ex}");
                }

                // Ensure the runtime-tests engine has an output destination.
                // The engine aborts if `UNO_RUNTIME_TESTS_OUTPUT_PATH` is not set,
                // so provide a sensible default inside the app output folder.
                var outputPath = System.Environment.GetEnvironmentVariable("UNO_RUNTIME_TESTS_OUTPUT_PATH");
                if (string.IsNullOrEmpty(outputPath))
                {
                    var dir = Path.Combine(AppContext.BaseDirectory, "test-results");
                    Directory.CreateDirectory(dir);
                    outputPath = Path.Combine(dir, "runtime-tests.xml");
                    System.Environment.SetEnvironmentVariable("UNO_RUNTIME_TESTS_OUTPUT_PATH", outputPath);
                    Console.WriteLine($"[RuntimeTests] UNO_RUNTIME_TESTS_OUTPUT_PATH not set; using '{outputPath}'");
                }

                rootFrame.Navigate(typeof(RuntimeTestsPage), args.Arguments);
            }
            else
                rootFrame.Navigate(typeof(MainPage), args.Arguments);
        }

        MainWindow.SetWindowIcon();
        // Ensure the current window is active
        MainWindow.Activate();
    }

    /// <summary>
    /// Invoked when Navigation to a certain page fails
    /// </summary>
    /// <param name="sender">The Frame which failed navigation</param>
    /// <param name="e">Details about the navigation failure</param>
    void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
    {
        throw new InvalidOperationException($"Failed to load {e.SourcePageType.FullName}: {e.Exception}");
    }

    /// <summary>
    /// Configures global Uno Platform logging
    /// </summary>
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
}
