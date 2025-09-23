using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProseFlow.Application.Events;
using ProseFlow.Application.Services;
using ProseFlow.Core.Enums;
using ProseFlow.Core.Models;
using Window = ShadUI.Window;

namespace ProseFlow.UI.ViewModels.Providers;

public partial class CloudProviderEditorViewModel(CloudProviderConfiguration config, CloudProviderManagementService providerService) : ViewModelBase
{
    private readonly bool _isNewConfig = config.Id == 0;

    [ObservableProperty]
    private CloudProviderConfiguration _config = new() // Clone to avoid modifying the original until save
    {
        Id = config.Id,
        Name = config.Name,
        ProviderType = config.ProviderType,
        IsEnabled = config.IsEnabled,
        ApiKey = config.ApiKey,
        BaseUrl = config.BaseUrl,
        Model = config.Model,
        Temperature = config.Temperature,
        SortOrder = config.SortOrder
    };
    
    [ObservableProperty]
    private float _configTemperature = config.Temperature;
    
    public List<ProviderType> AvailableProviderTypes { get; } = Enum.GetValues(typeof(ProviderType)).Cast<ProviderType>().ToList();
    
    
    partial void OnConfigTemperatureChanged(float value)
    {
        Config.Temperature = value;
    }

    [RelayCommand]
    private async Task SaveAsync(Window window)
    {
        if (string.IsNullOrWhiteSpace(Config.Name) || string.IsNullOrWhiteSpace(Config.Model))
        {
            AppEvents.RequestNotification("Name and Model fields cannot be empty.", NotificationType.Warning);
            return;
        }

        if (_isNewConfig)
            await providerService.CreateConfigurationAsync(Config);
        else
            await providerService.UpdateConfigurationAsync(Config);

        window.Close(true);
    }

    [RelayCommand]
    private void Cancel(Window window)
    {
        window.Close(false);
    }
}