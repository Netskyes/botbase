using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;

namespace Utility
{
    public enum LogLevel
    {
        Debug, Error
    }

    public static class Logger
    {
        private static string LogPath => Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        public static void LogException(Exception e)
        {
            var builder = new StringBuilder();
            builder.Append(e.Message + Environment.NewLine);
            builder.Append(e.StackTrace + Environment.NewLine);

            Log(builder.ToString(), LogLevel.Error);
        }

        public static void Log(string message, LogLevel logLevel = LogLevel.Debug)
        {
            var builder = new StringBuilder();
            builder.Append(DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"));
            builder.Append(" [");
            builder.Append(logLevel.ToString());
            builder.Append("] ");
            builder.Append(message);

            File.AppendAllText(Path.Combine(LogPath, $"{logLevel.ToString()}.log"), builder.ToString());
        }
    }
}
