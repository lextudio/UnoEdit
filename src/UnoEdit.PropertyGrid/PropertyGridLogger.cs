using System;
using System.IO;

namespace UnoEdit.PropertyGrid;

/// <summary>Simple file logger for property-grid debugging. Reset on app start via Reset().</summary>
public static class PropertyGridLogger
{
    private static string _logPath = Path.Combine(Path.GetTempPath(), "unoedit_propertygrid.log");
    private static readonly object _lock = new();
    private static bool _enabled;

    public static void Reset()
    {
        if (!_enabled)
        {
            return;
        }

        lock (_lock)
        {
            try { File.WriteAllText(_logPath, $"=== PropertyGrid log started {DateTime.Now:O} ===\n"); }
            catch { }
        }
    }

    public static void Log(string message)
    {
        if (!_enabled)
        {
            return;
        }

        lock (_lock)
        {
            try { File.AppendAllText(_logPath, $"{DateTime.Now:HH:mm:ss.fff} {message}\n"); }
            catch { }
        }
        System.Diagnostics.Debug.WriteLine($"[PG] {message}");
    }
}
