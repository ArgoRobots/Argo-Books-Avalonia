using ArgoBooks.Core.Services;
using ArgoBooks.Desktop.Services;
using Avalonia;

namespace ArgoBooks.Desktop;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        // Install crash handlers first so a failure anywhere in startup is captured.
        CrashReporter.InstallHandlers();

        // Create the update service for desktop platforms
        App.UpdateService = new NetSparkleUpdateService();

        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
