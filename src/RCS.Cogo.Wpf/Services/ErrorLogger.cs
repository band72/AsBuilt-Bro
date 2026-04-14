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
        string timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ssZ");
        
        // 1. Local Logging (App Data)
        try
        {
            if (!Directory.Exists(LogDir))
            {
                Directory.CreateDirectory(LogDir);
            }

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
            // Failsafe - if we can't write to the error log locally, swallow it
        }

        // 2. Remote Telemetry (Backend Error Reporting)
        try
        {
            // Unmanaged C++ string decryption call (Obfuscates destination API from memory scraping)
            string endpoint = NativeSecurityWrapper.GetSecureTelemetryEndpoint();
            if (!string.IsNullOrWhiteSpace(endpoint))
            {
                System.Threading.Tasks.Task.Run(async () =>
                {
                    try
                    {
                        using var client = new System.Net.Http.HttpClient();
                        client.Timeout = TimeSpan.FromSeconds(5);
                        var payload = new { Context = context, Type = ex.GetType().Name, Message = ex.Message, Stack = ex.StackTrace, Timestamp = timestamp };
                        var json = System.Text.Json.JsonSerializer.Serialize(payload);
                        var content = new System.Net.Http.StringContent(json, System.Text.Encoding.UTF8, "application/json");
                        await client.PostAsync(endpoint, content);
                    }
                    catch { /* Swallow telemetry drop if internet fails */ }
                });
            }
        }
        catch { }
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
