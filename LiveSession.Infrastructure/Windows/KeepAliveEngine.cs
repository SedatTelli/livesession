using LiveSession.Core.Interfaces;
using LiveSession.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LiveSession.Infrastructure.Windows;

public sealed class KeepAliveEngine : IKeepAliveEngine
{
    private readonly IFullscreenDetector _fullscreenDetector;
    private readonly IIdleDetector _idleDetector;
    private readonly ILogger<KeepAliveEngine> _logger;
    private readonly AppSettings _settings;
    private readonly Random _random = new();

    public KeepAliveEngine(
        IFullscreenDetector fullscreenDetector,
        IIdleDetector idleDetector,
        IOptions<AppSettings> settings,
        ILogger<KeepAliveEngine> logger)
    {
        _fullscreenDetector = fullscreenDetector;
        _idleDetector       = idleDetector;
        _settings           = settings.Value;
        _logger             = logger;
    }

    public KeepAliveResult Execute(string processName)
    {
        var idleTime = _idleDetector.GetIdleTime();

        if (_fullscreenDetector.IsFullscreenActive())
        {
            var reason = _fullscreenDetector.GetSkipReason();
            _logger.LogInformation("Skipped ({Process}): {Reason}", processName, reason);
            return new KeepAliveResult(KeepAliveAction.Skipped, idleTime, reason);
        }

        var action  = PickAction();
        var windows = ProcessWindowLocator.FindWindows(processName);

        if (windows.Count > 0)
        {
            PostToWindows(windows, action);
            _logger.LogInformation("Action: {Action} → {Process} ({Count} renderer(s))",
                action, processName, windows.Count);
        }
        else
        {
            SendGlobal(action);
            _logger.LogInformation("Action: {Action} → Global (no window for '{Process}')",
                action, processName);
        }

        return new KeepAliveResult(action, idleTime);
    }

    // ─── PostMessage to target windows ───────────────────────────────────────

    private static void PostToWindows(List<IntPtr> windows, KeepAliveAction action)
    {
        ushort vk = action switch
        {
            KeepAliveAction.CtrlKey  => NativeMethods.VK_LCONTROL,
            KeepAliveAction.ShiftKey => NativeMethods.VK_LSHIFT,
            _                        => NativeMethods.VK_LSHIFT
        };

        foreach (var hwnd in windows)
        {
            NativeMethods.PostMessage(hwnd,
                NativeMethods.WM_KEYDOWN,
                (IntPtr)vk,
                (IntPtr)(int)NativeMethods.LPARAM_KEYDOWN);

            NativeMethods.PostMessage(hwnd,
                NativeMethods.WM_KEYUP,
                (IntPtr)vk,
                unchecked((IntPtr)(int)NativeMethods.LPARAM_KEYUP));
        }
    }

    // ─── Global SendInput (fallback) ─────────────────────────────────────────

    private void SendGlobal(KeepAliveAction action)
    {
        switch (action)
        {
            case KeepAliveAction.CtrlKey:  SendKey(NativeMethods.VK_LCONTROL); break;
            case KeepAliveAction.ShiftKey: SendKey(NativeMethods.VK_LSHIFT);   break;
            case KeepAliveAction.Scroll:   SendScroll();                        break;
            case KeepAliveAction.MouseMove: SendMouseMove();                    break;
        }
    }

    private KeepAliveAction PickAction()
    {
        var available = new List<KeepAliveAction>();

        if (_settings.EnableKeyboardEvents)
        {
            available.Add(KeepAliveAction.CtrlKey);
            available.Add(KeepAliveAction.ShiftKey);
        }

        if (_settings.EnableScrollEvents) available.Add(KeepAliveAction.Scroll);
        if (_settings.EnableMouseMove)    available.Add(KeepAliveAction.MouseMove);
        if (available.Count == 0)         available.Add(KeepAliveAction.CtrlKey);

        return available[_random.Next(available.Count)];
    }

    // ─── SendInput helpers ────────────────────────────────────────────────────

    private static void SendKey(ushort vk)
    {
        var inputs = new[]
        {
            MakeKeyInput(vk, 0),
            MakeKeyInput(vk, NativeMethods.KEYEVENTF_KEYUP)
        };
        NativeMethods.SendInput((uint)inputs.Length, inputs,
            System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.INPUT>());
    }

    private static void SendScroll()
    {
        var inputs = new[] { MakeScrollInput(120), MakeScrollInput(-120) };
        NativeMethods.SendInput((uint)inputs.Length, inputs,
            System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.INPUT>());
    }

    private static void SendMouseMove()
    {
        if (!NativeMethods.GetCursorPos(out _)) return;
        var inputs = new[] { MakeMouseMoveInput(1, 0), MakeMouseMoveInput(-1, 0) };
        NativeMethods.SendInput((uint)inputs.Length, inputs,
            System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.INPUT>());
    }

    private static NativeMethods.INPUT MakeKeyInput(ushort vk, uint flags) => new()
    {
        type = NativeMethods.INPUT_KEYBOARD,
        u    = new NativeMethods.InputUnion { ki = new NativeMethods.KEYBDINPUT { wVk = vk, dwFlags = flags } }
    };

    private static NativeMethods.INPUT MakeScrollInput(int delta) => new()
    {
        type = NativeMethods.INPUT_MOUSE,
        u    = new NativeMethods.InputUnion { mi = new NativeMethods.MOUSEINPUT { dwFlags = NativeMethods.MOUSEEVENTF_WHEEL, mouseData = delta } }
    };

    private static NativeMethods.INPUT MakeMouseMoveInput(int dx, int dy) => new()
    {
        type = NativeMethods.INPUT_MOUSE,
        u    = new NativeMethods.InputUnion { mi = new NativeMethods.MOUSEINPUT { dx = dx, dy = dy, dwFlags = NativeMethods.MOUSEEVENTF_MOVE } }
    };
}
