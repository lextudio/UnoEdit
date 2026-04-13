using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace LeXtudio.UI.Text.Core
{
    /// <summary>
    /// macOS text input adapter that bridges AppKit IME callbacks through
    /// libUnoEditMacInput.dylib when available.
    /// </summary>
    internal sealed class MacOSTextInputAdapter : IPlatformTextInputAdapter
    {
        private static readonly bool s_debug =
            string.Equals(Environment.GetEnvironmentVariable("LEXTUDIO_DEBUG_MACOS_IME"), "1", StringComparison.Ordinal);

        private static long s_nextEventId;

        private readonly InsertTextDelegate _insertTextDelegate;
        private readonly CommandDelegate _commandDelegate;

        private CoreTextEditContext? _context;
        private GCHandle _selfHandle;
        private nint _bridgeHandle;
        private bool _disposed;

        public MacOSTextInputAdapter()
        {
            _insertTextDelegate = OnInsertText;
            _commandDelegate = OnCommand;
        }

        /// <inheritdoc />
        public bool Attach(nint windowHandle, CoreTextEditContext context)
        {
            if (windowHandle == nint.Zero)
            {
                Log("Attach failed: window handle is zero.");
                return false;
            }

            _context = context;
            _selfHandle = GCHandle.Alloc(this);

            try
            {
                nint managedContext = GCHandle.ToIntPtr(_selfHandle);
                _bridgeHandle = NativeMethods.unoedit_ime_create(
                    windowHandle,
                    managedContext,
                    Marshal.GetFunctionPointerForDelegate(_insertTextDelegate),
                    Marshal.GetFunctionPointerForDelegate(_commandDelegate));

                if (_bridgeHandle == nint.Zero)
                {
                    Log("Attach failed: unoedit_ime_create returned null.");
                    _selfHandle.Free();
                    return false;
                }

                Log($"Attach succeeded: bridge=0x{_bridgeHandle:X}");
                return true;
            }
            catch (DllNotFoundException)
            {
                Log("Attach failed: libUnoEditMacInput.dylib was not found.");
            }
            catch (EntryPointNotFoundException ex)
            {
                Log($"Attach failed: missing native entry point: {ex.Message}");
            }
            catch (Exception ex)
            {
                Log($"Attach failed: {ex.Message}");
            }

            if (_selfHandle.IsAllocated)
            {
                _selfHandle.Free();
            }

            return false;
        }

        /// <inheritdoc />
        public void NotifyCaretRectChanged(double x, double y, double width, double height)
        {
            if (_bridgeHandle == nint.Zero)
            {
                return;
            }

            try
            {
                ulong eventId = (ulong)Interlocked.Increment(ref s_nextEventId);
                NativeMethods.unoedit_ime_update_caret_rect(_bridgeHandle, eventId, x, y, width, height);
            }
            catch (Exception ex)
            {
                Log($"NotifyCaretRectChanged failed: {ex.Message}");
            }
        }

        /// <inheritdoc />
        public void NotifyFocusEnter()
        {
            if (_bridgeHandle == nint.Zero)
            {
                return;
            }

            try
            {
                NativeMethods.unoedit_ime_focus(_bridgeHandle, true);
            }
            catch (Exception ex)
            {
                Log($"NotifyFocusEnter failed: {ex.Message}");
            }
        }

        /// <inheritdoc />
        public void NotifyFocusLeave()
        {
            if (_bridgeHandle == nint.Zero)
            {
                return;
            }

            try
            {
                NativeMethods.unoedit_ime_focus(_bridgeHandle, false);
            }
            catch (Exception ex)
            {
                Log($"NotifyFocusLeave failed: {ex.Message}");
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            if (_bridgeHandle != nint.Zero)
            {
                try
                {
                    NativeMethods.unoedit_ime_destroy(_bridgeHandle);
                }
                catch (Exception ex)
                {
                    Log($"Dispose failed: {ex.Message}");
                }

                _bridgeHandle = nint.Zero;
            }

            if (_selfHandle.IsAllocated)
            {
                _selfHandle.Free();
            }

            _context = null;
        }

        private static void OnInsertText(nint context, nint utf8Text)
        {
            if (GCHandle.FromIntPtr(context).Target is not MacOSTextInputAdapter adapter)
            {
                return;
            }

            string? text = Marshal.PtrToStringUTF8(utf8Text);
            if (string.IsNullOrEmpty(text) || adapter._context == null)
            {
                return;
            }

            // Committed text from AppKit IME.
            var request = new CoreTextTextRequest(text);
            adapter._context.RaiseTextRequested(new CoreTextTextRequestedEventArgs(request));
        }

        private static void OnCommand(nint context, nint utf8Command)
        {
            if (GCHandle.FromIntPtr(context).Target is not MacOSTextInputAdapter adapter)
            {
                return;
            }

            string? command = Marshal.PtrToStringUTF8(utf8Command);
            if (string.IsNullOrEmpty(command) || adapter._context == null)
            {
                return;
            }

            // Forward the command to the consumer via CommandReceived.
            var args = new CoreTextCommandReceivedEventArgs(command);
            adapter._context.RaiseCommandReceived(args);

            // If the consumer didn't handle it, apply default composition logic.
            if (!args.Handled)
            {
                if (string.Equals(command, "insertNewline:", StringComparison.Ordinal)
                    || string.Equals(command, "cancelOperation:", StringComparison.Ordinal))
                {
                    adapter._context.RaiseCompositionCompleted();
                }
            }
        }

        private static void Log(string message)
        {
            if (s_debug)
            {
                Console.Error.WriteLine($"[LeXtudio macOS IME] {message}");
            }
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void InsertTextDelegate(nint context, nint utf8Text);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void CommandDelegate(nint context, nint utf8Command);

        private static class NativeMethods
        {
            [DllImport("libUnoEditMacInput.dylib", CallingConvention = CallingConvention.Cdecl)]
            internal static extern nint unoedit_ime_create(
                nint windowHandle,
                nint managedContext,
                nint insertTextCallback,
                nint commandCallback);

            [DllImport("libUnoEditMacInput.dylib", CallingConvention = CallingConvention.Cdecl)]
            internal static extern void unoedit_ime_destroy(nint bridgeHandle);

            [DllImport("libUnoEditMacInput.dylib", CallingConvention = CallingConvention.Cdecl)]
            internal static extern void unoedit_ime_focus(nint bridgeHandle, [MarshalAs(UnmanagedType.I1)] bool focus);

            [DllImport("libUnoEditMacInput.dylib", CallingConvention = CallingConvention.Cdecl)]
            internal static extern void unoedit_ime_update_caret_rect(
                nint bridgeHandle,
                ulong eventId,
                double x,
                double y,
                double width,
                double height);
        }
    }
}
