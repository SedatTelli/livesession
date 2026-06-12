using LiveSession.Core.Interfaces;
using LiveSession.Core.Models;
using LiveSession.Infrastructure.Chromium;
using LiveSession.Infrastructure.Windows;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LiveSession.Infrastructure.Services;

public sealed class SessionOrchestrator : BackgroundService, ISessionOrchestrator
{
    private readonly IIdleDetector _idleDetector;
    private readonly IKeepAliveEngine _keepAliveEngine;
    private readonly ILogger<SessionOrchestrator> _logger;
    private readonly AppSettings _settings;
    private readonly SalesforceHeartbeat _sfHeartbeat;

    // Per-target last keep-alive timestamp
    private readonly Dictionary<string, DateTime> _lastKeepAlive = new(StringComparer.OrdinalIgnoreCase);
    private readonly Lock _lock = new();

    public SessionStatus Status { get; } = new();

    public event EventHandler<KeepAliveResult>? KeepAliveSent;
    public event EventHandler<SessionStatus>? StatusChanged;

    public SessionOrchestrator(
        IIdleDetector idleDetector,
        IKeepAliveEngine keepAliveEngine,
        IOptions<AppSettings> settings,
        ILogger<SessionOrchestrator> logger)
    {
        _idleDetector    = idleDetector;
        _keepAliveEngine = keepAliveEngine;
        _settings        = settings.Value;
        _logger          = logger;
        _sfHeartbeat     = new SalesforceHeartbeat(logger);

        // Başlangıçta Status.Targets listesini ayarla
        SyncTargets(_settings.TargetProcesses);
    }

    public void Pause()
    {
        Status.IsPaused = true;
        _logger.LogInformation("Protection paused.");
        StatusChanged?.Invoke(this, Status);
    }

    public void Resume()
    {
        Status.IsPaused = false;
        _logger.LogInformation("Protection resumed.");
        StatusChanged?.Invoke(this, Status);
    }

    public void UpdateTargets(List<TargetProcess> targets)
    {
        lock (_lock)
        {
            _settings.TargetProcesses.Clear();
            _settings.TargetProcesses.AddRange(targets);
            SyncTargets(targets);
        }

        _logger.LogInformation("Targets updated: {Count} target(s)", targets.Count);
        StatusChanged?.Invoke(this, Status);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("LiveSession started. Check interval: {Interval}s", _settings.CheckIntervalSeconds);

        var interval = TimeSpan.FromSeconds(_settings.CheckIntervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.Now;
                Status.CurrentIdleTime = _idleDetector.GetIdleTime();

                if (!Status.IsPaused && Status.IsProtectionEnabled)
                {
                    List<TargetProcess> targets;
                    lock (_lock)
                        targets = [.. _settings.TargetProcesses];

                    foreach (var target in targets.Where(t => t.Enabled))
                    {
                        var threshold = TimeSpan.FromMinutes(target.IntervalMinutes);
                        var isRunning = ProcessWindowLocator.IsProcessRunning(target.ProcessName);

                        // Process çalışmıyorsa koruyacak oturum yok → atla
                        if (!isRunning)
                        {
                            UpdateTargetStatus(target, false, now);
                            continue;
                        }

                        // Son keep-alive'dan bu yana interval geçti mi?
                        bool shouldFire;
                        lock (_lock)
                        {
                            var lastTime = _lastKeepAlive.GetValueOrDefault(target.ProcessName, DateTime.MinValue);
                            shouldFire = (now - lastTime) >= threshold;
                        }

                        if (shouldFire)
                        {
                            var result = _keepAliveEngine.Execute(target.ProcessName);

                            if (result.WasSent)
                            {
                                lock (_lock)
                                    _lastKeepAlive[target.ProcessName] = now;

                                Status.LastKeepAliveTime = result.Timestamp;
                                Status.LastAction        = result.Action;
                                Status.LastActionTarget  = target.DisplayName.Length > 0
                                                            ? target.DisplayName
                                                            : target.ProcessName;
                                Status.TodayActionsCount++;
                                KeepAliveSent?.Invoke(this, result);

                                // Island Browser için Salesforce HTTP heartbeat — server-side session'ı sıfırla
                                if (target.ProcessName.Equals("Island", StringComparison.OrdinalIgnoreCase))
                                    _ = _sfHeartbeat.PingAsync();
                            }
                        }

                        // Durum listesini güncelle
                        UpdateTargetStatus(target, isRunning, now);
                    }
                }

                StatusChanged?.Invoke(this, Status);
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Orchestrator loop error.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        _logger.LogInformation("LiveSession stopped.");
    }

    private void SyncTargets(List<TargetProcess> targets)
    {
        Status.Targets = targets.Select(t => new TargetProcessStatus
        {
            ProcessName    = t.ProcessName,
            DisplayName    = t.DisplayName,
            Enabled        = t.Enabled,
            IntervalMinutes = t.IntervalMinutes,
            IsRunning      = false
        }).ToList();
    }

    private void UpdateTargetStatus(TargetProcess target, bool isRunning, DateTime now)
    {
        var status = Status.Targets.FirstOrDefault(t =>
            string.Equals(t.ProcessName, target.ProcessName, StringComparison.OrdinalIgnoreCase));

        if (status == null) return;

        status.IsRunning       = isRunning;
        status.Enabled         = target.Enabled;
        status.IntervalMinutes = target.IntervalMinutes;

        lock (_lock)
        {
            if (_lastKeepAlive.TryGetValue(target.ProcessName, out var last) && last != DateTime.MinValue)
                status.LastKeepAlive = last;
        }
    }
}
