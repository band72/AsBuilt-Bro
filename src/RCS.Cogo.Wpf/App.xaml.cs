using System;
using System.Configuration;
using System.Data;
using System.Windows;
using System.Threading.Tasks;
using RCS.Cogo.Wpf.Services;

namespace RCS.Cogo.Wpf;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // UI Thread Exception Handling
        this.DispatcherUnhandledException += App_DispatcherUnhandledException;

        // Background Thread Exception Handling
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

        // Task Exception Handling
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        
        ErrorLogger.LogMessage("Application Started.");
    }

    private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        ErrorLogger.LogException(e.Exception, "Dispatcher");
        e.Handled = true; // Prevent app crash if possible
        MessageBox.Show($"An unexpected error occurred. A log has been generated in %AppData%\\RCS_Crash_Dumps.\n\n{e.Exception.Message}", "Critical Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            ErrorLogger.LogException(ex, "AppDomain");
            MessageBox.Show($"A fatal background error occurred. A log has been generated in %AppData%\\RCS_Crash_Dumps.\n\n{ex.Message}", "Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        ErrorLogger.LogException(e.Exception, "TaskScheduler");
        e.SetObserved();
    }
    
    protected override void OnExit(ExitEventArgs e)
    {
        ErrorLogger.LogMessage("Application Exited.");
        base.OnExit(e);
    }
}
