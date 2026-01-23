using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using ArgoBooks.Services;
using Avalonia;
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
/// Represents a step indicator dot for the tour progress.
/// </summary>
public partial class StepIndicator : ObservableObject
{
    [ObservableProperty]
    private bool _isActive;

    public int Index { get; init; }
}

/// <summary>
/// ViewModel for the interactive app tour overlay.
/// </summary>
public partial class AppTourViewModel : ViewModelBase
{
    private static bool IsMacOS => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
    private static string QuickActionsShortcut => IsMacOS ? "Cmd+K" : "Ctrl+K";

    private static List<TourStep> GetTourSteps() =>
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
            Description = $"Press {QuickActionsShortcut} anytime to open Quick Actions. It's the fastest way to create expenses, revenue, and more.",
            TargetArea = "searchbar",
            Icon = Icons.Lightning
        },
        new TourStep
        {
            Title = "You're All Set!",
            Description = "Check the Setup Checklist on your Dashboard to complete your first tasks. You can restart this tour anytime from the Help menu.",
            TargetArea = "center",
            Icon = Icons.Check
        }
    ];

    private readonly List<TourStep> _tourSteps;

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

    // Dynamic highlight bounds (set by code-behind)
    [ObservableProperty]
    private Thickness _highlightMargin;

    [ObservableProperty]
    private double _highlightWidth = double.NaN;

    [ObservableProperty]
    private double _highlightHeight = double.NaN;

    [ObservableProperty]
    private bool _showHighlight;

    [ObservableProperty]
    private CornerRadius _highlightCornerRadius = new(8);

    /// <summary>
    /// Event raised when the target area changes and bounds need to be recalculated.
    /// </summary>
    public event EventHandler? TargetAreaChanged;

    /// <summary>
    /// Step indicators for the progress dots.
    /// </summary>
    public ObservableCollection<StepIndicator> StepIndicators { get; } = [];

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
        _tourSteps = GetTourSteps();
        TotalSteps = _tourSteps.Count;

        // Initialize step indicators
        for (int i = 0; i < TotalSteps; i++)
        {
            StepIndicators.Add(new StepIndicator { Index = i, IsActive = false });
        }
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
        if (CurrentStepIndex < _tourSteps.Count - 1)
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
        if (CurrentStepIndex >= 0 && CurrentStepIndex < _tourSteps.Count)
        {
            var step = _tourSteps[CurrentStepIndex];
            CurrentTitle = step.Title;
            CurrentDescription = step.Description;
            CurrentTargetArea = step.TargetArea;
            CurrentIcon = step.Icon;
            IsFirstStep = CurrentStepIndex == 0;
            IsLastStep = CurrentStepIndex == _tourSteps.Count - 1;
            ProgressText = $"{CurrentStepIndex + 1} of {TotalSteps}";

            // Update step indicators
            for (int i = 0; i < StepIndicators.Count; i++)
            {
                StepIndicators[i].IsActive = i == CurrentStepIndex;
            }

            // Notify that bounds need to be recalculated
            TargetAreaChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Updates the highlight bounds. Called from code-behind after measuring elements.
    /// </summary>
    public void SetHighlightBounds(Rect bounds, CornerRadius cornerRadius)
    {
        if (bounds == Rect.Empty)
        {
            ShowHighlight = false;
            return;
        }

        HighlightMargin = new Thickness(bounds.Left, bounds.Top, 0, 0);
        HighlightWidth = bounds.Width;
        HighlightHeight = bounds.Height;
        HighlightCornerRadius = cornerRadius;
        ShowHighlight = true;
    }

    /// <summary>
    /// Hides the highlight.
    /// </summary>
    public void HideHighlight()
    {
        ShowHighlight = false;
    }
}
