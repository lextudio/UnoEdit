using System;
using System.IO;
using System.Threading;

namespace UnoEdit.Logging
{
    public static class PlatformImeLogger
    {
        private static readonly string s_logPath = Path.Combine(Path.GetTempPath(), "unoedit_ime.log");
        private static readonly object s_lock = new object();

#if DEBUG
        public static bool Enabled => true;
#else
        public static bool Enabled => false;
#endif
        public static string LogPath => s_logPath;

        public static void Log(string message)
        {
#if DEBUG
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
#endif
        }
    }
}
