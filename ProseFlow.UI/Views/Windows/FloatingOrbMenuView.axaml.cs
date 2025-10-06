using System;
using Avalonia;
using Avalonia.Interactivity;
using Window = ShadUI.Window;

namespace ProseFlow.UI.Views.Windows;

public partial class FloatingOrbMenuView : Window
{
    public FloatingOrbMenuView()
    {
        InitializeComponent();
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        
        
        if (Owner is not Window owner) return;

        // Position the menu near the owner (the Orb window)
        var ownerPos = owner.Position;
        var ownerSize = owner.Bounds.Size;
        var menuSize = Bounds.Size;

        Position = new PixelPoint(
            ownerPos.X + (int)(ownerSize.Width / 2 - menuSize.Width / 2),
            ownerPos.Y - (int)menuSize.Height - 8 // Position above the orb
        );
        
    }
    
    private void OnDeactivated(object? sender, EventArgs e)
    {
        // Automatically close the menu when the user clicks away
        Close();
    }
}