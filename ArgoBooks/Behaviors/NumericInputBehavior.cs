using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using System.Text.RegularExpressions;

namespace ArgoBooks.Behaviors;

/// <summary>
/// Attached behavior to restrict TextBox input to numeric values only.
/// Uses KeyDown event to prevent non-numeric characters from being entered.
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
            textBox.AddHandler(InputElement.KeyDownEvent, OnIntegerKeyDown, RoutingStrategies.Tunnel);
            textBox.AddHandler(InputElement.TextInputEvent, OnIntegerTextInput, RoutingStrategies.Tunnel);
        }
        else
        {
            textBox.RemoveHandler(InputElement.KeyDownEvent, OnIntegerKeyDown);
            textBox.RemoveHandler(InputElement.TextInputEvent, OnIntegerTextInput);
        }
    }

    private static void OnIsDecimalOnlyChanged(TextBox textBox, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.NewValue is true)
        {
            textBox.AddHandler(InputElement.KeyDownEvent, OnDecimalKeyDown, RoutingStrategies.Tunnel);
            textBox.AddHandler(InputElement.TextInputEvent, OnDecimalTextInput, RoutingStrategies.Tunnel);
        }
        else
        {
            textBox.RemoveHandler(InputElement.KeyDownEvent, OnDecimalKeyDown);
            textBox.RemoveHandler(InputElement.TextInputEvent, OnDecimalTextInput);
        }
    }

    private static void OnIntegerKeyDown(object? sender, KeyEventArgs e)
    {
        if (IsAllowedControlKey(e))
            return;

        // Allow digits (both main keyboard and numpad)
        if (IsDigitKey(e.Key))
            return;

        // Block all other keys
        e.Handled = true;
    }

    private static void OnDecimalKeyDown(object? sender, KeyEventArgs e)
    {
        if (IsAllowedControlKey(e))
            return;

        // Allow digits (both main keyboard and numpad)
        if (IsDigitKey(e.Key))
            return;

        // Allow decimal point (period and numpad decimal)
        if (e.Key == Key.OemPeriod || e.Key == Key.Decimal)
        {
            // Only allow if there's no decimal point already
            if (sender is TextBox textBox)
            {
                var currentText = textBox.Text ?? string.Empty;
                var selectionStart = textBox.SelectionStart;
                var selectionEnd = textBox.SelectionEnd;

                // Check if selection contains the decimal point
                var selectedText = selectionEnd > selectionStart
                    ? currentText.Substring(selectionStart, selectionEnd - selectionStart)
                    : string.Empty;

                // Allow if no existing decimal or the selection contains it
                if (!currentText.Contains('.') || selectedText.Contains('.'))
                    return;
            }
        }

        // Block all other keys
        e.Handled = true;
    }

    private static bool IsDigitKey(Key key)
    {
        return key >= Key.D0 && key <= Key.D9 ||
               key >= Key.NumPad0 && key <= Key.NumPad9;
    }

    private static bool IsAllowedControlKey(KeyEventArgs e)
    {
        // Allow navigation and editing keys
        if (e.Key == Key.Back || e.Key == Key.Delete ||
            e.Key == Key.Left || e.Key == Key.Right ||
            e.Key == Key.Home || e.Key == Key.End ||
            e.Key == Key.Tab || e.Key == Key.Enter ||
            e.Key == Key.Up || e.Key == Key.Down)
            return true;

        // Allow Ctrl+A, Ctrl+C, Ctrl+V, Ctrl+X, Ctrl+Z
        if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            if (e.Key == Key.A || e.Key == Key.C || e.Key == Key.V ||
                e.Key == Key.X || e.Key == Key.Z)
                return true;
        }

        return false;
    }

    // TextInput handlers as backup for paste operations
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
        var selectionStart = textBox.SelectionStart;
        var selectionEnd = textBox.SelectionEnd;
        var selectionLength = selectionEnd - selectionStart;

        // Build the resulting text
        string newText;
        if (selectionLength > 0)
        {
            newText = currentText.Substring(0, selectionStart) + e.Text + currentText.Substring(selectionEnd);
        }
        else
        {
            var caretIndex = textBox.CaretIndex;
            newText = currentText.Substring(0, caretIndex) + e.Text + currentText.Substring(caretIndex);
        }

        // Check if the result would be a valid decimal
        if (!IsValidDecimalInput(newText))
        {
            e.Handled = true;
        }
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
