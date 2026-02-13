# Customer Payment Portal - Full Plan

## Table of Contents

1. [Answering Your Questions First](#1-answering-your-questions-first)
2. [System Architecture Overview](#2-system-architecture-overview)
3. [Where Does the Money Go?](#3-where-does-the-money-go)
4. [Website Payment Page (Customer-Facing)](#4-website-payment-page-customer-facing)
5. [Argo Books Payments Page (Business-Facing)](#5-argo-books-payments-page-business-facing)
6. [Server/API Layer](#6-serverapi-layer)
7. [Payment Flow End-to-End](#7-payment-flow-end-to-end)
8. [Token Security - Deep Dive](#8-token-security---deep-dive)
9. [Implementation Phases](#9-implementation-phases)

---

## 1. Answering Your Questions First

### Is the Payments page different from the Invoices page?

Yes - they serve fundamentally different purposes:

| | Invoices Page | Payments Page |
|---|---|---|
| **What it is** | A list of **bills you sent** to customers | A list of **money you received** from customers |
| **Analogy** | The bill at a restaurant | The credit card receipt after paying |
| **Actions** | Create, edit, send, preview, track status | Record, edit, delete payments received |
| **Statuses** | Draft, Pending, Sent, Viewed, Partial, Paid, Overdue, Cancelled (8 states) | Completed, Pending, Partial, Refunded (4 states) |
| **Key data** | Line items, due dates, amounts owed | Amount received, payment method, reference number |
| **Relationship** | One invoice exists per bill | Multiple payments can exist per invoice (partial payments, installments) |

**Example:** You send Customer A an invoice for $5,000. They pay $2,000 now and $3,000 next month. The invoices page shows 1 invoice with status "Partial." The payments page shows 2 payment records ($2,000 and $3,000), each linked to that invoice.

### Is the Payments page needed?

**Yes, absolutely.** Here's why:

1. **Partial payments** - Customers often pay invoices in installments. You need a ledger of each individual payment transaction.
2. **Payment method tracking** - You need to know which payments came in as cash, check, bank transfer, or online (for reconciliation and accounting).
3. **Refund tracking** - Refunds are negative payment records. The invoices page has no concept of this.
4. **Portal sync hub** - The payments page is where online portal payments show up after customers pay through the website. It's the sync point between your Argo Books file and the server.
5. **Audit trail** - Accountants need a record of every payment transaction, not just the net balance on an invoice.

The invoices page answers: "What do my customers owe me?"
The payments page answers: "What money has come in, when, and how?"

Both are essential. The payment portal feature makes the payments page even more important because it becomes the place where online payments appear after syncing.

---

## 2. System Architecture Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                        CUSTOMER SIDE                            │
│                                                                 │
│  Customer receives email with invoice                           │
│       ↓                                                         │
│  Clicks "View & Pay Invoice" link                               │
│       ↓                                                         │
│  ┌──────────────────────────────────────────┐                   │
│  │   Website Payment Portal                  │                  │
│  │   (argorobots.com/invoice/{token})        │                  │
│  │                                           │                  │
│  │   • Views invoice details                 │                  │
│  │   • Sees all active invoices              │                  │
│  │   • Views payment history                 │                  │
│  │   • Pays via Stripe, PayPal, or Square    │                  │
│  └──────────────┬───────────────────────────┘                   │
│                 │                                                │
└─────────────────┼───────────────────────────────────────────────┘
                  │ HTTPS API calls
                  ▼
┌─────────────────────────────────────────────────────────────────┐
│              SERVER (argorobots.com - EXISTING)                  │
│                                                                 │
│  ┌──────────────────────────────────────────┐                   │
│  │  Payment Portal API (NEW)                 │                  │
│  │  (PHP + MySQL, same stack as existing)    │                  │
│  │                                           │                  │
│  │  • Stores published invoices              │                  │
│  │  • Routes payments via connected accounts │                  │
│  │    (Stripe Connect / PayPal / Square)     │                  │
│  │  • Records payment transactions           │                  │
│  │  • Manages customer access tokens         │                  │
│  │  • Provides sync endpoint for Argo Books  │                  │
│  └──────────────┬───────────────────────────┘                   │
│                 │                                                │
│  ALREADY EXISTS (reused by portal):                             │
│  • stripe/stripe-php ^17.1 → now with Connect                  │
│  • square/square ^42.1 → now with OAuth                        │
│  • PayPal REST API → now with Marketplace                      │
│  • PDO/MySQL, PHPMailer, .env config                           │
│                                                                 │
│  MONEY FLOW:                                                    │
│  Customer pays → Stripe/PayPal/Square → Business's bank account │
│                                     (optional ArgoRobots fee) ↗ │
│                                                                 │
└─────────────────┼───────────────────────────────────────────────┘
                  │ HTTPS API calls (sync)
                  ▼
┌─────────────────────────────────────────────────────────────────┐
│                    BUSINESS SIDE (Argo Books)                    │
│                                                                 │
│  ┌──────────────────────────────────────────┐                   │
│  │  Settings                                 │                  │
│  │  • Connect Stripe/PayPal/Square accounts  │                  │
│  │  • OAuth flow for each provider           │                  │
│  └──────────────────────────────────────────┘                   │
│                                                                 │
│  ┌──────────────────────────────────────────┐                   │
│  │  Invoices Page                            │                  │
│  │  • Creates invoices                       │                  │
│  │  • Sends invoices via email (existing)    │                  │
│  │  • Publishes invoices to portal (new)     │                  │
│  └──────────────────────────────────────────┘                   │
│                                                                 │
│  ┌──────────────────────────────────────────┐                   │
│  │  Payments Page                            │                  │
│  │  • Shows all payments (manual + online)   │                  │
│  │  • Syncs with portal to pull new payments │                  │
│  │  • Shows which invoices are unpaid        │                  │
│  │  • Portal connection status               │                  │
│  └──────────────────────────────────────────┘                   │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

The server acts as the middleman. Argo Books publishes invoice data to the server. The customer views and pays on the website. Argo Books syncs to pull payment records back down. This works because Argo Books is a file-based app (no always-on database) -- the server provides the persistent online layer.

**Key insight:** The existing website already has the Stripe, PayPal, and Square SDKs installed for license purchases. The portal reuses those same SDKs but with **connected accounts** so money flows to the Argo Books user's bank account, not ArgoRobots'. See [Section 3](#3-where-does-the-money-go) for details.

---

## 3. Where Does the Money Go?

### The Problem

The existing website's Stripe/PayPal/Square integration sends money to **ArgoRobots' bank accounts** -- that's correct for license sales (customers buying Argo Books software). But the invoice payment portal is different:

- An Argo Books user (a business/accountant) sends an invoice to their customer
- Their customer pays the invoice through the portal
- That money needs to go to **the Argo Books user's bank account**, not ArgoRobots'

If a plumber sends a $500 invoice to a homeowner, and the homeowner pays online, that $500 goes to the plumber -- not to ArgoRobots.

### The Solution: Connected Accounts (Stripe Connect / PayPal for Marketplaces / Square OAuth)

Each payment provider has an official way for platforms to route payments to the correct business:

#### Stripe Connect

Stripe Connect is designed exactly for this. ArgoRobots becomes a "platform" and each Argo Books user connects their own Stripe account.

```
How it works:
1. Argo Books user clicks "Connect Stripe Account" in Settings
2. They're redirected to Stripe's OAuth page (hosted by Stripe)
3. They log in to their Stripe account (or create one)
4. They authorize ArgoRobots to create charges on their behalf
5. Stripe returns a connected_account_id (e.g., "acct_1234567")
6. This ID is stored on the server linked to their portal

When a customer pays an invoice:
- The payment is created with: stripe_account = "acct_1234567"
- Money goes DIRECTLY to the Argo Books user's Stripe account
- ArgoRobots can optionally take a small platform fee (application_fee_amount)
  or charge nothing (free feature for users)
```

**Key Stripe Connect code change** (vs. the existing direct charge):
```php
// EXISTING (license sales - money goes to ArgoRobots):
$payment_intent = \Stripe\PaymentIntent::create([
    'amount' => 2500,
    'currency' => 'cad',
]);

// NEW (invoice portal - money goes to the Argo Books user):
$payment_intent = \Stripe\PaymentIntent::create([
    'amount' => 194400,
    'currency' => 'usd',
    'application_fee_amount' => 0,  // or a small platform fee
], [
    'stripe_account' => 'acct_1234567',  // the Argo Books user's connected account
]);
```

#### PayPal for Marketplaces

PayPal has a similar system. The Argo Books user connects their PayPal account, and payments are routed to them.

```
How it works:
1. Argo Books user clicks "Connect PayPal Account" in Settings
2. They go through PayPal's partner onboarding flow
3. PayPal returns a merchant_id for the user
4. When a customer pays: the PayPal order is created with the user's merchant_id
   as the payee, so money goes directly to their PayPal account
```

#### Square OAuth

Square also supports OAuth for third-party applications.

```
How it works:
1. Argo Books user clicks "Connect Square Account" in Settings
2. They're redirected to Square's OAuth page
3. They authorize ArgoRobots' app to process payments
4. Square returns an access_token and merchant_id for the user
5. When a customer pays: the payment is created using the user's access_token
   and location_id, so money goes to their Square account
```

### What the Argo Books User Sees

In the Argo Books Settings (or the Payment Portal settings), there's a "Payment Accounts" section:

```
┌──────────────────────────────────────────────────────────┐
│  Payment Accounts                                        │
│                                                          │
│  Connect your payment accounts so customers can pay      │
│  invoices online. Money goes directly to your account.   │
│                                                          │
│  Stripe     [Connected ●]  john@plumbingco.com   [Disconnect] │
│  PayPal     [Connect →]                                  │
│  Square     [Connect →]                                  │
│                                                          │
│  At least one payment account must be connected for      │
│  the payment portal to work. Customers will see only     │
│  the payment methods you have connected.                 │
│                                                          │
└──────────────────────────────────────────────────────────┘
```

- They connect whichever payment providers they use (could be all 3, could be just 1)
- The customer-facing portal only shows the payment methods the business has connected
- If a business only has Stripe connected, their customers only see a credit card option
- If they have all 3, customers get to choose

### Revenue Model (Optional)

ArgoRobots can optionally take a small platform fee on each transaction:

| Option | How it works |
|--------|-------------|
| **Free** (no fee) | Portal is a free feature of Argo Books. All money goes to the user. Good for user adoption. |
| **Small platform fee** | e.g., 0.5% per transaction. Set via `application_fee_amount` in Stripe Connect. Covers server costs. |
| **Premium-only feature** | Portal only available to Premium subscribers ($5/mo). No per-transaction fee. |

This is a business decision. The technical implementation supports all options.

### Summary

```
                    EXISTING (License Sales)
                    ========================
Customer  →  Stripe/PayPal/Square  →  ArgoRobots' Bank Account
(buying Argo Books software)           (correct!)


                    NEW (Invoice Portal)
                    ====================
Customer  →  Stripe Connect / PayPal / Square  →  Argo Books User's Bank Account
(paying their plumber's invoice)                   (correct!)
                                              ↘
                                          Optional small
                                          platform fee to
                                          ArgoRobots
```

---

## 4. Website Payment Page (Customer-Facing)

### 4.1 How Customers Access It

Customers get a unique, secure link. There are two access paths:

1. **Direct invoice link** (in the email they already receive): `https://argorobots.com/invoice/{invoice-token}`
   - Shows a specific invoice with a "Pay Now" button
   - The invoice token is a cryptographically secure random string (see Section 8 for full security analysis)

2. **Customer portal link**: `https://argorobots.com/portal/{customer-token}`
   - Shows ALL active invoices for that customer
   - The customer token is a separate, long-lived token tied to the customer

No login required -- token-based access (like how Google Docs sharing links work). This keeps it simple for customers who just want to pay. See **Section 8** for a detailed breakdown of why this is secure and what protections are in place.

### 4.2 Page Layout & Design

The website will be a clean, modern, responsive page hosted on the existing argorobots.com server, matching the existing site's HTML/CSS/jQuery stack (no new frameworks needed).

#### Header
- Company logo (uploaded from Argo Books company settings)
- Company name
- "Invoice Portal" subtitle

#### Invoice Summary Section (when viewing a specific invoice)
```
┌──────────────────────────────────────────────────────────┐
│  Invoice #INV-2024-00042                                 │
│                                                          │
│  From: [Company Name]              To: [Customer Name]   │
│  Issue Date: Jan 15, 2025          Due Date: Feb 15, 2025│
│                                                          │
│  ┌────────────────────────────────────────────────────┐  │
│  │ Description          Qty    Price     Total        │  │
│  │ Web Development      10     $150      $1,500.00    │  │
│  │ Hosting (Annual)     1      $300      $300.00      │  │
│  │                                                    │  │
│  │                       Subtotal:       $1,800.00    │  │
│  │                       Tax (8%):       $144.00      │  │
│  │                       ─────────────────────────    │  │
│  │                       Total:          $1,944.00    │  │
│  │                       Paid:           $0.00        │  │
│  │                       Balance Due:    $1,944.00    │  │
│  └────────────────────────────────────────────────────┘  │
│                                                          │
│  Status: [OVERDUE badge]            Due: Feb 15, 2025    │
│                                                          │
│  ┌────────────────────────────────────────────────────┐  │
│  │  Pay with:  [Stripe]  [PayPal]  [Square]           │  │
│  │                                                    │  │
│  │         [Pay $1,944.00 Now]                        │  │
│  │                                                    │  │
│  │  Or pay a partial amount: [________] [Pay]         │  │
│  └────────────────────────────────────────────────────┘  │
│                                                          │
│  Notes: Payment due within 30 days of issue date.        │
│                                                          │
└──────────────────────────────────────────────────────────┘
```

#### Payment Method Selection

The customer chooses their preferred payment method, just like the existing license checkout page (`/upgrade/standard/checkout/?method=`):

- **Stripe** -- Credit/debit card via Stripe Elements (existing `stripe/stripe-php ^17.1`)
- **PayPal** -- PayPal Checkout SDK (existing PayPal REST API integration)
- **Square** -- Square Web Payments SDK (existing `square/square ^42.1`)

The existing `main.js` from `/upgrade/standard/checkout/` already handles all three payment method UIs -- the portal checkout JS can follow the same pattern.

#### Active Invoices Tab (Customer Portal View)
When a customer accesses their portal link (`/portal/{customer-token}`), they see a tabbed interface:

**Tab 1: Active Invoices** (default)
- Table/card list of all unpaid/partially paid invoices
- Each row shows: Invoice #, Date, Due Date, Amount, Balance, Status, [Pay] button
- Sorted by due date (most urgent first)
- Overdue invoices highlighted in red/orange
- Total outstanding amount shown at top

**Tab 2: Payment History**
- List of all completed payments
- Each row shows: Date, Invoice #, Amount Paid, Payment Method, Reference #
- Shows receipt/confirmation details
- Sorted by date (newest first)

**Tab 3: All Invoices**
- Complete history of all invoices (paid, unpaid, cancelled)
- Each row shows: Invoice #, Date, Due Date, Total, Status
- Click any invoice to expand and see full details + line items
- Paid invoices shown with green checkmark

#### Payment Flow

When the customer clicks "Pay Now":

1. **Amount confirmation** -- Shows the balance due, or lets them enter a custom (partial) amount
2. **Payment method selection** -- Customer picks Stripe, PayPal, or Square
3. **Checkout** -- Based on selection:
   - **Stripe**: Embedded card form via Stripe Elements, calls `confirmCardPayment()` with client_secret from server-created PaymentIntent
   - **PayPal**: PayPal Checkout button, creates order via `actions.order.create()`, captures on approval
   - **Square**: Square Web Payments card form, gets source_id, posts to server `/v2/payments`
4. **Server processing** -- Backend creates payment record, verifies amount, processes through the selected provider (reusing the same patterns from `process-stripe-payment.php`, `process-paypal-payment.php`, `process-square-payment.php`)
5. **Confirmation page** -- After payment:
   - "Payment Successful" message with green checkmark
   - Payment amount, date, reference number, payment method used
   - Updated invoice status
   - Option to download receipt as PDF
   - "Return to invoices" link

#### Download Options
- **Download Invoice as PDF** button on each invoice
- **Download Receipt** button after payment

### 4.3 Feature List

| Feature | Description |
|---------|-------------|
| View specific invoice | Full invoice detail with line items, tax, totals |
| View all active invoices | List of all unpaid/partially paid invoices for the customer |
| View payment history | List of all past payments with dates and amounts |
| View all invoices | Complete invoice history including paid ones |
| Pay full amount | One-click payment for the full balance |
| Pay partial amount | Enter a custom amount for partial payment |
| Stripe payment | Credit/debit card via Stripe Elements (existing integration) |
| PayPal payment | PayPal Checkout (existing integration) |
| Square payment | Square Web Payments (existing integration) |
| Payment confirmation | Confirmation page with receipt details |
| Download invoice PDF | Download a PDF copy of any invoice |
| Download receipt | Download a payment receipt/confirmation |
| Mobile responsive | Works on phone, tablet, and desktop |
| No login required | Secure token-based access from email links |
| Company branding | Shows the business's logo and name |
| Real-time status | Invoice status updates immediately after payment |

### 4.4 Mobile Design

On mobile, the layout collapses:
- Invoice details become a stacked card layout (no wide table)
- Line items scroll horizontally or stack vertically
- Pay button is full-width and sticky at the bottom
- Tabs become swipeable
- Payment method selection becomes a vertical stack

---

## 5. Argo Books Payments Page (Business-Facing)

### 5.1 Current State

The Payments page already exists with:
- Statistics cards (Received This Month, Transactions, Pending, Refunds)
- Payment Portal section (stub UI with "Connected" badge, Sync button, Open Portal button -- all placeholder/TODO)
- Payment table with columns: ID, Invoice, Customer, Date, Method, Amount, Status, Actions
- Add/Edit/Delete/Filter modals
- Search, sorting, pagination, column visibility

### 5.2 What Changes Are Needed

The Payments page needs these enhancements to work with the portal:

#### A. Enhanced Payment Portal Section

The existing portal stub section gets upgraded:

```
┌──────────────────────────────────────────────────────────┐
│  Payment Portal                                          │
│                                                          │
│  [Connected ●]  or  [Disconnected ○]                     │
│                                                          │
│  When customers receive invoices, they can pay through   │
│  your online portal. Payments sync automatically.        │
│                                                          │
│  Portal URL: https://argorobots.com/portal/abc123 [Copy] │
│                                                          │
│  Last synced: 2 minutes ago                              │
│                                                          │
│  [↻ Sync Now]     [↗ Open Portal]     [⚙ Settings]      │
│                                                          │
│  ┌────────────────────────────────────────────────────┐  │
│  │  3 new online payments since last sync             │  │
│  │  $2,450.00 received online this month              │  │
│  └────────────────────────────────────────────────────┘  │
│                                                          │
└──────────────────────────────────────────────────────────┘
```

#### B. Payment Source Indicator

Each payment in the table gets a source indicator:
- **Manual** -- entered by hand in Argo Books (existing behavior)
- **Online** -- received through the payment portal (new)

This shows up as a small badge or icon in the table, so you can tell at a glance which payments came from the website vs. which were entered manually.

#### C. Sync Behavior

When "Sync Now" is clicked (or on auto-sync):
1. Argo Books calls the server API: `GET /api/portal/payments/sync?since={lastSyncTimestamp}`
2. Server returns all new payment records since last sync
3. Argo Books creates Payment records in the local CompanyData
4. Updates the corresponding Invoice status/balance (AmountPaid, Balance, Status)
5. Updates LastSyncTime
6. Refreshes the table

#### D. Unpaid Invoices Quick View

A new collapsible section or tab showing invoices that are waiting for payment through the portal:

```
┌──────────────────────────────────────────────────────────┐
│  Invoices Awaiting Payment (Published to Portal)         │
│                                                          │
│  INV-2024-042  |  Customer A  |  $1,944  |  Overdue     │
│  INV-2024-045  |  Customer B  |  $3,200  |  Sent        │
│  INV-2024-048  |  Customer C  |  $750    |  Viewed      │
│                                                          │
│  Total outstanding: $5,894.00                            │
└──────────────────────────────────────────────────────────┘
```

#### E. Portal Settings

Accessible via the settings button, or via the main app Settings modal:
- **Portal API Key** -- Authentication key for the portal API (stored in .env or company settings)
- **Auto-sync interval** -- How often to check for new payments (e.g., every 5 minutes when app is open)
- **Notification preferences** -- Get notified when a payment comes in

### 5.3 Integration with Invoices Page

When sending an invoice (existing email flow), Argo Books will also:
1. **Publish the invoice to the server** -- POST invoice data to the portal API
2. **Include the payment link in the email** -- The invoice email template gets a "View & Pay Online" button/link pointing to the portal

This happens transparently as part of the existing "Send Invoice" workflow. No new button needed -- sending an invoice automatically publishes it to the portal.

---

## 6. Server/API Layer

### 6.1 Building on the Existing Website

The existing argorobots.com website already has everything we need as foundation:

| What exists | Where | How the portal reuses it |
|---|---|---|
| Stripe PHP SDK (`^17.1`) | `composer.json`, `vendor/` | Same SDK for creating PaymentIntents for invoice payments |
| Square PHP SDK (`^42.1`) | `composer.json`, `vendor/` | Same SDK for creating Square payments |
| PayPal REST API client | `process-paypal-payment.php` | Same access token + order capture pattern |
| Processing fee calculation | `config/pricing.php` | Same `calculate_processing_fee()` function |
| PDO database connection | `db_connect.php` | Same connection for new portal tables |
| .env configuration | `vlucas/phpdotenv` | Same `.env` file for portal API keys |
| Email sending | `email_sender.php` + PHPMailer | Same for payment confirmation emails |
| Security headers (CSP) | `.htaccess` | Extend existing CSP to allow portal pages |
| Payment webhook patterns | `webhooks/` | Same patterns for portal payment webhooks |

**No new SDKs or dependencies needed.** The portal is a new set of pages and API endpoints on the existing stack.

### 6.2 New API Endpoints

All endpoints live on the existing argorobots.com server alongside the existing invoice email API.

| Method | Endpoint | Purpose |
|--------|----------|---------|
| `POST` | `/api/portal/invoices` | Publish/update an invoice from Argo Books to the portal |
| `GET` | `/api/portal/invoices/{token}` | Get invoice data for customer-facing page |
| `GET` | `/api/portal/customer/{token}` | Get all invoices for a customer |
| `POST` | `/api/portal/checkout` | Create a payment session (Stripe/PayPal/Square, based on `method` param) |
| `POST` | `/api/portal/webhooks/stripe` | Stripe webhook for invoice payment confirmation |
| `POST` | `/api/portal/webhooks/paypal` | PayPal webhook/IPN for invoice payment confirmation |
| `POST` | `/api/portal/webhooks/square` | Square webhook for invoice payment confirmation |
| `GET` | `/api/portal/payments/sync` | Argo Books pulls new payments since last sync |
| `GET` | `/api/portal/status` | Check portal connection status |

### 6.3 New Database Tables (Server-Side MySQL)

Added to the existing MySQL database alongside `license_keys`, `payment_transactions`, `community_users`, etc.

```sql
-- Registered portal companies (one per Argo Books user)
CREATE TABLE portal_companies (
  id INT AUTO_INCREMENT PRIMARY KEY,
  api_key VARCHAR(64) NOT NULL UNIQUE,        -- for Argo Books <-> server authentication
  company_name VARCHAR(255),
  company_logo_url VARCHAR(500),
  -- Connected payment accounts (from OAuth flows)
  stripe_account_id VARCHAR(255),             -- Stripe Connect account ID (acct_xxx)
  paypal_merchant_id VARCHAR(255),            -- PayPal merchant ID
  square_merchant_id VARCHAR(255),            -- Square merchant ID
  square_access_token TEXT,                   -- Square OAuth access token (encrypted)
  settings JSON,                              -- portal preferences
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
);

-- Published invoices (copy of invoice data for the portal)
CREATE TABLE portal_invoices (
  id INT AUTO_INCREMENT PRIMARY KEY,
  company_id INT NOT NULL,                    -- FK to portal_companies
  invoice_id VARCHAR(32) NOT NULL,            -- matches Argo Books ID (e.g., INV-2024-00042)
  invoice_token VARCHAR(64) NOT NULL UNIQUE,  -- secure URL token (see Section 8)
  customer_token VARCHAR(64) NOT NULL,        -- secure customer portal token
  customer_name VARCHAR(255) NOT NULL,
  customer_email VARCHAR(255),
  invoice_data JSON NOT NULL,                 -- full invoice details (line items, totals, etc.)
  status VARCHAR(20) DEFAULT 'sent',          -- mirrors Argo Books status
  total_amount DECIMAL(12,2) NOT NULL,
  balance_due DECIMAL(12,2) NOT NULL,
  currency VARCHAR(3) DEFAULT 'USD',
  due_date DATE,
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  INDEX idx_invoice_token (invoice_token),
  INDEX idx_customer_token (customer_token),
  INDEX idx_company (company_id),
  FOREIGN KEY (company_id) REFERENCES portal_companies(id)
);

-- Payment records from online payments
CREATE TABLE portal_payments (
  id INT AUTO_INCREMENT PRIMARY KEY,
  company_id INT NOT NULL,                    -- FK to portal_companies
  invoice_id VARCHAR(32) NOT NULL,
  customer_name VARCHAR(255),
  amount DECIMAL(12,2) NOT NULL,
  processing_fee DECIMAL(8,2) DEFAULT 0.00,
  currency VARCHAR(3) DEFAULT 'USD',
  payment_method VARCHAR(20) NOT NULL,        -- 'stripe', 'paypal', 'square'
  provider_payment_id VARCHAR(255),           -- Stripe PaymentIntent ID / PayPal Order ID / Square Payment ID
  provider_transaction_id VARCHAR(255),       -- Stripe Charge ID / PayPal Capture ID / Square Transaction ID
  reference_number VARCHAR(32) NOT NULL,      -- human-readable confirmation number
  status VARCHAR(20) DEFAULT 'completed',     -- 'completed', 'pending', 'failed', 'refunded'
  synced_to_argo TINYINT(1) DEFAULT 0,        -- whether Argo Books has pulled this payment yet
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  INDEX idx_company_sync (company_id, synced_to_argo),
  INDEX idx_invoice (invoice_id),
  FOREIGN KEY (company_id) REFERENCES portal_companies(id)
);
```

### 6.4 Authentication

- **Argo Books -> Server**: API key in the `Authorization: Bearer {key}` header (same pattern as existing `InvoiceEmailService` which uses `X-Api-Key` header)
- **Customer -> Server**: Token in the URL path (no login required -- see Section 7 for security analysis)
- **Stripe/PayPal/Square -> Server**: Webhook signature verification (existing patterns in `webhooks/`)

### 6.5 How It Connects to Argo Books

Since Argo Books uses file-based storage (`.argo` files), there is no persistent database connection. Instead:

1. **Publishing**: When the user sends an invoice, Argo Books makes an HTTP POST to the server with the invoice data. The server stores a copy. This extends the existing `InvoiceEmailService` flow.
2. **Syncing**: When the user clicks "Sync" (or on a timer), Argo Books makes an HTTP GET to pull new payment records. It then creates local Payment objects and updates Invoice balances in the in-memory CompanyData.
3. **Saving**: The next time the user saves their `.argo` file, the synced payments are persisted locally.

This is the exact same pattern used by the existing `InvoiceEmailService` -- HTTP calls to argorobots.com with API key auth.

---

## 7. Payment Flow End-to-End

### 7.1 Publishing an Invoice

```
1. User creates invoice in Argo Books (Invoices Page)
2. User clicks "Send Invoice" (existing flow)
3. Argo Books sends the email (existing InvoiceEmailService)
4. NEW: Argo Books also publishes the invoice to the portal API
   POST /api/portal/invoices
   Body: { invoiceId, customerName, customerEmail, lineItems, total, dueDate, ... }
5. Server stores the invoice and generates secure tokens
6. Server returns: { invoiceToken, customerToken, portalUrl }
7. The email template includes a "View & Pay Online" button with the portal URL
8. Invoice status updates to "Sent"
```

### 7.2 Customer Pays Online

```
1. Customer opens email, clicks "View & Pay Online"
2. Browser loads: argorobots.com/invoice/{invoice-token}
3. Website fetches invoice data from: GET /api/portal/invoices/{token}
4. Customer sees invoice details, selects payment method (Stripe/PayPal/Square)
5. Customer clicks "Pay $X Now"
6. Based on payment method:

   STRIPE:
   a. Frontend calls POST /api/portal/checkout with method=stripe
   b. Server creates Stripe PaymentIntent (same as existing stripe-payment-intent.php)
   c. Frontend receives client_secret, shows Stripe Elements card form
   d. Customer enters card, frontend calls stripe.confirmCardPayment()
   e. Stripe sends webhook to /api/portal/webhooks/stripe

   PAYPAL:
   a. Frontend renders PayPal Checkout button (same SDK as existing)
   b. Customer clicks, PayPal order created via actions.order.create()
   c. Customer approves in PayPal popup
   d. Frontend captures order, sends to /api/portal/checkout for server verification
   e. Server verifies via PayPal /v2/checkout/orders/{id}

   SQUARE:
   a. Frontend renders Square card form (same SDK as existing)
   b. Customer enters card, frontend gets source_id
   c. Frontend sends source_id to POST /api/portal/checkout with method=square
   d. Server processes via Square /v2/payments endpoint

7. Server records the payment:
   a. Creates portal_payments record with status "completed"
   b. Updates portal_invoices balance_due
   c. If fully paid, updates invoice status to "Paid"
   d. Sets synced_to_argo = 0 (Argo Books hasn't pulled it yet)
8. Customer sees confirmation page with receipt
```

### 7.3 Syncing Payment to Argo Books

```
1. User opens Argo Books, goes to Payments page
2. Clicks "Sync Now" (or auto-sync triggers)
3. Argo Books calls: GET /api/portal/payments/sync?since={lastSyncTimestamp}
   Header: Authorization: Bearer {api-key}
4. Server returns: [{ invoiceId, amount, date, method, referenceNumber, providerName, ... }]
   (only payments where synced_to_argo = 0 or created after lastSyncTimestamp)
5. For each new payment:
   a. Create a new Payment object in CompanyData.Payments
      - Id: auto-generated (PAY-2024-XXXXX)
      - InvoiceId: matched from server response
      - Amount: from server
      - PaymentMethod: mapped (CreditCard for Stripe/Square, PayPal for PayPal)
      - ReferenceNumber: confirmation number from server
      - Notes: "Online payment via [Stripe/PayPal/Square]"
      - Source: "Online" (new field)
   b. Update the linked Invoice:
      - AmountPaid += payment amount
      - Balance -= payment amount
      - Status = "Paid" (if Balance <= 0) or "Partial" (if Balance > 0)
   c. Add InvoiceHistoryEntry: "Payment of $X received online via [provider]"
6. Argo Books confirms sync: POST /api/portal/payments/sync/confirm
   Server marks those payments as synced_to_argo = 1
7. Argo Books updates LastSyncTime
8. Payments table refreshes showing new online payments
9. Invoice statuses update on the Invoices page too
```

### 7.4 Marking Invoices as Paid

An invoice gets marked as "Paid" through this logic:

```
When a payment is recorded (either manual or from portal sync):
  1. Look up the invoice by InvoiceId
  2. Calculate total payments: SUM(all payments where InvoiceId matches AND amount > 0)
  3. Update invoice:
     - AmountPaid = total payments
     - Balance = Total - AmountPaid
     - If Balance <= 0: Status = "Paid"
     - If Balance > 0 and AmountPaid > 0: Status = "Partial"
  4. Add history entry
  5. Push status update to server: POST /api/portal/invoices (with updated status)
  6. Server updates portal_invoices so the customer portal reflects the new status
```

This logic already partially exists in `PaymentsPageViewModel.GetPaymentStatus()` (lines 388-409). It will be formalized into a shared service.

---

## 8. Token Security - Deep Dive

### The Concern

> "Is having argorobots.com/invoice/{token} secure? What if someone enters a random token or somehow ends up on someone else's payment page?"

This is a valid concern. Here's the full analysis:

### Why Token-Based Access is Industry Standard

Token-based access (no login required) is how most invoice/payment systems work:

- **Stripe Invoices**: `invoice.stripe.com/i/inv_XXXXXXXX` (token in URL)
- **PayPal Invoices**: `paypal.com/invoice/p/#XXXXXXXX` (token in URL)
- **Square Invoices**: `squareup.com/pay-invoice/XXXXXXXX` (token in URL)
- **FreshBooks**: `my.freshbooks.com/#/invoice/XXXXXXXX` (token in URL)
- **QuickBooks**: `customer.qbo.intuit.com/qbo/pay/XXXXXXXX` (token in URL)

None of these require the customer to create an account or log in. The token IS the authentication.

### Why Random Guessing Won't Work

The tokens will be 48-character hex strings generated from `random_bytes(24)` (PHP's cryptographically secure random generator). That gives **192 bits of entropy**.

To put this in perspective:

| Scenario | Probability |
|----------|-------------|
| Guessing a specific token correctly | 1 in 6.3 × 10⁵⁷ (a number with 57 zeros) |
| Guessing ANY valid token if 1,000,000 invoices exist | 1 in 6.3 × 10⁵¹ |
| Trying 1 billion guesses per second for 1 billion years | Still essentially 0% chance |

For comparison, your chance of winning the lottery every week for a year straight is higher than guessing a single token. This is the same level of security that powers JWT tokens, API keys, and password reset links everywhere on the internet.

### Additional Security Layers

Beyond the unguessable tokens, we add these protections:

**1. Rate Limiting**
```
The existing website already has rate limiting (rate_limits.json pattern).
Portal token lookups will be rate-limited:
- 10 failed lookups per IP per 15 minutes
- After that, IP gets temporarily blocked
- This makes automated guessing attempts practically impossible
```

**2. Token Expiration**
```
Invoice tokens can optionally expire:
- Active invoice tokens: valid until invoice is paid or cancelled
- Paid invoice tokens: remain viewable (read-only) for 90 days, then expire
- Customer portal tokens: long-lived but can be revoked from Argo Books
```

**3. Read-Only Until Payment**
```
Even if someone somehow got a token:
- They can only VIEW the invoice (customer name, amounts, line items)
- They CANNOT modify the invoice
- They CANNOT see other customers' invoices
- They CAN pay the invoice -- but paying someone else's bill is not harmful
  (the business gets the money, the "attacker" loses money)
```

**4. No Sensitive Data Exposed**
```
The invoice page shows:
- Company name and logo (public info)
- Customer first/last name (already on the invoice they received)
- Invoice line items and amounts (already in the email)
- Payment status

It does NOT show:
- Customer email address (masked or hidden)
- Customer physical address (hidden)
- Customer phone number (hidden)
- Other customers' data
- Payment card details (handled by Stripe/PayPal/Square, never on our page)
- Internal business data
```

**5. Server-Side Validation**
```
When a token is used:
- Token looked up via indexed database query (instant, not iterable)
- Invalid tokens return a generic "Invoice not found" page (no info leakage)
- No enumeration possible (tokens are random, not sequential)
- All payment amounts validated server-side against the actual invoice balance
```

**6. Monitoring & Alerts**
```
- Log all token lookup attempts
- Alert on unusual patterns (many failed lookups from same IP)
- Ability to revoke/regenerate tokens from Argo Books if concerned
```

### Summary: Token Security

The token approach is:
- **More secure** than email-based login (emails can be compromised, reused passwords)
- **Same approach** used by Stripe, PayPal, Square, FreshBooks, and QuickBooks for their own invoice portals
- **Mathematically unguessable** with 192-bit random tokens
- **Rate-limited** against brute force
- **Low-risk even in worst case** (viewing an invoice is not sensitive; paying someone else's bill only hurts the attacker)

The simplicity (no login) is actually a feature -- customers are more likely to pay promptly when they can click a link and pay immediately without creating yet another account.

---

## 9. Implementation Phases

### Phase 1: Server API, Database & Connected Accounts
- Add `portal_companies`, `portal_invoices`, and `portal_payments` tables to existing MySQL database
- Set up Stripe Connect (register ArgoRobots as a platform on Stripe)
- Set up PayPal for Marketplaces (partner referral API)
- Set up Square OAuth (register app for third-party access)
- Build OAuth callback endpoints for each provider (user connects their account)
- Build the portal API endpoints (PHP, same patterns as existing `api/` folder)
- Build checkout endpoints that create charges on the user's connected account (not ArgoRobots')
- Build invoice publish endpoint (receives data from Argo Books)
- Build payment sync endpoint (returns new payments to Argo Books)
- Add webhook endpoints for all three payment providers

### Phase 2: Customer-Facing Website
- Build the portal frontend pages (HTML/CSS/jQuery, same stack as existing site)
- Invoice detail view at `/invoice/{token}`
- Customer portal view at `/portal/{token}` with Active Invoices, Payment History, All Invoices tabs
- Payment method selection -- only show providers the business has connected
- Adapt existing `main.js` checkout logic for connected account payments
- Payment confirmation page with receipt
- PDF download for invoices and receipts
- Mobile responsive design
- Add CSP headers for portal pages in `.htaccess`

### Phase 3: Argo Books Integration
- Create `PaymentPortalService` in ArgoBooks.Core (HTTP client for portal API)
- Add "Connect Payment Accounts" UI in Settings (OAuth flows for Stripe/PayPal/Square)
- Modify invoice send flow to also publish to portal
- Update invoice email templates to include "View & Pay Online" button/link
- Implement real sync in `PaymentsPageViewModel` (replace the existing stub)
- Implement `OpenPortal` command (open portal URL in default browser)
- Add "Source" field to Payment model (Manual vs Online)
- Add portal settings to CompanySettings and Settings modal
- Add "Invoices Awaiting Payment" section to Payments page
- Update invoice status when synced payments come in
- Add portal API key to `.env` configuration

### Phase 4: Polish & Testing
- End-to-end testing of the full payment flow (all 3 providers with connected accounts)
- Test OAuth connect/disconnect flows for each provider
- Error handling (network failures, payment errors, sync conflicts)
- Auto-sync on a timer when app is open
- Notifications when new online payments arrive
- Edge cases: partial payments, refunds, currency conversion
- Rate limiting and security testing on token endpoints
