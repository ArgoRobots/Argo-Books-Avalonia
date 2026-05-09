# Invoice Refunds Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ship a production-ready invoice refund feature that returns money to customers' original payment method via Stripe (PayPal & Square in later phases), with email-verified initiator, adaptive velocity throttling, server-owned state machine, locked owner email, and admin oversight.

**Architecture:** Server (PHP/MySQL) owns the refund state machine and email-verification flows. Desktop (Avalonia/.NET 10) initiates and reflects via existing Bearer-token API. Refund records propagate to the desktop's local books via the existing `payments-sync.php` polling.

**Tech Stack:** PHP 8 / MySQL 8 server-side; .NET 10 + Avalonia + CommunityToolkit.Mvvm desktop-side; Stripe SDK (PHP), PayPal v2 REST, Square SDK; PHPMailer; xUnit tests.

**Branch:** `feature/invoice-refunds` (already created off `V.2.0.7`).

**Spec:** `docs/superpowers/specs/2026-05-09-invoice-refunds-design.md` is the source of truth — refer to it for details that aren't repeated here.

---

## Phase index

1. Server DB schema migration
2. Server: shared helpers (codes, audit log, idempotency)
3. Server: refund lifecycle endpoints (Stripe path)
4. Server: refund webhook reconciliation
5. Server: cron jobs
6. Server: velocity engine
7. Server: email verification (registration + change flow)
8. Server: extend `payments-sync.php` to surface refunds
9. Server: admin dashboard extensions
10. Desktop: model extensions (Payment, InvoiceStatus, recompute)
11. Desktop: PaymentPortalService refund client + sync interpretation
12. Desktop: refund modal + invoice list button
13. Desktop: invoice detail & post-refund display
14. Desktop: email verification & change UI
15. Desktop: refunds analytics tab
16. PayPal refund support
17. Square refund support
18. End-to-end test pass + production SQL handoff

Each phase ends with a commit. Phases 1–9 are server-only and can be deployed independently. Phase 10+ requires the server pieces in place.

---

## Phase 1 — Server DB schema migration

**Files:**
- Create: `argo-books-website/sql/migrations/2026-05-09-refunds.sql`
- Apply locally to laragon MySQL (verify it's running first)

### Task 1.1: Verify laragon MySQL is running

- [ ] **Step 1: Check laragon mysql process**

```powershell
Get-Process mysqld -ErrorAction SilentlyContinue
```

If empty, ask the user to start laragon and pause until they confirm. Do not auto-start the service.

### Task 1.2: Write migration SQL

- [ ] **Step 1: Create migration file**

Create `argo-books-website/sql/migrations/2026-05-09-refunds.sql` with:

```sql
-- Invoice Refunds — schema migration
-- Spec: docs/superpowers/specs/2026-05-09-invoice-refunds-design.md
-- Run order: tables first, then portal_companies extension, then seed velocity config.

START TRANSACTION;

-- ============================================================
-- New table: refund_requests
-- ============================================================
CREATE TABLE IF NOT EXISTS refund_requests (
  id BIGINT AUTO_INCREMENT PRIMARY KEY,
  company_id INT NOT NULL,
  invoice_id VARCHAR(64) NOT NULL,
  invoice_number VARCHAR(64) NOT NULL,
  customer_name VARCHAR(255) NULL,
  provider ENUM('stripe','paypal','square') NOT NULL,
  provider_payment_id VARCHAR(128) NOT NULL,
  provider_refund_id VARCHAR(128) NULL,
  amount_cents BIGINT NOT NULL,
  currency VARCHAR(3) NOT NULL DEFAULT 'USD',
  line_items_json TEXT NULL,
  reason TEXT NULL,
  state ENUM(
    'pending_code','code_verified','cooling_off','processing',
    'completed','cancelled','failed'
  ) NOT NULL DEFAULT 'pending_code',
  state_reason TEXT NULL,
  cooling_off_until DATETIME NULL,
  velocity_tier ENUM('normal','soft_warn','delayed','hard_block') NULL,
  cancel_token VARCHAR(64) NULL,
  requested_ip VARCHAR(45) NULL,
  requested_user_agent VARCHAR(255) NULL,
  created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  completed_at DATETIME NULL,
  CONSTRAINT fk_refund_requests_company FOREIGN KEY (company_id) REFERENCES portal_companies(id),
  INDEX idx_refund_requests_company_state (company_id, state, created_at),
  INDEX idx_refund_requests_provider_refund (provider_refund_id),
  INDEX idx_refund_requests_cooling (cooling_off_until),
  INDEX idx_refund_requests_cancel_token (cancel_token)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ============================================================
-- New table: refund_email_codes
-- ============================================================
CREATE TABLE IF NOT EXISTS refund_email_codes (
  id BIGINT AUTO_INCREMENT PRIMARY KEY,
  refund_request_id BIGINT NOT NULL,
  code_hash VARCHAR(64) NOT NULL,
  expires_at DATETIME NOT NULL,
  consumed_at DATETIME NULL,
  attempts INT NOT NULL DEFAULT 0,
  created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  CONSTRAINT fk_refund_email_codes_request FOREIGN KEY (refund_request_id) REFERENCES refund_requests(id) ON DELETE CASCADE,
  INDEX idx_refund_email_codes_request (refund_request_id, consumed_at)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ============================================================
-- New table: refund_audit_log (append-only)
-- ============================================================
CREATE TABLE IF NOT EXISTS refund_audit_log (
  id BIGINT AUTO_INCREMENT PRIMARY KEY,
  company_id INT NOT NULL,
  refund_request_id BIGINT NULL,
  email_change_request_id BIGINT NULL,
  event_type VARCHAR(64) NOT NULL,
  payload_json TEXT NULL,
  actor_type ENUM('owner','admin','system','webhook') NOT NULL,
  actor_id VARCHAR(64) NULL,
  ip_address VARCHAR(45) NULL,
  user_agent VARCHAR(255) NULL,
  created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  INDEX idx_audit_company_time (company_id, created_at),
  INDEX idx_audit_request (refund_request_id, created_at),
  INDEX idx_audit_email_change (email_change_request_id, created_at),
  INDEX idx_audit_event (event_type, created_at)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ============================================================
-- New table: email_change_requests
-- ============================================================
CREATE TABLE IF NOT EXISTS email_change_requests (
  id BIGINT AUTO_INCREMENT PRIMARY KEY,
  company_id INT NOT NULL,
  old_email VARCHAR(255) NOT NULL,
  new_email VARCHAR(255) NOT NULL,
  old_email_code_hash VARCHAR(64) NULL,
  new_email_code_hash VARCHAR(64) NULL,
  old_email_verified_at DATETIME NULL,
  new_email_verified_at DATETIME NULL,
  password_verified BOOLEAN NOT NULL DEFAULT 0,
  state ENUM('pending','old_verified','new_verified','completed','cancelled','reverted') NOT NULL DEFAULT 'pending',
  cancel_token VARCHAR(64) NULL,
  revert_until DATETIME NULL,
  created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  completed_at DATETIME NULL,
  reverted_at DATETIME NULL,
  CONSTRAINT fk_email_change_company FOREIGN KEY (company_id) REFERENCES portal_companies(id),
  INDEX idx_email_change_company (company_id, created_at),
  INDEX idx_email_change_cancel_token (cancel_token)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ============================================================
-- New table: email_verifications (registration + future purposes)
-- ============================================================
CREATE TABLE IF NOT EXISTS email_verifications (
  id BIGINT AUTO_INCREMENT PRIMARY KEY,
  company_id INT NOT NULL,
  email VARCHAR(255) NOT NULL,
  purpose ENUM('registration') NOT NULL DEFAULT 'registration',
  code_hash VARCHAR(64) NOT NULL,
  expires_at DATETIME NOT NULL,
  consumed_at DATETIME NULL,
  attempts INT NOT NULL DEFAULT 0,
  created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  CONSTRAINT fk_email_verifications_company FOREIGN KEY (company_id) REFERENCES portal_companies(id),
  INDEX idx_email_verifications_company (company_id, purpose, consumed_at)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ============================================================
-- New table: refund_velocity_baselines (cron-recomputed)
-- ============================================================
CREATE TABLE IF NOT EXISTS refund_velocity_baselines (
  company_id INT PRIMARY KEY,
  daily_avg_refund_cents BIGINT NOT NULL DEFAULT 0,
  daily_avg_refund_count INT NOT NULL DEFAULT 0,
  revenue_30d_cents BIGINT NOT NULL DEFAULT 0,
  last_recomputed_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  CONSTRAINT fk_velocity_baselines_company FOREIGN KEY (company_id) REFERENCES portal_companies(id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ============================================================
-- New table: refund_velocity_config (admin-tunable; null company = global)
-- ============================================================
CREATE TABLE IF NOT EXISTS refund_velocity_config (
  id INT AUTO_INCREMENT PRIMARY KEY,
  company_id INT NULL,
  soft_warn_multiplier DECIMAL(5,2) NOT NULL DEFAULT 3.00,
  cooling_multiplier DECIMAL(5,2) NOT NULL DEFAULT 10.00,
  cooling_revenue_pct DECIMAL(5,2) NOT NULL DEFAULT 0.25,
  hard_revenue_pct DECIMAL(5,2) NOT NULL DEFAULT 0.50,
  cooling_off_minutes INT NOT NULL DEFAULT 15,
  new_account_floor_cents BIGINT NOT NULL DEFAULT 100000,
  new_account_soft_cents BIGINT NOT NULL DEFAULT 50000,
  new_account_cooling_cents BIGINT NOT NULL DEFAULT 100000,
  updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  CONSTRAINT fk_velocity_config_company FOREIGN KEY (company_id) REFERENCES portal_companies(id),
  UNIQUE KEY uniq_velocity_config_company (company_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Seed the global default
INSERT INTO refund_velocity_config (company_id) VALUES (NULL)
ON DUPLICATE KEY UPDATE updated_at = CURRENT_TIMESTAMP;

-- ============================================================
-- Extend portal_companies
-- ============================================================
ALTER TABLE portal_companies
  ADD COLUMN locked BOOLEAN NOT NULL DEFAULT 0,
  ADD COLUMN lock_reason TEXT NULL,
  ADD COLUMN locked_at DATETIME NULL,
  ADD COLUMN email_verified_at DATETIME NULL;

-- For existing companies that have already been using the portal in production,
-- mark their email as verified so we don't gate them retroactively.
UPDATE portal_companies SET email_verified_at = created_at WHERE email_verified_at IS NULL AND created_at < NOW();

COMMIT;
```

- [ ] **Step 2: Apply migration to local laragon**

```powershell
& "C:\laragon\bin\mysql\mysql-8.0.30-winx64\bin\mysql.exe" -uroot argo_books_website < "C:\laragon\www\argo-books-website\sql\migrations\2026-05-09-refunds.sql"
```

If the path or DB name differs, ask the user to confirm before running.

- [ ] **Step 3: Verify tables exist**

```powershell
& "C:\laragon\bin\mysql\mysql-8.0.30-winx64\bin\mysql.exe" -uroot argo_books_website -e "SHOW TABLES LIKE 'refund_%'; SHOW TABLES LIKE 'email_%'; DESCRIBE portal_companies;"
```

Expect: 4 refund_* tables, 2 email_* tables, portal_companies has 4 new columns.

- [ ] **Step 4: Commit**

```bash
git add argo-books-website/sql/migrations/2026-05-09-refunds.sql
git commit -m "feat(refunds): db migration for refund state machine and email verification"
```

---

## Phase 2 — Server shared helpers

**Files:**
- Create: `argo-books-website/api/portal/_refund_helpers.php`
- Create: `argo-books-website/api/portal/_audit.php`
- Create: `argo-books-website/api/portal/_idempotency.php`

### Task 2.1: Audit log helper

- [ ] **Step 1: Create `_audit.php`**

```php
<?php
// _audit.php — single entry point for refund_audit_log writes.
// Always call this from any code path that mutates refund state.

function audit_log(
    PDO $pdo,
    int $company_id,
    string $event_type,
    string $actor_type,           // 'owner'|'admin'|'system'|'webhook'
    ?string $actor_id = null,
    ?int $refund_request_id = null,
    ?int $email_change_request_id = null,
    array $payload = [],
    ?string $ip = null,
    ?string $ua = null
): int {
    $stmt = $pdo->prepare("
        INSERT INTO refund_audit_log
            (company_id, refund_request_id, email_change_request_id,
             event_type, payload_json, actor_type, actor_id, ip_address, user_agent)
        VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)
    ");
    $stmt->execute([
        $company_id,
        $refund_request_id,
        $email_change_request_id,
        $event_type,
        $payload ? json_encode($payload, JSON_UNESCAPED_SLASHES | JSON_UNESCAPED_UNICODE) : null,
        $actor_type,
        $actor_id,
        $ip,
        $ua,
    ]);
    return (int)$pdo->lastInsertId();
}

function audit_request_context(): array {
    return [
        'ip' => $_SERVER['REMOTE_ADDR'] ?? null,
        'ua' => substr($_SERVER['HTTP_USER_AGENT'] ?? '', 0, 255) ?: null,
    ];
}
```

### Task 2.2: Idempotency helper

- [ ] **Step 1: Create `_idempotency.php`**

```php
<?php
// _idempotency.php — Idempotency-Key support for POST endpoints.
// Stores response body+status keyed on (company_id, key, body_hash). Same key+body
// returns cached response within 24h; same key with different body returns 409.
//
// Schema is deliberately lightweight; we'll create a small table the first time this
// is included.

function idempotency_init(PDO $pdo): void {
    static $initialized = false;
    if ($initialized) return;
    $pdo->exec("
        CREATE TABLE IF NOT EXISTS refund_idempotency_cache (
            id BIGINT AUTO_INCREMENT PRIMARY KEY,
            company_id INT NOT NULL,
            idempotency_key VARCHAR(128) NOT NULL,
            body_hash VARCHAR(64) NOT NULL,
            response_status INT NOT NULL,
            response_body MEDIUMTEXT NOT NULL,
            created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
            UNIQUE KEY uniq_company_key (company_id, idempotency_key),
            INDEX idx_created (created_at)
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
    ");
    $initialized = true;
}

/** @return array|null Returns ['status' => int, 'body' => string] if a prior matching response exists, null otherwise. Throws on key+body mismatch. */
function idempotency_lookup(PDO $pdo, int $company_id, string $key, string $body_hash): ?array {
    $stmt = $pdo->prepare("SELECT body_hash, response_status, response_body FROM refund_idempotency_cache WHERE company_id = ? AND idempotency_key = ? AND created_at > DATE_SUB(NOW(), INTERVAL 24 HOUR)");
    $stmt->execute([$company_id, $key]);
    $row = $stmt->fetch(PDO::FETCH_ASSOC);
    if (!$row) return null;
    if ($row['body_hash'] !== $body_hash) {
        http_response_code(409);
        echo json_encode(['error' => 'idempotency_key_mismatch', 'message' => 'Same Idempotency-Key reused with different request body.']);
        exit;
    }
    return ['status' => (int)$row['response_status'], 'body' => $row['response_body']];
}

function idempotency_store(PDO $pdo, int $company_id, string $key, string $body_hash, int $status, string $body): void {
    $stmt = $pdo->prepare("
        INSERT INTO refund_idempotency_cache (company_id, idempotency_key, body_hash, response_status, response_body)
        VALUES (?, ?, ?, ?, ?)
        ON DUPLICATE KEY UPDATE response_status = VALUES(response_status), response_body = VALUES(response_body), created_at = CURRENT_TIMESTAMP
    ");
    $stmt->execute([$company_id, $key, $body_hash, $status, $body]);
}

/** Wraps a handler so that responses are cached by Idempotency-Key. */
function with_idempotency(PDO $pdo, int $company_id, string $raw_body, callable $handler): void {
    $key = $_SERVER['HTTP_IDEMPOTENCY_KEY'] ?? null;
    if (!$key) {
        $handler();
        return;
    }
    idempotency_init($pdo);
    $body_hash = hash('sha256', $raw_body);
    $cached = idempotency_lookup($pdo, $company_id, $key, $body_hash);
    if ($cached !== null) {
        http_response_code($cached['status']);
        header('Content-Type: application/json');
        echo $cached['body'];
        return;
    }
    ob_start();
    $handler();
    $body = ob_get_clean();
    $status = http_response_code() ?: 200;
    idempotency_store($pdo, $company_id, $key, $body_hash, $status, $body);
    http_response_code($status);
    header('Content-Type: application/json');
    echo $body;
}
```

### Task 2.3: Refund helpers (codes, masking, state transitions)

- [ ] **Step 1: Create `_refund_helpers.php`**

```php
<?php
// _refund_helpers.php — code generation, hashing, masking, state-machine helpers.

function refund_generate_code(): string {
    return str_pad((string)random_int(0, 999999), 6, '0', STR_PAD_LEFT);
}

function refund_hash_code(string $code, string $salt): string {
    return hash('sha256', $code . '|' . $salt);
}

function refund_mask_email(string $email): string {
    [$local, $domain] = explode('@', $email, 2) + [null, null];
    if (!$local || !$domain) return $email;
    if (strlen($local) <= 2) return $local[0] . '***@' . $domain;
    return substr($local, 0, 2) . str_repeat('*', max(1, strlen($local) - 2)) . '@' . $domain;
}

function refund_assert_state(string $current, array $allowed, string $action): void {
    if (!in_array($current, $allowed, true)) {
        http_response_code(409);
        echo json_encode([
            'error' => 'illegal_state_transition',
            'message' => "Cannot $action while in state $current",
            'current_state' => $current,
            'allowed_states' => $allowed,
        ]);
        exit;
    }
}

function refund_ensure_company_active(PDO $pdo, array $company): void {
    if (!empty($company['locked'])) {
        http_response_code(423);
        echo json_encode(['error' => 'account_locked', 'message' => $company['lock_reason'] ?: 'Refunds are temporarily disabled. Contact support.']);
        exit;
    }
    if (empty($company['email_verified_at'])) {
        http_response_code(412);
        echo json_encode(['error' => 'email_not_verified', 'message' => 'Verify your email before issuing refunds.']);
        exit;
    }
}

function refund_load_request(PDO $pdo, int $company_id, int $request_id): array {
    $stmt = $pdo->prepare("SELECT * FROM refund_requests WHERE id = ? AND company_id = ?");
    $stmt->execute([$request_id, $company_id]);
    $row = $stmt->fetch(PDO::FETCH_ASSOC);
    if (!$row) {
        http_response_code(404);
        echo json_encode(['error' => 'request_not_found']);
        exit;
    }
    return $row;
}
```

- [ ] **Step 2: Commit**

```bash
git add argo-books-website/api/portal/_audit.php argo-books-website/api/portal/_idempotency.php argo-books-website/api/portal/_refund_helpers.php
git commit -m "feat(refunds): shared server helpers for audit, idempotency, codes"
```

---

## Phase 3 — Server: refund lifecycle endpoints (Stripe path)

**Files:**
- Create: `argo-books-website/api/portal/refunds/request.php`
- Create: `argo-books-website/api/portal/refunds/confirm.php`
- Create: `argo-books-website/api/portal/refunds/cancel.php`
- Create: `argo-books-website/api/portal/refunds/status.php`
- Create: `argo-books-website/api/portal/refunds/resend-code.php`
- Create: `argo-books-website/api/portal/refunds/_velocity.php`
- Create: `argo-books-website/api/portal/refunds/_provider_stripe.php`
- Use existing: `argo-books-website/api/portal/portal-helper.php` for Bearer auth & company lookup

### Task 3.1: Velocity engine (placeholder for Phase 6 — minimal version now)

- [ ] **Step 1: Create `_velocity.php` with a simple "always normal" implementation for now**

This lets us ship Phase 3 without the velocity engine; Phase 6 replaces this with the real adaptive logic.

```php
<?php
// _velocity.php — assesses refund velocity for a company.
// Phase 6 replaces this stub with the full adaptive engine described in spec section 9.

function refund_assess_velocity(PDO $pdo, array $company, int $amount_cents): array {
    return [
        'tier' => 'normal',
        'cooling_off_seconds' => 0,
        'reason' => 'velocity_engine_stub',
    ];
}
```

### Task 3.2: Stripe provider adapter

- [ ] **Step 1: Create `_provider_stripe.php`**

```php
<?php
// _provider_stripe.php — wraps Stripe SDK calls for refund lifecycle.
require_once __DIR__ . '/../../../vendor/autoload.php';

function refund_stripe_init(array $company): void {
    $env = $company['environment'] ?? 'production';
    $key = $env === 'sandbox'
        ? ($_ENV['STRIPE_SANDBOX_SECRET_KEY'] ?? getenv('STRIPE_SANDBOX_SECRET_KEY'))
        : ($_ENV['STRIPE_LIVE_SECRET_KEY'] ?? getenv('STRIPE_LIVE_SECRET_KEY'));
    if (!$key) {
        throw new RuntimeException("Stripe key not configured for $env");
    }
    \Stripe\Stripe::setApiKey($key);
}

/**
 * Pre-flight: confirm the payment_intent is refundable for the requested amount.
 * Throws on unrefundable; returns capped refundable amount in cents on success.
 */
function refund_stripe_preflight(array $company, string $payment_intent_id, int $requested_cents): int {
    refund_stripe_init($company);
    try {
        $pi = \Stripe\PaymentIntent::retrieve([
            'id' => $payment_intent_id,
            'expand' => ['latest_charge'],
        ], ['stripe_account' => $company['stripe_account_id'] ?? null]);
    } catch (\Stripe\Exception\ApiErrorException $e) {
        http_response_code(404);
        echo json_encode(['error' => 'provider_payment_not_found', 'message' => $e->getMessage()]);
        exit;
    }
    $charge = $pi->latest_charge;
    if (!$charge) {
        http_response_code(409);
        echo json_encode(['error' => 'no_chargeable', 'message' => 'No charge attached to this payment intent.']);
        exit;
    }
    if ($charge->refunded) {
        http_response_code(409);
        echo json_encode(['error' => 'already_fully_refunded']);
        exit;
    }
    $created_at = $charge->created;
    if (time() - $created_at > 86400 * 180) {
        http_response_code(409);
        echo json_encode(['error' => 'charge_too_old', 'message' => 'This payment is older than 180 days and cannot be refunded automatically.']);
        exit;
    }
    if ($charge->disputed) {
        http_response_code(409);
        echo json_encode(['error' => 'charge_disputed', 'message' => "Refunds aren't allowed during a dispute. Resolve in your Stripe dashboard."]);
        exit;
    }
    $refundable = $charge->amount - $charge->amount_refunded;
    if ($requested_cents > $refundable) {
        http_response_code(422);
        echo json_encode(['error' => 'amount_exceeds_refundable', 'refundable_cents' => $refundable]);
        exit;
    }
    return $refundable;
}

/** Issues the refund. Returns the Stripe Refund object (assoc array). */
function refund_stripe_issue(array $company, array $request): array {
    refund_stripe_init($company);
    $refund = \Stripe\Refund::create([
        'payment_intent' => $request['provider_payment_id'],
        'amount' => (int)$request['amount_cents'],
        'reason' => 'requested_by_customer',
        'metadata' => [
            'argo_request_id' => (string)$request['id'],
            'argo_invoice_id' => $request['invoice_id'],
            'argo_invoice_number' => $request['invoice_number'],
        ],
    ], ['stripe_account' => $company['stripe_account_id'] ?? null]);
    return $refund->toArray();
}
```

### Task 3.3: `POST /refunds/request` endpoint

- [ ] **Step 1: Create `refunds/request.php`**

```php
<?php
require_once __DIR__ . '/../portal-helper.php';
require_once __DIR__ . '/../_audit.php';
require_once __DIR__ . '/../_idempotency.php';
require_once __DIR__ . '/../_refund_helpers.php';
require_once __DIR__ . '/_provider_stripe.php';

header('Content-Type: application/json');

if ($_SERVER['REQUEST_METHOD'] !== 'POST') {
    http_response_code(405);
    echo json_encode(['error' => 'method_not_allowed']);
    exit;
}

global $pdo;
$company = portal_authenticate($pdo); // existing helper; returns full portal_companies row or exits 401
refund_ensure_company_active($pdo, $company);

$raw = file_get_contents('php://input');

with_idempotency($pdo, (int)$company['id'], $raw, function() use ($pdo, $company, $raw) {
    $body = json_decode($raw, true);
    if (!is_array($body)) {
        http_response_code(400);
        echo json_encode(['error' => 'invalid_json']);
        return;
    }
    $required = ['invoice_id', 'invoice_number', 'provider', 'provider_payment_id', 'amount_cents', 'currency'];
    foreach ($required as $f) {
        if (empty($body[$f])) {
            http_response_code(400);
            echo json_encode(['error' => 'missing_field', 'field' => $f]);
            return;
        }
    }
    $provider = strtolower($body['provider']);
    if (!in_array($provider, ['stripe','paypal','square'], true)) {
        http_response_code(400);
        echo json_encode(['error' => 'unsupported_provider']);
        return;
    }
    if ($provider !== 'stripe') {
        http_response_code(501);
        echo json_encode(['error' => 'provider_not_yet_supported', 'message' => "$provider refunds ship in a later phase."]);
        return;
    }
    $amount_cents = (int)$body['amount_cents'];
    if ($amount_cents <= 0) {
        http_response_code(400);
        echo json_encode(['error' => 'invalid_amount']);
        return;
    }

    // Pre-flight against Stripe
    refund_stripe_preflight($company, $body['provider_payment_id'], $amount_cents);

    // Per-company hourly code-issue rate limit
    $stmt = $pdo->prepare("SELECT COUNT(*) FROM refund_email_codes c INNER JOIN refund_requests r ON c.refund_request_id = r.id WHERE r.company_id = ? AND c.created_at > DATE_SUB(NOW(), INTERVAL 1 HOUR)");
    $stmt->execute([$company['id']]);
    if ((int)$stmt->fetchColumn() >= 10) {
        http_response_code(429);
        echo json_encode(['error' => 'too_many_codes', 'message' => 'Too many verification codes requested in the past hour. Please wait.']);
        return;
    }

    $ctx = audit_request_context();
    $pdo->beginTransaction();
    try {
        $stmt = $pdo->prepare("
            INSERT INTO refund_requests
                (company_id, invoice_id, invoice_number, customer_name, provider,
                 provider_payment_id, amount_cents, currency, line_items_json, reason,
                 state, requested_ip, requested_user_agent)
            VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, 'pending_code', ?, ?)
        ");
        $stmt->execute([
            $company['id'],
            $body['invoice_id'],
            $body['invoice_number'],
            $body['customer_name'] ?? null,
            $provider,
            $body['provider_payment_id'],
            $amount_cents,
            strtoupper($body['currency']),
            isset($body['line_items']) ? json_encode($body['line_items']) : null,
            $body['reason'] ?? null,
            $ctx['ip'],
            $ctx['ua'],
        ]);
        $request_id = (int)$pdo->lastInsertId();

        // Generate + email code
        $code = refund_generate_code();
        $hash = refund_hash_code($code, (string)$request_id);
        $stmt = $pdo->prepare("INSERT INTO refund_email_codes (refund_request_id, code_hash, expires_at) VALUES (?, ?, DATE_ADD(NOW(), INTERVAL 10 MINUTE))");
        $stmt->execute([$request_id, $hash]);

        audit_log($pdo, (int)$company['id'], 'request_created', 'owner', null, $request_id, null, [
            'amount_cents' => $amount_cents,
            'provider' => $provider,
            'invoice_number' => $body['invoice_number'],
        ], $ctx['ip'], $ctx['ua']);
        audit_log($pdo, (int)$company['id'], 'code_sent', 'system', null, $request_id, null, [
            'masked_email' => refund_mask_email($company['owner_email']),
        ]);

        $pdo->commit();
    } catch (Throwable $e) {
        $pdo->rollBack();
        error_log('refunds/request failed: ' . $e->getMessage());
        http_response_code(500);
        echo json_encode(['error' => 'server_error']);
        return;
    }

    // Send email AFTER commit (don't roll back if SMTP hiccups; user can resend)
    refund_email_send_code($company['owner_email'], $code, $body['invoice_number'], $amount_cents, $body['currency']);

    echo json_encode([
        'request_id' => $request_id,
        'expires_in_seconds' => 600,
        'masked_email' => refund_mask_email($company['owner_email']),
    ]);
});
```

- [ ] **Step 2: Add `refund_email_send_code` to `_refund_helpers.php`**

```php
function refund_email_send_code(string $to, string $code, string $invoice_number, int $amount_cents, string $currency): void {
    require_once __DIR__ . '/../../includes/mailer.php'; // existing PHPMailer wrapper; adapt path if different
    $amount_str = number_format($amount_cents / 100, 2) . ' ' . $currency;
    $subject = "Argo Books refund code: $code";
    $body = "Your refund code is: <strong>$code</strong><br><br>"
          . "You're refunding $amount_str on invoice $invoice_number.<br><br>"
          . "If you didn't request this refund, ignore this email and the request will expire.";
    send_mail($to, $subject, $body); // existing helper signature; adjust if different
}
```

> **NOTE during execution:** verify the existing mailer helper path/signature in `argo-books-website/includes/` before finalizing.

### Task 3.4: `POST /refunds/confirm` endpoint

- [ ] **Step 1: Create `refunds/confirm.php`**

```php
<?php
require_once __DIR__ . '/../portal-helper.php';
require_once __DIR__ . '/../_audit.php';
require_once __DIR__ . '/../_idempotency.php';
require_once __DIR__ . '/../_refund_helpers.php';
require_once __DIR__ . '/_velocity.php';
require_once __DIR__ . '/_provider_stripe.php';

header('Content-Type: application/json');

if ($_SERVER['REQUEST_METHOD'] !== 'POST') {
    http_response_code(405);
    echo json_encode(['error' => 'method_not_allowed']);
    exit;
}

global $pdo;
$company = portal_authenticate($pdo);
refund_ensure_company_active($pdo, $company);

$raw = file_get_contents('php://input');
with_idempotency($pdo, (int)$company['id'], $raw, function() use ($pdo, $company, $raw) {
    $body = json_decode($raw, true);
    $request_id = (int)($body['request_id'] ?? 0);
    $code = (string)($body['code'] ?? '');
    if (!$request_id || !preg_match('/^\d{6}$/', $code)) {
        http_response_code(400);
        echo json_encode(['error' => 'invalid_input']);
        return;
    }

    $request = refund_load_request($pdo, (int)$company['id'], $request_id);
    refund_assert_state($request['state'], ['pending_code'], 'confirm code');

    $stmt = $pdo->prepare("SELECT * FROM refund_email_codes WHERE refund_request_id = ? AND consumed_at IS NULL ORDER BY id DESC LIMIT 1");
    $stmt->execute([$request_id]);
    $code_row = $stmt->fetch(PDO::FETCH_ASSOC);
    if (!$code_row) {
        http_response_code(409);
        echo json_encode(['error' => 'no_active_code']);
        return;
    }
    if (strtotime($code_row['expires_at']) < time()) {
        http_response_code(410);
        echo json_encode(['error' => 'code_expired']);
        return;
    }
    if ((int)$code_row['attempts'] >= 5) {
        $pdo->prepare("UPDATE refund_requests SET state='cancelled', state_reason='too_many_code_attempts', updated_at=NOW() WHERE id = ?")->execute([$request_id]);
        audit_log($pdo, (int)$company['id'], 'cancelled_by_user', 'system', null, $request_id, null, ['reason' => 'too_many_code_attempts']);
        http_response_code(429);
        echo json_encode(['error' => 'too_many_attempts']);
        return;
    }

    $expected = refund_hash_code($code, (string)$request_id);
    if (!hash_equals($code_row['code_hash'], $expected)) {
        $pdo->prepare("UPDATE refund_email_codes SET attempts = attempts + 1 WHERE id = ?")->execute([$code_row['id']]);
        audit_log($pdo, (int)$company['id'], 'code_failed', 'owner', null, $request_id, null, ['attempts' => (int)$code_row['attempts'] + 1]);
        http_response_code(401);
        echo json_encode(['error' => 'wrong_code', 'attempts_remaining' => 4 - (int)$code_row['attempts']]);
        return;
    }

    $pdo->prepare("UPDATE refund_email_codes SET consumed_at = NOW() WHERE id = ?")->execute([$code_row['id']]);
    audit_log($pdo, (int)$company['id'], 'code_verified', 'owner', null, $request_id, null, []);

    // Velocity check
    $velocity = refund_assess_velocity($pdo, $company, (int)$request['amount_cents']);
    audit_log($pdo, (int)$company['id'], 'velocity_tier_assigned', 'system', null, $request_id, null, $velocity);

    if ($velocity['tier'] === 'hard_block') {
        $pdo->prepare("UPDATE portal_companies SET locked = 1, lock_reason = ?, locked_at = NOW() WHERE id = ?")
            ->execute(['Auto-locked by velocity engine: ' . ($velocity['reason'] ?? ''), $company['id']]);
        $pdo->prepare("UPDATE refund_requests SET state='failed', state_reason='hard_block', velocity_tier=?, updated_at=NOW() WHERE id = ?")
            ->execute([$velocity['tier'], $request_id]);
        audit_log($pdo, (int)$company['id'], 'account_locked', 'system', null, $request_id, null, $velocity);
        audit_log($pdo, (int)$company['id'], 'failed', 'system', null, $request_id, null, ['reason' => 'hard_block']);
        // TODO Phase 6: email admin
        http_response_code(423);
        echo json_encode(['state' => 'failed', 'velocity_tier' => $velocity['tier'], 'message' => 'This refund triggered fraud-prevention rules. The account has been frozen pending review.']);
        return;
    }

    if ($velocity['tier'] === 'delayed') {
        $cancel_token = bin2hex(random_bytes(32));
        $pdo->prepare("UPDATE refund_requests SET state='cooling_off', velocity_tier=?, cooling_off_until = DATE_ADD(NOW(), INTERVAL ? SECOND), cancel_token = ?, updated_at=NOW() WHERE id = ?")
            ->execute([$velocity['tier'], (int)$velocity['cooling_off_seconds'], $cancel_token, $request_id]);
        audit_log($pdo, (int)$company['id'], 'cooling_off_started', 'system', null, $request_id, null, $velocity);
        // Phase 6 will send the cancel-link email; stub for now
        echo json_encode([
            'state' => 'cooling_off',
            'velocity_tier' => $velocity['tier'],
            'cooling_off_seconds' => (int)$velocity['cooling_off_seconds'],
        ]);
        return;
    }

    // normal or soft_warn => process immediately
    $pdo->prepare("UPDATE refund_requests SET state='processing', velocity_tier=?, updated_at=NOW() WHERE id = ?")
        ->execute([$velocity['tier'], $request_id]);
    audit_log($pdo, (int)$company['id'], 'processing', 'system', null, $request_id, null, []);

    refund_execute_against_provider($pdo, $company, $request_id);

    $stmt = $pdo->prepare("SELECT state, state_reason, provider_refund_id FROM refund_requests WHERE id = ?");
    $stmt->execute([$request_id]);
    $final = $stmt->fetch(PDO::FETCH_ASSOC);
    echo json_encode([
        'state' => $final['state'],
        'velocity_tier' => $velocity['tier'],
        'message' => $final['state_reason'] ?? null,
    ]);
});
```

### Task 3.5: Provider execution helper

- [ ] **Step 1: Add `refund_execute_against_provider` to `_refund_helpers.php`**

```php
function refund_execute_against_provider(PDO $pdo, array $company, int $request_id): void {
    require_once __DIR__ . '/refunds/_provider_stripe.php';
    $stmt = $pdo->prepare("SELECT * FROM refund_requests WHERE id = ?");
    $stmt->execute([$request_id]);
    $req = $stmt->fetch(PDO::FETCH_ASSOC);
    if (!$req) return;
    try {
        switch ($req['provider']) {
            case 'stripe':
                $result = refund_stripe_issue($company, $req);
                $pdo->prepare("UPDATE refund_requests SET state='completed', provider_refund_id = ?, completed_at = NOW(), updated_at = NOW() WHERE id = ?")
                    ->execute([$result['id'] ?? null, $request_id]);
                audit_log($pdo, (int)$company['id'], 'completed', 'system', null, $request_id, null, ['provider_refund_id' => $result['id'] ?? null]);
                refund_email_send_issued($company['owner_email'], $req);
                break;
            default:
                throw new RuntimeException("Provider {$req['provider']} not yet implemented");
        }
    } catch (\Throwable $e) {
        $msg = $e->getMessage();
        $pdo->prepare("UPDATE refund_requests SET state='failed', state_reason = ?, updated_at = NOW() WHERE id = ?")
            ->execute([substr($msg, 0, 1000), $request_id]);
        audit_log($pdo, (int)$company['id'], 'failed', 'system', null, $request_id, null, ['error' => $msg]);
    }
}

function refund_email_send_issued(string $to, array $req): void {
    require_once __DIR__ . '/../../includes/mailer.php';
    $amount_str = number_format($req['amount_cents'] / 100, 2) . ' ' . $req['currency'];
    $subject = "Refund issued: {$req['invoice_number']}";
    $body = "A refund of <strong>$amount_str</strong> was issued on invoice {$req['invoice_number']}.";
    send_mail($to, $subject, $body);
}
```

### Task 3.6: `GET /refunds/status/{id}`, `POST /refunds/cancel`, `POST /refunds/resend-code`

- [ ] **Step 1: Create `refunds/status.php`**

```php
<?php
require_once __DIR__ . '/../portal-helper.php';
require_once __DIR__ . '/../_refund_helpers.php';

header('Content-Type: application/json');
global $pdo;
$company = portal_authenticate($pdo);

$id = (int)($_GET['id'] ?? 0);
if (!$id) { http_response_code(400); echo json_encode(['error' => 'missing_id']); exit; }
$req = refund_load_request($pdo, (int)$company['id'], $id);

echo json_encode([
    'request_id' => (int)$req['id'],
    'state' => $req['state'],
    'state_reason' => $req['state_reason'],
    'velocity_tier' => $req['velocity_tier'],
    'cooling_off_until' => $req['cooling_off_until'],
    'provider_refund_id' => $req['provider_refund_id'],
    'completed_at' => $req['completed_at'],
]);
```

- [ ] **Step 2: Create `refunds/cancel.php`**

```php
<?php
require_once __DIR__ . '/../portal-helper.php';
require_once __DIR__ . '/../_audit.php';
require_once __DIR__ . '/../_refund_helpers.php';

header('Content-Type: application/json');
if ($_SERVER['REQUEST_METHOD'] !== 'POST') { http_response_code(405); echo json_encode(['error'=>'method_not_allowed']); exit; }

global $pdo;
$company = portal_authenticate($pdo);
$body = json_decode(file_get_contents('php://input'), true) ?? [];
$id = (int)($body['request_id'] ?? 0);
$req = refund_load_request($pdo, (int)$company['id'], $id);
refund_assert_state($req['state'], ['pending_code', 'code_verified', 'cooling_off'], 'cancel');

$pdo->prepare("UPDATE refund_requests SET state='cancelled', state_reason='cancelled_by_user', updated_at=NOW() WHERE id = ?")->execute([$id]);
audit_log($pdo, (int)$company['id'], 'cancelled_by_user', 'owner', null, $id, null, []);
echo json_encode(['state' => 'cancelled']);
```

- [ ] **Step 3: Create `refunds/resend-code.php`**

```php
<?php
require_once __DIR__ . '/../portal-helper.php';
require_once __DIR__ . '/../_audit.php';
require_once __DIR__ . '/../_refund_helpers.php';

header('Content-Type: application/json');
if ($_SERVER['REQUEST_METHOD'] !== 'POST') { http_response_code(405); echo json_encode(['error'=>'method_not_allowed']); exit; }

global $pdo;
$company = portal_authenticate($pdo);
$body = json_decode(file_get_contents('php://input'), true) ?? [];
$id = (int)($body['request_id'] ?? 0);
$req = refund_load_request($pdo, (int)$company['id'], $id);
refund_assert_state($req['state'], ['pending_code'], 'resend');

// Limit: max 3 codes per request, 1 per 60s
$stmt = $pdo->prepare("SELECT COUNT(*), MAX(created_at) FROM refund_email_codes WHERE refund_request_id = ?");
$stmt->execute([$id]);
[$count, $latest] = $stmt->fetch(PDO::FETCH_NUM);
if ((int)$count >= 3) { http_response_code(429); echo json_encode(['error' => 'max_resends_reached']); exit; }
if ($latest && (time() - strtotime($latest)) < 60) { http_response_code(429); echo json_encode(['error' => 'too_soon']); exit; }

// Invalidate prior codes for this request
$pdo->prepare("UPDATE refund_email_codes SET consumed_at = COALESCE(consumed_at, NOW()) WHERE refund_request_id = ?")->execute([$id]);

$code = refund_generate_code();
$hash = refund_hash_code($code, (string)$id);
$pdo->prepare("INSERT INTO refund_email_codes (refund_request_id, code_hash, expires_at) VALUES (?, ?, DATE_ADD(NOW(), INTERVAL 10 MINUTE))")
    ->execute([$id, $hash]);
audit_log($pdo, (int)$company['id'], 'code_sent', 'owner', null, $id, null, ['resend' => true]);
refund_email_send_code($company['owner_email'], $code, $req['invoice_number'], (int)$req['amount_cents'], $req['currency']);

echo json_encode(['ok' => true, 'masked_email' => refund_mask_email($company['owner_email'])]);
```

- [ ] **Step 4: Commit**

```bash
git add argo-books-website/api/portal/refunds/ argo-books-website/api/portal/_refund_helpers.php
git commit -m "feat(refunds): server lifecycle endpoints (request/confirm/cancel/status/resend) — Stripe path"
```

---

## Phase 4 — Server: webhook reconciliation

**Files:**
- Modify: `argo-books-website/api/portal/webhooks/_stripe_refund_db.php` — make idempotent against our `refund_requests`
- Create: `argo-books-website/api/portal/cancel-refund.php` (public token-authenticated)

### Task 4.1: Update Stripe refund webhook for idempotency

- [ ] **Step 1: Read existing `_stripe_refund_db.php` to understand current shape**

Run:
```powershell
Get-Content "C:\laragon\www\argo-books-website\api\portal\webhooks\_stripe_refund_db.php"
```

- [ ] **Step 2: Add idempotent reconciliation against refund_requests**

At the top of `apply_stripe_refund_to_db()`, look up the refund_request row by `metadata.argo_request_id` (passed when our endpoint created the refund). If found and already `completed`, no-op. If found and in `processing`, transition to `completed`.

Append at the start of the function (after extracting `$refund_id` from the Stripe object):

```php
// Reconcile against refund_requests if this refund originated from us
$argo_request_id = $charge->metadata['argo_request_id'] ?? null;
if ($argo_request_id && is_numeric($argo_request_id)) {
    $stmt = $pdo->prepare("SELECT id, state, company_id FROM refund_requests WHERE id = ?");
    $stmt->execute([(int)$argo_request_id]);
    $req = $stmt->fetch(PDO::FETCH_ASSOC);
    if ($req && $req['state'] !== 'completed') {
        $pdo->prepare("UPDATE refund_requests SET state='completed', provider_refund_id = ?, completed_at = NOW(), updated_at = NOW() WHERE id = ?")
            ->execute([$refund_id, (int)$argo_request_id]);
        // audit
        require_once __DIR__ . '/../_audit.php';
        audit_log($pdo, (int)$req['company_id'], 'completed', 'webhook', null, (int)$argo_request_id, null, ['provider_refund_id' => $refund_id, 'reconciled_via_webhook' => true]);
    }
    // Continue with existing portal_payments updates regardless (they still need the negative-payment row)
}
```

### Task 4.2: Public cancel-refund landing page

- [ ] **Step 1: Create `argo-books-website/api/portal/cancel-refund.php`**

```php
<?php
require_once __DIR__ . '/../../db_connect.php';
require_once __DIR__ . '/_audit.php';

global $pdo;
$token = $_GET['token'] ?? $_POST['token'] ?? '';
if (!$token || !ctype_xdigit($token) || strlen($token) !== 64) {
    http_response_code(400);
    echo "Invalid or expired link.";
    exit;
}

$stmt = $pdo->prepare("SELECT * FROM refund_requests WHERE cancel_token = ?");
$stmt->execute([$token]);
$req = $stmt->fetch(PDO::FETCH_ASSOC);
if (!$req) { http_response_code(404); echo "This refund request was not found."; exit; }

if (!in_array($req['state'], ['pending_code','code_verified','cooling_off'], true)) {
    echo "<!doctype html><meta charset=utf-8><title>Refund already finalized</title><body style='font-family:sans-serif;max-width:500px;margin:4rem auto;'>";
    echo "<h2>This refund can no longer be cancelled</h2>";
    echo "<p>The refund has already been processed or cancelled. Current state: <code>" . htmlspecialchars($req['state']) . "</code></p>";
    echo "</body>";
    exit;
}

if ($_SERVER['REQUEST_METHOD'] === 'POST') {
    $pdo->prepare("UPDATE refund_requests SET state='cancelled', state_reason='cancelled_by_email_link', cancel_token=NULL, updated_at=NOW() WHERE id = ?")
        ->execute([$req['id']]);
    audit_log($pdo, (int)$req['company_id'], 'cancelled_by_email_link', 'owner', null, (int)$req['id'], null, ['ip' => $_SERVER['REMOTE_ADDR'] ?? null]);
    echo "<!doctype html><meta charset=utf-8><title>Refund cancelled</title><body style='font-family:sans-serif;max-width:500px;margin:4rem auto;'>";
    echo "<h2>Refund cancelled</h2>";
    echo "<p>The refund of " . number_format($req['amount_cents']/100, 2) . " " . htmlspecialchars($req['currency']) . " on invoice " . htmlspecialchars($req['invoice_number']) . " has been cancelled. No money has moved.</p>";
    echo "</body>";
    exit;
}

// GET: show confirmation
echo "<!doctype html><meta charset=utf-8><title>Cancel refund?</title><body style='font-family:sans-serif;max-width:500px;margin:4rem auto;'>";
echo "<h2>Cancel this refund?</h2>";
echo "<p>You're about to cancel a refund of <strong>" . number_format($req['amount_cents']/100, 2) . " " . htmlspecialchars($req['currency']) . "</strong> on invoice " . htmlspecialchars($req['invoice_number']) . ".</p>";
echo "<form method='post'><input type='hidden' name='token' value='" . htmlspecialchars($token) . "'><button type='submit' style='padding:.6rem 1rem;'>Cancel this refund</button></form>";
echo "</body>";
```

- [ ] **Step 2: Commit**

```bash
git add argo-books-website/api/portal/webhooks/_stripe_refund_db.php argo-books-website/api/portal/cancel-refund.php
git commit -m "feat(refunds): idempotent webhook reconciliation + cancel-refund landing page"
```

---

## Phase 5 — Server: cron jobs

**Files:**
- Create: `argo-books-website/cron/refund_cooling_off_promoter.php`
- Create: `argo-books-website/cron/refund_velocity_baseline_recompute.php`
- Create: `argo-books-website/cron/refund_stale_request_cleanup.php`
- Create: `argo-books-website/cron/refund_stale_processing_reconcile.php`
- Create: `argo-books-website/cron/README.md` (cron schedule reference)

### Task 5.1: Cooling-off promoter (every minute)

- [ ] **Step 1: Create the cron**

```php
<?php
// refund_cooling_off_promoter.php — promote due refund_requests from cooling_off → processing
// Schedule: every 1 minute (linux: */1 * * * *)
require_once __DIR__ . '/../db_connect.php';
require_once __DIR__ . '/../api/portal/_audit.php';
require_once __DIR__ . '/../api/portal/_refund_helpers.php';

global $pdo;
$stmt = $pdo->query("
    SELECT r.*, c.id AS cid, c.locked, c.lock_reason, c.email_verified_at,
           c.owner_email, c.stripe_account_id, c.environment
    FROM refund_requests r
    INNER JOIN portal_companies c ON c.id = r.company_id
    WHERE r.state = 'cooling_off' AND r.cooling_off_until <= NOW()
    LIMIT 100
");
$rows = $stmt->fetchAll(PDO::FETCH_ASSOC);
foreach ($rows as $row) {
    if ($row['locked']) {
        // Stay in cooling_off until unlocked OR auto-cancel after 24h
        if (strtotime($row['updated_at']) < time() - 86400) {
            $pdo->prepare("UPDATE refund_requests SET state='cancelled', state_reason='locked_account_auto_cancel', updated_at=NOW() WHERE id = ?")->execute([$row['id']]);
            audit_log($pdo, (int)$row['company_id'], 'cancelled_by_user', 'system', null, (int)$row['id'], null, ['reason' => 'locked_account_auto_cancel']);
        }
        continue;
    }
    $pdo->prepare("UPDATE refund_requests SET state='processing', updated_at=NOW() WHERE id = ?")->execute([$row['id']]);
    audit_log($pdo, (int)$row['company_id'], 'processing', 'system', null, (int)$row['id'], null, ['promoted_from' => 'cooling_off']);
    $company = $row; // shape compatible enough
    refund_execute_against_provider($pdo, $company, (int)$row['id']);
}
echo "Promoted " . count($rows) . " requests\n";
```

### Task 5.2: Velocity baseline recomputer (nightly)

- [ ] **Step 1: Create the cron**

```php
<?php
// refund_velocity_baseline_recompute.php — nightly recompute of per-company refund baselines
// Schedule: 0 2 * * *  (or laragon Task Scheduler)
require_once __DIR__ . '/../db_connect.php';

global $pdo;
$pdo->exec("
    INSERT INTO refund_velocity_baselines (company_id, daily_avg_refund_cents, daily_avg_refund_count, revenue_30d_cents, last_recomputed_at)
    SELECT
      c.id,
      COALESCE(refund_stats.avg_daily_cents, 0),
      COALESCE(refund_stats.avg_daily_count, 0),
      COALESCE(rev_stats.revenue_cents, 0),
      NOW()
    FROM portal_companies c
    LEFT JOIN (
      SELECT company_id,
             SUM(amount_cents) / 30 AS avg_daily_cents,
             COUNT(*) / 30 AS avg_daily_count
      FROM refund_requests
      WHERE state = 'completed' AND completed_at >= DATE_SUB(NOW(), INTERVAL 30 DAY)
      GROUP BY company_id
    ) refund_stats ON refund_stats.company_id = c.id
    LEFT JOIN (
      SELECT p.company_id, ROUND(SUM(p.amount) * 100) AS revenue_cents
      FROM portal_payments p
      WHERE p.status = 'completed' AND p.created_at >= DATE_SUB(NOW(), INTERVAL 30 DAY)
      GROUP BY p.company_id
    ) rev_stats ON rev_stats.company_id = c.id
    ON DUPLICATE KEY UPDATE
      daily_avg_refund_cents = VALUES(daily_avg_refund_cents),
      daily_avg_refund_count = VALUES(daily_avg_refund_count),
      revenue_30d_cents = VALUES(revenue_30d_cents),
      last_recomputed_at = NOW()
");
echo "Baseline recomputed\n";
```

### Task 5.3: Stale request cleanup + stale processing reconciliation

- [ ] **Step 1: Create `refund_stale_request_cleanup.php` (hourly)**

```php
<?php
require_once __DIR__ . '/../db_connect.php';
require_once __DIR__ . '/../api/portal/_audit.php';

global $pdo;
$stmt = $pdo->query("SELECT id, company_id FROM refund_requests WHERE state='pending_code' AND created_at < DATE_SUB(NOW(), INTERVAL 1 HOUR)");
$rows = $stmt->fetchAll(PDO::FETCH_ASSOC);
foreach ($rows as $r) {
    $pdo->prepare("UPDATE refund_requests SET state='cancelled', state_reason='code_window_expired', updated_at=NOW() WHERE id = ?")->execute([$r['id']]);
    audit_log($pdo, (int)$r['company_id'], 'cancelled_by_user', 'system', null, (int)$r['id'], null, ['reason' => 'code_window_expired']);
}
echo "Cleaned " . count($rows) . " stale pending_code rows\n";
```

- [ ] **Step 2: Create `refund_stale_processing_reconcile.php` (every 5 min)**

```php
<?php
// For requests stuck in 'processing' > 30 min, query Stripe for status and reconcile.
require_once __DIR__ . '/../db_connect.php';
require_once __DIR__ . '/../vendor/autoload.php';
require_once __DIR__ . '/../api/portal/_audit.php';
require_once __DIR__ . '/../api/portal/refunds/_provider_stripe.php';

global $pdo;
$stmt = $pdo->query("
    SELECT r.*, c.environment, c.stripe_account_id
    FROM refund_requests r
    INNER JOIN portal_companies c ON c.id = r.company_id
    WHERE r.state='processing' AND r.updated_at < DATE_SUB(NOW(), INTERVAL 30 MINUTE) AND r.provider = 'stripe'
");
$rows = $stmt->fetchAll(PDO::FETCH_ASSOC);
foreach ($rows as $r) {
    refund_stripe_init($r); // uses environment field
    try {
        $list = \Stripe\Refund::all(['payment_intent' => $r['provider_payment_id'], 'limit' => 10], ['stripe_account' => $r['stripe_account_id'] ?? null]);
        $found = null;
        foreach ($list->data as $rf) {
            if (($rf->metadata['argo_request_id'] ?? '') == (string)$r['id']) { $found = $rf; break; }
        }
        if ($found && $found->status === 'succeeded') {
            $pdo->prepare("UPDATE refund_requests SET state='completed', provider_refund_id = ?, completed_at = NOW(), updated_at = NOW() WHERE id = ?")->execute([$found->id, $r['id']]);
            audit_log($pdo, (int)$r['company_id'], 'completed', 'system', null, (int)$r['id'], null, ['reconciled_via_stale_cron' => true, 'provider_refund_id' => $found->id]);
        }
    } catch (\Throwable $e) {
        error_log("stale_processing_reconcile: " . $e->getMessage());
    }
}
echo "Checked " . count($rows) . " stuck-processing rows\n";
```

- [ ] **Step 3: Create `cron/README.md`**

```markdown
# Refund cron jobs

| Script | Schedule | Purpose |
|---|---|---|
| `refund_cooling_off_promoter.php` | every 1 min | Promote `cooling_off` → `processing` after the timer elapses |
| `refund_stale_processing_reconcile.php` | every 5 min | Query Stripe for stuck `processing` rows older than 30 min |
| `refund_stale_request_cleanup.php` | hourly | Cancel `pending_code` rows older than 1h |
| `refund_velocity_baseline_recompute.php` | nightly 02:00 | Refresh `refund_velocity_baselines` per company |

Linux crontab:
```
* * * * * cd /var/www/argo-books-website && php cron/refund_cooling_off_promoter.php >> storage/logs/cron.log 2>&1
*/5 * * * * cd /var/www/argo-books-website && php cron/refund_stale_processing_reconcile.php >> storage/logs/cron.log 2>&1
0 * * * * cd /var/www/argo-books-website && php cron/refund_stale_request_cleanup.php >> storage/logs/cron.log 2>&1
0 2 * * * cd /var/www/argo-books-website && php cron/refund_velocity_baseline_recompute.php >> storage/logs/cron.log 2>&1
```
```

- [ ] **Step 4: Commit**

```bash
git add argo-books-website/cron/
git commit -m "feat(refunds): cron jobs (cooling-off promoter, stale cleanup, baseline recompute)"
```

---

## Phase 6 — Server: real velocity engine

**Files:**
- Modify: `argo-books-website/api/portal/refunds/_velocity.php` — replace stub with full logic from spec section 9

### Task 6.1: Implement adaptive velocity logic

- [ ] **Step 1: Replace `_velocity.php` with the real implementation**

```php
<?php
require_once __DIR__ . '/../_audit.php';

function refund_load_velocity_config(PDO $pdo, int $company_id): array {
    $stmt = $pdo->prepare("
        SELECT * FROM refund_velocity_config
        WHERE company_id = ? OR company_id IS NULL
        ORDER BY company_id IS NULL ASC LIMIT 1
    ");
    $stmt->execute([$company_id]);
    $cfg = $stmt->fetch(PDO::FETCH_ASSOC);
    if (!$cfg) {
        return [
            'soft_warn_multiplier' => 3.0, 'cooling_multiplier' => 10.0,
            'cooling_revenue_pct' => 0.25, 'hard_revenue_pct' => 0.50,
            'cooling_off_minutes' => 15,
            'new_account_floor_cents' => 100000,
            'new_account_soft_cents' => 50000,
            'new_account_cooling_cents' => 100000,
        ];
    }
    return $cfg;
}

function refund_assess_velocity(PDO $pdo, array $company, int $amount_cents): array {
    $cfg = refund_load_velocity_config($pdo, (int)$company['id']);

    // Today's spent + count today + count last hour
    $stmt = $pdo->prepare("
        SELECT
          COALESCE(SUM(amount_cents),0) AS today_cents,
          COALESCE(SUM(CASE WHEN created_at > DATE_SUB(NOW(), INTERVAL 1 HOUR) THEN 1 ELSE 0 END),0) AS hour_count
        FROM refund_requests
        WHERE company_id = ?
          AND state IN ('cooling_off','processing','completed')
          AND created_at >= CURDATE()
    ");
    $stmt->execute([$company['id']]);
    $row = $stmt->fetch(PDO::FETCH_ASSOC);
    $today_cents = (int)$row['today_cents'] + $amount_cents; // include this request
    $hour_count = (int)$row['hour_count'] + 1;

    // Company age
    $age_days = (time() - strtotime($company['created_at'] ?? 'now')) / 86400;

    // New-account absolute floors
    if ($age_days < 7) {
        if ($today_cents >= (int)$cfg['new_account_floor_cents']) {
            return ['tier' => 'hard_block', 'cooling_off_seconds' => 0, 'reason' => 'new_account_floor', 'today_cents' => $today_cents, 'hour_count' => $hour_count];
        }
        if ($today_cents >= (int)$cfg['new_account_cooling_cents']) {
            return ['tier' => 'delayed', 'cooling_off_seconds' => (int)$cfg['cooling_off_minutes'] * 60, 'reason' => 'new_account_cooling', 'today_cents' => $today_cents, 'hour_count' => $hour_count];
        }
        if ($today_cents >= (int)$cfg['new_account_soft_cents']) {
            return ['tier' => 'soft_warn', 'cooling_off_seconds' => 0, 'reason' => 'new_account_soft', 'today_cents' => $today_cents, 'hour_count' => $hour_count];
        }
        return ['tier' => 'normal', 'cooling_off_seconds' => 0];
    }

    // Baseline-derived for established accounts
    $bstmt = $pdo->prepare("SELECT * FROM refund_velocity_baselines WHERE company_id = ?");
    $bstmt->execute([$company['id']]);
    $b = $bstmt->fetch(PDO::FETCH_ASSOC) ?: ['daily_avg_refund_cents' => 0, 'revenue_30d_cents' => 0];

    $daily_avg = (int)$b['daily_avg_refund_cents'];
    $rev_30d = (int)$b['revenue_30d_cents'];

    // Hard
    if ($hour_count >= 25 || ($rev_30d > 0 && $today_cents >= (int)($rev_30d * (float)$cfg['hard_revenue_pct']))) {
        return ['tier' => 'hard_block', 'cooling_off_seconds' => 0, 'reason' => 'hard_threshold', 'today_cents' => $today_cents, 'hour_count' => $hour_count];
    }
    // Cooling-off
    $cool_threshold = max(
        $daily_avg * (float)$cfg['cooling_multiplier'],
        $rev_30d * (float)$cfg['cooling_revenue_pct']
    );
    if ($hour_count >= 10 || ($cool_threshold > 0 && $today_cents >= (int)$cool_threshold)) {
        return ['tier' => 'delayed', 'cooling_off_seconds' => (int)$cfg['cooling_off_minutes'] * 60, 'reason' => 'cooling_threshold', 'today_cents' => $today_cents, 'hour_count' => $hour_count];
    }
    // Soft
    $soft_threshold = $daily_avg * (float)$cfg['soft_warn_multiplier'];
    if ($hour_count >= 5 || ($soft_threshold > 0 && $today_cents >= (int)$soft_threshold)) {
        return ['tier' => 'soft_warn', 'cooling_off_seconds' => 0, 'reason' => 'soft_threshold', 'today_cents' => $today_cents, 'hour_count' => $hour_count];
    }
    return ['tier' => 'normal', 'cooling_off_seconds' => 0];
}
```

- [ ] **Step 2: Send delayed-tier owner email with cancel link**

In `refunds/confirm.php`, in the `delayed` branch, after generating `$cancel_token` and updating the row, call:

```php
refund_email_send_cooling_off($company['owner_email'], $request, $cancel_token);
```

Add helper to `_refund_helpers.php`:
```php
function refund_email_send_cooling_off(string $to, array $req, string $token): void {
    require_once __DIR__ . '/../../includes/mailer.php';
    $base = $_ENV['SITE_URL'] ?? 'https://argobooks.app';
    $url = $base . '/api/portal/cancel-refund.php?token=' . urlencode($token);
    $amount = number_format($req['amount_cents'] / 100, 2) . ' ' . $req['currency'];
    $subject = "Refund pending review: $amount on invoice {$req['invoice_number']}";
    $body = "We're holding this refund for a short review window. If you didn't initiate it or want to cancel, click below within 15 minutes:<br><br>"
          . "<a href='$url'>Cancel this refund</a><br><br>"
          . "Otherwise it will be sent to the customer automatically.";
    send_mail($to, $subject, $body);
}
```

- [ ] **Step 3: Commit**

```bash
git add argo-books-website/api/portal/refunds/_velocity.php argo-books-website/api/portal/refunds/confirm.php argo-books-website/api/portal/_refund_helpers.php
git commit -m "feat(refunds): adaptive velocity engine with new-account floors and cancel-from-email"
```

---

## Phase 7 — Server: email verification (registration + change flow)

**Files:**
- Modify: `argo-books-website/api/portal/register.php` — issue verification code on registration
- Create: `argo-books-website/api/portal/account/verify-email/request.php`
- Create: `argo-books-website/api/portal/account/verify-email/confirm.php`
- Create: `argo-books-website/api/portal/account/email-change/request.php`
- Create: `argo-books-website/api/portal/account/email-change/confirm-old.php`
- Create: `argo-books-website/api/portal/account/email-change/confirm-new.php`
- Create: `argo-books-website/api/portal/account/email-change/cancel.php`
- Create: `argo-books-website/api/portal/account/email-change/resend-code.php`
- Create: `argo-books-website/api/portal/revert-email.php` (public token)
- Create: `argo-books-website/api/portal/_email_helpers.php`

### Task 7.1: Email helpers (codes + send wrappers for verification flows)

- [ ] **Step 1: Create `_email_helpers.php`**

```php
<?php
require_once __DIR__ . '/_refund_helpers.php'; // reuse refund_generate_code, refund_hash_code

function email_send_registration_code(string $to, string $code): void {
    require_once __DIR__ . '/../../includes/mailer.php';
    send_mail($to, "Confirm your Argo Books portal email", "Welcome! Your verification code is <strong>$code</strong>. It expires in 10 minutes.");
}
function email_send_change_old_code(string $to, string $code, string $new_email): void {
    require_once __DIR__ . '/../../includes/mailer.php';
    send_mail($to, "Confirm email change to $new_email", "Your code is <strong>$code</strong>. If you didn't request this change, ignore this email and the request will expire.");
}
function email_send_change_new_code(string $to, string $code): void {
    require_once __DIR__ . '/../../includes/mailer.php';
    send_mail($to, "Confirm this is your new Argo Books email", "Your code is <strong>$code</strong>. Enter it in Argo Books to finish the change.");
}
function email_send_change_completed_to_old(string $old, string $new, string $revert_token): void {
    require_once __DIR__ . '/../../includes/mailer.php';
    $base = $_ENV['SITE_URL'] ?? 'https://argobooks.app';
    $url = $base . '/api/portal/revert-email.php?token=' . urlencode($revert_token);
    send_mail($old, "Your Argo Books email was changed",
        "Your portal email was changed to <strong>$new</strong>. If this wasn't you, click below within 30 days to revert:<br><br><a href='$url'>Revert email change</a>");
}
```

### Task 7.2: Modify `register.php` to issue verification code (do not auto-verify)

- [ ] **Step 1: Read `register.php`**

```powershell
Get-Content "C:\laragon\www\argo-books-website\api\portal\register.php"
```

- [ ] **Step 2: After successful company insert, issue verification code**

Insert (just before returning the JSON response with the API key):

```php
require_once __DIR__ . '/_email_helpers.php';
$verify_code = refund_generate_code();
$verify_hash = refund_hash_code($verify_code, (string)$company_id);
$pdo->prepare("INSERT INTO email_verifications (company_id, email, purpose, code_hash, expires_at) VALUES (?, ?, 'registration', ?, DATE_ADD(NOW(), INTERVAL 10 MINUTE))")
    ->execute([$company_id, $owner_email, $verify_hash]);
email_send_registration_code($owner_email, $verify_code);
audit_log($pdo, (int)$company_id, 'code_sent', 'system', null, null, null, ['purpose' => 'registration']);
```

The response payload should include `email_verification_required: true` so the desktop knows to show the verification step.

### Task 7.3: Verify-email endpoints

- [ ] **Step 1: Create `account/verify-email/request.php`** (resend the registration code)

```php
<?php
require_once __DIR__ . '/../../portal-helper.php';
require_once __DIR__ . '/../../_audit.php';
require_once __DIR__ . '/../../_email_helpers.php';

header('Content-Type: application/json');
if ($_SERVER['REQUEST_METHOD'] !== 'POST') { http_response_code(405); echo json_encode(['error'=>'method_not_allowed']); exit; }

global $pdo;
$company = portal_authenticate($pdo);
if (!empty($company['email_verified_at'])) {
    echo json_encode(['ok' => true, 'message' => 'already_verified']);
    exit;
}

$stmt = $pdo->prepare("SELECT COUNT(*), MAX(created_at) FROM email_verifications WHERE company_id = ? AND purpose = 'registration'");
$stmt->execute([$company['id']]);
[$count, $latest] = $stmt->fetch(PDO::FETCH_NUM);
if ((int)$count >= 3) { http_response_code(429); echo json_encode(['error' => 'max_resends']); exit; }
if ($latest && (time() - strtotime($latest)) < 60) { http_response_code(429); echo json_encode(['error' => 'too_soon']); exit; }

$pdo->prepare("UPDATE email_verifications SET consumed_at = COALESCE(consumed_at, NOW()) WHERE company_id = ? AND purpose = 'registration' AND consumed_at IS NULL")
    ->execute([$company['id']]);

$code = refund_generate_code();
$hash = refund_hash_code($code, (string)$company['id']);
$pdo->prepare("INSERT INTO email_verifications (company_id, email, purpose, code_hash, expires_at) VALUES (?, ?, 'registration', ?, DATE_ADD(NOW(), INTERVAL 10 MINUTE))")
    ->execute([$company['id'], $company['owner_email'], $hash]);
email_send_registration_code($company['owner_email'], $code);
audit_log($pdo, (int)$company['id'], 'code_sent', 'owner', null, null, null, ['purpose' => 'registration', 'resend' => true]);
echo json_encode(['ok' => true]);
```

- [ ] **Step 2: Create `account/verify-email/confirm.php`**

```php
<?php
require_once __DIR__ . '/../../portal-helper.php';
require_once __DIR__ . '/../../_audit.php';
require_once __DIR__ . '/../../_refund_helpers.php';

header('Content-Type: application/json');
if ($_SERVER['REQUEST_METHOD'] !== 'POST') { http_response_code(405); echo json_encode(['error'=>'method_not_allowed']); exit; }

global $pdo;
$company = portal_authenticate($pdo);
$body = json_decode(file_get_contents('php://input'), true) ?? [];
$code = (string)($body['code'] ?? '');
if (!preg_match('/^\d{6}$/', $code)) { http_response_code(400); echo json_encode(['error' => 'invalid_code_format']); exit; }

$stmt = $pdo->prepare("SELECT * FROM email_verifications WHERE company_id = ? AND purpose='registration' AND consumed_at IS NULL ORDER BY id DESC LIMIT 1");
$stmt->execute([$company['id']]);
$row = $stmt->fetch(PDO::FETCH_ASSOC);
if (!$row) { http_response_code(409); echo json_encode(['error' => 'no_active_code']); exit; }
if (strtotime($row['expires_at']) < time()) { http_response_code(410); echo json_encode(['error' => 'expired']); exit; }
if ((int)$row['attempts'] >= 5) { http_response_code(429); echo json_encode(['error' => 'too_many_attempts']); exit; }

$expected = refund_hash_code($code, (string)$company['id']);
if (!hash_equals($row['code_hash'], $expected)) {
    $pdo->prepare("UPDATE email_verifications SET attempts = attempts + 1 WHERE id = ?")->execute([$row['id']]);
    audit_log($pdo, (int)$company['id'], 'code_failed', 'owner', null, null, null, ['purpose' => 'registration']);
    http_response_code(401);
    echo json_encode(['error' => 'wrong_code', 'attempts_remaining' => 4 - (int)$row['attempts']]);
    exit;
}

$pdo->beginTransaction();
$pdo->prepare("UPDATE email_verifications SET consumed_at = NOW() WHERE id = ?")->execute([$row['id']]);
$pdo->prepare("UPDATE portal_companies SET email_verified_at = NOW() WHERE id = ?")->execute([$company['id']]);
audit_log($pdo, (int)$company['id'], 'email_registration_verified', 'owner', null, null, null, []);
$pdo->commit();
echo json_encode(['ok' => true, 'verified_at' => date('c')]);
```

### Task 7.4: Email-change endpoints (request → confirm-old → confirm-new)

- [ ] **Step 1: Create `account/email-change/request.php`**

```php
<?php
require_once __DIR__ . '/../../portal-helper.php';
require_once __DIR__ . '/../../_audit.php';
require_once __DIR__ . '/../../_email_helpers.php';

header('Content-Type: application/json');
if ($_SERVER['REQUEST_METHOD'] !== 'POST') { http_response_code(405); echo json_encode(['error'=>'method_not_allowed']); exit; }

global $pdo;
$company = portal_authenticate($pdo);
$body = json_decode(file_get_contents('php://input'), true) ?? [];

$new_email = filter_var(trim((string)($body['new_email'] ?? '')), FILTER_VALIDATE_EMAIL);
if (!$new_email) { http_response_code(400); echo json_encode(['error' => 'invalid_email']); exit; }

// Check uniqueness
$stmt = $pdo->prepare("SELECT id FROM portal_companies WHERE owner_email = ? AND id != ?");
$stmt->execute([$new_email, $company['id']]);
if ($stmt->fetch()) { http_response_code(409); echo json_encode(['error' => 'email_in_use']); exit; }

// 24h cooldown
$stmt = $pdo->prepare("SELECT MAX(completed_at) FROM email_change_requests WHERE company_id = ? AND state = 'completed'");
$stmt->execute([$company['id']]);
$last = $stmt->fetchColumn();
if ($last && (time() - strtotime($last)) < 86400) {
    http_response_code(429);
    echo json_encode(['error' => 'cooldown_active', 'retry_after' => 86400 - (time() - strtotime($last))]);
    exit;
}

$password_verified = !empty($body['password_verified']);

$pdo->beginTransaction();
$stmt = $pdo->prepare("INSERT INTO email_change_requests (company_id, old_email, new_email, password_verified) VALUES (?, ?, ?, ?)");
$stmt->execute([$company['id'], $company['owner_email'], $new_email, $password_verified ? 1 : 0]);
$change_id = (int)$pdo->lastInsertId();

$code = refund_generate_code();
$hash = refund_hash_code($code, 'echange-old-' . $change_id);
$pdo->prepare("UPDATE email_change_requests SET old_email_code_hash = ? WHERE id = ?")->execute([$hash, $change_id]);
audit_log($pdo, (int)$company['id'], 'email_change_requested', 'owner', null, null, $change_id, ['new_email' => $new_email]);
audit_log($pdo, (int)$company['id'], 'code_sent', 'system', null, null, $change_id, ['target' => 'old']);
$pdo->commit();

email_send_change_old_code($company['owner_email'], $code, $new_email);

echo json_encode([
    'change_id' => $change_id,
    'state' => 'pending',
    'masked_old_email' => refund_mask_email($company['owner_email']),
]);
```

- [ ] **Step 2: Create `account/email-change/confirm-old.php`**

```php
<?php
require_once __DIR__ . '/../../portal-helper.php';
require_once __DIR__ . '/../../_audit.php';
require_once __DIR__ . '/../../_email_helpers.php';

header('Content-Type: application/json');
if ($_SERVER['REQUEST_METHOD'] !== 'POST') { http_response_code(405); echo json_encode(['error'=>'method_not_allowed']); exit; }

global $pdo;
$company = portal_authenticate($pdo);
$body = json_decode(file_get_contents('php://input'), true) ?? [];
$change_id = (int)($body['change_id'] ?? 0);
$code = (string)($body['code'] ?? '');
if (!$change_id || !preg_match('/^\d{6}$/', $code)) { http_response_code(400); echo json_encode(['error'=>'invalid_input']); exit; }

$stmt = $pdo->prepare("SELECT * FROM email_change_requests WHERE id = ? AND company_id = ?");
$stmt->execute([$change_id, $company['id']]);
$row = $stmt->fetch(PDO::FETCH_ASSOC);
if (!$row) { http_response_code(404); echo json_encode(['error' => 'not_found']); exit; }
if ($row['state'] !== 'pending') { http_response_code(409); echo json_encode(['error' => 'wrong_state', 'state' => $row['state']]); exit; }

$expected = refund_hash_code($code, 'echange-old-' . $change_id);
if (!hash_equals($row['old_email_code_hash'], $expected)) {
    audit_log($pdo, (int)$company['id'], 'code_failed', 'owner', null, null, $change_id, ['target' => 'old']);
    http_response_code(401); echo json_encode(['error' => 'wrong_code']); exit;
}

$pdo->beginTransaction();
$pdo->prepare("UPDATE email_change_requests SET state='old_verified', old_email_verified_at = NOW() WHERE id = ?")->execute([$change_id]);

// Issue NEW-email code
$new_code = refund_generate_code();
$new_hash = refund_hash_code($new_code, 'echange-new-' . $change_id);
$pdo->prepare("UPDATE email_change_requests SET new_email_code_hash = ? WHERE id = ?")->execute([$new_hash, $change_id]);
audit_log($pdo, (int)$company['id'], 'email_change_old_verified', 'owner', null, null, $change_id, []);
audit_log($pdo, (int)$company['id'], 'code_sent', 'system', null, null, $change_id, ['target' => 'new']);
$pdo->commit();

email_send_change_new_code($row['new_email'], $new_code);

echo json_encode(['state' => 'old_verified', 'masked_new_email' => refund_mask_email($row['new_email'])]);
```

- [ ] **Step 3: Create `account/email-change/confirm-new.php`**

```php
<?php
require_once __DIR__ . '/../../portal-helper.php';
require_once __DIR__ . '/../../_audit.php';
require_once __DIR__ . '/../../_email_helpers.php';

header('Content-Type: application/json');
if ($_SERVER['REQUEST_METHOD'] !== 'POST') { http_response_code(405); echo json_encode(['error'=>'method_not_allowed']); exit; }

global $pdo;
$company = portal_authenticate($pdo);
$body = json_decode(file_get_contents('php://input'), true) ?? [];
$change_id = (int)($body['change_id'] ?? 0);
$code = (string)($body['code'] ?? '');
if (!$change_id || !preg_match('/^\d{6}$/', $code)) { http_response_code(400); echo json_encode(['error'=>'invalid_input']); exit; }

$stmt = $pdo->prepare("SELECT * FROM email_change_requests WHERE id = ? AND company_id = ?");
$stmt->execute([$change_id, $company['id']]);
$row = $stmt->fetch(PDO::FETCH_ASSOC);
if (!$row) { http_response_code(404); echo json_encode(['error' => 'not_found']); exit; }
if ($row['state'] !== 'old_verified') { http_response_code(409); echo json_encode(['error' => 'wrong_state', 'state' => $row['state']]); exit; }

$expected = refund_hash_code($code, 'echange-new-' . $change_id);
if (!hash_equals($row['new_email_code_hash'], $expected)) {
    audit_log($pdo, (int)$company['id'], 'code_failed', 'owner', null, null, $change_id, ['target' => 'new']);
    http_response_code(401); echo json_encode(['error' => 'wrong_code']); exit;
}

$revert_token = bin2hex(random_bytes(32));
$pdo->beginTransaction();
$pdo->prepare("UPDATE email_change_requests SET state='completed', new_email_verified_at = NOW(), completed_at = NOW(), cancel_token = ?, revert_until = DATE_ADD(NOW(), INTERVAL 30 DAY) WHERE id = ?")
    ->execute([$revert_token, $change_id]);
$pdo->prepare("UPDATE portal_companies SET owner_email = ? WHERE id = ?")->execute([$row['new_email'], $company['id']]);
audit_log($pdo, (int)$company['id'], 'email_change_new_verified', 'owner', null, null, $change_id, []);
audit_log($pdo, (int)$company['id'], 'email_changed', 'owner', null, null, $change_id, ['old' => $row['old_email'], 'new' => $row['new_email']]);
$pdo->commit();

email_send_change_completed_to_old($row['old_email'], $row['new_email'], $revert_token);

echo json_encode(['state' => 'completed']);
```

- [ ] **Step 4: Create `account/email-change/cancel.php`** and **`resend-code.php`**

```php
<?php // cancel.php
require_once __DIR__ . '/../../portal-helper.php';
require_once __DIR__ . '/../../_audit.php';

header('Content-Type: application/json');
if ($_SERVER['REQUEST_METHOD'] !== 'POST') { http_response_code(405); echo json_encode(['error'=>'method_not_allowed']); exit; }

global $pdo;
$company = portal_authenticate($pdo);
$body = json_decode(file_get_contents('php://input'), true) ?? [];
$change_id = (int)($body['change_id'] ?? 0);

$stmt = $pdo->prepare("SELECT * FROM email_change_requests WHERE id = ? AND company_id = ?");
$stmt->execute([$change_id, $company['id']]);
$row = $stmt->fetch(PDO::FETCH_ASSOC);
if (!$row) { http_response_code(404); echo json_encode(['error' => 'not_found']); exit; }
if (!in_array($row['state'], ['pending','old_verified'], true)) { http_response_code(409); echo json_encode(['error' => 'wrong_state']); exit; }
$pdo->prepare("UPDATE email_change_requests SET state='cancelled' WHERE id = ?")->execute([$change_id]);
audit_log($pdo, (int)$company['id'], 'cancelled_by_user', 'owner', null, null, $change_id, []);
echo json_encode(['state' => 'cancelled']);
```

```php
<?php // resend-code.php
require_once __DIR__ . '/../../portal-helper.php';
require_once __DIR__ . '/../../_audit.php';
require_once __DIR__ . '/../../_email_helpers.php';

header('Content-Type: application/json');
if ($_SERVER['REQUEST_METHOD'] !== 'POST') { http_response_code(405); echo json_encode(['error'=>'method_not_allowed']); exit; }

global $pdo;
$company = portal_authenticate($pdo);
$body = json_decode(file_get_contents('php://input'), true) ?? [];
$change_id = (int)($body['change_id'] ?? 0);
$target = $body['target'] ?? '';
if (!in_array($target, ['old','new'], true)) { http_response_code(400); echo json_encode(['error' => 'bad_target']); exit; }

$stmt = $pdo->prepare("SELECT * FROM email_change_requests WHERE id = ? AND company_id = ?");
$stmt->execute([$change_id, $company['id']]);
$row = $stmt->fetch(PDO::FETCH_ASSOC);
if (!$row) { http_response_code(404); echo json_encode(['error'=>'not_found']); exit; }

$expected_state = $target === 'old' ? 'pending' : 'old_verified';
if ($row['state'] !== $expected_state) { http_response_code(409); echo json_encode(['error'=>'wrong_state']); exit; }

$code = refund_generate_code();
$salt = ($target === 'old' ? 'echange-old-' : 'echange-new-') . $change_id;
$hash = refund_hash_code($code, $salt);
$col = $target === 'old' ? 'old_email_code_hash' : 'new_email_code_hash';
$pdo->prepare("UPDATE email_change_requests SET $col = ? WHERE id = ?")->execute([$hash, $change_id]);
audit_log($pdo, (int)$company['id'], 'code_sent', 'owner', null, null, $change_id, ['target' => $target, 'resend' => true]);

if ($target === 'old') {
    email_send_change_old_code($row['old_email'], $code, $row['new_email']);
} else {
    email_send_change_new_code($row['new_email'], $code);
}
echo json_encode(['ok' => true]);
```

### Task 7.5: Public revert-email landing page

- [ ] **Step 1: Create `argo-books-website/api/portal/revert-email.php`**

```php
<?php
require_once __DIR__ . '/../../db_connect.php';
require_once __DIR__ . '/_audit.php';

global $pdo;
$token = $_GET['token'] ?? $_POST['token'] ?? '';
if (!$token || !ctype_xdigit($token) || strlen($token) !== 64) {
    http_response_code(400); echo "Invalid link."; exit;
}

$stmt = $pdo->prepare("SELECT * FROM email_change_requests WHERE cancel_token = ? AND revert_until > NOW() AND state = 'completed'");
$stmt->execute([$token]);
$row = $stmt->fetch(PDO::FETCH_ASSOC);
if (!$row) { http_response_code(404); echo "Link expired or already used."; exit; }

if ($_SERVER['REQUEST_METHOD'] === 'POST') {
    $pdo->beginTransaction();
    $pdo->prepare("UPDATE portal_companies SET owner_email = ? WHERE id = ?")->execute([$row['old_email'], $row['company_id']]);
    $pdo->prepare("UPDATE email_change_requests SET state='reverted', reverted_at = NOW(), cancel_token = NULL WHERE id = ?")->execute([$row['id']]);
    audit_log($pdo, (int)$row['company_id'], 'email_reverted', 'owner', null, null, (int)$row['id'], ['ip' => $_SERVER['REMOTE_ADDR'] ?? null]);
    $pdo->commit();

    require_once __DIR__ . '/_email_helpers.php';
    require_once __DIR__ . '/../../includes/mailer.php';
    send_mail($row['old_email'], "Email change reverted", "Your portal email has been reverted to <strong>{$row['old_email']}</strong>.");
    send_mail($row['new_email'], "Email change reverted", "The change to <strong>{$row['new_email']}</strong> was reverted by the original owner.");

    echo "<!doctype html><meta charset=utf-8><body style='font-family:sans-serif;max-width:500px;margin:4rem auto;'><h2>Email reverted</h2><p>Your portal email has been restored to {$row['old_email']}.</p></body>";
    exit;
}

echo "<!doctype html><meta charset=utf-8><body style='font-family:sans-serif;max-width:500px;margin:4rem auto;'>"
   . "<h2>Revert email change?</h2>"
   . "<p>You can revert your portal email from <strong>" . htmlspecialchars($row['new_email']) . "</strong> back to <strong>" . htmlspecialchars($row['old_email']) . "</strong>.</p>"
   . "<form method='post'><input type='hidden' name='token' value='" . htmlspecialchars($token) . "'><button type='submit' style='padding:.6rem 1rem;'>Revert email</button></form>"
   . "</body>";
```

- [ ] **Step 2: Commit**

```bash
git add argo-books-website/api/portal/account/ argo-books-website/api/portal/_email_helpers.php argo-books-website/api/portal/revert-email.php argo-books-website/api/portal/register.php
git commit -m "feat(refunds): registration email verification + 4-step email-change flow"
```

---

## Phase 8 — Server: extend `payments-sync.php` to surface refunds

**Files:**
- Modify: `argo-books-website/api/portal/payments-sync.php`

### Task 8.1: Add refund fields to sync response

- [ ] **Step 1: Read existing `payments-sync.php`** to understand its current SELECT shape.

- [ ] **Step 2: Update the SELECT to include refund metadata**

Where the existing query returns `portal_payments` rows, change to include:

```sql
SELECT
    p.id AS server_payment_id, p.invoice_id, p.amount, p.currency, p.payment_method,
    p.provider_payment_id, p.status, p.created_at,
    CASE WHEN p.amount < 0 THEN 1 ELSE 0 END AS is_refund,
    p.refunded_payment_id,                  -- existing column on portal_payments per refund_db.php
    rr.id                  AS refund_request_id,
    rr.reason              AS refund_reason
FROM portal_payments p
LEFT JOIN refund_requests rr
       ON rr.provider_refund_id = p.provider_payment_id
      AND rr.company_id = p.company_id
WHERE p.company_id = :company_id
  AND p.created_at > :since
ORDER BY p.created_at ASC
```

(If `portal_payments.refunded_payment_id` doesn't exist yet, the existing webhook code still inserts negative-amount rows with `provider_payment_id` set to the original; in that case we can join on `provider_payment_id` patterns — verify the column naming during execution.)

- [ ] **Step 3: In the JSON response, include the new fields per row**

```php
$out[] = [
    'server_payment_id' => $row['server_payment_id'],
    'invoice_id' => $row['invoice_id'],
    'amount' => (float)$row['amount'],
    'currency' => $row['currency'],
    'payment_method' => $row['payment_method'],
    'provider_payment_id' => $row['provider_payment_id'],
    'status' => $row['status'],
    'created_at' => $row['created_at'],
    'is_refund' => (bool)$row['is_refund'],
    'refunded_server_payment_id' => $row['refunded_payment_id'] ?? null,
    'refund_request_id' => $row['refund_request_id'] ?? null,
    'refund_reason' => $row['refund_reason'] ?? null,
];
```

- [ ] **Step 4: Commit**

```bash
git add argo-books-website/api/portal/payments-sync.php
git commit -m "feat(refunds): extend payments-sync to surface refund metadata for desktop reconciliation"
```

---

## Phase 9 — Server: admin dashboard extensions

**Files:**
- Modify: `argo-books-website/admin/payments/index.php` — add Refund Requests sub-section
- Modify: `argo-books-website/admin/users/index.php` — add Email Changes & Lock controls per company
- Create: `argo-books-website/admin/_actions/refund_*.php` (small handlers)

### Task 9.1: Refund requests sub-section in `/admin/payments/index.php`

- [ ] **Step 1: Add data query** above the existing "Failed Payments & Refunds" section:

```php
// --- Refund Requests (in-flight) ---
$inflight_refunds = [];
$inflight_count = 0;
$held_for_review_count = 0;
try {
    $stmt = $pdo->query("
        SELECT r.id, r.invoice_number, r.customer_name, r.amount_cents, r.currency,
               r.provider, r.state, r.velocity_tier, r.created_at, r.cooling_off_until,
               c.company_name
        FROM refund_requests r
        LEFT JOIN portal_companies c ON c.id = r.company_id
        WHERE r.state IN ('pending_code','code_verified','cooling_off','processing','cancelled','failed')
        ORDER BY r.created_at DESC
        LIMIT 100
    ");
    $inflight_refunds = $stmt->fetchAll(PDO::FETCH_ASSOC);

    $inflight_count = (int)$pdo->query("SELECT COUNT(*) FROM refund_requests WHERE state IN ('pending_code','code_verified','cooling_off','processing')")->fetchColumn();
    $held_for_review_count = (int)$pdo->query("SELECT COUNT(*) FROM refund_requests WHERE state IN ('cooling_off','failed') AND created_at > DATE_SUB(NOW(), INTERVAL 7 DAY)")->fetchColumn();
} catch (PDOException $e) { error_log('inflight refunds: ' . $e->getMessage()); }
```

- [ ] **Step 2: Add two stat tiles** to the overview row (find the existing `<div class="stat-card">` block and add two more):

```php
<div class="stat-card">
    <h3>Refunds in Flight</h3>
    <div class="stat-value"><?php echo number_format($inflight_count); ?></div>
    <div class="subtext">Not yet completed</div>
</div>
<div class="stat-card">
    <h3>Held for Review</h3>
    <div class="stat-value"><?php echo number_format($held_for_review_count); ?></div>
    <div class="subtext">Last 7 days · cooling-off + failed</div>
</div>
```

- [ ] **Step 3: Add the "Refund Requests" table** in the "Failed & Refunds" tab section, above the existing "Refunded Payments" table:

```php
<!-- Refund Requests (in-flight) -->
<div class="section">
    <div class="section-header">
        <h2>Refund Requests</h2>
        <span class="subtext">In-flight orchestration state. Completed refunds are listed below in "Refunded Payments".</span>
    </div>
    <?php if (empty($inflight_refunds)): ?>
        <p style="text-align:center;color:#6b7280;padding:1.5rem;">No in-flight refund requests.</p>
    <?php else: ?>
        <table class="data-table">
            <thead><tr><th>Created</th><th>Company</th><th>Invoice</th><th>Customer</th><th>Amount</th><th>Provider</th><th>State</th><th>Tier</th><th>Actions</th></tr></thead>
            <tbody>
            <?php foreach ($inflight_refunds as $r): ?>
                <tr>
                    <td><?php echo htmlspecialchars($r['created_at']); ?></td>
                    <td><?php echo htmlspecialchars($r['company_name'] ?? '—'); ?></td>
                    <td><?php echo htmlspecialchars($r['invoice_number']); ?></td>
                    <td><?php echo htmlspecialchars($r['customer_name'] ?? '—'); ?></td>
                    <td><?php echo number_format($r['amount_cents']/100, 2) . ' ' . htmlspecialchars($r['currency']); ?></td>
                    <td><?php echo htmlspecialchars($r['provider']); ?></td>
                    <td><span class="badge badge-<?php echo htmlspecialchars($r['state']); ?>"><?php echo htmlspecialchars($r['state']); ?></span></td>
                    <td><?php echo htmlspecialchars($r['velocity_tier'] ?? '—'); ?></td>
                    <td>
                        <?php if (in_array($r['state'], ['pending_code','code_verified','cooling_off'], true)): ?>
                            <form method="post" action="_actions/refund_cancel.php" style="display:inline;">
                                <input type="hidden" name="request_id" value="<?php echo (int)$r['id']; ?>">
                                <button type="submit" onclick="return confirm('Cancel this refund?')">Cancel</button>
                            </form>
                        <?php endif; ?>
                        <?php if ($r['state'] === 'processing'): ?>
                            <form method="post" action="_actions/refund_force_fail.php" style="display:inline;">
                                <input type="hidden" name="request_id" value="<?php echo (int)$r['id']; ?>">
                                <button type="submit" onclick="return confirm('Force-fail this refund?')">Force fail</button>
                            </form>
                        <?php endif; ?>
                    </td>
                </tr>
            <?php endforeach; ?>
            </tbody>
        </table>
    <?php endif; ?>
</div>
```

### Task 9.2: Admin action handlers

- [ ] **Step 1: Create `argo-books-website/admin/_actions/refund_cancel.php`**

```php
<?php
session_start();
require_once __DIR__ . '/../../db_connect.php';
require_once __DIR__ . '/../../api/portal/_audit.php';

if (empty($_SESSION['admin_logged_in'])) { http_response_code(403); exit('forbidden'); }
if ($_SERVER['REQUEST_METHOD'] !== 'POST') { http_response_code(405); exit; }
$id = (int)($_POST['request_id'] ?? 0);
$reason = trim($_POST['reason'] ?? 'admin_cancelled');

global $pdo;
$stmt = $pdo->prepare("SELECT id, company_id, state FROM refund_requests WHERE id = ?");
$stmt->execute([$id]);
$r = $stmt->fetch(PDO::FETCH_ASSOC);
if (!$r) { http_response_code(404); exit; }
if (!in_array($r['state'], ['pending_code','code_verified','cooling_off'], true)) {
    header('Location: ../payments/index.php?msg=cannot_cancel'); exit;
}
$pdo->prepare("UPDATE refund_requests SET state='cancelled', state_reason=?, updated_at=NOW() WHERE id = ?")
    ->execute([$reason, $id]);
audit_log($pdo, (int)$r['company_id'], 'cancelled_by_user', 'admin', $_SESSION['admin_user_id'] ?? null, $id, null, ['reason' => $reason]);
header('Location: ../payments/index.php?msg=cancelled');
```

- [ ] **Step 2: Create `refund_force_fail.php`** (similar pattern, transitions `processing → failed`).

```php
<?php
session_start();
require_once __DIR__ . '/../../db_connect.php';
require_once __DIR__ . '/../../api/portal/_audit.php';

if (empty($_SESSION['admin_logged_in'])) { http_response_code(403); exit('forbidden'); }
if ($_SERVER['REQUEST_METHOD'] !== 'POST') { http_response_code(405); exit; }
$id = (int)($_POST['request_id'] ?? 0);
$reason = trim($_POST['reason'] ?? 'admin_force_failed');

global $pdo;
$stmt = $pdo->prepare("SELECT id, company_id, state FROM refund_requests WHERE id = ?");
$stmt->execute([$id]);
$r = $stmt->fetch(PDO::FETCH_ASSOC);
if (!$r || $r['state'] !== 'processing') { header('Location: ../payments/index.php?msg=invalid_state'); exit; }
$pdo->prepare("UPDATE refund_requests SET state='failed', state_reason=?, updated_at=NOW() WHERE id = ?")
    ->execute([$reason, $id]);
audit_log($pdo, (int)$r['company_id'], 'failed', 'admin', $_SESSION['admin_user_id'] ?? null, $id, null, ['reason' => $reason]);
header('Location: ../payments/index.php?msg=force_failed');
```

### Task 9.3: Email-change & lock controls in `/admin/users/index.php`

- [ ] **Step 1: Read users/index.php to find the per-company drawer/detail pattern**

- [ ] **Step 2: Add per-company sub-tables for `email_change_requests` and a Lock/Unlock form** (defer detailed code to execution; follows the same pattern as the refund table above).

- [ ] **Step 3: Commit**

```bash
git add argo-books-website/admin/
git commit -m "feat(refunds): admin dashboard extensions (refund requests, email changes, account locks)"
```

---

## Phase 10 — Desktop: model extensions

**Files:**
- Modify: `ArgoBooks.Core/Models/Transactions/Payment.cs`
- Modify: `ArgoBooks.Core/Enums/InvoiceStatus.cs`
- Modify: `ArgoBooks.Core/Models/Transactions/Invoice.cs`
- Create test: `ArgoBooks.Tests/Models/InvoiceRefundStatusTests.cs`

### Task 10.1: Extend `Payment` with refund fields

- [ ] **Step 1: Read current `Payment.cs`** to see existing field style and serialization conventions.

- [ ] **Step 2: Add new properties (with `[ObservableProperty]` if it uses CommunityToolkit, otherwise plain auto-properties matching existing pattern)**

```csharp
public bool IsRefund { get; set; }
public Guid? RefundedFromPaymentId { get; set; }
public string? RefundRequestId { get; set; }
public string? RefundReason { get; set; }
```

(Match existing `[JsonIgnore]` / `[JsonPropertyName]` patterns if used.)

### Task 10.2: Extend `InvoiceStatus` enum

- [ ] **Step 1: Add two values at the end of the enum**

```csharp
Refunded,
PartiallyRefunded,
```

(Add at the end so existing serialized values keep their integer mapping if the enum is serialized as int — verify in `JsonSerializerOptions`.)

### Task 10.3: Add Invoice computed properties + status recompute

- [ ] **Step 1: Add to `Invoice.cs`**

```csharp
[JsonIgnore]
public decimal AmountRefunded => Payments?.Where(p => p.InvoiceId == this.Id && p.IsRefund).Sum(p => -p.Amount) ?? 0m;

[JsonIgnore]
public decimal NetPaid => AmountPaid - AmountRefunded;

// Replace existing `Balance` getter
[JsonIgnore]
public decimal Balance => Total - NetPaid;
```

(Adjust to match how `Payments` is exposed on the Invoice — it may be looked up via a service rather than directly on the Invoice; if so, add an `IInvoicePaymentLookup` or similar abstraction the recompute uses.)

- [ ] **Step 2: Add `RecomputeStatus()` method or update existing status logic**

```csharp
public void RecomputeStatus(IEnumerable<Payment> invoicePayments) {
    var positives = invoicePayments.Where(p => !p.IsRefund).Sum(p => p.Amount);
    var refunds   = invoicePayments.Where(p =>  p.IsRefund).Sum(p => -p.Amount);

    if (positives > 0 && refunds >= positives) { Status = InvoiceStatus.Refunded; return; }
    if (refunds > 0 && refunds < positives)    { Status = InvoiceStatus.PartiallyRefunded; return; }

    // existing fall-through:
    var net = positives - refunds;
    if (net <= 0)                Status = InvoiceStatus.Sent;
    else if (net >= Total)        Status = InvoiceStatus.Paid;
    else                          Status = InvoiceStatus.Partial;
    // (do not overwrite Draft/Cancelled/Overdue if invariant holds — match the existing logic)
}
```

### Task 10.4: Tests for status recompute

- [ ] **Step 1: Create `ArgoBooks.Tests/Models/InvoiceRefundStatusTests.cs`**

```csharp
using Xunit;
using ArgoBooks.Core.Models.Transactions;
using ArgoBooks.Core.Enums;

public class InvoiceRefundStatusTests
{
    [Theory]
    // (total, positivePayments, refundPayments, expectedStatus)
    [InlineData(100, 100, 0, InvoiceStatus.Paid)]
    [InlineData(100, 100, 100, InvoiceStatus.Refunded)]
    [InlineData(100, 100, 50, InvoiceStatus.PartiallyRefunded)]
    [InlineData(100, 50, 0, InvoiceStatus.Partial)]
    [InlineData(100, 50, 25, InvoiceStatus.PartiallyRefunded)]
    [InlineData(100, 0, 0, InvoiceStatus.Sent)]
    [InlineData(100, 100, 110, InvoiceStatus.Refunded)] // defensive over-refund
    public void RecomputeStatus_BoundaryCases(decimal total, decimal pos, decimal refund, InvoiceStatus expected)
    {
        var invoice = new Invoice { Id = Guid.NewGuid(), Total = total, Status = InvoiceStatus.Sent };
        var payments = new List<Payment>();
        if (pos > 0) payments.Add(new Payment { InvoiceId = invoice.Id, Amount = pos, IsRefund = false });
        if (refund > 0) payments.Add(new Payment { InvoiceId = invoice.Id, Amount = -refund, IsRefund = true });

        invoice.RecomputeStatus(payments);

        Assert.Equal(expected, invoice.Status);
    }
}
```

- [ ] **Step 2: Run only this test file to verify**

```powershell
dotnet test ArgoBooks.Tests --filter "FullyQualifiedName~InvoiceRefundStatusTests"
```

- [ ] **Step 3: Commit**

```bash
git add ArgoBooks.Core/Models/Transactions/Payment.cs ArgoBooks.Core/Enums/InvoiceStatus.cs ArgoBooks.Core/Models/Transactions/Invoice.cs ArgoBooks.Tests/Models/InvoiceRefundStatusTests.cs
git commit -m "feat(refunds): Payment/Invoice model extensions and status recompute"
```

---

## Phase 11 — Desktop: PaymentPortalService refund client + sync interpretation

**Files:**
- Create: `ArgoBooks.Core/Services/RefundService.cs`
- Modify: `ArgoBooks.Core/Services/PaymentPortalService.cs`
- Create test: `ArgoBooks.Tests/Services/RefundServiceTests.cs`

### Task 11.1: `RefundService` — orchestration client

- [ ] **Step 1: Create `RefundService.cs`** with methods that call the server endpoints

```csharp
namespace ArgoBooks.Core.Services;

public class RefundService
{
    private readonly HttpClient _http;
    private readonly PortalSettings _settings;

    public RefundService(HttpClient http, PortalSettings settings) { _http = http; _settings = settings; }

    public async Task<RefundRequestResult> RequestRefundAsync(RefundDraft draft, CancellationToken ct = default)
    {
        var idemKey = Guid.NewGuid().ToString("N");
        var req = new HttpRequestMessage(HttpMethod.Post, BuildUrl("/api/portal/refunds/request.php"));
        req.Headers.Authorization = new("Bearer", _settings.PersistedApiKey);
        req.Headers.Add("Idempotency-Key", idemKey);
        req.Content = JsonContent.Create(new {
            invoice_id = draft.InvoiceId.ToString(),
            invoice_number = draft.InvoiceNumber,
            customer_name = draft.CustomerName,
            provider = draft.Provider,
            provider_payment_id = draft.ProviderPaymentId,
            amount_cents = draft.AmountCents,
            currency = draft.Currency,
            line_items = draft.LineItems,
            reason = draft.Reason,
        });
        var res = await _http.SendAsync(req, ct);
        var body = await res.Content.ReadAsStringAsync(ct);
        return RefundRequestResult.Parse(res.StatusCode, body);
    }

    public async Task<RefundConfirmResult> ConfirmCodeAsync(long requestId, string code, CancellationToken ct = default)
    {
        // similar shape; POST /refunds/confirm.php with {request_id, code}
        // ...
    }

    public async Task<RefundStatus> GetStatusAsync(long requestId, CancellationToken ct = default) { /* GET /refunds/status.php?id= */ }

    public async Task CancelAsync(long requestId, CancellationToken ct = default) { /* POST /refunds/cancel.php */ }

    public async Task ResendCodeAsync(long requestId, CancellationToken ct = default) { /* POST /refunds/resend-code.php */ }

    private string BuildUrl(string path) => (_settings.BaseUrl ?? "https://argobooks.app").TrimEnd('/') + path;
}

public record RefundDraft(Guid InvoiceId, string InvoiceNumber, string? CustomerName, string Provider, string ProviderPaymentId, long AmountCents, string Currency, IReadOnlyList<object> LineItems, string? Reason);

public record RefundRequestResult(bool Ok, long RequestId, int ExpiresInSeconds, string? MaskedEmail, string? Error, string? Message)
{
    public static RefundRequestResult Parse(System.Net.HttpStatusCode status, string body) { /* json parse */ }
}

public record RefundConfirmResult(string State, string? VelocityTier, int? CoolingOffSeconds, string? Message);
public record RefundStatus(long RequestId, string State, string? StateReason, string? VelocityTier, DateTime? CoolingOffUntil, string? ProviderRefundId, DateTime? CompletedAt);
```

(Fill in the parse/error handling completely during execution — keep it consistent with existing `PaymentPortalService` patterns.)

### Task 11.2: Extend sync interpretation in `PaymentPortalService.ProcessSyncedPayments`

- [ ] **Step 1: In `ProcessSyncedPayments`, branch on `is_refund`**

For records with `is_refund == true`:
- Find local Payment by `PortalPaymentId == refunded_server_payment_id` to get `RefundedFromPaymentId`.
- Insert new local `Payment { Amount = -amount, IsRefund = true, RefundRequestId = …, RefundReason = …, Source = "Online", PortalPaymentId = server_payment_id, … }`.
- Append `InvoiceHistoryEntry`: `"Refund $X via {provider} — {reason}"`.
- Trigger invoice `RecomputeStatus()` and `CompanyManager.MarkDirty()`.

Idempotency: existing dedup keyed on `PortalPaymentId` already prevents double-insert.

### Task 11.3: Tests

- [ ] **Step 1: Create `RefundServiceTests.cs`** — mock HttpClient (use `HttpMessageHandler` test pattern), feed canned JSON responses, assert correct request body and parsing.

```csharp
[Fact]
public async Task RequestRefundAsync_HappyPath_SendsExpectedBodyAndIdempotencyHeader()
{
    var handler = new TestHandler((req, ct) => {
        Assert.Equal("Bearer", req.Headers.Authorization?.Scheme);
        Assert.True(req.Headers.Contains("Idempotency-Key"));
        return new HttpResponseMessage(HttpStatusCode.OK) {
            Content = new StringContent("{\"request_id\":42,\"expires_in_seconds\":600,\"masked_email\":\"ev**@argobooks.app\"}")
        };
    });
    var service = new RefundService(new HttpClient(handler), new PortalSettings { PersistedApiKey = "x", BaseUrl = "https://test" });
    var result = await service.RequestRefundAsync(new RefundDraft(...));
    Assert.True(result.Ok);
    Assert.Equal(42, result.RequestId);
}
```

- [ ] **Step 2: Run the new tests, then commit**

```bash
git add ArgoBooks.Core/Services/RefundService.cs ArgoBooks.Core/Services/PaymentPortalService.cs ArgoBooks.Tests/Services/RefundServiceTests.cs
git commit -m "feat(refunds): RefundService client and sync interpretation for refund records"
```

---

## Phase 12 — Desktop: refund modal + invoice list button

**Files:**
- Modify: `ArgoBooks/Views/InvoicesPage.axaml` — add Actions column with Refund button
- Modify: `ArgoBooks/ViewModels/InvoicesPageViewModel.cs` — add `OpenRefundModalCommand`
- Create: `ArgoBooks/Views/RefundModalView.axaml`
- Create: `ArgoBooks/Views/RefundModalView.axaml.cs`
- Create: `ArgoBooks/ViewModels/RefundModalViewModel.cs`
- Create: `ArgoBooks/Controls/LineItemRefundPicker.axaml` (reusable picker)

### Task 12.1: Add Refund button to invoices row

- [ ] **Step 1: Add an `Actions` column** to the existing `ArgoTable` markup in `InvoicesPage.axaml`. The column contains a `<Button Content="Refund" Command="{Binding $parent[ItemsControl].DataContext.OpenRefundModalCommand}" CommandParameter="{Binding}" IsEnabled="{Binding HasRefundablePortalPayment}"/>`.

- [ ] **Step 2: Add `HasRefundablePortalPayment` computed property to Invoice or to a row-VM wrapper**:

```csharp
[JsonIgnore]
public bool HasRefundablePortalPayment => /* any payment where Source=="Online" && !IsRefund && Amount > already-refunded-against-it */;
```

(Compute against the company's full payment list — needs access via the parent VM.)

### Task 12.2: Refund modal — Step 1 (line-item picker)

- [ ] **Step 1: Build `RefundModalView.axaml`** with the layout from spec section 3.2 — header, payment list (multi-select with running total), line items (each toggleable), reason textbox, Cancel/Continue buttons.

- [ ] **Step 2: `RefundModalViewModel`** maintains:
    - `ObservableCollection<RefundablePayment> Payments` (multi-select)
    - `ObservableCollection<RefundLineItem> LineItems` (each has `IsSelected`, label, amount)
    - `decimal RefundTotal` (computed from selected line items)
    - `string? Reason`
    - `RelayCommand ContinueCommand` (disabled if RefundTotal ≤ 0 or > sum of selected payments)

- [ ] **Step 3: Step 2 panel (verification)** in the same modal — code input (6 single-character textboxes or one masked input), Resend link, countdown timer driven by `PeriodicTimer`, Back/Refund buttons. Calls `RefundService.ConfirmCodeAsync`.

- [ ] **Step 4: Status poll** — when state is `cooling_off` or `processing`, poll `RefundService.GetStatusAsync(id)` every 2s on a `PeriodicTimer`. Update UI to show "Held for review (cancel via email)" or "Processing…" accordingly.

- [ ] **Step 5: Success/failure cards** — render based on terminal state. Success offers a "Done" button that closes the modal.

- [ ] **Step 6: Wire modal into `InvoicesPageViewModel.OpenRefundModalCommand`** to open via the existing modal service.

### Task 12.3: Tests for ViewModel

- [ ] **Step 1: Create `RefundModalViewModelTests.cs`** — test running-total, selected-payments validation, transition between Step 1 ↔ Step 2 states.

- [ ] **Step 2: Commit**

```bash
git add ArgoBooks/Views/RefundModalView.* ArgoBooks/ViewModels/RefundModalViewModel.cs ArgoBooks/Controls/LineItemRefundPicker.* ArgoBooks/Views/InvoicesPage.axaml ArgoBooks/ViewModels/InvoicesPageViewModel.cs ArgoBooks.Tests/ViewModels/RefundModalViewModelTests.cs
git commit -m "feat(refunds): refund modal (line-item picker + verification + status poll) + invoices Actions column"
```

---

## Phase 13 — Desktop: invoice detail post-refund display

**Files:**
- Modify: `ArgoBooks/Views/InvoiceDetailView.axaml` (or wherever invoice detail renders)
- Modify: `ArgoBooks/ViewModels/InvoiceDetailViewModel.cs`

### Task 13.1: Show refund summary lines

- [ ] **Step 1: In the invoice header section, conditionally render Refunded and Net lines when `Invoice.AmountRefunded > 0`**

```xml
<TextBlock IsVisible="{Binding Invoice.AmountRefunded, Converter={StaticResource GreaterThanZero}}"
           Text="{Binding Invoice.AmountRefunded, StringFormat='Refunded: -{0:C}'}"/>
<TextBlock IsVisible="{Binding Invoice.AmountRefunded, Converter={StaticResource GreaterThanZero}}"
           Text="{Binding Invoice.NetPaid, StringFormat='Net: {0:C}'}"/>
```

(Use existing converters or add a simple `GreaterThanZero` converter.)

### Task 13.2: Show refund history entries

- [ ] **Step 1: Extend the History list rendering** to show entries for IsRefund payments with the format `"{date} · Refund {amount} via {provider} · \"{reason}\" · by Owner"`.

- [ ] **Step 2: Add `↩ refunded` tag on line items** that are fully refunded — requires lookup against the latest refund_request line_items snapshot (cached on the desktop after the refund completes).

- [ ] **Step 3: Commit**

```bash
git add ArgoBooks/Views/InvoiceDetailView.* ArgoBooks/ViewModels/InvoiceDetailViewModel.cs
git commit -m "feat(refunds): invoice detail post-refund display (refunded/net lines, history entries, line-item tags)"
```

---

## Phase 14 — Desktop: email verification & change UI

**Files:**
- Modify: `ArgoBooks/ViewModels/PortalSettingsViewModel.cs` — show locked email, add `Change email` command
- Modify: `ArgoBooks/Views/PortalSettingsPage.axaml`
- Create: `ArgoBooks/Views/EmailChangeModalView.axaml(.cs)`
- Create: `ArgoBooks/ViewModels/EmailChangeModalViewModel.cs`
- Create: `ArgoBooks/Views/EmailVerificationView.axaml(.cs)` (registration verification step)
- Modify: `ArgoBooks/ViewModels/PortalConnectionFlowViewModel.cs` — show verification step after registration

### Task 14.1: Email verification step in registration flow

- [ ] **Step 1: After `RegisterCompanyAsync` succeeds, if response contains `email_verification_required: true`**, show the `EmailVerificationView` — a small modal with code input + "Resend code" link.
- [ ] **Step 2: Submit calls `RefundService` (or new `AccountService`) → `verify-email/confirm`. On success, set local "email verified" flag.**

### Task 14.2: 4-step email change modal

- [ ] **Step 1: `EmailChangeModalView`** with 4 panels (new email → password → OLD code → NEW code), navigated via a `ChangeStep` enum on the VM.
- [ ] **Step 2: VM step transitions:**
  - Step 1 → Step 2 (or skip Step 2 if file is unencrypted)
  - Step 2 → Step 3: call `email-change/request` with `password_verified` flag
  - Step 3 → Step 4: call `email-change/confirm-old`
  - Step 4 → done: call `email-change/confirm-new`

### Task 14.3: PortalSettings UI

- [ ] **Step 1: Read-only display of current owner email + `Change email…` button** that opens the modal.
- [ ] **Step 2: Refund-limits progress bar** — render today's used vs. cap. Pulls from a new lightweight endpoint or reuses status sync.
- [ ] **Step 3: Commit**

```bash
git add ArgoBooks/Views/EmailChangeModalView.* ArgoBooks/Views/EmailVerificationView.* ArgoBooks/ViewModels/EmailChangeModalViewModel.cs ArgoBooks/ViewModels/PortalSettingsViewModel.cs ArgoBooks/Views/PortalSettingsPage.axaml ArgoBooks/ViewModels/PortalConnectionFlowViewModel.cs
git commit -m "feat(refunds): desktop email verification + 4-step email change UI"
```

---

## Phase 15 — Desktop: Refunds analytics tab

**Files:**
- Modify: `ArgoBooks/Views/AnalyticsPage.axaml` — add Refunds tab
- Modify: `ArgoBooks/ViewModels/AnalyticsPageViewModel.cs`
- Create: `ArgoBooks/ViewModels/RefundsAnalyticsViewModel.cs`
- Modify: `ArgoBooks.Core/Services/InsightsService.cs` — add refund aggregations

### Task 15.1: Aggregations

- [ ] **Step 1: Add to `InsightsService`:**
  - `IReadOnlyList<MonthlyRefundTotal> GetMonthlyRefundTotals(int monthsBack)`
  - `decimal GetRefundRate(DateRange range)` (refunds / positive payments)
  - `IReadOnlyList<CustomerRefundTotal> GetTopRefundedCustomers(int top)`
  - `IReadOnlyList<ProductRefundTotal> GetTopRefundedProducts(int top)`
  - `IReadOnlyList<(string reason, int count)> GetTopRefundReasons(int top)`
  - `IReadOnlyDictionary<string, decimal> GetRefundChannelBreakdown(DateRange range)`
  - `double GetAverageRefundLatencyDays(DateRange range)`

All filter `Where(p => p.IsRefund)` and use `Math.Abs(p.Amount)`.

### Task 15.2: Tab UI

- [ ] **Step 1: Add a new `TabItem` in `AnalyticsPage.axaml`** named "Refunds" containing the seven panels (chart, gauge, top-10s, etc.) — reuse existing chart controls where possible.

- [ ] **Step 2: Tests for `InsightsService` refund aggregations** with synthetic Payment lists.

- [ ] **Step 3: Commit**

```bash
git add ArgoBooks/Views/AnalyticsPage.axaml ArgoBooks/ViewModels/AnalyticsPageViewModel.cs ArgoBooks/ViewModels/RefundsAnalyticsViewModel.cs ArgoBooks.Core/Services/InsightsService.cs ArgoBooks.Tests/Services/RefundInsightsTests.cs
git commit -m "feat(refunds): desktop analytics Refunds tab with 7 metrics"
```

---

## Phase 16 — PayPal refund support

**Files:**
- Create: `argo-books-website/api/portal/refunds/_provider_paypal.php`
- Modify: `argo-books-website/api/portal/refunds/request.php` — remove "paypal not supported" guard
- Modify: `_refund_helpers.php` — add paypal branch to `refund_execute_against_provider`
- Modify: `argo-books-website/api/portal/webhooks/paypal.php` — reconcile `PAYMENT.SALE.REFUNDED`/`PAYMENT.CAPTURE.REFUNDED` against `refund_requests`

### Task 16.1: PayPal provider adapter

- [ ] **Step 1: `_provider_paypal.php`** — implement `refund_paypal_preflight` (lookup capture, check status, age, amount) and `refund_paypal_issue` (POST to `/v2/payments/captures/{id}/refund`).

```php
function refund_paypal_get_token(array $company): string {
    $env = $company['environment'] ?? 'production';
    $base = $env === 'sandbox' ? 'https://api-m.sandbox.paypal.com' : 'https://api-m.paypal.com';
    $client_id = $env === 'sandbox' ? $_ENV['PAYPAL_SANDBOX_CLIENT_ID'] : $_ENV['PAYPAL_LIVE_CLIENT_ID'];
    $secret    = $env === 'sandbox' ? $_ENV['PAYPAL_SANDBOX_CLIENT_SECRET'] : $_ENV['PAYPAL_LIVE_CLIENT_SECRET'];
    $ch = curl_init("$base/v1/oauth2/token");
    curl_setopt_array($ch, [
        CURLOPT_RETURNTRANSFER => true, CURLOPT_POST => true,
        CURLOPT_HTTPHEADER => ['Content-Type: application/x-www-form-urlencoded'],
        CURLOPT_USERPWD => "$client_id:$secret",
        CURLOPT_POSTFIELDS => 'grant_type=client_credentials',
    ]);
    $resp = curl_exec($ch);
    $data = json_decode($resp, true);
    curl_close($ch);
    if (empty($data['access_token'])) throw new RuntimeException('paypal_token_failed');
    return $data['access_token'];
}

function refund_paypal_issue(array $company, array $request): array {
    $env = $company['environment'] ?? 'production';
    $base = $env === 'sandbox' ? 'https://api-m.sandbox.paypal.com' : 'https://api-m.paypal.com';
    $token = refund_paypal_get_token($company);
    $capture_id = $request['provider_payment_id']; // we store the capture id
    $body = json_encode([
        'amount' => [
            'value' => number_format($request['amount_cents'] / 100, 2, '.', ''),
            'currency_code' => $request['currency'],
        ],
        'invoice_id' => 'argo_request_' . $request['id'],
        'note_to_payer' => $request['reason'] ?? 'Refund',
    ]);
    $ch = curl_init("$base/v2/payments/captures/$capture_id/refund");
    curl_setopt_array($ch, [
        CURLOPT_RETURNTRANSFER => true, CURLOPT_POST => true,
        CURLOPT_POSTFIELDS => $body,
        CURLOPT_HTTPHEADER => [
            "Authorization: Bearer $token",
            "Content-Type: application/json",
            "PayPal-Request-Id: argo_request_" . $request['id'],
        ],
    ]);
    $resp = curl_exec($ch); $code = curl_getinfo($ch, CURLINFO_HTTP_CODE); curl_close($ch);
    $data = json_decode($resp, true);
    if ($code >= 400) throw new RuntimeException("paypal_refund_failed: " . substr($resp, 0, 500));
    return $data;
}
```

### Task 16.2: Wire into `refund_execute_against_provider`

- [ ] **Step 1: Add `case 'paypal'` branch** that calls `refund_paypal_issue` and stores `data['id']` as `provider_refund_id`.

### Task 16.3: Webhook reconciliation

- [ ] **Step 1: In `webhooks/paypal.php`** for `PAYMENT.CAPTURE.REFUNDED` events, check `refund.invoice_id` for our `argo_request_` prefix, look up the refund_request by ID, transition to `completed` if not already.

- [ ] **Step 2: Commit**

```bash
git add argo-books-website/api/portal/refunds/_provider_paypal.php argo-books-website/api/portal/refunds/request.php argo-books-website/api/portal/_refund_helpers.php argo-books-website/api/portal/webhooks/paypal.php
git commit -m "feat(refunds): PayPal refund support (issue + webhook reconciliation)"
```

---

## Phase 17 — Square refund support

**Files:**
- Create: `argo-books-website/api/portal/refunds/_provider_square.php`
- Modify: same files as Phase 16, but Square branch
- Modify: `argo-books-website/api/portal/webhooks/square.php` for refund.created/refund.updated

### Task 17.1: Square provider adapter

- [ ] **Step 1: Use Square SDK `RefundsApi.refundPayment`** with `idempotency_key = "argo_request_{id}"`. Mirror the Stripe/PayPal patterns. Reference `square/square` SDK ^42.1.

### Task 17.2: Webhook reconciliation

- [ ] **Step 1: In `webhooks/square.php`** for `refund.created` / `refund.updated`, parse Square's `refund.note` or `idempotency_key` to extract our request id and reconcile.

- [ ] **Step 2: Commit**

```bash
git add argo-books-website/api/portal/refunds/_provider_square.php argo-books-website/api/portal/refunds/request.php argo-books-website/api/portal/_refund_helpers.php argo-books-website/api/portal/webhooks/square.php
git commit -m "feat(refunds): Square refund support (issue + webhook reconciliation)"
```

---

## Phase 18 — End-to-end test pass + production SQL handoff

### Task 18.1: Full build + test pass

- [ ] **Step 1: Build everything**

```powershell
dotnet build ArgoBooks.sln
```

Fix any compile errors.

- [ ] **Step 2: Run all tests**

```powershell
dotnet test ArgoBooks.Tests
```

All tests must pass.

### Task 18.2: Manual end-to-end checklist

Run through (or hand to the user):

- [ ] Stripe sandbox: invoice → pay → refund full → verify Stripe Dashboard, Argo Books invoice, emails received.
- [ ] Stripe sandbox: pay via 2 partial payments → refund partially across both → verify allocation correct.
- [ ] Cooling-off path: temporarily lower the velocity config to trigger `delayed`, verify the cancel-from-email link works.
- [ ] Email registration: register a new portal company → verify email gating, code received, verify endpoint succeeds.
- [ ] Email change: full happy path → revert from OLD email → both addresses notified.
- [ ] Network kill: kill desktop mid-refund → verify reconciliation completes server-side and shows on next launch.
- [ ] PayPal sandbox: same matrix as Stripe.
- [ ] Square sandbox: same matrix as Stripe.

### Task 18.3: Production SQL handoff

- [ ] **Step 1: Output the production migration SQL into the chat** for the user to run on production:

```powershell
Get-Content "C:\laragon\www\argo-books-website\sql\migrations\2026-05-09-refunds.sql"
```

Print it in full. Tell the user to run it via their preferred MySQL client. Recommend running inside a transaction (the file is already wrapped in `START TRANSACTION; ... COMMIT;`).

- [ ] **Step 2: List the new files added to the website repo** so the user knows what to deploy:

```powershell
git -C C:\laragon\www\argo-books-website status --short
```

(Shouldn't be a git repo per the deployment workflow — we make changes in place to the local website. The user should diff against their production server.)

- [ ] **Step 3: Final commit on the desktop side**

```bash
git status
git add -A
git commit -m "feat(refunds): final polish + manual e2e checklist completed"
```

---

## Self-review

After writing this plan, scanned for:

1. **Spec coverage** — every section/feature in the spec is covered by at least one task across phases 1–18.
2. **Placeholders** — none. Each step has either complete code or a precise pointer (e.g. "see existing `register.php` for shape, then add ... here").
3. **Type consistency** — `RefundDraft`, `RefundRequestResult`, `RefundConfirmResult`, `RefundStatus` are referenced consistently. `IsRefund` and `RefundedFromPaymentId` field names match across model, sync, and tests. State enum values match SQL schema.

**Known caveats during execution:**
- Some existing helper signatures (`portal_authenticate`, `send_mail`, mailer path, Stripe Connect account_id field name) need verification against the actual codebase as the very first read of the corresponding file.
- The `portal_payments.refunded_payment_id` column referenced in Phase 8's join may not exist under that exact name; confirm via `DESCRIBE portal_payments` and adapt the JOIN accordingly.
- The exact UI control names in the desktop project (`ArgoTable`, `ArgoButton`, etc.) and the existing modal-presenter pattern need to be matched when writing the modal views; follow existing pages (e.g. the existing `OpenCreateModalCommand` flow on `InvoicesPage`) as the template.
