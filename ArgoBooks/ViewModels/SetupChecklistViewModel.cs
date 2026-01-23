using System.Collections.ObjectModel;
using ArgoBooks.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// Represents a single item in the setup checklist.
/// </summary>
public partial class ChecklistItemViewModel : ObservableObject
{
    [ObservableProperty]
    private string _id = "";

    [ObservableProperty]
    private string _title = "";

    [ObservableProperty]
    private string _description = "";

    [ObservableProperty]
    private string _icon = "";

    [ObservableProperty]
    private string _navigationTarget = "";

    [ObservableProperty]
    private bool _isCompleted;
}

/// <summary>
/// ViewModel for the setup checklist component shown on the dashboard.
/// </summary>
public partial class SetupChecklistViewModel : ViewModelBase
{
    [ObservableProperty]
    private bool _isVisible;

    [ObservableProperty]
    private bool _isExpanded = true;

    [ObservableProperty]
    private int _completedCount;

    [ObservableProperty]
    private int _totalCount;

    [ObservableProperty]
    private string _progressText = "";

    [ObservableProperty]
    private double _progressPercentage;

    [ObservableProperty]
    private bool _isAllCompleted;

    public ObservableCollection<ChecklistItemViewModel> Items { get; } = [];

    /// <summary>
    /// Event raised when a checklist item is clicked for navigation.
    /// </summary>
    public event EventHandler<string>? NavigationRequested;

    public SetupChecklistViewModel()
    {
        InitializeItems();
        TutorialService.Instance.ChecklistItemCompleted += OnChecklistItemCompleted;
        TutorialService.Instance.TutorialStateChanged += OnTutorialStateChanged;
    }

    private void InitializeItems()
    {
        Items.Clear();
        Items.Add(new ChecklistItemViewModel
        {
            Id = TutorialService.ChecklistItems.CreateCategory,
            Title = "Create a category",
            Description = "Organize your transactions",
            Icon = Icons.Categories,
            NavigationTarget = "Categories"
        });
        Items.Add(new ChecklistItemViewModel
        {
            Id = TutorialService.ChecklistItems.AddPaymentMethod,
            Title = "Set up payment methods",
            Description = "Track how money moves",
            Icon = Icons.Payments,
            NavigationTarget = "Payments"
        });
        Items.Add(new ChecklistItemViewModel
        {
            Id = TutorialService.ChecklistItems.RecordExpense,
            Title = "Record your first expense",
            Description = "Log a business expense",
            Icon = Icons.Expenses,
            NavigationTarget = "Expenses"
        });
        Items.Add(new ChecklistItemViewModel
        {
            Id = TutorialService.ChecklistItems.RecordRevenue,
            Title = "Record your first revenue",
            Description = "Log income or a sale",
            Icon = Icons.Revenue,
            NavigationTarget = "Revenue"
        });

        TotalCount = Items.Count;
        RefreshCompletionState();
    }

    /// <summary>
    /// Refreshes the visibility and completion state of the checklist.
    /// </summary>
    public void Refresh()
    {
        IsVisible = TutorialService.Instance.ShouldShowSetupChecklist;
        RefreshCompletionState();
    }

    private void RefreshCompletionState()
    {
        var completedItems = TutorialService.Instance.GetCompletedChecklistItems();

        foreach (var item in Items)
        {
            item.IsCompleted = completedItems.Contains(item.Id);
        }

        CompletedCount = completedItems.Count;
        ProgressText = $"{CompletedCount} of {TotalCount} completed";
        ProgressPercentage = TotalCount > 0 ? (double)CompletedCount / TotalCount * 100 : 0;
        IsAllCompleted = TutorialService.Instance.AreAllChecklistItemsCompleted();
    }

    private void OnChecklistItemCompleted(object? sender, string itemId)
    {
        RefreshCompletionState();
    }

    private void OnTutorialStateChanged(object? sender, EventArgs e)
    {
        Refresh();
    }

    [RelayCommand]
    private void ToggleExpanded()
    {
        IsExpanded = !IsExpanded;
    }

    [RelayCommand]
    private void NavigateToItem(ChecklistItemViewModel? item)
    {
        if (item != null && !string.IsNullOrEmpty(item.NavigationTarget))
        {
            NavigationRequested?.Invoke(this, item.NavigationTarget);
        }
    }

    [RelayCommand]
    private void DismissChecklist()
    {
        TutorialService.Instance.HideSetupChecklist();
        IsVisible = false;
    }

    [RelayCommand]
    private void CompleteAll()
    {
        // Mark all remaining items as completed (user confirms they've done everything)
        foreach (var item in Items)
        {
            if (!item.IsCompleted)
            {
                TutorialService.Instance.CompleteChecklistItem(item.Id);
            }
        }
    }
}
