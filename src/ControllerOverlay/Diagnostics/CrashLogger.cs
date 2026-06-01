using System;
using System.IO;
using System.Text;
using System.Windows;

namespace ControllerOverlay.Diagnostics
{
    internal static class CrashLogger
    {
        public static string Write(string source, Exception exception)
        {
            try
            {
                string logDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "ControllerOverlay",
                    "logs");
                Directory.CreateDirectory(logDir);

                string logPath = Path.Combine(logDir, $"crash-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{source}.log");
                var content = new StringBuilder();
                content.AppendLine($"TimeUtc: {DateTime.UtcNow:O}");
                content.AppendLine($"Source: {source}");
                content.AppendLine($"AppVersion: {typeof(CrashLogger).Assembly.GetName().Version}");
                content.AppendLine($"BaseDirectory: {AppContext.BaseDirectory}");
                content.AppendLine();
                content.AppendLine(exception.ToString());

                File.WriteAllText(logPath, content.ToString(), Encoding.UTF8);
                return logPath;
            }
            catch
            {
                return string.Empty;
            }
        }

        public static void ShowFatalError(string logPath, Exception exception)
        {
            string message = "ControllerOverlay failed to start.";
            if (!string.IsNullOrWhiteSpace(logPath))
            {
                message += Environment.NewLine + "Crash log: " + logPath;
            }

            message += Environment.NewLine + Environment.NewLine + exception.Message;

            try
            {
                MessageBox.Show(message, "ControllerOverlay", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch
            {
            }
        }
    }
}
