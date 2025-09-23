using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProseFlow.Application.DTOs.Models;
using ProseFlow.Application.Events;
using ProseFlow.Application.Interfaces;
using ProseFlow.Application.Services;
using ProseFlow.UI.Services;
using ProseFlow.UI.ViewModels.Downloads;

namespace ProseFlow.UI.ViewModels.Providers;

public partial class ModelLibraryViewModel(
    IModelCatalogService catalogService,
    ILocalModelManagementService localModelService,
    IDownloadManager downloadManager,
    SettingsService settingsService,
    IDialogService dialogService) : ViewModelBase, IDisposable
{
    [ObservableProperty]
    private int _selectedTabIndex;
    
    [ObservableProperty]
    private bool _isLoading = true;

    [ObservableProperty]
    private bool _isOnboardingMode;

    [ObservableProperty]
    private LocalModelViewModel? _selectedModel;
    
    public ObservableCollection<AvailableModelViewModel> AvailableModels { get; } = [];
    public ObservableCollection<LocalModelViewModel> LocalModels { get; } = [];

    public override async Task OnNavigatedToAsync()
    {
        localModelService.ModelsChanged += OnLocalOrDownloadsChanged;
        downloadManager.DownloadsChanged += OnLocalOrDownloadsChanged;
        
        await LoadLocalModelsAsync();
        await LoadAvailableModelsAsync();
        SyncStates();
    }

    private async void OnLocalOrDownloadsChanged()
    {
        await LoadLocalModelsAsync();
        SyncStates();
    }
    
    private async Task LoadAvailableModelsAsync()
    {
        IsLoading = true;
        
        foreach (var vm in AvailableModels) vm.Dispose();
        AvailableModels.Clear();

        var catalog = await catalogService.GetAvailableModelsAsync();
        foreach (var entry in catalog)
        {
            var vm = new AvailableModelViewModel(entry, downloadManager, () => SelectedTabIndex = 1);
            AvailableModels.Add(vm);
        }
        
        IsLoading = false;
    }

    private async Task LoadLocalModelsAsync()
    {
        LocalModels.Clear();
        var modelsWithStatus = await localModelService.GetModelsWithStatusAsync();
        var settings = await settingsService.GetProviderSettingsAsync();
        
        foreach (var entry in modelsWithStatus)
        {
            var vm = new LocalModelViewModel(entry.Model, entry.IsMissing, localModelService, settingsService, dialogService, RelocateModelAsync);
            LocalModels.Add(vm);
        }
        
        var modelToSelect = LocalModels.FirstOrDefault(m => !m.IsMissing && m.Model.FilePath == settings.LocalModelPath);
        if (modelToSelect != null) await SelectLocalModelAsync(modelToSelect);
    }
    
    /// <summary>
    /// This is the core synchronization logic.
    /// It ensures the state of each "Available" card reflects reality.
    /// </summary>
    private void SyncStates()
    {
        var localModelPaths = new HashSet<string>(LocalModels.Select(m => m.Model.FilePath));

        foreach (var availableVm in AvailableModels)
        {
            availableVm.ActiveDownloadTask = null;

            // Check if any quantization of this model is already installed
            var isInstalled = availableVm.Model.Quantizations
                .Any(q => localModelPaths.Contains(localModelService.GetManagedModelsDirectory() + Path.DirectorySeparatorChar + q.FileName));
            
            if (isInstalled)
            {
                availableVm.CurrentState = DownloadCardState.Installed;
                continue;
            }

            // Check if any quantization of this model is currently being downloaded
            var activeTask = downloadManager.AllDownloads
                .FirstOrDefault(t => t.Model.Id == availableVm.Model.Id && t.Status is DownloadStatus.Downloading or DownloadStatus.Queued or DownloadStatus.Paused);

            if (activeTask is not null)
                availableVm.ActiveDownloadTask = activeTask; // Link the live task to the view model
            else
                availableVm.CurrentState = DownloadCardState.NotDownloaded; // If not installed and not downloading, it's available
        }
    }

    partial void OnSelectedModelChanged(LocalModelViewModel? value)
    {
        OnPropertyChanged(nameof(IsAModelSelected));
    }
    
    public bool IsAModelSelected => SelectedModel is not null;

    [RelayCommand]
    private async Task ImportToLibraryAsync()
    {
        var importData = await dialogService.ShowImportModelDialogAsync();
        if (importData is null) return;

        try
        {
            await localModelService.ImportManagedModelAsync(importData);
            AppEvents.RequestNotification("Model imported to library successfully.", NotificationType.Success);
        }
        catch (Exception ex)
        {
            AppEvents.RequestNotification($"Failed to import model: {ex.Message}", NotificationType.Error);
        }
    }
    
    [RelayCommand]
    private async Task LinkExternalModelAsync()
    {
        var importData = await dialogService.ShowImportModelDialogAsync();
        if (importData is null) return;

        try
        {
            await localModelService.LinkExternalModelAsync(importData);
            AppEvents.RequestNotification("Model linked successfully.", NotificationType.Success);
        }
        catch (Exception ex)
        {
            AppEvents.RequestNotification($"Failed to link model: {ex.Message}", NotificationType.Error);
        }
    }
    
    [RelayCommand]
    private async Task SelectLocalModelAsync(LocalModelViewModel model)
    {
        await model.Select();
        foreach (var otherModel in LocalModels) otherModel.IsSelected = otherModel == model;
        
        SelectedModel = model;
    }
    
    private async Task RelocateModelAsync(LocalModelViewModel modelVm)
    {
        var newPath = await dialogService.ShowOpenFileDialogAsync($"Locate '{modelVm.Model.Name}'", "GGUF File", "*.gguf");
        if (string.IsNullOrWhiteSpace(newPath)) return;

        await localModelService.UpdateModelPathAsync(modelVm.Model.Id, newPath);
        AppEvents.RequestNotification("Model path updated successfully.", NotificationType.Success);
        
        // Refresh the list to reflect the new state
        await LoadLocalModelsAsync();
    }

    public void OnClosing()
    {
        localModelService.ModelsChanged -= OnLocalOrDownloadsChanged;
        downloadManager.DownloadsChanged -= OnLocalOrDownloadsChanged;
        foreach (var vm in AvailableModels) vm.Dispose();
    }

    public void Dispose()
    {
        OnClosing();
        GC.SuppressFinalize(this);
    }
}