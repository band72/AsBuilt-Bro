using System;
using System.IO;

namespace RCS.Cogo.Wpf.Services;

public static class ErrorLogger
{
    private static readonly string LogDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
        "RCS_Crash_Dumps");
    
    private static readonly string LogFile = Path.Combine(LogDir, "crashlog.txt");

    public static void LogException(Exception ex, string context = "Unhandled")
    {
        try
        {
            if (!Directory.Exists(LogDir))
            {
                Directory.CreateDirectory(LogDir);
            }

            string timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ssZ");
            string entry = $"[{timestamp}] [{context}] {ex.GetType().Name}: {ex.Message}\n" +
                           $"{ex.StackTrace}\n";
            
            if (ex.InnerException != null)
            {
                entry += $"--- Inner Exception ---\n{ex.InnerException.GetType().Name}: {ex.InnerException.Message}\n{ex.InnerException.StackTrace}\n";
            }
            entry += new string('-', 80) + "\n";

            File.AppendAllText(LogFile, entry);
        }
        catch 
        {
            // Failsafe - if we can't write to the error log, there's not much we can do
        }
    }
    
    public static void LogMessage(string message)
    {
        try
        {
            if (!Directory.Exists(LogDir)) Directory.CreateDirectory(LogDir);
            string timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ssZ");
            File.AppendAllText(LogFile, $"[{timestamp}] [INFO] {message}\n");
        }
        catch {}
    }
}
