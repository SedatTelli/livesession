using LiveSession.Core.Interfaces;
using LiveSession.Core.Models;
using LiveSession.Infrastructure.Windows;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace LiveSession.Tests;

public class KeepAliveEngineTests
{
    private readonly Mock<IFullscreenDetector> _fullscreenMock = new();
    private readonly Mock<IIdleDetector> _idleMock = new();

    private KeepAliveEngine CreateEngine(bool mouseMove = false, bool keyboard = true, bool scroll = true)
    {
        var settings = Options.Create(new AppSettings
        {
            EnableMouseMove      = mouseMove,
            EnableKeyboardEvents = keyboard,
            EnableScrollEvents   = scroll
        });

        return new KeepAliveEngine(
            _fullscreenMock.Object,
            _idleMock.Object,
            settings,
            NullLogger<KeepAliveEngine>.Instance);
    }

    [Fact]
    public void Returns_Skipped_When_Fullscreen_Active()
    {
        _fullscreenMock.Setup(x => x.IsFullscreenActive()).Returns(true);
        _fullscreenMock.Setup(x => x.GetSkipReason()).Returns("Fullscreen application");
        _idleMock.Setup(x => x.GetIdleTime()).Returns(TimeSpan.FromMinutes(5));

        var sut    = CreateEngine();
        var result = sut.Execute("TestProcess");

        Assert.Equal(KeepAliveAction.Skipped, result.Action);
        Assert.False(result.WasSent);
        Assert.Equal("Fullscreen application", result.SkipReason);
    }

    [Fact]
    public void WasSent_True_When_Action_Sent()
    {
        _fullscreenMock.Setup(x => x.IsFullscreenActive()).Returns(false);
        _idleMock.Setup(x => x.GetIdleTime()).Returns(TimeSpan.FromMinutes(5));

        var sut    = CreateEngine(keyboard: true, scroll: false);
        var result = sut.Execute("TestProcess");

        Assert.NotEqual(KeepAliveAction.Skipped, result.Action);
        Assert.NotEqual(KeepAliveAction.None, result.Action);
        Assert.True(result.WasSent);
    }

    [Fact]
    public void Timestamp_Is_Set_On_Result()
    {
        _fullscreenMock.Setup(x => x.IsFullscreenActive()).Returns(false);
        _idleMock.Setup(x => x.GetIdleTime()).Returns(TimeSpan.FromMinutes(5));

        var before = DateTime.Now.AddSeconds(-1);
        var sut    = CreateEngine();
        var result = sut.Execute("TestProcess");
        var after  = DateTime.Now.AddSeconds(1);

        Assert.InRange(result.Timestamp, before, after);
    }
}
