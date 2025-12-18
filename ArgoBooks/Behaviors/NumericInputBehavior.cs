using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using System.Text.RegularExpressions;

namespace ArgoBooks.Behaviors;

/// <summary>
/// Attached behavior to restrict TextBox input to numeric values only.
/// </summary>
public static partial class NumericInputBehavior
{
    /// <summary>
    /// Allows only integer input (no decimals).
    /// </summary>
    public static readonly AttachedProperty<bool> IsIntegerOnlyProperty =
        AvaloniaProperty.RegisterAttached<TextBox, bool>("IsIntegerOnly", typeof(NumericInputBehavior));

    /// <summary>
    /// Allows decimal input (numbers with optional decimal point).
    /// </summary>
    public static readonly AttachedProperty<bool> IsDecimalOnlyProperty =
        AvaloniaProperty.RegisterAttached<TextBox, bool>("IsDecimalOnly", typeof(NumericInputBehavior));

    static NumericInputBehavior()
    {
        IsIntegerOnlyProperty.Changed.AddClassHandler<TextBox>(OnIsIntegerOnlyChanged);
        IsDecimalOnlyProperty.Changed.AddClassHandler<TextBox>(OnIsDecimalOnlyChanged);
    }

    public static bool GetIsIntegerOnly(TextBox element) => element.GetValue(IsIntegerOnlyProperty);
    public static void SetIsIntegerOnly(TextBox element, bool value) => element.SetValue(IsIntegerOnlyProperty, value);

    public static bool GetIsDecimalOnly(TextBox element) => element.GetValue(IsDecimalOnlyProperty);
    public static void SetIsDecimalOnly(TextBox element, bool value) => element.SetValue(IsDecimalOnlyProperty, value);

    private static void OnIsIntegerOnlyChanged(TextBox textBox, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.NewValue is true)
        {
            textBox.AddHandler(InputElement.TextInputEvent, OnIntegerTextInput, RoutingStrategies.Tunnel);
            textBox.PastingFromClipboard += OnIntegerPaste;
        }
        else
        {
            textBox.RemoveHandler(InputElement.TextInputEvent, OnIntegerTextInput);
            textBox.PastingFromClipboard -= OnIntegerPaste;
        }
    }

    private static void OnIsDecimalOnlyChanged(TextBox textBox, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.NewValue is true)
        {
            textBox.AddHandler(InputElement.TextInputEvent, OnDecimalTextInput, RoutingStrategies.Tunnel);
            textBox.PastingFromClipboard += OnDecimalPaste;
        }
        else
        {
            textBox.RemoveHandler(InputElement.TextInputEvent, OnDecimalTextInput);
            textBox.PastingFromClipboard -= OnDecimalPaste;
        }
    }

    private static void OnIntegerTextInput(object? sender, TextInputEventArgs e)
    {
        if (e.Text != null && !IntegerRegex().IsMatch(e.Text))
        {
            e.Handled = true;
        }
    }

    private static void OnDecimalTextInput(object? sender, TextInputEventArgs e)
    {
        if (sender is not TextBox textBox || e.Text == null)
            return;

        var currentText = textBox.Text ?? string.Empty;
        var caretIndex = textBox.CaretIndex;
        var selectionLength = textBox.SelectionEnd - textBox.SelectionStart;

        // Build the resulting text
        var newText = currentText.Substring(0, caretIndex - selectionLength) + e.Text + currentText.Substring(caretIndex);

        // Check if the result would be a valid decimal
        if (!IsValidDecimalInput(newText))
        {
            e.Handled = true;
        }
    }

    private static void OnIntegerPaste(object? sender, RoutedEventArgs e)
    {
        // Handled at paste time - could filter paste content if needed
    }

    private static void OnDecimalPaste(object? sender, RoutedEventArgs e)
    {
        // Handled at paste time - could filter paste content if needed
    }

    private static bool IsValidDecimalInput(string text)
    {
        if (string.IsNullOrEmpty(text))
            return true;

        // Allow digits and at most one decimal point
        return DecimalInputRegex().IsMatch(text);
    }

    [GeneratedRegex(@"^[0-9]*$")]
    private static partial Regex IntegerRegex();

    [GeneratedRegex(@"^[0-9]*\.?[0-9]*$")]
    private static partial Regex DecimalInputRegex();
}
