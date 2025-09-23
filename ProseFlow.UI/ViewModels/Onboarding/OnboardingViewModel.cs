using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using ProseFlow.Application.DTOs;
using ProseFlow.Application.Events;
using ProseFlow.Application.Interfaces;
using ProseFlow.Application.Services;
using ProseFlow.Core.Interfaces;
using ProseFlow.Core.Models;
using ProseFlow.UI.ViewModels.Providers;
using ShadUI;

namespace ProseFlow.UI.ViewModels.Onboarding;

public enum OnboardingStep
{
    Welcome,
    ProviderChoice,
    CloudSetup,
    LocalSetup,
    ActionsIntro,
    HotkeySetup,
    HotkeyTutorial,
    Graduation
}

public partial class PresetOnboardingViewModel(PresetDto preset) : ViewModelBase
{
    [ObservableProperty]
    private bool _isSelected = true; // Default to selected
    public PresetDto Preset { get; } = preset;
}

public partial class OnboardingViewModel(
    IServiceProvider serviceProvider,
    SettingsService settingsService,
    CloudProviderManagementService cloudProviderService,
    IPresetService presetService,
    IOsService osService) : ViewModelBase
{
    [ObservableProperty]
    private OnboardingStep _currentStep = OnboardingStep.Welcome;

    [ObservableProperty]
    private ViewModelBase? _currentContentViewModel;

    // Data collected during onboarding
    public CloudProviderConfiguration? CloudProviderConfig { get; set; }
    public string? LocalModelPath { get; set; }
    public bool LaunchAtLogin { get; set; } = true;
    [ObservableProperty]
    private string _actionMenuHotkey = "Ctrl+J";

    // Button Visibility and Enabled States
    [ObservableProperty]
    private bool _isBackButtonVisible;

    [ObservableProperty]
    private bool _isNextButtonEnabled = true;

    [ObservableProperty]
    private string _nextButtonText = "Continue";
    
    public ObservableCollection<PresetOnboardingViewModel> AvailablePresets { get; } = [];

    public OnboardingViewModel() : this(
        Ioc.Default.GetRequiredService<IServiceProvider>(), 
        Ioc.Default.GetRequiredService<SettingsService>(), 
        Ioc.Default.GetRequiredService<CloudProviderManagementService>(),
        Ioc.Default.GetRequiredService<IPresetService>(),
        Ioc.Default.GetRequiredService<IOsService>()) {}

    public async Task InitializeAsync()
    {
        var presets = await presetService.GetAvailablePresetsAsync();
        foreach (var preset in presets)
        {
            AvailablePresets.Add(new PresetOnboardingViewModel(preset));
        }
    }

    partial void OnCurrentStepChanged(OnboardingStep value)
    {
        UpdateStep(value);
    }

    private void UpdateStep(OnboardingStep newStep)
    {
        CurrentStep = newStep;
        IsBackButtonVisible = newStep > OnboardingStep.Welcome;
        NextButtonText = newStep == OnboardingStep.Graduation ? "Finish" : "Continue";

        // Unsubscribe from previous VM events and global events
        if (CurrentContentViewModel is IDisposable disposable) disposable.Dispose();
        if (CurrentContentViewModel is CloudOnboardingViewModel oldCloudVm) oldCloudVm.PropertyChanged -= OnContentViewModelPropertyChanged;
        if (CurrentContentViewModel is ModelLibraryViewModel oldLocalVm) oldLocalVm.PropertyChanged -= OnContentViewModelPropertyChanged;
        if (CurrentContentViewModel is HotkeyTutorialViewModel oldTutorialVm)
        {
            oldTutorialVm.PropertyChanged -= OnContentViewModelPropertyChanged;
            osService.ActionMenuHotkeyPressed -= OnTutorialHotkeyPressed; // Unsubscribe
        }

        // Set the new content view model and enable/disable next button
        switch (newStep)
        {
            case OnboardingStep.CloudSetup:
                var cloudVm = serviceProvider.GetRequiredService<CloudOnboardingViewModel>();
                cloudVm.PropertyChanged += OnContentViewModelPropertyChanged;
                CurrentContentViewModel = cloudVm;
                IsNextButtonEnabled = cloudVm.Status == TestStatus.Success;
                break;
            case OnboardingStep.LocalSetup:
                var localVm = serviceProvider.GetRequiredService<ModelLibraryViewModel>();
                localVm.IsOnboardingMode = true;
                localVm.PropertyChanged += OnContentViewModelPropertyChanged;
                _ = localVm.OnNavigatedToAsync();
                CurrentContentViewModel = localVm;
                IsNextButtonEnabled = localVm.IsAModelSelected;
                break;
            case OnboardingStep.HotkeyTutorial:
                var tutorialVm = serviceProvider.GetRequiredService<HotkeyTutorialViewModel>();
                tutorialVm.PropertyChanged += OnContentViewModelPropertyChanged;
                tutorialVm.ConfiguredHotkey = ActionMenuHotkey; // Pass the configured hotkey
                CurrentContentViewModel = tutorialVm;
                IsNextButtonEnabled = tutorialVm.IsCompleted;
                osService.ActionMenuHotkeyPressed += OnTutorialHotkeyPressed; // Subscribe
                break;
            default:
                CurrentContentViewModel = null; // For simple steps handled by DataTemplates
                IsNextButtonEnabled = true;
                break;
        }
    }

    private void OnTutorialHotkeyPressed()
    {
        // Ensure this only triggers the UI on the tutorial step.
        if (CurrentStep != OnboardingStep.HotkeyTutorial || CurrentContentViewModel is not HotkeyTutorialViewModel vm) return;
        
        // The event might come from a background thread, so dispatch to UI thread.
        Avalonia.Threading.Dispatcher.UIThread.Post(() => vm.ShowMenuCommand.Execute(null));
    }

    private void OnContentViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Dynamically enable the 'Next' button based on sub-viewmodel state
        IsNextButtonEnabled = sender switch
        {
            CloudOnboardingViewModel cloudVm when e.PropertyName == nameof(CloudOnboardingViewModel.Status) => cloudVm.Status == TestStatus.Success,
            ModelLibraryViewModel localVm when e.PropertyName == nameof(ModelLibraryViewModel.IsAModelSelected) => localVm.IsAModelSelected,
            HotkeyTutorialViewModel tutorialVm when e.PropertyName == nameof(HotkeyTutorialViewModel.IsCompleted) => tutorialVm.IsCompleted,
            _ => IsNextButtonEnabled
        };
    }

    [RelayCommand]
    private void NextStep(Window window)
    {
        switch (CurrentStep)
        {
            // Final step, close the dialog with a success result
            case OnboardingStep.Graduation:
                window.Close(true);
                return;
            // Handle branching from provider choice
            case OnboardingStep.ProviderChoice:
                // This is handled by choice-specific buttons in the view.
                return;
            // Before leaving setup steps, capture the data
            case OnboardingStep.CloudSetup when CurrentContentViewModel is CloudOnboardingViewModel cloudVm:
                CloudProviderConfig = cloudVm.GetConfiguration();
                break;
            case OnboardingStep.LocalSetup when CurrentContentViewModel is ModelLibraryViewModel localVm:
                LocalModelPath = localVm.SelectedModel?.Model.FilePath;
                break;
            case OnboardingStep.HotkeySetup:
                osService.UpdateHotkeys(ActionMenuHotkey, "Ctrl+Shift+V");
                break;
        }

        // Go to the next logical step
        var nextStep = CurrentStep switch
        {
            OnboardingStep.CloudSetup => OnboardingStep.ActionsIntro,
            OnboardingStep.LocalSetup => OnboardingStep.ActionsIntro,
            OnboardingStep.ActionsIntro => OnboardingStep.HotkeySetup,
            _ => CurrentStep + 1
        };

        UpdateStep(nextStep);
    }

    [RelayCommand]
    private void ChooseProviderPath(string path)
    {
        UpdateStep(path == "Cloud" ? OnboardingStep.CloudSetup : OnboardingStep.LocalSetup);
    }

    [RelayCommand]
    private void PreviousStep()
    {
        if (CurrentStep == OnboardingStep.Welcome) return;

        // If coming back from a setup path, go to provider choice
        var prevStep = CurrentStep switch
        {
            OnboardingStep.ActionsIntro => OnboardingStep.ProviderChoice,
            OnboardingStep.CloudSetup => OnboardingStep.ProviderChoice,
            OnboardingStep.LocalSetup => OnboardingStep.ProviderChoice,
            OnboardingStep.HotkeySetup => OnboardingStep.ActionsIntro,
            _ => CurrentStep - 1
        };

        UpdateStep(prevStep);
    }

    public async Task SaveSettingsAsync()
    {
        var generalSettings = await settingsService.GetGeneralSettingsAsync();
        var providerSettings = await settingsService.GetProviderSettingsAsync();

        generalSettings.LaunchAtLogin = LaunchAtLogin;
        generalSettings.ActionMenuHotkey = ActionMenuHotkey;
        osService.SetLaunchAtLogin(LaunchAtLogin);

        if (CloudProviderConfig is not null)
        {
            providerSettings.PrimaryServiceType = "Cloud";
            await cloudProviderService.CreateConfigurationAsync(CloudProviderConfig);
        }
        else if (!string.IsNullOrWhiteSpace(LocalModelPath))
        {
            providerSettings.PrimaryServiceType = "Local";
            providerSettings.LocalModelPath = LocalModelPath;
        }

        // Import selected presets
        var presetsToImport = AvailablePresets.Where(p => p.IsSelected).ToList();
        if (presetsToImport.Count > 0)
        {
            foreach (var presetVm in presetsToImport)
            {
                await presetService.ImportPresetAsync(presetVm.Preset.ResourcePath);
            }
            AppEvents.RequestNotification($"{presetsToImport.Count} preset group(s) imported!", NotificationType.Success);
        }

        await settingsService.SaveGeneralSettingsAsync(generalSettings);
        await settingsService.SaveProviderSettingsAsync(providerSettings);
    }
    
    public void OnClosing()
    {
        osService.ActionMenuHotkeyPressed -= OnTutorialHotkeyPressed;
    }
}