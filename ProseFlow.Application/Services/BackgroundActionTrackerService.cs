using ProseFlow.Application.DTOs;
using ProseFlow.Application.Interfaces;
using ProseFlow.Core.Enums;

namespace ProseFlow.Application.Services;

/// <summary>
/// A singleton service that provides centralized tracking for all active background actions.
/// It is fully UI-agnostic and uses events to communicate changes.
/// </summary>
public class BackgroundActionTrackerService : IBackgroundActionTrackerService
{
    private readonly List<TrackedAction> _activeActions = [];
    private readonly object _lock = new();

    public event Action<TrackedAction>? ActionAdded;
    public event Action<TrackedAction>? ActionRemoved;

    /// <inheritdoc />
    public IEnumerable<TrackedAction> GetActiveActions()
    {
        lock (_lock)
        {
            return _activeActions.ToList();
        }
    }

    /// <inheritdoc />
    public TrackedAction AddAction(string name, string icon)
    {
        var action = new TrackedAction
        {
            Id = Guid.NewGuid(),
            Name = name,
            Icon = icon,
            StartTime = DateTime.UtcNow,
            Status = ActionStatus.Queued
        };

        lock (_lock)
        {
            _activeActions.Add(action);
        }

        ActionAdded?.Invoke(action);
        return action;
    }

    /// <inheritdoc />
    public void UpdateStatus(Guid id, ActionStatus newStatus)
    {
        TrackedAction? action;
        lock (_lock)
        {
            action = _activeActions.FirstOrDefault(a => a.Id == id);
        }

        if (action != null)
        {
            action.Status = newStatus;
        }
    }

    /// <inheritdoc />
    public void RequestCancellation(Guid id)
    {
        TrackedAction? action;
        lock (_lock)
        {
            action = _activeActions.FirstOrDefault(a => a.Id == id);
        }

        action?.Cts.Cancel();
    }

    /// <inheritdoc />
    public void CompleteAction(Guid id, ActionStatus finalStatus, TimeSpan displayDuration)
    {
        TrackedAction? action;
        lock (_lock)
        {
            action = _activeActions.FirstOrDefault(a => a.Id == id);
        }

        if (action == null) return;

        action.Status = finalStatus;

        Task.Delay(displayDuration).ContinueWith(_ =>
        {
            TrackedAction? actionToRemove;
            lock (_lock)
            {
                actionToRemove = _activeActions.FirstOrDefault(a => a.Id == id);
                if (actionToRemove != null)
                {
                    _activeActions.Remove(actionToRemove);
                }
            }
            if (actionToRemove != null)
            {
                ActionRemoved?.Invoke(actionToRemove);
            }
        });
    }
}