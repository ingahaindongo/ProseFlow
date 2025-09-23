using ProseFlow.Core.Abstracts;
using ProseFlow.Core.Enums;

namespace ProseFlow.Core.Models;

/// <summary>
/// Represents a single, user-configured cloud provider instance (e.g., an OpenAI account, a Groq account).
/// </summary>
public class CloudProviderConfiguration : EntityBase
{
    /// <summary>
    /// A user-defined name for this configuration, e.g., "My Personal OpenAI".
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// The type of the provider, which determines how to interact with its API.
    /// </summary>
    public ProviderType ProviderType { get; set; } = ProviderType.OpenAi;

    /// <summary>
    /// Whether this provider configuration is active and can be used in the fallback chain.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// The API Key for this provider. This will be encrypted when stored in the database.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// An optional base URL for custom or self-hosted endpoints compatible with a provider's API.
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// The specific model to use for this configuration, e.g., "gpt-4o", "llama3-70b-8192".
    /// </summary>
    public string Model { get; set; } = string.Empty;

    /// <summary>
    /// The temperature setting for this provider, controlling randomness (0.0 to 2.0).
    /// </summary>
    public float Temperature { get; set; } = 0.7f;
    
    /// <summary>
    /// The display and fallback order of the provider. Lower numbers are tried first.
    /// </summary>
    public int SortOrder { get; set; }
}