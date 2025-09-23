using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProseFlow.Application.Interfaces;
using ProseFlow.Application.Services;
using ProseFlow.Core.Models;
using ProseFlow.UI.Services;

namespace ProseFlow.UI.ViewModels.Downloads;

public partial class LocalModelViewModel(
    LocalModel model,
    bool isMissing,
    ILocalModelManagementService localModelService,
    SettingsService settingsService,
    IDialogService dialogService,
    Func<LocalModelViewModel, Task> relocateAction) : ViewModelBase
{
    [ObservableProperty] private LocalModel _model = model;
    [ObservableProperty] private bool _isSelected;
    [ObservableProperty] private bool _isMissing = isMissing;

    [RelayCommand]
    private async Task RelocateAsync()
    {
        await relocateAction(this);
    }

    [RelayCommand]
    internal async Task Select()
    {
        var settings = await settingsService.GetProviderSettingsAsync();
        settings.LocalModelPath = Model.FilePath;
        await settingsService.SaveProviderSettingsAsync(settings);
        IsSelected = true;
    }

    [RelayCommand]
    internal void Delete()
    {
        var confirmationMessage = IsMissing
            ? $"This will remove the invalid entry for '{Model.Name}' from your library. Are you sure?"
            : Model.IsManaged
                ? $"This will delete the model from your library and free up {Model.FileSizeGb:F1} GB of disk space. Are you sure?"
                : "This will remove the link to this model from your library. The original file will not be affected. Are you sure?";

        dialogService.ShowConfirmationDialogAsync(
            $"Delete '{Model.Name}'?",
            confirmationMessage,
            async () => await localModelService.DeleteModelAsync(Model.Id)
        );
    }
}