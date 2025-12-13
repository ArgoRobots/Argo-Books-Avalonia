using System.Collections;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;

namespace ArgoBooks.Controls;

/// <summary>
/// Custom ComboBox control with label, validation, and consistent styling.
/// </summary>
public partial class ArgoComboBox : UserControl
{
    #region Styled Properties

    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
        AvaloniaProperty.Register<ArgoComboBox, IEnumerable?>(nameof(ItemsSource));

    public static readonly StyledProperty<object?> SelectedItemProperty =
        AvaloniaProperty.Register<ArgoComboBox, object?>(nameof(SelectedItem), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<int> SelectedIndexProperty =
        AvaloniaProperty.Register<ArgoComboBox, int>(nameof(SelectedIndex), -1, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<IBinding?> DisplayMemberPathProperty =
        AvaloniaProperty.Register<ArgoComboBox, IBinding?>(nameof(DisplayMemberPath));

    public static readonly StyledProperty<string?> LabelProperty =
        AvaloniaProperty.Register<ArgoComboBox, string?>(nameof(Label));

    public static readonly StyledProperty<string?> PlaceholderProperty =
        AvaloniaProperty.Register<ArgoComboBox, string?>(nameof(Placeholder));

    public static readonly StyledProperty<string?> HelperTextProperty =
        AvaloniaProperty.Register<ArgoComboBox, string?>(nameof(HelperText));

    public static readonly StyledProperty<string?> ErrorMessageProperty =
        AvaloniaProperty.Register<ArgoComboBox, string?>(nameof(ErrorMessage));

    public static readonly StyledProperty<bool> HasErrorProperty =
        AvaloniaProperty.Register<ArgoComboBox, bool>(nameof(HasError));

    public static readonly StyledProperty<bool> IsRequiredProperty =
        AvaloniaProperty.Register<ArgoComboBox, bool>(nameof(IsRequired));

    public static readonly StyledProperty<bool> IsReadOnlyProperty =
        AvaloniaProperty.Register<ArgoComboBox, bool>(nameof(IsReadOnly));

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the items source.
    /// </summary>
    public IEnumerable? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    /// <summary>
    /// Gets or sets the selected item.
    /// </summary>
    public object? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    /// <summary>
    /// Gets or sets the selected index.
    /// </summary>
    public int SelectedIndex
    {
        get => GetValue(SelectedIndexProperty);
        set => SetValue(SelectedIndexProperty, value);
    }

    /// <summary>
    /// Gets or sets the display member path binding.
    /// </summary>
    public IBinding? DisplayMemberPath
    {
        get => GetValue(DisplayMemberPathProperty);
        set => SetValue(DisplayMemberPathProperty, value);
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

    /// <summary>
    /// Gets or sets whether the control is read-only.
    /// </summary>
    public bool IsReadOnly
    {
        get => GetValue(IsReadOnlyProperty);
        set => SetValue(IsReadOnlyProperty, value);
    }

    #endregion

    public ArgoComboBox()
    {
        InitializeComponent();
    }
}
