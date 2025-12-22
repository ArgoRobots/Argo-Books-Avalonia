using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.Controls;

/// <summary>
/// A color picker input control with a color swatch button and hex text input.
/// </summary>
public partial class ColorPickerInput : UserControl
{
    #region Styled Properties

    public static readonly StyledProperty<string> ColorValueProperty =
        AvaloniaProperty.Register<ColorPickerInput, string>(nameof(ColorValue), "#000000");

    public static readonly StyledProperty<bool> IsPickerOpenProperty =
        AvaloniaProperty.Register<ColorPickerInput, bool>(nameof(IsPickerOpen));

    public static readonly StyledProperty<Color> SelectedColorProperty =
        AvaloniaProperty.Register<ColorPickerInput, Color>(nameof(SelectedColor), Colors.Black);

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
    /// The selected color in the picker.
    /// </summary>
    public Color SelectedColor
    {
        get => GetValue(SelectedColorProperty);
        set => SetValue(SelectedColorProperty, value);
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

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ColorValueProperty)
        {
            UpdateColorBrush();
            UpdateSelectedColor();
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

    private void UpdateSelectedColor()
    {
        try
        {
            if (!string.IsNullOrEmpty(ColorValue))
            {
                SelectedColor = Color.Parse(ColorValue);
            }
        }
        catch
        {
            // Ignore parse errors
        }
    }

    private void TogglePicker()
    {
        // Update selected color before opening
        UpdateSelectedColor();
        IsPickerOpen = !IsPickerOpen;
    }

    private void ApplyColor()
    {
        // Convert the selected color to hex string
        ColorValue = $"#{SelectedColor.R:X2}{SelectedColor.G:X2}{SelectedColor.B:X2}";
        IsPickerOpen = false;
    }
}
