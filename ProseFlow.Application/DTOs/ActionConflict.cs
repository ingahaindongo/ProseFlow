using Action = ProseFlow.Core.Models.Action;

namespace ProseFlow.Application.DTOs;

public enum ConflictResolutionType { Skip, Overwrite, Rename }

/// <summary>
/// A Data Transfer Object to represent a conflict between an imported action and an existing action.
/// </summary>
public record ActionConflict(
    Action ExistingAction,
    ActionDto ImportedActionDto,
    ConflictResolutionType Resolution = ConflictResolutionType.Skip);