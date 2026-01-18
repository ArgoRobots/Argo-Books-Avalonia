# Integrations

Argo Books integrates with external services to provide AI-powered features and real-time data.

## Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                    DIAGRAM PLACEHOLDER                          │
│                                                                 │
│  External Integrations (Integrations.svg)                       │
│                                                                 │
│                    ┌─────────────────┐                         │
│                    │   Argo Books    │                         │
│                    └────────┬────────┘                         │
│                             │                                   │
│       ┌─────────────────────┼─────────────────────┐            │
│       │          │          │          │          │            │
│       ▼          ▼          ▼          ▼          ▼            │
│  ┌─────────┐┌─────────┐┌─────────┐┌─────────┐┌─────────┐      │
│  │  Azure  ││ OpenAI  ││ Google  ││Exchange ││ Stripe/ │      │
│  │Cognitive││   API   ││ Sheets  ││  Rate   ││ PayPal  │      │
│  │Services ││         ││   API   ││  APIs   ││(Future) │      │
│  └─────────┘└─────────┘└─────────┘└─────────┘└─────────┘      │
│      │           │          │          │          │            │
│      ▼           ▼          ▼          ▼          ▼            │
│  ┌─────────┐┌─────────┐┌─────────┐┌─────────┐┌─────────┐      │
│  │ Receipt ││   AI    ││  Data   ││Currency ││ Online  │      │
│  │Scanning ││Insights ││  Sync   ││Conversion│ Payments│      │
│  └─────────┘└─────────┘└─────────┘└─────────┘└─────────┘      │
│                                                                 │
│  Show: All external service integrations                        │
│  Include: Purpose of each integration                           │
└─────────────────────────────────────────────────────────────────┘
```

## Azure Receipt Scanning

AI-powered OCR for receipt processing.



### Features
- Azure Cognitive Services Form Recognizer
- 99% accuracy on receipt data extraction
- Automatic field extraction:
  - Vendor name
  - Date/time
  - Total amount
  - Line items
  - Tax
  - Payment method

### Receipt Processing Pipeline

```
┌─────────────────────────────────────────────────────────────────┐
│                    DIAGRAM PLACEHOLDER                          │
│                                                                 │
│  Receipt Processing Pipeline (ReceiptPipeline.svg)              │
│                                                                 │
│  ┌─────────────┐                                               │
│  │ Image Input │                                               │
│  │ (Photo/Scan)│                                               │
│  └──────┬──────┘                                               │
│         │                                                       │
│         ▼                                                       │
│  ┌─────────────┐    ┌──────────────────────────────────────┐  │
│  │  Prepare    │    │         Azure Cognitive Services      │  │
│  │  Request    │───▶│  ┌────────────────────────────────┐  │  │
│  └─────────────┘    │  │     Form Recognizer API        │  │  │
│                     │  │  ┌──────────────────────────┐  │  │  │
│                     │  │  │  Pre-built Receipt Model │  │  │  │
│                     │  │  └──────────────────────────┘  │  │  │
│                     │  └────────────────────────────────┘  │  │
│                     └───────────────┬──────────────────────┘  │
│                                     │                          │
│                                     ▼                          │
│  ┌─────────────┐    ┌─────────────────────────────────────┐   │
│  │   OcrData   │◀───│  Extracted Fields:                  │   │
│  │   Result    │    │  • MerchantName    • Date           │   │
│  └──────┬──────┘    │  • Total           • LineItems[]    │   │
│         │           │  • Tax             • PaymentMethod  │   │
│         │           └─────────────────────────────────────┘   │
│         ▼                                                      │
│  ┌─────────────┐                                              │
│  │ Auto-fill   │                                              │
│  │ Expense Form│                                              │
│  └─────────────┘                                              │
│                                                                │
│  Show: End-to-end receipt processing                           │
│  Include: Azure service interaction, field extraction          │
└─────────────────────────────────────────────────────────────────┘
```

## OpenAI Integration

AI-powered business insights and suggestions.

### Features
- Business insights generation
- Natural language analysis
- Anomaly explanation
- Recommendation generation
- Trend interpretation

### OpenAI Integration Flow

```
┌─────────────────────────────────────────────────────────────────┐
│                    DIAGRAM PLACEHOLDER                          │
│                                                                 │
│  OpenAI Integration Flow (OpenAIFlow.svg)                       │
│                                                                 │
│  ┌─────────────────┐                                           │
│  │  Business Data  │                                           │
│  │ (Aggregated)    │                                           │
│  └────────┬────────┘                                           │
│           │                                                     │
│           ▼                                                     │
│  ┌─────────────────┐    ┌────────────────────────────────┐    │
│  │ Build Context   │    │         OpenAI API             │    │
│  │ + Prompt        │───▶│  ┌──────────────────────────┐ │    │
│  └─────────────────┘    │  │      GPT-4 / GPT-3.5     │ │    │
│                         │  └──────────────────────────┘ │    │
│                         └──────────────┬─────────────────┘    │
│                                        │                       │
│                                        ▼                       │
│                         ┌─────────────────────────────────┐   │
│                         │         AI Response             │   │
│                         │  • Insights                     │   │
│                         │  • Recommendations              │   │
│                         │  • Anomaly explanations         │   │
│                         └─────────────────────────────────┘   │
│                                        │                       │
│                                        ▼                       │
│                         ┌─────────────────────────────────┐   │
│                         │      Insights Panel UI          │   │
│                         └─────────────────────────────────┘   │
│                                                                │
│  Show: Data flow to OpenAI and response handling               │
│  Include: Context building, API call, UI display               │
└─────────────────────────────────────────────────────────────────┘
```

## Google Sheets Integration

Cloud sync and data backup via Google Sheets.

### Features
- OAuth 2.0 authentication
- Data export to Google Sheets
- Cloud backup functionality
- Spreadsheet formatting

### Google Sheets Sync Flow

```
┌─────────────────────────────────────────────────────────────────┐
│                    DIAGRAM PLACEHOLDER                          │
│                                                                 │
│  Google Sheets Sync Flow (GoogleSheetsFlow.svg)                 │
│                                                                 │
│  ┌─────────────┐     ┌─────────────────┐                       │
│  │ User Action │────▶│ Check OAuth     │                       │
│  │ (Export)    │     │ Credentials     │                       │
│  └─────────────┘     └────────┬────────┘                       │
│                               │                                 │
│            ┌──────────────────┼──────────────────┐             │
│            ▼                                     ▼             │
│  ┌─────────────────┐                  ┌─────────────────┐     │
│  │   Has Token     │                  │  Need Login     │     │
│  └────────┬────────┘                  └────────┬────────┘     │
│           │                                    │               │
│           │                                    ▼               │
│           │                           ┌─────────────────┐     │
│           │                           │  Google OAuth   │     │
│           │                           │  Login Flow     │     │
│           │                           └────────┬────────┘     │
│           │                                    │               │
│           └──────────────┬─────────────────────┘              │
│                          ▼                                     │
│                 ┌─────────────────┐                           │
│                 │ Google Sheets   │                           │
│                 │     API         │                           │
│                 └────────┬────────┘                           │
│                          │                                     │
│         ┌────────────────┼────────────────┐                   │
│         ▼                ▼                ▼                   │
│  ┌────────────┐  ┌────────────┐  ┌────────────┐              │
│  │   Create   │  │   Update   │  │    Read    │              │
│  │   Sheet    │  │    Data    │  │    Data    │              │
│  └────────────┘  └────────────┘  └────────────┘              │
│                                                                │
│  Show: OAuth flow and Sheets API operations                    │
│  Include: Authentication, CRUD operations                      │
└─────────────────────────────────────────────────────────────────┘
```

## Exchange Rate Service

Real-time currency conversion.

### Features
- Real-time exchange rates
- Multiple currency support
- Rate caching (15-minute refresh)
- Offline fallback with cached rates
- Currency conversion calculations

### Exchange Rate Flow

```
┌─────────────────────────────────────────────────────────────────┐
│                    DIAGRAM PLACEHOLDER                          │
│                                                                 │
│  Exchange Rate Flow (ExchangeRateFlow.svg)                      │
│                                                                 │
│  ┌─────────────────┐                                           │
│  │ Currency        │                                           │
│  │ Conversion      │                                           │
│  │ Request         │                                           │
│  └────────┬────────┘                                           │
│           │                                                     │
│           ▼                                                     │
│  ┌─────────────────┐    Fresh    ┌─────────────────┐          │
│  │ Check Cache     │────────────▶│ Return Cached   │          │
│  └────────┬────────┘             │ Rate            │          │
│           │ Stale/Miss           └─────────────────┘          │
│           ▼                                                     │
│  ┌─────────────────┐    ┌────────────────────────────┐        │
│  │ Fetch from      │───▶│   Exchange Rate API        │        │
│  │ API             │    │   (e.g., exchangerate.host)│        │
│  └────────┬────────┘    └────────────────────────────┘        │
│           │                                                     │
│           ▼                                                     │
│  ┌─────────────────┐                                           │
│  │ Update Cache    │                                           │
│  │ (15 min TTL)    │                                           │
│  └────────┬────────┘                                           │
│           │                                                     │
│           ▼                                                     │
│  ┌─────────────────┐                                           │
│  │ Return Rate     │                                           │
│  │ & Convert       │                                           │
│  └─────────────────┘                                           │
│                                                                 │
│  Show: Caching strategy for exchange rates                      │
│  Include: Cache hit/miss paths, TTL refresh                     │
└─────────────────────────────────────────────────────────────────┘
```
