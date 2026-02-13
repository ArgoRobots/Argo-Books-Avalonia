# Portal Changes Needed for Argo-Books-website

Branch: `claude/implement-payment-portal-Wm5ns`

These changes fix the "Open Portal" button in the Argo Books desktop app, which currently navigates to `https://argorobots.com/portal/` and gets a 404.

---

## 1. `portal/index.php` — Show landing page when no token provided

**Problem:** The portal page requires a 48-character hex token (`/portal/{token}`). When accessed without a token (just `/portal/`), it returns a 404 error page. The desktop app's "Open Portal" button navigates to `/portal/` which hits this 404.

**Fix:** Replace the early 404 exit (lines 17-22) with a branded landing page that tells visitors to use the link from their invoice email.

**Current code (lines 15-22):**
```php
$token = $_GET['token'] ?? '';

// Validate token format
if (empty($token) || !preg_match('/^[a-fA-F0-9]{48}$/', $token)) {
    http_response_code(404);
    include __DIR__ . '/../error-pages/404.html';
    exit;
}
```

**Replace with:**
```php
$token = $_GET['token'] ?? '';

// If no token provided, show a generic portal landing page
if (empty($token) || !preg_match('/^[a-fA-F0-9]{48}$/', $token)) {
    ?>
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <meta name="robots" content="noindex, nofollow">
    <title>Payment Portal - Argo Books</title>
    <link rel="shortcut icon" type="image/x-icon" href="/resources/images/argo-logo/A-logo.ico">
    <link rel="stylesheet" href="/resources/styles/custom-colors.css">
    <link rel="stylesheet" href="/portal/style.css">
</head>
<body>
    <div class="portal-page">
        <header class="portal-header">
            <div class="portal-header-inner">
                <div class="company-info">
                    <h1 class="company-name">Payment Portal</h1>
                    <span class="portal-subtitle">Powered by Argo Books</span>
                </div>
            </div>
        </header>
        <main class="portal-main">
            <div class="portal-welcome" style="text-align: center; padding: 48px 20px;">
                <h2>Welcome to the Payment Portal</h2>
                <p style="margin-top: 12px; color: var(--gray-600); font-size: 15px; max-width: 480px; margin-left: auto; margin-right: auto;">
                    To view and pay your invoices, please use the link provided in your invoice email.
                </p>
                <a href="https://argorobots.com" style="display: inline-block; margin-top: 24px; color: var(--primary-blue); text-decoration: none; font-weight: 500;">
                    &larr; Back to Argo Books
                </a>
            </div>
        </main>
        <footer class="portal-footer">
            <p>Secure payments powered by <a href="https://argorobots.com" target="_blank" rel="noopener">Argo Books</a></p>
        </footer>
    </div>
</body>
</html>
    <?php
    exit;
}
```

---

## 2. `api/portal/status.php` — Return `portalUrl` in response

**Problem:** The status endpoint doesn't return a `portalUrl` field. The desktop app checks `status.PortalUrl` after calling this endpoint (in `PaymentsPageViewModel.CheckPortalStatusAsync`), but it's always null because the server never sends it.

**Fix:** Add `'portalUrl' => 'https://argorobots.com/portal/'` to the JSON response.

**Current code (around line 70-89):**
```php
send_json_response(200, [
    'success' => true,
    'connected' => true,
    'company' => [
        ...
    ],
    ...
    'timestamp' => date('c')
]);
```

**Add the `portalUrl` field after `'connected' => true,`:**
```php
send_json_response(200, [
    'success' => true,
    'connected' => true,
    'portalUrl' => 'https://argorobots.com/portal/',
    'company' => [
        ...
    ],
    ...
    'timestamp' => date('c')
]);
```

---

## Summary

| File | Change | Why |
|------|--------|-----|
| `portal/index.php` | Show landing page instead of 404 when no token | So `/portal/` works for the desktop "Open Portal" button |
| `api/portal/status.php` | Add `portalUrl` to JSON response | So the desktop app receives and stores the portal URL |
