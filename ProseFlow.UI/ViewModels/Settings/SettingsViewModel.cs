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
using ProseFlow.Core.Interfaces;
using ProseFlow.Core.Models;
using Action = ProseFlow.Core.Models.Action;

namespace ProseFlow.UI.ViewModels.Settings;

public partial class PresetViewModel(PresetDto preset) : ViewModelBase
{
    [ObservableProperty]
    private bool _isImported;
    
    public PresetDto Preset { get; } = preset;
}

public partial class SettingsViewModel(
    SettingsService settingsService, 
    ActionManagementService actionService,
    IPresetService presetService,
    IOsService osService) : ViewModelBase
{
    public override string Title => "Settings";
    public override IconSymbol Icon => IconSymbol.Settings;

    [ObservableProperty]
    private GeneralSettings? _settings;

    [ObservableProperty]
    private bool _hasHotkeyConflict;

    public ObservableCollection<Action> AvailableActions { get; } = [];
    public ObservableCollection<PresetViewModel> AvailablePresets { get; } = [];
    public List<string> AvailableThemes => Enum.GetNames(typeof(ThemeType)).ToList();
    
    [ObservableProperty]
    private Action? _selectedSmartPasteAction;
    
    [ObservableProperty]
    private string _selectedTheme = nameof(ThemeType.System);

    partial void OnSettingsChanged(GeneralSettings? value)
    {
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
        Settings = await settingsService.GetGeneralSettingsAsync();
        SelectedTheme = Settings.Theme;
    }
    
    private async Task LoadActionsAsync()
    {
        AvailableActions.Clear();
        var actions = await actionService.GetActionsAsync();
        foreach (var action in actions) AvailableActions.Add(action);
        SelectedSmartPasteAction = AvailableActions.FirstOrDefault(a => a.Id == Settings?.SmartPasteActionId);
    }
    
    private async Task LoadPresetsAsync()
    {
        AvailablePresets.Clear();
        var presets = await presetService.GetAvailablePresetsAsync();
        var existingGroupsWithActions = await actionService.GetActionGroupsWithActionsAsync();
        
        var existingActionsByGroup = existingGroupsWithActions
            .ToDictionary(g => g.Name, 
                          g => g.Actions.Select(a => a.Name).ToHashSet(StringComparer.OrdinalIgnoreCase),
                          StringComparer.OrdinalIgnoreCase);

        foreach (var presetDto in presets)
        {
            var presetVm = new PresetViewModel(presetDto);
            
            if (existingActionsByGroup.TryGetValue(presetDto.Name, out var existingActionNames))
            {
                var presetActionNames = await presetService.GetActionNamesFromPresetAsync(presetDto.ResourcePath);
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
            await presetService.ImportPresetAsync(presetVm.Preset.ResourcePath);
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
        
        ValidateHotkeys();
        if (HasHotkeyConflict)
        {
            AppEvents.RequestNotification("Cannot save with conflicting hotkeys.", NotificationType.Error);
            return;
        }

        osService.SetLaunchAtLogin(Settings.LaunchAtLogin);
        osService.UpdateHotkeys(Settings.ActionMenuHotkey, Settings.SmartPasteHotkey);
        await settingsService.SaveGeneralSettingsAsync(Settings);
        AppEvents.RequestNotification("Settings saved successfully.", NotificationType.Success);
    }
    
    public void ValidateHotkeys()
    {
        if (Settings is null)
        {
            HasHotkeyConflict = false;
            return;
        }

        HasHotkeyConflict = !string.IsNullOrWhiteSpace(Settings.ActionMenuHotkey) &&
                            Settings.ActionMenuHotkey.Equals(Settings.SmartPasteHotkey, StringComparison.OrdinalIgnoreCase);
    }
}