using System;
using System.Runtime.InteropServices;

namespace LeXtudio.UI.Text.Core
{
    /// <summary>
    /// Factory for creating <see cref="CoreTextEditContext"/> instances.
    /// Automatically selects the correct platform adapter for the current OS.
    /// </summary>
    public sealed class CoreTextServicesManager
    {
        private static CoreTextServicesManager? s_instance;

        private CoreTextServicesManager() { }

        /// <summary>
        /// Gets the singleton <see cref="CoreTextServicesManager"/> for the current process.
        /// </summary>
        public static CoreTextServicesManager GetForCurrentView()
        {
            return s_instance ??= new CoreTextServicesManager();
        }

        /// <summary>
        /// Creates a new <see cref="CoreTextEditContext"/> backed by the platform-appropriate
        /// text input adapter. Consumers subscribe to the returned context's events without
        /// needing to know which platform adapter is in use.
        /// </summary>
        public CoreTextEditContext CreateEditContext()
        {
            IPlatformTextInputAdapter adapter = CreatePlatformAdapter();
            return new CoreTextEditContext(adapter);
        }

        private static IPlatformTextInputAdapter CreatePlatformAdapter()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return new Win32TextInputAdapter();
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return new MacOSTextInputAdapter();
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return new LinuxIbusTextInputAdapter();
            }

            return new NullTextInputAdapter();
        }
    }
}
