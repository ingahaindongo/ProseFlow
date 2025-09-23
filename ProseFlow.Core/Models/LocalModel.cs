using ProseFlow.Core.Abstracts;

namespace ProseFlow.Core.Models;

/// <summary>
/// Represents a local GGUF model whose metadata is stored in the database.
/// </summary>
public class LocalModel : EntityBase
{
    /// <summary>
    /// The user-facing name of the model.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// The creator or author of the model (e.g., "Meta", "Google").
    /// </summary>
    public string Creator { get; set; } = string.Empty;

    /// <summary>
    /// A user-provided description of the model.
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// A tag for the model, e.g., "Recommended", "Experimental", "Code".
    /// </summary>
    public string Tag { get; set; } = string.Empty;

    /// <summary>
    /// The absolute path to the .gguf file on the user's filesystem.
    /// </summary>
    public required string FilePath { get; set; }

    /// <summary>
    /// The size of the model file in gigabytes.
    /// </summary>
    public double FileSizeGb { get; set; }

    /// <summary>
    /// If true, this model resides in the application's managed models directory,
    /// and the application is responsible for its file lifecycle (e.g., deletion).
    /// If false, this is a link to an external file that the app will not delete.
    /// </summary>
    public bool IsManaged { get; set; }

    /// <summary>
    /// The date and time when the model was added to the library.
    /// </summary>
    public DateTime AddedAt { get; set; }
}