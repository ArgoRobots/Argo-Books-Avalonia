using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// ViewModel for the Check for Update modal.
/// </summary>
public partial class CheckForUpdateModalViewModel : ViewModelBase
{
    [ObservableProperty]
    private bool _isOpen;

    [ObservableProperty]
    private string _currentVersion = "2.4.1";

    [ObservableProperty]
    private string _newVersion = "2.5.0";

    [ObservableProperty]
    private string _lastChecked = "Just now";

    [ObservableProperty]
    private bool _isChecking;

    [ObservableProperty]
    private bool _isUpToDate;

    [ObservableProperty]
    private bool _hasUpdate;

    [ObservableProperty]
    private bool _hasError;

    /// <summary>
    /// Default constructor.
    /// </summary>
    public CheckForUpdateModalViewModel()
    {
        // Default state: up to date
        _isUpToDate = true;
    }

    /// <summary>
    /// Opens the modal and starts checking for updates.
    /// </summary>
    [RelayCommand]
    private async Task Open()
    {
        IsOpen = true;
        await CheckForUpdates();
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
    /// Checks for updates.
    /// </summary>
    [RelayCommand]
    private async Task CheckForUpdates()
    {
        // Reset states
        IsChecking = true;
        IsUpToDate = false;
        HasUpdate = false;
        HasError = false;

        try
        {
            // Simulate checking for updates
            await Task.Delay(1500);

            // For demo purposes, show "up to date"
            IsUpToDate = true;
            LastChecked = DateTime.Now.ToString("MMM d, yyyy 'at' h:mm tt");
        }
        catch
        {
            HasError = true;
        }
        finally
        {
            IsChecking = false;
        }
    }

    /// <summary>
    /// Downloads the update.
    /// </summary>
    [RelayCommand]
    private void DownloadUpdate()
    {
        // TODO: Open download URL in browser
        DownloadUpdateRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Views the release notes.
    /// </summary>
    [RelayCommand]
    private void ViewReleaseNotes()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://argorobots.com/whats-new/",
                UseShellExecute = true
            });
        }
        catch
        {
            // Ignore errors opening URL
        }
        ViewReleaseNotesRequested?.Invoke(this, EventArgs.Empty);
    }

    #region Events

    public event EventHandler? DownloadUpdateRequested;
    public event EventHandler? ViewReleaseNotesRequested;

    #endregion
}
