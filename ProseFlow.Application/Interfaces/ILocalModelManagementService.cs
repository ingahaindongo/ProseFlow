using ProseFlow.Application.DTOs.Models;
using ProseFlow.Core.Models;
using Action = System.Action;

namespace ProseFlow.Application.Interfaces;

public interface ILocalModelManagementService
{
    event Action? ModelsChanged;
    
    Task<List<LocalModel>> GetModelsAsync();
    Task<List<LocalModelStatusDto>> GetModelsWithStatusAsync();
    Task ImportManagedModelAsync(CustomModelImportData importData);
    Task LinkExternalModelAsync(CustomModelImportData importData);
    Task CreateManagedModelFromDownloadAsync(ModelCatalogEntry catalogEntry, string destinationPath);
    Task DeleteModelAsync(int modelId);
    Task UpdateModelPathAsync(int modelId, string newPath);
    string GetManagedModelsDirectory();
    void RaiseModelsChanged();
}