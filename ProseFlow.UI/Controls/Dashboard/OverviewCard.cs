﻿using Avalonia;
using Avalonia.Controls.Primitives;
using ProseFlow.UI.Utils;

namespace ProseFlow.UI.Controls.Dashboard;

public class OverviewCard : TemplatedControl
{
    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<OverviewCard, string>(nameof(Title));

    public string Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public static readonly StyledProperty<object?> ValueProperty =
        AvaloniaProperty.Register<OverviewCard, object?>(nameof(Value));

    public object? Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public static readonly StyledProperty<string> HintProperty =
        AvaloniaProperty.Register<OverviewCard, string>(nameof(Hint));

    public string Hint
    {
        get => GetValue(HintProperty);
        set => SetValue(HintProperty, value);
    }

    public static readonly StyledProperty<IconSymbol> IconProperty =
        AvaloniaProperty.Register<OverviewCard, IconSymbol>(nameof(Icon));

    public IconSymbol Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }
}