using System.Collections.ObjectModel;
using Avalonia;
using ArgoBooks.Helpers;
using ArgoBooks.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// ViewModel for the Categories page first-visit tutorial.
/// </summary>
public partial class CategoriesTutorialViewModel : ViewModelBase
{
    private readonly List<TutorialStep> _steps =
    [
        new TutorialStep
        {
            Title = "Expense & Revenue Categories",
            Description = "Use the tabs above to switch between Expense categories (costs like supplies, utilities, rent) and Revenue categories (income sources like sales, services, consulting).",
            HighlightArea = "tabs"
        },
        new TutorialStep
        {
            Title = "Organize Your Products",
            Description = "Categories help you group similar products and services. This makes it easier to track spending patterns and generate reports.",
            HighlightArea = "content"
        },
        new TutorialStep
        {
            Title = "Example Categories",
            Description = "A bakery might use expense categories like 'Ingredients', 'Packaging', and 'Equipment'. For revenue: 'Cakes', 'Bread', 'Catering'.\n\nYou can nest categories too - like 'Ingredients > Flour' or 'Cakes > Wedding Cakes'.",
            HighlightArea = "none"
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
    private string _currentHighlightArea = "none";

    [ObservableProperty]
    private int _totalSteps;

    [ObservableProperty]
    private bool _isFirstStep = true;

    [ObservableProperty]
    private bool _isLastStep;

    [ObservableProperty]
    private string _progressText = "";

    // Dynamic highlight positioning properties
    [ObservableProperty]
    private bool _showHighlight;

    [ObservableProperty]
    private Thickness _highlightMargin;

    [ObservableProperty]
    private double _highlightWidth;

    [ObservableProperty]
    private double _highlightHeight;

    [ObservableProperty]
    private CornerRadius _highlightCornerRadius = new(0);

    public ObservableCollection<StepIndicator> StepIndicators { get; } = [];

    public event EventHandler? TutorialCompleted;
    public event EventHandler? HighlightAreaChanged;

    /// <summary>
    /// Sets the highlight bounds for the dynamic highlight border.
    /// </summary>
    public void SetHighlightBounds(Rect bounds, CornerRadius cornerRadius)
    {
        HighlightMargin = new Thickness(bounds.X, bounds.Y, 0, 0);
        HighlightWidth = bounds.Width;
        HighlightHeight = bounds.Height;
        HighlightCornerRadius = cornerRadius;
        ShowHighlight = true;
    }

    /// <summary>
    /// Hides the dynamic highlight border.
    /// </summary>
    public void HideHighlight()
    {
        ShowHighlight = false;
    }

    public CategoriesTutorialViewModel()
    {
        TotalSteps = _steps.Count;

        for (int i = 0; i < TotalSteps; i++)
        {
            StepIndicators.Add(new StepIndicator { Index = i, IsActive = false });
        }
    }

    public void ShowIfFirstVisit()
    {
        if (!TutorialService.Instance.ShowFirstVisitHints ||
            TutorialService.Instance.HasVisitedPage(TutorialService.Pages.Categories))
        {
            return;
        }

        StartTutorial();
    }

    public void StartTutorial()
    {
        CurrentStepIndex = 0;
        UpdateCurrentStep();
        IsOpen = true;
    }

    [RelayCommand]
    private void NextStep()
    {
        if (CurrentStepIndex < _steps.Count - 1)
        {
            CurrentStepIndex++;
            UpdateCurrentStep();
        }
        else
        {
            CompleteTutorial();
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
    private void Skip()
    {
        TutorialService.Instance.MarkPageVisited(TutorialService.Pages.Categories);
        IsOpen = false;
    }

    private void CompleteTutorial()
    {
        TutorialService.Instance.MarkPageVisited(TutorialService.Pages.Categories);
        IsOpen = false;
        TutorialCompleted?.Invoke(this, EventArgs.Empty);
    }

    private void UpdateCurrentStep()
    {
        if (CurrentStepIndex >= 0 && CurrentStepIndex < _steps.Count)
        {
            var step = _steps[CurrentStepIndex];
            CurrentTitle = step.Title;
            CurrentDescription = step.Description;
            CurrentHighlightArea = step.HighlightArea;
            IsFirstStep = CurrentStepIndex == 0;
            IsLastStep = CurrentStepIndex == _steps.Count - 1;
            ProgressText = $"{CurrentStepIndex + 1} of {TotalSteps}";

            for (int i = 0; i < StepIndicators.Count; i++)
            {
                StepIndicators[i].IsActive = i == CurrentStepIndex;
            }

            HighlightAreaChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
