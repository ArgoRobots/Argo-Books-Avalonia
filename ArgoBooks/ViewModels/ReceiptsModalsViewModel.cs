using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// ViewModel for Receipts modals (Filter).
/// </summary>
public partial class ReceiptsModalsViewModel : ViewModelBase
{
    #region Events

    /// <summary>
    /// Raised when filters are applied.
    /// </summary>
    public event EventHandler? FiltersApplied;

    /// <summary>
    /// Raised when filters are cleared.
    /// </summary>
    public event EventHandler? FiltersCleared;

    #endregion

    #region Filter Modal State

    [ObservableProperty]
    private bool _isFilterModalOpen;

    [ObservableProperty]
    private string _filterType = "All";

    [ObservableProperty]
    private DateTimeOffset? _filterDateFrom;

    [ObservableProperty]
    private DateTimeOffset? _filterDateTo;

    [ObservableProperty]
    private string? _filterAmountMin;

    [ObservableProperty]
    private string? _filterAmountMax;

    [ObservableProperty]
    private string _filterSource = "All";

    [ObservableProperty]
    private string _filterFileType = "All";

    /// <summary>
    /// Receipt type filter options.
    /// </summary>
    public ObservableCollection<string> TypeOptions { get; } = ["All", "Expense", "Revenue"];

    /// <summary>
    /// Source filter options.
    /// </summary>
    public ObservableCollection<string> SourceOptions { get; } = ["All", "Manual", "AI Scanned"];

    /// <summary>
    /// File type filter options.
    /// </summary>
    public ObservableCollection<string> FileTypeOptions { get; } = ["All", "Image", "PDF"];

    #endregion

    #region Filter Modal Commands

    /// <summary>
    /// Opens the filter modal.
    /// </summary>
    public void OpenFilterModal()
    {
        IsFilterModalOpen = true;
    }

    /// <summary>
    /// Closes the filter modal.
    /// </summary>
    [RelayCommand]
    private void CloseFilterModal()
    {
        IsFilterModalOpen = false;
    }

    /// <summary>
    /// Applies the current filters.
    /// </summary>
    [RelayCommand]
    private void ApplyFilters()
    {
        FiltersApplied?.Invoke(this, EventArgs.Empty);
        CloseFilterModal();
    }

    /// <summary>
    /// Clears all filters.
    /// </summary>
    [RelayCommand]
    private void ClearFilters()
    {
        FilterType = "All";
        FilterDateFrom = null;
        FilterDateTo = null;
        FilterAmountMin = null;
        FilterAmountMax = null;
        FilterSource = "All";
        FilterFileType = "All";
        FiltersCleared?.Invoke(this, EventArgs.Empty);
        CloseFilterModal();
    }

    #endregion
}
