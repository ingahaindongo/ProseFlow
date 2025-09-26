using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using ProseFlow.Application.DTOs;
using ProseFlow.Application.Events;
using TextCopy;

namespace ProseFlow.UI.ViewModels.Windows;

public partial class DiffViewModel(DiffViewData data) : ViewModelBase
{
    internal readonly TaskCompletionSource<DiffViewResult?> CompletionSource = new();

    [ObservableProperty] private string _actionName = data.ActionName;
    [ObservableProperty] private string _originalText = data.OriginalText;
    [ObservableProperty] private string _generatedText = data.GeneratedText;
    
    [ObservableProperty] private SideBySideDiffModel _diffModel = ProcessDiff(data);

    [ObservableProperty] private string _refinementInstruction = string.Empty;
    [ObservableProperty] private bool _isRefining;
    
    /// <summary>
    /// Processes the original and new text to create a side-by-side diff model,
    /// and then enhances the "Old Text" pane with word-level sub-piece highlighting.
    /// </summary>
    private static SideBySideDiffModel ProcessDiff(DiffViewData data)
    {
        var sideBySideBuilder = new SideBySideDiffBuilder(new Differ());
        var diffModel = sideBySideBuilder.BuildDiffModel(data.OriginalText, data.GeneratedText);

        var inlineBuilder = new InlineDiffBuilder(new Differ());

        // Iterate through the lines to find modifications and generate sub-pieces for the old text.
        for (var i = 0; i < diffModel.OldText.Lines.Count; i++)
        {
            var oldLine = diffModel.OldText.Lines[i];
            
            // A modification is represented as a deletion on the left and an insertion on the right.
            if (i < diffModel.NewText.Lines.Count && oldLine.Type == ChangeType.Deleted)
            {
                var newLine = diffModel.NewText.Lines[i];
                if (newLine.Type == ChangeType.Inserted)
                {
                    // This is a modified line. Run an inline diff on it.
                    var inlineDiff = inlineBuilder.BuildDiffModel(oldLine.Text, newLine.Text);
                    
                    // Filter out the insertions, as we only want to show deletions and unchanged parts on the left side.
                    var oldTextSubPieces = inlineDiff.Lines
                        .Where(p => p.Type != ChangeType.Inserted)
                        .Select(p => new DiffPiece(p.Text, p.Type, p.Position))
                        .ToList();

                    // If there are any changes, assign the new sub-pieces.
                    if (oldTextSubPieces.Any(p => p.Type != ChangeType.Unchanged))
                    {
                        oldLine.SubPieces = oldTextSubPieces;
                    }
                }
            }
        }
        
        return diffModel;
    }
    
    [RelayCommand]
    private void Accept(Window window)
    {
        CompletionSource.TrySetResult(new Accepted(GeneratedText));
        window.Close();
    }

    [RelayCommand]
    private void Regenerate(Window window)
    {
        CompletionSource.TrySetResult(new Regenerated());
        window.Close();
    }

    [RelayCommand]
    private void ToggleRefine()
    {
        IsRefining = !IsRefining;
    }

    [RelayCommand]
    private void SubmitRefinement(Window window)
    {
        if (string.IsNullOrWhiteSpace(RefinementInstruction)) return;
        CompletionSource.TrySetResult(new Refined(RefinementInstruction));
        window.Close();
    }

    [RelayCommand]
    private async Task CopyAsync()
    {
        await ClipboardService.SetTextAsync(GeneratedText);
        AppEvents.RequestNotification("Copied to clipboard.", NotificationType.Success);
    }
    
    [RelayCommand]
    private void Cancel(Window window)
    {
        CompletionSource.TrySetResult(new Cancelled());
        window.Close();
    }
}