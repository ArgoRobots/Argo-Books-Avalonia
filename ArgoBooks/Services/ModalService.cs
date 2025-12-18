using ArgoBooks.Controls;
using ArgoBooks.Core.Services;
using Avalonia.Controls;
using Avalonia.Threading;

namespace ArgoBooks.Services;

/// <summary>
/// Implementation of the modal service for Avalonia.
/// </summary>
public class ModalService : IModalService
{
    private ModalOverlay? _overlay;
    private TaskCompletionSource<ModalResult>? _resultTcs;
    private TaskCompletionSource<object?>? _valueResultTcs;
    private object? _resultValue;

    /// <summary>
    /// Sets the modal overlay control that this service manages.
    /// </summary>
    /// <param name="overlay">The modal overlay control.</param>
    public void SetOverlay(ModalOverlay overlay)
    {
        if (_overlay != null)
        {
            _overlay.Closed -= OnOverlayClosed;
        }

        _overlay = overlay;

        if (_overlay != null)
        {
            _overlay.Closed += OnOverlayClosed;
        }
    }

    /// <inheritdoc />
    public bool IsModalOpen => _overlay?.IsOpen ?? false;

    /// <inheritdoc />
    public event EventHandler<object>? ModalOpened;

    /// <inheritdoc />
    public event EventHandler<ModalResult>? ModalClosed;

    /// <inheritdoc />
    public async Task<ModalResult> ShowAsync<TContent>(TContent content, ModalOptions? options = null) where TContent : class
    {
        if (_overlay == null)
            throw new InvalidOperationException("Modal overlay has not been set. Call SetOverlay first.");

        options ??= new ModalOptions();

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            // Create modal container if content isn't already a ModalContainer
            if (content is not ModalContainer)
            {
                var container = new ModalContainer
                {
                    Title = options.Title,
                    Subtitle = options.Subtitle,
                    ModalContent = content,
                    ShowCloseButton = options.ShowCloseButton,
                    PrimaryButtonText = options.PrimaryButtonText,
                    SecondaryButtonText = options.SecondaryButtonText,
                    Size = ParseSize(options.Size)
                };

                container.CloseRequested += (_, _) => Close(ModalResult.Cancel);
                container.PrimaryCommand = new RelayCommand(() => Close(ModalResult.Ok));
                container.SecondaryCommand = new RelayCommand(() => Close(ModalResult.Cancel));

                _overlay.Content = container;
            }
            else
            {
                _overlay.Content = content;
            }

            _overlay.CloseOnBackdropClick = options.CloseOnBackdropClick;
            _overlay.CloseOnEscape = options.CloseOnEscape;
            _overlay.IsOpen = true;
        });

        _resultTcs = new TaskCompletionSource<ModalResult>();
        ModalOpened?.Invoke(this, content);

        return await _resultTcs.Task;
    }

    /// <inheritdoc />
    public async Task<TResult?> ShowAsync<TContent, TResult>(TContent content, ModalOptions? options = null) where TContent : class
    {
        if (_overlay == null)
            throw new InvalidOperationException("Modal overlay has not been set. Call SetOverlay first.");

        options ??= new ModalOptions();
        _resultValue = null;

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (content is not ModalContainer)
            {
                var container = new ModalContainer
                {
                    Title = options.Title,
                    Subtitle = options.Subtitle,
                    ModalContent = content,
                    ShowCloseButton = options.ShowCloseButton,
                    PrimaryButtonText = options.PrimaryButtonText,
                    SecondaryButtonText = options.SecondaryButtonText,
                    Size = ParseSize(options.Size)
                };

                container.CloseRequested += (_, _) => Close(ModalResult.Cancel);
                _overlay.Content = container;
            }
            else
            {
                _overlay.Content = content;
            }

            _overlay.CloseOnBackdropClick = options.CloseOnBackdropClick;
            _overlay.CloseOnEscape = options.CloseOnEscape;
            _overlay.IsOpen = true;
        });

        _valueResultTcs = new TaskCompletionSource<object?>();
        ModalOpened?.Invoke(this, content);

        var result = await _valueResultTcs.Task;
        return result is TResult typedResult ? typedResult : default;
    }

    /// <inheritdoc />
    public async Task<bool> ConfirmAsync(string title, string message, string confirmText = "Confirm", string cancelText = "Cancel")
    {
        var content = new TextBlock
        {
            Text = message,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap
        };

        var options = new ModalOptions
        {
            Title = title,
            Size = "Small",
            PrimaryButtonText = confirmText,
            SecondaryButtonText = cancelText,
            CloseOnBackdropClick = false
        };

        var result = await ShowAsync(content, options);
        return result == ModalResult.Ok;
    }

    /// <inheritdoc />
    public async Task AlertAsync(string title, string message, string buttonText = "OK")
    {
        var content = new TextBlock
        {
            Text = message,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap
        };

        var options = new ModalOptions
        {
            Title = title,
            Size = "Small",
            PrimaryButtonText = buttonText,
            SecondaryButtonText = null,
            CloseOnBackdropClick = true
        };

        await ShowAsync(content, options);
    }

    /// <inheritdoc />
    public void Close(ModalResult result = ModalResult.None)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (_overlay != null)
            {
                _overlay.IsOpen = false;
            }

            _resultTcs?.TrySetResult(result);
            _valueResultTcs?.TrySetResult(_resultValue);

            ModalClosed?.Invoke(this, result);
        });
    }

    /// <inheritdoc />
    public void Close<TResult>(TResult result)
    {
        _resultValue = result;
        Close(ModalResult.Ok);
    }

    private void OnOverlayClosed(object? sender, EventArgs e)
    {
        // If modal was closed externally (backdrop click, escape), set result
        _resultTcs?.TrySetResult(ModalResult.Cancel);
        _valueResultTcs?.TrySetResult(null);
    }

    private static ModalSize ParseSize(string size)
    {
        return size.ToLowerInvariant() switch
        {
            "small" or "sm" => ModalSize.Small,
            "medium" or "md" => ModalSize.Medium,
            "large" or "lg" => ModalSize.Large,
            "extralarge" or "xl" => ModalSize.ExtraLarge,
            "full" => ModalSize.Full,
            _ => ModalSize.Medium
        };
    }

    /// <summary>
    /// Simple relay command for internal use.
    /// </summary>
    private class RelayCommand : System.Windows.Input.ICommand
    {
        private readonly Action _execute;

        public RelayCommand(Action execute)
        {
            _execute = execute;
        }

        // CanExecute is always true, so we never need to raise CanExecuteChanged
        public event EventHandler? CanExecuteChanged
        {
            add { }
            remove { }
        }

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter) => _execute();
    }
}
