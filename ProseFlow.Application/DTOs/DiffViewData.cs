namespace ProseFlow.Application.DTOs;

/// <summary>
/// A Data Transfer Object to carry all necessary information for displaying the Diff View Window.
/// </summary>
/// <param name="ActionName">The name of the action performed, for the window title.</param>
/// <param name="OriginalText">The user's original, selected text.</param>
/// <param name="GeneratedText">The AI-generated text to be compared.</param>
public record DiffViewData(string ActionName, string OriginalText, string GeneratedText);