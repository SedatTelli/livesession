using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveSession.Core.Interfaces;
using LiveSession.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LiveSession.UI.ViewModels;

public sealed class TargetStatusRow
{
    public string DisplayLabel   { get; init; } = "";
    public bool   IsRunning      { get; init; }
    public string LastKeepalive  { get; init; } = "—";
    public int    IntervalMinutes{ get; init; }
}

public partial class DashboardViewModel : ObservableObject
{
    private readonly ISessionOrchestrator? _orchestrator;

    [ObservableProperty] private string _statusText = "Protected";
    [ObservableProperty] private string _lastKeepAliveTime = "—";
    [ObservableProperty] private string _lastActionType = "—";
    [ObservableProperty] private string _lastTarget = "—";
    [ObservableProperty] private string _currentIdleTime = "0s";
    [ObservableProperty] private int _todayActionsCount;
    [ObservableProperty] private bool _isPaused;
    [ObservableProperty] private List<TargetStatusRow> _targets = [];

    public string PauseButtonText => IsPaused ? "Resume Protection" : "Pause Protection";

    public DashboardViewModel() { }

    public DashboardViewModel(ISessionOrchestrator orchestrator)
    {
        _orchestrator = orchestrator;
        _orchestrator.StatusChanged += OnStatusChanged;
        _orchestrator.KeepAliveSent += OnKeepAliveSent;
        SyncFromStatus(orchestrator.Status);
    }

    private void OnStatusChanged(object? sender, SessionStatus status)
        => Avalonia.Threading.Dispatcher.UIThread.Post(() => SyncFromStatus(status));

    private void OnKeepAliveSent(object? sender, KeepAliveResult result)
        => Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            LastKeepAliveTime = result.Timestamp.ToString("HH:mm:ss");
            LastActionType    = result.Action.ToString();
        });

    private void SyncFromStatus(SessionStatus status)
    {
        StatusText        = status.StatusText;
        IsPaused          = status.IsPaused;
        TodayActionsCount = status.TodayActionsCount;
        CurrentIdleTime   = FormatIdle(status.CurrentIdleTime);
        LastTarget        = status.LastActionTarget;

        if (status.LastKeepAliveTime.HasValue)
            LastKeepAliveTime = status.LastKeepAliveTime.Value.ToString("HH:mm:ss");

        if (status.LastAction != KeepAliveAction.None)
            LastActionType = status.LastAction.ToString();

        Targets = status.Targets.Select(t => new TargetStatusRow
        {
            DisplayLabel    = !string.IsNullOrEmpty(t.DisplayName) ? t.DisplayName : t.ProcessName,
            IsRunning       = t.IsRunning,
            LastKeepalive   = t.LastKeepAlive.HasValue ? t.LastKeepAlive.Value.ToString("HH:mm:ss") : "—",
            IntervalMinutes = t.IntervalMinutes
        }).ToList();
    }

    [RelayCommand]
    private void TogglePause()
    {
        if (_orchestrator is null) return;
        if (_orchestrator.Status.IsPaused) _orchestrator.Resume();
        else _orchestrator.Pause();
    }

    [RelayCommand]
    private static void OpenLinkedIn()
    {
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName        = "https://www.linkedin.com/in/sedattelli/",
            UseShellExecute = true
        });
    }

    partial void OnIsPausedChanged(bool value) => OnPropertyChanged(nameof(PauseButtonText));

    private static string FormatIdle(TimeSpan t)
        => t.TotalMinutes >= 1
            ? $"{(int)t.TotalMinutes}m {t.Seconds}s"
            : $"{t.Seconds}s";
}
