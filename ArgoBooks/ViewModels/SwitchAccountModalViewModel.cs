using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// Represents an account that can be switched to.
/// </summary>
public partial class AccountItem : ObservableObject
{
    [ObservableProperty]
    private string _id = string.Empty;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private string _initials = string.Empty;

    [ObservableProperty]
    private string _color = "#3B82F6";

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private bool _isCurrent;
}

/// <summary>
/// ViewModel for the switch account modal.
/// </summary>
public partial class SwitchAccountModalViewModel : ViewModelBase
{
    [ObservableProperty]
    private bool _isOpen;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private AccountItem? _selectedAccount;

    /// <summary>
    /// Available accounts to switch between.
    /// </summary>
    public ObservableCollection<AccountItem> Accounts { get; } = new();

    /// <summary>
    /// Default constructor for design-time.
    /// </summary>
    public SwitchAccountModalViewModel()
    {
        // Add sample accounts for design-time and testing
        Accounts.Add(new AccountItem
        {
            Id = "1",
            Name = "Acme Corporation",
            Description = "Main business account",
            Initials = "AC",
            Color = "#3B82F6",
            IsCurrent = true
        });
        Accounts.Add(new AccountItem
        {
            Id = "2",
            Name = "Smith Consulting LLC",
            Description = "Consulting services",
            Initials = "SC",
            Color = "#10B981"
        });
        Accounts.Add(new AccountItem
        {
            Id = "3",
            Name = "Personal Finances",
            Description = "Personal accounting",
            Initials = "PF",
            Color = "#8B5CF6"
        });
        Accounts.Add(new AccountItem
        {
            Id = "4",
            Name = "Johnson & Partners",
            Description = "Investment portfolio",
            Initials = "JP",
            Color = "#F97316"
        });
    }

    #region Commands

    /// <summary>
    /// Opens the modal.
    /// </summary>
    [RelayCommand]
    private void Open()
    {
        SearchQuery = string.Empty;
        IsOpen = true;
    }

    /// <summary>
    /// Closes the modal.
    /// </summary>
    [RelayCommand]
    private void Close()
    {
        IsOpen = false;
    }

    /// <summary>
    /// Selects an account.
    /// </summary>
    [RelayCommand]
    private void SelectAccount(AccountItem? account)
    {
        if (account == null) return;

        // Clear previous selection
        foreach (var acc in Accounts)
        {
            acc.IsSelected = false;
        }

        account.IsSelected = true;
        SelectedAccount = account;
    }

    /// <summary>
    /// Switches to the selected account.
    /// </summary>
    [RelayCommand]
    private void SwitchToAccount()
    {
        if (SelectedAccount == null) return;

        // Update current account
        foreach (var acc in Accounts)
        {
            acc.IsCurrent = acc.Id == SelectedAccount.Id;
        }

        Close();
    }

    /// <summary>
    /// Creates a new account.
    /// </summary>
    [RelayCommand]
    private void CreateAccount()
    {
        // TODO: Open create company wizard
        Close();
    }

    #endregion
}
