using System.ComponentModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using ProseFlow.UI.Utils;

namespace ProseFlow.UI.ViewModels;

public interface IPageViewModel : INotifyPropertyChanged
{
    string Title { get; }
    IconSymbol Icon { get; }
    bool IsSelected { get; set; }

    Task OnNavigatedFromAsync();
    Task OnNavigatedToAsync();
}

public abstract partial class ViewModelBase : ObservableObject, IPageViewModel
{
    public virtual string Title { get; set; } = string.Empty;
    public virtual IconSymbol Icon => IconSymbol.Atom;

    [ObservableProperty]
    private bool _isSelected;

    public virtual Task OnNavigatedFromAsync() => Task.CompletedTask;

    public virtual Task OnNavigatedToAsync() => Task.CompletedTask;
}