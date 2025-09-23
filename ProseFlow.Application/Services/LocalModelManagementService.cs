using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProseFlow.Application.DTOs.Models;
using ProseFlow.Application.Events;
using ProseFlow.Application.Interfaces;
using ProseFlow.Core.Interfaces;
using ProseFlow.Core.Models;
using Action = System.Action;

namespace ProseFlow.Application.Services;

public class LocalModelManagementService : ILocalModelManagementService
{
    private readonly string _managedModelsDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ProseFlow", "models");
    
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<LocalModelManagementService> _logger;
    
    public event Action? ModelsChanged;
    
    public LocalModelManagementService(IServiceScopeFactory scopeFactory, ILogger<LocalModelManagementService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger; 
        Directory.CreateDirectory(_managedModelsDirectory);
    }

    public Task<List<LocalModel>> GetModelsAsync()
    {
        return ExecuteQueryAsync(unitOfWork => unitOfWork.LocalModels.GetAllAsync());
    }

    public async Task<List<LocalModelStatusDto>> GetModelsWithStatusAsync()
    {
        var models = await GetModelsAsync();
        return models.Select(model => new LocalModelStatusDto(model, !File.Exists(model.FilePath)))
            .ToList();
    }

    public string GetManagedModelsDirectory()
    {
        return _managedModelsDirectory;
    }

    public void RaiseModelsChanged()
    {
        ModelsChanged?.Invoke();
    }

    public async Task ImportManagedModelAsync(CustomModelImportData importData)
    {
        _logger.LogInformation("Attempting to import custom model from: {SourcePath}", importData.SourceGgufPath);
        if (!File.Exists(importData.SourceGgufPath) || !importData.SourceGgufPath.EndsWith(".gguf", StringComparison.OrdinalIgnoreCase))
            throw new FileNotFoundException("The selected file is not a valid GGUF file or does not exist.", importData.SourceGgufPath);

        var fileName = Path.GetFileName(importData.SourceGgufPath);
        var destinationPath = Path.Combine(_managedModelsDirectory, fileName);

        if (File.Exists(destinationPath))
            throw new InvalidOperationException($"A model with the name '{fileName}' already exists in the library.");
        
        // File Copy
        File.Copy(importData.SourceGgufPath, destinationPath);
        _logger.LogInformation("Copied model file to: {DestinationPath}", destinationPath);
        
        var fileInfo = new FileInfo(destinationPath);
        var newModel = new LocalModel
        {
            Name = importData.Name,
            Creator = importData.Creator,
            Description = importData.Description,
            FilePath = destinationPath,
            FileSizeGb = Math.Round(fileInfo.Length / 1024.0 / 1024.0 / 1024.0, 2),
            IsManaged = true,
            AddedAt = DateTime.UtcNow
        };
        
        await ExecuteCommandAsync(unitOfWork => unitOfWork.LocalModels.AddAsync(newModel));
        RaiseModelsChanged();
    }

    public async Task LinkExternalModelAsync(CustomModelImportData importData)
    {
        _logger.LogInformation("Attempting to link external model from: {SourcePath}", importData.SourceGgufPath);
        if (!File.Exists(importData.SourceGgufPath) || !importData.SourceGgufPath.EndsWith(".gguf", StringComparison.OrdinalIgnoreCase))
            throw new FileNotFoundException("The selected file is not a valid GGUF file or does not exist.", importData.SourceGgufPath);

        await ExecuteCommandAsync(async unitOfWork =>
        {
            var existing = (await unitOfWork.LocalModels.GetByExpressionAsync(m => m.FilePath == importData.SourceGgufPath)).FirstOrDefault();
            if (existing is not null)
                throw new InvalidOperationException("This model file has already been added to the library.");
            
            var fileInfo = new FileInfo(importData.SourceGgufPath);
            var newModel = new LocalModel
            {
                Name = importData.Name,
                Creator = importData.Creator,
                Description = importData.Description,
                FilePath = importData.SourceGgufPath,
                FileSizeGb = Math.Round(fileInfo.Length / 1024.0 / 1024.0 / 1024.0, 2),
                IsManaged = false,
                AddedAt = DateTime.UtcNow
            };
            
            await unitOfWork.LocalModels.AddAsync(newModel);
        });
        
        RaiseModelsChanged();
    }
    
    public async Task CreateManagedModelFromDownloadAsync(ModelCatalogEntry catalogEntry, string destinationPath)
    {
        var quantization = catalogEntry.Quantizations.FirstOrDefault(q => q.FileName == Path.GetFileName(destinationPath));
        if (quantization is null)
        {
            _logger.LogError("Could not find matching quantization in catalog entry for downloaded file {FileName}", Path.GetFileName(destinationPath));
            return;
        }

        var newModel = new LocalModel
        {
            Name = catalogEntry.Name,
            Creator = catalogEntry.Creator,
            Description = catalogEntry.Description,
            Tag = catalogEntry.Tag,
            FilePath = destinationPath,
            FileSizeGb = quantization.FileSizeGb,
            IsManaged = true,
            AddedAt = DateTime.UtcNow
        };
        
        await ExecuteCommandAsync(unitOfWork => unitOfWork.LocalModels.AddAsync(newModel));
        RaiseModelsChanged();
    }

    public async Task DeleteModelAsync(int modelId)
    {
        await ExecuteCommandAsync(async unitOfWork =>
        {
            var modelToDelete = await unitOfWork.LocalModels.GetByIdAsync(modelId);
            if (modelToDelete is null)
            {
                _logger.LogWarning("Attempted to delete non-existent local model with ID {ModelId}", modelId);
                return;
            }

            // If the model is managed by the application, delete the physical file.
            if (modelToDelete.IsManaged && File.Exists(modelToDelete.FilePath))
                try
                {
                    File.Delete(modelToDelete.FilePath);
                    _logger.LogInformation("Deleted managed model file: {FilePath}", modelToDelete.FilePath);
                }
                catch (Exception ex)
                {
                    // If the file could not be deleted, still proceed to delete the DB entry.
                    _logger.LogError(ex, "Failed to delete managed model file: {FilePath}, Please delete the file manually.", modelToDelete.FilePath);
                    AppEvents.RequestNotification($"Failed to delete model file {modelToDelete.FilePath}, Please delete the file manually.", NotificationType.Error);
                }

            // Always delete the database record.
            unitOfWork.LocalModels.Delete(modelToDelete);
        });
        
        RaiseModelsChanged();
    }
    
    public Task UpdateModelPathAsync(int modelId, string newPath)
    {
        return ExecuteCommandAsync(async unitOfWork =>
        {
            var modelToUpdate = await unitOfWork.LocalModels.GetByIdAsync(modelId);
            if (modelToUpdate is null)
            {
                _logger.LogWarning("Attempted to update path for non-existent model with ID {ModelId}", modelId);
                return;
            }

            modelToUpdate.FilePath = newPath;
            unitOfWork.LocalModels.Update(modelToUpdate);
        });
    }

    private async Task ExecuteCommandAsync(Func<IUnitOfWork, Task> command)
    {
        using var scope = _scopeFactory.CreateScope();
        await using var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        await command(unitOfWork);
        await unitOfWork.SaveChangesAsync();
    }

    private async Task<T> ExecuteQueryAsync<T>(Func<IUnitOfWork, Task<T>> query)
    {
        using var scope = _scopeFactory.CreateScope();
        await using var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        return await query(unitOfWork);
    }
}