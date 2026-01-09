using ArgoBooks.Core.Data;
using ArgoBooks.Core.Models;

namespace ArgoBooks.Core.Services;

/// <summary>
/// Central manager for company file operations and state.
/// Coordinates between FileService, EncryptionService, and SettingsService.
/// </summary>
public class CompanyManager : IDisposable
{
    private readonly FileService _fileService;
    private readonly IEncryptionService _encryptionService;
    private readonly GlobalSettingsService _settingsService;
    private readonly FooterService _footerService;

    private string? _currentFilePath;
    private string? _currentTempDirectory;
    private string? _currentPassword;
    private CompanyData? _companyData;
    private bool _isDisposed;

    /// <summary>
    /// Gets whether a company is currently open.
    /// </summary>
    public bool IsCompanyOpen => _companyData != null && _currentTempDirectory != null;

    /// <summary>
    /// Gets the current company data.
    /// </summary>
    public CompanyData? CompanyData => _companyData;

    /// <summary>
    /// Gets the current company file path.
    /// </summary>
    public string? CurrentFilePath => _currentFilePath;

    /// <summary>
    /// Gets the current company name.
    /// </summary>
    public string? CurrentCompanyName => _companyData?.Settings.Company.Name;

    /// <summary>
    /// Gets whether the current company has unsaved changes.
    /// </summary>
    public bool HasUnsavedChanges => _companyData?.ChangesMade ?? false;

    /// <summary>
    /// Gets whether the current company file is encrypted.
    /// </summary>
    public bool IsEncrypted => !string.IsNullOrEmpty(_currentPassword);

    /// <summary>
    /// Verifies if the provided password matches the current company's password.
    /// </summary>
    /// <param name="password">The password to verify.</param>
    /// <returns>True if the password matches, false otherwise.</returns>
    public bool VerifyCurrentPassword(string? password)
    {
        if (!IsCompanyOpen) return false;
        if (!IsEncrypted) return string.IsNullOrEmpty(password);
        return _currentPassword == password;
    }

    /// <summary>
    /// Gets the current password for the open company file.
    /// Used for storing password securely for biometric unlock.
    /// </summary>
    /// <returns>The current password, or null if no company is open or it's not encrypted.</returns>
    public string? GetCurrentPassword()
    {
        return _currentPassword;
    }

    /// <summary>
    /// Gets the current company settings.
    /// </summary>
    public CompanySettings? CurrentCompanySettings => _companyData?.Settings;

    /// <summary>
    /// Gets the current company logo file path, if one exists.
    /// </summary>
    public string? CurrentCompanyLogoPath
    {
        get
        {
            if (_companyData?.Settings.Company.LogoFileName == null || _currentTempDirectory == null)
                return null;

            var logoPath = Path.Combine(_currentTempDirectory, _companyData.Settings.Company.LogoFileName);
            return File.Exists(logoPath) ? logoPath : null;
        }
    }

    /// <summary>
    /// Updates the current file path after a file rename.
    /// </summary>
    public void UpdateFilePath(string newPath)
    {
        _currentFilePath = newPath;
    }

    /// <summary>
    /// Event raised when a company is opened.
    /// </summary>
    public event EventHandler<CompanyOpenedEventArgs>? CompanyOpened;

    /// <summary>
    /// Event raised when a company is closed.
    /// </summary>
    public event EventHandler? CompanyClosed;

    /// <summary>
    /// Event raised when a company is saved.
    /// </summary>
    public event EventHandler? CompanySaved;

    /// <summary>
    /// Event raised when the company data changes.
    /// </summary>
    public event EventHandler? CompanyDataChanged;

    /// <summary>
    /// Event raised when a password is needed to open an encrypted file.
    /// </summary>
    public event EventHandler<PasswordRequiredEventArgs>? PasswordRequired;

    /// <summary>
    /// Async callback for requesting password from UI. Set this to enable async password prompts.
    /// </summary>
    public Func<string, Task<string?>>? PasswordRequestCallback { get; set; }

    /// <summary>
    /// Creates a new CompanyManager instance.
    /// </summary>
    public CompanyManager(
        FileService fileService,
        IEncryptionService encryptionService,
        GlobalSettingsService settingsService,
        FooterService footerService)
    {
        _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
        _encryptionService = encryptionService ?? throw new ArgumentNullException(nameof(encryptionService));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _footerService = footerService ?? throw new ArgumentNullException(nameof(footerService));
    }

    /// <summary>
    /// Creates a new company file.
    /// </summary>
    /// <param name="filePath">Path where the file will be saved.</param>
    /// <param name="companyName">Name of the company.</param>
    /// <param name="password">Optional password for encryption.</param>
    /// <param name="companyInfo">Optional company information.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task CreateCompanyAsync(
        string filePath,
        string companyName,
        string? password = null,
        CompanyInfo? companyInfo = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(filePath);
        ArgumentException.ThrowIfNullOrEmpty(companyName);

        // Close any existing company
        if (IsCompanyOpen)
        {
            await CloseCompanyAsync(cancellationToken);
        }

        // Create temporary directory for the new company
        _currentTempDirectory = CreateTempDirectory();

        try
        {
            // Create company directory inside temp
            var companyDir = Path.Combine(_currentTempDirectory, companyName);
            Directory.CreateDirectory(companyDir);

            // Create default company data
            _companyData = new CompanyData();
            _companyData.Settings.Company.Name = companyName;
            _companyData.Settings.AppVersion = "1.0.0";

            // Apply company info if provided
            if (companyInfo != null)
            {
                _companyData.Settings.Company = companyInfo;
            }

            // Save all data to temp directory first (before creating receipts subdirectory,
            // otherwise GetCompanyDirectory will incorrectly find receipts/ as the company dir)
            await _fileService.SaveCompanyDataAsync(companyDir, _companyData, cancellationToken);

            // Create receipts subdirectory after saving data files
            Directory.CreateDirectory(Path.Combine(companyDir, "receipts"));

            // Save to file
            await _fileService.SaveCompanyAsync(filePath, _currentTempDirectory, password, cancellationToken);

            _currentFilePath = filePath;
            _currentPassword = password;

            // Add to recent companies
            _settingsService.AddRecentCompany(filePath);
            await _settingsService.SaveGlobalSettingsAsync(cancellationToken);

            // Raise event
            CompanyOpened?.Invoke(this, new CompanyOpenedEventArgs(companyName, filePath, false));
        }
        catch
        {
            // Clean up on failure
            if (_currentTempDirectory != null && Directory.Exists(_currentTempDirectory))
            {
                Directory.Delete(_currentTempDirectory, recursive: true);
            }
            _currentTempDirectory = null;
            _companyData = null;
            throw;
        }
    }

    /// <summary>
    /// Opens an existing company file.
    /// </summary>
    /// <param name="filePath">Path to the .argo file.</param>
    /// <param name="password">Password if the file is encrypted (or null to prompt).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<bool> OpenCompanyAsync(
        string filePath,
        string? password = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(filePath);

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("Company file not found.", filePath);
        }

        // Close any existing company
        if (IsCompanyOpen)
        {
            await CloseCompanyAsync(cancellationToken);
        }

        // Check if file is encrypted
        var isEncrypted = await _fileService.IsFileEncryptedAsync(filePath);

        if (isEncrypted && string.IsNullOrEmpty(password))
        {
            // Try async callback first (preferred)
            if (PasswordRequestCallback != null)
            {
                password = await PasswordRequestCallback(filePath);
                if (string.IsNullOrEmpty(password))
                {
                    return false;
                }
            }
            else
            {
                // Fall back to synchronous event (for backwards compatibility)
                var args = new PasswordRequiredEventArgs(filePath);
                PasswordRequired?.Invoke(this, args);

                if (args.IsCancelled || string.IsNullOrEmpty(args.Password))
                {
                    return false;
                }

                password = args.Password;
            }
        }

        try
        {
            // Open the file
            _currentTempDirectory = await _fileService.OpenCompanyAsync(filePath, password, cancellationToken);

            // Load company data
            _companyData = await _fileService.LoadCompanyDataAsync(_currentTempDirectory, cancellationToken);

            _currentFilePath = filePath;
            _currentPassword = password;

            // Add to recent companies
            _settingsService.AddRecentCompany(filePath);
            await _settingsService.SaveGlobalSettingsAsync(cancellationToken);

            // Raise event
            CompanyOpened?.Invoke(this, new CompanyOpenedEventArgs(
                _companyData.Settings.Company.Name,
                filePath,
                isEncrypted));

            return true;
        }
        catch (UnauthorizedAccessException)
        {
            // Invalid password - let UI handle retry
            throw;
        }
        catch
        {
            // Clean up on failure
            if (_currentTempDirectory != null && Directory.Exists(_currentTempDirectory))
            {
                Directory.Delete(_currentTempDirectory, recursive: true);
            }
            _currentTempDirectory = null;
            _companyData = null;
            throw;
        }
    }

    /// <summary>
    /// Saves the current company to its file.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task SaveCompanyAsync(CancellationToken cancellationToken = default)
    {
        if (!IsCompanyOpen || _currentFilePath == null || _currentTempDirectory == null)
        {
            throw new InvalidOperationException("No company is currently open.");
        }

        // Save data to temp directory
        var companyDir = GetCompanyDirectory(_currentTempDirectory);
        await _fileService.SaveCompanyDataAsync(companyDir, _companyData!, cancellationToken);

        // Save to file
        await _fileService.SaveCompanyAsync(_currentFilePath, _currentTempDirectory, _currentPassword, cancellationToken);

        // Mark as saved
        _companyData!.MarkAsSaved();

        // Raise event
        CompanySaved?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Saves the current company to a new file location.
    /// </summary>
    /// <param name="newFilePath">New file path.</param>
    /// <param name="newPassword">New password (null to keep existing, empty string to remove).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task SaveCompanyAsAsync(
        string newFilePath,
        string? newPassword = null,
        CancellationToken cancellationToken = default)
    {
        if (!IsCompanyOpen || _currentTempDirectory == null)
        {
            throw new InvalidOperationException("No company is currently open.");
        }

        ArgumentException.ThrowIfNullOrEmpty(newFilePath);

        // Determine password to use
        var passwordToUse = newPassword ?? _currentPassword;
        if (newPassword == string.Empty)
        {
            passwordToUse = null; // Remove encryption
        }

        // Save data to temp directory
        var companyDir = GetCompanyDirectory(_currentTempDirectory);
        await _fileService.SaveCompanyDataAsync(companyDir, _companyData!, cancellationToken);

        // Save to new file
        await _fileService.SaveCompanyAsync(newFilePath, _currentTempDirectory, passwordToUse, cancellationToken);

        // Update current file path and password
        _currentFilePath = newFilePath;
        _currentPassword = passwordToUse;

        // Mark as saved
        _companyData!.MarkAsSaved();

        // Add to recent companies
        _settingsService.AddRecentCompany(newFilePath);
        await _settingsService.SaveGlobalSettingsAsync(cancellationToken);

        // Raise event
        CompanySaved?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Closes the current company.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task CloseCompanyAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTempDirectory != null)
        {
            await _fileService.CloseCompanyAsync(_currentTempDirectory);
            _currentTempDirectory = null;
        }

        _companyData = null;
        _currentFilePath = null;
        _currentPassword = null;

        // Raise event
        CompanyClosed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Sets the company logo from a file path.
    /// </summary>
    /// <param name="logoPath">Path to the logo image file.</param>
    public async Task SetCompanyLogoAsync(string logoPath)
    {
        if (_companyData == null || _currentTempDirectory == null)
            throw new InvalidOperationException("No company is currently open.");

        if (!File.Exists(logoPath))
            throw new FileNotFoundException("Logo file not found.", logoPath);

        // Generate a unique filename for the logo
        var extension = Path.GetExtension(logoPath);
        var logoFileName = $"logo{extension}";
        var destPath = Path.Combine(_currentTempDirectory, logoFileName);

        // Copy the logo file to the temp directory
        await Task.Run(() => File.Copy(logoPath, destPath, overwrite: true));

        // Update settings
        _companyData.Settings.Company.LogoFileName = logoFileName;
        _companyData.ChangesMade = true;

        CompanyDataChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Removes the current company logo.
    /// </summary>
    public async Task RemoveCompanyLogoAsync()
    {
        if (_companyData == null || _currentTempDirectory == null)
            throw new InvalidOperationException("No company is currently open.");

        var logoFileName = _companyData.Settings.Company.LogoFileName;
        if (string.IsNullOrEmpty(logoFileName))
            return;

        var logoPath = Path.Combine(_currentTempDirectory, logoFileName);

        // Delete the logo file if it exists
        if (File.Exists(logoPath))
        {
            await Task.Run(() => File.Delete(logoPath));
        }

        // Update settings
        _companyData.Settings.Company.LogoFileName = null;
        _companyData.ChangesMade = true;

        CompanyDataChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Gets file information from a company file without fully opening it.
    /// </summary>
    /// <param name="filePath">Path to the .argo file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>File footer containing metadata.</returns>
    public async Task<FileFooter?> GetFileInfoAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
            return null;

        return await _footerService.ReadFooterAsync(filePath, cancellationToken);
    }

    /// <summary>
    /// Gets the list of recent companies with their metadata.
    /// </summary>
    /// <returns>List of recent company info.</returns>
    public async Task<List<RecentCompanyInfo>> GetRecentCompaniesAsync(CancellationToken cancellationToken = default)
    {
        var result = new List<RecentCompanyInfo>();
        var recentPaths = _settingsService.GetValidRecentCompanies();

        foreach (var path in recentPaths)
        {
            try
            {
                var footer = await GetFileInfoAsync(path, cancellationToken);
                if (footer != null)
                {
                    result.Add(new RecentCompanyInfo
                    {
                        FilePath = path,
                        CompanyName = footer.CompanyName,
                        IsEncrypted = footer.IsEncrypted,
                        ModifiedAt = footer.ModifiedAt
                    });
                }
            }
            catch
            {
                // File may be corrupted or inaccessible, skip it
            }
        }

        return result;
    }

    /// <summary>
    /// Changes the password for the current company.
    /// </summary>
    /// <param name="newPassword">New password (null or empty to remove encryption).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task ChangePasswordAsync(string? newPassword, CancellationToken cancellationToken = default)
    {
        if (!IsCompanyOpen || _currentFilePath == null)
        {
            throw new InvalidOperationException("No company is currently open.");
        }

        // Save with new password
        await SaveCompanyAsAsync(_currentFilePath, newPassword ?? string.Empty, cancellationToken);
    }

    /// <summary>
    /// Marks the company data as changed.
    /// </summary>
    public void MarkAsChanged()
    {
        if (_companyData != null)
        {
            _companyData.ChangesMade = true;
            CompanyDataChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Opens the containing folder for the current company file.
    /// </summary>
    public void ShowInFolder()
    {
        if (string.IsNullOrEmpty(_currentFilePath))
            return;

        var directory = Path.GetDirectoryName(_currentFilePath);
        if (string.IsNullOrEmpty(directory))
            return;

        try
        {
            if (OperatingSystem.IsWindows())
            {
                System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{_currentFilePath}\"");
            }
            else if (OperatingSystem.IsMacOS())
            {
                System.Diagnostics.Process.Start("open", $"-R \"{_currentFilePath}\"");
            }
            else if (OperatingSystem.IsLinux())
            {
                // Try common file managers
                System.Diagnostics.Process.Start("xdg-open", directory);
            }
        }
        catch
        {
            // Ignore errors opening folder
        }
    }

    private static string CreateTempDirectory()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), "ArgoBooks", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempPath);
        return tempPath;
    }

    private static string GetCompanyDirectory(string tempDirectory)
    {
        var subdirs = Directory.GetDirectories(tempDirectory);
        return subdirs.Length > 0 ? subdirs[0] : tempDirectory;
    }

    public void Dispose()
    {
        if (_isDisposed) return;

        // Clean up temp directory
        if (_currentTempDirectory != null && Directory.Exists(_currentTempDirectory))
        {
            try
            {
                Directory.Delete(_currentTempDirectory, recursive: true);
            }
            catch
            {
                // Best effort cleanup
            }
        }

        _isDisposed = true;
    }
}

/// <summary>
/// Event args for company opened event.
/// </summary>
public class CompanyOpenedEventArgs(string companyName, string filePath, bool isEncrypted) : EventArgs
{
    public string CompanyName { get; } = companyName;
    public string FilePath { get; } = filePath;
    public bool IsEncrypted { get; } = isEncrypted;
}

/// <summary>
/// Event args for password required event.
/// </summary>
public class PasswordRequiredEventArgs(string filePath) : EventArgs
{
    public string FilePath { get; } = filePath;
    public string? Password { get; set; }
    public bool IsCancelled { get; set; }
}

/// <summary>
/// Information about a recent company.
/// </summary>
public class RecentCompanyInfo
{
    public string FilePath { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public bool IsEncrypted { get; set; }
    public DateTime ModifiedAt { get; set; }
}
