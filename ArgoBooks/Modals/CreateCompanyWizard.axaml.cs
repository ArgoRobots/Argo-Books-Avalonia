using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using ArgoBooks.Utilities;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Modals;

/// <summary>
/// Multi-step wizard for creating a new company file.
/// </summary>
public partial class CreateCompanyWizard : UserControl
{
    private bool _eventsSubscribed;

    public CreateCompanyWizard()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is CreateCompanyViewModel vm && !_eventsSubscribed)
        {
            _eventsSubscribed = true;

            vm.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(CreateCompanyViewModel.IsOpen))
                {
                    if (vm.IsOpen)
                    {
                        // Animate in
                        Dispatcher.UIThread.Post(() =>
                        {
                            if (WizardBorder != null)
                            {
                                WizardBorder.Opacity = 1;
                                WizardBorder.RenderTransform = new ScaleTransform(1, 1);
                            }
                            WizardBorder?.Focus();
                        }, DispatcherPriority.Render);
                    }
                    else
                    {
                        // Reset for next open
                        Dispatcher.UIThread.Post(() =>
                        {
                            if (WizardBorder != null)
                            {
                                WizardBorder.Opacity = 0;
                                WizardBorder.RenderTransform = new ScaleTransform(0.95, 0.95);
                            }
                        }, DispatcherPriority.Background);

                        ModalHelper.ReturnFocusToAppShell(this);
                    }
                }
            };
        }
    }

    private void Wizard_KeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not CreateCompanyViewModel vm) return;

        switch (e.Key)
        {
            case Key.Escape:
                vm.CloseCommand.Execute(null);
                e.Handled = true;
                break;
            case Key.Enter:
                if (vm is { IsLastStep: true, CanCreate: true })
                {
                    vm.CreateCompanyCommand.Execute(null);
                }
                else if (vm is { CanGoNext: true, IsStep1Valid: true })
                {
                    vm.NextStepCommand.Execute(null);
                }
                e.Handled = true;
                break;
        }
    }
}
