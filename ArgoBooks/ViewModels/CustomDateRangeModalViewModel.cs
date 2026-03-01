using ArgoBooks.Core.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// ViewModel for the custom date range modal, shared between Dashboard and Analytics pages.
/// </summary>
public partial class CustomDateRangeModalViewModel : ViewModelBase
{
    [ObservableProperty]
    private bool _isOpen;

    [ObservableProperty]
    private DateTime _modalStartDate = new(DateTime.Now.Year, DateTime.Now.Month, 1);

    [ObservableProperty]
    private DateTime _modalEndDate = DateTime.Now;

    private Action<DateTime, DateTime>? _onApply;
    private Action? _onCancel;

    /// <summary>
    /// Gets or sets the modal start date as DateTimeOffset for DatePicker binding.
    /// </summary>
    public DateTimeOffset? ModalStartDateOffset
    {
        get => new DateTimeOffset(ModalStartDate);
        set
        {
            if (value.HasValue)
            {
                ModalStartDate = value.Value.DateTime;
            }
        }
    }

    /// <summary>
    /// Gets or sets the modal end date as DateTimeOffset for DatePicker binding.
    /// </summary>
    public DateTimeOffset? ModalEndDateOffset
    {
        get => new DateTimeOffset(ModalEndDate);
        set
        {
            if (value.HasValue)
            {
                ModalEndDate = value.Value.DateTime;
            }
        }
    }

    /// <summary>
    /// Opens the custom date range modal with the specified dates and callbacks.
    /// </summary>
    /// <param name="startDate">Initial start date.</param>
    /// <param name="endDate">Initial end date.</param>
    /// <param name="onApply">Callback invoked with the selected start and end dates when the user applies.</param>
    /// <param name="onCancel">Callback invoked when the user cancels.</param>
    public void Open(DateTime startDate, DateTime endDate, Action<DateTime, DateTime> onApply, Action? onCancel = null)
    {
        ModalStartDate = startDate;
        ModalEndDate = endDate;
        OnPropertyChanged(nameof(ModalStartDateOffset));
        OnPropertyChanged(nameof(ModalEndDateOffset));
        _onApply = onApply;
        _onCancel = onCancel;
        IsOpen = true;
    }

    [RelayCommand]
    private async Task ApplyCustomDateRange()
    {
        if (ModalStartDate > ModalEndDate)
        {
            var result = await App.ConfirmationDialog!.ShowAsync(new ConfirmationDialogOptions
            {
                Title = "Invalid Date Range",
                Message = "The start date is after the end date. Would you like to swap the dates?",
                PrimaryButtonText = "Swap Dates",
                CancelButtonText = "Cancel"
            });

            if (result == ConfirmationResult.Primary)
            {
                (ModalStartDate, ModalEndDate) = (ModalEndDate, ModalStartDate);
                OnPropertyChanged(nameof(ModalStartDateOffset));
                OnPropertyChanged(nameof(ModalEndDateOffset));
            }
            else
            {
                return;
            }
        }

        IsOpen = false;
        _onApply?.Invoke(ModalStartDate, ModalEndDate);
    }

    [RelayCommand]
    private void CancelCustomDateRange()
    {
        IsOpen = false;
        _onCancel?.Invoke();
    }
}
