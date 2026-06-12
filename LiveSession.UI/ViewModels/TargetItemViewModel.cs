using CommunityToolkit.Mvvm.ComponentModel;
using LiveSession.Core.Models;

namespace LiveSession.UI.ViewModels;

public partial class TargetItemViewModel : ObservableObject
{
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(IntervalLabel))]
    private int _intervalMinutes = 4;

    [ObservableProperty] private bool _isEnabled = true;

    public string ProcessName { get; }
    public string DisplayName { get; }
    public string DisplayLabel => !string.IsNullOrEmpty(DisplayName) ? DisplayName : ProcessName;
    public string IntervalLabel => $"{IntervalMinutes}m";

    public TargetItemViewModel(TargetProcess target)
    {
        ProcessName      = target.ProcessName;
        DisplayName      = target.DisplayName;
        _isEnabled       = target.Enabled;
        _intervalMinutes = target.IntervalMinutes;
    }

    public TargetItemViewModel(string processName, string displayName = "")
    {
        ProcessName = processName;
        DisplayName = displayName;
    }

    public TargetProcess ToModel() => new()
    {
        ProcessName     = ProcessName,
        DisplayName     = DisplayName,
        Enabled         = IsEnabled,
        IntervalMinutes = IntervalMinutes
    };
}
