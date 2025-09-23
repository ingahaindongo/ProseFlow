using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using ProseFlow.Application.DTOs;
using ProseFlow.Application.DTOs.Models;
using ProseFlow.UI.Models;
using Action = ProseFlow.Core.Models.Action;

namespace ProseFlow.UI.Services;

public interface IDialogService
{
    /// <summary>
    /// Opens a file dialog to select a file for opening.
    /// </summary>
    /// <param name="title">The title of the dialog window.</param>
    /// <param name="filterName">The name of the file filter (e.g., "JSON files").</param>
    /// <param name="filterExtensions">The file extensions for the filter (e.g., "*.json").</param>
    /// <returns>The path to the selected file, or null if cancelled.</returns>
    Task<string?> ShowOpenFileDialogAsync(string title, string filterName, params string[] filterExtensions);
    
    /// <summary>
    /// Opens a URL, file, or folder in the default associated application.
    /// </summary>
    /// <param name="url">The URL, file, or folder to open.
    /// Can be a web URL, or a local file or folder path.</param>
    Task OpenUrlAsync(string url);

    /// <summary>
    /// Opens a file dialog to select a path for saving a file.
    /// </summary>
    /// <param name="title">The title of the dialog window.</param>
    /// <param name="defaultFileName">The default name for the file.</param>
    /// <param name="filterName">The name of the file filter.</param>
    /// <param name="filterExtensions">The file extensions for the filter.</param>
    /// <returns>The path to save the file, or null if cancelled.</returns>
    Task<string?> ShowSaveFileDialogAsync(string title, string defaultFileName, string filterName, params string[] filterExtensions);

    /// <summary>
    /// Shows the Action Editor dialog window.
    /// </summary>
    /// <param name="action">The action to edit. Pass a new Action to create one.</param>
    /// <returns>True if the action was saved; otherwise, false.</returns>
    Task<bool> ShowActionEditorDialogAsync(Action action);

    /// <summary>
    /// Shows a confirmation message dialog with synchronous actions.
    /// </summary>
    /// <param name="title">The title of the dialog.</param>
    /// <param name="message">The message to display to the user.</param>
    /// <param name="onConfirm">The action to perform if the user confirms.</param>
    /// <param name="onCancel">The action to perform if the user cancels.</param>
    /// <returns>True if the user confirmed; otherwise, false.</returns>
    void ShowConfirmationDialog(string title, string message, System.Action? onConfirm = null, System.Action? onCancel = null);
    
    /// <summary>
    /// Shows a confirmation message dialog with asynchronous actions.
    /// </summary>
    /// <param name="title">The title of the dialog.</param>
    /// <param name="message">The message to display to the user.</param>
    /// <param name="onConfirm">The asynchronous action to perform if the user confirms.</param>
    /// <param name="onCancel">The asynchronous action to perform if the user cancels.</param>
    /// <returns>True if the user confirmed; otherwise, false.</returns>
    void ShowConfirmationDialogAsync(string title, string message, Func<Task>? onConfirm = null, Func<Task>? onCancel = null);
    
    /// <summary>
    /// Shows a dialog to get a single string input from the user.
    /// </summary>
    /// <param name="title">The title of the dialog.</param>
    /// <param name="message">The message/prompt to display to the user.</param>
    /// <param name="confirmButtonText">The text for the confirmation button (e.g., "Create", "Rename").</param>
    /// <param name="initialValue">An optional initial value for the input box.</param>
    /// <returns>A result object indicating success and the entered text.</returns>
    Task<InputDialogResult> ShowInputDialogAsync(string title, string message, string confirmButtonText, string? initialValue = null);
    
    
    /// <summary>
    /// Shows a model library dialog with the given model library view model.
    /// </summary>
    Task ShowModelLibraryDialogAsync();
    
    /// <summary>
    /// Shows the downloads popup view to monitor current downloads.
    /// </summary>
    void ShowDownloadsDialog();
    
    /// <summary>
    /// Shows a dialog to import a custom GGUF model from the local filesystem.
    /// </summary>
    /// <returns>A result object containing the model's metadata and source path, or null if cancelled.</returns>
    Task<CustomModelImportData?> ShowImportModelDialogAsync();

    /// <summary>
    /// Shows a dialog for resolving conflicts when importing actions.
    /// </summary>
    /// <param name="conflicts">A list of detected conflicts.</param>
    /// <returns>A list of conflicts with user-selected resolutions, or null if cancelled.</returns>
    Task<List<ActionConflict>?> ShowConflictResolutionDialogAsync(List<ActionConflict> conflicts);
    
    /// <summary>
    /// Shows a critical, blocking confirmation dialog. This is intended for startup errors (e.g., database corruption)
    /// where the main application window may not be fully initialized, and a simple, native dialog is required.
    /// </summary>
    /// <param name="owner">The owner window for the modal dialog.</param>
    /// <param name="title">The title of the dialog window.</param>
    /// <param name="message">The message to display to the user.</param>
    /// <param name="confirmText">The text for the confirmation (positive) button.</param>
    /// <param name="cancelText">The text for the cancellation (negative) button.</param>
    /// <returns>A task that resolves to `true` if the user confirmed, and `false` otherwise.</returns>
    Task<bool> ShowCriticalConfirmationDialogAsync(Window owner, string title, string message, string confirmText, string cancelText);
}