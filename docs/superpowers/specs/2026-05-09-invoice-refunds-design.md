# Invoice Refunds — Design

**Status:** Draft for review
**Date:** 2026-05-09
**Author:** Evan + Claude (brainstorm)
**Codebases:** `Argo-Books-Avalonia` (desktop) + `argo-books-website` (PHP/MySQL)

---

## 1. Goals & non-goals

**Goal:** Let a company owner refund a paid or partially-paid invoice from inside Argo Books. Money returns to the customer's original payment method via Stripe, PayPal, or Square. Refunds support full, partial-by-line-item, or custom-amount. The flow is fast and friendly for legitimate users, but materially harder for someone who has wrongful access to a `.argo` file to weaponize.

**Non-goals (v1):**
- Multi-user roles inside a company (still single-user; planned later).
- In-app account-locking UI (server-side flag managed from the admin dashboard only).
- Pre-authorize-higher-cap email link (defer to v1.1 if a real customer needs it).
- Mobile push approval.
- Refunds for non-portal payments (cash, check, bank transfer). No refund rails exist for these; the money was never moved by Argo Books, so there's nothing to refund. Bookkeeping-only manual refund recording is also out of scope for v1 — the user can edit/delete a payment record directly if they need to reflect a cash refund in their books.

---

## 2. Architecture

```
                                        ┌─────────────────────────┐
                                        │   Stripe / PayPal /     │
                                        │   Square (external)     │
                                        └────────▲────────────┬───┘
                                                 │            │
                                          API call│            │webhook
                                                 │            │
┌──────────────────────┐       HTTPS      ┌──────┴────────────▼───┐
│  Argo Books Desktop  │ ◀──────────────▶ │  argo-books-website   │
│  (Avalonia, .NET 10) │     API key      │  (PHP, MySQL)         │
│                      │                  │                       │
│  RefundService       │                  │  /api/portal/refunds/*│
│  RefundViewModel     │                  │  refund_requests tbl  │
│  PaymentPortalSvc    │                  │  refund_email_codes   │
│  Local Payment ledger│                  │  refund_audit_log     │
└──────────────────────┘                  └───────────────────────┘
        │                                            │
        │       sync /api/portal/payments-sync       │
        └────────────────────────────────────────────┘
        (existing — extended to include refund records)
```

The server owns the **refund-request state machine**. The desktop initiates and reflects, but the server is the source of truth. This makes cancel-from-email, cooling-off, velocity rules, and audit logging trivial; it also means a desktop crash mid-flow doesn't strand a refund — the server completes it and the desktop reconciles next sync.

**Components touched, by codebase:**

- **Desktop (`ArgoBooks.Core` + `ArgoBooks` UI):** new `RefundService`, `RefundRequestModel`, line-item picker UI, three modal screens (request → verify → success), `Payment` model extensions (`IsRefund`, `RefundedFromPaymentId`, `RefundRequestId`, `RefundReason`), two new `InvoiceStatus` enum values, refund history rendering on invoice detail, Refunds tab in Analytics, locked-email change flow (with verification of both old AND new email), email verification on initial portal registration.
- **Website (`/api/portal/`):** new endpoints under `/refunds/` and `/account/email-change/`, three new tables, two new public token-authenticated pages (`cancel-refund.php`, `revert-email.php`), extended `payments-sync.php`, extended Stripe/PayPal/Square webhooks for reconciliation, three new admin pages.

---

## 3. UI specification

### 3.1 Invoice list — `InvoicesPage`

- Add an **Actions** column with an inline `Refund` button. Disabled and dimmed for invoices that have no refundable portal payment (status `Unpaid`/`Cancelled`/`Draft`, or all payments were non-portal). Hidden entirely if the company has no portal connection.
- Status column gains two new pills: `Refunded`, `Partially Refunded`.

### 3.2 Refund modal — portal flow

Two-step modal in one window.

**Step 1: choose what to refund**
- Header: invoice number, customer, original total, total paid, refundable now.
- Payment list — each portal payment shown as a row; multi-select with running total.
- Line items — one checkbox per line item; each toggleable. Shipping, tax, discount, fee, security deposit shown as their own toggleable lines at the **actual amount paid** — no proportional recomputation.
- Optional reason textbox (1 line, ~250 chars).
- Footer: running refund total, Cancel button, Continue button.

**Step 2: verify**
- Summary recap of step 1.
- 6-digit code input + resend link + countdown.
- Back button + `Refund $X` button.
- Loading spinner while server transitions through `cooling_off` (if applicable) and `processing`.

**Success state:** green check, "Refund issued. The customer will see the money in 5–10 business days."

### 3.3 Invoice detail view

After a refund, the invoice detail shows:
- Header with new lines: `Refunded: -$X`, `Net: $Y`.
- Line items get a small `↩ refunded` tag when fully refunded.
- History entries for each refund: `May 8 · Refund $2,200.00 to Visa •••4242 · "wrong item shipped" · by Evan`.

### 3.4 Payments page

Refund Payments (`IsRefund == true`) display with a `↩ Refund` chip and the absolute amount (sign flipped for display). Portal-payment rows can be clicked to view the originating invoice and use that invoice's Refund button.

No "Mark as refund" quick action — manual/bookkeeping-only refunds are out of scope for v1. If a user needs to record a non-portal refund, they edit the payment directly.

### 3.5 Analytics — Refunds tab

New tab on the Analytics page with seven panels:
1. Total refunded over time (line chart, monthly).
2. Refund rate vs. revenue (gauge with 30/90/365-day toggle).
3. Top-10 refunded customers.
4. Top refunded products / line items.
5. Refund reasons (top 5 list).
6. Channel breakdown (Stripe / PayPal / Square donut).
7. Average time-to-refund (single stat, days from payment to refund).

All seven derive from the local `Payments` collection after sync — no new server endpoints.

### 3.6 Portal Settings page

Two new sections:
- **Owner Email** — read-only display + `Change email…` button. There is **no in-place edit field**; that's intentional.
- **Refund limits** — progress bar showing today's refund spend vs. the current adaptive cap, and a count of refunds in the past hour vs. limit.

### 3.7 Email change modal

Four steps in one window. Both old and new emails must be verified.

1. **Enter new email.**
2. **Enter `.argo` file password** — only shown if the file `IsEncrypted`. Verified locally by `VerifyCurrentPassword`; a flag (not the password) is sent to the server.
3. **Enter the code sent to the OLD email** — confirms the requester controls the current address.
4. **Enter the code sent to the NEW email** — confirms the new address is real and reachable.

After confirm, modal explains that the OLD email will receive a notification with a revert link.

### 3.8 Initial portal registration — email verification (new)

When a user first connects the payment portal (the existing `RegisterCompanyAsync` flow), they enter an email. After registration succeeds, the desktop now shows a **Verify your email** step:
- Server has emailed a 6-digit code to the registered email.
- User types the code in the desktop UI.
- On success, the company's `email_verified_at` is set on the server.
- Until verified, refund requests fail with a clear message (`/refunds/request` returns `412 email_not_verified`); invoice publishing still works (verification gates the high-trust action only).
- Resend logic: 1 per 60s, max 3, then user must contact support.

### 3.9 Public website pages (token-authenticated)

- **`cancel-refund.php`** — landing for the email link. 32-byte token in URL; single-use; expires when the refund leaves `cooling_off`. Shows refund summary; one button: `Cancel this refund`.
- **`revert-email.php`** — same pattern for the OLD-email notification link. Token valid 30 days, single-use.

### 3.10 Admin dashboard — extend existing pages

No new admin pages. Refund and account-management tooling is added into the existing pages so I keep one place to look.

**Extend `/admin/payments/index.php`** (already has overview stats, tabbed sections including "Failed & Refunds"):
- The existing **Failed & Refunds** tab gets a new sub-section above the "Refunded Payments" table: **"Refund Requests"**, listing rows from `refund_requests` filtered by state (excluding `completed` from this sub-section, since completed refunds already appear in the "Refunded Payments" table below — completed refunds are the same data viewed from a different table).
- Filters on the new sub-section: company, state, date range, amount range, velocity tier, provider.
- Row click expands a drawer showing the full `refund_audit_log` for that request, the `line_items_json` snapshot, and admin actions: `Cancel` (only if not yet `processing`), `Force-fail with note`, `Re-issue webhook reconciliation`.
- The overview stats row (top of the page) gains two new tiles: **Refunds in flight** (count of non-completed refund_requests) and **Held for review** (count of `cooling_off` + `failed` since the threshold).

**Extend `/admin/users/index.php`** (lists portal_companies):
- Per-company row gains a status indicator: `Locked`, `Email unverified`, `Pending email change`, etc.
- Per-company drawer/detail view gains:
  - **Email Changes** sub-table — rows from `email_change_requests` for this company. Filters: state, date. Row drawer shows full audit, IP/UA of requester, the verification-checklist UI (license key match, last-4 match, Stripe-on-file phone match, OLD-email out-of-band reply received), `Revert` action with mandatory reason.
  - **Account Lock** controls — `Lock` / `Unlock` buttons (each with mandatory reason); shows current lock state and history.
  - **Refund Velocity** read-out — current adaptive caps and today's usage; lets me adjust per-company `refund_velocity_config` overrides.

### 3.11 Email templates (new)

- `refund-code.html` — "Your refund code is XXXXXX. If you didn't request this, click here to cancel." Cancel link works only during cooling-off.
- `refund-issued.html` — sent post-completion: "A refund of $X was issued from your account on {date}."
- `email-verify-code.html` — sent to a newly-registered email: "Welcome to Argo Books portal. Confirm your email with code XXXXXX."
- `email-change-old-code.html` — sent to OLD email when an email change is requested: "Confirm your email change with code XXXXXX. If this wasn't you, ignore."
- `email-change-new-code.html` — sent to NEW email during the change flow: "Confirm this is your new Argo Books portal email with code XXXXXX."
- `email-change-notification.html` — sent to OLD email after change is finalized: "Your email was changed to <new>. Click here to revert."
- `velocity-alert.html` — sent to owner when soft/cooling/hard threshold hits.

---

## 4. Server data model

### 4.1 New tables

**`refund_requests`** — primary state machine row

| Column | Type | Notes |
|---|---|---|
| `id` | BIGINT PK | auto-increment |
| `company_id` | INT FK | → `portal_companies.id` |
| `invoice_id` | VARCHAR | desktop's GUID for the invoice |
| `invoice_number` | VARCHAR | display only |
| `customer_name` | VARCHAR | display only |
| `provider` | ENUM | `stripe`, `paypal`, `square` |
| `provider_payment_id` | VARCHAR | original charge / payment_intent / order |
| `provider_refund_id` | VARCHAR NULL | populated after successful provider call |
| `amount_cents` | BIGINT | total refund in smallest currency unit |
| `currency` | VARCHAR(3) | |
| `line_items_json` | TEXT | snapshot for display only — not used for math |
| `reason` | TEXT NULL | |
| `state` | ENUM | `pending_code`, `code_verified`, `cooling_off`, `processing`, `completed`, `cancelled`, `failed` |
| `state_reason` | TEXT NULL | populated for `cancelled`/`failed` |
| `cooling_off_until` | DATETIME NULL | when state expires; null if not in cooling-off |
| `velocity_tier` | ENUM | `normal`, `soft_warn`, `delayed`, `hard_block` (the `delayed` tier corresponds to landing in the `cooling_off` *state*; the names are intentionally distinct to avoid confusion) |
| `requested_ip` | VARCHAR(45) | |
| `requested_user_agent` | VARCHAR(255) | |
| `created_at`, `updated_at`, `completed_at` | DATETIME | |

Indexes: `(company_id, state, created_at)`, `(provider_refund_id)`, `(cooling_off_until)`.

**`refund_email_codes`**

| Column | Type | Notes |
|---|---|---|
| `id` | BIGINT PK | |
| `refund_request_id` | BIGINT FK | |
| `code_hash` | VARCHAR(64) | `SHA256(code \|\| request_id)` — request_id as per-row salt |
| `expires_at` | DATETIME | issued_at + 10 min |
| `consumed_at` | DATETIME NULL | |
| `attempts` | INT DEFAULT 0 | request auto-cancelled at 5 |
| `created_at` | DATETIME | |

**`refund_audit_log`** — append-only

| Column | Type | Notes |
|---|---|---|
| `id` | BIGINT PK | |
| `company_id` | INT | |
| `refund_request_id` | BIGINT NULL FK | null for non-refund events (e.g. email change) |
| `event_type` | VARCHAR | `request_created`, `code_sent`, `code_failed`, `code_verified`, `cooling_off_started`, `cancelled_by_user`, `cancelled_by_email_link`, `processing`, `completed`, `failed`, `velocity_tier_assigned`, `email_change_requested`, `email_change_old_verified`, `email_change_new_verified`, `email_changed`, `email_reverted`, `email_registration_verified`, `account_locked`, `account_unlocked` |
| `payload_json` | TEXT | event-specific |
| `actor_type` | ENUM | `owner`, `admin`, `system`, `webhook` |
| `actor_id` | VARCHAR NULL | admin user id when applicable |
| `ip_address`, `user_agent` | VARCHAR | |
| `created_at` | DATETIME | |

**`email_change_requests`**

| Column | Type | Notes |
|---|---|---|
| `id` | BIGINT PK | |
| `company_id` | INT FK | |
| `old_email` | VARCHAR | snapshot at request time |
| `new_email` | VARCHAR | |
| `old_email_code_hash` | VARCHAR(64) | same hashing scheme as refund codes |
| `new_email_code_hash` | VARCHAR(64) | code sent to the NEW email to verify it |
| `old_email_verified_at` | DATETIME NULL | filled when OLD-email code is consumed |
| `new_email_verified_at` | DATETIME NULL | filled when NEW-email code is consumed |
| `password_verified` | BOOLEAN | server records that desktop confirmed local password |
| `state` | ENUM | `pending`, `old_verified`, `new_verified`, `completed`, `cancelled`, `reverted` |
| `cancel_token` | VARCHAR(64) | for the OLD-email revert link |
| `revert_until` | DATETIME | default +30 days |
| `created_at`, `completed_at`, `reverted_at` | DATETIME | |

**`email_verifications`** — tracks initial-registration email verification (and any future "verify this email" purpose)

| Column | Type | Notes |
|---|---|---|
| `id` | BIGINT PK | |
| `company_id` | INT FK | |
| `email` | VARCHAR | the address being verified |
| `purpose` | ENUM | `registration` (others may be added later) |
| `code_hash` | VARCHAR(64) | |
| `expires_at` | DATETIME | issued_at + 10 min |
| `consumed_at` | DATETIME NULL | |
| `attempts` | INT DEFAULT 0 | locked at 5 |
| `created_at` | DATETIME | |

**`refund_velocity_baselines`** — recomputed nightly by cron

| Column | Type |
|---|---|
| `company_id` | INT PK |
| `daily_avg_refund_cents` | BIGINT |
| `daily_avg_refund_count` | INT |
| `revenue_30d_cents` | BIGINT |
| `last_recomputed_at` | DATETIME |

**`refund_velocity_config`** — admin-tunable thresholds, single row or per-company override

| Column | Type | Notes |
|---|---|---|
| `id` | INT PK | |
| `company_id` | INT NULL | null = global default |
| `soft_warn_multiplier` | DECIMAL(5,2) | default 3.0 |
| `cooling_multiplier` | DECIMAL(5,2) | default 10.0 |
| `cooling_revenue_pct` | DECIMAL(5,2) | default 0.25 |
| `hard_revenue_pct` | DECIMAL(5,2) | default 0.50 |
| `cooling_off_minutes` | INT | default 15 |
| `new_account_floor_cents` | BIGINT | default 100000 ($1k for first 7 days) |

### 4.2 Existing table extension

**`portal_companies`** — add four columns:
- `locked` BOOLEAN DEFAULT 0
- `lock_reason` TEXT NULL
- `locked_at` DATETIME NULL
- `email_verified_at` DATETIME NULL — set when initial registration email verification completes; null = unverified

When `locked=1`, all `/refunds/*` endpoints return `423 Locked`.
When `email_verified_at IS NULL`, `/refunds/*` endpoints return `412 email_not_verified` so the user can't issue refunds until they confirm their email.

---

## 5. Endpoints

### 5.1 Refund lifecycle (`/api/portal/refunds/`)

All Bearer-token authenticated. All POSTs honor `Idempotency-Key` (GUID generated by desktop per logical action; same key + same body within 24h returns cached response; same key + different body returns 409).

- **`POST /request`** — body: `invoice_id`, `provider`, `provider_payment_id`, `amount_cents`, `currency`, `line_items` (array, display only), `reason` (optional). Pre-flights provider refundability. Creates row in `pending_code`, generates code, hashes, emails. Returns `{request_id, expires_in_seconds, masked_email}`.
- **`POST /confirm`** — body: `request_id`, `code`. Validates code → runs velocity engine → transitions to `code_verified` then either `cooling_off`, `processing`, or `failed`. Returns `{state, cooling_off_until, velocity_tier, message}`.
- **`POST /cancel`** — body: `request_id`. User cancels from desktop's Back button. Allowed only in `pending_code`, `code_verified`, `cooling_off`.
- **`GET /status/{request_id}`** — full state. Desktop polls every 2s while in `cooling_off`/`processing`.
- **`POST /resend-code`** — rate-limited: 1 per 60s, max 3 per request.

### 5.2 Account email change (`/api/portal/account/email-change/`)

- **`POST /request`** — body: `new_email`, `password_verified` flag. Server verifies local-password flag was set (the password itself is not transmitted), creates a row in state `pending`, emails a code to the **OLD** email.
- **`POST /confirm-old`** — body: `change_id`, `code`. Verifies the OLD-email code; transitions state to `old_verified`; immediately emails a code to the **NEW** email.
- **`POST /confirm-new`** — body: `change_id`, `code`. Verifies the NEW-email code; transitions to `new_verified`; flips `portal_companies.owner_email`; transitions to `completed`; sends post-change notification with revert link to OLD email.
- **`POST /resend-code`** — body: `change_id`, `target` (`old` or `new`). Same rate-limit policy as refund codes (1/60s, max 3).
- **`POST /cancel`** — body: `change_id`. User-initiated abort; allowed in `pending`, `old_verified`. Logs `cancelled_by_user`.

### 5.3 Initial portal registration email verification (`/api/portal/account/verify-email/`)

The existing `/api/portal/register.php` is extended to **not** mark `email_verified_at` on creation; a verification code is sent in the same response cycle.

- **`POST /request`** — re-sends the registration verification code; rate-limited 1/60s, max 3, then `429 too_many_requests` with a "contact support" message.
- **`POST /confirm`** — body: `code`. Validates against the latest unconsumed `email_verifications` row for `purpose=registration`; on success sets `portal_companies.email_verified_at = NOW()` and logs `email_registration_verified`.

### 5.4 Public token-authenticated pages

- **`GET/POST /cancel-refund.php?token=...`** — transitions matching refund_request to `cancelled` if still in cancellable state.
- **`GET/POST /revert-email.php?token=...`** — reverts `owner_email` to old value; sends notification to both addresses; logs `email_reverted`.

### 5.5 Extended sync

**`GET /api/portal/payments-sync.php`** — existing. Response items gain fields:
```json
{
  "is_refund": true,
  "refunded_server_payment_id": "p_7w2...",
  "refund_request_id": "rr_4521",
  "refund_reason": "Wrong item shipped"
}
```

### 5.6 Admin actions (session-authenticated, served from existing admin pages)

These are the action endpoints the admin pages call to mutate state. Implementation can colocate handlers inside the existing `/admin/payments/index.php` and `/admin/users/index.php` (or split into small `_actions.php` includes) — the spec does not require dedicated route files.

- **Refund actions** (called from `/admin/payments/index.php` Failed & Refunds tab):
  - Cancel a non-`processing` refund request
  - Force-fail a refund request (with admin note)
  - Re-issue webhook reconciliation for a stuck request
- **Email-change actions** (called from `/admin/users/index.php` per-company drawer):
  - Revert a completed email change (with mandatory reason)
- **Account-lock actions** (called from `/admin/users/index.php` per-company drawer):
  - Lock company (with mandatory reason)
  - Unlock company (with mandatory reason)
- **Velocity-config actions** (called from `/admin/users/index.php` per-company drawer):
  - Upsert per-company `refund_velocity_config` overrides

Every admin action writes to `refund_audit_log` with `actor_type='admin'`, `actor_id=<admin user id>`, and the supplied reason in `payload_json`.

### 5.7 Provider call details

- **Stripe:** `Stripe\Refund::create(['payment_intent' => ..., 'amount' => ..., 'reason' => 'requested_by_customer', 'metadata' => ['argo_request_id' => ..., 'argo_invoice_id' => ...]])`. The `argo_request_id` in metadata makes the `charge.refunded` webhook idempotent.
- **PayPal:** `POST /v2/payments/captures/{capture_id}/refund` with our `request_id` in `invoice_id` / `custom_id`.
- **Square:** Square SDK `RefundsApi.refundPayment` with our `request_id` in `idempotency_key` and `note`.

### 5.8 Cron jobs (new)

- **Cooling-off promoter** — every minute. `SELECT FROM refund_requests WHERE state='cooling_off' AND cooling_off_until <= NOW()`. Checks `portal_companies.locked` first; if locked, no-op. Otherwise transitions to `processing` and calls provider API.
- **Velocity baseline recomputer** — nightly. Recomputes `refund_velocity_baselines` for every active company.
- **Stale request cleanup** — hourly. Cancels rows in `pending_code` older than 1 hour.
- **Stale processing reconciliation** — every 5 minutes. For rows in `processing` longer than 30 minutes with no `provider_refund_id`, query the provider API for status and reconcile.

---

## 6. State machine

```
  pending_code ──[code OK]──▶ code_verified
       │                          │
       │                    [velocity check]
       │                   /        │         \
       │              normal    soft_warn   hard_block
       │                  │         │           │
       │                  │     cooling_off    failed
       │                  │         │       (account locked)
       │                  ▼         ▼
       │              processing ◀──┘
       │                  │
       │           [provider API]
       │              /        \
       │            ok          err
       │            │            │
       │            ▼            ▼
       │        completed     failed
       │
       └──[5 bad codes | expired | user clicks Back | email cancel link]──▶ cancelled
```

Legal transitions are enforced server-side; any illegal transition is a 409 + audit log entry.

---

## 7. Verification — refund codes

- Code: 6 digits, `random_int(0, 999999)` zero-padded.
- Hash at rest: `SHA256(code || request_id)`. Codes never logged.
- Expiry: 10 minutes.
- Attempts: max 5 wrong → request auto-`cancelled`. (Not an account lock; user just starts over.)
- Resend: max 3 per request, no more often than once per 60s; each resend invalidates prior code.
- Per-company rate limit: 10 codes / hour across all requests; above that, request creation fails with a clear message and admin sees it.

---

## 8. Locked-email change flow

There is no in-place edit field for owner email anywhere in the desktop UI. The change flow has four user-visible steps and four server states (`pending → old_verified → new_verified → completed`).

1. **New email** — format-validated client-side, server-side check that it isn't already used by another company.
2. **`.argo` password** — only shown if file is encrypted. Verified locally; a `password_verified=true` flag is sent to server.
3. **Code from OLD email** — server emails the code to current `owner_email`. User types it in. Server transitions state to `old_verified` and immediately emails a different code to the NEW email.
4. **Code from NEW email** — confirms the new address is real and reachable. On success, server flips `owner_email`, transitions to `completed`, and sends the post-change notification (with revert link) to the OLD address.

On final confirm:
- API key remains valid (not rotated).
- Audit log: `email_change_requested`, `email_change_old_verified`, `email_change_new_verified`, `email_changed`.
- Cooldown: 24h between successful changes.

On revert click:
- Server reverts `owner_email`; notifies both addresses; logs `email_reverted`. Admin dashboard surfaces this prominently.

### 8.1 Initial registration email verification

The same primitives are reused for the initial portal registration. After `RegisterCompanyAsync()` succeeds, the desktop receives the API key but the company has `email_verified_at = NULL`. Refund endpoints are gated until the user enters the verification code (sent to the registered email at registration time). Invoice publishing and other portal features remain available — verification gates only the high-trust action (refunds).

---

## 9. Velocity rules

Computed at the `code_verified → next state` transition.

```text
let baseline   = refund_velocity_baselines for this company
let config     = refund_velocity_config (per-company override OR global default)
let today_$    = SUM(amount_cents) for completed+processing+cooling_off rows today
let hour_count = COUNT(rows) created in past hour

# Hard block
if hour_count >= 25
   OR today_$ >= config.hard_revenue_pct * baseline.revenue_30d_cents
   OR (company_age < 7 days AND today_$ >= config.new_account_floor_cents):
    tier = hard_block
    set portal_companies.locked = true (with reason)
    audit + email admin + email owner
    state -> failed

# Delayed (lands in cooling_off state)
elif hour_count >= 10
     OR today_$ >= MAX(config.cooling_multiplier * baseline.daily_avg_refund_cents,
                       config.cooling_revenue_pct * baseline.revenue_30d_cents):
    tier = delayed
    cooling_off_until = NOW() + config.cooling_off_minutes
    email owner with cancel link
    state -> cooling_off

# Soft warn
elif hour_count >= 5
     OR today_$ >= config.soft_warn_multiplier * baseline.daily_avg_refund_cents:
    tier = soft_warn
    state -> processing  # UI showed extra confirmation before user got here

else:
    tier = normal
    state -> processing
```

**Newly-connected companies** (portal age < 30 days) use absolute floors instead of baseline ratios:

| Age | Soft-warn at | Cooling-off at | Hard-block at |
|---|---|---|---|
| < 7 days | $500/day | $1k/day | $1k cumulative since connect |
| 7–30 days | $1k/day | $3k/day | $10k/day |
| 30+ days | baseline-derived | baseline-derived | baseline-derived |

The soft-warn UX detail: the desktop shows an extra confirmation panel before the user even types the code. So soft-warn is a **UX nudge**; cooling-off is a **server-side hold**.

Audit log on every transition records exact baseline values and which threshold fired.

---

## 10. Books integration (desktop side)

### 10.1 `Payment` model extensions (`ArgoBooks.Core/Models/Transactions/Payment.cs`)

- `bool IsRefund` (default false)
- `Guid? RefundedFromPaymentId`
- `string? RefundRequestId` (server's id; null for manual)
- `string? RefundReason`

**Convention:** refunds are stored as `Payment` rows with **negative `Amount`**, matching the existing server-side webhook code (`_stripe_refund_db.php`). `Sum(Payment.Amount)` naturally gives net paid; UI flips sign for display.

### 10.2 `InvoiceStatus` enum (`ArgoBooks.Core/Enums/InvoiceStatus.cs`)

Add: `Refunded`, `PartiallyRefunded`.

Status recompute (called whenever Payments change):

```csharp
gross  = payments.Where(p => !p.IsRefund).Sum(p => p.Amount);
refund = -payments.Where(p =>  p.IsRefund).Sum(p => p.Amount);  // absolute
net    = gross - refund;

if (gross > 0 && refund >= gross)        status = Refunded;            // >= for defensive over-refund case
else if (refund > 0 && refund < gross)   status = PartiallyRefunded;
else                                     /* existing Paid/Partial/Sent logic */
```

### 10.3 `Invoice` computed properties

- `decimal AmountRefunded` — absolute sum of refund Payments.
- `decimal NetPaid` — `AmountPaid - AmountRefunded`.
- `Balance` getter changes from `Total - AmountPaid` to `Total - NetPaid`.
- **Audit needed** during implementation: every existing call site that reads `AmountPaid` should be reviewed to confirm whether it wants gross paid or net of refunds; flag any that need to change.

### 10.4 Sync extension — `PaymentPortalService.ProcessSyncedPayments`

For records with `is_refund == true`:
- Look up local Payment by `PortalPaymentId == refunded_server_payment_id` to get `RefundedFromPaymentId`.
- Create new local `Payment` with `IsRefund=true`, negative `Amount`, `RefundRequestId`, `RefundReason`, `Source="Online"`, `PortalPaymentId=server_payment_id`.
- Append `InvoiceHistoryEntry`: `"Refund $X via {provider} — {reason}"`.
- Recompute invoice status, `MarkDirty()`.

Idempotency: keyed on `PortalPaymentId` — re-running sync is a no-op (existing pattern).

### 10.5 Charts / Insights / Reports

Existing aggregators sum `Payment.Amount`; with negative refund amounts, totals automatically become net of refunds — the desired behavior for revenue / profit charts.

New aggregations for the Refunds Analytics tab filter explicitly: `Where p.IsRefund == true`, with absolute amounts. Refund rate = `AbsSum(refund Payments) / Sum(positive Payments)` over a period.

Code-review note for implementation: any aggregation that needs **gross** revenue must include `Where !IsRefund` and a comment explaining why.

### 10.6 Persistence

- New `Payment` fields and new `InvoiceStatus` values are backward-compatible JSON additions; old `.argo` files round-trip correctly.
- No schema migration.

### 10.7 Threading / autosave

- All mutations route through `CompanyManager.MarkDirty()`.
- Status poll for active refund modal: `PeriodicTimer` on UI dispatcher, 2s interval, cancelled on modal close / completion / failure.

---

## 11. Error handling

| Failure mode | Behavior |
|---|---|
| Provider API returns error | `refund_request → failed`, `state_reason = error message`, audit log full response, email owner, modal stays open with error visible. No silent retry. |
| Webhook arrives before sync response returns | Webhook handler is idempotent; if request is already `completed`, no-op. If `processing`, transition to `completed`. |
| Webhook never arrives | Stale-processing cron polls provider for status after 30 min and reconciles. |
| Desktop loses connection mid-refund | Server completes regardless. Modal poll times out → "Refund is in progress — close safely. Result will appear on next sync." |
| Desktop crashes during modal | Server completes; user sees outcome on next launch via sync. |
| 5 wrong code attempts | Request auto-cancelled. User starts a new request. Not a security event — codes are 6-digit single-purpose. |
| Stripe refund window expired (charge > 180d) | Pre-flight check fails on `/refunds/request`; returns `409 charge_too_old`. Desktop shows a clear "this payment is too old to refund through the portal — please contact Stripe support if needed" message. |
| Currency edge cases | Refund in payment's currency, not invoice's. Modal shows both ("Refunding £200 — about $254 USD"). Zero-decimal currencies handled by existing webhook code path. |
| Refund > original payment amount | Validated client-side AND server-side (`422 amount_exceeds_refundable`). |
| Already-partially-refunded invoice | Allowed. Refundable = `original_payment_amount - already_refunded_amount`, computed fresh per request. |
| Cancelled invoice | Refund allowed (you might cancel *because* you're refunding). |
| Customer dispute on the charge | Stripe rejects; we surface the error: "Refunds aren't allowed during a dispute. Resolve in your Stripe dashboard." |
| Account locked mid-cooling-off | Cooling-off promoter checks `locked` first; refund stays in `cooling_off` until unlock OR auto-cancels after 24h (configurable). |
| Orphan refund (invoice missing on desktop) | Refund Payment still inserted, flagged in activity log, doesn't crash sync. |

---

## 12. Testing

### 12.1 Unit (xUnit, `ArgoBooks.Tests`)

- `RefundService` orchestration: request → poll → complete → local Payment created with right fields.
- `Invoice.RecomputeStatus()`: ~20-case table-driven test covering every transition (Paid → Refunded, Paid → PartiallyRefunded, Partial → PartiallyRefunded, Partial → Refunded, etc.).
- `Invoice.NetPaid` / `AmountRefunded` getters with mixed positive/negative payments.
- Sync interpretation: `is_refund=true` records create the right local Payment + history entry.
- Multi-payment refund split: refunding $700 across two source payments produces two refund Payments.
- Currency: refund Payment stores `OriginalCurrency` matching source payment, not invoice.

### 12.2 Server-side (PHPUnit if present, otherwise integration)

- State machine: every legal transition + every illegal transition rejected.
- Velocity engine: golden tests with synthetic baselines.
- Code generation/verification: 5-attempt lockout, 10-min expiry, hash never logged.
- Email change: revert window, password gate, OLD-email notification fired.
- Idempotency-Key: same key returns same response; conflicting body returns 409.
- Audit log: every state transition produces an entry (assertable).

### 12.3 End-to-end (manual checklist, run before release)

- Stripe sandbox: invoice → pay → refund full → verify Stripe dashboard, Argo Books invoice, emails.
- Stripe sandbox: pay via 2 partials → refund partially across both → verify allocation.
- Cooling-off path: trigger via test config → verify cancel-from-email link works.
- Email change: happy path + revert path + admin-revert path.
- Network kill: kill desktop mid-refund → verify reconciliation on next launch.
- PayPal sandbox: same matrix.
- Square sandbox: same matrix.

---

## 13. Out of scope (call out so we don't pull them in)

- Multi-user roles inside a company.
- In-app account locking UI.
- Pre-authorize-higher-cap email link.
- Mobile push approval.
- Refunds for non-portal payments (cash, check, bank transfer, etc.) — no rails exist; out of scope.
- Bookkeeping-only manual refund recording UI — user can edit a payment record directly if they need to reflect a non-portal refund.
- Customer-facing self-serve refund requests on the portal.

---

## 14. Open questions / risks

- **`AmountPaid` semantic change** — switching `Balance = Total - NetPaid` may affect existing charts and reports. Implementation must audit every call site of `AmountPaid` before merging.
- **Sync latency** — desktop relies on polling for the `status` endpoint during the modal flow. If the server is briefly slow, the user sees a spinner; this is acceptable but worth a UX tuning pass after first real use.
- **Velocity thresholds in real data** — the multipliers (3×, 10×, 25%, 50%) are educated defaults. We should review and adjust after the first 100 real refunds.
- **PayPal/Square reconciliation** — the existing webhook code is most robust for Stripe; PayPal and Square reconciliation needs equivalent logic (may surface gaps during implementation).

---

## 15. Definitions

- **Refund request** — a row in `refund_requests`; the orchestrating record for one refund attempt.
- **Refund Payment** — a `Payment` row in the desktop's books with `IsRefund=true` and negative `Amount`.
- **Locked email** — the email field on `portal_companies` that cannot be edited via the normal API; only via the email-change flow.
- **Cooling-off** — a server-side delay (default 15 min) during which a refund can be cancelled from the email link before it's sent to the provider.
- **Velocity tier** — the assessment of how anomalous a refund attempt looks: `normal`, `soft_warn`, `delayed`, `hard_block`.

---

## 16. Deployment / implementation workflow notes

These are workflow rules for the implementation phase; they do not affect the design itself.

- **New branch for coding.** All implementation work happens on a new branch (e.g. `feature/invoice-refunds`) cut from `main`. No work commits directly to `main`.
- **Local DB schema (laragon, dev).** The implementation phase generates a single migration SQL file with all the new tables and column additions described in Section 4. That SQL is run against the local laragon MySQL automatically as part of the implementation task. If laragon's MySQL isn't running, the implementer should pause and ask the user to start it rather than fail silently.
- **Production DB schema.** The same SQL is shown to the user at the end of implementation (printed in the chat or saved to `argo-books-website/sql/migrations/<date>-refunds.sql`) so the user can run it against the production server themselves. **No automated production deploy.**
- **Migration safety.** The migration adds new tables and adds nullable columns to existing tables; no data backfill required and no destructive changes. Safe to run online.

---

*End of design.*
