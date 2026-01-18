using Avalonia.Controls;
using Avalonia.Threading;
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

        // Subscribe to property changes to handle focus
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
                // Focus password box when modal opens
                Dispatcher.UIThread.Post(() =>
                {
                    PasswordBox?.Focus();
                }, DispatcherPriority.Loaded);
            }
        }
    }
}
