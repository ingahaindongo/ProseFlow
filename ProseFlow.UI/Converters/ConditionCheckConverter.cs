using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace ProseFlow.UI.Converters;

/// <summary>
/// A value converter that checks a condition and returns a value based on the result.
/// </summary>
/// <remarks>
/// This converter takes a value as input and checks if it matches a specified condition.
/// It returns one value if the condition is true and another value if it's false.
/// </remarks>
public class ConditionCheckConverter : IValueConverter
{
    /// <summary>
    /// Converts a value based on a condition.
    /// </summary>
    /// <param name="value">The value to check against the condition.</param>
    /// <param name="targetType">The target type of the conversion, not used.</param>
    /// <param name="parameter">A string containing the condition, true value, and false value, separated by "|" (pipe character). Example: "condition|trueValue|falseValue".</param>
    /// <param name="culture">The culture information, not used.</param>
    /// <returns>The true value if the condition is met, otherwise the false value.</returns>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (parameter is not string conditionString)
            throw new ArgumentException("Parameter is not a valid condition string.");

        // Split the condition string into parts.
        var parts = conditionString.Split('|', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length is < 3 or > 4)
            throw new ArgumentException(
                "Invalid condition string format. Expected 'condition|trueValue|falseValue' or 'condition|trueResult|falseResult'.");

        var conditions = parts[0].Split(';', StringSplitOptions.RemoveEmptyEntries);
        var trueResult = parts.Length >= 2 ? parts[1] : "True";
        var falseResult = parts.Length >= 3 ? parts[2] : "False";

        foreach (var condition in conditions)
            if (!CheckCondition(value, condition))
                return falseResult;

        return trueResult;
    }

    /// <summary>
    /// Converts a value back, which is not supported by this converter.
    /// </summary>
    /// <param name="value">The value to convert back.</param>
    /// <param name="targetType">The target type of the conversion.</param>
    /// <param name="parameter">The parameter for the conversion.</param>
    /// <param name="culture">The culture information.</param>
    /// <returns>Throws a `NotImplementedException`.</returns>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }

    private bool CheckCondition(object? input, string condition)
    {
        var trimmedCondition = condition.Trim();

        return trimmedCondition[0] switch
        {
            '=' => HandleEquality(input, trimmedCondition[1..]),
            '*' => HandleStartsWith(input, trimmedCondition),
            '%' => HandleEndsWith(input, trimmedCondition),
            '!' => HandleNotEqual(input, trimmedCondition),
            '>' => HandleGreaterThan(input, trimmedCondition[1..]),
            '<' => HandleLessThan(input, trimmedCondition[1..]),
            _ => HandleEquality(input, trimmedCondition)
        };
    }

    private static bool HandleEquality(object? input, string condition)
    {
        if (condition.Equals("null", StringComparison.OrdinalIgnoreCase))
            return input == null;

        if (input is null) return false;

        var inputValueAsString = input.ToString();
        return !string.IsNullOrEmpty(inputValueAsString) &&
               inputValueAsString.Equals(condition, StringComparison.OrdinalIgnoreCase);
    }

    private static bool HandleStartsWith(object? input, string condition)
    {
        var prefix = condition[1..];
        if (input is null) return false;

        var inputValueAsString = input.ToString();
        return !string.IsNullOrEmpty(inputValueAsString) &&
               inputValueAsString.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
    }

    private static bool HandleEndsWith(object? input, string condition)
    {
        var suffix = condition[1..];
        if (input is null) return false;

        var inputValueAsString = input.ToString();
        return !string.IsNullOrEmpty(inputValueAsString) &&
               inputValueAsString.EndsWith(suffix, StringComparison.OrdinalIgnoreCase);
    }

    private static bool HandleNotEqual(object? input, string condition)
    {
        var expectedValue = condition[1..];
        if (expectedValue.Equals("null", StringComparison.OrdinalIgnoreCase))
            return input != null;

        if (input is null) return true;

        var inputValueAsString = input.ToString();
        if (string.IsNullOrEmpty(inputValueAsString)) return true;

        return !inputValueAsString.Equals(expectedValue, StringComparison.OrdinalIgnoreCase);
    }

    private static bool HandleGreaterThan(object? input, string numericCondition)
    {
        if (!double.TryParse(numericCondition, out var conditionDouble))
            throw new ArgumentException($"Invalid numeric condition: {numericCondition}");

        return input is not null && double.TryParse(input.ToString(), out var inputValue) &&
               inputValue > conditionDouble;
    }

    private static bool HandleLessThan(object? input, string numericCondition)
    {
        if (!double.TryParse(numericCondition, out var conditionDouble))
            throw new ArgumentException($"Invalid numeric condition: {numericCondition}");

        return input is not null && double.TryParse(input.ToString(), out var inputValue) &&
               inputValue < conditionDouble;
    }
}