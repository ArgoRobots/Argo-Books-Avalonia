# Customer Payment Portal - Full Plan

## Table of Contents

1. [Answering Your Questions First](#1-answering-your-questions-first)
2. [System Architecture Overview](#2-system-architecture-overview)
3. [Website Payment Page (Customer-Facing)](#3-website-payment-page-customer-facing)
4. [Argo Books Payments Page (Business-Facing)](#4-argo-books-payments-page-business-facing)
5. [Server/API Layer](#5-serverapi-layer)
6. [Payment Flow End-to-End](#6-payment-flow-end-to-end)
7. [Security Considerations](#7-security-considerations)
8. [Implementation Phases](#8-implementation-phases)

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
│  │   (argorobots.com/pay/{token})            │                  │
│  │                                           │                  │
│  │   • Views invoice details                 │                  │
│  │   • Sees all active invoices              │                  │
│  │   • Views payment history                 │                  │
│  │   • Pays via Stripe (card/ACH)            │                  │
│  └──────────────┬───────────────────────────┘                   │
│                 │                                                │
└─────────────────┼───────────────────────────────────────────────┘
                  │ HTTPS API calls
                  ▼
┌─────────────────────────────────────────────────────────────────┐
│                     SERVER (argorobots.com)                      │
│                                                                 │
│  ┌──────────────────────────────────────────┐                   │
│  │  Payment Portal API                       │                  │
│  │  (PHP + MySQL on existing server)         │                  │
│  │                                           │                  │
│  │  • Stores published invoices              │                  │
│  │  • Processes Stripe payments              │                  │
│  │  • Records payment transactions           │                  │
│  │  • Manages customer access tokens         │                  │
│  │  • Provides sync endpoint for Argo Books  │                  │
│  └──────────────┬───────────────────────────┘                   │
│                 │                                                │
└─────────────────┼───────────────────────────────────────────────┘
                  │ HTTPS API calls (sync)
                  ▼
┌─────────────────────────────────────────────────────────────────┐
│                    BUSINESS SIDE (Argo Books)                    │
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

---

## 3. Website Payment Page (Customer-Facing)

### 3.1 How Customers Access It

Customers get a unique, secure link. There are two access paths:

1. **Direct invoice link** (in the email they already receive): `https://argorobots.com/pay/i/{invoice-token}`
   - Shows a specific invoice with a "Pay Now" button
   - The invoice token is a unique, non-guessable ID (UUID or similar)

2. **Customer portal link**: `https://argorobots.com/pay/{customer-token}`
   - Shows ALL active invoices for that customer
   - The customer token is a separate, long-lived token tied to the customer

No login required -- token-based access (like how Google Docs sharing links work). This keeps it simple for customers who just want to pay.

### 3.2 Page Layout & Design

The website will be a clean, modern, responsive page hosted on the existing argorobots.com server. The page has the following sections from top to bottom:

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
│  │              [Pay $1,944.00 Now]                    │  │
│  │                                                    │  │
│  │   Or pay a partial amount: [________] [Pay]        │  │
│  └────────────────────────────────────────────────────┘  │
│                                                          │
│  Notes: Payment due within 30 days of issue date.        │
│                                                          │
└──────────────────────────────────────────────────────────┘
```

#### Active Invoices Tab (Customer Portal View)
When a customer accesses their portal link, they see a tabbed interface:

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

#### Payment Flow (Stripe Checkout)

When the customer clicks "Pay Now":

1. **Amount confirmation** -- Shows the balance due, or lets them enter a custom (partial) amount
2. **Stripe Checkout** -- Redirects to Stripe-hosted checkout page (or embedded Stripe Elements form)
   - Accepts credit/debit cards
   - Accepts ACH bank transfer (optional, lower fees)
   - Stripe handles all PCI compliance
3. **Confirmation page** -- After payment:
   - "Payment Successful" message with green checkmark
   - Payment amount, date, reference number
   - Updated invoice status
   - Option to download receipt as PDF
   - "Return to invoices" link

#### Download Options
- **Download Invoice as PDF** button on each invoice
- **Download Receipt** button after payment

### 3.3 Feature List

| Feature | Description |
|---------|-------------|
| View specific invoice | Full invoice detail with line items, tax, totals |
| View all active invoices | List of all unpaid/partially paid invoices for the customer |
| View payment history | List of all past payments with dates and amounts |
| View all invoices | Complete invoice history including paid ones |
| Pay full amount | One-click payment for the full balance |
| Pay partial amount | Enter a custom amount for partial payment |
| Stripe checkout | Secure payment via credit card or ACH bank transfer |
| Payment confirmation | Confirmation page with receipt details |
| Download invoice PDF | Download a PDF copy of any invoice |
| Download receipt | Download a payment receipt/confirmation |
| Mobile responsive | Works on phone, tablet, and desktop |
| No login required | Token-based access from email links |
| Company branding | Shows the business's logo and name |
| Real-time status | Invoice status updates immediately after payment |

### 3.4 Mobile Design

On mobile, the layout collapses:
- Invoice details become a stacked card layout (no wide table)
- Line items scroll horizontally or stack vertically
- Pay button is full-width and sticky at the bottom
- Tabs become swipeable

---

## 4. Argo Books Payments Page (Business-Facing)

### 4.1 Current State

The Payments page already exists with:
- Statistics cards (Received This Month, Transactions, Pending, Refunds)
- Payment Portal section (stub UI with "Connected" badge, Sync button, Open Portal button -- all placeholder/TODO)
- Payment table with columns: ID, Invoice, Customer, Date, Method, Amount, Status, Actions
- Add/Edit/Delete/Filter modals
- Search, sorting, pagination, column visibility

### 4.2 What Changes Are Needed

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
│  Portal URL: https://argorobots.com/pay/abc123    [Copy] │
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
1. Argo Books calls the server API: `GET /api/portal/payments?since={lastSyncTimestamp}`
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
- **Stripe Configuration** -- Connected Stripe account info
- **Auto-sync interval** -- How often to check for new payments (e.g., every 5 minutes when app is open)
- **Payment methods enabled** -- Toggle credit card, ACH, etc.
- **Notification preferences** -- Get notified when a payment comes in

### 4.3 Integration with Invoices Page

When sending an invoice (existing email flow), Argo Books will also:
1. **Publish the invoice to the server** -- POST invoice data to the portal API
2. **Include the payment link in the email** -- The invoice email template gets a "Pay Online" button/link pointing to the portal

This happens transparently as part of the existing "Send Invoice" workflow. No new button needed -- sending an invoice automatically publishes it to the portal.

---

## 5. Server/API Layer

### 5.1 API Endpoints

All endpoints live on the existing argorobots.com server alongside the existing invoice email API.

| Method | Endpoint | Purpose |
|--------|----------|---------|
| `POST` | `/api/portal/invoices` | Publish/update an invoice from Argo Books to the portal |
| `GET` | `/api/portal/invoices/{token}` | Get invoice data for customer-facing page |
| `GET` | `/api/portal/customer/{token}` | Get all invoices for a customer |
| `POST` | `/api/portal/payments/create` | Create a payment record (from Stripe webhook) |
| `GET` | `/api/portal/payments/sync` | Argo Books pulls new payments since last sync |
| `POST` | `/api/portal/stripe/checkout` | Create a Stripe checkout session |
| `POST` | `/api/portal/stripe/webhook` | Stripe webhook for payment confirmation |
| `GET` | `/api/portal/status` | Check portal connection status |

### 5.2 Database Schema (Server-Side MySQL)

```sql
-- Published invoices (copy of invoice data for the portal)
portal_invoices:
  - id (PK)
  - company_id          -- identifies which Argo Books company
  - invoice_id          -- matches the Argo Books invoice ID (e.g., INV-2024-00042)
  - invoice_token       -- unique URL token for direct invoice access
  - customer_token      -- unique URL token for customer portal access
  - customer_name
  - customer_email
  - invoice_data (JSON) -- full invoice details (line items, totals, etc.)
  - status              -- mirrors Argo Books status
  - total_amount
  - balance_due
  - currency
  - created_at
  - updated_at

-- Payment records from online payments
portal_payments:
  - id (PK)
  - company_id
  - invoice_id
  - customer_name
  - amount
  - currency
  - payment_method      -- 'card' or 'ach'
  - stripe_payment_id   -- Stripe payment intent ID
  - stripe_charge_id    -- Stripe charge ID
  - reference_number    -- generated confirmation number
  - status              -- 'completed', 'pending', 'failed', 'refunded'
  - synced              -- whether Argo Books has pulled this payment yet
  - created_at

-- Company portal settings
portal_companies:
  - id (PK)
  - company_name
  - api_key             -- for authenticating Argo Books API calls
  - stripe_account_id   -- connected Stripe account (Stripe Connect)
  - logo_url
  - settings (JSON)     -- portal preferences
  - created_at
```

### 5.3 Authentication

- **Argo Books → Server**: API key in the `Authorization: Bearer {key}` header (same pattern as existing email API)
- **Customer → Server**: Token in the URL path (no login required)
- **Stripe → Server**: Stripe webhook signature verification

### 5.4 How It Connects to Argo Books

Since Argo Books uses file-based storage (`.argo` files), there is no persistent database connection. Instead:

1. **Publishing**: When the user sends an invoice, Argo Books makes an HTTP POST to the server with the invoice data. The server stores a copy.
2. **Syncing**: When the user clicks "Sync" (or on a timer), Argo Books makes an HTTP GET to pull new payment records. It then creates local Payment objects and updates Invoice balances in the in-memory CompanyData.
3. **Saving**: The next time the user saves their `.argo` file, the synced payments are persisted locally.

This is the same pattern used by the existing `InvoiceEmailService` -- HTTP calls to argorobots.com with API key auth.

---

## 6. Payment Flow End-to-End

### 6.1 Publishing an Invoice

```
1. User creates invoice in Argo Books (Invoices Page)
2. User clicks "Send Invoice" (existing flow)
3. Argo Books sends the email (existing InvoiceEmailService)
4. NEW: Argo Books also publishes the invoice to the portal API
   POST /api/portal/invoices
   Body: { invoiceId, customerName, customerEmail, lineItems, total, dueDate, ... }
5. Server stores the invoice and generates tokens
6. Server returns: { invoiceToken, customerToken, portalUrl }
7. The email template includes a "View & Pay Online" button with the portal URL
8. Invoice status updates to "Sent"
```

### 6.2 Customer Pays Online

```
1. Customer opens email, clicks "View & Pay Online"
2. Browser loads: argorobots.com/pay/i/{invoice-token}
3. Website fetches invoice data from: GET /api/portal/invoices/{token}
4. Customer sees invoice details, clicks "Pay $X Now"
5. Website creates Stripe checkout: POST /api/portal/stripe/checkout
6. Customer is redirected to Stripe checkout (or sees embedded form)
7. Customer enters card details, confirms payment
8. Stripe processes payment, sends webhook to: POST /api/portal/stripe/webhook
9. Server receives webhook:
   a. Verifies Stripe signature
   b. Creates a portal_payments record with status "completed"
   c. Updates portal_invoices balance_due
   d. If fully paid, updates status to "Paid"
   e. Marks the payment as synced = false (Argo Books hasn't pulled it yet)
10. Customer sees confirmation page with receipt
```

### 6.3 Syncing Payment to Argo Books

```
1. User opens Argo Books, goes to Payments page
2. Clicks "Sync Now" (or auto-sync triggers)
3. Argo Books calls: GET /api/portal/payments/sync?since={lastSyncTimestamp}
   Header: Authorization: Bearer {api-key}
4. Server returns: [{ invoiceId, amount, date, method, referenceNumber, ... }]
   (only payments where synced = false or created after lastSyncTimestamp)
5. For each new payment:
   a. Create a new Payment object in CompanyData.Payments
      - Id: auto-generated (PAY-2024-XXXXX)
      - InvoiceId: matched from server response
      - Amount: from server
      - PaymentMethod: mapped (CreditCard, BankTransfer)
      - ReferenceNumber: Stripe charge ID or confirmation number
      - Notes: "Online payment via portal"
      - Source: "Online" (new field)
   b. Update the linked Invoice:
      - AmountPaid += payment amount
      - Balance -= payment amount
      - Status = "Paid" (if Balance <= 0) or "Partial" (if Balance > 0)
   c. Add InvoiceHistoryEntry: "Payment of $X received online"
6. Server marks these payments as synced = true
7. Argo Books updates LastSyncTime
8. Payments table refreshes showing new online payments
9. Invoice statuses update on the Invoices page too
```

### 6.4 Marking Invoices as Paid

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

## 7. Security Considerations

| Concern | Mitigation |
|---------|------------|
| Token guessing | Use UUID v4 tokens (128-bit random) for invoice and customer URLs |
| Payment tampering | All payment processing goes through Stripe -- amounts are validated server-side, not from the client |
| API key exposure | Portal API key stored in `.env` file (same pattern as existing email API key) |
| Webhook spoofing | Verify Stripe webhook signatures using the signing secret |
| Data exposure | Customer can only see their own invoices via their token -- no cross-customer access |
| PCI compliance | Stripe handles all card data -- the portal never touches card numbers |
| HTTPS | All communication over TLS (argorobots.com already has SSL) |
| Rate limiting | API endpoints should have rate limits to prevent abuse |

---

## 8. Implementation Phases

### Phase 1: Server API & Database
- Set up MySQL tables on argorobots.com
- Build the portal API endpoints (PHP, matching existing server stack)
- Integrate Stripe (create checkout sessions, handle webhooks)
- Build invoice publish endpoint
- Build payment sync endpoint

### Phase 2: Customer-Facing Website
- Build the payment portal frontend pages (HTML/CSS/JS on argorobots.com)
- Invoice detail view with "Pay Now" flow
- Customer portal view with Active Invoices, Payment History, All Invoices tabs
- Stripe Checkout integration
- Payment confirmation page
- PDF download for invoices and receipts
- Mobile responsive design

### Phase 3: Argo Books Integration
- Create `PaymentPortalService` in ArgoBooks.Core (HTTP client for portal API)
- Modify invoice send flow to also publish to portal
- Update invoice email templates to include "Pay Online" button/link
- Implement real sync in `PaymentsPageViewModel` (replace stub)
- Implement `OpenPortal` command (open portal URL in browser)
- Add "Source" field to Payment model (Manual vs Online)
- Add portal settings to CompanySettings and Settings modal
- Add "Invoices Awaiting Payment" section to Payments page
- Update invoice status when synced payments come in
- Add portal API key to `.env` configuration

### Phase 4: Polish & Testing
- End-to-end testing of the full payment flow
- Error handling (network failures, Stripe errors, sync conflicts)
- Auto-sync on a timer when app is open
- Notifications when new online payments arrive
- Edge cases: partial payments, refunds, currency conversion
