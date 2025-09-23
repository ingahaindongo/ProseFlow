using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ProseFlow.UI.ViewModels.Onboarding;

public partial class HotkeyTutorialViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _headerText = "Let's try it!";
    
    [ObservableProperty]
    private string _instructionText = "Select the text below and press the hotkey to see your actions.";
    
    [ObservableProperty]
    private string _configuredHotkey = "Ctrl+J";

    [ObservableProperty]
    private string _sampleText = "ProseFlow is a grate tool it hlps me writng better";

    [ObservableProperty]
    private bool _showSimulatedMenu;

    [ObservableProperty]
    private bool _isCompleted;

    [RelayCommand]
    private void ShowMenu()
    {
        ShowSimulatedMenu = true;
    }

    [RelayCommand]
    private void SimulateFix()
    {
        ShowSimulatedMenu = false;
        SampleText = "✨ ProseFlow is a great tool. It helps me write better. ✨";
        HeaderText = "NICE! That's the magic.";
        InstructionText = "You can use this hotkey in any application.";
        IsCompleted = true;
    }
}