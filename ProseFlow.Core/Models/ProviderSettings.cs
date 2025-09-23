using ProseFlow.Core.Abstracts;

namespace ProseFlow.Core.Models;

/// <summary>
/// Stores settings related to AI providers, handling Local LLM settings and the global service type choice.
/// </summary>
public class ProviderSettings : EntityBase
{
    #region Local Model Settings

    /// <summary>
    /// The file path to the local model file (e.g., a .gguf file).
    /// </summary>
    public string LocalModelPath { get; set; } = string.Empty;

    /// <summary>
    /// The number of CPU cores to allocate for local inference.
    /// </summary>
    public int LocalCpuCores { get; set; } = 4;

    /// <summary>
    /// The context size for the local provider, determining the number of tokens to keep in memory.
    /// </summary>
    public int LocalModelContextSize { get; set; } = 4096;

    /// <summary>
    /// The maximum number of tokens to generate per request for the local provider.
    /// </summary>
    public int LocalModelMaxTokens { get; set; } = 2048;

    /// <summary>
    /// The temperature setting for the local provider, controlling randomness (0.0 to 2.0).
    /// </summary>
    public float LocalModelTemperature { get; set; } = 0.7f;
    
    
    /// <summary>
    /// If true, the application will attempt to use GPU acceleration for local models if available.
    /// </summary>
    public bool PreferGpu { get; set; } = true;
    
    /// <summary>
    /// If true, the application will attempt to load the local model on startup.
    /// </summary>
    public bool LocalModelLoadOnStartup { get; set; }

    /// <summary>
    /// If true, the local model will be automatically unloaded after a period of inactivity to free up resources.
    /// </summary>
    public bool LocalModelAutoUnloadEnabled { get; set; } = true;

    /// <summary>
    /// The number of minutes of inactivity before the local model is automatically unloaded.
    /// </summary>
    public int LocalModelIdleTimeoutMinutes { get; set; } = 30;

    /// <summary>
    /// Gets or sets a value indicating whether to memory-map the model file.
    /// </summary>
    /// <remarks>
    /// Defaults to <c>true</c>. When enabled, the model is not fully loaded into memory at once,
    /// allowing for faster startup times. If set to <c>false</c>, the entire model is loaded 
    /// into RAM, which can prevent performance degradation from disk swapping (pageouts) on systems 
    /// with sufficient RAM.
    /// </remarks>
    public bool LocalModelMemoryMap { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to lock the model in physical RAM.
    /// </summary>
    /// <remarks>
    /// Defaults to <c>false</c>. This prevents the operating system from swapping the model's memory
    /// to disk, which can significantly improve performance. This setting is only effective
    /// when <see cref="LocalModelMemoryMap"/> is <c>true</c> and requires that the entire model
    /// fits within the available physical RAM.
    /// </remarks>
    public bool LocalModelMemorylock { get; set; } = false;

    #endregion

    #region Provider Switching Logic

    /// <summary>
    /// The primary service type to use for requests ("Cloud" or "Local").
    /// </summary>
    public string PrimaryServiceType { get; set; } = "Cloud";

    /// <summary>
    /// The fallback service type to use if the primary one fails ("Cloud", "Local", or "None").
    /// </summary>
    public string FallbackServiceType { get; set; } = "None";

    #endregion
}