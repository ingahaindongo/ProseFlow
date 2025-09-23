using System.ComponentModel;
using System.Runtime.CompilerServices;
using ProseFlow.Core.Models;

namespace ProseFlow.Application.DTOs.Models;

public enum DownloadStatus { Queued, Downloading, Paused, Canceled, Failed, Completed }

/// <summary>
/// A data transfer object representing the state of a single download operation.
/// </summary>
public class DownloadTask : INotifyPropertyChanged
{
    public required ModelCatalogEntry Model { get; init; }
    public required ModelQuantization Quantization { get; init; }
    public required string DestinationPath { get; init; }
    
    private DownloadStatus _status;
    public DownloadStatus Status
    {
        get => _status;
        set => SetField(ref _status, value);
    }
    
    private double _progressPercentage;
    public double ProgressPercentage
    {
        get => _progressPercentage;
        set => SetField(ref _progressPercentage, value);
    }

    private long _bytesDownloaded;
    public long BytesDownloaded
    {
        get => _bytesDownloaded;
        set => SetField(ref _bytesDownloaded, value);
    }

    private long _totalBytes;
    public long TotalBytes
    {
        get => _totalBytes;
        set => SetField(ref _totalBytes, value);
    }
    
    private double _speed;
    public double Speed
    {
        get => _speed;
        set => SetField(ref _speed, value);
    }

    private string _errorMessage = string.Empty;
    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetField(ref _errorMessage, value);
    }

    public CancellationTokenSource Cts { get; } = new();

    #region INotifyPropertyChanged
    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        OnPropertyChanged(propertyName);
    }
    #endregion
}