#if WINDOWS_APP_SDK
namespace UnoEdit.Tests.WinUI;

public static class Program
{
    [System.STAThread]
    public static void Main(string[] args)
    {
        WinRT.ComWrappersSupport.InitializeComWrappers();
        Microsoft.UI.Xaml.Application.Start(_ => { var app = new TestApp(); });
    }
}
#endif
