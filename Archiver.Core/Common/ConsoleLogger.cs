using System;

namespace Archiver.Core.Common
{
    public class ConsoleLogger : ILogger
    {
        private string FormatMessage(string tag, string message, Exception ex = null)
            => $"{DateTime.UtcNow:yyyy.MM.dd HH:mm:ss.fff} [{tag}]: {message}" + (ex == null ? "" : $"\nError: {ex.Message}:\n StackTrace{ex.StackTrace}");

        public void Info(string message) => Console.WriteLine(FormatMessage("INF", message));

        public void Debug(string message)
        {
#if DEBUG
            Console.WriteLine(FormatMessage("DBG", message));
#endif
        }

        public void Warning(string message) => Console.WriteLine(FormatMessage("WRN", message));

        public void Error(string message, Exception ex = null) => Console.WriteLine(FormatMessage("ERR", message, ex));
    }
}