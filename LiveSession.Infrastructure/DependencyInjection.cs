using LiveSession.Core.Interfaces;
using LiveSession.Core.Models;
using LiveSession.Infrastructure.Configuration;
using LiveSession.Infrastructure.Services;
using LiveSession.Infrastructure.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace LiveSession.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, AppSettings settings)
    {
        services.Configure<AppSettings>(opts =>
        {
            opts.CheckIntervalSeconds  = settings.CheckIntervalSeconds;
            opts.StartWithWindows      = settings.StartWithWindows;
            opts.EnableMouseMove       = settings.EnableMouseMove;
            opts.EnableKeyboardEvents  = settings.EnableKeyboardEvents;
            opts.EnableScrollEvents    = settings.EnableScrollEvents;
            opts.TargetProcesses       = settings.TargetProcesses;
        });

        services.AddSingleton<IIdleDetector, IdleDetector>();
        services.AddSingleton<IFullscreenDetector, FullscreenDetector>();
        services.AddSingleton<IKeepAliveEngine, KeepAliveEngine>();
        services.AddSingleton<IProcessScanner, ProcessScanner>();
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IStartupRegistrar, StartupRegistrar>();

        services.AddSingleton<SessionOrchestrator>();
        services.AddSingleton<ISessionOrchestrator>(sp => sp.GetRequiredService<SessionOrchestrator>());
        services.AddHostedService(sp => sp.GetRequiredService<SessionOrchestrator>());

        return services;
    }
}
