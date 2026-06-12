using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Avalonia.Threading;
using LiveSession.UI.ViewModels;
using LiveSession.UI.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

namespace LiveSession.UI;

public partial class App : Application
{
    private readonly IHost? _host;
    private DashboardWindow? _dashboard;
    private SettingsWindow? _settingsWindow;
    private TrayIcon? _trayIcon;
    private NativeMenuItem? _pauseItem;
    private bool _isExiting;

    public App() { }
    public App(IHost host) { _host = host; }

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = null;
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            desktop.Exit += (_, _) => _isExiting = true;
            SetupTrayIcon(desktop);
        }
        base.OnFrameworkInitializationCompleted();
    }

    private void SetupTrayIcon(IClassicDesktopStyleApplicationLifetime desktop)
    {
        _trayIcon = new TrayIcon
        {
            Icon = new WindowIcon(AssetLoader.Open(new Uri("avares://LiveSession/Assets/livesession.ico"))),
            ToolTipText = "LiveSession - Active",
            IsVisible = true
        };

        var menu = new NativeMenu();

        var openItem = new NativeMenuItem("Open Dashboard");
        openItem.Click += (_, _) => ShowDashboard();

        var settingsItem = new NativeMenuItem("Settings");
        settingsItem.Click += (_, _) => ShowSettings();

        _pauseItem = new NativeMenuItem("Pause Protection");
        _pauseItem.Click += (_, _) => TogglePause();

        var logsItem = new NativeMenuItem("View Logs");
        logsItem.Click += (_, _) => OpenLogs();

        var exitItem = new NativeMenuItem("Exit");
        exitItem.Click += (_, _) =>
        {
            _isExiting = true;
            desktop.Shutdown();
        };

        menu.Items.Add(openItem);
        menu.Items.Add(settingsItem);
        menu.Items.Add(new NativeMenuItemSeparator());
        menu.Items.Add(_pauseItem);
        menu.Items.Add(new NativeMenuItemSeparator());
        menu.Items.Add(logsItem);
        menu.Items.Add(new NativeMenuItemSeparator());
        menu.Items.Add(exitItem);

        _trayIcon.Menu = menu;
        _trayIcon.Clicked += (_, _) => ShowDashboard();
    }

    private void ShowDashboard()
    {
        if (_dashboard is null)
        {
            var vm = _host is not null
                ? _host.Services.GetRequiredService<DashboardViewModel>()
                : new DashboardViewModel();

            _dashboard = new DashboardWindow { DataContext = vm };
            _dashboard.Closing += (s, e) =>
            {
                if (_isExiting) return;
                e.Cancel = true;
                Dispatcher.UIThread.Post(() => _dashboard?.Hide());
            };
        }

        if (!_dashboard.IsVisible)
            _dashboard.Show();

        _dashboard.Activate();
    }

    internal void ShowSettings()
    {
        if (_settingsWindow is null)
        {
            var vm = _host is not null
                ? _host.Services.GetRequiredService<SettingsViewModel>()
                : new SettingsViewModel();

            _settingsWindow = new SettingsWindow { DataContext = vm };
            _settingsWindow.Closing += (s, e) =>
            {
                if (_isExiting) return;
                e.Cancel = true;
                Dispatcher.UIThread.Post(() => _settingsWindow?.Hide());
            };
        }

        if (!_settingsWindow.IsVisible)
            _settingsWindow.Show();

        _settingsWindow.Activate();
    }

    private void TogglePause()
    {
        if (_host is null) return;
        var orchestrator = _host.Services.GetRequiredService<Core.Interfaces.ISessionOrchestrator>();

        if (orchestrator.Status.IsPaused)
        {
            orchestrator.Resume();
            _pauseItem!.Header = "Pause Protection";
            _trayIcon!.ToolTipText = "LiveSession - Active";
        }
        else
        {
            orchestrator.Pause();
            _pauseItem!.Header = "Resume Protection";
            _trayIcon!.ToolTipText = "LiveSession - Paused";
        }
    }

    private static void OpenLogs()
    {
        var logDir = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "LiveSession", "Logs");

        if (System.IO.Directory.Exists(logDir))
            System.Diagnostics.Process.Start("explorer.exe", logDir);
    }
}
