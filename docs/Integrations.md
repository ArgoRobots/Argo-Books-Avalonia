# Integrations

Argo Books integrates with external services to provide AI-powered features, cloud sync, and real-time data.

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

| Component | Location |
|-----------|----------|
| Interface | `ArgoBooks.Core/Services/IReceiptScannerService.cs` |
| Implementation | `ArgoBooks.Core/Services/AzureReceiptScannerService.cs` |
| Model | `ArgoBooks.Core/Models/OcrData.cs` |
| Model | `ArgoBooks.Core/Models/ReceiptAnalysisRequest.cs` |

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

| Component | Location |
|-----------|----------|
| Interface | `ArgoBooks.Core/Services/IOpenAiService.cs` |
| Implementation | `ArgoBooks.Core/Services/OpenAiService.cs` |

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

| Component | Location |
|-----------|----------|
| Service | `ArgoBooks.Core/Services/GoogleSheetsService.cs` |
| Credentials | `ArgoBooks.Core/Services/GoogleCredentialsManager.cs` |

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

| Component | Location |
|-----------|----------|
| Service | `ArgoBooks.Core/Services/ExchangeRateService.cs` |
| Cache | `ArgoBooks.Core/Services/ExchangeRateCache.cs` |
| Model | `ArgoBooks.Core/Models/CurrencyInfo.cs` |
| Model | `ArgoBooks.Core/Models/MonetaryValue.cs` |

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

## Spreadsheet Import/Export

Data exchange via Excel and CSV files.

| Component | Location |
|-----------|----------|
| Import | `ArgoBooks.Core/Services/SpreadsheetImportService.cs` |
| Export | `ArgoBooks.Core/Services/SpreadsheetExportService.cs` |
| Chart Export | `ArgoBooks.Core/Services/ChartExcelExportService.cs` |
| Validation | `ArgoBooks.Core/Models/ImportValidationResult.cs` |

### Features
- Excel (.xlsx) import/export
- CSV import/export
- Bulk data operations
- Import validation with error reporting
- Chart data export

### Import Validation Flow

```
┌─────────────────────────────────────────────────────────────────┐
│                    DIAGRAM PLACEHOLDER                          │
│                                                                 │
│  Import Validation Flow (ImportValidation.svg)                  │
│                                                                 │
│  ┌─────────────────┐                                           │
│  │ Select File     │                                           │
│  │ (.xlsx / .csv)  │                                           │
│  └────────┬────────┘                                           │
│           │                                                     │
│           ▼                                                     │
│  ┌─────────────────┐                                           │
│  │ Parse File      │                                           │
│  │ Structure       │                                           │
│  └────────┬────────┘                                           │
│           │                                                     │
│           ▼                                                     │
│  ┌─────────────────┐                                           │
│  │ Validate Each   │                                           │
│  │ Row             │                                           │
│  └────────┬────────┘                                           │
│           │                                                     │
│    ┌──────┴──────┐                                             │
│    ▼             ▼                                             │
│  ┌─────────┐  ┌─────────────┐                                 │
│  │ Valid   │  │ Has Errors  │                                 │
│  │ Rows    │  │ (Skip/Fix)  │                                 │
│  └────┬────┘  └──────┬──────┘                                 │
│       │              │                                         │
│       ▼              ▼                                         │
│  ┌───────────────────────────┐                                 │
│  │  ImportValidationResult   │                                 │
│  │  • ValidRows[]            │                                 │
│  │  • Errors[]               │                                 │
│  │  • Warnings[]             │                                 │
│  └───────────────────────────┘                                 │
│              │                                                  │
│              ▼                                                  │
│  ┌─────────────────┐                                           │
│  │ Preview &       │                                           │
│  │ Confirm Import  │                                           │
│  └─────────────────┘                                           │
│                                                                 │
│  Show: Validation workflow with error handling                  │
│  Include: Row-level validation, result structure                │
└─────────────────────────────────────────────────────────────────┘
```

## Configuration

API keys and credentials are stored in a `.env` file:

```
AZURE_FORM_RECOGNIZER_ENDPOINT=...
AZURE_FORM_RECOGNIZER_KEY=...
OPENAI_API_KEY=...
GOOGLE_CLIENT_ID=...
GOOGLE_CLIENT_SECRET=...
```

> **Note**: The `.env` file is not committed to source control and must be obtained separately.
