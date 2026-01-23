using System.Collections.ObjectModel;
using ArgoBooks.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// ViewModel for the Categories page first-visit tutorial.
/// </summary>
public partial class CategoriesTutorialViewModel : ViewModelBase
{
    private readonly List<CategoriesTutorialStep> _steps =
    [
        new CategoriesTutorialStep
        {
            Title = "Expense & Revenue Categories",
            Description = "Use the tabs above to switch between Expense categories (costs like supplies, utilities, rent) and Revenue categories (income sources like sales, services, consulting).",
            HighlightArea = "tabs"
        },
        new CategoriesTutorialStep
        {
            Title = "Organize Your Products",
            Description = "Categories help you group similar products and services. This makes it easier to track spending patterns and generate reports.",
            HighlightArea = "content"
        },
        new CategoriesTutorialStep
        {
            Title = "Example Categories",
            Description = "For expenses: 'Office Supplies', 'Marketing', 'Travel'.\nFor revenue: 'Product Sales', 'Service Fees', 'Subscriptions'.\n\nYou can also create sub-categories like 'Marketing > Social Media Ads'.",
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

    public ObservableCollection<StepIndicator> StepIndicators { get; } = [];

    public event EventHandler? TutorialCompleted;
    public event EventHandler? HighlightAreaChanged;

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

public class CategoriesTutorialStep
{
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required string HighlightArea { get; init; }
}
