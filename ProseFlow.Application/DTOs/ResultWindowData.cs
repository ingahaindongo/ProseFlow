namespace ProseFlow.Application.DTOs;

/// <summary>
/// A Data Transfer Object to carry all necessary information for displaying the Result Window.
/// </summary>
/// <param name="ActionName">The name of the action performed, for the window title.</param>
/// <param name="MainContent">The primary AI-generated output in Markdown format.</param>
/// <param name="ExplanationContent">The optional explanation of changes, also in Markdown format. Null if not provided.</param>
public record ResultWindowData(
    string ActionName,
    string MainContent,
    string? ExplanationContent = null);