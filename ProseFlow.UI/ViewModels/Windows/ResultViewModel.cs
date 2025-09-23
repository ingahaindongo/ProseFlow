using System.Threading.Tasks;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProseFlow.Application.DTOs;
using ProseFlow.Application.Events;
using TextCopy;

namespace ProseFlow.UI.ViewModels.Windows;

public partial class ResultViewModel(ResultWindowData data) : ViewModelBase
{
    [ObservableProperty]
    private string _actionName = data.ActionName;

    [ObservableProperty]
    private string _mainContent = data.MainContent;

    [ObservableProperty]
    private string? _explanationContent = data.ExplanationContent;

    [ObservableProperty]
    private bool _isExplanationVisible = !string.IsNullOrWhiteSpace(data.ExplanationContent);
    
    [ObservableProperty]
    private string _refinementInstruction = string.Empty;

    internal readonly TaskCompletionSource<RefinementRequest?> CompletionSource = new();
    internal bool IsRefinement;
    
    [RelayCommand]
    private void Refine(Window window)
    {
        if (string.IsNullOrWhiteSpace(RefinementInstruction)) return;

        IsRefinement = true;
        var request = new RefinementRequest(RefinementInstruction);
        CompletionSource.SetResult(request);
        window.Close();
    }


    [RelayCommand]
    private async Task CopyAsync()
    {
        await ClipboardService.SetTextAsync(MainContent);
        AppEvents.RequestNotification("Copied to clipboard.", NotificationType.Success);
    }
    
    [RelayCommand]
    private void Close(Window window)
    {
        CompletionSource.TrySetResult(null); // Signal "end of session"
        window.Close();
    }
}