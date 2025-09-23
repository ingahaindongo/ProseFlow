using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProseFlow.Application.DTOs.Models;
using ProseFlow.Application.Events;
using ProseFlow.UI.Services;
using Window = ShadUI.Window;

namespace ProseFlow.UI.ViewModels.Dialogs;

public partial class CustomModelImportViewModel(IDialogService dialogService) : ViewModelBase
{
    internal readonly TaskCompletionSource<CustomModelImportData?> CompletionSource = new();

    [ObservableProperty]
    private string _modelName = string.Empty;

    [ObservableProperty]
    private string _creator = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private string _sourceGgufPath = string.Empty;

    [RelayCommand]
    private async Task BrowseFileAsync()
    {
        var filePath = await dialogService.ShowOpenFileDialogAsync("Select GGUF Model", "GGUF Files", "*.gguf");
        if (!string.IsNullOrWhiteSpace(filePath)) SourceGgufPath = filePath;
    }

    [RelayCommand]
    private void Import(Window window)
    {
        if (string.IsNullOrWhiteSpace(ModelName) || string.IsNullOrWhiteSpace(SourceGgufPath))
        {
            AppEvents.RequestNotification("Model Name and File Path cannot be empty.", NotificationType.Warning);
            return;
        }

        var result = new CustomModelImportData(ModelName, Creator, Description, SourceGgufPath);
        CompletionSource.TrySetResult(result);
        window.Close();
    }

    [RelayCommand]
    private void Cancel(Window window)
    {
        CompletionSource.TrySetResult(null);
        window.Close();
    }
}