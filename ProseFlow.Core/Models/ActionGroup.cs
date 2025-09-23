using ProseFlow.Core.Abstracts;

namespace ProseFlow.Core.Models;

/// <summary>
/// Represents a user-defined group for organizing actions.
/// </summary>
public class ActionGroup : EntityBase
{
    /// <summary>
    /// The user-facing name of the group (e.g., "Writing", "Coding").
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// The display order of the group in the settings UI and Floating Action Menu.
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// The collection of actions belonging to this group.
    /// </summary>
    public ICollection<Action> Actions { get; set; } = new List<Action>();
}