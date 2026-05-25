using ArgoBooks.Core.Services;
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

    private void OnVisibilityChanged(object? sender, bool show)
    {
        if (_overlay != null)
            _overlay.IsOpen = show;
        if (!show)
            _viewModel.Reset();
    }

    protected override void OnUnloaded(Avalonia.Interactivity.RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        TutorialService.Instance.SourceSurveyVisibilityChanged -= OnVisibilityChanged;
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

    public bool CanSubmit
    {
        get
        {
            if (string.IsNullOrEmpty(SelectedAnswer) || IsSubmitting) return false;
            // When "Other" is selected, require a non-empty freeform answer.
            if (SelectedAnswer == "other" && string.IsNullOrWhiteSpace(OtherText)) return false;
            return true;
        }
    }

    partial void OnSelectedAnswerChanged(string? value) => OnPropertyChanged(nameof(CanSubmit));
    partial void OnIsSubmittingChanged(bool value) => OnPropertyChanged(nameof(CanSubmit));
    partial void OnOtherTextChanged(string value) => OnPropertyChanged(nameof(CanSubmit));

    public bool IsGoogleSelected
    {
        get => SelectedAnswer == "google";
        set { if (value) SelectedAnswer = "google"; }
    }

    public bool IsBingSelected
    {
        get => SelectedAnswer == "bing";
        set { if (value) SelectedAnswer = "bing"; }
    }

    public bool IsYouTubeSelected
    {
        get => SelectedAnswer == "youtube";
        set { if (value) SelectedAnswer = "youtube"; }
    }

    public bool IsRedditSelected
    {
        get => SelectedAnswer == "reddit";
        set { if (value) SelectedAnswer = "reddit"; }
    }

    public bool IsFriendSelected
    {
        get => SelectedAnswer == "friend";
        set { if (value) SelectedAnswer = "friend"; }
    }

    public bool IsEmailSelected
    {
        get => SelectedAnswer == "email";
        set { if (value) SelectedAnswer = "email"; }
    }

    public bool IsOtherSelected
    {
        get => SelectedAnswer == "other";
        set { if (value) SelectedAnswer = "other"; }
    }

    public void Reset()
    {
        SelectedAnswer = null;
        OtherText = string.Empty;
        IsSubmitting = false;
    }

    [RelayCommand]
    private async Task SubmitAsync()
    {
        var answer = SelectedAnswer;
        if (string.IsNullOrEmpty(answer)) return;

        var otherText = answer == "other" ? OtherText.Trim() : null;
        if (answer == "other" && string.IsNullOrEmpty(otherText)) return;

        IsSubmitting = true;
        try
        {
            var reporter = App.SourceSurveyReporter;
            var machineUuid = ReadMachineUuid();
            if (reporter != null && machineUuid != null)
            {
                await reporter.ReportAsync(answer, machineUuid, otherText);
            }
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
