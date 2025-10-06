namespace ProseFlow.Core.Models;

/// <summary>
/// A record that persists the state of the workspace connection to a local file.
/// </summary>
/// <param name="SharedPath">The absolute path to the shared workspace folder.</param>
/// <param name="LastSyncTimestamp">The UTC timestamp of the last successful sync operation.</param>
public record WorkspaceState(string? SharedPath, DateTime? LastSyncTimestamp);