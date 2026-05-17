#if WINDOWS_APP_SDK
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using Microsoft.UI.Xaml.Controls;
using MSTestAssert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

namespace UnoEdit.Tests.WinUI;

[TestClass]
public sealed class WinUIThreadTests
{
    [UITestMethod]
    public void CanCreateXamlControlOnUiThread()
    {
        var grid = new Grid();

        MSTestAssert.AreEqual(0, grid.MinWidth);
    }
}
#endif
