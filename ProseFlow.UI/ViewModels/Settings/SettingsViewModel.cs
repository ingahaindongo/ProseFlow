using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProseFlow.Application.DTOs;
using ProseFlow.UI.Utils;
using ProseFlow.Application.Events;
using ProseFlow.Application.Interfaces;
using ProseFlow.Application.Services;
using ProseFlow.Core.Enums;
using ProseFlow.Core.Interfaces.Os;
using ProseFlow.Core.Models;
using Action = ProseFlow.Core.Models.Action;

namespace ProseFlow.UI.ViewModels.Settings;

public partial class PresetViewModel(PresetDto preset) : ViewModelBase
{
    [ObservableProperty]
    private bool _isImported;
    
    public PresetDto Preset { get; } = preset;
}

public partial class SettingsViewModel : ViewModelBase, IDisposable
{
    public override string Title => "Settings";
    public override IconSymbol Icon => IconSymbol.Settings;

    private readonly SettingsService _settingsService;
    private readonly ActionManagementService _actionService;
    private readonly IPresetService _presetService;
    private readonly ISystemService _systemService;
    private readonly IHotkeyService _hotkeyService;
    private readonly IHotkeyRecordingService _recordingService;

    private const string RecordingPrompt = "Press a key combination...";

    [ObservableProperty]
    private GeneralSettings? _settings;

    [ObservableProperty]
    private bool _hasHotkeyConflict;

    // Properties to manage the text displayed in the hotkey text boxes
    [ObservableProperty]
    private string _actionMenuHotkeyText = string.Empty;

    [ObservableProperty]
    private string _smartPasteHotkeyText = string.Empty;
    
    // Properties to manage the recording state
    [ObservableProperty]
    private bool _isRecordingActionMenu;
    
    [ObservableProperty]
    private bool _isRecordingSmartPaste;

    public ObservableCollection<Action> AvailableActions { get; } = [];
    public ObservableCollection<PresetViewModel> AvailablePresets { get; } = [];
    public List<string> AvailableThemes => Enum.GetNames(typeof(ThemeType)).ToList();
    
    [ObservableProperty]
    private Action? _selectedSmartPasteAction;
    
    [ObservableProperty]
    private string _selectedTheme = nameof(ThemeType.System);
    
    public SettingsViewModel(
        SettingsService settingsService, 
        ActionManagementService actionService,
        IPresetService presetService,
        ISystemService systemService,
        IHotkeyService hotkeyService,
        IHotkeyRecordingService recordingService)
    {
        _settingsService = settingsService;
        _actionService = actionService;
        _presetService = presetService;
        _systemService = systemService;
        _hotkeyService = hotkeyService;
        _recordingService = recordingService;
        
        _recordingService.HotkeyDetected += OnHotkeyDetected;
        _recordingService.RecordingStateUpdated += OnRecordingStateUpdated;
    }

    partial void OnSettingsChanged(GeneralSettings? value)
    {
        if (value is null) return;
        ActionMenuHotkeyText = value.ActionMenuHotkey;
        SmartPasteHotkeyText = value.SmartPasteHotkey;
        ValidateHotkeys();
    }
    
    partial void OnSelectedSmartPasteActionChanged(Action? value)
    {
        if (Settings is null || value is null) return;
        Settings.SmartPasteActionId = value.Id;
    }
    
    partial void OnSelectedThemeChanged(string value)
    {
        if (Settings is null || Avalonia.Application.Current is null || value == Settings.Theme) return;
        
        Settings.Theme = value;
        Avalonia.Application.Current.RequestedThemeVariant = value switch
        {
            nameof(ThemeType.System) => ThemeVariant.Default,
            nameof(ThemeType.Light) => ThemeVariant.Light,
            nameof(ThemeType.Dark) => ThemeVariant.Dark,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };
        
        _ = SaveAsync();
    }
    
    public override async Task OnNavigatedToAsync()
    {
        await LoadSettingsAsync();
        await LoadActionsAsync();
        await LoadPresetsAsync();
    }
    
    private async Task LoadSettingsAsync()
    {
        Settings = await _settingsService.GetGeneralSettingsAsync();
        SelectedTheme = Settings.Theme;
    }
    
    private async Task LoadActionsAsync()
    {
        AvailableActions.Clear();
        var actions = await _actionService.GetActionsAsync();
        foreach (var action in actions) AvailableActions.Add(action);
        SelectedSmartPasteAction = AvailableActions.FirstOrDefault(a => a.Id == Settings?.SmartPasteActionId);
    }
    
    private async Task LoadPresetsAsync()
    {
        AvailablePresets.Clear();
        var presets = await _presetService.GetAvailablePresetsAsync();
        var existingGroupsWithActions = await _actionService.GetActionGroupsWithActionsAsync();
        
        var existingActionsByGroup = existingGroupsWithActions
            .ToDictionary(g => g.Name, 
                          g => g.Actions.Select(a => a.Name).ToHashSet(StringComparer.OrdinalIgnoreCase),
                          StringComparer.OrdinalIgnoreCase);

        foreach (var presetDto in presets)
        {
            var presetVm = new PresetViewModel(presetDto);
            
            if (existingActionsByGroup.TryGetValue(presetDto.Name, out var existingActionNames))
            {
                var presetActionNames = await _presetService.GetActionNamesFromPresetAsync(presetDto.ResourcePath);
                if (presetActionNames.Overlaps(existingActionNames)) 
                    presetVm.IsImported = true;
            }
            
            AvailablePresets.Add(presetVm);
        }
    }
    
    [RelayCommand]
    private async Task ImportPresetAsync(PresetViewModel presetVm)
    {
        try
        {
            await _presetService.ImportPresetAsync(presetVm.Preset.ResourcePath);
            AppEvents.RequestNotification($"'{presetVm.Preset.Name}' presets imported successfully!", NotificationType.Success);
            presetVm.IsImported = true;
        }
        catch (Exception)
        {
            AppEvents.RequestNotification($"Failed to import '{presetVm.Preset.Name}'.", NotificationType.Error);
        }
    }
    
    [RelayCommand]
    private async Task SaveAsync()
    {
        if (Settings is null) return;
        
        if (_recordingService.IsRecording) StopRecording();
        
        ValidateHotkeys();
        if (HasHotkeyConflict)
        {
            AppEvents.RequestNotification("Cannot save with conflicting hotkeys.", NotificationType.Error);
            return;
        }

        _systemService.SetLaunchAtLogin(Settings.LaunchAtLogin);
        _hotkeyService.UpdateHotkeys(Settings.ActionMenuHotkey, Settings.SmartPasteHotkey);
        await _settingsService.SaveGeneralSettingsAsync(Settings);
        AppEvents.RequestNotification("Settings saved successfully.", NotificationType.Success);
    }
    
    private void ValidateHotkeys()
    {
        if (Settings is null)
        {
            HasHotkeyConflict = false;
            return;
        }

        HasHotkeyConflict = !string.IsNullOrWhiteSpace(Settings.ActionMenuHotkey) &&
                            Settings.ActionMenuHotkey.Equals(Settings.SmartPasteHotkey, StringComparison.OrdinalIgnoreCase);
    }
    
    [RelayCommand]
    private void StartRecording(string hotkeyName)
    {
        StopRecording(); // Stop any other recording first

        if (hotkeyName == "ActionMenu")
        {
            IsRecordingActionMenu = true;
            ActionMenuHotkeyText = RecordingPrompt;
        }
        else if (hotkeyName == "SmartPaste")
        {
            IsRecordingSmartPaste = true;
            SmartPasteHotkeyText = RecordingPrompt;
        }

        _recordingService.BeginRecording();
    }
    
    [RelayCommand]
    private void StopRecording()
    {
        if (!_recordingService.IsRecording) return;
        
        IsRecordingActionMenu = false;
        IsRecordingSmartPaste = false;
        _recordingService.EndRecording();

        if (Settings is not null)
        {
            ActionMenuHotkeyText = Settings.ActionMenuHotkey;
            SmartPasteHotkeyText = Settings.SmartPasteHotkey;
        }
    }
    
    private void OnRecordingStateUpdated(string partialHotkeyText)
    {
        if (IsRecordingActionMenu) ActionMenuHotkeyText = partialHotkeyText;
        else if (IsRecordingSmartPaste) SmartPasteHotkeyText = partialHotkeyText;
    }

    private void OnHotkeyDetected(HotkeyData data)
    {
        if (Settings is null) return;

        var hotkeyString = FormatHotkeyData(data);
        
        if (IsRecordingActionMenu)
        {
            Settings.ActionMenuHotkey = hotkeyString;
            ActionMenuHotkeyText = hotkeyString;
        }
        else if (IsRecordingSmartPaste)
        {
            Settings.SmartPasteHotkey = hotkeyString;
            SmartPasteHotkeyText = hotkeyString;
        }

        ValidateHotkeys();
        StopRecording();
    }
    
    private string FormatHotkeyData(HotkeyData data)
    {
        var allParts = data.Modifiers.ToList();
        allParts.Add(data.Key);
        return string.Join("+", allParts);
    }
    
    public void Dispose()
    {
        _recordingService.HotkeyDetected -= OnHotkeyDetected;
        _recordingService.RecordingStateUpdated -= OnRecordingStateUpdated;
        GC.SuppressFinalize(this);
    }
}