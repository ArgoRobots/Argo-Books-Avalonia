using Avalonia.Controls;
using Avalonia.Platform.Storage;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Modals;

public partial class InvoiceTemplateDesignerModal : UserControl
{
    public InvoiceTemplateDesignerModal()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Handles logo file selection.
    /// </summary>
    public async Task SelectLogoFile()
    {
        if (DataContext is not InvoiceTemplateDesignerViewModel viewModel)
            return;

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select Logo Image",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Images")
                {
                    Patterns = ["*.png", "*.jpg", "*.jpeg", "*.gif", "*.webp"]
                }
            ]
        });

        if (files.Count > 0)
        {
            var file = files[0];
            var path = file.Path.LocalPath;
            await viewModel.SetLogoFromFileAsync(path);
        }
    }
}
