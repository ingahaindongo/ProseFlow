using System.ComponentModel;
using System.Runtime.CompilerServices;
using ProseFlow.Core.Enums;

namespace ProseFlow.Application.DTOs;

/// <summary>
/// A Data Transfer Object representing a single background action being tracked by the system.
/// It implements INotifyPropertyChanged to allow UI elements to react to status changes.
/// </summary>
public class TrackedAction : INotifyPropertyChanged
{
    public Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Icon { get; init; }
    public DateTime StartTime { get; init; }
    public CancellationTokenSource Cts { get; } = new();

    private ActionStatus _status;
    public ActionStatus Status
    {
        get => _status;
        set => SetField(ref _status, value);
    }

    #region INotifyPropertyChanged
    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        OnPropertyChanged(propertyName);
    }
    #endregion
}