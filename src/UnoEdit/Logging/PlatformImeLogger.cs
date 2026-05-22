using System;
using System.IO;
using System.Threading;

namespace UnoEdit.Logging
{
    public static class PlatformImeLogger
    {
        private static readonly string s_logPath = Path.Combine(Path.GetTempPath(), "unoedit_ime.log");
        private static readonly object s_lock = new object();
        private static bool s_enabled;

        public static bool Enabled
        {
            get => s_enabled;
            set
            {
                s_enabled = value;
                Environment.SetEnvironmentVariable("UNOEDIT_DEBUG_IME", value ? "1" : null);
            }
        }

        public static string LogPath => s_logPath;

        public static void Enable() => Enabled = true;

        public static void Disable() => Enabled = false;

        public static void Reset()
        {
            lock (s_lock)
            {
                if (File.Exists(s_logPath))
                {
                    File.Delete(s_logPath);
                }
            }
        }

        public static void Log(string message)
        {
            if (!s_enabled)
            {
                return;
            }

            try
            {
                string ts = DateTime.UtcNow.ToString("o");
                int tid = Thread.CurrentThread.ManagedThreadId;
                string line = $"[{ts}] [T{tid}] {message}";
                lock (s_lock)
                {
                    File.AppendAllText(s_logPath, line + Environment.NewLine);
                }
            }
            catch
            {
                // best-effort logging
            }
        }
    }
}
