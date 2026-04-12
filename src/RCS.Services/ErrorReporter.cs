using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace RCS.Services;

/// <summary>
/// Production error reporter. Sends crash and diagnostic payloads to the RCS Cloud Run
/// endpoint. Supports offline buffering (JSONL queue flushed on next startup) and
/// flood-limiting (same exception type / frame capped at one report per 10 minutes).
/// </summary>
public class ErrorReporter
{
    // ── Configuration ────────────────────────────────────────────────────────
    private const string Endpoint    = "https://dotnet-error-guard-684278904905.us-west1.run.app/api/errors";
    private const string AppSource   = "RCS Cogo Enterprise Modern";
    private const int    TimeoutMs   = 8000;
    private const int    FloodWindowMinutes = 10;

    // ── Shared infrastructure ─────────────────────────────────────────────────
    private static readonly HttpClient _http = new()
    {
        Timeout = TimeSpan.FromMilliseconds(TimeoutMs)
    };

    // ── Offline queue ─────────────────────────────────────────────────────────
    private static readonly string QueueDir  =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                     "RCS_Crash_Dumps");

    private static readonly string QueueFile = Path.Combine(QueueDir, "pending_reports.jsonl");

    // ── Flood limiter ─────────────────────────────────────────────────────────
    // Key = "ExceptionType|TopFrame", Value = last reported UTC
    private static readonly Dictionary<string, DateTime> _floodMap = [];
    private static readonly object _floodLock = new();

    // ─────────────────────────────────────────────────────────────────────────
    // PUBLIC API
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Automatically called from global crash handlers (fire-and-forget safe).
    /// Enriched payload: version, OS, machine ID, context, full inner-exception chain.
    /// Flood-limited + offline-queued on network failure.
    /// </summary>
    public async Task<bool> ReportCrashAsync(Exception ex, string context, string? machineId = null)
    {
        if (IsFloodLimited(ex)) return false;

        var payload = BuildPayload(ex, context, "high", machineId);
        bool sent   = await PostAsync(payload);

        if (!sent) EnqueueOffline(payload);   // store for next-startup flush
        else MarkFloodSent(ex);

        return sent;
    }

    /// <summary>
    /// Manual user-initiated report (from ErrorReportWindow). No flood limiter applied.
    /// Legacy-compatible with the existing window code.
    /// </summary>
    public async Task<bool> ReportErrorAsync(Exception ex, string severity = "high", string? userId = null)
    {
        var payload = BuildPayload(ex, "ManualReport", severity, userId);
        bool sent   = await PostAsync(payload);
        if (!sent) EnqueueOffline(payload);
        return sent;
    }

    /// <summary>
    /// Call once on App.OnStartup to flush any crash reports that failed during
    /// a previous session when the network was unavailable.
    /// </summary>
    public async Task FlushPendingReportsAsync()
    {
        if (!File.Exists(QueueFile)) return;

        string[] lines;
        try
        {
            lines = await File.ReadAllLinesAsync(QueueFile);
            File.Delete(QueueFile);   // optimistic delete; re-queue on failure below
        }
        catch { return; }

        var failed = new List<string>();
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            try
            {
                var content = new StringContent(line, Encoding.UTF8, "application/json");
                var resp    = await _http.PostAsync(Endpoint, content);
                if (!resp.IsSuccessStatusCode)
                    failed.Add(line);
            }
            catch
            {
                failed.Add(line);   // network still down — keep for next time
            }
        }

        if (failed.Count > 0)
        {
            try { await File.WriteAllLinesAsync(QueueFile, failed); }
            catch { /* nothing we can do */ }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PRIVATE HELPERS
    // ─────────────────────────────────────────────────────────────────────────

    private static object BuildPayload(Exception ex, string context, string severity, string? userId)
    {
        return new
        {
            message   = FlattenException(ex),
            severity  = severity,
            source    = AppSource,
            version   = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "unknown",
            os        = Environment.OSVersion.VersionString,
            context   = context,
            userId    = userId ?? "anonymous",
            timestamp = DateTime.UtcNow.ToString("O"),
            url       = $"/crash/{context.ToLower()}"
        };
    }

    private static async Task<bool> PostAsync(object payload)
    {
        try
        {
            string json    = JsonSerializer.Serialize(payload);
            var    content = new StringContent(json, Encoding.UTF8, "application/json");
            var    resp    = await _http.PostAsync(Endpoint, content);
            return resp.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private static void EnqueueOffline(object payload)
    {
        try
        {
            Directory.CreateDirectory(QueueDir);
            string line = JsonSerializer.Serialize(payload);
            File.AppendAllText(QueueFile, line + Environment.NewLine);
        }
        catch { /* disk may also be failing — nothing to do */ }
    }

    private static string FlattenException(Exception ex)
    {
        var sb = new StringBuilder();
        var current = ex;
        int depth   = 0;
        while (current != null && depth < 5)
        {
            if (depth > 0) sb.AppendLine("--- Inner Exception ---");
            sb.AppendLine($"{current.GetType().FullName}: {current.Message}");
            if (!string.IsNullOrEmpty(current.StackTrace))
                sb.AppendLine(current.StackTrace);
            current = current.InnerException;
            depth++;
        }
        return sb.ToString();
    }

    // ── Flood limiter ─────────────────────────────────────────────────────────

    private static string FloodKey(Exception ex)
    {
        // Key = ExceptionType + first user-code stack frame that contains a known namespace
        var frames = ex.StackTrace?.Split('\n');
        var topFrame = frames?.FirstOrDefault(f => f.Contains("RCS.")) ?? frames?.FirstOrDefault() ?? "unknown";
        return $"{ex.GetType().Name}|{topFrame.Trim()}";
    }

    private static bool IsFloodLimited(Exception ex)
    {
        var key = FloodKey(ex);
        lock (_floodLock)
        {
            if (_floodMap.TryGetValue(key, out var last))
                return (DateTime.UtcNow - last).TotalMinutes < FloodWindowMinutes;
            return false;
        }
    }

    private static void MarkFloodSent(Exception ex)
    {
        var key = FloodKey(ex);
        lock (_floodLock)
            _floodMap[key] = DateTime.UtcNow;
    }
}
