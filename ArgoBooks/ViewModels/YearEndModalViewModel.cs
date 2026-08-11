using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using ArgoBooks.Core.Data;
using ArgoBooks.Core.Models.Payroll;
using ArgoBooks.Core.Services.Payroll;
using ArgoBooks.Localization;
using ArgoBooks.Services;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// Year end: produce the T4 slips, the summary, and the XML for filing.
///
/// Used once a year, so it is a modal from the Pay runs page rather than a permanent nav item.
/// Nothing here is stored: the return is rebuilt from approved pay runs every time it is
/// opened, so it always agrees with the books.
/// </summary>
public partial class YearEndModalViewModel : ViewModelBase
{
    private readonly T4Service _t4 = new();
    private T4Return? _return;

    [ObservableProperty]
    private bool _isOpen;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasProblems))]
    [NotifyPropertyChangedFor(nameof(CanFile))]
    private int _selectedYear = DateTime.Today.Year;

    public ObservableCollection<int> AvailableYears { get; } = [];

    /// <summary>One row per employee, so the figures can be checked before anything is filed.</summary>
    public ObservableCollection<T4RowViewModel> Rows { get; } = [];

    /// <summary>
    /// Everything CRA would reject, or that would make the filing wrong. Shown all at once
    /// rather than one at a time, because fixing them means visiting several screens and it is
    /// better to know the whole list before starting.
    /// </summary>
    public ObservableCollection<string> Problems { get; } = [];

    public bool HasProblems => Problems.Count > 0;

    /// <summary>
    /// The slips can always be produced. Filing is what a problem blocks, and the distinction
    /// matters: an employer with a missing SIN can still hand out stubs while chasing it.
    /// </summary>
    public bool CanFile => Problems.Count == 0 && Rows.Count > 0;

    public bool HasRows => Rows.Count > 0;

    [ObservableProperty]
    private string _totalIncome = "$0.00";

    [ObservableProperty]
    private string _totalDeductions = "$0.00";

    [ObservableProperty]
    private string _totalRemitted = "$0.00";

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    #region Filing details

    /// <summary>
    /// The three things CRA needs that are not derivable from pay runs. They live here rather
    /// than in company settings because this is the only screen that needs them and the only
    /// screen that reports their absence: being told what is missing and being able to fix it
    /// in the same place beats a message pointing at a settings tab.
    /// </summary>
    [ObservableProperty]
    private string _accountNumber = string.Empty;

    [ObservableProperty]
    private string _contactName = string.Empty;

    [ObservableProperty]
    private string _contactPhone = string.Empty;

    /// <summary>Stops the setters writing back while Open is filling them in.</summary>
    private bool _loading;

    partial void OnAccountNumberChanged(string value) => SaveDetails();

    partial void OnContactNameChanged(string value) => SaveDetails();

    partial void OnContactPhoneChanged(string value) => SaveDetails();

    /// <summary>
    /// Written straight through as they are typed, so the problem list updates live and the
    /// employer can watch the reason they cannot file disappear.
    /// </summary>
    private void SaveDetails()
    {
        if (_loading || App.CompanyManager?.CompanyData is not { } data)
        {
            return;
        }

        data.Settings.Company.PayrollAccountNumber = AccountNumber.Trim();
        data.Settings.Company.PayrollContactName = ContactName.Trim();
        data.Settings.Company.PayrollContactPhone = ContactPhone.Trim();
        App.CompanyManager?.MarkAsChanged();

        Rebuild();
    }

    #endregion

    partial void OnSelectedYearChanged(int value) => Rebuild();

    public void Open()
    {
        CompanyData? data = App.CompanyManager?.CompanyData;

        AvailableYears.Clear();

        // Only years that actually have pay runs. Offering every year since 2020 would invite
        // filing an empty return.
        var years = data?.PayRuns
            .Where(r => r.Status != PayRunStatus.Draft)
            .Select(r => r.PayDate.Year)
            .Distinct()
            .OrderByDescending(y => y)
            .ToList() ?? [];

        foreach (int year in years)
        {
            AvailableYears.Add(year);
        }

        if (AvailableYears.Count == 0)
        {
            AvailableYears.Add(DateTime.Today.Year);
        }

        _loading = true;
        AccountNumber = data?.Settings.Company.PayrollAccountNumber ?? string.Empty;
        ContactName = data?.Settings.Company.PayrollContactName ?? string.Empty;
        ContactPhone = data?.Settings.Company.PayrollContactPhone ?? data?.Settings.Company.Phone ?? string.Empty;
        _loading = false;

        SelectedYear = AvailableYears[0];
        Rebuild();
        IsOpen = true;
    }

    [RelayCommand]
    private void Close() => IsOpen = false;

    private void Rebuild()
    {
        Rows.Clear();
        Problems.Clear();
        StatusMessage = string.Empty;

        CompanyData? data = App.CompanyManager?.CompanyData;
        if (data == null)
        {
            return;
        }

        _return = _t4.Build(data, SelectedYear);

        foreach (T4Slip slip in _return.Slips)
        {
            Rows.Add(new T4RowViewModel
            {
                Name = $"{slip.GivenName} {slip.Surname}".Trim(),
                Income = CurrencyService.Format(slip.EmploymentIncome),
                Cpp = CurrencyService.Format(slip.CppContributions + slip.Cpp2Contributions),
                Ei = CurrencyService.Format(slip.EiPremiums),
                Tax = CurrencyService.Format(slip.IncomeTaxDeducted),
                HasSin = slip.Sin.Count(char.IsAsciiDigit) == 9,
            });
        }

        foreach (string problem in T4Service.Validate(data, _return))
        {
            Problems.Add(problem);
        }

        TotalIncome = CurrencyService.Format(_return.TotalEmploymentIncome);
        TotalDeductions = CurrencyService.Format(
            _return.TotalEmployeeCpp + _return.TotalEmployeeCpp2 + _return.TotalEmployeeEi + _return.TotalIncomeTax);
        TotalRemitted = CurrencyService.Format(
            _return.TotalEmployeeCpp + _return.TotalEmployeeCpp2 + _return.TotalEmployerCpp + _return.TotalEmployerCpp2
            + _return.TotalEmployeeEi + _return.TotalEmployerEi + _return.TotalIncomeTax);

        OnPropertyChanged(nameof(HasProblems));
        OnPropertyChanged(nameof(CanFile));
        OnPropertyChanged(nameof(HasRows));
    }

    /// <summary>Saves a slip per employee plus the summary, into a folder of the user's choosing.</summary>
    [RelayCommand]
    private async Task DownloadSlipsAsync()
    {
        if (_return == null || _return.Slips.Count == 0)
        {
            return;
        }

        string? directory = await PickFolderAsync("Choose where to save the T4 slips");
        if (directory == null)
        {
            return;
        }

        try
        {
            T4Return t4 = _return;

            foreach (T4Slip slip in t4.Slips)
            {
                byte[] bytes = await Task.Run(() => T4PdfRenderer.RenderSlip(t4, slip));
                string name = $"T4-{t4.TaxYear}-{Sanitize($"{slip.GivenName} {slip.Surname}")}.pdf";
                await File.WriteAllBytesAsync(Path.Combine(directory, name), bytes);
            }

            byte[] summary = await Task.Run(() => T4PdfRenderer.RenderSummary(t4));
            await File.WriteAllBytesAsync(Path.Combine(directory, $"T4-Summary-{t4.TaxYear}.pdf"), summary);

            StatusMessage = "Saved {0} slips and the summary.".TranslateFormat(t4.Slips.Count);
        }
        catch (Exception ex)
        {
            App.ErrorLogger?.LogError(ex, Core.Models.Telemetry.ErrorCategory.Validation, "Payroll.T4Slips");
            StatusMessage = "Could not save the slips: {0}".TranslateFormat(ex.Message);
        }
    }

    /// <summary>
    /// Writes the XML CRA accepts. Deliberately separate from the slips: the slips go to
    /// employees and the XML goes to CRA, and producing one does not mean the other happened.
    /// </summary>
    [RelayCommand]
    private async Task ExportXmlAsync()
    {
        if (_return == null || !CanFile)
        {
            return;
        }

        var topLevel = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null;

        if (topLevel?.StorageProvider == null)
        {
            return;
        }

        try
        {
            IStorageFile? file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save the T4 XML for filing".Translate(),
                SuggestedFileName = $"T4-{_return.TaxYear}.xml",
                DefaultExtension = "xml",
                FileTypeChoices = [new FilePickerFileType("XML") { Patterns = ["*.xml"] }],
            });

            if (file == null)
            {
                return;
            }

            await File.WriteAllTextAsync(file.Path.LocalPath, T4XmlWriter.BuildString(_return), new UTF8Encoding(false));

            StatusMessage = "Saved. Upload it through CRA's Internet File Transfer, which will add the transmittal record."
                .Translate();
        }
        catch (Exception ex)
        {
            App.ErrorLogger?.LogError(ex, Core.Models.Telemetry.ErrorCategory.Validation, "Payroll.T4Xml");
            StatusMessage = "Could not save the XML: {0}".TranslateFormat(ex.Message);
        }
    }

    private static async Task<string?> PickFolderAsync(string title)
    {
        var topLevel = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null;

        if (topLevel?.StorageProvider == null)
        {
            return null;
        }

        IReadOnlyList<IStorageFolder> folders = await topLevel.StorageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions { Title = title.Translate(), AllowMultiple = false });

        return folders.Count == 0 ? null : folders[0].Path.LocalPath;
    }

    private static string Sanitize(string name)
    {
        char[] invalid = Path.GetInvalidFileNameChars();
        string result = new(name.Select(c => invalid.Contains(c) || c == ' ' ? '-' : c).ToArray());
        return string.IsNullOrEmpty(result.Trim('-')) ? "employee" : result.Trim('-');
    }
}

/// <summary>One employee's line on the year end review.</summary>
public partial class T4RowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _income = string.Empty;

    [ObservableProperty]
    private string _cpp = string.Empty;

    [ObservableProperty]
    private string _ei = string.Empty;

    [ObservableProperty]
    private string _tax = string.Empty;

    /// <summary>Shown per row, so a missing one can be traced to a person at a glance.</summary>
    [ObservableProperty]
    private bool _hasSin;
}
