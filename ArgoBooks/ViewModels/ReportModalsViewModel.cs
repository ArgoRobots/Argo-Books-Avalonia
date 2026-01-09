using CommunityToolkit.Mvvm.ComponentModel;

namespace ArgoBooks.ViewModels;

/// <summary>
/// ViewModel for report-related modals (Page Settings, Save Template, etc.).
/// Acts as a bridge to provide access to the ReportsPageViewModel from the AppShell level.
/// </summary>
public partial class ReportModalsViewModel : ViewModelBase
{
    [ObservableProperty]
    private ReportsPageViewModel? _reportsPageViewModel;

    /// <summary>
    /// Gets whether the page settings modal is open.
    /// </summary>
    public bool IsPageSettingsOpen => ReportsPageViewModel?.IsPageSettingsOpen ?? false;

    /// <summary>
    /// Gets whether the save template modal is open.
    /// </summary>
    public bool IsSaveTemplateOpen => ReportsPageViewModel?.IsSaveTemplateOpen ?? false;

    /// <summary>
    /// Gets whether the delete template modal is open.
    /// </summary>
    public bool IsDeleteTemplateOpen => ReportsPageViewModel?.IsDeleteTemplateOpen ?? false;

    /// <summary>
    /// Gets whether the rename template modal is open.
    /// </summary>
    public bool IsRenameTemplateOpen => ReportsPageViewModel?.IsRenameTemplateOpen ?? false;

    partial void OnReportsPageViewModelChanged(ReportsPageViewModel? oldValue, ReportsPageViewModel? newValue)
    {
        // Unsubscribe from old ViewModel
        if (oldValue != null)
        {
            oldValue.PropertyChanged -= OnReportsPageViewModelPropertyChanged;
        }

        // Subscribe to new ViewModel
        if (newValue != null)
        {
            newValue.PropertyChanged += OnReportsPageViewModelPropertyChanged;
        }

        // Notify all modal visibility properties
        NotifyModalVisibilityChanged();
    }

    private void OnReportsPageViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // Forward relevant property changes
        switch (e.PropertyName)
        {
            case nameof(ReportsPageViewModel.IsPageSettingsOpen):
                OnPropertyChanged(nameof(IsPageSettingsOpen));
                break;
            case nameof(ReportsPageViewModel.IsSaveTemplateOpen):
                OnPropertyChanged(nameof(IsSaveTemplateOpen));
                break;
            case nameof(ReportsPageViewModel.IsDeleteTemplateOpen):
                OnPropertyChanged(nameof(IsDeleteTemplateOpen));
                break;
            case nameof(ReportsPageViewModel.IsRenameTemplateOpen):
                OnPropertyChanged(nameof(IsRenameTemplateOpen));
                break;
        }
    }

    private void NotifyModalVisibilityChanged()
    {
        OnPropertyChanged(nameof(IsPageSettingsOpen));
        OnPropertyChanged(nameof(IsSaveTemplateOpen));
        OnPropertyChanged(nameof(IsDeleteTemplateOpen));
        OnPropertyChanged(nameof(IsRenameTemplateOpen));
    }
}
