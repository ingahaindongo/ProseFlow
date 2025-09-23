namespace ProseFlow.Application.DTOs.Models;

/// <summary>
/// A Data Transfer Object to carry the necessary information for importing a custom GGUF model.
/// </summary>
public record CustomModelImportData(string Name, string Creator, string Description, string SourceGgufPath);