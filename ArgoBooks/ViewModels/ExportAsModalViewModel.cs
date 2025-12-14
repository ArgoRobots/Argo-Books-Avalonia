using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// ViewModel for the Export As modal.
/// </summary>
public partial class ExportAsModalViewModel : ViewModelBase
{
    [ObservableProperty]
    private bool _isOpen;

    [ObservableProperty]
    private string? _selectedFormat;

    /// <summary>
    /// Default constructor.
    /// </summary>
    public ExportAsModalViewModel()
    {
    }

    /// <summary>
    /// Opens the modal.
    /// </summary>
    [RelayCommand]
    private void Open()
    {
        SelectedFormat = null;
        IsOpen = true;
    }

    /// <summary>
    /// Closes the modal.
    /// </summary>
    [RelayCommand]
    private void Close()
    {
        IsOpen = false;
    }

    /// <summary>
    /// Selects an export format and proceeds.
    /// </summary>
    [RelayCommand]
    private void SelectFormat(string? format)
    {
        if (string.IsNullOrEmpty(format)) return;

        SelectedFormat = format;
        Close();

        // Raise event to open save file dialog
        FormatSelected?.Invoke(this, format);
    }

    #region Events

    public event EventHandler<string>? FormatSelected;

    #endregion
}
