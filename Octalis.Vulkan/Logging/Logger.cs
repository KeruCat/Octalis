using System.Diagnostics;

namespace Octalis.Vulkan.Logging;

internal static class Logger
{
    private static readonly object _lock = new();

#if DEBUG
    internal static LogLevel MinimumLevel { get; set; }
        = LogLevel.Trace;
#else
    internal static LogLevel MinimumLevel { get; set; }
        = LogLevel.Information;
#endif
    internal static void Trace(string category, string message) => Write(LogLevel.Trace, category, message);
    internal static void Debug(string category, string message) => Write(LogLevel.Debug, category, message);
    internal static void Information(string category, string message) => Write(LogLevel.Information, category, message);
    internal static void Warning(string category, string message) => Write(LogLevel.Warning, category, message);
    internal static void Error(string category, string message) => Write(LogLevel.Error, category, message);
    internal static void Fatal(string category, string message) => Write(LogLevel.Fatal, category, message);

    private static void Write(LogLevel level, string category, string message)
    {
        if (level < MinimumLevel)
            return;

        lock (_lock)
        {
            Console.ForegroundColor = GetColor(level);

            Console.Error.WriteLine(
                $"[{DateTime.Now:HH:mm:ss}] " +
                $"[{level}] " +
                $"[{category}] " +
                $"{message}");

            Console.ResetColor();
        }
    }

    private static ConsoleColor GetColor(LogLevel level)
    {
        return level switch
        {
            LogLevel.Trace => ConsoleColor.DarkGray,
            LogLevel.Debug => ConsoleColor.Gray,
            LogLevel.Information => ConsoleColor.White,
            LogLevel.Warning => ConsoleColor.Yellow,
            LogLevel.Error => ConsoleColor.Red,
            LogLevel.Fatal => ConsoleColor.DarkRed,
            _ => ConsoleColor.White
        };
    }
}
internal enum LogLevel
{
    Trace,
    Debug,
    Information,
    Warning,
    Error,
    Fatal
}