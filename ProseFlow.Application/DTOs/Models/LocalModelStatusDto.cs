using ProseFlow.Core.Models;

namespace ProseFlow.Application.DTOs.Models;

/// <summary>
/// A Data Transfer Object to carry a LocalModel entity and its current on-disk status.
/// </summary>
/// <param name="Model">The LocalModel entity from the database.</param>
/// <param name="IsMissing">True if the model's file path does not exist on disk; otherwise, false.</param>
public record LocalModelStatusDto(LocalModel Model, bool IsMissing);