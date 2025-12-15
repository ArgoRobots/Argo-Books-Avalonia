using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Modals;

public partial class EditCompanyModal : UserControl
{
    private bool _eventsSubscribed;
    private bool _isFilePickerOpen;

    public EditCompanyModal()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is EditCompanyModalViewModel vm && !_eventsSubscribed)
        {
            _eventsSubscribed = true;
            vm.BrowseLogoRequested += async (_, _) => await BrowseLogoAsync();
        }
    }

    private async Task BrowseLogoAsync()
    {
        if (_isFilePickerOpen) return;
        if (DataContext is not EditCompanyModalViewModel vm) return;

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        try
        {
            _isFilePickerOpen = true;

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
        finally
        {
            _isFilePickerOpen = false;
        }
    }
}
