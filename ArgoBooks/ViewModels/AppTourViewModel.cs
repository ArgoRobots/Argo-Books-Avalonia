using ArgoBooks.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// Represents a single step in the app tour.
/// </summary>
public class TourStep
{
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required string TargetArea { get; init; }
    public string? Icon { get; init; }
}

/// <summary>
/// ViewModel for the interactive app tour overlay.
/// </summary>
public partial class AppTourViewModel : ViewModelBase
{
    private static readonly List<TourStep> TourSteps =
    [
        new TourStep
        {
            Title = "Welcome to Argo Books",
            Description = "Let's take a quick tour to help you get started. This will only take a minute.",
            TargetArea = "center",
            Icon = Icons.Dashboard
        },
        new TourStep
        {
            Title = "Navigation Sidebar",
            Description = "Use the sidebar to navigate between different sections of the app. You can collapse it by clicking the menu icon at the top.",
            TargetArea = "sidebar",
            Icon = Icons.Menu
        },
        new TourStep
        {
            Title = "Dashboard Overview",
            Description = "The Dashboard shows your key business metrics at a glance. Revenue, expenses, and recent transactions are all here.",
            TargetArea = "content",
            Icon = Icons.Dashboard
        },
        new TourStep
        {
            Title = "Quick Actions",
            Description = "Press Ctrl+K (or Cmd+K on Mac) anytime to open Quick Actions. It's the fastest way to create expenses, revenue, and more.",
            TargetArea = "header",
            Icon = Icons.Lightning
        },
        new TourStep
        {
            Title = "Search & Settings",
            Description = "Use the header to access search, notifications, company settings, and your profile.",
            TargetArea = "header",
            Icon = Icons.Search
        },
        new TourStep
        {
            Title = "You're All Set!",
            Description = "Check the Setup Checklist on your Dashboard to complete your first tasks. You can restart this tour anytime from the Help menu.",
            TargetArea = "center",
            Icon = Icons.Check
        }
    ];

    [ObservableProperty]
    private bool _isOpen;

    [ObservableProperty]
    private int _currentStepIndex;

    [ObservableProperty]
    private string _currentTitle = "";

    [ObservableProperty]
    private string _currentDescription = "";

    [ObservableProperty]
    private string _currentTargetArea = "center";

    [ObservableProperty]
    private string? _currentIcon;

    [ObservableProperty]
    private int _totalSteps;

    [ObservableProperty]
    private bool _isFirstStep = true;

    [ObservableProperty]
    private bool _isLastStep;

    [ObservableProperty]
    private string _progressText = "";

    /// <summary>
    /// Event raised when the tour is completed.
    /// </summary>
    public event EventHandler? TourCompleted;

    /// <summary>
    /// Event raised when the tour is skipped/exited early.
    /// </summary>
    public event EventHandler? TourSkipped;

    public AppTourViewModel()
    {
        TotalSteps = TourSteps.Count;
    }

    /// <summary>
    /// Starts the app tour from the beginning.
    /// </summary>
    public void StartTour()
    {
        CurrentStepIndex = 0;
        UpdateCurrentStep();
        IsOpen = true;
    }

    /// <summary>
    /// Shows the tour if the user hasn't completed it yet.
    /// </summary>
    public void ShowIfNeeded()
    {
        if (!TutorialService.Instance.HasCompletedAppTour)
        {
            StartTour();
        }
    }

    [RelayCommand]
    private void NextStep()
    {
        if (CurrentStepIndex < TourSteps.Count - 1)
        {
            CurrentStepIndex++;
            UpdateCurrentStep();
        }
        else
        {
            CompleteTour();
        }
    }

    [RelayCommand]
    private void PreviousStep()
    {
        if (CurrentStepIndex > 0)
        {
            CurrentStepIndex--;
            UpdateCurrentStep();
        }
    }

    [RelayCommand]
    private void SkipTour()
    {
        IsOpen = false;
        TutorialService.Instance.CompleteAppTour();
        TourSkipped?.Invoke(this, EventArgs.Empty);
    }

    private void CompleteTour()
    {
        IsOpen = false;
        TutorialService.Instance.CompleteAppTour();
        TourCompleted?.Invoke(this, EventArgs.Empty);
    }

    private void UpdateCurrentStep()
    {
        if (CurrentStepIndex >= 0 && CurrentStepIndex < TourSteps.Count)
        {
            var step = TourSteps[CurrentStepIndex];
            CurrentTitle = step.Title;
            CurrentDescription = step.Description;
            CurrentTargetArea = step.TargetArea;
            CurrentIcon = step.Icon;
            IsFirstStep = CurrentStepIndex == 0;
            IsLastStep = CurrentStepIndex == TourSteps.Count - 1;
            ProgressText = $"{CurrentStepIndex + 1} of {TotalSteps}";
        }
    }
}
