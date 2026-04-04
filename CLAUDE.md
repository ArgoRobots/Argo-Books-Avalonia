# Argo Books — Claude Code Guide

## Workflow Rules

- **Do NOT build or run tests after every change.** Only build/test when explicitly asked.

## Project Overview

Argo Books is a cross-platform desktop accounting application built with C# .NET 10 and Avalonia UI. It provides AI receipt scanning, expense/revenue tracking, predictive analytics, inventory management, rental management, customer management, and invoicing. Data is stored locally in encrypted `.argo` files — no cloud or database required. Available for Windows, macOS, and Linux.

## Build & Run Commands

- **Build:** `dotnet build ArgoBooks.sln`
- **Run desktop (Windows):** `dotnet run --project ArgoBooks.Desktop -f net10.0-windows10.0.17763.0`
- **Run desktop (cross-platform):** `dotnet run --project ArgoBooks.Desktop -f net10.0`
- **Run all tests:** `dotnet test ArgoBooks.Tests`
- **Run single test:** `dotnet test ArgoBooks.Tests --filter "FullyQualifiedName~TestClassName.TestMethodName"`
- **Run test category:** `dotnet test ArgoBooks.Tests --filter "FullyQualifiedName~Namespace"`

Requires .NET 10 SDK. A `.env` file with API keys must be placed at the solution root.

## Architecture

**MVVM pattern** with Avalonia UI. CommunityToolkit.Mvvm provides `[ObservableProperty]`, `[RelayCommand]`, and other source generators.

### Projects

| Project | Role |
|---------|------|
| **ArgoBooks** | UI layer: Views (.axaml), ViewModels, Controls, UI Services |
| **ArgoBooks.Core** | Business logic: Models, Services, Data, Security, Platform abstraction |
| **ArgoBooks.Desktop** | Entry point for Windows/macOS/Linux |
| **ArgoBooks.Browser** | Entry point for WebAssembly |
| **ArgoBooks.Tests** | xUnit tests (references both ArgoBooks and ArgoBooks.Core) |

**Dependency flow:** Desktop/Browser -> ArgoBooks -> ArgoBooks.Core

### Data Storage

File-based, not database. Company data lives in encrypted `.argo` files (AES-256). `CompanyManager` orchestrates file lifecycle: load, save, auto-save, encryption, temp directory management. All data is held in memory for performance.

### Key Patterns

- **Singleton services** via `Microsoft.Extensions.DependencyInjection`
- **Compiled bindings** enabled by default (`AvaloniaUseCompiledBindingsByDefault=true`)
- **Platform abstraction** via `PlatformServiceFactory` with platform-specific implementations (Windows Hello, etc.)
- **Central package versioning** in `Directory.Packages.props`; app version in `Directory.Build.props`
- **Conditional compilation:** `WINDOWS` constant defined when targeting `net10.0-windows`; WebView2 is Windows-only

### Service Layer (ArgoBooks.Core/Services/)

Core business services include: `CompanyManager` (file lifecycle), `EncryptionService` (AES-256), `SpreadsheetImportService`/`SpreadsheetExportService` (Excel/CSV), `OpenAiService` (AI matching), `ProxyReceiptScannerService` (OCR), `InsightsService` (analytics), `ReportRenderer` (PDF via QuestPDF), `LicenseService`, `PaymentPortalService`.

UI services in `ArgoBooks/Services/` handle navigation, theming, localization, undo/redo, modals, and chart loading.

### Models (ArgoBooks.Core/Models/)

Organized by domain: `Entities/`, `Transactions/`, `Invoices/`, `Inventory/`, `Rentals/`, `Reports/`, `Charts/`, `AI/`, `Portal/`, `Insights/`.

## Testing

- **Framework:** xUnit with Coverlet for coverage
- **Test data:** Excel/CSV files in `TestData/` at the solution root
- Tests mirror source structure: `Converters/`, `Data/`, `Models/`, `Services/`, `Utilities/`, `Validation/`, `ViewModels/`, etc.

## Multi-Target Builds

ArgoBooks and ArgoBooks.Core target both `net10.0` and `net10.0-windows10.0.17763.0`. Windows-specific code (WebView2, Windows Hello, DPAPI) is gated behind the `WINDOWS` compilation constant or target framework conditions in csproj files.
