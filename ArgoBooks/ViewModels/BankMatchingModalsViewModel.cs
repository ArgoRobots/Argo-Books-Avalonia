using System.Collections.ObjectModel;
using ArgoBooks.Core.Models.BankMatching;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// ViewModel for the Bank Matching modals (the "choose matching book entry" picker).
/// Hosted at the AppShell level so it overlays the whole app, like the other modals.
/// </summary>
public partial class BankMatchingModalsViewModel : ViewModelBase
{
    /// <summary>Raised when the user picks a candidate. The page applies the match.</summary>
    public event EventHandler<BankMatchChosenEventArgs>? CandidateChosen;

    [ObservableProperty]
    private bool _isCandidateModalOpen;

    [ObservableProperty]
    private bool _hasCandidates;

    [ObservableProperty]
    private string _lineDescription = string.Empty;

    private string? _lineId;

    public ObservableCollection<BankMatchCandidate> CandidateOptions { get; } = [];

    /// <summary>Opens the candidate picker for a bank line.</summary>
    public void OpenCandidatePicker(string lineId, string lineDescription, IEnumerable<BankMatchCandidate> candidates)
    {
        _lineId = lineId;
        LineDescription = lineDescription;
        CandidateOptions.Clear();
        foreach (var c in candidates)
            CandidateOptions.Add(c);
        HasCandidates = CandidateOptions.Count > 0;
        IsCandidateModalOpen = true;
    }

    [RelayCommand]
    private void CloseCandidateModal() => IsCandidateModalOpen = false;

    [RelayCommand]
    private void ChooseCandidate(BankMatchCandidate? candidate)
    {
        if (_lineId == null || candidate == null) return;
        IsCandidateModalOpen = false;
        CandidateChosen?.Invoke(this, new BankMatchChosenEventArgs(_lineId, candidate));
    }
}

/// <summary>Event args carrying the user's chosen candidate for a bank line.</summary>
public class BankMatchChosenEventArgs(string lineId, BankMatchCandidate candidate) : EventArgs
{
    public string LineId { get; } = lineId;
    public BankMatchCandidate Candidate { get; } = candidate;
}
