# .env Migration Plan — Move Secrets to Backend Proxy

## Problem

The app uses a `.env` file for API keys, but the `.env` is not included in the installer build. Every env-dependent feature silently fails in production. The `.env` cannot be shipped with the app because secrets would be exposed in plaintext on the user's machine.

## Solution

Route all third-party API calls through `argorobots.com` server endpoints. The server holds all secrets. The app authenticates to the server using the **portal API key** (per-company, already obtained during registration).

---

## Current .env Keys and Migration Path

| Key | Current Usage | Migration |
|-----|--------------|-----------|
| `OPENAI_API_KEY` | Direct calls to OpenAI API | **Server proxy** — `argorobots.com/api/ai/completions.php` |
| `OPENAI_MODEL` | Selects GPT model | Send as parameter to server proxy |
| `AZURE_DOCUMENT_INTELLIGENCE_ENDPOINT` | Direct calls to Azure | **Server proxy** — `argorobots.com/api/receipt/scan.php` |
| `AZURE_DOCUMENT_INTELLIGENCE_API_KEY` | Direct calls to Azure | Server holds this key |
| `GOOGLE_CLIENT_ID` | Google OAuth for Sheets/Drive | **Server proxy** — `argorobots.com/api/google/auth.php` |
| `GOOGLE_CLIENT_SECRET` | Google OAuth for Sheets/Drive | Server holds this key |
| `OPENEXCHANGERATES_API_KEY` | Direct calls to OpenExchangeRates | **Server proxy** — `argorobots.com/api/exchange-rates.php` |
| `INVOICE_EMAIL_API_KEY` | Calls to `argorobots.com/api/invoice/send-email.php` | **Already on server** — authenticate with portal key instead of separate email key |
| `PAYMENT_PORTAL_API_KEY` | Per-company portal key | **No change** — this is the auth token for everything |
| `PORTAL_REGISTRATION_KEY` | Master key for company registration | **Server-side only** — registration flow should use a different mechanism (e.g., license key validation) |
| `UPLOAD_API_KEY` / `ARGO_TELEMETRY_API_KEY` | Telemetry upload to `argorobots.com/api/data/upload.php` | **Authenticate with portal key** instead of separate telemetry key |

## After Migration

- The **only key** the app needs is `PAYMENT_PORTAL_API_KEY`, which is already provisioned per-company during registration and stored via `DotEnv.Set()`
- The `.env` file is no longer needed
- All third-party secrets live exclusively on the server

---

## App-Side Changes

### 1. OpenAiService — proxy through server

**File:** `ArgoBooks.Core/Services/OpenAiService.cs`

- Change `ApiEndpoint` from `https://api.openai.com/v1/chat/completions` to `https://argorobots.com/api/ai/completions.php`
- Remove `OPENAI_API_KEY` env var usage
- Authenticate requests with `PAYMENT_PORTAL_API_KEY` in the `Authorization` header
- Send `OPENAI_MODEL` value (or default) as a request parameter instead of reading from env
- `IsConfigured` checks `PortalSettings.IsConfigured` instead of `DotEnv.HasValue(ApiKeyEnvVar)`

### 2. AzureReceiptScannerService — proxy through server

**File:** `ArgoBooks.Core/Services/AzureReceiptScannerService.cs`

- Replace direct Azure SDK calls with HTTP POST to `https://argorobots.com/api/receipt/scan.php`
- Send the receipt image as multipart form data
- Authenticate with portal API key
- Remove `Azure.AI.FormRecognizer` NuGet dependency from the app (move to server)
- Remove `EndpointEnvVar` and `ApiKeyEnvVar` constants
- `IsConfigured` checks `PortalSettings.IsConfigured`

### 3. GoogleCredentialsManager / GoogleSheetsService — proxy through server

**Files:** `ArgoBooks.Core/Services/GoogleCredentialsManager.cs`, `ArgoBooks.Core/Services/GoogleSheetsService.cs`

- Replace Google OAuth + direct Sheets/Drive API calls with server endpoints:
  - `argorobots.com/api/google/auth.php` — initiate OAuth flow (server handles client secret)
  - `argorobots.com/api/google/sheets/export.php` — create/update spreadsheet
- The server performs the Google API calls with its own credentials
- Remove `GOOGLE_CLIENT_ID` and `GOOGLE_CLIENT_SECRET` env vars
- Consider: Google OAuth can also work with just a public client ID + PKCE (no secret needed), which would let the app keep direct Google API access without needing a proxy

### 4. ExchangeRateService — proxy through server

**File:** `ArgoBooks.Core/Services/ExchangeRateService.cs`

- Change API endpoint from `https://openexchangerates.org/api` to `https://argorobots.com/api/exchange-rates.php`
- Remove `OPENEXCHANGERATES_API_KEY` env var
- Authenticate with portal API key
- Server fetches from OpenExchangeRates with its own key and returns the rates

### 5. InvoiceEmailService — use portal key for auth

**File:** `ArgoBooks.Core/Services/InvoiceTemplates/InvoiceEmailService.cs`

- Already calls `argorobots.com/api/invoice/send-email.php`
- Replace `InvoiceEmailSettings.ApiKey` (from env) with `PortalSettings.ApiKey` in the `Authorization` header
- Remove `INVOICE_EMAIL_API_KEY` env var
- `IsConfigured` checks `PortalSettings.IsConfigured`

### 6. TelemetryUploadService — use portal key for auth

**File:** `ArgoBooks.Core/Services/TelemetryUploadService.cs`

- Already calls `argorobots.com/api/data/upload.php`
- Replace `GetApiKey()` method (reads `UPLOAD_API_KEY` / `ARGO_TELEMETRY_API_KEY`) with `PortalSettings.ApiKey`
- Remove env var usage

### 7. Portal Registration — rethink flow

**File:** `ArgoBooks/ViewModels/SettingsModalViewModel.cs`

- Currently reads `PORTAL_REGISTRATION_KEY` from env to register a new company
- This key won't exist without a `.env` file
- New flow: registration should validate through the **license key** (which the user already has) instead of a separate registration key
- Server endpoint accepts the license key and returns a portal API key

### 8. DotEnv cleanup

**File:** `ArgoBooks.Core/Services/DotEnv.cs`

- Keep `DotEnv.Get()` and `DotEnv.Set()` for the portal API key (which is written at runtime during registration)
- Consider storing the portal API key in `GlobalSettings` or platform credential storage (DPAPI/Keychain) instead of a `.env` file
- Once all other env vars are removed, evaluate whether `DotEnv` is still needed

### 9. Remove env var constants and IsConfigured checks

**Files:** `InvoiceEmailSettings.cs`, `PortalSettings.cs`, `AzureReceiptScannerService.cs`, `OpenAiService.cs`

- Remove `ApiKeyEnvVar`, `EndpointEnvVar`, `ModelEnvVar` constants
- Remove `RegistrationKeyEnvVar` from `PortalSettings`
- Update all `IsConfigured` properties to check `PortalSettings.IsConfigured`

---

## Server-Side Changes (argorobots.com)

New PHP endpoints needed:

| Endpoint | Purpose | Receives | Returns |
|----------|---------|----------|---------|
| `/api/ai/completions.php` | Proxy OpenAI calls | prompt, model preference | OpenAI response |
| `/api/receipt/scan.php` | Proxy Azure Doc Intelligence | receipt image | parsed receipt data |
| `/api/google/auth.php` | Handle Google OAuth | auth code | access token |
| `/api/google/sheets/export.php` | Proxy Google Sheets export | spreadsheet data | sheet URL |
| `/api/exchange-rates.php` | Proxy exchange rates | date, currencies | rate data |

All endpoints:
- Validate the portal API key from the `Authorization` header
- Forward the request to the third-party API using server-side secrets
- Return the response to the app
- Can add rate limiting, usage tracking, and cost controls per company

---

## Implementation Order

1. **Server endpoints first** — build and deploy the proxy endpoints
2. **Invoice email + telemetry** — easiest wins, already call argorobots.com, just swap auth
3. **Exchange rates** — simple proxy, low risk
4. **OpenAI** — proxy with model parameter
5. **Azure receipt scanning** — remove SDK dependency, send images to proxy
6. **Google Sheets** — most complex due to OAuth flow
7. **Portal registration** — rework to use license key
8. **DotEnv cleanup** — remove unused env vars, consider removing DotEnv class entirely
