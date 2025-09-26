using ProseFlow.Core.Enums;
using Action = ProseFlow.Core.Models.Action;

namespace ProseFlow.Application.DTOs;

/// <summary>
/// A request to execute a specific action with potential overrides.
/// </summary>
/// <param name="ActionToExecute">The action chosen by the user.</param>
/// <param name="Mode">The desired output mode for the result (e.g., InPlace, Windowed, Diff).</param>
/// <param name="ProviderOverride">Optional provider name to override the default for this single execution.</param>
public record ActionExecutionRequest(Action ActionToExecute, OutputMode Mode, string? ProviderOverride);