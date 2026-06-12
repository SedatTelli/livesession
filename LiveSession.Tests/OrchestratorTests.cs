using LiveSession.Core.Interfaces;
using LiveSession.Core.Models;
using LiveSession.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace LiveSession.Tests;

public class OrchestratorTests
{
    private readonly Mock<IIdleDetector> _idleDetectorMock = new();
    private readonly Mock<IKeepAliveEngine> _engineMock = new();

    private SessionOrchestrator CreateOrchestrator(int intervalMinutes = 4, int checkIntervalSeconds = 1)
    {
        var settings = Options.Create(new AppSettings
        {
            CheckIntervalSeconds = checkIntervalSeconds,
            TargetProcesses =
            [
                new TargetProcess
                {
                    ProcessName    = "TestApp",
                    DisplayName    = "Test Application",
                    IntervalMinutes = intervalMinutes,
                    Enabled        = true
                }
            ]
        });

        return new SessionOrchestrator(
            _idleDetectorMock.Object,
            _engineMock.Object,
            settings,
            NullLogger<SessionOrchestrator>.Instance);
    }

    [Fact]
    public void Pause_Sets_IsPaused_True()
    {
        var sut = CreateOrchestrator();
        sut.Pause();
        Assert.True(sut.Status.IsPaused);
    }

    [Fact]
    public void Resume_After_Pause_Sets_IsPaused_False()
    {
        var sut = CreateOrchestrator();
        sut.Pause();
        sut.Resume();
        Assert.False(sut.Status.IsPaused);
    }

    [Fact]
    public void StatusChanged_Event_Fires_On_Pause()
    {
        var sut = CreateOrchestrator();
        SessionStatus? received = null;
        sut.StatusChanged += (_, s) => received = s;

        sut.Pause();

        Assert.NotNull(received);
        Assert.True(received!.IsPaused);
    }

    [Fact]
    public async Task Engine_Execute_Called_When_Idle_Exceeds_Threshold()
    {
        // Process not running + idle > threshold → should fire
        _idleDetectorMock.Setup(x => x.GetIdleTime()).Returns(TimeSpan.FromMinutes(5));
        _engineMock.Setup(x => x.Execute(It.IsAny<string>()))
                   .Returns(new KeepAliveResult(KeepAliveAction.CtrlKey, TimeSpan.FromMinutes(5)));

        var sut = CreateOrchestrator(intervalMinutes: 4, checkIntervalSeconds: 0);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
        await sut.StartAsync(cts.Token);
        await Task.Delay(150);
        await sut.StopAsync(CancellationToken.None);

        _engineMock.Verify(x => x.Execute(It.IsAny<string>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task Engine_Not_Called_When_Paused()
    {
        _idleDetectorMock.Setup(x => x.GetIdleTime()).Returns(TimeSpan.FromMinutes(5));

        var sut = CreateOrchestrator(intervalMinutes: 4, checkIntervalSeconds: 0);
        sut.Pause();

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
        await sut.StartAsync(cts.Token);
        await Task.Delay(150);
        await sut.StopAsync(CancellationToken.None);

        _engineMock.Verify(x => x.Execute(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Engine_Not_Called_When_Idle_Below_Threshold()
    {
        // Process not running + idle < threshold → should NOT fire
        _idleDetectorMock.Setup(x => x.GetIdleTime()).Returns(TimeSpan.FromMinutes(1));

        var sut = CreateOrchestrator(intervalMinutes: 4, checkIntervalSeconds: 0);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
        await sut.StartAsync(cts.Token);
        await Task.Delay(150);
        await sut.StopAsync(CancellationToken.None);

        _engineMock.Verify(x => x.Execute(It.IsAny<string>()), Times.Never);
    }
}
