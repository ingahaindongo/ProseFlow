using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Styling;

namespace ProseFlow.UI.Behaviors;

/// <summary>
/// A behavior that animates a TextBlock when its value changes.
/// - For numeric values, it performs a "count-up" animation.
/// - For non-numeric string values, it performs a "typewriter" effect.
/// It attaches to a TextBlock and updates its Text property smoothly when the bound value changes.
/// </summary>
public class AnimatingTextBlockBehavior : AvaloniaObject
{
    #region Private Attached Properties

    /// <summary>
    /// Stores the CancellationTokenSource for the current animation, allowing it to be cancelled.
    /// </summary>
    private static readonly AttachedProperty<CancellationTokenSource?> CtsProperty =
        AvaloniaProperty.RegisterAttached<AnimatingTextBlockBehavior, TextBlock, CancellationTokenSource?>("Cts");

    /// <summary>
    /// Stores the intermediate numeric value during a count-up animation. This property is animated directly.
    /// </summary>
    private static readonly AttachedProperty<double> CurrentDisplayedValueProperty =
        AvaloniaProperty.RegisterAttached<AnimatingTextBlockBehavior, TextBlock, double>("CurrentDisplayedValue");

    #endregion

    #region Public Attached Properties

    /// <summary>
    /// Defines the AnimatedValue attached property.
    /// Binding a value to this property will trigger the animation.
    /// The behavior will determine whether to use a numeric or string animation based on the value's type.
    /// </summary>
    public static readonly AttachedProperty<object?> AnimatedValueProperty =
        AvaloniaProperty.RegisterAttached<AnimatingTextBlockBehavior, TextBlock, object?>("AnimatedValue");

    /// <summary>Gets the value of the AnimatedValue attached property for a specified TextBlock.</summary>
    /// <param name="element">The TextBlock from which to read the property value.</param>
    /// <returns>The value of the AnimatedValue property.</returns>
    public static object? GetAnimatedValue(TextBlock element) => element.GetValue(AnimatedValueProperty);
    /// <summary>Sets the value of the AnimatedValue attached property for a specified TextBlock.</summary>
    /// <param name="element">The TextBlock on which to set the property value.</param>
    /// <param name="value">The value to set.</param>
    public static void SetAnimatedValue(TextBlock element, object? value) => element.SetValue(AnimatedValueProperty, value);

    /// <summary>
    /// Defines the FormatString attached property.
    /// This string is used to format the numeric value during the count-up animation.
    /// The default is "N0" (a number with thousand separators and no decimal places).
    /// </summary>
    public static readonly AttachedProperty<string?> FormatStringProperty =
        AvaloniaProperty.RegisterAttached<AnimatingTextBlockBehavior, TextBlock, string?>("FormatString", "N0");

    /// <summary>Gets the value of the FormatString attached property for a specified TextBlock.</summary>
    /// <param name="element">The TextBlock from which to read the property value.</param>
    /// <returns>The format string.</returns>
    public static string? GetFormatString(TextBlock element) => element.GetValue(FormatStringProperty);
    /// <summary>Sets the value of the FormatString attached property for a specified TextBlock.</summary>
    /// <param name="element">The TextBlock on which to set the property value.</param>
    /// <param name="value">The format string to set.</param>
    public static void SetFormatString(TextBlock element, string? value) => element.SetValue(FormatStringProperty, value);

    /// <summary>
    /// Defines the NumberAnimationDuration attached property.
    /// This controls the total time it takes for the numeric count-up animation to complete.
    /// </summary>
    public static readonly AttachedProperty<TimeSpan> NumberAnimationDurationProperty =
        AvaloniaProperty.RegisterAttached<AnimatingTextBlockBehavior, TextBlock, TimeSpan>("NumberAnimationDuration", TimeSpan.FromSeconds(1.5));

    /// <summary>Gets the value of the NumberAnimationDuration attached property for a specified TextBlock.</summary>
    /// <param name="element">The TextBlock from which to read the property value.</param>
    /// <returns>The duration of the numeric animation.</returns>
    public static TimeSpan GetNumberAnimationDuration(TextBlock element) => element.GetValue(NumberAnimationDurationProperty);
    /// <summary>Sets the value of the NumberAnimationDuration attached property for a specified TextBlock.</summary>
    /// <param name="element">The TextBlock on which to set the property value.</param>
    /// <param name="value">The duration to set.</param>
    public static void SetNumberAnimationDuration(TextBlock element, TimeSpan value) => element.SetValue(NumberAnimationDurationProperty, value);
    
    /// <summary>
    /// Defines the StringAnimationDuration attached property. This controls the total time it takes for the typewriter effect to complete for string values.
    /// </summary>
    public static readonly AttachedProperty<TimeSpan> StringAnimationDurationProperty =
        AvaloniaProperty.RegisterAttached<AnimatingTextBlockBehavior, TextBlock, TimeSpan>("StringAnimationDuration", TimeSpan.FromSeconds(1.0));

    /// <summary>Gets the value of the StringAnimationDuration attached property for a specified TextBlock.</summary>
    /// <param name="element">The TextBlock from which to read the property value.</param>
    /// <returns>The duration of the string animation.</returns>
    public static TimeSpan GetStringAnimationDuration(TextBlock element) => element.GetValue(StringAnimationDurationProperty);
    /// <summary>Sets the value of the StringAnimationDuration attached property for a specified TextBlock.</summary>
    /// <param name="element">The TextBlock on which to set the property value.</param>
    /// <param name="value">The duration to set.</param>
    public static void SetStringAnimationDuration(TextBlock element, TimeSpan value) => element.SetValue(StringAnimationDurationProperty, value);

    #endregion
    
    /// <summary>
    /// Initializes static members of the <see cref="AnimatingTextBlockBehavior"/> class.
    /// Registers class handlers for property changes.
    /// </summary>
    static AnimatingTextBlockBehavior()
    {
        AnimatedValueProperty.Changed.AddClassHandler<TextBlock>(OnAnimatedValueChanged);
        
        // This handler updates the TextBlock's Text property whenever the intermediate animated value changes.
        CurrentDisplayedValueProperty.Changed.AddClassHandler<TextBlock>((textBlock, args) =>
        {
            if (args.NewValue is not double v) return;
            var formatString = GetFormatString(textBlock);
            textBlock.Text = v.ToString(formatString, CultureInfo.CurrentCulture);
        });
    }
    
    /// <summary>
    /// Called when the <see cref="AnimatedValueProperty"/> changes.
    /// This method cancels any ongoing animation and starts a new one based on the new value's type.
    /// </summary>
    /// <param name="textBlock">The TextBlock instance where the property changed.</param>
    /// <param name="e">The event arguments, containing old and new values.</param>
    private static async void OnAnimatedValueChanged(TextBlock textBlock, AvaloniaPropertyChangedEventArgs e)
    {
        if (Equals(e.OldValue, e.NewValue)) return;
        
        // Cancel any previous animation that might be running on this TextBlock.
        textBlock.GetValue(CtsProperty)?.Cancel();
        var newCts = new CancellationTokenSource();
        textBlock.SetValue(CtsProperty, newCts);
        
        var newValue = e.NewValue;

        var isNumeric = double.TryParse(
            Convert.ToString(newValue, CultureInfo.InvariantCulture),
            NumberStyles.Any,
            CultureInfo.InvariantCulture,
            out var numericValue
        );

        try
        {
            // Restore opacity in case a previous string animation was cancelled midway
            textBlock.Opacity = 1;

            if (isNumeric)
                await AnimateNumberAsync(textBlock, numericValue, newCts.Token);
            else
                await AnimateStringAsync(textBlock, newValue?.ToString() ?? string.Empty, newCts.Token);
        }
        catch (TaskCanceledException)
        {
            // Expected if another value change cancels the animation. The new animation will take over.
        }
    }

    /// <summary>
    /// Performs a "count-up" or "count-down" animation for a numeric value.
    /// </summary>
    /// <param name="textBlock">The TextBlock to animate.</param>
    /// <param name="targetValue">The target numeric value to animate to.</param>
    /// <param name="token">A cancellation token to stop the animation.</param>
    /// <returns>A task that represents the asynchronous animation operation.</returns>
    private static async Task AnimateNumberAsync(TextBlock textBlock, double targetValue, CancellationToken token)
    {
        var duration = GetNumberAnimationDuration(textBlock);
        var startValue = textBlock.GetValue(CurrentDisplayedValueProperty);

        // If the value hasn't changed significantly, set it directly and return.
        if (Math.Abs(startValue - targetValue) < 0.001)
        {
            textBlock.Text = targetValue.ToString(GetFormatString(textBlock), CultureInfo.CurrentCulture);
            textBlock.SetValue(CurrentDisplayedValueProperty, targetValue);
            return;
        }
        
        var animation = new Animation
        {
            Duration = duration,
            Easing = new CubicEaseOut(),
            Children =
            {
                new KeyFrame { KeyTime = TimeSpan.Zero, Setters = { new Setter(CurrentDisplayedValueProperty, startValue) } },
                new KeyFrame { KeyTime = duration, Setters = { new Setter(CurrentDisplayedValueProperty, targetValue) } }
            }
        };
            
        await animation.RunAsync(textBlock, token);
        
        if (!token.IsCancellationRequested) textBlock.SetValue(CurrentDisplayedValueProperty, targetValue);
    }
    
    /// <summary>
    /// Performs a "typewriter" animation for a string value.
    /// </summary>
    /// <param name="textBlock">The TextBlock to animate.</param>
    /// <param name="newText">The target string value to display.</param>
    /// <param name="token">A cancellation token to stop the animation.</param>
    /// <returns>A task that represents the asynchronous animation operation.</returns>
    private static async Task AnimateStringAsync(TextBlock textBlock, string newText, CancellationToken token)
    {
        var duration = GetStringAnimationDuration(textBlock);
        
        // Reset the numeric state so the next numeric animation starts from 0 (or its actual start value).
        textBlock.SetValue(CurrentDisplayedValueProperty, 0d);
        
        if (string.IsNullOrEmpty(newText))
        {
            // If the new text is empty, just clear it immediately.
            textBlock.Text = string.Empty;
            return;
        }

        // Calculate the delay between each character.
        var delayPerChar = duration.TotalMilliseconds / newText.Length;
        
        // Use a loop to "type" out the string character by character.
        for (var i = 1; i <= newText.Length; i++)
        {
            token.ThrowIfCancellationRequested();
            textBlock.Text = newText[..i];
            
            try
            {
                await Task.Delay(TimeSpan.FromMilliseconds(delayPerChar), token);
            }
            catch (TaskCanceledException)
            {
                // When cancelled, immediately show the old final value,
                throw;
            }
        }
        
        // Ensure the final text is correctly set in case of timing issues or if animation completes.
        if (!token.IsCancellationRequested) textBlock.Text = newText;
    }
}