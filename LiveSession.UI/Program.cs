using Avalonia;
using LiveSession.Core.Interfaces;
using LiveSession.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.IO;

namespace LiveSession.UI;

sealed class Program
{
    public static IServiceProvider Services { get; private set; } = null!;

    [STAThread]
    public static void Main(string[] args)
    {
        var logPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "LiveSession", "Logs", "LiveSession.log");

        Log.Logger = new LoggerConfiguration()
            .WriteTo.File(logPath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                fileSizeLimitBytes: 10 * 1024 * 1024,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        try
        {
            var settingsService = new LiveSession.Infrastructure.Configuration.SettingsService();
            var settings = settingsService.Load();

            var host = Host.CreateDefaultBuilder(args)
                .UseSerilog()
                .ConfigureServices(services =>
                {
                    services.AddInfrastructure(settings);
                    services.AddSingleton(settingsService);
                    services.AddTransient<ViewModels.DashboardViewModel>(sp =>
                        new ViewModels.DashboardViewModel(sp.GetRequiredService<Core.Interfaces.ISessionOrchestrator>()));
                    services.AddTransient<ViewModels.SettingsViewModel>(sp =>
                        new ViewModels.SettingsViewModel(
                            sp.GetRequiredService<Core.Interfaces.ISettingsService>(),
                            sp.GetRequiredService<Core.Interfaces.IProcessScanner>(),
                            sp.GetRequiredService<Core.Interfaces.ISessionOrchestrator>(),
                            sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<Core.Models.AppSettings>>()));
                })
                .Build();

            Services = host.Services;
            host.StartAsync().GetAwaiter().GetResult();

            if (settings.StartWithWindows)
            {
                var registrar = host.Services.GetRequiredService<IStartupRegistrar>();
                if (!registrar.IsEnabled()) registrar.Enable();
            }

            BuildAvaloniaApp(host).StartWithClassicDesktopLifetime(args);

            host.StopAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application crashed.");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    public static AppBuilder BuildAvaloniaApp(IHost? host = null)
        => AppBuilder.Configure(() => host is not null ? new App(host) : new App())
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
}
