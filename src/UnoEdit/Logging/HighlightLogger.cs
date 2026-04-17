using System;
using System.IO;
using System.Threading;

namespace UnoEdit.Logging
{
    public static class HighlightLogger
    {
        private static readonly bool s_enabled =
            string.Equals(Environment.GetEnvironmentVariable("UNOEDIT_DEBUG_HIGHLIGHT"), "1", StringComparison.Ordinal);

        private static readonly string s_logPath = Path.Combine(Path.GetTempPath(), "unoedit_highlight.log");
        private static readonly object s_lock = new object();

        public static bool Enabled => s_enabled;
        public static string LogPath => s_logPath;

        public static void Log(string category, string message)
        {
            if (!s_enabled)
            {
                return;
            }

            try
            {
                string ts = DateTime.UtcNow.ToString("o");
                int tid = Thread.CurrentThread.ManagedThreadId;
                string line = $"[{ts}] [T{tid}] [{category}] {message}";
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
