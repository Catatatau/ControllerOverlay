using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace ControllerOverlay;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        DispatcherUnhandledException += App_DispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += AppDomain_UnhandledException;
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        base.OnStartup(e);
    }

    private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        string logPath = Diagnostics.CrashLogger.Write("dispatcher", e.Exception);
        Diagnostics.CrashLogger.ShowFatalError(logPath, e.Exception);
        e.Handled = true;
        Shutdown(1);
    }

    private static void AppDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception exception)
        {
            Diagnostics.CrashLogger.Write("appdomain", exception);
        }
    }

    private static void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        Diagnostics.CrashLogger.Write("task", e.Exception);
        e.SetObserved();
    }
}
