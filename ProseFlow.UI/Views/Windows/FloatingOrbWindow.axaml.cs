using System;
using Avalonia;
using Avalonia.Input;
using CommunityToolkit.Mvvm.DependencyInjection;
using ProseFlow.UI.Services;
using ProseFlow.UI.ViewModels.Windows;
using Window = ShadUI.Window;

#if WINDOWS
using System.Runtime.InteropServices;
#endif

namespace ProseFlow.UI.Views.Windows;

public partial class FloatingOrbWindow : Window
{
#if WINDOWS
    // Win32 API calls to make the window a tool window (no taskbar icon)
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int WS_EX_APPWINDOW = 0x00040000;

    [DllImport("user32.dll")]
    public static extern int GetWindowLong(IntPtr hwnd, int index);

    [DllImport("user32.dll")]
    public static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);
#endif

    // State for drag detection vs. click
    public bool IsDragging { get; private set; }
    private bool _isPointerDown;
    private Point _startPosition;
    private PointerPressedEventArgs? _initialPressEventArgs;
    
    public FloatingOrbWindow()
    {
        InitializeComponent();
        DataContext = Ioc.Default.GetRequiredService<FloatingOrbViewModel>();

        // Enable receiving drag-and-drop events for text
        DragDrop.SetAllowDrop(this, true);
        AddHandler(DragDrop.DragOverEvent, OnDragOver);
        AddHandler(DragDrop.DropEvent, OnDrop);
    }
    
    private void OnDragOver(object? sender, DragEventArgs e)
    {
        // Provide visual feedback that a drop is possible
        e.DragEffects = e.Data.Contains(DataFormats.Text) ? DragDropEffects.Copy : DragDropEffects.None;
    }
    
    /// <summary>
    /// Handles text being dropped onto the Orb, triggering the Arc Menu.
    /// </summary>
    private async void OnDrop(object? sender, DragEventArgs e)
    {
        if (e.Data.GetText() is not { } droppedText || string.IsNullOrWhiteSpace(droppedText)) return;
        var orbService = Ioc.Default.GetRequiredService<FloatingOrbService>();
        await orbService.ShowArcMenuForDroppedTextAsync(droppedText);
    }
    
    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var properties = e.GetCurrentPoint(this).Properties;
        if (properties.IsLeftButtonPressed)
        {
            _isPointerDown = true;
            IsDragging = false;
            _startPosition = e.GetPosition(this);
            _initialPressEventArgs = e;
        }
        else if (properties.IsRightButtonPressed && DataContext is FloatingOrbViewModel vm)
        {
            vm.ShowContextMenuCommand.Execute(this);
        }
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isPointerDown || IsDragging || _initialPressEventArgs is null) return;
        
        var currentPosition = e.GetPosition(this);
        var distance = Vector.Distance(_startPosition, currentPosition);

        // If moved beyond a threshold, start a window drag operation
        if (distance > 3)
        {
            IsDragging = true;
            BeginMoveDrag(_initialPressEventArgs);
        }
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        // If the pointer was down but we never started dragging, it's a click
        if (_isPointerDown && !IsDragging && e.InitialPressMouseButton == MouseButton.Left)
        {
            if (DataContext is FloatingOrbViewModel vm)
            {
                vm.TriggerActionMenuCommand.Execute(null);
            }
        }

        // Reset state
        _isPointerDown = false;
        IsDragging = false;
        _initialPressEventArgs = null;
    }

    private void OnWindowOpened(object? sender, EventArgs e)
    {
#if WINDOWS
        if (TryGetPlatformHandle() is { } handle)
        {
            var hwnd = handle.Handle;
            var exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            exStyle |= WS_EX_TOOLWINDOW;
            exStyle &= ~WS_EX_APPWINDOW;
            SetWindowLong(hwnd, GWL_EXSTYLE, exStyle);
        }
#endif
    }
}