using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ProseFlow.Application.Interfaces;
using ProseFlow.Core.Models;

namespace ProseFlow.Application.Services;

/// <summary>
/// Orchestrates the push and pull operations between the local database and the shared workspace files.
/// </summary>
public class WorkspaceSyncService(
    IWorkspaceManager workspaceManager,
    IWorkspaceProtector workspaceProtector,
    ActionManagementService actionService,
    CloudProviderManagementService providerService,
    SettingsService settingsService,
    ILogger<WorkspaceSyncService> logger)
{
    /// <summary>
    /// Pulls configuration from the specified workspace path and overwrites local settings.
    /// </summary>
    /// <param name="workspacePath">The absolute path to the workspace folder.</param>
    /// <param name="password">The workspace password to decrypt provider configurations.</param>
    public async Task PullFromWorkspaceAsync(string workspacePath, string password)
    {
        if (string.IsNullOrWhiteSpace(workspacePath) || !Directory.Exists(workspacePath))
            throw new DirectoryNotFoundException("The specified workspace path is not valid.");

        var actionsPath = Path.Combine(workspacePath, "actions.json");
        var providersPath = Path.Combine(workspacePath, "providers.json");

        // Import Actions
        if (File.Exists(actionsPath))
        {
            await using var actionsStream = new FileStream(actionsPath, FileMode.Open, FileAccess.Read);
            
            // Determine the conflict resolution strategy from user settings.
            var settings = await settingsService.GetGeneralSettingsAsync();
            await actionService.ImportActionsFromJsonStreamAsync(actionsStream, settings.WorkspaceSyncConflictStrategy);
            
            logger.LogInformation("Successfully pulled actions from workspace using '{Strategy}' strategy.", settings.WorkspaceSyncConflictStrategy);
        }

        // Import Providers
        if (File.Exists(providersPath))
        {
            workspaceProtector.Initialize(password);
            var providersJson = await File.ReadAllTextAsync(providersPath);
            var encryptedProviders = JsonSerializer.Deserialize<List<CloudProviderConfiguration>>(providersJson) ?? [];

            // Decrypt keys before importing
            foreach (var provider in encryptedProviders)
            {
                if (string.IsNullOrWhiteSpace(provider.ApiKey)) continue;
                try
                {
                    provider.ApiKey = workspaceProtector.Unprotect(provider.ApiKey);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to decrypt API key for provider '{ProviderName}'. The workspace password may be incorrect.", provider.Name);
                    throw new CryptographicException("Failed to decrypt provider data. The workspace password may be incorrect.");
                }
            }

            await providerService.ImportProvidersAsync(encryptedProviders);
            logger.LogInformation("Successfully pulled providers from workspace.");
        }

        // Update sync timestamp if this is the active workspace
        if (workspaceManager.IsConnected && workspaceManager.CurrentState.SharedPath == workspacePath) 
            await workspaceManager.SaveStateAsync(workspaceManager.CurrentState with { LastSyncTimestamp = DateTime.UtcNow });
    }

    /// <summary>
    /// Pushes local configuration to the specified workspace path, overwriting remote files.
    /// Validates the password against existing data before pushing to prevent corruption.
    /// </summary>
    /// <param name="workspacePath">The absolute path to the workspace folder.</param>
    /// <param name="password">The workspace password to encrypt provider configurations.</param>
    public async Task PushToWorkspaceAsync(string workspacePath, string password)
    {
        if (string.IsNullOrWhiteSpace(workspacePath))
            throw new ArgumentNullException(nameof(workspacePath));
        
        workspaceProtector.Initialize(password);

        Directory.CreateDirectory(workspacePath);

        var providersPath = Path.Combine(workspacePath, "providers.json");

        // Validate password against existing workspace data by attempting to decrypt it before pushing.
        if (File.Exists(providersPath))
        {
            try
            {
                logger.LogInformation("Validating password against existing workspace data before push...");
                var existingProvidersJson = await File.ReadAllTextAsync(providersPath);
                if (!string.IsNullOrWhiteSpace(existingProvidersJson))
                {
                    var existingProviders = JsonSerializer.Deserialize<List<CloudProviderConfiguration>>(existingProvidersJson) ?? [];
                    var firstProtectedProvider = existingProviders.FirstOrDefault(p => !string.IsNullOrEmpty(p.ApiKey));

                    if (firstProtectedProvider != null) _ = workspaceProtector.Unprotect(firstProtectedProvider.ApiKey);
                }
            }
            catch (JsonException ex)
            {
                logger.LogError(ex, "Could not parse existing providers.json for validation. Push aborted to prevent data loss.");
                throw new InvalidOperationException("Workspace 'providers.json' is corrupt. Push aborted.");
            }
            catch (CryptographicException)
            {
                logger.LogWarning("Password validation failed for push operation.");
                throw new CryptographicException("Incorrect password. Push operation aborted to prevent data corruption.");
            }
        }
        
        var actionsPath = Path.Combine(workspacePath, "actions.json");

        // Export Actions
        await actionService.ExportActionsToJsonAsync(actionsPath);
        logger.LogInformation("Successfully pushed actions to workspace.");

        // Export Providers
        var providers = await providerService.GetConfigurationsAsync();
        
        // Encrypt keys before exporting
        var encryptedProviders = providers.Select(p =>
        {
            var encryptedKey = string.IsNullOrWhiteSpace(p.ApiKey) ? string.Empty : workspaceProtector.Protect(p.ApiKey);
            return new CloudProviderConfiguration
            {
                Name = p.Name,
                ProviderType = p.ProviderType,
                IsEnabled = p.IsEnabled,
                ApiKey = encryptedKey,
                BaseUrl = p.BaseUrl,
                Model = p.Model,
                Temperature = p.Temperature,
                SortOrder = p.SortOrder
            };
        }).ToList();

        var providersJson = JsonSerializer.Serialize(encryptedProviders, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(providersPath, providersJson, Encoding.UTF8);
        logger.LogInformation("Successfully pushed providers to workspace.");

        // Update sync timestamp if this is the active workspace
        if (workspaceManager.IsConnected && workspaceManager.CurrentState.SharedPath == workspacePath) 
            await workspaceManager.SaveStateAsync(workspaceManager.CurrentState with { LastSyncTimestamp = DateTime.UtcNow });
    }
}