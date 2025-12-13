using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;

namespace ArgoBooks.Controls;

/// <summary>
/// Custom DatePicker control with label, validation, and consistent styling.
/// </summary>
public partial class ArgoDatePicker : UserControl
{
    #region Styled Properties

    public static readonly StyledProperty<DateTimeOffset?> SelectedDateProperty =
        AvaloniaProperty.Register<ArgoDatePicker, DateTimeOffset?>(nameof(SelectedDate), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<DateTimeOffset> DisplayDateProperty =
        AvaloniaProperty.Register<ArgoDatePicker, DateTimeOffset>(nameof(DisplayDate), DateTimeOffset.Now);

    public static readonly StyledProperty<DateTimeOffset?> MinDateProperty =
        AvaloniaProperty.Register<ArgoDatePicker, DateTimeOffset?>(nameof(MinDate));

    public static readonly StyledProperty<DateTimeOffset?> MaxDateProperty =
        AvaloniaProperty.Register<ArgoDatePicker, DateTimeOffset?>(nameof(MaxDate));

    public static readonly StyledProperty<string?> LabelProperty =
        AvaloniaProperty.Register<ArgoDatePicker, string?>(nameof(Label));

    public static readonly StyledProperty<string?> PlaceholderProperty =
        AvaloniaProperty.Register<ArgoDatePicker, string?>(nameof(Placeholder), "Select a date");

    public static readonly StyledProperty<string?> HelperTextProperty =
        AvaloniaProperty.Register<ArgoDatePicker, string?>(nameof(HelperText));

    public static readonly StyledProperty<string?> ErrorMessageProperty =
        AvaloniaProperty.Register<ArgoDatePicker, string?>(nameof(ErrorMessage));

    public static readonly StyledProperty<bool> HasErrorProperty =
        AvaloniaProperty.Register<ArgoDatePicker, bool>(nameof(HasError));

    public static readonly StyledProperty<bool> IsRequiredProperty =
        AvaloniaProperty.Register<ArgoDatePicker, bool>(nameof(IsRequired));

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the selected date.
    /// </summary>
    public DateTimeOffset? SelectedDate
    {
        get => GetValue(SelectedDateProperty);
        set => SetValue(SelectedDateProperty, value);
    }

    /// <summary>
    /// Gets or sets the display date (initial calendar view).
    /// </summary>
    public DateTimeOffset DisplayDate
    {
        get => GetValue(DisplayDateProperty);
        set => SetValue(DisplayDateProperty, value);
    }

    /// <summary>
    /// Gets or sets the minimum selectable date.
    /// </summary>
    public DateTimeOffset? MinDate
    {
        get => GetValue(MinDateProperty);
        set => SetValue(MinDateProperty, value);
    }

    /// <summary>
    /// Gets or sets the maximum selectable date.
    /// </summary>
    public DateTimeOffset? MaxDate
    {
        get => GetValue(MaxDateProperty);
        set => SetValue(MaxDateProperty, value);
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
    /// Gets or sets the helper text.
    /// </summary>
    public string? HelperText
    {
        get => GetValue(HelperTextProperty);
        set => SetValue(HelperTextProperty, value);
    }

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    public string? ErrorMessage
    {
        get => GetValue(ErrorMessageProperty);
        set => SetValue(ErrorMessageProperty, value);
    }

    /// <summary>
    /// Gets or sets whether the control has a validation error.
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

    #endregion

    public ArgoDatePicker()
    {
        InitializeComponent();
    }
}
