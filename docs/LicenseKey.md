# License Key

Argo Books uses a license key system to manage access to premium features. License keys are validated both locally and online, stored with machine-specific encryption, and tied to a specific device.

## Key Format

License keys follow the format `XXXX-XXXX-XXXX-XXXX-XXXX` (20 alphanumeric characters separated by 4 dashes, 24 characters total). Keys are case-insensitive and normalized to uppercase before processing.

## License Validation Flow

![License Validation Flow](diagrams/license-key/license-validation-flow.svg)

### Activation (Redemption)

When a user enters a license key in the upgrade modal:

1. **Format validation** - the key is checked for the correct `XXXX-XXXX-XXXX-XXXX-XXXX` format (24 characters including dashes)
2. **Server redemption** - the key and a hashed device ID are sent to the redemption API (`/api/license/redeem.php`)
3. **Device binding** - the server marks the key as used and binds it to the device
4. **Local storage** - on success, the license data (premium status, key, activation date) is encrypted and saved locally
5. **Feature unlock** - premium features become available immediately

### Online Validation

Stored licenses can be re-validated against the server to check ongoing subscription status:

1. The stored license key and device ID are sent to the validation API (`/api/license/validate.php`)
2. The server returns one of the following statuses:
   - `Valid` - license is active and matches the device
   - `InvalidKey` - the key is not recognized
   - `ExpiredSubscription` - the subscription has lapsed
   - `WrongDevice` - the key is bound to a different device
   - `NetworkError` - the server could not be reached

## Local Storage and Encryption

License data is stored locally in global settings, encrypted with a machine-specific key so it cannot be transferred between devices.

### What Gets Stored

| Field                | Description                                      |
|----------------------|--------------------------------------------------|
| `LicenseData`        | Encrypted JSON containing premium status, license key, and activation date |
| `Salt`               | Salt used for the encryption key derivation       |
| `Iv`                 | Initialization vector for AES-GCM encryption      |
| `LastValidationDate` | Timestamp of the most recent online validation     |

### Machine-Specific Encryption

The license data is encrypted using AES-256-GCM with a key derived from:

1. **Machine ID** - a stable, platform-specific hardware identifier obtained via `IPlatformService.GetMachineId()`
2. **Application salt** - a static string (`ArgoBooks_License_v2`) appended to the machine ID
3. **SHA-256 hash** - the combined value is hashed to produce a fixed-length encryption key

This ensures license data is bound to the machine it was activated on and cannot be copied to another device.

### Encryption Process

1. License data (`HasPremium`, `LicenseKey`, `ActivationDate`) is serialized to JSON
2. A random salt and IV are generated
3. The JSON is encrypted using AES-256-GCM with the machine-derived key
4. The encrypted data, salt, and IV are stored in global settings

### Decryption Process

1. The encrypted data, salt, and IV are loaded from global settings
2. The machine key is re-derived from the current machine ID
3. AES-256-GCM decryption is performed
4. The JSON is deserialized back to license data

If decryption fails (e.g., because the machine ID has changed), the license is treated as absent and the user is returned to free-tier status.

## Device Identification

Each device generates a hashed identifier used for:

- **License binding** - preventing a single key from being used on multiple devices
- **Server-side tracking** - the server records which device redeemed each key

The device ID is computed the same way as the machine encryption key (SHA-256 of machine ID + application salt), so it is consistent across app restarts but unique per device.

## Premium Plans and Pricing

The upgrade modal fetches current pricing from the pricing API (`/api/pricing/plans.php`).

Users can:

- **Purchase a subscription** via the web portal at `argorobots.com/upgrade/premium/`
- **Enter a license key** received after purchase to activate premium features
- **Cancel a subscription** through the community portal at `argorobots.com/community/users/subscription.php`

## Receipt Scan Usage Tracking

Premium license keys also govern receipt scanning limits. The `ReceiptUsageService` tracks usage per license key:

- **Check usage** - queries the server (`/api/receipt/usage.php`) for current scan count, monthly limit, remaining scans, and tier
- **Increment usage** - reports each successful scan to the server
- **Caching** - usage data is cached locally for 5 minutes to reduce API calls
- **Offline fallback** - if the server is unreachable, cached data is used to allow scanning; increments are accepted optimistically

### Usage Data

| Field          | Description                                  |
|----------------|----------------------------------------------|
| `ScanCount`    | Number of scans used this month              |
| `MonthlyLimit` | Maximum scans allowed for the current tier   |
| `Remaining`    | Scans remaining this month                   |
| `Tier`         | Subscription tier name                       |
| `ResetsAt`     | Date when the usage counter resets (first of next month) |

## Error Handling and Connectivity

All license and usage API calls include layered connectivity diagnostics:

1. **Internet check** - verifies general internet access
2. **Host check** - verifies that `argorobots.com` is reachable
3. **Specific error messages** - the user sees a targeted message depending on where connectivity fails:
   - No internet connection
   - Argo Books servers unreachable
   - Generic validation/verification failure

API requests have a 30-second timeout for license operations and a 15-second timeout for usage tracking.

## Key Services

| Service                | File                                              | Responsibility                                      |
|------------------------|---------------------------------------------------|-----------------------------------------------------|
| `LicenseService`       | `ArgoBooks.Core/Services/LicenseService.cs`       | License storage, encryption, loading, online validation, device ID |
| `ReceiptUsageService`  | `ArgoBooks.Core/Services/ReceiptUsageService.cs`  | Receipt scan usage tracking and limit enforcement    |
| `UpgradeModalViewModel`| `ArgoBooks/ViewModels/UpgradeModalViewModel.cs`   | UI logic for upgrade modal, key entry, redemption, pricing |

## Activating on a New Device

A license key can be activated on a new device at any time by entering it in the upgrade modal. The server rebinds the key to the new device immediately - no manual deactivation on the old device is required.

The next time the old device opens the app, the startup license validation detects that the key is now bound to a different device (`WrongDevice` status). The app automatically clears the local license data and shows a message: *"Your license key has been activated on a different device."*

## Clearing a License

You can manually delete the global settings file to remove all stored license data from the device:

| Platform | Settings file path |
|----------|-------------------|
| **Windows** | `%APPDATA%\ArgoBooks\settings.json` |
| **macOS** | `~/Library/Application Support/ArgoBooks/settings.json` |
| **Linux** | `$XDG_CONFIG_HOME/ArgoBooks/settings.json` (or `~/.config/ArgoBooks/settings.json`) |

Deleting `settings.json` removes all application settings, not just the license data. Alternatively, you could remove only the license key data from the json.
