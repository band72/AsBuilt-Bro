using System;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Threading.Tasks;
using RCS.Cogo.Wpf.Services;
using RCS.Services;

namespace RCS.Cogo.Wpf;

/// <summary>
/// Application entry point.
/// Handles all global exception vectors and wires automatic crash reporting
/// to both the local disk log and the remote RCS Cloud Run endpoint.
/// </summary>
public partial class App : Application
{
    // Shared singleton — avoids socket exhaustion from multiple instances.
    private static readonly ErrorReporter _reporter = new();
    private static readonly HttpClient    _http     = new() { Timeout = TimeSpan.FromSeconds(5) };

    private const string GitHubReleasesApi =
        "https://api.github.com/repos/band72/RCS.Cogo.Enterprise.Modern/releases/latest";
    private const string GitHubReleasesPage =
        "https://github.com/band72/RCS.Cogo.Enterprise.Modern/releases";

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // ── Global exception handlers ─────────────────────────────────────
        DispatcherUnhandledException          += App_DispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

        // ── Flush any crash reports that failed to send in a previous session ─
        _ = FlushPendingReportsAsync();

        // ── Background update check (non-blocking, swallows all errors) ───────
        _ = CheckForUpdateAsync();

        ErrorLogger.LogMessage("Application Started.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // UI-thread crashes (WPF Dispatcher)
    // ─────────────────────────────────────────────────────────────────────────
    private void App_DispatcherUnhandledException(
        object sender,
        System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        ErrorLogger.LogException(e.Exception, "Dispatcher");

        // Fire-and-forget: never await inside a dispatcher exception handler
        _ = _reporter.ReportCrashAsync(
                e.Exception,
                context:   "Dispatcher",
                machineId: TryGetMachineId());

        e.Handled = true;   // prevent WPF from terminating the process

        MessageBox.Show(
            $"An unexpected error occurred. A report has been sent automatically.\n\n{e.Exception.Message}",
            "Unexpected Error",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Background / native thread crashes
    // ─────────────────────────────────────────────────────────────────────────
    private void CurrentDomain_UnhandledException(
        object sender,
        UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is not Exception ex) return;

        ErrorLogger.LogException(ex, "AppDomain");

        // Use Task.Run because we may be on a finalizer or native thread
        Task.Run(() => _reporter.ReportCrashAsync(ex, "AppDomain", TryGetMachineId()))
            .GetAwaiter()
            .GetResult();   // safe to block here — process is terminating anyway

        if (e.IsTerminating)
        {
            MessageBox.Show(
                $"A fatal background error occurred. A crash report has been sent.\n\n{ex.Message}",
                "Fatal Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Unobserved Task exceptions (async fire-and-forget)
    // ─────────────────────────────────────────────────────────────────────────
    private void TaskScheduler_UnobservedTaskException(
        object? sender,
        UnobservedTaskExceptionEventArgs e)
    {
        ErrorLogger.LogException(e.Exception, "TaskScheduler");

        _ = _reporter.ReportCrashAsync(
                e.Exception,
                context:   "TaskScheduler",
                machineId: TryGetMachineId());

        e.SetObserved();    // prevent process termination
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Shutdown
    // ─────────────────────────────────────────────────────────────────────────
    protected override void OnExit(ExitEventArgs e)
    {
        ErrorLogger.LogMessage("Application Exited.");
        base.OnExit(e);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Flush crash reports that accumulated during a previous offline session.
    /// Runs silently in the background — never blocks startup.
    /// </summary>
    private static async Task FlushPendingReportsAsync()
    {
        try
        {
            await _reporter.FlushPendingReportsAsync();
        }
        catch
        {
            // Flush is best-effort — never let it break startup
        }
    }

    /// <summary>
    /// Non-blocking: queries GitHub for the latest release tag and prompts the user
    /// to download if a newer version is available.  All failures are silently swallowed.
    /// </summary>
    private static async Task CheckForUpdateAsync()
    {
        try
        {
            // GitHub API requires a User-Agent header
            using var request = new HttpRequestMessage(HttpMethod.Get, GitHubReleasesApi);
            request.Headers.Add("User-Agent", "RCS-Cogo-Enterprise");
            request.Headers.Add("Accept", "application/vnd.github+json");

            var response = await _http.SendAsync(request).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode) return;

            var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("tag_name", out var tagEl)) return;
            var tag = tagEl.GetString()?.TrimStart('v') ?? "";

            if (!Version.TryParse(tag, out var latestVersion)) return;

            // Read current version from assembly metadata
            var currentVersion = Assembly.GetExecutingAssembly()
                .GetCustomAttribute<AssemblyFileVersionAttribute>()
                ?.Version ?? "0.0.0.0";
            if (!Version.TryParse(currentVersion, out var thisVersion)) return;

            if (latestVersion <= thisVersion) return;   // already up to date

            // Show prompt on UI thread
            Application.Current.Dispatcher.Invoke(() =>
            {
                var result = MessageBox.Show(
                    $"A new version of RCS COGO Enterprise is available!\n\n" +
                    $"  Current : v{thisVersion}\n" +
                    $"  Latest  : v{latestVersion}\n\n" +
                    "Open the download page now?",
                    "Update Available",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);

                if (result == MessageBoxResult.Yes)
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName        = GitHubReleasesPage,
                        UseShellExecute = true
                    });
            });
        }
        catch
        {
            // Network unavailable, API rate-limit, parse error — all silently ignored
        }
    }

    /// <summary>
    /// Safe wrapper for hardware fingerprint — returns null on failure so the
    /// reporter still sends without a machine ID rather than throwing.
    /// </summary>
    private static string? TryGetMachineId()
    {
        try { return NativeSecurityWrapper.GetHardwareFingerprint(); }
        catch { return null; }
    }
}
