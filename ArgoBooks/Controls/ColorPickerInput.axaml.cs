using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Media;
using ColorPicker;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.Controls;

/// <summary>
/// A color picker input control with a color swatch button and hex text input.
/// </summary>
public partial class ColorPickerInput : UserControl
{
    private StandardColorPicker? _colorPickerControl;
    private bool _isUpdatingFromPicker;

    #region Styled Properties

    public static readonly StyledProperty<string> ColorValueProperty =
        AvaloniaProperty.Register<ColorPickerInput, string>(nameof(ColorValue), "#000000");

    public static readonly StyledProperty<bool> IsPickerOpenProperty =
        AvaloniaProperty.Register<ColorPickerInput, bool>(nameof(IsPickerOpen));

    public static readonly StyledProperty<IBrush?> ColorBrushProperty =
        AvaloniaProperty.Register<ColorPickerInput, IBrush?>(nameof(ColorBrush));

    #endregion

    #region Properties

    /// <summary>
    /// The hex color value (e.g., "#FF0000").
    /// </summary>
    public string ColorValue
    {
        get => GetValue(ColorValueProperty);
        set => SetValue(ColorValueProperty, value);
    }

    /// <summary>
    /// Whether the color picker popup is open.
    /// </summary>
    public bool IsPickerOpen
    {
        get => GetValue(IsPickerOpenProperty);
        set => SetValue(IsPickerOpenProperty, value);
    }

    /// <summary>
    /// The brush for the color swatch preview.
    /// </summary>
    public IBrush? ColorBrush
    {
        get => GetValue(ColorBrushProperty);
        private set => SetValue(ColorBrushProperty, value);
    }

    #endregion

    #region Commands

    public IRelayCommand TogglePickerCommand { get; }
    public IRelayCommand ApplyColorCommand { get; }

    #endregion

    public ColorPickerInput()
    {
        TogglePickerCommand = new RelayCommand(TogglePicker);
        ApplyColorCommand = new RelayCommand(ApplyColor);

        InitializeComponent();
        UpdateColorBrush();
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _colorPickerControl = this.FindControl<StandardColorPicker>("ColorPickerControl");
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ColorValueProperty && !_isUpdatingFromPicker)
        {
            UpdateColorBrush();
            UpdatePickerColor();
        }
    }

    private void UpdateColorBrush()
    {
        try
        {
            if (!string.IsNullOrEmpty(ColorValue))
            {
                var color = Color.Parse(ColorValue);
                ColorBrush = new SolidColorBrush(color);
            }
            else
            {
                ColorBrush = Brushes.Black;
            }
        }
        catch
        {
            ColorBrush = Brushes.Black;
        }
    }

    private void UpdatePickerColor()
    {
        if (_colorPickerControl == null) return;

        try
        {
            if (!string.IsNullOrEmpty(ColorValue))
            {
                var color = Color.Parse(ColorValue);
                _colorPickerControl.SelectedColor = color;
            }
        }
        catch
        {
            // Ignore parse errors
        }
    }

    private void OnColorPickerColorChanged(object? sender, RoutedEventArgs e)
    {
        if (sender is not StandardColorPicker picker) return;

        _isUpdatingFromPicker = true;
        try
        {
            var color = picker.SelectedColor;
            ColorValue = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
            UpdateColorBrush();
        }
        finally
        {
            _isUpdatingFromPicker = false;
        }
    }

    private void TogglePicker()
    {
        // Update picker color before opening
        UpdatePickerColor();
        IsPickerOpen = !IsPickerOpen;
    }

    private void ApplyColor()
    {
        IsPickerOpen = false;
    }
}
