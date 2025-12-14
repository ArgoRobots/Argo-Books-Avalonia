using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Modals;

public partial class CreateCompanyWizard : UserControl
{
    public CreateCompanyWizard()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is CreateCompanyViewModel vm)
        {
            vm.BrowseLogoRequested += async (_, _) => await BrowseLogoAsync();

            vm.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(CreateCompanyViewModel.IsOpen) && vm.IsOpen)
                {
                    Dispatcher.UIThread.Post(() => WizardBorder?.Focus(), DispatcherPriority.Background);
                }
            };
        }
    }

    private void Backdrop_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is CreateCompanyViewModel vm)
        {
            vm.CloseCommand.Execute(null);
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
                if (vm.IsLastStep && vm.CanCreate)
                {
                    vm.CreateCompanyCommand.Execute(null);
                }
                else if (vm.CanGoNext && vm.IsStep1Valid)
                {
                    vm.NextStepCommand.Execute(null);
                }
                e.Handled = true;
                break;
        }
    }

    private async Task BrowseLogoAsync()
    {
        if (DataContext is not CreateCompanyViewModel vm) return;

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select Company Logo",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Image Files")
                {
                    Patterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.bmp" },
                    MimeTypes = new[] { "image/png", "image/jpeg", "image/bmp" }
                }
            }
        });

        if (files.Count == 1)
        {
            var file = files[0];
            try
            {
                await using var stream = await file.OpenReadAsync();
                var bitmap = new Bitmap(stream);
                vm.SetLogo(file.Path.LocalPath, bitmap);
            }
            catch
            {
                // Handle error silently or show message
            }
        }
    }
}
