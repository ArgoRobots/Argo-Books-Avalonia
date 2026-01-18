# Transactions

The Transactions module handles all financial record-keeping including invoices, payments, expenses, revenue, and receipts.

## Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                    DIAGRAM PLACEHOLDER                          │
│                                                                 │
│  Transaction Flow Diagram (Transactions.svg)                    │
│                                                                 │
│  ┌──────────┐    ┌──────────┐    ┌──────────┐                  │
│  │ Invoice  │───▶│ Payment  │───▶│ Revenue  │                  │
│  └──────────┘    └──────────┘    └──────────┘                  │
│                                                                 │
│  ┌──────────┐    ┌──────────┐                                  │
│  │ Receipt  │───▶│ Expense  │                                  │
│  └──────────┘    └──────────┘                                  │
│                                                                 │
│  Show: Data flow between transaction types                      │
│  Include: Status transitions, relationships                     │
└─────────────────────────────────────────────────────────────────┘
```

## Invoices

Invoices track money owed by customers for goods or services.

| Component | Location |
|-----------|----------|
| Model | `ArgoBooks.Core/Models/Invoice.cs` |
| ViewModel | `ArgoBooks/ViewModels/InvoicesPageViewModel.cs` |
| View | `ArgoBooks/Views/InvoicesPage.axaml` |
| Modals | `ArgoBooks/Modals/InvoiceModals/` |

### Features
- Create and edit invoices with line items
- Recurring invoice support (`RecurringInvoice`)
- Invoice status workflow (Draft → Sent → Paid → Overdue)
- Payment tracking and partial payments
- Invoice templates
- Overdue alert notifications

### Invoice Status Flow

```
┌─────────────────────────────────────────────────────────────────┐
│                    DIAGRAM PLACEHOLDER                          │
│                                                                 │
│  Invoice Status Flow (InvoiceStatus.svg)                        │
│                                                                 │
│  [Draft] ──▶ [Sent] ──▶ [Partially Paid] ──▶ [Paid]            │
│                │                                                │
│                ▼                                                │
│            [Overdue]                                            │
│                                                                 │
│  Show: All InvoiceStatus enum values and transitions            │
└─────────────────────────────────────────────────────────────────┘
```

## Payments

Payments record money received from customers.

| Component | Location |
|-----------|----------|
| Model | `ArgoBooks.Core/Models/Payment.cs` |
| ViewModel | `ArgoBooks/ViewModels/PaymentsPageViewModel.cs` |
| View | `ArgoBooks/Views/PaymentsPage.axaml` |
| Modals | `ArgoBooks/Modals/PaymentModals/` |

### Features
- Record payments against invoices
- Multiple payment methods (Cash, Card, Bank Transfer, etc.)
- Payment reconciliation
- Payment history tracking

## Expenses

Expenses track money spent by the business.

| Component | Location |
|-----------|----------|
| Model | `ArgoBooks.Core/Models/Transaction.cs` |
| ViewModel | `ArgoBooks/ViewModels/ExpensesPageViewModel.cs` |
| View | `ArgoBooks/Views/ExpensesPage.axaml` |
| Modals | `ArgoBooks/Modals/ExpenseModals/` |

### Features
- Category-based expense tracking
- Expense analytics and trends
- Receipt attachment support
- Supplier association

## Revenue

Revenue tracks income from sales and other sources.

| Component | Location |
|-----------|----------|
| Model | `ArgoBooks.Core/Models/Sale.cs` |
| ViewModel | `ArgoBooks/ViewModels/RevenuePageViewModel.cs` |
| View | `ArgoBooks/Views/RevenuePage.axaml` |
| Modals | `ArgoBooks/Modals/RevenueModals/` |

### Features
- Revenue stream tracking
- Trend analysis
- Revenue forecasting integration
- Category breakdown

## Receipts

Receipts capture purchase documentation with AI-powered scanning.

| Component | Location |
|-----------|----------|
| Model | `ArgoBooks.Core/Models/Tracking/Receipt.cs` |
| ViewModel | `ArgoBooks/ViewModels/ReceiptsPageViewModel.cs` |
| View | `ArgoBooks/Views/ReceiptsPage.axaml` |
| Modals | `ArgoBooks/Modals/ReceiptsModals/` |
| Service | `ArgoBooks.Core/Services/AzureReceiptScannerService.cs` |

### Features
- AI-powered OCR scanning (99% accuracy)
- Automatic field extraction (vendor, date, amount, line items)
- Auto-population of expense records
- Image storage and retrieval

### Receipt Processing Flow

```
┌─────────────────────────────────────────────────────────────────┐
│                    DIAGRAM PLACEHOLDER                          │
│                                                                 │
│  Receipt Processing Flow (ReceiptProcessing.svg)                │
│                                                                 │
│  [Image Upload] ──▶ [Azure OCR] ──▶ [OcrData] ──▶ [Expense]    │
│        │                                                        │
│        ▼                                                        │
│  [Camera Capture]                                               │
│                                                                 │
│  Show: Image input → Azure service → data extraction → expense  │
│  Include: OcrData fields extracted                              │
└─────────────────────────────────────────────────────────────────┘
```

## Data Models

### LineItem
Shared structure for invoice and receipt line items:
- Description
- Quantity
- Unit price
- Total

### MonetaryValue
Currency-aware money representation:
- Amount (decimal)
- CurrencyInfo (code, symbol, decimals)
