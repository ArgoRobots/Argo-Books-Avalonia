using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using ArgoBooks.Utilities;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Modals;

/// <summary>
/// Modal dialog for user authentication when opening password-protected company files.
/// </summary>
public partial class LoginModal : UserControl
{
    public LoginModal()
    {
        InitializeComponent();

        // Subscribe to property changes to handle animations
        if (DataContext is LoginModalViewModel vm)
        {
            vm.PropertyChanged += ViewModel_PropertyChanged;
        }

        DataContextChanged += (_, _) =>
        {
            if (DataContext is LoginModalViewModel newVm)
            {
                newVm.PropertyChanged += ViewModel_PropertyChanged;
            }
        };
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(LoginModalViewModel.IsOpen))
        {
            if (DataContext is LoginModalViewModel { IsOpen: true })
            {
                // Trigger opening animation
                Dispatcher.UIThread.Post(() =>
                {
                    var border = this.FindControl<Border>("ModalBorder");
                    if (border != null)
                    {
                        border.Opacity = 1;
                        border.RenderTransform = new Avalonia.Media.ScaleTransform(1, 1);
                    }

                    // Focus password box
                    var passwordBox = this.FindControl<TextBox>("PasswordBox");
                    passwordBox?.Focus();
                }, DispatcherPriority.Loaded);
            }
            else
            {
                // Reset for next opening
                var border = this.FindControl<Border>("ModalBorder");
                if (border != null)
                {
                    border.Opacity = 0;
                    border.RenderTransform = new Avalonia.Media.ScaleTransform(0.95, 0.95);
                }
                ModalHelper.ReturnFocusToAppShell(this);
            }
        }
    }

    private void Backdrop_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is LoginModalViewModel vm)
        {
            vm.CancelCommand.Execute(null);
        }
    }

    private void Modal_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && DataContext is LoginModalViewModel vm)
        {
            vm.CancelCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == Key.Enter && DataContext is LoginModalViewModel loginVm)
        {
            loginVm.LoginCommand.Execute(null);
            e.Handled = true;
        }
    }
}
