using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using ProseFlow.Application.DTOs;
using ProseFlow.Application.Interfaces;
using ProseFlow.Application.Services;
using ProseFlow.Core.Enums;
using ProseFlow.UI.Views.Windows;

namespace ProseFlow.UI.ViewModels.Windows;

/// <summary>
/// ViewModel for the FloatingOrbWindow, providing commands and state management for the Orb.
/// </summary>
public partial class FloatingOrbViewModel : ViewModelBase, IDisposable
{
    private readonly ActionOrchestrationService _actionOrchestrationService;
    private readonly IServiceProvider _serviceProvider;
    private readonly IBackgroundActionTrackerService _trackerService;
    private bool _isFlashing;

    [ObservableProperty]
    private ActionProcessingState _state = ActionProcessingState.Idle;

    [ObservableProperty]
    private int _activeActionCount;

    [ObservableProperty]
    private bool _isBadgeVisible;

    public FloatingOrbViewModel(
        ActionOrchestrationService actionOrchestrationService,
        IServiceProvider serviceProvider,
        IBackgroundActionTrackerService trackerService)
    {
        _actionOrchestrationService = actionOrchestrationService;
        _serviceProvider = serviceProvider;
        _trackerService = trackerService;

        // Populate initial state and subscribe to future changes
        foreach (var action in _trackerService.GetActiveActions())
        {
            action.PropertyChanged += OnTrackedActionPropertyChanged;
        }
        _trackerService.ActionAdded += OnActionAdded;
        _trackerService.ActionRemoved += OnActionRemoved;

        UpdateCount();
    }

    private void OnActionAdded(TrackedAction action)
    {
        Dispatcher.UIThread.Post(() =>
        {
            action.PropertyChanged += OnTrackedActionPropertyChanged;
            UpdateCount();
            UpdateOrbState();
        });
    }

    private void OnActionRemoved(TrackedAction action)
    {
        Dispatcher.UIThread.Post(() =>
        {
            action.PropertyChanged -= OnTrackedActionPropertyChanged;
            UpdateCount();
            UpdateOrbState();
        });
    }

    private async void OnTrackedActionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(TrackedAction.Status) || sender is not TrackedAction action) return;

        if (action.Status is ActionStatus.Success or ActionStatus.Error)
        {
            await FlashStateAsync(action.Status == ActionStatus.Success
                ? ActionProcessingState.Success
                : ActionProcessingState.Error);
        }
    }

    private async Task FlashStateAsync(ActionProcessingState flashState)
    {
        _isFlashing = true;
        State = flashState;
        await Task.Delay(1500);
        _isFlashing = false;
        UpdateOrbState();
    }

    private void UpdateCount()
    {
        if (_trackerService.GetActiveActions() is List<TrackedAction> actions) 
            ActiveActionCount = actions.Count;
        IsBadgeVisible = ActiveActionCount > 0;
    }

    private void UpdateOrbState()
    {
        if (_isFlashing) return;

        State = _trackerService.GetActiveActions().Any(a => a.Status is ActionStatus.Processing or ActionStatus.Queued)
            ? ActionProcessingState.Processing
            : ActionProcessingState.Idle;
    }
    
    /// <summary>
    /// Hides the floating orb and triggers the display of the Floating Action Menu.
    /// </summary>
    [RelayCommand]
    private void TriggerActionMenu()
    {
        _ = _actionOrchestrationService.HandleActionMenuHotkeyAsync();
    }

    /// <summary>
    /// Shows the Orb's context menu.
    /// </summary>
    [RelayCommand]
    private void ShowContextMenu(FloatingOrbWindow owner)
    {
        var menuViewModel = _serviceProvider.GetService<FloatingOrbMenuViewModel>();
        var menuView = new FloatingOrbMenuView
        {
            DataContext = menuViewModel,
        };

        menuView.Show(owner);
    }

    public void Dispose()
    {
        _trackerService.ActionAdded -= OnActionAdded;
        _trackerService.ActionRemoved -= OnActionRemoved;
        foreach (var action in _trackerService.GetActiveActions())
        {
            action.PropertyChanged -= OnTrackedActionPropertyChanged;
        }
        GC.SuppressFinalize(this);
    }
}