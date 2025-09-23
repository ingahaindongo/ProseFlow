using ProseFlow.Core.Models;

namespace ProseFlow.Application.Interfaces;

public interface IModelCatalogService
{
    Task<List<ModelCatalogEntry>> GetAvailableModelsAsync();
}