# License API Specification for Argo Books Website

This document defines the server-side API contract for the Argo Books license key system (v2.1+). The desktop app sends these requests. The website repo needs to implement the endpoints and database changes described below.

**NOT backwards compatible** with v2.0.0 or v2.0.1.

---

## Overview

The new license system:
- Does **NOT** require a user account (no `user_id` or `email`)
- Uses a `device_id` (hashed machine identifier) to track which device a key is active on
- Supports single-device activation: redeeming a key on a new device overwrites the previous device
- Validates subscription status and device ownership on every app launch

---

## Database Changes

### `premium_subscription_keys` table

Add a new column:

```sql
ALTER TABLE premium_subscription_keys
ADD COLUMN device_id VARCHAR(255) DEFAULT NULL AFTER redeemed_by_user_id;
```

The `device_id` stores the hashed machine identifier of the device that last redeemed the key. When a key is redeemed from a new device, this value is updated (deactivating the old device).

**Note:** The `redeemed_by_user_id` and `email` columns can remain for backwards compatibility with admin tools, but they are no longer required by the app. The app will no longer send `user_id` or `email` during redemption.

---

## Endpoints

### `POST /api/license/redeem.php`

Redeems (activates) a license key and binds it to a device.

#### Request

```json
{
  "premium_key": "PREM-XXXX-XXXX-XXXX-XXXX",
  "device_id": "base64-encoded-hashed-device-id"
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `premium_key` | string | Yes | The license key in XXXX-XXXX-XXXX-XXXX-XXXX format |
| `device_id` | string | Yes | Hashed machine identifier (SHA-256, base64-encoded, ~44 chars) |

#### Logic

1. Validate the key exists in `premium_subscription_keys`
2. If the key has **never been redeemed**:
   - Create a new `premium_subscriptions` record (same as current logic)
   - Mark the key as redeemed: set `redeemed_at = NOW()`, `device_id = <device_id>`
   - Return success with subscription details
3. If the key has **already been redeemed**:
   - Check if the subscription is still active (not expired/cancelled)
   - If active: update `device_id` to the new value (this deactivates the old device)
   - If expired: return error
   - Return success with subscription details
4. If the key doesn't exist: return error

#### Success Response

```json
{
  "success": true,
  "type": "premium",
  "status": "active",
  "message": "License activated successfully!",
  "subscription_id": "SUB-XXXXX",
  "end_date": "2027-02-22T00:00:00Z",
  "duration_months": 12
}
```

#### Error Responses

Key not found:
```json
{
  "success": false,
  "status": "invalid_key",
  "message": "Invalid license key."
}
```

Subscription expired:
```json
{
  "success": false,
  "status": "expired",
  "message": "This license key's subscription has expired."
}
```

Missing device_id:
```json
{
  "success": false,
  "status": "error",
  "message": "Device ID is required."
}
```

---

### `POST /api/license/validate.php`

Validates a license key's subscription status and device ownership. Called on every app launch.

#### Request

```json
{
  "license_key": "PREM-XXXX-XXXX-XXXX-XXXX",
  "device_id": "base64-encoded-hashed-device-id"
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `license_key` | string | Yes | The license key to validate |
| `device_id` | string | Yes | This device's hashed machine identifier |

#### Logic

1. Look up the key in `premium_subscription_keys`
2. If the key doesn't exist or was never redeemed: return `invalid_key`
3. If the key's `device_id` doesn't match the request's `device_id`: return `wrong_device`
4. Look up the associated subscription in `premium_subscriptions`
5. If the subscription `status` is not `active` or `end_date < NOW()`: return `expired`
6. Otherwise: return `valid`

#### Success Response (valid)

```json
{
  "success": true,
  "status": "valid",
  "message": "License is valid."
}
```

#### Error Responses

Invalid or unredeemed key:
```json
{
  "success": false,
  "status": "invalid_key",
  "message": "License key is not valid."
}
```

Wrong device (key is active on a different device):
```json
{
  "success": false,
  "status": "wrong_device",
  "message": "This license key is active on a different device."
}
```

Subscription expired:
```json
{
  "success": false,
  "status": "expired",
  "message": "Your premium subscription has expired."
}
```

---

## Implementation Notes

### Changes to `license_functions.php`

#### `redeem_premium_key($key, $device_id)`

Replace the current `redeem_premium_key($key, $user_id, $email)` function:

- Remove `$user_id` and `$email` parameters
- Add `$device_id` parameter
- When the key is already redeemed AND subscription is still active, UPDATE the `device_id` instead of returning "already redeemed" error
- When creating the subscription record, `user_id` can be set to `NULL` or `0`
- Store `device_id` in `premium_subscription_keys` when marking as redeemed

#### New function: `validate_license($key, $device_id)`

```php
function validate_license($key, $device_id) {
    // 1. Look up key in premium_subscription_keys
    // 2. Check if redeemed (redeemed_at IS NOT NULL)
    // 3. Check device_id matches
    // 4. Look up subscription via subscription_id
    // 5. Check subscription status and end_date
    // Return: { success, status, message }
}
```

### Changes to `api/license/redeem.php`

Update to read `device_id` from the request body instead of `user_id` and `email`:

```php
$data = json_decode(file_get_contents('php://input'), true);
$premium_key = $data['premium_key'] ?? '';
$device_id = $data['device_id'] ?? '';

if (empty($device_id)) {
    echo json_encode(['success' => false, 'status' => 'error', 'message' => 'Device ID is required.']);
    exit;
}

$result = redeem_premium_key($premium_key, $device_id);
```

### New file: `api/license/validate.php`

```php
<?php
require_once '../../license_functions.php';

header('Content-Type: application/json');

$data = json_decode(file_get_contents('php://input'), true);
$license_key = $data['license_key'] ?? '';
$device_id = $data['device_id'] ?? '';

if (empty($license_key) || empty($device_id)) {
    echo json_encode(['success' => false, 'status' => 'error', 'message' => 'License key and device ID are required.']);
    exit;
}

$result = validate_license($license_key, $device_id);
echo json_encode($result);
```

---

## Device ID Format

The `device_id` sent by the app is a SHA-256 hash (base64-encoded) of the machine's unique identifier combined with a static application salt. Example value:

```
dGhpcyBpcyBhIGJhc2U2NCBlbmNvZGVkIGhhc2g=
```

It is approximately 44 characters long and is stable across app restarts on the same machine. Different machines will produce different device IDs.

---

## Migration Path

Since this is NOT backwards compatible:
- Old license keys stored locally (from v2.0.0/v2.0.1) will fail to validate via the new endpoint and the user will be prompted to re-enter their key
- The `device_id` column defaults to `NULL`, so existing redeemed keys will need to be re-redeemed from the app to set their device_id
