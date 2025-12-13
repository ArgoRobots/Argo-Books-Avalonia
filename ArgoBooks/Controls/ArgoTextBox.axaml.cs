using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.Controls;

/// <summary>
/// Custom TextBox control with label, validation, and icon support.
/// </summary>
public partial class ArgoTextBox : UserControl
{
    #region Styled Properties

    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<ArgoTextBox, string?>(nameof(Text), defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<string?> LabelProperty =
        AvaloniaProperty.Register<ArgoTextBox, string?>(nameof(Label));

    public static readonly StyledProperty<string?> PlaceholderProperty =
        AvaloniaProperty.Register<ArgoTextBox, string?>(nameof(Placeholder));

    public static readonly StyledProperty<string?> HelperTextProperty =
        AvaloniaProperty.Register<ArgoTextBox, string?>(nameof(HelperText));

    public static readonly StyledProperty<string?> ErrorMessageProperty =
        AvaloniaProperty.Register<ArgoTextBox, string?>(nameof(ErrorMessage));

    public static readonly StyledProperty<bool> HasErrorProperty =
        AvaloniaProperty.Register<ArgoTextBox, bool>(nameof(HasError));

    public static readonly StyledProperty<bool> IsRequiredProperty =
        AvaloniaProperty.Register<ArgoTextBox, bool>(nameof(IsRequired));

    public static readonly StyledProperty<bool> IsReadOnlyProperty =
        AvaloniaProperty.Register<ArgoTextBox, bool>(nameof(IsReadOnly));

    public static readonly StyledProperty<int> MaxLengthProperty =
        AvaloniaProperty.Register<ArgoTextBox, int>(nameof(MaxLength), int.MaxValue);

    public static readonly StyledProperty<char> PasswordCharProperty =
        AvaloniaProperty.Register<ArgoTextBox, char>(nameof(PasswordChar), '\0');

    public static readonly StyledProperty<bool> AcceptsReturnProperty =
        AvaloniaProperty.Register<ArgoTextBox, bool>(nameof(AcceptsReturn));

    public static readonly StyledProperty<TextWrapping> TextWrappingProperty =
        AvaloniaProperty.Register<ArgoTextBox, TextWrapping>(nameof(TextWrapping), TextWrapping.NoWrap);

    public static readonly StyledProperty<Geometry?> LeftIconProperty =
        AvaloniaProperty.Register<ArgoTextBox, Geometry?>(nameof(LeftIcon));

    public static readonly StyledProperty<Geometry?> RightIconProperty =
        AvaloniaProperty.Register<ArgoTextBox, Geometry?>(nameof(RightIcon));

    public static readonly StyledProperty<bool> ShowClearButtonProperty =
        AvaloniaProperty.Register<ArgoTextBox, bool>(nameof(ShowClearButton));

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the text content.
    /// </summary>
    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    /// <summary>
    /// Gets or sets the label text.
    /// </summary>
    public string? Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    /// <summary>
    /// Gets or sets the placeholder text.
    /// </summary>
    public string? Placeholder
    {
        get => GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    /// <summary>
    /// Gets or sets the helper text shown below the input.
    /// </summary>
    public string? HelperText
    {
        get => GetValue(HelperTextProperty);
        set => SetValue(HelperTextProperty, value);
    }

    /// <summary>
    /// Gets or sets the error message shown when HasError is true.
    /// </summary>
    public string? ErrorMessage
    {
        get => GetValue(ErrorMessageProperty);
        set => SetValue(ErrorMessageProperty, value);
    }

    /// <summary>
    /// Gets or sets whether the input has a validation error.
    /// </summary>
    public bool HasError
    {
        get => GetValue(HasErrorProperty);
        set => SetValue(HasErrorProperty, value);
    }

    /// <summary>
    /// Gets or sets whether the field is required.
    /// </summary>
    public bool IsRequired
    {
        get => GetValue(IsRequiredProperty);
        set => SetValue(IsRequiredProperty, value);
    }

    /// <summary>
    /// Gets or sets whether the input is read-only.
    /// </summary>
    public bool IsReadOnly
    {
        get => GetValue(IsReadOnlyProperty);
        set => SetValue(IsReadOnlyProperty, value);
    }

    /// <summary>
    /// Gets or sets the maximum length of input.
    /// </summary>
    public int MaxLength
    {
        get => GetValue(MaxLengthProperty);
        set => SetValue(MaxLengthProperty, value);
    }

    /// <summary>
    /// Gets or sets the password character (for password fields).
    /// </summary>
    public char PasswordChar
    {
        get => GetValue(PasswordCharProperty);
        set => SetValue(PasswordCharProperty, value);
    }

    /// <summary>
    /// Gets or sets whether the textbox accepts return/newline.
    /// </summary>
    public bool AcceptsReturn
    {
        get => GetValue(AcceptsReturnProperty);
        set => SetValue(AcceptsReturnProperty, value);
    }

    /// <summary>
    /// Gets or sets the text wrapping mode.
    /// </summary>
    public TextWrapping TextWrapping
    {
        get => GetValue(TextWrappingProperty);
        set => SetValue(TextWrappingProperty, value);
    }

    /// <summary>
    /// Gets or sets the left icon geometry.
    /// </summary>
    public Geometry? LeftIcon
    {
        get => GetValue(LeftIconProperty);
        set => SetValue(LeftIconProperty, value);
    }

    /// <summary>
    /// Gets or sets the right icon geometry.
    /// </summary>
    public Geometry? RightIcon
    {
        get => GetValue(RightIconProperty);
        set => SetValue(RightIconProperty, value);
    }

    /// <summary>
    /// Gets or sets whether to show the clear button.
    /// </summary>
    public bool ShowClearButton
    {
        get => GetValue(ShowClearButtonProperty);
        set => SetValue(ShowClearButtonProperty, value);
    }

    /// <summary>
    /// Command to clear the text.
    /// </summary>
    public ICommand ClearCommand { get; }

    #endregion

    public ArgoTextBox()
    {
        ClearCommand = new RelayCommand(ClearText);
        InitializeComponent();
    }

    private void ClearText()
    {
        Text = string.Empty;
    }
}
