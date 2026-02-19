# Payment Portal

Argo Books includes an online payment portal that allows customers to view and pay invoices through Stripe, PayPal, or Square.

## Overview

![Payment Portal Overview](diagrams/payment-portal/portal-overview.svg)

## Company Registration

Register your company with the portal to enable online invoicing and payments.

- Registration key validation
- API key provisioning
- Company logo upload (PNG, JPG, GIF, WebP, BMP, SVG)
- API key stored securely in `.env` file

### Registration Flow

![Company Registration Flow](diagrams/payment-portal/company-registration-flow.svg)

## Payment Providers

Connect payment providers via OAuth to accept online payments.

- **Stripe** - Credit and debit card payments
- **PayPal** - PayPal account payments
- **Square** - Square account payments

### Provider Connection Flow

![Provider Connection Flow](diagrams/payment-portal/provider-connect-flow.svg)

Providers can be connected or disconnected at any time through Settings.

## Invoice Publishing

Publish invoices to the portal so customers can view and pay online.

- Custom HTML invoice rendering using the desktop template
- Automatic email notifications to customers
- Payment link generation for each published invoice
- Requires at least one connected payment provider

### Publish Flow

![Invoice Publish Flow](diagrams/payment-portal/invoice-publish-flow.svg)

## Payment Synchronization

Online payments are automatically synced back to the local company file.

- Configurable auto-sync interval (default: 5 minutes)
- Duplicate prevention via portal payment IDs
- Multi-currency support with USD conversion
- Online payments distinguished from manual entries

### Sync Flow

![Payment Sync Flow](diagrams/payment-portal/payment-sync-flow.svg)

## Configuration

| Setting | Description |
|---------|-------------|
| **Auto-Sync Interval** | Frequency of payment sync (default: 5 minutes) |
| **Payment Notifications** | Toggle notifications for received online payments |
| **Portal URL** | Customer-facing portal URL |
| **Connected Accounts** | Manage Stripe, PayPal, Square connections |
| **Company Logo** | Upload or remove company logo displayed on portal |
