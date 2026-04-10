using Uno.UI.Hosting;

namespace UnoEdit.Skia.Desktop;

internal class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
#if DEBUG
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("UNOEDIT_ENABLE_EXPERIMENTAL_MACOS_IME")))
            Environment.SetEnvironmentVariable("UNOEDIT_ENABLE_EXPERIMENTAL_MACOS_IME", "1");
#endif
        App.InitializeLogging();

        var host = UnoPlatformHostBuilder.Create()
            .App(() => new App())
            .UseX11()
            .UseLinuxFrameBuffer()
            .UseMacOS()
            .UseWin32()
            .Build();

        host.Run();
    }
}
