namespace ProseFlow.Application.DTOs;

/// <summary>
/// Represents the user's decision from the Diff View window.
/// This is a discriminated union pattern.
/// </summary>
public abstract record DiffViewResult;

/// <summary>
/// The user accepted the changes. The new text should be pasted.
/// </summary>
public record Accepted(string NewText) : DiffViewResult;

/// <summary>
/// The user wants to refine the output with a new instruction.
/// </summary>
public record Refined(string RefinementInstruction) : DiffViewResult;

/// <summary>
/// The user wants to re-run the original action to get a new suggestion.
/// </summary>
public record Regenerated : DiffViewResult;

/// <summary>
/// The user closed the window or cancelled the operation.
/// </summary>
public record Cancelled : DiffViewResult;