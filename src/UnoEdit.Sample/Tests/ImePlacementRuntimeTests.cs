using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests;
using ICSharpCode.AvalonEdit.Document;
using UnoEdit.Skia.Desktop.Controls;

namespace UnoEdit.Skia.Desktop.Tests;

[TestClass]
[RunsOnUIThread]
public class ImePlacementRuntimeTests
{
    [TestMethod]
    public async Task IME_FirstRectMatchesManagedExpectation()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            Assert.Inconclusive("Test only valid on macOS.");
        }

        // Enable experimental bridge for the test
        Environment.SetEnvironmentVariable("UNOEDIT_ENABLE_EXPERIMENTAL_MACOS_IME", "1");
        Environment.SetEnvironmentVariable("UNOEDIT_DEBUG_MACOS_IME", "1");

        var doc = new TextDocument("hello world");
        var editor = new TextEditor { Document = doc };

        UnitTestsUIContentHelper.Content = editor;
        await UnitTestsUIContentHelper.WaitForIdle();

        editor.CurrentOffset = 5;
        await UnitTestsUIContentHelper.WaitForIdle();

        var textView = editor.TextArea.TextView;
        Assert.IsNotNull(textView);

        // Invoke private CalculatePlatformInputCaretRect
        var tvType = textView.GetType();
        var calcMethod = tvType.GetMethod("CalculatePlatformInputCaretRect", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(calcMethod, "CalculatePlatformInputCaretRect not found");
        var rawRect = (Windows.Foundation.Rect)calcMethod.Invoke(textView, null)!;

        // Access private native bridge instance
        var bridgeField = tvType.GetField("_macOSNativeImeBridge", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(bridgeField, "_macOSNativeImeBridge field not found");
        var bridgeObj = bridgeField.GetValue(textView);
        Assert.IsNotNull(bridgeObj, "Native bridge not created");

        var bridgeType = bridgeObj.GetType();
        var handleField = bridgeType.GetField("_bridgeHandle", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(handleField, "_bridgeHandle field not found on bridge");
        var handleVal = handleField.GetValue(bridgeObj);
        Assert.IsNotNull(handleVal, "Bridge handle is null");
        IntPtr bridgeHandle;
        if (handleVal is IntPtr ip)
        {
            bridgeHandle = ip;
        }
        else
        {
            bridgeHandle = new IntPtr(Convert.ToInt64(handleVal));
        }

        // Query native backing scale
        double backing = unoedit_ime_get_backing_scale(bridgeHandle);

        // Compute managed-aligned rectangle (same logic as UpdateCaretRect)
        double px = rawRect.X * backing;
        double py = rawRect.Y * backing;
        double pw = Math.Max(1.0, rawRect.Width * backing);
        double ph = Math.Max(1.0, rawRect.Height * backing);

        double rpx = Math.Round(px);
        double rpy = Math.Round(py);
        double rpw = Math.Max(1.0, Math.Round(pw));
        double rph = Math.Max(1.0, Math.Round(ph));

        var adjustedX = rpx / backing;
        var adjustedY = rpy / backing;
        var adjustedW = rpw / backing;
        var adjustedH = rph / backing;

        // Compute expected final first-rect using native helper for the adjusted rect
        unoedit_ime_compute_first_rect_from_rect(bridgeHandle, adjustedX, adjustedY, adjustedW, adjustedH, out double ex, out double ey, out double ew, out double eh);

        // Query native current first-rect
        unoedit_ime_get_first_rect(bridgeHandle, out double nx, out double ny, out double nw, out double nh);

        const double tol = 0.5;
        Assert.AreEqual(ex, nx, tol, "FirstRect X mismatch");
        Assert.AreEqual(ey, ny, tol, "FirstRect Y mismatch");
        Assert.AreEqual(ew, nw, tol, "FirstRect W mismatch");
        Assert.AreEqual(eh, nh, tol, "FirstRect H mismatch");
    }

    [DllImport("libUnoEditMacInput.dylib", CallingConvention = CallingConvention.Cdecl)]
    private static extern void unoedit_ime_get_first_rect(IntPtr bridgeHandle, out double x, out double y, out double w, out double h);

    [DllImport("libUnoEditMacInput.dylib", CallingConvention = CallingConvention.Cdecl)]
    private static extern void unoedit_ime_compute_first_rect_from_rect(IntPtr bridgeHandle, double x, double y, double width, double height, out double outX, out double outY, out double outW, out double outH);

    [DllImport("libUnoEditMacInput.dylib", CallingConvention = CallingConvention.Cdecl)]
    private static extern double unoedit_ime_get_backing_scale(IntPtr bridgeHandle);
}
