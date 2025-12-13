# Argo Books - Implementation Order

This document describes the recommended order for implementing the Argo Books Avalonia application.

---

## Overview

The implementation is organized into **8 phases**, each building upon the previous. Dependencies are clearly marked to ensure features are built in the correct order.

---

## Phase 1: Project Foundation

**Goal:** Set up the project structure and core infrastructure.

### 1.1 Project Setup
- [ ] Create Avalonia solution with proper project structure
- [ ] Configure .NET 10 target frameworks
- [ ] Set up multi-platform targets (Windows, macOS, Linux, Browser)
- [ ] Configure NuGet packages

**Files to create:**
```
ArgoBooks/
├── ArgoBooks.sln
├── src/
│   ├── ArgoBooks.Desktop/           # Desktop app (Windows, macOS, Linux)
│   │   ├── ArgoBooks.Desktop.csproj
│   │   └── Program.cs
│   ├── ArgoBooks.Browser/           # WebAssembly app
│   │   ├── ArgoBooks.Browser.csproj
│   │   └── Program.cs
│   ├── ArgoBooks.Core/              # Shared business logic
│   │   └── ArgoBooks.Core.csproj
│   └── ArgoBooks.UI/                # Shared UI (Avalonia views)
│       └── ArgoBooks.UI.csproj
└── tests/
    └── ArgoBooks.Tests/
        └── ArgoBooks.Tests.csproj
```

**NuGet Packages:**
```xml
<!-- Core -->
<PackageReference Include="Avalonia" Version="11.*" />
<PackageReference Include="Avalonia.Desktop" Version="11.*" />
<PackageReference Include="Avalonia.Browser" Version="11.*" />
<PackageReference Include="CommunityToolkit.Mvvm" Version="8.*" />

<!-- Data -->
<PackageReference Include="System.Text.Json" Version="9.*" />
<PackageReference Include="System.Formats.Tar" Version="9.*" />

<!-- Charts -->
<PackageReference Include="LiveChartsCore.SkiaSharpView.Avalonia" Version="2.*" />

<!-- PDF -->
<PackageReference Include="QuestPDF" Version="2024.*" />

<!-- Excel -->
<PackageReference Include="ClosedXML" Version="0.102.*" />

<!-- Auto-Update (Desktop only) -->
<PackageReference Include="NetSparkleUpdater.UI.Avalonia" Version="2.*" />

<!-- Google APIs -->
<PackageReference Include="Google.Apis.Sheets.v4" Version="1.*" />
<PackageReference Include="Google.Apis.Drive.v3" Version="1.*" />
```

### 1.2 Core Services Architecture
- [ ] Create service interfaces
- [ ] Set up dependency injection
- [ ] Create base ViewModels

**Files to create:**
```
ArgoBooks.Core/
├── Interfaces/
│   ├── IFileService.cs
│   ├── IEncryptionService.cs
│   ├── ICompanyService.cs
│   ├── ISettingsService.cs
│   └── INavigationService.cs
├── Services/
│   └── (implementations in later phases)
└── ViewModels/
    └── ViewModelBase.cs
```

### 1.3 Theme System
- [ ] Create theme definitions (Light, Dark)
- [ ] Create color palette (accent colors)
- [ ] Set up system theme detection
- [ ] Create base styles

**Files to create:**
```
ArgoBooks.UI/
├── Themes/
│   ├── LightTheme.axaml
│   ├── DarkTheme.axaml
│   └── Colors.axaml
├── Styles/
│   ├── BaseStyles.axaml
│   ├── ButtonStyles.axaml
│   ├── TextStyles.axaml
│   └── CardStyles.axaml
└── App.axaml
```

**Dependencies:** None

---

## Phase 2: Data Layer

**Goal:** Implement data models, file operations, and encryption.

### 2.1 Data Models
- [ ] Create all entity models
- [ ] Create all transaction models
- [ ] Create enumerations
- [ ] Add JSON serialization attributes

**Files to create:**
```
ArgoBooks.Core/
├── Models/
│   ├── Entities/
│   │   ├── Customer.cs
│   │   ├── Product.cs
│   │   ├── Supplier.cs
│   │   ├── Employee.cs
│   │   ├── Department.cs
│   │   ├── Category.cs
│   │   ├── Accountant.cs
│   │   └── Location.cs
│   ├── Transactions/
│   │   ├── Sale.cs
│   │   ├── Purchase.cs
│   │   ├── Invoice.cs
│   │   ├── Payment.cs
│   │   └── RecurringInvoice.cs
│   ├── Inventory/
│   │   ├── InventoryItem.cs
│   │   ├── StockAdjustment.cs
│   │   ├── StockTransfer.cs
│   │   └── PurchaseOrder.cs
│   ├── Rentals/
│   │   ├── RentalItem.cs
│   │   └── RentalRecord.cs
│   ├── Tracking/
│   │   ├── Return.cs
│   │   ├── LostDamaged.cs
│   │   └── Receipt.cs
│   ├── Reports/
│   │   └── ReportTemplate.cs
│   └── Common/
│       ├── Address.cs
│       ├── LineItem.cs
│       └── OcrData.cs
├── Enums/
│   ├── InvoiceStatus.cs
│   ├── RentalStatus.cs
│   ├── InventoryStatus.cs
│   ├── TransferStatus.cs
│   ├── ReturnStatus.cs
│   ├── EmployeeStatus.cs
│   ├── PaymentMethod.cs
│   └── Frequency.cs
```

### 2.2 Company Data Container
- [ ] Create CompanyData class to hold all data
- [ ] Create ID generator service
- [ ] Create data validation

**Files to create:**
```
ArgoBooks.Core/
├── Data/
│   ├── CompanyData.cs           # Main container for all data
│   ├── CompanySettings.cs       # Per-company settings
│   └── IdGenerator.cs           # Sequential ID generation
├── Validation/
│   └── DataValidator.cs
```

### 2.3 File Operations
- [ ] Implement TAR creation/extraction
- [ ] Implement GZip compression
- [ ] Implement file footer handling
- [ ] Implement file save/open

**Files to create:**
```
ArgoBooks.Core/
├── Services/
│   ├── FileService.cs
│   ├── CompressionService.cs
│   └── FooterService.cs
```

### 2.4 Encryption
- [ ] Implement AES-256-GCM encryption
- [ ] Implement password hashing (PBKDF2)
- [ ] Implement key derivation

**Files to create:**
```
ArgoBooks.Core/
├── Services/
│   └── EncryptionService.cs
├── Security/
│   ├── PasswordHasher.cs
│   └── KeyDerivation.cs
```

### 2.5 Global Settings
- [ ] Implement global settings storage
- [ ] Implement recent companies list
- [ ] Platform-specific paths

**Files to create:**
```
ArgoBooks.Core/
├── Services/
│   └── GlobalSettingsService.cs
├── Platform/
│   ├── IPlatformService.cs
│   ├── WindowsPlatformService.cs
│   ├── MacPlatformService.cs
│   ├── LinuxPlatformService.cs
│   └── BrowserPlatformService.cs
```

**Dependencies:** Phase 1

---

## Phase 3: Core UI Components

**Goal:** Build reusable UI components used throughout the app.

### 3.1 Base Components
- [ ] Custom Button styles
- [ ] Custom TextBox with validation
- [ ] Custom ComboBox
- [ ] Custom DatePicker
- [ ] Badge component
- [ ] Loading spinner

**Files to create:**
```
ArgoBooks.UI/
├── Controls/
│   ├── ArgoButton.axaml / .axaml.cs
│   ├── ArgoTextBox.axaml / .axaml.cs
│   ├── ArgoComboBox.axaml / .axaml.cs
│   ├── ArgoDatePicker.axaml / .axaml.cs
│   ├── Badge.axaml / .axaml.cs
│   ├── LoadingSpinner.axaml / .axaml.cs
│   └── LoadingPanel.axaml / .axaml.cs
```

### 3.2 Searchable Dropdown
- [ ] Filterable dropdown with search
- [ ] Keyboard navigation
- [ ] "Add new" option support

**Files to create:**
```
ArgoBooks.UI/
├── Controls/
│   └── SearchableDropdown.axaml / .axaml.cs
```

### 3.3 Data Table Component
- [ ] Sortable columns
- [ ] Row selection
- [ ] Pagination
- [ ] Search/filter
- [ ] Row styling (alternating, highlight)

**Files to create:**
```
ArgoBooks.UI/
├── Controls/
│   └── DataTable.axaml / .axaml.cs
├── ViewModels/
│   └── DataTableViewModel.cs
```

### 3.4 Modal System
- [ ] Modal overlay
- [ ] Modal container
- [ ] Modal service for opening/closing

**Files to create:**
```
ArgoBooks.UI/
├── Controls/
│   ├── ModalOverlay.axaml / .axaml.cs
│   └── ModalContainer.axaml / .axaml.cs
├── Services/
│   └── ModalService.cs
```

### 3.5 Stat Card Component
- [ ] Icon with colored background
- [ ] Value and label display
- [ ] Percentage change indicator

**Files to create:**
```
ArgoBooks.UI/
├── Controls/
│   └── StatCard.axaml / .axaml.cs
```

### 3.6 Custom Message Box
- [ ] Info, Warning, Error, Success icons
- [ ] Configurable buttons
- [ ] Return result

**Files to create:**
```
ArgoBooks.UI/
├── Dialogs/
│   ├── MessageBox.axaml / .axaml.cs
│   └── MessageBoxViewModel.cs
```

**Dependencies:** Phase 1

---

## Phase 4: App Shell & Navigation

**Goal:** Build the main application shell, sidebar, and navigation system.

### 4.1 Main Window
- [ ] Window chrome customization
- [ ] Minimum size constraints
- [ ] State persistence (position, size)

**Files to create:**
```
ArgoBooks.UI/
├── Views/
│   └── MainWindow.axaml / .axaml.cs
├── ViewModels/
│   └── MainWindowViewModel.cs
```

### 4.2 Sidebar Navigation
- [ ] Collapsible sidebar
- [ ] Section grouping (Main, Transactions, etc.)
- [ ] Active item highlighting
- [ ] Module visibility based on settings

**Files to create:**
```
ArgoBooks.UI/
├── Controls/
│   ├── Sidebar.axaml / .axaml.cs
│   ├── SidebarSection.axaml / .axaml.cs
│   └── SidebarItem.axaml / .axaml.cs
├── ViewModels/
│   └── SidebarViewModel.cs
```

### 4.3 Header
- [ ] Company name display
- [ ] Global search
- [ ] Quick actions button
- [ ] Notifications button
- [ ] User menu button

**Files to create:**
```
ArgoBooks.UI/
├── Controls/
│   └── Header.axaml / .axaml.cs
├── ViewModels/
│   └── HeaderViewModel.cs
```

### 4.4 Navigation Service
- [ ] Page navigation
- [ ] Navigation history
- [ ] Parameter passing

**Files to create:**
```
ArgoBooks.Core/
├── Services/
│   └── NavigationService.cs
```

### 4.5 Quick Actions Panel
- [ ] Searchable action list
- [ ] Grouped actions
- [ ] Keyboard shortcuts

**Files to create:**
```
ArgoBooks.UI/
├── Panels/
│   └── QuickActionsPanel.axaml / .axaml.cs
├── ViewModels/
│   └── QuickActionsViewModel.cs
```

### 4.6 Notification Panel
- [ ] Notification list
- [ ] Mark as read
- [ ] Clear all
- [ ] Link to settings

**Files to create:**
```
ArgoBooks.UI/
├── Panels/
│   └── NotificationPanel.axaml / .axaml.cs
├── ViewModels/
│   └── NotificationPanelViewModel.cs
├── Services/
│   └── NotificationService.cs
```

### 4.7 User Panel
- [ ] User profile display
- [ ] Settings button
- [ ] Help button
- [ ] Logout/Close company

**Files to create:**
```
ArgoBooks.UI/
├── Panels/
│   └── UserPanel.axaml / .axaml.cs
├── ViewModels/
│   └── UserPanelViewModel.cs
```

**Dependencies:** Phase 1, Phase 3

---

## Phase 5: Startup & Company Management

**Goal:** Implement welcome screen, company creation, and file operations UI.

### 5.1 Welcome/Startup Screen
- [ ] Logo and branding
- [ ] Create new company button
- [ ] Open existing company button
- [ ] Recent companies list
- [ ] Recover unsaved work prompt

**Files to create:**
```
ArgoBooks.UI/
├── Views/
│   └── StartupView.axaml / .axaml.cs
├── ViewModels/
│   └── StartupViewModel.cs
```

### 5.2 Company Creation Wizard
- [ ] Company name input
- [ ] Contact information
- [ ] Logo upload
- [ ] Password setup (optional)

**Files to create:**
```
ArgoBooks.UI/
├── Dialogs/
│   └── CreateCompanyDialog.axaml / .axaml.cs
├── ViewModels/
│   └── CreateCompanyViewModel.cs
```

### 5.3 Open Company Dialog
- [ ] File browser integration
- [ ] Password entry (if encrypted)
- [ ] Version compatibility check
- [ ] Migration prompt

**Files to create:**
```
ArgoBooks.UI/
├── Dialogs/
│   └── OpenCompanyDialog.axaml / .axaml.cs
├── ViewModels/
│   └── OpenCompanyViewModel.cs
```

### 5.4 File Menu
- [ ] New, Open, Save, Save As, Close
- [ ] Recent companies submenu
- [ ] Import/Export
- [ ] Show in folder

**Files to create:**
```
ArgoBooks.UI/
├── Menus/
│   └── FileMenu.axaml / .axaml.cs
├── ViewModels/
│   └── FileMenuViewModel.cs
```

### 5.5 Company Settings Modal
- [ ] Edit company info
- [ ] Change logo
- [ ] Rename company

**Files to create:**
```
ArgoBooks.UI/
├── Dialogs/
│   └── CompanySettingsDialog.axaml / .axaml.cs
├── ViewModels/
│   └── CompanySettingsViewModel.cs
```

### 5.6 Recovery Dialog
- [ ] List unsaved companies
- [ ] Recover or discard options
- [ ] Save location picker

**Files to create:**
```
ArgoBooks.UI/
├── Dialogs/
│   └── RecoveryDialog.axaml / .axaml.cs
├── ViewModels/
│   └── RecoveryViewModel.cs
```

**Dependencies:** Phase 2, Phase 3, Phase 4

---

## Phase 6: Core Feature Pages

**Goal:** Implement the main feature pages in dependency order.

### 6.1 Categories (First - no dependencies)
- [ ] Category list with tabs (Purchase/Sales/Rental)
- [ ] Add/Edit category modal
- [ ] Hierarchical categories
- [ ] Color/icon picker

**Files to create:**
```
ArgoBooks.UI/
├── Views/
│   └── CategoriesView.axaml / .axaml.cs
├── ViewModels/
│   └── CategoriesViewModel.cs
├── Dialogs/
│   └── CategoryDialog.axaml / .axaml.cs
```

### 6.2 Locations (For inventory)
- [ ] Location list
- [ ] Add/Edit location modal
- [ ] Capacity tracking

**Files to create:**
```
ArgoBooks.UI/
├── Views/
│   └── LocationsView.axaml / .axaml.cs
├── ViewModels/
│   └── LocationsViewModel.cs
├── Dialogs/
│   └── LocationDialog.axaml / .axaml.cs
```

### 6.3 Departments (For employees)
- [ ] Department list
- [ ] Add/Edit department modal
- [ ] Department head assignment

**Files to create:**
```
ArgoBooks.UI/
├── Views/
│   └── DepartmentsView.axaml / .axaml.cs
├── ViewModels/
│   └── DepartmentsViewModel.cs
├── Dialogs/
│   └── DepartmentDialog.axaml / .axaml.cs
```

### 6.4 Accountants
- [ ] Accountant list
- [ ] Add/Edit accountant modal

**Files to create:**
```
ArgoBooks.UI/
├── Views/
│   └── AccountantsView.axaml / .axaml.cs
├── ViewModels/
│   └── AccountantsViewModel.cs
├── Dialogs/
│   └── AccountantDialog.axaml / .axaml.cs
```

### 6.5 Suppliers (Depends on: Categories)
- [ ] Supplier list with search/filter
- [ ] Add/Edit supplier modal
- [ ] Supplier details

**Files to create:**
```
ArgoBooks.UI/
├── Views/
│   └── SuppliersView.axaml / .axaml.cs
├── ViewModels/
│   └── SuppliersViewModel.cs
├── Dialogs/
│   └── SupplierDialog.axaml / .axaml.cs
```

### 6.6 Customers
- [ ] Customer list with search/filter
- [ ] Add/Edit customer modal
- [ ] Customer details

**Files to create:**
```
ArgoBooks.UI/
├── Views/
│   └── CustomersView.axaml / .axaml.cs
├── ViewModels/
│   └── CustomersViewModel.cs
├── Dialogs/
│   └── CustomerDialog.axaml / .axaml.cs
```

### 6.7 Products/Services (Depends on: Categories, Suppliers)
- [ ] Product list with search/filter
- [ ] Add/Edit product modal
- [ ] Inventory tracking toggle

**Files to create:**
```
ArgoBooks.UI/
├── Views/
│   └── ProductsView.axaml / .axaml.cs
├── ViewModels/
│   └── ProductsViewModel.cs
├── Dialogs/
│   └── ProductDialog.axaml / .axaml.cs
```

### 6.8 Employees (Depends on: Departments)
- [ ] Employee list
- [ ] Payroll tab
- [ ] Add/Edit employee modal
- [ ] Run payroll modal

**Files to create:**
```
ArgoBooks.UI/
├── Views/
│   └── EmployeesView.axaml / .axaml.cs
├── ViewModels/
│   └── EmployeesViewModel.cs
├── Dialogs/
│   ├── EmployeeDialog.axaml / .axaml.cs
│   └── PayrollDialog.axaml / .axaml.cs
```

**Dependencies:** Phase 4, Phase 5

---

## Phase 7: Transaction & Inventory Pages

**Goal:** Implement transaction recording and inventory management.

### 7.1 Expenses (Depends on: Categories, Suppliers, Accountants)
- [ ] Expense list with stats
- [ ] Add/Edit expense modal
- [ ] Receipt attachment

**Files to create:**
```
ArgoBooks.UI/
├── Views/
│   └── ExpensesView.axaml / .axaml.cs
├── ViewModels/
│   └── ExpensesViewModel.cs
├── Dialogs/
│   └── ExpenseDialog.axaml / .axaml.cs
```

### 7.2 Revenue/Sales (Depends on: Customers, Products, Accountants)
- [ ] Sales list with stats
- [ ] Add/Edit sale modal
- [ ] Line items management

**Files to create:**
```
ArgoBooks.UI/
├── Views/
│   └── RevenueView.axaml / .axaml.cs
├── ViewModels/
│   └── RevenueViewModel.cs
├── Dialogs/
│   └── SaleDialog.axaml / .axaml.cs
```

### 7.3 Invoices (Depends on: Customers, Products)
- [ ] Invoice list with tabs (All, Drafts, Recurring)
- [ ] Invoice stats
- [ ] Create invoice modal
- [ ] Recurring invoice setup modal
- [ ] Invoice history modal
- [ ] Payment reminders

**Files to create:**
```
ArgoBooks.UI/
├── Views/
│   └── InvoicesView.axaml / .axaml.cs
├── ViewModels/
│   └── InvoicesViewModel.cs
├── Dialogs/
│   ├── InvoiceDialog.axaml / .axaml.cs
│   ├── RecurringInvoiceDialog.axaml / .axaml.cs
│   └── InvoiceHistoryDialog.axaml / .axaml.cs
```

### 7.4 Payments (Depends on: Invoices, Customers)
- [ ] Payment list
- [ ] Record payment modal
- [ ] Partial payment support

**Files to create:**
```
ArgoBooks.UI/
├── Views/
│   └── PaymentsView.axaml / .axaml.cs
├── ViewModels/
│   └── PaymentsViewModel.cs
├── Dialogs/
│   └── PaymentDialog.axaml / .axaml.cs
```

### 7.5 Stock Levels (Depends on: Products, Locations)
- [ ] Inventory list with tabs (All, Low, Out, Overstock)
- [ ] Inventory stats
- [ ] Add inventory item modal
- [ ] Edit inventory item modal
- [ ] View details modal

**Files to create:**
```
ArgoBooks.UI/
├── Views/
│   └── StockLevelsView.axaml / .axaml.cs
├── ViewModels/
│   └── StockLevelsViewModel.cs
├── Dialogs/
│   ├── InventoryItemDialog.axaml / .axaml.cs
│   └── InventoryDetailsDialog.axaml / .axaml.cs
```

### 7.6 Stock Adjustments (Depends on: Stock Levels)
- [ ] Adjust stock modal
- [ ] Adjustment history view

**Files to create:**
```
ArgoBooks.UI/
├── Views/
│   └── StockAdjustmentsView.axaml / .axaml.cs
├── ViewModels/
│   └── StockAdjustmentsViewModel.cs
├── Dialogs/
│   └── AdjustStockDialog.axaml / .axaml.cs
```

### 7.7 Stock Transfers (Depends on: Stock Levels, Locations)
- [ ] Transfer list
- [ ] Create transfer modal
- [ ] Transfer status tracking

**Files to create:**
```
ArgoBooks.UI/
├── Views/
│   └── StockTransfersView.axaml / .axaml.cs
├── ViewModels/
│   └── StockTransfersViewModel.cs
├── Dialogs/
│   └── TransferDialog.axaml / .axaml.cs
```

### 7.8 Purchase Orders (Depends on: Products, Suppliers)
- [ ] PO list
- [ ] Create PO modal
- [ ] Bulk reorder modal
- [ ] Receive inventory

**Files to create:**
```
ArgoBooks.UI/
├── Views/
│   └── PurchaseOrdersView.axaml / .axaml.cs
├── ViewModels/
│   └── PurchaseOrdersViewModel.cs
├── Dialogs/
│   ├── PurchaseOrderDialog.axaml / .axaml.cs
│   └── BulkReorderDialog.axaml / .axaml.cs
```

### 7.9 Rental Inventory (Depends on: Suppliers)
- [ ] Rental items list with stats
- [ ] Add/Edit rental item modal
- [ ] Rent out modal

**Files to create:**
```
ArgoBooks.UI/
├── Views/
│   └── RentalInventoryView.axaml / .axaml.cs
├── ViewModels/
│   └── RentalInventoryViewModel.cs
├── Dialogs/
│   ├── RentalItemDialog.axaml / .axaml.cs
│   └── RentOutDialog.axaml / .axaml.cs
```

### 7.10 Rental Records (Depends on: Rental Inventory, Customers)
- [ ] Rental records list
- [ ] Return rental modal

**Files to create:**
```
ArgoBooks.UI/
├── Views/
│   └── RentalRecordsView.axaml / .axaml.cs
├── ViewModels/
│   └── RentalRecordsViewModel.cs
├── Dialogs/
│   └── ReturnRentalDialog.axaml / .axaml.cs
```

### 7.11 Returns (Depends on: Sales, Products)
- [ ] Returns list
- [ ] Record return modal
- [ ] Undo return

**Files to create:**
```
ArgoBooks.UI/
├── Views/
│   └── ReturnsView.axaml / .axaml.cs
├── ViewModels/
│   └── ReturnsViewModel.cs
├── Dialogs/
│   └── ReturnDialog.axaml / .axaml.cs
```

### 7.12 Lost/Damaged (Depends on: Products, Stock Levels)
- [ ] Lost/damaged list
- [ ] Record loss modal
- [ ] Undo loss

**Files to create:**
```
ArgoBooks.UI/
├── Views/
│   └── LostDamagedView.axaml / .axaml.cs
├── ViewModels/
│   └── LostDamagedViewModel.cs
├── Dialogs/
│   └── LostDamagedDialog.axaml / .axaml.cs
```

### 7.13 Receipts (Depends on: Expenses, Revenue)
- [ ] Receipt archive grid
- [ ] Receipt preview modal
- [ ] AI scan receipt feature
- [ ] Filter receipts modal

**Files to create:**
```
ArgoBooks.UI/
├── Views/
│   └── ReceiptsView.axaml / .axaml.cs
├── ViewModels/
│   └── ReceiptsViewModel.cs
├── Dialogs/
│   ├── ReceiptPreviewDialog.axaml / .axaml.cs
│   └── ScanReceiptDialog.axaml / .axaml.cs
├── Services/
│   └── OcrService.cs
```

**Dependencies:** Phase 6

---

## Phase 8: Dashboard, Analytics & Advanced Features

**Goal:** Complete the application with dashboard, analytics, and advanced features.

### 8.1 Dashboard (Depends on: All transaction pages)
- [ ] Stats cards (Revenue, Expenses, Profit, Customers)
- [ ] Quick actions panel
- [ ] Revenue chart (LiveCharts2)
- [ ] Expense breakdown chart
- [ ] Recent transactions table
- [ ] AI insights panel
- [ ] Active rentals table

**Files to create:**
```
ArgoBooks.UI/
├── Views/
│   └── DashboardView.axaml / .axaml.cs
├── ViewModels/
│   └── DashboardViewModel.cs
├── Controls/
│   ├── RevenueChart.axaml / .axaml.cs
│   ├── ExpenseBreakdownChart.axaml / .axaml.cs
│   └── InsightsPanel.axaml / .axaml.cs
```

### 8.2 Analytics Page
- [ ] Time range selector
- [ ] Multiple chart types
- [ ] Trend analysis
- [ ] Comparison views

**Files to create:**
```
ArgoBooks.UI/
├── Views/
│   └── AnalyticsView.axaml / .axaml.cs
├── ViewModels/
│   └── AnalyticsViewModel.cs
```

### 8.3 Insights Page
- [ ] AI-generated insights
- [ ] Recommendations
- [ ] Anomaly detection

**Files to create:**
```
ArgoBooks.UI/
├── Views/
│   └── InsightsView.axaml / .axaml.cs
├── ViewModels/
│   └── InsightsViewModel.cs
├── Services/
│   └── InsightsService.cs
```

### 8.4 Report Generator
- [ ] Report layout designer
- [ ] Element drag-and-drop
- [ ] Report preview
- [ ] Export (PDF, PNG, JPG)
- [ ] Template save/load

**Files to create:**
```
ArgoBooks.UI/
├── Views/
│   └── ReportGeneratorView.axaml / .axaml.cs
├── ViewModels/
│   └── ReportGeneratorViewModel.cs
├── Controls/
│   ├── ReportCanvas.axaml / .axaml.cs
│   └── ReportElement.axaml / .axaml.cs
├── Dialogs/
│   ├── ReportDataSelectionDialog.axaml / .axaml.cs
│   ├── PageSettingsDialog.axaml / .axaml.cs
│   └── ExportDialog.axaml / .axaml.cs
├── Services/
│   └── ReportService.cs
```

### 8.5 Settings
- [ ] General settings tab
- [ ] Features toggle tab
- [ ] Notifications tab
- [ ] Appearance tab
- [ ] Security tab

**Files to create:**
```
ArgoBooks.UI/
├── Dialogs/
│   └── SettingsDialog.axaml / .axaml.cs
├── ViewModels/
│   └── SettingsViewModel.cs
├── Controls/
│   ├── GeneralSettingsTab.axaml / .axaml.cs
│   ├── FeaturesSettingsTab.axaml / .axaml.cs
│   ├── NotificationsSettingsTab.axaml / .axaml.cs
│   ├── AppearanceSettingsTab.axaml / .axaml.cs
│   └── SecuritySettingsTab.axaml / .axaml.cs
```

### 8.6 Import/Export
- [ ] Import wizard (Excel)
- [ ] Export wizard
- [ ] Backup creation
- [ ] Backup restoration

**Files to create:**
```
ArgoBooks.UI/
├── Dialogs/
│   ├── ImportDialog.axaml / .axaml.cs
│   └── ExportDialog.axaml / .axaml.cs
├── ViewModels/
│   ├── ImportViewModel.cs
│   └── ExportViewModel.cs
├── Services/
│   ├── ImportService.cs
│   └── ExportService.cs
```

### 8.7 Help System
- [ ] Help panel
- [ ] Keyboard shortcuts reference
- [ ] Getting started guide links

**Files to create:**
```
ArgoBooks.UI/
├── Panels/
│   └── HelpPanel.axaml / .axaml.cs
├── ViewModels/
│   └── HelpPanelViewModel.cs
```

### 8.8 Biometric Authentication (Platform-specific)
- [ ] Windows Hello integration
- [ ] macOS Touch ID integration
- [ ] Fallback to password

**Files to create:**
```
ArgoBooks.Core/
├── Platform/
│   ├── IBiometricService.cs
│   ├── WindowsBiometricService.cs
│   ├── MacBiometricService.cs
│   └── FallbackBiometricService.cs
```

### 8.9 Email Service
- [ ] PHP API integration
- [ ] Email queue
- [ ] Send invoice emails
- [ ] Payment reminders

**Files to create:**
```
ArgoBooks.Core/
├── Services/
│   └── EmailService.cs
```

### 8.10 Recurring Invoice Checker
- [ ] Check on startup
- [ ] Show due invoices UI
- [ ] User confirmation required

**Files to create:**
```
ArgoBooks.UI/
├── Dialogs/
│   └── RecurringInvoicesDialog.axaml / .axaml.cs
├── ViewModels/
│   └── RecurringInvoicesViewModel.cs
├── Services/
│   └── RecurringInvoiceService.cs
```

### 8.11 Licensing System
- [ ] License validation service (web API)
- [ ] Upgrade modal UI
- [ ] License key entry modal
- [ ] Feature gating based on tier
- [ ] Standard/Premium tier detection

**Files to create:**
```
ArgoBooks.Core/
├── Services/
│   └── LicenseService.cs
├── Models/
│   └── LicenseInfo.cs
ArgoBooks.UI/
├── Dialogs/
│   ├── UpgradeDialog.axaml / .axaml.cs
│   └── LicenseKeyDialog.axaml / .axaml.cs
├── ViewModels/
│   └── LicenseViewModel.cs
```

### 8.12 Auto-Update System (Desktop Only)
- [ ] NetSparkle integration
- [ ] Check for updates on startup
- [ ] Update notification UI
- [ ] Download and install updates

**Files to create:**
```
ArgoBooks.Core/
├── Services/
│   └── UpdateService.cs
ArgoBooks.Desktop/
├── Services/
│   └── NetSparkleUpdateService.cs
```

### 8.13 Google Sheets Export
- [ ] Google OAuth 2.0 authentication
- [ ] Export chart data to Sheets
- [ ] Create formatted spreadsheets

**Files to create:**
```
ArgoBooks.Core/
├── Services/
│   └── GoogleSheetsService.cs
├── Auth/
│   └── GoogleCredentialsManager.cs
```

### 8.14 Exchange Rate Service
- [ ] OpenExchangeRates API integration
- [ ] Exchange rate caching
- [ ] Currency conversion utilities

**Files to create:**
```
ArgoBooks.Core/
├── Services/
│   └── ExchangeRateService.cs
├── Models/
│   └── ExchangeRateCache.cs
```

### 8.15 Password Manager
- [ ] Password vault UI
- [ ] Add/Edit/Delete passwords
- [ ] Encrypted storage
- [ ] Copy to clipboard

**Files to create:**
```
ArgoBooks.UI/
├── Views/
│   └── PasswordManagerView.axaml / .axaml.cs
├── ViewModels/
│   └── PasswordManagerViewModel.cs
├── Dialogs/
│   └── PasswordEntryDialog.axaml / .axaml.cs
ArgoBooks.Core/
├── Services/
│   └── PasswordVaultService.cs
├── Models/
│   └── StoredPassword.cs
```

### 8.16 Log & Debug System
- [ ] Log service with categories
- [ ] Log viewer UI
- [ ] Save/export logs
- [ ] Error tracking

**Files to create:**
```
ArgoBooks.Core/
├── Services/
│   └── LogService.cs
├── Models/
│   └── LogEntry.cs
ArgoBooks.UI/
├── Views/
│   └── LogViewerView.axaml / .axaml.cs
├── ViewModels/
│   └── LogViewerViewModel.cs
```

**Dependencies:** Phase 7

---

## Implementation Summary

### Phase Order
```
Phase 1: Project Foundation
    ↓
Phase 2: Data Layer
    ↓
Phase 3: Core UI Components
    ↓
Phase 4: App Shell & Navigation
    ↓
Phase 5: Startup & Company Mgmt
    ↓
Phase 6: Core Feature Pages
    ↓
Phase 7: Transaction & Inventory
    ↓
Phase 8: Dashboard & Advanced
```

### Dependency Graph (Simplified)
```
Categories ──────────────────────┬──→ Products ──→ Sales ──→ Invoices ──→ Payments
                                 │                  ↓
Departments ──→ Employees        │              Returns
                                 │
Locations ──→ Stock Levels ──→ Adjustments
                    ↓
                Transfers
                    ↓
Suppliers ────────────────────→ Purchase Orders
     │
     └──→ Rental Inventory ──→ Rental Records

Customers ──→ Sales, Invoices, Rentals

Accountants ──→ Sales, Purchases, Rentals
```

### Critical Path
The minimum path to a working application:
1. Phase 1: Foundation
2. Phase 2: Data Layer
3. Phase 3: Core UI Components
4. Phase 4: App Shell
5. Phase 5: Company Management
6. Phase 6.1-6.7: Categories → Suppliers → Customers → Products
7. Phase 7.1-7.2: Expenses → Revenue
8. Phase 8.1: Dashboard

This gives you a functional app for recording income and expenses with a dashboard.

---

## Testing Checkpoints

### After Phase 2
- [ ] Can create and save a company file
- [ ] Can open an existing company file
- [ ] Encryption works correctly
- [ ] Data persists between sessions

### After Phase 5
- [ ] Welcome screen displays correctly
- [ ] Can create new company
- [ ] Can open existing company
- [ ] Recent companies list works
- [ ] Password protection works

### After Phase 6
- [ ] All entity management pages work
- [ ] CRUD operations function correctly
- [ ] Data relationships maintained

### After Phase 7
- [ ] All transaction pages work
- [ ] Inventory tracking accurate
- [ ] Rental system functional

### After Phase 8
- [ ] Dashboard displays correct data
- [ ] Charts render properly
- [ ] Reports generate correctly
- [ ] All settings functional
- [ ] Cross-platform testing passes
