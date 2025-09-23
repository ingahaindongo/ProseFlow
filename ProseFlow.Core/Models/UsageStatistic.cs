using ProseFlow.Core.Abstracts;

namespace ProseFlow.Core.Models;

/// <summary>
/// Represents aggregated token usage statistics for a specific month, stored in the database.
/// </summary>
public class UsageStatistic : EntityBase
{
    /// <summary>
    /// The year of the usage record (e.g., 2024).
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// The month of the usage record (1-12).
    /// </summary>
    public int Month { get; set; }
    
    /// <summary>
    /// The total number of tokens sent to the API in prompts for the month.
    /// </summary>
    public long PromptTokens { get; set; }
    
    /// <summary>
    /// The total number of tokens received from the API in completions for the month.
    /// </summary>
    public long CompletionTokens { get; set; }
}