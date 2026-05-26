using System.Collections.ObjectModel;
using ArgoBooks.Core.Services;
using ArgoBooks.Localization;
using ArgoBooks.Services;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.Controls;

public partial class SourceSurveyOverlay : UserControl
{
    private ModalOverlay? _overlay;
    private readonly SourceSurveyOverlayViewModel _viewModel;

    public SourceSurveyOverlay()
    {
        InitializeComponent();

        _viewModel = new SourceSurveyOverlayViewModel();
        DataContext = _viewModel;

        _overlay = this.FindControl<ModalOverlay>("Overlay");

        TutorialService.Instance.SourceSurveyVisibilityChanged += OnVisibilityChanged;
    }

    private async void OnVisibilityChanged(object? sender, bool show)
    {
        if (_overlay != null)
            _overlay.IsOpen = show;

        if (show)
        {
            // The collection already holds the bundled defaults (instant render);
            // refresh from the server so newly added options appear without a release.
            await _viewModel.LoadOptionsAsync();
        }
        else
        {
            _viewModel.Reset();
        }
    }

    protected override void OnUnloaded(Avalonia.Interactivity.RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        TutorialService.Instance.SourceSurveyVisibilityChanged -= OnVisibilityChanged;
    }
}

/// <summary>
/// A single selectable survey option in the overlay's options list. Raises
/// <see cref="SelectionRequested"/> when the user picks it so the parent
/// view model can enforce single selection and track the chosen key.
/// </summary>
public partial class SurveyOptionItem : ObservableObject
{
    public string Key { get; }
    public string Label { get; }
    public bool Freeform { get; }

    [ObservableProperty]
    private bool _isSelected;

    public event Action<SurveyOptionItem>? SelectionRequested;

    public SurveyOptionItem(string key, string label, bool freeform)
    {
        Key = key;
        Label = label;
        Freeform = freeform;
    }

    partial void OnIsSelectedChanged(bool value)
    {
        if (value)
            SelectionRequested?.Invoke(this);
    }
}

public partial class SourceSurveyOverlayViewModel : ObservableObject
{
    [ObservableProperty]
    private string? _selectedAnswer;

    [ObservableProperty]
    private bool _isSubmitting;

    [ObservableProperty]
    private string _otherText = string.Empty;

    [ObservableProperty]
    private string? _submitError;

    // True when the currently selected option is freeform (reveals the text box).
    [ObservableProperty]
    private bool _isFreeformSelected;

    /// <summary>The options shown as radio choices. Seeded with bundled defaults.</summary>
    public ObservableCollection<SurveyOptionItem> Options { get; } = new();

    public SourceSurveyOverlayViewModel()
    {
        SetOptions(SourceSurveyOptionsService.DefaultOptions);
    }

    public bool CanSubmit
    {
        get
        {
            if (string.IsNullOrEmpty(SelectedAnswer) || IsSubmitting) return false;
            // A freeform option requires a non-empty freeform answer.
            if (IsFreeformSelected && string.IsNullOrWhiteSpace(OtherText)) return false;
            return true;
        }
    }

    partial void OnSelectedAnswerChanged(string? value) => OnPropertyChanged(nameof(CanSubmit));
    partial void OnIsSubmittingChanged(bool value) => OnPropertyChanged(nameof(CanSubmit));
    partial void OnOtherTextChanged(string value) => OnPropertyChanged(nameof(CanSubmit));
    partial void OnIsFreeformSelectedChanged(bool value) => OnPropertyChanged(nameof(CanSubmit));

    /// <summary>
    /// Fetches the latest options from the server and rebuilds the list.
    /// The service returns bundled defaults on failure, so this never throws.
    /// </summary>
    public async Task LoadOptionsAsync()
    {
        var service = App.SourceSurveyOptionsService;
        if (service == null) return;
        var options = await service.GetOptionsAsync();
        SetOptions(options);
    }

    private void SetOptions(IReadOnlyList<SurveyOption> options)
    {
        foreach (var existing in Options)
            existing.SelectionRequested -= OnOptionSelectionRequested;
        Options.Clear();

        foreach (var o in options)
        {
            var item = new SurveyOptionItem(o.Key, o.Label, o.Freeform);
            item.SelectionRequested += OnOptionSelectionRequested;
            Options.Add(item);
        }

        // The option set changed, so clear any prior selection.
        SelectedAnswer = null;
        IsFreeformSelected = false;
    }

    private void OnOptionSelectionRequested(SurveyOptionItem selected)
    {
        // Enforce single selection: deselect every other item.
        foreach (var item in Options)
        {
            if (!ReferenceEquals(item, selected) && item.IsSelected)
                item.IsSelected = false;
        }

        SelectedAnswer = selected.Key;
        IsFreeformSelected = selected.Freeform;
    }

    public void Reset()
    {
        foreach (var item in Options)
            item.IsSelected = false;
        SelectedAnswer = null;
        IsFreeformSelected = false;
        OtherText = string.Empty;
        IsSubmitting = false;
        SubmitError = null;
    }

    [RelayCommand]
    private async Task SubmitAsync()
    {
        var answer = SelectedAnswer;
        if (string.IsNullOrEmpty(answer)) return;

        var isFreeform = IsFreeformSelected;
        var otherText = isFreeform ? OtherText.Trim() : null;
        if (isFreeform && string.IsNullOrEmpty(otherText)) return;

        IsSubmitting = true;
        SubmitError = null;
        try
        {
            var reporter = App.SourceSurveyReporter;
            var machineUuid = ReadMachineUuid();
            if (reporter == null || machineUuid == null)
            {
                SubmitError = "Could not record your answer. Please try again later.".Translate();
                return;
            }

            var ok = await reporter.ReportAsync(answer, machineUuid, otherText);
            if (!ok)
            {
                SubmitError = "Could not record your answer. Please check your connection and try again.".Translate();
                return;
            }

            // Only mark answered after a successful POST so a failure doesn't
            // permanently suppress the survey with no record on the server.
            TutorialService.Instance.MarkSourceSurveyAnswered(answer);
        }
        finally
        {
            IsSubmitting = false;
        }
    }

    private static string? ReadMachineUuid()
    {
        try
        {
            var path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ArgoBooks",
                "machine_uuid.txt");
            if (!File.Exists(path)) return null;
            var raw = File.ReadAllText(path).Trim();
            return Guid.TryParse(raw, out _) ? raw : null;
        }
        catch
        {
            return null;
        }
    }
}
