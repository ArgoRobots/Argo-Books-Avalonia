# Security Audit Report - Argo Books Avalonia

**Date:** 2026-03-14
**Scope:** Full application security audit
**Application Version:** 2.0.2
**Framework:** .NET 10 / Avalonia 11.3.12

---

## Executive Summary

This audit covers cryptography, authentication, file handling, external API integrations, input validation, dependency management, and secrets handling across the entire Argo Books codebase. **1 critical**, **2 high**, **12 medium**, and **4 low** severity findings were identified. The most urgent issue is a cryptographic design flaw where the password verification hash stored in the plaintext file footer is identical to the AES-256 encryption key, completely defeating file encryption.

---

## Findings

### CRITICAL

#### C-1: Password Hash in Footer Is Identical to the Encryption Key
**Files:** `ArgoBooks.Core/Security/KeyDerivation.cs:48-58, 115-126`, `ArgoBooks.Core/Services/FileService.cs:152-155`, `ArgoBooks.Core/Models/FileFooter.cs:30-33`
**CVSS Estimate:** 9.1

`DeriveKey()` produces `PBKDF2(password, salt, 100000, SHA256, outputLength=32)` and `ComputePasswordHash()` produces `PBKDF2(password, salt, 100000, SHA256, outputLength=32)`. These are identical functions with identical parameters. In `FileService.SaveCompanyAsync()`, the same salt is used for both:

```csharp
salt = encryptionService.GenerateSalt();           // line 152
passwordHash = encryptionService.HashPassword(password, salt);  // line 154
contentStream = await encryptionService.EncryptAsync(..., password, salt, iv);  // line 155
```

The `passwordHash` is stored in the unencrypted JSON footer of the `.argo` file. Since this hash is byte-for-byte identical to the AES-256-GCM encryption key, **anyone who can read the file can decrypt it without knowing the password**. The encryption is completely defeated.

**Remediation:** Use separate salts for password verification and encryption key derivation, or use HKDF-Expand with distinct info labels from a single master key. For example:
- `masterKey = PBKDF2(password, salt, iterations, SHA256, 64)`
- `encryptionKey = masterKey[0..32]`
- `verificationHash = masterKey[32..64]`

---

### HIGH

#### H-1: Plaintext HTTP Used for GeoLocation API
**File:** `ArgoBooks.Core/Services/GeoLocationService.cs:13`

```csharp
private const string IpApiUrl = "http://ip-api.com/json/?fields=status,country,countryCode,region,city,timezone,proxy";
```

The primary geolocation API is called over plaintext HTTP, allowing network-level attackers to observe and tamper with responses. While the IP is hashed before storage (good), the request itself exposes the user's IP address and the fact that they're using Argo Books.

**Remediation:** Use HTTPS for all external API calls. If ip-api.com doesn't support HTTPS on the free tier, move it below the HTTPS fallbacks in priority order, or remove it.

#### H-2: PBKDF2 Iteration Count Below OWASP Recommendation
**File:** `ArgoBooks.Core/Security/KeyDerivation.cs:15`

```csharp
public const int Iterations = 100_000;
```

OWASP recommends at least 600,000 iterations for PBKDF2-SHA256 (2023 guidance). The current 100,000 is 6x below recommendation, reducing brute-force cost significantly. The code comment acknowledges this gap.

**Remediation:** Increase to 600,000 iterations, or migrate to Argon2id which is memory-hard and more resistant to GPU/ASIC attacks. Implement a version migration path for existing files.

---

### MEDIUM

#### M-1: Derived Key Material Not Cleared from Memory
**File:** `ArgoBooks.Core/Services/EncryptionService.cs:54, 85`

The `key` byte array returned by `KeyDerivation.DeriveKey()` is never zeroed after use. Similarly, `plaintext`, `ciphertext`, and `tag` arrays persist in managed memory.

**Remediation:** Call `CryptographicOperations.ZeroMemory()` on sensitive byte arrays in `try/finally` blocks.

#### M-2: Hardcoded Hash Salt for IP Anonymization
**File:** `ArgoBooks.Core/Services/GeoLocationService.cs:17`

```csharp
private const string HashSalt = "ArgoBooks2024GeoHash";
```

The salt for IP address hashing is hardcoded in source code. If this salt is known (e.g., from a leaked build), the truncated hashes (16 hex chars) could be rainbow-tabled against the IPv4 address space.

**Remediation:** Use a per-installation random salt stored in the app data directory, or use a proper HMAC with a key derived from the machine ID.

#### M-3: Machine Key Derivation Uses Weak Static Salt
**File:** `ArgoBooks.Core/Services/LicenseService.cs:327`

```csharp
machineInfo.Append("ArgoBooks_License_v2");
```

The license encryption key is derived from `SHA256(machineId + "ArgoBooks_License_v2")`. This static string provides no meaningful additional entropy. An attacker who knows the machine GUID (readable from the registry) can derive the key.

**Remediation:** This is inherent to machine-bound encryption without user input, but consider using DPAPI on Windows (already available via `System.Security.Cryptography.ProtectedData`) for all platforms where it's supported.

#### M-4: OpenAI API Error Responses Logged with Full Body
**File:** `ArgoBooks.Core/Services/OpenAiService.cs:244`

```csharp
_errorLogger?.LogError($"OpenAI API error {response.StatusCode}: {errorBody}", ...);
```

Full API error response bodies are logged. OpenAI error responses can echo back portions of the request including the API key in certain error conditions.

**Remediation:** Truncate error body logging to status code and a sanitized summary. Never log full response bodies from authenticated APIs.

#### M-5: OpenAI Response Logged on Parse Failure
**File:** `ArgoBooks.Core/Services/OpenAiService.cs:378`

```csharp
_errorLogger?.LogError(ex, ErrorCategory.Parsing, $"Failed to parse OpenAI response: {response}");
```

The full OpenAI response content is included in the error log. If telemetry is enabled, this data (potentially containing business data sent in the prompt) could be uploaded to the telemetry server.

**Remediation:** Log only the exception message, not the full response content.

#### M-6: DotEnv Traverses Entire Directory Tree
**File:** `ArgoBooks.Core/Services/DotEnv.cs:67-93`

`FindEnvFile()` walks up the entire directory tree from `AppDomain.CurrentDomain.BaseDirectory` looking for a `.env` file. An attacker who can place a `.env` file in any parent directory could inject arbitrary environment variables (API keys, endpoints) into the application.

**Remediation:** Limit the search to the application directory and one level up, or require the `.env` path to be explicitly configured.

#### M-7: Password Strings Persist in Managed Memory
**Files:** `ArgoBooks.Core/Services/EncryptionService.cs`, `ArgoBooks.Core/Security/KeyDerivation.cs`

All password-handling methods accept `string` parameters. .NET strings are immutable and can persist in memory indefinitely, potentially being swapped to disk.

**Remediation:** Accept `ReadOnlySpan<char>` where possible. The `Rfc2898DeriveBytes.Pbkdf2` overload accepts `ReadOnlySpan<byte>`, so the password could be UTF-8 encoded into a clearable buffer.

#### M-8: Google Sheets Exports Set World-Writable Permissions
**File:** `ArgoBooks.Core/Services/GoogleSheetsService.cs:206`

```csharp
new Permission { Type = "anyone", Role = "writer", AllowFileDiscovery = false }
```

The multi-dataset Google Sheets export grants `writer` access to `anyone` with the link. This means anyone who obtains the link can modify the exported financial data. The single-dataset export method does not set any sharing permissions, suggesting this was an oversight.

**Remediation:** Change `Role = "writer"` to `Role = "reader"`, or remove automatic sharing entirely and let users share manually from Google Drive.

#### M-9: Company Data Unencrypted in Temp Directory During Session
**File:** `ArgoBooks.Core/Services/FileService.cs:122-127`, `ArgoBooks.Core/Services/CompanyManager.cs`

When a password-protected `.argo` file is opened, it is decrypted and extracted to `%TEMP%/ArgoBooks/<GUID>` as plaintext JSON files. All financial data (customers, invoices, payments, employees) resides unencrypted on disk for the entire session. If the app crashes, temp files remain indefinitely. On SSDs, data may persist after deletion due to wear leveling.

**Remediation:** Add startup cleanup of orphaned temp directories older than a threshold. Document this risk for users handling highly sensitive financial data.

#### M-10: Company Name Used Unsanitized in Directory Path
**File:** `ArgoBooks.Core/Services/FileService.cs:34`

```csharp
var companyDir = Path.Combine(tempDirectory, companyName);
```

User-provided `companyName` is used directly in `Path.Combine` to create a subdirectory. A name containing `../` could create directories outside the intended temp directory. While the risk is limited (within temp), this violates defense-in-depth.

**Remediation:** Sanitize `companyName` to strip path separators and `..` sequences, similar to `ReportTemplateStorage.SanitizeFileName`.

#### M-11: Exception Messages Exposed Directly to Users
**Files:** `ArgoBooks.Core/Services/AzureReceiptScannerService.cs:99,108`, `ArgoBooks.Core/Services/LicenseService.cs:291`, `ArgoBooks.Core/Services/PaymentPortalService.cs:192`

Several services return raw `ex.Message` to users in error responses:
```csharp
$"Azure API error: {ex.Message}"
$"Validation error: {ex.Message}"
$"Error: {ex.Message}"
```

Exception messages can contain internal paths, connection strings, or stack details that aid attackers in reconnaissance.

**Remediation:** Use generic user-facing error messages and log the detailed exception internally only.

#### M-12: No Size Limit on Decompression (Zip Bomb Risk)
**File:** `ArgoBooks.Core/Services/CompressionService.cs:154-167`

`DecompressGZipAsync` reads the entire decompressed stream into a `MemoryStream` with no size limit. A crafted `.argo` file with a small compressed payload that expands to gigabytes could cause `OutOfMemoryException` (denial of service).

**Remediation:** Add a maximum decompression size limit and abort if exceeded. For a bookkeeping app, 500MB decompressed would be generous.

---

### LOW

#### L-1: Telemetry Collects Geolocation Data
**Files:** `ArgoBooks.Core/Services/TelemetryManager.cs`, `ArgoBooks.Core/Services/GeoLocationService.cs`

When telemetry consent is granted, the app collects country, region, city, timezone, and a hashed IP address. While consent-gated, this is more location data than typical for a desktop bookkeeping app.

**Remediation:** Consider collecting only country code and timezone (sufficient for analytics) rather than city-level granularity. Document exactly what is collected in the consent UI.

#### L-2: Biometric Password Storage File Not Encrypted at Rest on Non-Windows
**File:** `ArgoBooks.Core/Platform/WindowsPlatformService.cs:131-152`

On Windows, DPAPI is correctly used to protect stored passwords. However, there's no equivalent protection on macOS or Linux - the `StorePasswordForBiometric` and `GetPasswordForBiometric` methods in the base class may store passwords with weaker or no encryption.

**Remediation:** Use the macOS Keychain on macOS and `libsecret` on Linux for biometric password storage.

#### L-3: Update Download May Bypass Signature Verification
**File:** `ArgoBooks.Desktop/Services/NetSparkleUpdateService.cs:126-201`

`DownloadUpdateAsync()` downloads updates via `HttpClient` directly, potentially bypassing NetSparkle's Ed25519 signature verification pipeline. The appcast check enforces strict Ed25519 verification, but the manual download path does not appear to verify the downloaded file's signature before marking it as ready to install.

**Remediation:** Verify the Ed25519 signature of the downloaded file before marking it `ReadyToInstall`.

#### L-4: Silent Exception Swallowing in Multiple Services
**Files:** `ArgoBooks.Core/Services/FileService.cs:202-203`, `ArgoBooks.Core/Services/LicenseService.cs:147-148`, `ArgoBooks.Core/Platform/WindowsPlatformService.cs:148`

Multiple catch blocks silently swallow all exceptions:
```csharp
catch { /* Best effort cleanup */ }
```

While sometimes appropriate for cleanup code, this pattern makes debugging difficult and could mask security-relevant errors.

**Remediation:** At minimum, log these at debug level so they appear in diagnostic logs.

---

## Dependency Analysis

| Package | Version | Risk Notes |
|---------|---------|------------|
| Avalonia | 11.3.12 | UI framework - keep updated |
| ClosedXML | 0.105.0 | Excel parsing - ensure XML parsing is safe |
| EPPlus | 8.4.2 | Excel - commercial license required for commercial use |
| Azure.AI.FormRecognizer | 4.1.0 | Azure SDK - keep updated |
| Microsoft.Web.WebView2 | 1.0.3800.47 | Renders HTML - ensure content is trusted |
| QuestPDF | 2026.2.1 | PDF generation - keep updated |
| LiveChartsCore | 2.0.0-rc5.4 | Pre-release package - monitor for GA |
| Microsoft.ML | 5.0.0 | ML framework - large dependency surface |

All packages use central version management (`Directory.Packages.props`), which is good practice for consistency.

