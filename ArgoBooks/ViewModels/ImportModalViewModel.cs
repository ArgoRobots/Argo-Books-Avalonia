using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// ViewModel for the Import modal.
/// </summary>
public partial class ImportModalViewModel : ViewModelBase
{
    [ObservableProperty]
    private bool _isOpen;

    [ObservableProperty]
    private string? _selectedFormat;

    /// <summary>
    /// Default constructor.
    /// </summary>
    public ImportModalViewModel()
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
    /// Selects an import format and proceeds.
    /// </summary>
    [RelayCommand]
    private void SelectFormat(string? format)
    {
        if (string.IsNullOrEmpty(format)) return;

        SelectedFormat = format;
        Close();

        // Raise event to open file picker
        FormatSelected?.Invoke(this, format);
    }

    #region Events

    public event EventHandler<string>? FormatSelected;

    #endregion
}
