using System.Collections.ObjectModel;
using System.Diagnostics;
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

    /// <summary>
    /// Gets or sets whether this is the current item to complete (next in sequence).
    /// Only the current item can be clicked.
    /// </summary>
    [ObservableProperty]
    private bool _isCurrentItem;
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
            Id = TutorialService.ChecklistItems.AddProduct,
            Title = "Create a product",
            Description = "Add items you sell or track",
            Icon = Icons.Products,
            NavigationTarget = "Products"
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
            Id = TutorialService.ChecklistItems.VisitAnalytics,
            Title = "Visit the Analytics page",
            Description = "See your business insights",
            Icon = Icons.Analytics,
            NavigationTarget = "Analytics"
        });

        TotalCount = Items.Count;
        RefreshCompletionState();
    }

    /// <summary>
    /// Refreshes the visibility and completion state of the checklist.
    /// </summary>
    public void Refresh()
    {
        RefreshCompletionState();

        var tutorialService = TutorialService.Instance;

        // If user explicitly dismissed the checklist, always hide it
        if (tutorialService.IsSetupChecklistDismissed)
        {
            IsVisible = false;
            return;
        }

        // Show checklist if there are incomplete items, OR if all items are complete
        // (so the user can see the completion state and close it manually)
        IsVisible = true;
    }

    private void RefreshCompletionState()
    {
        var completedItems = TutorialService.Instance.GetCompletedChecklistItems();

        // Only count items that are in our current visible checklist
        var visibleCompletedCount = 0;
        var foundCurrentItem = false;

        foreach (var item in Items)
        {
            item.IsCompleted = completedItems.Contains(item.Id);
            if (item.IsCompleted)
            {
                visibleCompletedCount++;
                item.IsCurrentItem = false;
            }
            else if (!foundCurrentItem)
            {
                // First incomplete item is the current one
                item.IsCurrentItem = true;
                foundCurrentItem = true;
            }
            else
            {
                // Subsequent incomplete items are not current
                item.IsCurrentItem = false;
            }
        }

        CompletedCount = visibleCompletedCount;
        ProgressText = $"{CompletedCount} of {TotalCount} completed";
        ProgressPercentage = TotalCount > 0 ? (double)CompletedCount / TotalCount * 100 : 0;
        IsAllCompleted = CompletedCount >= TotalCount;
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

    [RelayCommand]
    private void OpenUpgradeUrl()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://www.argorobots.com/upgrade/",
                UseShellExecute = true
            });
        }
        catch
        {
            // Ignore errors opening URL
        }
    }
}
