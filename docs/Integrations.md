# Integrations

Argo Books integrates with external services to provide AI-powered features and real-time data.

## Overview

![Integrations Overview](diagrams/integrations/integrations-overview.svg)

## Argo Books Server

`argorobots.com` hosts a PHP backend that sits between the desktop app and every external service it touches. It plays two distinct roles:

1. **Proxy** for third-party services. The app never talks to Gemini, Google, or the Exchange Rates provider directly; every call is mediated by the server. This centralizes API keys, cost tracking, and per-license quota enforcement.
2. **First-party API host** for functionality that has no upstream service: licensing, quota counters, payment-portal customer flows, transactional emails, and more.

### Third-party services proxied through the server

| Service | What the server proxies |
|---|---|
| Google Gemini | Receipt OCR, supplier/category matching, AI spreadsheet import. See [ReceiptScanning](ReceiptScanning.md) and [AISpreadsheetImport](AISpreadsheetImport.md). |
| Google OAuth & Sheets | Sign-in-with-Google flow and chart export to Google Sheets. |
| Exchange rate provider | Cached real-time and historical USD-base rates. |

### First-party server APIs

| Category | Purpose |
|---|---|
| License & subscription | Validate license keys, redeem purchased keys, fetch live pricing. See [LicenseKey](LicenseKey.md). |
| Usage quotas | Per-license monthly counters for receipt scans, AI imports, and published invoices. |
| Invoice email | Server-side delivery of invoice emails to customers. |
| Payment portal | Customer-facing refund requests, email verification, and email-change confirmations. See [PaymentPortal](PaymentPortal.md). |

Endpoints follow a stable category-prefix pattern (`/api/<area>/<action>.php`). Specific URLs are scattered across the relevant Core services; treat the tables above as the authoritative list of integration surfaces, not the individual endpoints.

Most endpoints authenticate via the user's license key in an `Authorization: Bearer` header plus an `X-Device-Id` header. The customer payment portal is the exception: it uses short-lived email-delivered codes, because end-customers do not have a license.

## Google Gemini Integration

AI-powered receipt scanning via Gemini 2.5 Flash vision, plus supplier and category matching for receipt processing.

![Gemini Integration](diagrams/integrations/gemini.svg )

## Google Sheets Integration

Export charts to Google Sheets.

![Integrations Overview](diagrams/integrations/google-sheets.svg )

## Exchange Rate Service

Real-time currency conversion.

![Integrations Overview](diagrams/integrations/exchange-rates.svg )
