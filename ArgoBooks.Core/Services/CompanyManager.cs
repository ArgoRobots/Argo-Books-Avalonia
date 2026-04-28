using System.Security.Cryptography;
using System.Text;
using ArgoBooks.Core.Data;
using ArgoBooks.Core.Models;
using ArgoBooks.Core.Models.Entities;
using ArgoBooks.Core.Models.Telemetry;
using ArgoBooks.Core.Models.Common;

namespace ArgoBooks.Core.Services;

/// <summary>
/// Central manager for company file operations and state.
/// Coordinates between FileService, EncryptionService, and SettingsService.
/// </summary>
public class CompanyManager : IDisposable
{
    private readonly FileService _fileService;
    private readonly GlobalSettingsService _settingsService;
    private readonly FooterService _footerService;
    private readonly IErrorLogger? _errorLogger;

    private readonly SemaphoreSlim _saveLock = new(1, 1);
    private string? _currentTempDirectory;
    private string? _currentPassword;
    private FileStream? _fileLock;
    private bool _isDisposed;

    /// <summary>
    /// Gets whether a company is currently open.
    /// </summary>
    public bool IsCompanyOpen => CompanyData != null && _currentTempDirectory != null;

    /// <summary>
    /// Gets the current company data.
    /// </summary>
    public CompanyData? CompanyData { get; private set; }

    /// <summary>
    /// Gets the current company file path.
    /// </summary>
    public string? CurrentFilePath { get; private set; }

    /// <summary>
    /// Gets the current company name.
    /// </summary>
    public string? CurrentCompanyName => CompanyData?.Settings.Company.Name;

    /// <summary>
    /// Gets whether the current company has unsaved changes.
    /// </summary>
    public bool HasUnsavedChanges => CompanyData?.ChangesMade ?? false;

    /// <summary>
    /// Gets whether the current company file is encrypted.
    /// </summary>
    public bool IsEncrypted => !string.IsNullOrEmpty(_currentPassword);

    /// <summary>
    /// Gets whether the currently open company is the sample company.
    /// The sample company should not be modified directly; use Save As instead.
    /// </summary>
    public bool IsSampleCompany => CurrentFilePath != null &&
        string.Equals(CurrentFilePath, SampleCompanyService.GetSampleCompanyPath(), StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Verifies if the provided password matches the current company's password.
    /// </summary>
    /// <param name="password">The password to verify.</param>
    /// <returns>True if the password matches, false otherwise.</returns>
    public bool VerifyCurrentPassword(string? password)
    {
        if (!IsCompanyOpen) return false;
        if (!IsEncrypted) return string.IsNullOrEmpty(password);

        // Use constant-time comparison to prevent timing attacks
        if (_currentPassword == null || password == null)
            return _currentPassword == null && password == null;

        var storedBytes = Encoding.UTF8.GetBytes(_currentPassword);
        var inputBytes = Encoding.UTF8.GetBytes(password);
        return CryptographicOperations.FixedTimeEquals(storedBytes, inputBytes);
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
    public CompanySettings? CurrentCompanySettings => CompanyData?.Settings;

    /// <summary>
    /// Gets the current company logo file path, if one exists.
    /// </summary>
    public string? CurrentCompanyLogoPath
    {
        get
        {
            if (CompanyData?.Settings.Company.LogoFileName == null || _currentTempDirectory == null)
                return null;

            var logoPath = Path.Combine(_currentTempDirectory, CompanyData.Settings.Company.LogoFileName);
            return File.Exists(logoPath) ? logoPath : null;
        }
    }

    private const string CustomerAvatarSubdirectory = "customer_avatars";
    private const string SupplierAvatarSubdirectory = "supplier_avatars";
    private const int AvatarMaxDimension = 256;

    /// <summary>
    /// Resolves an avatar's relative path to an absolute path within the company temp
    /// directory, or null if the relative path escapes that directory. Defends against
    /// crafted .argo files that set <c>avatarFileName</c> to a traversal path
    /// (e.g. <c>../../etc/passwd</c>) which would otherwise let a load read — or a
    /// remove/rename operation delete or move — files outside the temp directory.
    /// </summary>
    private string? ResolveAvatarPathSafely(string? relativeAvatarPath)
    {
        if (string.IsNullOrEmpty(relativeAvatarPath) || _currentTempDirectory == null)
            return null;

        if (Path.IsPathRooted(relativeAvatarPath))
            return null;

        var candidate = Path.GetFullPath(Path.Combine(_currentTempDirectory, relativeAvatarPath));
        var tempRoot = Path.GetFullPath(_currentTempDirectory);
        var rootWithSep = tempRoot.EndsWith(Path.DirectorySeparatorChar)
            ? tempRoot
            : tempRoot + Path.DirectorySeparatorChar;

        return candidate.StartsWith(rootWithSep, StringComparison.OrdinalIgnoreCase)
            ? candidate
            : null;
    }

    private string? GetEntityAvatarPath(IAvatarOwner? entity)
    {
        if (entity == null) return null;
        var path = ResolveAvatarPathSafely(entity.AvatarFileName);
        return path != null && File.Exists(path) ? path : null;
    }

    private async Task SetEntityAvatarFromPathAsync(IAvatarOwner entity, string sourceImagePath, string subdirectory)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentException.ThrowIfNullOrEmpty(sourceImagePath);
        if (CompanyData == null || _currentTempDirectory == null)
            throw new InvalidOperationException("No company is currently open.");
        if (!File.Exists(sourceImagePath))
            throw new FileNotFoundException("Avatar source file not found.", sourceImagePath);

        var (destPath, relativePath) = PrepareAvatarDestination(entity.Id, subdirectory);
        var ok = await Task.Run(() => ReceiptImageHelper.ResizeAndSaveAsPng(sourceImagePath, destPath, AvatarMaxDimension));
        if (!ok)
            throw new InvalidOperationException("Selected file could not be loaded as an image.");

        FinalizeAvatarUpdate(entity, relativePath);
    }

    private async Task SetEntityAvatarFromBytesAsync(IAvatarOwner entity, byte[] sourceBytes, string subdirectory)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(sourceBytes);
        if (CompanyData == null || _currentTempDirectory == null)
            throw new InvalidOperationException("No company is currently open.");

        var (destPath, relativePath) = PrepareAvatarDestination(entity.Id, subdirectory);
        var ok = await Task.Run(() => ReceiptImageHelper.ResizeBytesAndSaveAsPng(sourceBytes, destPath, AvatarMaxDimension));
        if (!ok)
            throw new InvalidOperationException("Provided bytes could not be decoded as an image.");

        FinalizeAvatarUpdate(entity, relativePath);
    }

    private (string DestPath, string RelativePath) PrepareAvatarDestination(string entityId, string subdirectory)
    {
        var avatarsDir = Path.Combine(_currentTempDirectory!, subdirectory);
        Directory.CreateDirectory(avatarsDir);
        var safeId = SanitizeForFileName(entityId);
        var fileName = $"{safeId}.png";
        var destPath = Path.Combine(avatarsDir, fileName);
        var relativePath = Path.Combine(subdirectory, fileName).Replace('\\', '/');
        return (destPath, relativePath);
    }

    private void FinalizeAvatarUpdate(IAvatarOwner entity, string relativePath)
    {
        entity.AvatarFileName = relativePath;
        entity.UpdatedAt = DateTime.UtcNow;
        CompanyData!.ChangesMade = true;
        CompanyDataChanged?.Invoke(this, EventArgs.Empty);
    }

    private async Task RemoveEntityAvatarAsync(IAvatarOwner entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        if (CompanyData == null || _currentTempDirectory == null)
            throw new InvalidOperationException("No company is currently open.");

        var existing = entity.AvatarFileName;
        if (string.IsNullOrEmpty(existing))
            return;

        // Only delete files that resolve safely under the temp directory — guard against
        // a crafted AvatarFileName escaping into the rest of the filesystem.
        var fullPath = ResolveAvatarPathSafely(existing);
        if (fullPath != null && File.Exists(fullPath))
        {
            await Task.Run(() => File.Delete(fullPath));
        }

        entity.AvatarFileName = null;
        entity.UpdatedAt = DateTime.UtcNow;
        CompanyData.ChangesMade = true;
        CompanyDataChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Move the avatar file to track a renamed entity Id. Failure is non-fatal — if the
    /// file move can't complete, AvatarFileName is left at its previous value and the
    /// avatar simply won't load until the user re-uploads.
    /// </summary>
    private void TryMoveEntityAvatarOnRename(IAvatarOwner entity, string newId, string subdirectory)
    {
        if (string.IsNullOrEmpty(entity.AvatarFileName) || _currentTempDirectory == null)
            return;

        try
        {
            var oldPath = ResolveAvatarPathSafely(entity.AvatarFileName);
            var ext = Path.GetExtension(entity.AvatarFileName);
            var safeNewId = SanitizeForFileName(newId);
            var newRelative = Path.Combine(subdirectory, safeNewId + ext).Replace('\\', '/');
            var newPath = Path.Combine(_currentTempDirectory, newRelative);

            if (oldPath != null && File.Exists(oldPath) && !string.Equals(oldPath, newPath, StringComparison.OrdinalIgnoreCase))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(newPath)!);
                if (File.Exists(newPath))
                    File.Delete(newPath);
                File.Move(oldPath, newPath);
            }
            // Always update AvatarFileName to the new relative path: even if the old
            // file was missing or unsafe, the entity record should now point inside
            // the temp dir using the new Id.
            entity.AvatarFileName = newRelative;
        }
        catch
        {
            // Leave AvatarFileName as-is.
        }
    }

    /// <summary>
    /// Gets the absolute on-disk path to a customer's avatar image, or null when there is no
    /// avatar set, the file is missing, or the stored path escapes the company temp directory.
    /// </summary>
    public string? GetCustomerAvatarPath(Customer customer) => GetEntityAvatarPath(customer);

    /// <summary>
    /// Gets the absolute on-disk path to a supplier's avatar image, or null when there is no
    /// avatar set, the file is missing, or the stored path escapes the company temp directory.
    /// </summary>
    public string? GetSupplierAvatarPath(Supplier supplier) => GetEntityAvatarPath(supplier);

    /// <summary>
    /// Schedules a file rename to be applied on the next save.
    /// The rename is deferred so that closing without saving leaves the original file untouched.
    /// </summary>
    public void SetPendingRename(string newPath)
    {
        PendingRenamePath = newPath;
    }

    /// <summary>
    /// Clears any pending rename (e.g., when changes are undone or discarded).
    /// </summary>
    public void ClearPendingRename()
    {
        PendingRenamePath = null;
    }

    /// <summary>
    /// Gets the path the file will be renamed to on next save, or null if no rename is pending.
    /// </summary>
    public string? PendingRenamePath { get; private set; }

    /// <summary>
    /// Event raised when a company is opened.
    /// </summary>
    public event EventHandler<CompanyOpenedEventArgs>? CompanyOpened;

    /// <summary>
    /// Event raised when a company is closed.
    /// </summary>
    public event EventHandler? CompanyClosed;

    /// <summary>
    /// Event raised just before a company is saved, allowing listeners to sync
    /// in-memory state (like the event log) to CompanyData before persistence.
    /// </summary>
    public event EventHandler? CompanySaving;

    /// <summary>
    /// Event raised when a company is saved.
    /// </summary>
    public event EventHandler? CompanySaved;

    /// <summary>
    /// Event raised when the open company's file was renamed during a save.
    /// Listeners should refresh any cached recent-company UI to reflect the new path.
    /// </summary>
    public event EventHandler<CompanyRenamedEventArgs>? CompanyRenamed;

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
        GlobalSettingsService settingsService,
        FooterService footerService,
        IErrorLogger? errorLogger = null)
    {
        _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _footerService = footerService ?? throw new ArgumentNullException(nameof(footerService));
        _errorLogger = errorLogger;
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
            CompanyData = new CompanyData();
            CompanyData.Settings.Company.Name = companyName;
            CompanyData.Settings.AppVersion = "1.0.0";

            // Apply company info if provided
            if (companyInfo != null)
            {
                CompanyData.Settings.Company = companyInfo;
            }

            if (string.IsNullOrEmpty(CompanyData.Settings.Company.Name))
                CompanyData.Settings.Company.Name = companyName;

            // Save all data to temp directory first (before creating receipts subdirectory,
            // otherwise GetCompanyDirectory will incorrectly find receipts/ as the company dir)
            await _fileService.SaveCompanyDataAsync(companyDir, CompanyData, cancellationToken);

            // Create receipts subdirectory after saving data files
            Directory.CreateDirectory(Path.Combine(companyDir, "receipts"));

            // Save to file
            await _fileService.SaveCompanyAsync(filePath, _currentTempDirectory, password, cancellationToken);

            CurrentFilePath = filePath;
            _currentPassword = password;

            // Hold a read lock on the file to prevent deletion while the company is open
            AcquireFileLock(filePath);

            // Add to recent companies
            _settingsService.AddRecentCompany(filePath);
            await _settingsService.SaveGlobalSettingsAsync(cancellationToken);

            // Raise event
            CompanyOpened?.Invoke(this, new CompanyOpenedEventArgs(companyName, filePath, false));
        }
        catch (Exception ex)
        {
            // Clean up on failure
            _errorLogger?.LogError(ex, ErrorCategory.FileSystem, "Failed to create company");
            if (_currentTempDirectory != null && Directory.Exists(_currentTempDirectory))
            {
                Directory.Delete(_currentTempDirectory, recursive: true);
            }
            _currentTempDirectory = null;
            CompanyData = null;
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
                var args = new PasswordRequiredEventArgs();
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
            CompanyData = await _fileService.LoadCompanyDataAsync(_currentTempDirectory, cancellationToken);

            CurrentFilePath = filePath;
            _currentPassword = password;

            // Sync the company name from the file name so that external renames
            // (e.g., via the OS file explorer) are reflected in the app
            var fileBaseName = Path.GetFileNameWithoutExtension(filePath);
            if (!string.IsNullOrEmpty(fileBaseName) && CompanyData.Settings.Company.Name != fileBaseName)
            {
                CompanyData.Settings.Company.Name = fileBaseName;
            }

            // Hold a read lock on the file to prevent deletion while the company is open
            AcquireFileLock(filePath);

            // Add to recent companies
            _settingsService.AddRecentCompany(filePath);
            await _settingsService.SaveGlobalSettingsAsync(cancellationToken);

            // Raise event
            CompanyOpened?.Invoke(this, new CompanyOpenedEventArgs(
                CompanyData.Settings.Company.Name,
                filePath,
                isEncrypted));

            return true;
        }
        catch (UnauthorizedAccessException)
        {
            // Invalid password - let UI handle retry
            throw;
        }
        catch (Exception ex)
        {
            // Clean up on failure
            ReleaseFileLock();
            _errorLogger?.LogError(ex, ErrorCategory.FileSystem, "Failed to open company");
            if (_currentTempDirectory != null && Directory.Exists(_currentTempDirectory))
            {
                Directory.Delete(_currentTempDirectory, recursive: true);
            }
            _currentTempDirectory = null;
            CompanyData = null;
            throw;
        }
    }

    /// <summary>
    /// Saves the current company to its file.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task SaveCompanyAsync(CancellationToken cancellationToken = default)
    {
        await _saveLock.WaitAsync(cancellationToken);
        try
        {
            if (!IsCompanyOpen || CurrentFilePath == null || _currentTempDirectory == null)
            {
                throw new InvalidOperationException("No company is currently open.");
            }

            // Notify listeners to sync in-memory state before saving
            CompanySaving?.Invoke(this, EventArgs.Empty);

            // Save data to temp directory
            var companyDir = GetCompanyDirectory(_currentTempDirectory);
            await _fileService.SaveCompanyDataAsync(companyDir, CompanyData!, cancellationToken);

            // Apply pending rename before saving so the file is saved at the new path.
            // Capture the rename so we can fire CompanyRenamed AFTER the file save,
            // when the footer at the new path contains the updated company name.
            string? renamedFromPath = null;
            string? renamedToPath = null;

            if (PendingRenamePath != null && PendingRenamePath != CurrentFilePath)
            {
                var oldPath = CurrentFilePath;
                ReleaseFileLock();
                try
                {
                    // overwrite: false makes File.Move atomic — the OS rejects an existing
                    // destination, so there's no TOCTOU window between the check and the move.
                    File.Move(CurrentFilePath, PendingRenamePath, overwrite: false);
                    CurrentFilePath = PendingRenamePath;
                }
                finally
                {
                    AcquireFileLock(CurrentFilePath);
                }

                // Update the recent companies list so the old path is replaced with the new one
                if (CurrentFilePath != oldPath)
                {
                    _settingsService.RemoveRecentCompany(oldPath);
                    _settingsService.AddRecentCompany(CurrentFilePath);
                    await _settingsService.SaveGlobalSettingsAsync(cancellationToken);
                    renamedFromPath = oldPath;
                    renamedToPath = CurrentFilePath;
                }

                PendingRenamePath = null;
            }

            // Release file lock before saving (save uses exclusive access), then re-acquire
            ReleaseFileLock();
            try
            {
                await _fileService.SaveCompanyAsync(CurrentFilePath, _currentTempDirectory, _currentPassword, cancellationToken);
            }
            finally
            {
                AcquireFileLock(CurrentFilePath);
            }

            // Mark as saved
            CompanyData!.MarkAsSaved();

            // Now that the file at the new path contains the freshly-written footer
            // with the updated company name, listeners can refresh recent-company
            // UI from disk and pick up the new name.
            if (renamedFromPath != null && renamedToPath != null)
            {
                CompanyRenamed?.Invoke(this, new CompanyRenamedEventArgs(renamedFromPath, renamedToPath));
            }

            // Raise event
            CompanySaved?.Invoke(this, EventArgs.Empty);
        }
        finally
        {
            _saveLock.Release();
        }
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
        await _saveLock.WaitAsync(cancellationToken);
        try
        {
            if (!IsCompanyOpen || _currentTempDirectory == null)
            {
                throw new InvalidOperationException("No company is currently open.");
            }

            ArgumentException.ThrowIfNullOrEmpty(newFilePath);

            // Notify listeners to sync in-memory state before saving
            CompanySaving?.Invoke(this, EventArgs.Empty);

            // Determine password to use
            var passwordToUse = newPassword ?? _currentPassword;
            if (newPassword == string.Empty)
            {
                passwordToUse = null; // Remove encryption
            }

            // Sync company name with the new file name so they stay consistent
            var newName = Path.GetFileNameWithoutExtension(newFilePath);
            if (!string.IsNullOrEmpty(newName) && CompanyData!.Settings.Company.Name != newName)
            {
                CompanyData.Settings.Company.Name = newName;
            }

            // Save data to temp directory
            var companyDir = GetCompanyDirectory(_currentTempDirectory);
            await _fileService.SaveCompanyDataAsync(companyDir, CompanyData!, cancellationToken);

            // Release file lock before saving, then re-acquire on new path
            ReleaseFileLock();
            try
            {
                await _fileService.SaveCompanyAsync(newFilePath, _currentTempDirectory, passwordToUse, cancellationToken);

                // Update current file path and password
                CurrentFilePath = newFilePath;
                _currentPassword = passwordToUse;
            }
            finally
            {
                AcquireFileLock(newFilePath);
            }

            // Mark as saved
            CompanyData!.MarkAsSaved();

            // Add to recent companies
            _settingsService.AddRecentCompany(newFilePath);
            await _settingsService.SaveGlobalSettingsAsync(cancellationToken);

            // Raise event
            CompanySaved?.Invoke(this, EventArgs.Empty);
        }
        finally
        {
            _saveLock.Release();
        }
    }

    /// <summary>
    /// Closes the current company.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task CloseCompanyAsync(CancellationToken cancellationToken = default)
    {
        ReleaseFileLock();

        if (_currentTempDirectory != null)
        {
            await _fileService.CloseCompanyAsync(_currentTempDirectory);
            _currentTempDirectory = null;
        }

        CompanyData = null;
        CurrentFilePath = null;
        _currentPassword = null;
        PendingRenamePath = null;

        // Raise event
        CompanyClosed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Sets the company logo from a file path.
    /// </summary>
    /// <param name="logoPath">Path to the logo image file.</param>
    public async Task SetCompanyLogoAsync(string logoPath)
    {
        if (CompanyData == null || _currentTempDirectory == null)
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
        CompanyData.Settings.Company.LogoFileName = logoFileName;
        CompanyData.ChangesMade = true;

        CompanyDataChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Removes the current company logo.
    /// </summary>
    public async Task RemoveCompanyLogoAsync()
    {
        if (CompanyData == null || _currentTempDirectory == null)
            throw new InvalidOperationException("No company is currently open.");

        var logoFileName = CompanyData.Settings.Company.LogoFileName;
        if (string.IsNullOrEmpty(logoFileName))
            return;

        var logoPath = Path.Combine(_currentTempDirectory, logoFileName);

        // Delete the logo file if it exists
        if (File.Exists(logoPath))
        {
            await Task.Run(() => File.Delete(logoPath));
        }

        // Update settings
        CompanyData.Settings.Company.LogoFileName = null;
        CompanyData.ChangesMade = true;

        CompanyDataChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Sets a customer's avatar from a source image on disk. The image is resized down
    /// to a small PNG inside the company temp directory, so it gets bundled into the
    /// encrypted .argo file on next save.
    /// </summary>
    public Task SetCustomerAvatarAsync(Customer customer, string sourceImagePath)
        => SetEntityAvatarFromPathAsync(customer, sourceImagePath, CustomerAvatarSubdirectory);

    /// <summary>
    /// Removes a customer's avatar image. Safe to call when no avatar is set.
    /// </summary>
    public Task RemoveCustomerAvatarAsync(Customer customer)
        => RemoveEntityAvatarAsync(customer);

    /// <summary>
    /// Sets a supplier's avatar from a source image on disk. Same semantics as the
    /// customer variant — image is resized to a PNG inside the company temp directory.
    /// </summary>
    public Task SetSupplierAvatarAsync(Supplier supplier, string sourceImagePath)
        => SetEntityAvatarFromPathAsync(supplier, sourceImagePath, SupplierAvatarSubdirectory);

    /// <summary>
    /// Sets a supplier's avatar from already-loaded bytes (e.g. a downloaded favicon).
    /// The bytes can be in any Skia-supported format (ICO, PNG, JPG, ...) and are
    /// re-encoded to PNG.
    /// </summary>
    public Task SetSupplierAvatarFromBytesAsync(Supplier supplier, byte[] imageBytes)
        => SetEntityAvatarFromBytesAsync(supplier, imageBytes, SupplierAvatarSubdirectory);

    /// <summary>
    /// Removes a supplier's avatar image. Safe to call when no avatar is set.
    /// </summary>
    public Task RemoveSupplierAvatarAsync(Supplier supplier)
        => RemoveEntityAvatarAsync(supplier);

    private static string SanitizeForFileName(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return Guid.NewGuid().ToString("N");

        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(raw.Where(c => !invalid.Contains(c) && c != '.').ToArray()).Trim();
        return string.IsNullOrEmpty(cleaned) ? Guid.NewGuid().ToString("N") : cleaned;
    }

    /// <summary>
    /// Renames a customer's Id, cascading to every reference inside the open company
    /// (invoices, revenues, payments, rentals, recurring invoices, returns) and moving
    /// the avatar file. Throws if newId is empty, equals an existing customer's Id,
    /// or if no company is open. A no-op when newId equals the current Id.
    /// </summary>
    public void ChangeCustomerId(Customer customer, string newId)
    {
        ArgumentNullException.ThrowIfNull(customer);
        if (CompanyData == null || _currentTempDirectory == null)
            throw new InvalidOperationException("No company is currently open.");

        var trimmed = newId?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(trimmed))
            throw new ArgumentException("Customer ID cannot be empty.", nameof(newId));

        var oldId = customer.Id;
        if (string.Equals(oldId, trimmed, StringComparison.Ordinal))
            return;

        if (CompanyData.Customers.Any(c => !ReferenceEquals(c, customer) && c.Id == trimmed))
            throw new InvalidOperationException($"Another customer already uses ID '{trimmed}'.");

        // Cascade FK references
        foreach (var inv in CompanyData.Invoices)
            if (inv.CustomerId == oldId) inv.CustomerId = trimmed;
        foreach (var rev in CompanyData.Revenues)
            if (rev.CustomerId == oldId) rev.CustomerId = trimmed;
        foreach (var pay in CompanyData.Payments)
            if (pay.CustomerId == oldId) pay.CustomerId = trimmed;
        foreach (var rent in CompanyData.Rentals)
            if (rent.CustomerId == oldId) rent.CustomerId = trimmed;
        foreach (var ri in CompanyData.RecurringInvoices)
            if (ri.CustomerId == oldId) ri.CustomerId = trimmed;
        foreach (var ret in CompanyData.Returns)
            if (ret.CustomerId == oldId) ret.CustomerId = trimmed;

        TryMoveEntityAvatarOnRename(customer, trimmed, CustomerAvatarSubdirectory);

        customer.Id = trimmed;
        customer.UpdatedAt = DateTime.UtcNow;
        // Cached Id→Customer lookup is keyed on the old Id; invalidate so the next
        // GetCustomer(newId) rebuilds the dictionary.
        CompanyData.InvalidateLookupCaches();
        CompanyData.ChangesMade = true;
        CompanyDataChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Renames a supplier's Id, cascading to every reference inside the open company
    /// (products, purchase orders, returns, expenses).
    /// </summary>
    public void ChangeSupplierId(Supplier supplier, string newId)
    {
        ArgumentNullException.ThrowIfNull(supplier);
        if (CompanyData == null)
            throw new InvalidOperationException("No company is currently open.");

        var trimmed = newId?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(trimmed))
            throw new ArgumentException("Supplier ID cannot be empty.", nameof(newId));

        var oldId = supplier.Id;
        if (string.Equals(oldId, trimmed, StringComparison.Ordinal))
            return;

        if (CompanyData.Suppliers.Any(s => !ReferenceEquals(s, supplier) && s.Id == trimmed))
            throw new InvalidOperationException($"Another supplier already uses ID '{trimmed}'.");

        foreach (var prod in CompanyData.Products)
            if (prod.SupplierId == oldId) prod.SupplierId = trimmed;
        foreach (var po in CompanyData.PurchaseOrders)
            if (po.SupplierId == oldId) po.SupplierId = trimmed;
        foreach (var ret in CompanyData.Returns)
            if (ret.SupplierId == oldId) ret.SupplierId = trimmed;
        foreach (var exp in CompanyData.Expenses)
            if (exp.SupplierId == oldId) exp.SupplierId = trimmed;

        TryMoveEntityAvatarOnRename(supplier, trimmed, SupplierAvatarSubdirectory);

        supplier.Id = trimmed;
        supplier.UpdatedAt = DateTime.UtcNow;
        CompanyData.InvalidateLookupCaches();
        CompanyData.ChangesMade = true;
        CompanyDataChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Renames a product's Id, cascading to every reference inside the open company
    /// (inventory items, line items on invoices/revenues/expenses, purchase orders,
    /// lost-damaged records, return items).
    /// </summary>
    public void ChangeProductId(Product product, string newId)
    {
        ArgumentNullException.ThrowIfNull(product);
        if (CompanyData == null)
            throw new InvalidOperationException("No company is currently open.");

        var trimmed = newId?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(trimmed))
            throw new ArgumentException("Product ID cannot be empty.", nameof(newId));

        var oldId = product.Id;
        if (string.Equals(oldId, trimmed, StringComparison.Ordinal))
            return;

        if (CompanyData.Products.Any(p => !ReferenceEquals(p, product) && p.Id == trimmed))
            throw new InvalidOperationException($"Another product already uses ID '{trimmed}'.");

        foreach (var item in CompanyData.Inventory)
            if (item.ProductId == oldId) item.ProductId = trimmed;
        foreach (var po in CompanyData.PurchaseOrders)
            if (po.ProductId == oldId) po.ProductId = trimmed;
        foreach (var ld in CompanyData.LostDamaged)
            if (ld.ProductId == oldId) ld.ProductId = trimmed;

        // Line items live on the parent (Invoice / Revenue / Expense) — both Revenue
        // and Expense derive from Transaction which exposes LineItems.
        foreach (var inv in CompanyData.Invoices)
            CascadeProductIdInLineItems(inv.LineItems, oldId, trimmed);
        foreach (var rev in CompanyData.Revenues)
            CascadeProductIdInLineItems(rev.LineItems, oldId, trimmed);
        foreach (var exp in CompanyData.Expenses)
            CascadeProductIdInLineItems(exp.LineItems, oldId, trimmed);

        // Return items live nested inside Return.Items
        foreach (var ret in CompanyData.Returns)
        {
            foreach (var ri in ret.Items)
                if (ri.ProductId == oldId) ri.ProductId = trimmed;
        }

        product.Id = trimmed;
        product.UpdatedAt = DateTime.UtcNow;
        CompanyData.InvalidateLookupCaches();
        CompanyData.ChangesMade = true;
        CompanyDataChanged?.Invoke(this, EventArgs.Empty);
    }

    private static void CascadeProductIdInLineItems(List<LineItem> lineItems, string oldId, string newId)
    {
        foreach (var li in lineItems)
            if (li.ProductId == oldId) li.ProductId = newId;
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
    /// Extracts the company logo from a .argo file without fully opening it.
    /// Returns null for files without a logo.
    /// </summary>
    public Task<byte[]?> ExtractLogoFromFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        return _fileService.ExtractLogoFromFileAsync(filePath, cancellationToken);
    }

    /// <summary>
    /// Gets the list of recent companies with their metadata.
    /// </summary>
    /// <returns>List of recent company info.</returns>
    public async Task<List<RecentCompanyInfo>> GetRecentCompaniesAsync(CancellationToken cancellationToken = default)
    {
        var recentPaths = _settingsService.GetValidRecentCompanies();

        // Footer reads are pure I/O on independent files opened with FileShare.Read, so we can
        // run them concurrently. Per-task try/catch preserves the previous skip-on-error behavior;
        // Task.WhenAll returns results in input order, preserving most-recent-first ordering.
        var tasks = recentPaths.Select(async path =>
        {
            try
            {
                var footer = await GetFileInfoAsync(path, cancellationToken);
                if (footer == null)
                    return null;

                return new RecentCompanyInfo
                {
                    FilePath = path,
                    CompanyName = footer.CompanyName,
                    IsEncrypted = footer.IsEncrypted,
                    ModifiedAt = footer.ModifiedAt,
                    LogoThumbnail = footer.LogoThumbnail
                };
            }
            catch
            {
                // File may be corrupted or inaccessible, skip it
                return null;
            }
        });

        var results = await Task.WhenAll(tasks);
        return results.Where(r => r != null).Cast<RecentCompanyInfo>().ToList();
    }

    /// <summary>
    /// Changes the password for the current company.
    /// This re-encrypts the file with the new password WITHOUT saving any pending data changes.
    /// </summary>
    /// <param name="newPassword">New password (null or empty to remove encryption).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task ChangePasswordAsync(string? newPassword, CancellationToken cancellationToken = default)
    {
        await _saveLock.WaitAsync(cancellationToken);
        try
        {
            if (!IsCompanyOpen || CurrentFilePath == null || _currentTempDirectory == null)
            {
                throw new InvalidOperationException("No company is currently open.");
            }

            // Determine password to use
            var passwordToUse = string.IsNullOrEmpty(newPassword) ? null : newPassword;

            // Re-encrypt the file with the new password WITHOUT saving data changes
            // This only packages the existing temp directory content with the new encryption
            // Release file lock before saving (save uses exclusive access), then re-acquire
            ReleaseFileLock();
            try
            {
                await _fileService.SaveCompanyAsync(CurrentFilePath, _currentTempDirectory, passwordToUse, cancellationToken);
            }
            finally
            {
                AcquireFileLock(CurrentFilePath);
            }

            // Update current password
            _currentPassword = passwordToUse;

            // Note: We intentionally do NOT:
            // - Call SaveCompanyDataAsync (preserves unsaved changes in memory)
            // - Call _companyData.MarkAsSaved() (keeps HasUnsavedChanges state)
            // - Raise CompanySaved event (no data was saved, only password changed)
        }
        finally
        {
            _saveLock.Release();
        }
    }

    /// <summary>
    /// Exports a backup of the current company to a .argobk file.
    /// This saves the current in-memory state to a separate file without affecting
    /// the working file, file lock, or unsaved changes state.
    /// </summary>
    /// <param name="backupPath">The path for the .argobk backup file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task ExportBackupAsync(
        string backupPath,
        CancellationToken cancellationToken = default)
    {
        await _saveLock.WaitAsync(cancellationToken);
        try
        {
            if (!IsCompanyOpen || _currentTempDirectory == null || CompanyData == null)
            {
                throw new InvalidOperationException("No company is currently open.");
            }

            ArgumentException.ThrowIfNullOrEmpty(backupPath);

            // Sync in-memory state before exporting (e.g., event log)
            CompanySaving?.Invoke(this, EventArgs.Empty);

            // Save current data to temp directory
            var companyDir = GetCompanyDirectory(_currentTempDirectory);
            await _fileService.SaveCompanyDataAsync(companyDir, CompanyData, cancellationToken);

            // Export the entire temp directory as-is (includes receipts/)
            await _fileService.SaveCompanyAsync(backupPath, _currentTempDirectory, null, cancellationToken);

            // Note: We intentionally do NOT:
            // - Change _currentFilePath (backup is a separate file)
            // - Release/acquire file lock (working file stays locked)
            // - Mark as saved (unsaved changes state is unchanged)
            // - Add to recent companies (backups are not working files)
        }
        finally
        {
            _saveLock.Release();
        }
    }

    /// <summary>
    /// Saves only the payment-sync-related files (payments, invoices, revenues, id counters, settings)
    /// to the temp directory and repackages the .argo file, without triggering a full company save
    /// workflow (no CompanySaving/CompanySaved events, no MarkAsSaved).
    /// </summary>
    public async Task SavePaymentSyncAsync(CancellationToken cancellationToken = default)
    {
        await _saveLock.WaitAsync(cancellationToken);
        try
        {
            if (!IsCompanyOpen || CurrentFilePath == null || _currentTempDirectory == null || CompanyData == null)
                return;

            var companyDir = GetCompanyDirectory(_currentTempDirectory);
            await _fileService.SaveCompanyDataAsync(companyDir, CompanyData, cancellationToken);

            // Repackage the .argo file so changes persist across restarts
            ReleaseFileLock();
            try
            {
                await _fileService.SaveCompanyAsync(CurrentFilePath, _currentTempDirectory, _currentPassword, cancellationToken);
            }
            finally
            {
                AcquireFileLock(CurrentFilePath);
            }
        }
        finally
        {
            _saveLock.Release();
        }
    }

    /// <summary>
    /// Marks the company data as changed.
    /// </summary>
    public void MarkAsChanged()
    {
        if (CompanyData != null)
        {
            CompanyData.ChangesMade = true;
            CompanyDataChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Notifies listeners that company data has been updated without marking as modified.
    /// Used for in-memory transformations like sample data time-shifting.
    /// </summary>
    public void NotifyDataChanged()
    {
        CompanyDataChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Opens the containing folder for the current company file.
    /// </summary>
    public void ShowInFolder()
    {
        if (string.IsNullOrEmpty(CurrentFilePath))
            return;

        var directory = Path.GetDirectoryName(CurrentFilePath);
        if (string.IsNullOrEmpty(directory))
            return;

        try
        {
            if (OperatingSystem.IsWindows())
            {
                System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{CurrentFilePath}\"");
            }
            else if (OperatingSystem.IsMacOS())
            {
                System.Diagnostics.Process.Start("open", $"-R \"{CurrentFilePath}\"");
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
        if (subdirs.Length == 0) return tempDirectory;
        if (subdirs.Length == 1) return subdirs[0];

        // Multiple subdirectories: pick the one that contains company data files
        // (exclude known non-company directories like "receipts")
        var companyDir = subdirs.FirstOrDefault(d =>
            File.Exists(Path.Combine(d, "settings.json")) ||
            File.Exists(Path.Combine(d, "revenues.json")) ||
            File.Exists(Path.Combine(d, "expenses.json")));

        return companyDir ?? subdirs[0];
    }

    public void Dispose()
    {
        if (_isDisposed) return;

        ReleaseFileLock();
        _saveLock.Dispose();
        _currentPassword = null;

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

    /// <summary>
    /// Acquires a read lock on the company file to prevent deletion while open.
    /// </summary>
    private void AcquireFileLock(string filePath)
    {
        ReleaseFileLock();
        try
        {
            _fileLock = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }
        catch (Exception ex)
        {
            _errorLogger?.LogWarning($"Could not acquire file lock on {filePath}: {ex.Message}", "FileLock");
        }
    }

    /// <summary>
    /// Releases the file lock on the company file.
    /// </summary>
    private void ReleaseFileLock()
    {
        if (_fileLock != null)
        {
            _fileLock.Dispose();
            _fileLock = null;
        }
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
/// Event args for the company renamed event.
/// </summary>
public class CompanyRenamedEventArgs(string oldFilePath, string newFilePath) : EventArgs
{
    public string OldFilePath { get; } = oldFilePath;
    public string NewFilePath { get; } = newFilePath;
}

/// <summary>
/// Event args for password required event.
/// </summary>
public class PasswordRequiredEventArgs() : EventArgs
{
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
    public string? LogoThumbnail { get; set; }
}
