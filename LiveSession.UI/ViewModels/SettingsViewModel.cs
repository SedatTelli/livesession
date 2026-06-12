using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveSession.Core.Interfaces;
using LiveSession.Core.Models;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace LiveSession.UI.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService? _settingsService;
    private readonly IProcessScanner? _processScanner;
    private readonly ISessionOrchestrator? _orchestrator;
    private readonly AppSettings? _settings;

    [ObservableProperty] private string _statusMessage = "";

    public ObservableCollection<TargetItemViewModel> Targets { get; } = [];

    public SettingsViewModel() { }

    public SettingsViewModel(
        ISettingsService settingsService,
        IProcessScanner processScanner,
        ISessionOrchestrator orchestrator,
        IOptions<AppSettings> settings)
    {
        _settingsService = settingsService;
        _processScanner  = processScanner;
        _orchestrator    = orchestrator;
        _settings        = settings.Value;

        LoadTargets();
    }

    private void LoadTargets()
    {
        Targets.Clear();
        if (_settings is null) return;

        foreach (var t in _settings.TargetProcesses)
            Targets.Add(new TargetItemViewModel(t));
    }

    [RelayCommand]
    private void Refresh()
    {
        if (_processScanner is null) return;

        var running = _processScanner.GetWindowedProcesses();
        var existing = new HashSet<string>(
            Targets.Select(t => t.ProcessName), StringComparer.OrdinalIgnoreCase);

        int added = 0;
        foreach (var proc in running)
        {
            if (existing.Contains(proc.ProcessName)) continue;
            Targets.Add(new TargetItemViewModel(proc.ProcessName, proc.WindowTitle) { IsEnabled = false });
            existing.Add(proc.ProcessName);
            added++;
        }

        StatusMessage = added > 0 ? $"{added} new app(s) found." : "No new apps detected.";
    }

    [RelayCommand]
    private void Save()
    {
        if (_settingsService is null || _settings is null) return;

        var updatedTargets = Targets.Select(t => t.ToModel()).ToList();
        _settings.TargetProcesses = updatedTargets;
        _settingsService.Save(_settings);
        _orchestrator?.UpdateTargets(updatedTargets);

        StatusMessage = "Settings saved.";
    }
}
