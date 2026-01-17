using System.Globalization;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using ArgoBooks.Localization;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.Controls;

/// <summary>
/// Type of message box which determines the icon and primary button style.
/// </summary>
public enum MessageBoxType
{
    /// <summary>
    /// Information message.
    /// </summary>
    Info,

    /// <summary>
    /// Success message.
    /// </summary>
    Success,

    /// <summary>
    /// Warning message.
    /// </summary>
    Warning,

    /// <summary>
    /// Error message.
    /// </summary>
    Error,

    /// <summary>
    /// Question/confirmation message.
    /// </summary>
    Question
}

/// <summary>
/// Button configurations for message boxes.
/// </summary>
public enum MessageBoxButtons
{
    /// <summary>
    /// Single OK button.
    /// </summary>
    Ok,

    /// <summary>
    /// OK and Cancel buttons.
    /// </summary>
    OkCancel,

    /// <summary>
    /// Yes and No buttons.
    /// </summary>
    YesNo,

    /// <summary>
    /// Yes, No, and Cancel buttons.
    /// </summary>
    YesNoCancel
}

/// <summary>
/// Result from a message box dialog.
/// </summary>
public enum MessageBoxResult
{
    /// <summary>
    /// No result (dialog was closed without selection).
    /// </summary>
    None,

    /// <summary>
    /// OK button was clicked.
    /// </summary>
    Ok,

    /// <summary>
    /// Cancel button was clicked.
    /// </summary>
    Cancel,

    /// <summary>
    /// Yes button was clicked.
    /// </summary>
    Yes,

    /// <summary>
    /// No button was clicked.
    /// </summary>
    No
}

/// <summary>
/// Event args for message box result selection.
/// </summary>
public class MessageBoxResultEventArgs : EventArgs
{
    /// <summary>
    /// Gets the result that was selected.
    /// </summary>
    public MessageBoxResult Result { get; }

    /// <summary>
    /// Creates a new instance with the specified result.
    /// </summary>
    public MessageBoxResultEventArgs(MessageBoxResult result)
    {
        Result = result;
    }
}

/// <summary>
/// A custom message box control for displaying alerts, confirmations, and messages.
/// </summary>
public partial class MessageBox : UserControl
{
    #region Styled Properties

    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<MessageBox, string?>(nameof(Title));

    public static readonly StyledProperty<string?> MessageProperty =
        AvaloniaProperty.Register<MessageBox, string?>(nameof(Message));

    public static readonly StyledProperty<MessageBoxType> MessageTypeProperty =
        AvaloniaProperty.Register<MessageBox, MessageBoxType>(nameof(MessageType));

    public static readonly StyledProperty<MessageBoxButtons> ButtonsProperty =
        AvaloniaProperty.Register<MessageBox, MessageBoxButtons>(nameof(Buttons));

    public static readonly StyledProperty<bool> ShowIconProperty =
        AvaloniaProperty.Register<MessageBox, bool>(nameof(ShowIcon), true);

    public static readonly StyledProperty<string?> PrimaryButtonTextProperty =
        AvaloniaProperty.Register<MessageBox, string?>(nameof(PrimaryButtonText), "OK");

    public static readonly StyledProperty<string?> SecondaryButtonTextProperty =
        AvaloniaProperty.Register<MessageBox, string?>(nameof(SecondaryButtonText));

    public static readonly StyledProperty<string?> TertiaryButtonTextProperty =
        AvaloniaProperty.Register<MessageBox, string?>(nameof(TertiaryButtonText));

    public static readonly StyledProperty<ICommand?> PrimaryCommandProperty =
        AvaloniaProperty.Register<MessageBox, ICommand?>(nameof(PrimaryCommand));

    public static readonly StyledProperty<ICommand?> SecondaryCommandProperty =
        AvaloniaProperty.Register<MessageBox, ICommand?>(nameof(SecondaryCommand));

    public static readonly StyledProperty<ICommand?> TertiaryCommandProperty =
        AvaloniaProperty.Register<MessageBox, ICommand?>(nameof(TertiaryCommand));

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the message box title.
    /// </summary>
    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <summary>
    /// Gets or sets the message content.
    /// </summary>
    public string? Message
    {
        get => GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    /// <summary>
    /// Gets or sets the message type (determines icon and colors).
    /// </summary>
    public MessageBoxType MessageType
    {
        get => GetValue(MessageTypeProperty);
        set => SetValue(MessageTypeProperty, value);
    }

    /// <summary>
    /// Gets or sets the button configuration.
    /// </summary>
    public MessageBoxButtons Buttons
    {
        get => GetValue(ButtonsProperty);
        set => SetValue(ButtonsProperty, value);
    }

    /// <summary>
    /// Gets or sets whether to show the icon.
    /// </summary>
    public bool ShowIcon
    {
        get => GetValue(ShowIconProperty);
        set => SetValue(ShowIconProperty, value);
    }

    /// <summary>
    /// Gets or sets the primary button text.
    /// </summary>
    public string? PrimaryButtonText
    {
        get => GetValue(PrimaryButtonTextProperty);
        set => SetValue(PrimaryButtonTextProperty, value);
    }

    /// <summary>
    /// Gets or sets the secondary button text.
    /// </summary>
    public string? SecondaryButtonText
    {
        get => GetValue(SecondaryButtonTextProperty);
        set => SetValue(SecondaryButtonTextProperty, value);
    }

    /// <summary>
    /// Gets or sets the tertiary button text.
    /// </summary>
    public string? TertiaryButtonText
    {
        get => GetValue(TertiaryButtonTextProperty);
        set => SetValue(TertiaryButtonTextProperty, value);
    }

    /// <summary>
    /// Gets or sets the primary button command.
    /// </summary>
    public ICommand? PrimaryCommand
    {
        get => GetValue(PrimaryCommandProperty);
        set => SetValue(PrimaryCommandProperty, value);
    }

    /// <summary>
    /// Gets or sets the secondary button command.
    /// </summary>
    public ICommand? SecondaryCommand
    {
        get => GetValue(SecondaryCommandProperty);
        set => SetValue(SecondaryCommandProperty, value);
    }

    /// <summary>
    /// Gets or sets the tertiary button command.
    /// </summary>
    public ICommand? TertiaryCommand
    {
        get => GetValue(TertiaryCommandProperty);
        set => SetValue(TertiaryCommandProperty, value);
    }

    #endregion

    #region Events

    /// <summary>
    /// Event raised when a result is selected.
    /// </summary>
    public event EventHandler<MessageBoxResultEventArgs>? ResultSelected;

    #endregion

    #region Converters

    /// <summary>
    /// Converter to get CSS class for message type.
    /// </summary>
    public static readonly IMultiValueConverter MessageTypeClassConverter = new MessageTypeToClassConverter();

    private class MessageTypeToClassConverter : IMultiValueConverter
    {
        public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values.Count == 0 || values[0] is not MessageBoxType messageType)
                return new Classes("info");

            var className = messageType switch
            {
                MessageBoxType.Info => "info",
                MessageBoxType.Success => "success",
                MessageBoxType.Warning => "warning",
                MessageBoxType.Error => "error",
                MessageBoxType.Question => "question",
                _ => "info"
            };

            return new Classes(className);
        }
    }

    #endregion

    public MessageBox()
    {
        InitializeComponent();
        UpdateButtonConfiguration();
        UpdateMessageTypeClasses();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ButtonsProperty)
        {
            UpdateButtonConfiguration();
        }
        else if (change.Property == MessageTypeProperty)
        {
            UpdateMessageTypeClasses();
        }
    }

    private void UpdateMessageTypeClasses()
    {
        var className = MessageType switch
        {
            MessageBoxType.Info => "info",
            MessageBoxType.Success => "success",
            MessageBoxType.Warning => "warning",
            MessageBoxType.Error => "error",
            MessageBoxType.Question => "question",
            _ => "info"
        };

        var iconBorder = this.FindControl<Border>("IconBorder");
        var iconPath = this.FindControl<PathIcon>("IconPath");
        var primaryButton = this.FindControl<ArgoButton>("PrimaryButton");

        if (iconBorder != null)
        {
            iconBorder.Classes.Clear();
            iconBorder.Classes.Add(className);
        }

        if (iconPath != null)
        {
            iconPath.Classes.Clear();
            iconPath.Classes.Add(className);
        }

        if (primaryButton != null)
        {
            primaryButton.Classes.Clear();
            primaryButton.Classes.Add(className);
        }
    }

    private void UpdateButtonConfiguration()
    {
        // Set default button texts based on Buttons property
        switch (Buttons)
        {
            case MessageBoxButtons.Ok:
                if (string.IsNullOrEmpty(PrimaryButtonText) || PrimaryButtonText == "OK" || PrimaryButtonText == "Yes")
                    PrimaryButtonText = "OK".Translate();
                SecondaryButtonText = null;
                TertiaryButtonText = null;
                break;

            case MessageBoxButtons.OkCancel:
                if (string.IsNullOrEmpty(PrimaryButtonText) || PrimaryButtonText == "Yes")
                    PrimaryButtonText = "OK".Translate();
                SecondaryButtonText = "Cancel".Translate();
                TertiaryButtonText = null;
                break;

            case MessageBoxButtons.YesNo:
                PrimaryButtonText = "Yes".Translate();
                SecondaryButtonText = "No".Translate();
                TertiaryButtonText = null;
                break;

            case MessageBoxButtons.YesNoCancel:
                PrimaryButtonText = "Yes".Translate();
                SecondaryButtonText = "No".Translate();
                TertiaryButtonText = "Cancel".Translate();
                break;
        }

        // Set up default commands if not already set
        PrimaryCommand ??= new RelayCommand(() => OnResultSelected(GetPrimaryResult()));
        SecondaryCommand ??= new RelayCommand(() => OnResultSelected(GetSecondaryResult()));
        TertiaryCommand ??= new RelayCommand(() => OnResultSelected(MessageBoxResult.Cancel));
    }

    private MessageBoxResult GetPrimaryResult()
    {
        return Buttons switch
        {
            MessageBoxButtons.Ok => MessageBoxResult.Ok,
            MessageBoxButtons.OkCancel => MessageBoxResult.Ok,
            MessageBoxButtons.YesNo => MessageBoxResult.Yes,
            MessageBoxButtons.YesNoCancel => MessageBoxResult.Yes,
            _ => MessageBoxResult.Ok
        };
    }

    private MessageBoxResult GetSecondaryResult()
    {
        return Buttons switch
        {
            MessageBoxButtons.OkCancel => MessageBoxResult.Cancel,
            MessageBoxButtons.YesNo => MessageBoxResult.No,
            MessageBoxButtons.YesNoCancel => MessageBoxResult.No,
            _ => MessageBoxResult.Cancel
        };
    }

    private void OnResultSelected(MessageBoxResult result)
    {
        ResultSelected?.Invoke(this, new MessageBoxResultEventArgs(result));
    }

    /// <summary>
    /// Creates a pre-configured MessageBox for info messages.
    /// </summary>
    public static MessageBox CreateInfo(string title, string message, Action<MessageBoxResult>? onResult = null)
    {
        var box = new MessageBox
        {
            Title = title,
            Message = message,
            MessageType = MessageBoxType.Info,
            Buttons = MessageBoxButtons.Ok
        };
        if (onResult != null)
            box.ResultSelected += (_, e) => onResult(e.Result);
        return box;
    }

    /// <summary>
    /// Creates a pre-configured MessageBox for success messages.
    /// </summary>
    public static MessageBox CreateSuccess(string title, string message, Action<MessageBoxResult>? onResult = null)
    {
        var box = new MessageBox
        {
            Title = title,
            Message = message,
            MessageType = MessageBoxType.Success,
            Buttons = MessageBoxButtons.Ok
        };
        if (onResult != null)
            box.ResultSelected += (_, e) => onResult(e.Result);
        return box;
    }

    /// <summary>
    /// Creates a pre-configured MessageBox for warning messages.
    /// </summary>
    public static MessageBox CreateWarning(string title, string message, Action<MessageBoxResult>? onResult = null)
    {
        var box = new MessageBox
        {
            Title = title,
            Message = message,
            MessageType = MessageBoxType.Warning,
            Buttons = MessageBoxButtons.Ok
        };
        if (onResult != null)
            box.ResultSelected += (_, e) => onResult(e.Result);
        return box;
    }

    /// <summary>
    /// Creates a pre-configured MessageBox for error messages.
    /// </summary>
    public static MessageBox CreateError(string title, string message, Action<MessageBoxResult>? onResult = null)
    {
        var box = new MessageBox
        {
            Title = title,
            Message = message,
            MessageType = MessageBoxType.Error,
            Buttons = MessageBoxButtons.Ok
        };
        if (onResult != null)
            box.ResultSelected += (_, e) => onResult(e.Result);
        return box;
    }

    /// <summary>
    /// Creates a pre-configured MessageBox for confirmation dialogs.
    /// </summary>
    public static MessageBox CreateConfirmation(string title, string message, Action<MessageBoxResult>? onResult = null)
    {
        var box = new MessageBox
        {
            Title = title,
            Message = message,
            MessageType = MessageBoxType.Question,
            Buttons = MessageBoxButtons.YesNo
        };
        if (onResult != null)
            box.ResultSelected += (_, e) => onResult(e.Result);
        return box;
    }
}
