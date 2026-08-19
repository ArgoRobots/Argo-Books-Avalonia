# Argo Books

## Introduction

**Argo Books** is easy-to-use accounting software with receipt scanning, predictive analytics, inventory management, and more. Everything you need to run your business. Available for Windows, macOS, and Linux. It's available for download at www.argorobots.com.

## Features

- **AI Receipt Scanning**: Take a photo of any receipt with your phone or upload from your computer. Our AI automatically extracts vendor, date, amount, and line items with 99% accuracy.
- **Invoicing**: Create, send, and track invoices with ease. Customize templates, set payment terms, and get paid faster.
- **Expense & Revenue Tracking**: An intuitive interface makes recording expenses and revenue simple. Smart input validation prevents errors before they happen.
- **Customer Management**: Keep track of customer information, purchase history, and preferences.
- **Predictive Analytics**: Analyzes your historical data to forecast sales trends, identify seasonal patterns, and help you make data-driven decisions.
- **Inventory Management**: Track stock levels, set reorder points, and never run out of your best-selling items. Real-time visibility across all your products.
- **Rental Management**: Manage equipment rentals, track availability, and handle bookings with ease.
- **Security**: AES-256 encryption protects all your data. Your data stays on your computer, not in the cloud. Biometric authentication support.

## Technologies Used

- **[C# .NET 10](https://dotnet.microsoft.com/en-us/)**: Core framework for the application's logic and UI.
- **[Avalonia UI](https://avaloniaui.net/)**: Cross-platform UI framework for building native applications on Windows, macOS, and Linux.
- **[LiveCharts2](https://livecharts.dev/)**: Used to visualize data through beautiful and interactive charts.

## Prerequisites

- **.NET 10 SDK**: Make sure you have the latest .NET 10 SDK installed on your machine. You can download it [here](https://dotnet.microsoft.com/en-us/download/dotnet/10.0).
- **JetBrains Rider**: This is the IDE used for Avalonia UI. You can download it [here](https://www.jetbrains.com/rider/).
- **API Keys**: You'll need to obtain the .env file containing the API keys from Evan Di Placido.

## Installation

1. Clone the repository to your local machine.
2. Place the .env file in the project root directory (where the .sln file is located).
3. Build the project by pressing `Ctrl + Shift + B`.

## Running the Application

After building the solution, press F5 to run the application in Debug mode, or `Ctrl + F5` to run it in Release mode.

## Publishing

See [Publishing](docs/Publishing.md) for platform-specific build and packaging instructions.

## Documentation

Reference docs live in [docs/](docs/). Start with [Architecture](docs/Architecture.md) for a map
of the codebase.

### Architecture and data

| Document | Read it when |
|---|---|
| [Architecture](docs/Architecture.md) | Getting oriented in the projects and how they fit together |
| [Data Storage](docs/DataStorage.md) | Working with `.argo` files, the archive format, or saving and loading |
| [Calculations](docs/Calculations.md) | Touching any money figure. The single source of truth for revenue, profit, tax and refunds |
| [Security](docs/Security.md) | Changing encryption, key handling, passwords or biometric unlock |

### Features

| Document | Read it when |
|---|---|
| [Receipt Scanning](docs/ReceiptScanning.md) | Working on scanning, extraction, or supplier and category matching |
| [AI Spreadsheet Import](docs/AISpreadsheetImport.md) | Working on spreadsheet import, column mapping or entity detection |
| [Reports](docs/Reports.md) | Adding or changing report templates, PDF or image output |
| [Insights](docs/Insights.md) | Working on forecasting, anomaly detection or recommendations |
| [Payment Portal](docs/PaymentPortal.md) | Working on the online invoice payment flow |
| [Localization](docs/Localization.md) | Adding translatable strings or changing language handling |
| [License Key](docs/LicenseKey.md) | Working on licensing, activation or premium gating |

### Setup and integrations

| Document | Read it when |
|---|---|
| [Integrations](docs/Integrations.md) | Wiring up an external service |
| [Azure Setup](docs/setup/AzureSetup.md) | Setting up Azure from scratch for builds and signing |
| [Advanced Installer project setup](docs/Advanced%20Installer%20project%20setup.md) | Rebuilding or repairing the Windows installer project |

### Operations

| Document | Read it when |
|---|---|
| [Publishing](docs/Publishing.md) | Building and packaging a release for Windows, Linux or macOS, and signing it |
| [Password recovery](tools/ArgoBooks.Recovery/README.md) | A customer has lost their company file password. Internal, not for customers |

### Sub-projects

| Document | Read it when |
|---|---|
| [ArgoBooks.Mobile](ArgoBooks.Mobile/README.md) | Working on the Android companion app |
| [ArgoBooks.Translations](tools/ArgoBooks.Translations/README.md) | Working on the translation tooling |
