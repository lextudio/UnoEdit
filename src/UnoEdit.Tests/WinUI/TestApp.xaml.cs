#if WINDOWS_APP_SDK
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;

[assembly: WinUITestTarget(typeof(UnoEdit.Tests.WinUI.TestApp))]

namespace UnoEdit.Tests.WinUI;

public sealed partial class TestApp : Microsoft.UI.Xaml.Application
{
    public TestApp()
    {
        InitializeComponent();
    }
}
#endif
