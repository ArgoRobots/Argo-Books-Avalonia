# Argo Books - Complete Features Document

This document describes all features of the Argo Books Avalonia application based on the UI mockup analysis.

---

## Table of Contents

1. [Application Overview](#1-application-overview)
2. [File Management](#2-file-management)
3. [Dashboard](#3-dashboard)
4. [Analytics & Insights](#4-analytics--insights)
5. [Reports](#5-reports)
6. [Transactions - Expenses](#6-transactions---expenses)
7. [Transactions - Revenue](#7-transactions---revenue)
8. [Transactions - Invoices](#8-transactions---invoices)
9. [Transactions - Payments](#9-transactions---payments)
10. [Rentals - Inventory](#10-rentals---inventory)
11. [Rentals - Records](#11-rentals---records)
12. [Management - Customers](#12-management---customers)
13. [Management - Products/Services](#13-management---productsservices)
14. [Management - Categories](#14-management---categories)
15. [Management - Suppliers](#15-management---suppliers)
16. [Inventory - Stock Levels](#16-inventory---stock-levels)
17. [Inventory - Adjustments](#17-inventory---adjustments)
18. [Inventory - Locations](#18-inventory---locations)
19. [Inventory - Transfers](#19-inventory---transfers)
20. [Inventory - Purchase Orders](#20-inventory---purchase-orders)
21. [Team - Employees](#21-team---employees)
22. [Team - Departments](#22-team---departments)
23. [Team - Accountants](#23-team---accountants)
24. [Tracking - Returns](#24-tracking---returns)
25. [Tracking - Lost/Damaged](#25-tracking---lostdamaged)
26. [Tracking - Receipts](#26-tracking---receipts)
27. [Settings](#27-settings)
28. [Quick Actions](#28-quick-actions)
29. [Notifications](#29-notifications)
30. [Help & Support](#30-help--support)
31. [Security](#31-security)
32. [Import/Export](#32-importexport)

---

## 1. Application Overview

### 1.1 Target Platforms
- Windows (primary)
- macOS
- Linux
- Browser (WebAssembly)
- **NOT mobile**

### 1.2 Technology Stack
- **Framework:** Avalonia UI (.NET 10)
- **Language:** C#
- **Data Storage:** Custom .argo file format (tar + gzip + optional AES-256 encryption)
- **Charts:** LiveCharts2
- **PDF Generation:** QuestPDF
- **OCR:** Google Cloud Vision API (For scanning receipts)
- **Email:** PHP API on web server

### 1.3 Multi-Company Support
- Each company is stored as a separate `.argo` file
- Users can switch between companies
- Recent companies list (up to 10)
- Company information includes:
  - Company name
  - Address
  - Logo (image)

---

## 2. File Management

### 2.1 File Menu Operations
| Action | Shortcut | Description |
|--------|----------|-------------|
| Create New Company | - | Opens company creation wizard |
| Open Company | - | File dialog to select .argo file |
| Open Recent | - | Submenu showing last 10 companies |
| Save | Ctrl+S | Save current company |
| Save As | Ctrl+Shift+S | Save to new location |
| Close Company | - | Close current company (prompts to save) |
| Import | - | Import data from Excel |
| Export As | - | Export to backup or spreadsheet |
| Show Company in Folder | - | Opens file explorer to company location |

### 2.2 Company Creation
Fields:
- Company name (required)
- Address (optional)
- Logo upload (optional)

### 2.3 Auto-Save & Recovery
- Track changes made across all forms
- Prompt to save on close if unsaved changes exist
- Recover unsaved work on startup after crash
- Changes tracked per entity type (sales, purchases, customers, etc.)

---

## 3. Dashboard

### 3.1 Statistics Cards
Four main stat cards displayed at the top:

| Stat | Icon | Description |
|------|------|-------------|
| Total Revenue | Dollar sign (blue) | Sum of all revenue, with % change from last month |
| Total Expenses | Shopping cart (red) | Sum of all expenses, with % change from last month |
| Net Profit | Pie chart (green) | Revenue - Expenses, with % change |
| Active Customers | Users (purple) | Count of active customers, with % change |

### 3.2 Quick Actions Panel
Four quick action buttons:
- New Expense → links to Expenses page
- Record Sale → links to Revenue page
- Create Invoice → links to Invoices page
- New Rental → links to Rentals page

### 3.3 Charts
Two charts side-by-side:

**Revenue Overview Chart**
- Type: Area/Line chart
- Time filter: "This Month" dropdown
- Shows revenue trend over time

**Expense Breakdown Chart**
- Type: Pie chart
- Time filter: "This Month" dropdown
- Shows expense distribution by category

### 3.4 Recent Transactions Table
Columns:
- ID (invoice number)
- Customer
- Date
- Amount
- Status (badge: Paid/Pending/Overdue)

Actions:
- "View All" button links to Revenue page

### 3.5 AI Business Insights Panel
Activity timeline showing AI-generated insights:
- Revenue trends with recommendations
- Inventory alerts (low stock warnings)
- Seasonal pattern detection
- Overdue payment reminders

Each insight has:
- Colored dot indicator (green/yellow/blue/red)
- Title
- Description text
- Actionable recommendation

### 3.6 Active Rentals Table
Columns:
- Rental ID
- Customer
- Item
- Start Date
- Due Date
- Rate (daily/weekly)
- Status (badge: Active/Overdue)

Actions:
- "View All" button links to Rental Records page

---

## 4. Analytics & Insights

### 4.1 Analytics Page
Comprehensive analytics with interactive charts and date range filtering.

#### 4.1.1 Revenue & Expense Charts
- **Total Revenue** - Bar/line chart showing revenue over time (daily/weekly/monthly/yearly)
- **Total Expenses** - Bar/line chart showing expenses over time
- **Revenue Distribution** - Pie chart breakdown by category/product
- **Expenses Distribution** - Pie chart breakdown by category/vendor
- **Total Profits** - Bar/line chart showing profit margins over time
- **Sales vs Expenses** - Dual-axis comparison chart (revenue vs expenses over time)

#### 4.1.2 Transaction Analysis Charts
- **Average Transaction Value** - Line chart comparing average purchase vs sale values over time
- **Total Transactions** - Bar chart showing transaction count (purchases vs sales) over time
- **Average Shipping Costs** - Line chart comparing shipping costs for purchases vs sales (with option to include/exclude free shipping)
- **Growth Rates** - Line chart showing expense and revenue growth rate percentages over time

#### 4.1.3 Geographic Analysis Charts
- **Countries of Origin** - Pie chart showing where purchases come from
- **Companies of Origin** - Pie chart showing vendor distribution for purchases
- **Countries of Destination** - Pie chart showing where sales are shipped to
- **World Map** (LiveCharts GeoMap) - Interactive geographic heat map showing transaction distribution (Combined/Purchases Only/Sales Only modes)

#### 4.1.4 Operational Charts
- **Transactions by Accountant** - Pie chart showing workload distribution across accountants

#### 4.1.5 Returns Analysis Charts
- **Returns Over Time** - Dual-axis chart showing purchase returns vs sale returns over time
- **Return Reasons** - Pie chart breakdown of why items are returned
- **Financial Impact of Returns** - Dual-axis chart showing monetary value of returns over time
- **Returns by Category** - Pie chart showing which product categories have most returns
- **Returns by Product** - Pie chart showing which specific products are returned most
- **Purchase vs Sale Returns** - Comparison bar chart of return counts

#### 4.1.6 Losses Analysis Charts
- **Losses Over Time** - Dual-axis chart showing purchase losses vs sale losses over time
- **Loss Reasons** - Pie chart breakdown of why items were lost (damaged, stolen, expired, etc.)
- **Financial Impact of Losses** - Dual-axis chart showing monetary value of losses over time
- **Losses by Category** - Pie chart showing which categories have most losses
- **Losses by Product** - Pie chart showing which products are lost most frequently
- **Purchase vs Sale Losses** - Comparison bar chart of loss counts

#### 4.1.7 Customer Analysis Charts
- **Top Customers by Revenue** - Bar chart showing highest-spending customers
- **Customer Payment Status** - Pie chart showing paid vs outstanding balances
- **Customer Growth** - Line chart showing new customer acquisition over time
- **Active vs Inactive Customers** - Pie chart showing customer engagement
- **Customer Lifetime Value** - Bar chart showing top customers by total lifetime spend
- **Rentals per Customer** - Distribution chart showing customer rental frequency (0, 1, 2-5, 6-10, 11-20, 21+ rentals)

### 4.2 Chart Features
- Toggle between bar chart and line chart views (where applicable)
- Date range filtering (custom start/end dates)
- Pie chart grouping options (individual items, grouped by threshold percentage)
- Right-click context menu on charts
- Export chart to Excel
- Export chart as image
- Interactive tooltips with detailed values
- Theme-aware colors (adapts to light/dark mode)


### 4.2 Insights Page
AI-powered business insights:
- Trend analysis
- Anomaly detection
- Forecasting
- Recommendations

---

## 5. Reports

### 5.1 Report Generator
Custom report builder with drag-and-drop elements:

**Report Elements:**
- Labels (text)
- Images (company logo, etc.)
- Tables (data grids)
- Charts (various types)
- Date range selectors
- Summary statistics

**Page Settings:**
- Page size (Letter, A4, Legal, etc.)
- Orientation (Portrait/Landscape)
- Margins
- Header/Footer visibility
- Page numbers

**Report Templates:**
- Built-in templates
- Custom template save/load
- Template manager

### 5.2 Export Options
- PDF export with quality settings
- PNG export
- JPG export
- Quality slider (affects file size)

### 5.3 Report Data Selection
Select data to include:
- Date range
- Transaction types
- Categories
- Customers
- Products

---

## 6. Transactions - Expenses

### 6.1 Expense List View
**Statistics Cards:**
- Total Expenses (current period)
- Average Expense
- Largest Expense
- Number of Transactions

**Table Columns:**
- Date
- Description
- Category
- Amount
- Payment Method
- Vendor/Supplier
- Receipt attached (icon)
- Actions

**Features:**
- Search box
- Filter modal (date range, category, amount range, vendor)
- Add Expense button
- Pagination

### 6.2 Add/Edit Expense Modal
Fields:
- Date (required)
- Description (required)
- Category (searchable dropdown, required)
- Amount (required)
- Payment method (dropdown)
- Vendor/Supplier (searchable dropdown)
- Notes (textarea)
- Receipt upload (drag & drop or browse)

---

## 7. Transactions - Revenue

### 7.1 Revenue List View
**Statistics Cards:**
- Total Revenue (current period)
- Average Sale
- Largest Sale
- Number of Transactions

**Table Columns:**
- Date
- Transaction ID
- Customer
- Items/Description
- Amount
- Payment Status
- Actions

**Features:**
- Search box
- Filter modal
- Add Sale button
- Pagination

### 7.2 Add/Edit Sale Modal
Fields:
- Date (required)
- Customer (searchable dropdown)
- Line items table:
  - Product/Service
  - Quantity
  - Unit Price
  - Amount (calculated)
  - Delete button
- Add Line Item button
- Subtotal (calculated)
- Tax percentage
- Total (calculated)
- Payment method
- Notes

---

## 8. Transactions - Invoices

### 8.1 Invoice Statistics Cards
| Stat | Color | Description |
|------|-------|-------------|
| Total Outstanding | Blue | Sum of unpaid invoices |
| Paid This Month | Green | Sum of paid invoices this month |
| Overdue | Red | Sum of overdue invoices |
| Due This Week | Purple | Count of invoices due this week |

### 8.2 Invoice Tabs
Three tabs:
1. **All Invoices** - Complete invoice list
2. **Drafts** - Unpublished draft invoices
3. **Recurring** - Recurring invoice schedules

### 8.3 Invoice List Table
Columns:
- Invoice # (bold, e.g., #INV-2024-001)
- Customer (with avatar initials)
- Issue Date
- Due Date
- Amount (bold)
- Status (badge: Pending/Paid/Partial/Overdue)
- Actions (View History, Download)

Overdue rows highlighted with red background.

### 8.4 Create Invoice Modal
Fields:
- Customer (searchable dropdown, required)
- Invoice Number (auto-generated, readonly)
- Issue Date (required)
- Due Date (required)
- Line Items table:
  - Description
  - Quantity
  - Rate
  - Amount (calculated)
  - Delete button
- Add Line Item button
- Notes textarea
- **Recurring Invoice Toggle:**
  - When enabled, shows:
    - Frequency (Weekly/Monthly/Quarterly/Yearly)
    - End Date (optional)
- **Payment Reminders Section:**
  - Checkboxes for automatic reminders:
    - 1 day after due date
    - 7 days after due date
    - 14 days after due date
    - 30 days after due date

Footer buttons:
- Cancel
- Save as Draft
- Create & Send

### 8.5 Recurring Invoice Setup Modal
Fields:
- Customer (searchable dropdown, required)
- Amount (required)
- Frequency (Weekly/Monthly/Quarterly/Yearly)
- Start Date (required)
- End Date (optional)
- Invoice Description (required)
- Payment Terms (Due on Receipt/Net 15/Net 30/Net 45/Net 60)
- Notes
- Auto-send checkbox

### 8.6 Invoice History Modal
Shows:
- Invoice number and customer
- Status badge
- Summary box (Amount, Issue Date, Due Date)
- Activity Timeline:
  - Invoice Created (timestamp)
  - Invoice Sent (with recipient email)
  - Invoice Viewed (opened by recipient)
  - Reminder Email Sent (if applicable)
  - Payment Received (if paid)
  - Marked as Overdue (if applicable)

Actions:
- Download PDF
- Send Reminder

### 8.7 Invoice Filter Modal
Fields:
- Customer (searchable dropdown)
- Issue Date From/To
- Due Date From/To
- Status (All/Pending/Paid/Partial/Overdue)
- Amount Range (Under $1,000 / $1,000-$5,000 / $5,000-$10,000 / Over $10,000)

---

## 9. Transactions - Payments

### 9.1 Payment Recording
Track payments against invoices:
- Payment date
- Amount
- Payment method
- Reference number
- Notes

### 9.2 Partial Payments
Support for partial payments on invoices:
- Track multiple payments per invoice
- Show remaining balance
- Auto-update invoice status (Partial → Paid)

---

## 10. Rentals - Inventory

### 10.1 Rental Inventory Statistics
| Stat | Color | Description |
|------|-------|-------------|
| Total Items | Blue | Total rental inventory count |
| Available | Green | Items available for rent |
| Rented Out | Purple | Currently rented items |
| In Maintenance | Yellow | Items being serviced |

### 10.2 Rental Items Table
Columns:
- Item (name + ID like #RNT-ITM-001)
- Supplier
- Status (badge: Available/In Maintenance/All Rented)
- Total Qty
- Available
- Rented
- Daily Rate
- Weekly Rate
- Deposit
- Actions (Edit, Rent Out)

### 10.3 Add Rental Item Modal
Fields:
- Product Name (required)
- Supplier (searchable dropdown)
- Total Quantity
- Daily Rate
- Weekly Rate
- Monthly Rate
- Security Deposit
- Notes

### 10.4 Edit Rental Item Modal
Same as Add, plus:
- Status dropdown (Available/In Maintenance/Discontinued)

### 10.5 Rent Out Modal
Fields:
- Item (readonly, pre-filled)
- Customer (searchable dropdown, required)
- Quantity (with "X available" hint)
- Rate Type (Daily/Weekly/Monthly with price shown)
- Security Deposit
- Start Date
- Due Date
- Assigned Accountant (searchable dropdown)
- Notes

Summary box showing:
- Rental Rate
- Security Deposit
- Estimated Total

### 10.6 Filter Rentals Modal
Fields:
- Supplier (searchable dropdown)
- Status (All/Available/All Rented/In Maintenance)
- Min/Max Daily Rate
- Availability (Any/Has Available Units/Fully Rented)

---

## 11. Rentals - Records

### 11.1 Rental Records View
Track all rental transactions:
- Active rentals
- Completed rentals
- Overdue rentals

### 11.2 Rental Record Details
- Rental ID
- Customer
- Item(s)
- Start Date
- Due Date
- Return Date (if returned)
- Rental Rate
- Deposit
- Total Cost
- Status
- Notes

### 11.3 Return Rental Process
- Record return date
- Check item condition
- Process deposit return/deduction
- Calculate final charges

---

## 12. Management - Customers

### 12.1 Customer List View
**Table Columns:**
- Customer Name
- Email
- Phone
- Company (if B2B)
- Total Purchases
- Last Transaction Date
- Status (Active/Inactive)
- Actions

### 12.2 Add/Edit Customer Modal
Fields:
- Customer Name (required)
- Email
- Phone
- Company Name
- Address
- Notes
- Tags/Labels

---

## 13. Management - Products/Services

### 13.1 Products List View
**Table Columns:**
- Product Name
- SKU
- Category
- Unit Price
- Cost Price
- Stock Quantity
- Status
- Actions

### 13.2 Add/Edit Product Modal
Fields:
- Product Name (required)
- SKU (auto-generated or manual)
- Category (searchable dropdown)
- Description
- Unit Price (required)
- Cost Price
- Tax Rate
- Track Inventory checkbox
- Initial Stock (if tracking)
- Reorder Point
- Supplier (searchable dropdown)
- Image upload

---

## 14. Management - Categories

### 14.1 Category Types
Three category types (tabs):
- Purchase Categories
- Sales Categories
- Rental Categories

### 14.2 Category Management
- Hierarchical categories (parent/child)
- Color coding
- Icon selection
- Default tax rate per category

---

## 15. Management - Suppliers

### 15.1 Supplier List View
**Table Columns:**
- Supplier Name
- Contact Person
- Email
- Phone
- Products Supplied
- Total Orders
- Actions

### 15.2 Add/Edit Supplier Modal
Fields:
- Supplier Name (required)
- Contact Person
- Email
- Phone
- Address
- Website
- Payment Terms
- Notes

---

## 16. Inventory - Stock Levels

### 16.1 Inventory Statistics
| Stat | Color | Description |
|------|-------|-------------|
| Total Items | Blue | Total inventory item count |
| In Stock | Green | Items above reorder point |
| Low Stock | Yellow | Items at or below reorder point |
| Out of Stock | Red | Items with zero quantity |
| Overstock | Purple | Items exceeding overstock threshold |

### 16.2 Inventory Tabs
Four tabs:
1. **All Items** - Complete inventory
2. **Low Stock** - Items needing reorder (with badge count)
3. **Out of Stock** - Zero quantity items (with badge count)
4. **Overstock** - Excess inventory (with badge count)

### 16.3 Stock Levels Table
Columns:
- Product (name + description)
- SKU
- Category
- Location
- In Stock
- Reserved
- Available (calculated)
- Reorder Point
- Status (badge)
- Actions (View, Adjust, Edit)

### 16.4 View Item Details Modal
Shows:
- Product name and SKU
- Status badge
- Category, Location, Supplier, Unit of Measure
- Stock Information card:
  - In Stock
  - Reserved
  - Available
- Reorder Point, Overstock Threshold
- Unit Cost, Total Value
- Last Updated timestamp

Actions: Adjust Stock, Edit Item

### 16.5 Add Inventory Item Modal
Fields:
- Product (searchable dropdown, required)
- SKU (required)
- Location (searchable dropdown, required)
- Initial Quantity (required)
- Reorder Point
- Overstock Threshold

### 16.6 Edit Inventory Item Modal
Fields:
- Product Name (required)
- SKU (disabled)
- Description
- Category (dropdown)
- Location (searchable dropdown)
- Supplier (searchable dropdown)
- Unit of Measure (Each/Box/Pack/Carton/Pallet/Kg/Liter)
- Current Quantity
- Unit Cost
- Reorder Point
- Overstock Threshold

### 16.7 Filter Inventory Modal
Fields:
- Product (searchable dropdown)
- Category (searchable dropdown with hierarchy)
- Location (searchable dropdown)
- Stock Status (All/In Stock/Low Stock/Out of Stock/Overstock)
- Supplier (searchable dropdown)
- Min/Max Stock Level

---

## 17. Inventory - Adjustments

### 17.1 Adjust Stock Modal
Fields:
- Adjustment Type dropdown:
  - Add Stock (Received)
  - Remove Stock (Sold/Used)
  - Damaged/Lost
  - Inventory Correction
  - Customer Return
- Current Stock (readonly)
- Adjustment Quantity
- New Stock Level (calculated, readonly)
- Reason/Notes (textarea)
- Reference Number (e.g., PO-12345, INV-67890)

### 17.2 Adjustment History
Track all adjustments:
- Date/Time
- User
- Adjustment Type
- Quantity Change
- Reference
- Notes

---

## 18. Inventory - Locations

### 18.1 Location Management
Manage warehouse/storage locations:
- Main Warehouse
- Secondary Storage
- Factory Floor
- Retail Store
- Distribution Center

### 18.2 Location Details
- Location Name
- Address
- Contact Person
- Phone
- Capacity
- Current Utilization

---

## 19. Inventory - Transfers

### 19.1 Stock Transfer
Move inventory between locations:
- Source Location
- Destination Location
- Product(s)
- Quantity
- Transfer Date
- Notes

### 19.2 Transfer History
Track all transfers with status:
- Pending
- In Transit
- Completed

---

## 20. Inventory - Purchase Orders

### 20.1 Purchase Order Creation
Fields:
- Supplier (required)
- Order Date
- Expected Delivery Date
- Line Items:
  - Product
  - Quantity
  - Unit Cost
  - Total
- Shipping Cost
- Notes

### 20.2 Purchase Order Status
- Draft
- Sent to Supplier
- Partially Received
- Fully Received
- Cancelled

### 20.3 Bulk Reorder Modal
For low/out of stock items:
- Checkbox list of items needing reorder
- Suggested quantities based on reorder point
- Estimated total cost
- Create Purchase Order button

---

## 21. Team - Employees

### 21.1 Employee Tabs
Two tabs:
1. **Employee List** - All employees
2. **Payroll** - Payroll processing

### 21.2 Employee Statistics
| Stat | Color | Description |
|------|-------|-------------|
| Total Employees | Blue | Total employee count |
| Active | Green | Currently employed |
| On Leave | Yellow | Employees on leave |
| Monthly Payroll | Purple | Total monthly payroll cost |

### 21.3 Employee List Table
Columns:
- Employee (name + email with avatar)
- Department
- Position
- Hire Date
- Status (badge: Active/On Leave/Terminated)
- Salary
- Actions (Edit, Process Pay)

### 21.4 Add Employee Modal
Fields:
- **Basic Information:**
  - First Name (required)
  - Last Name (required)
  - Email
  - Phone
  - Date of Birth
- **Employment Details:**
  - Department (searchable dropdown)
  - Position
  - Hire Date
  - Employment Type (Full-time/Part-time/Contract)
- **Compensation:**
  - Salary Type (Annual/Hourly)
  - Salary Amount
  - Pay Frequency (Weekly/Bi-weekly/Monthly)
- **Emergency Contact:**
  - Name
  - Phone
  - Relationship

### 21.5 Payroll Tab
**Payroll Settings:**
- Pay Frequency (Weekly/Bi-weekly/Monthly)
- Overtime settings (overtime rate multiplier)

**Run Payroll Button:**
Opens Review Payroll modal

### 21.6 Review Payroll Modal
Per-employee adjustments:
- Employee name and base salary
- Bonus amount input
- Tax % input
- Deduction % input
- Net Pay (calculated)

Summary:
- Total Gross
- Total Deductions
- Total Net Pay

Actions:
- Cancel
- Process Payroll

---

## 22. Team - Departments

### 22.1 Department Management
- Department Name
- Department Head (employee)
- Employee Count
- Budget

---

## 23. Team - Accountants

### 23.1 Accountant List
Manage accountants who handle transactions:
- Name
- Email
- Phone
- Assigned Transactions Count

### 23.2 Accountant Assignment
Assign accountants to:
- Sales transactions
- Purchase transactions
- Rentals

---

## 24. Tracking - Returns

### 24.1 Return Processing
Track product returns:
- Original Transaction
- Customer
- Product(s)
- Return Reason
- Return Date
- Refund Amount
- Restocking Fee
- Status (Pending/Approved/Completed/Rejected)

### 24.2 Undo Return
Ability to reverse a return if processed incorrectly.

---

## 25. Tracking - Lost/Damaged

### 25.1 Lost/Damaged Tracking
Record lost or damaged inventory:
- Product
- Quantity
- Reason (Lost/Damaged/Stolen/Expired)
- Date Discovered
- Value Lost
- Notes
- Insurance Claim checkbox

### 25.2 Undo Loss
Ability to reverse a loss record if item is found.

---

## 26. Tracking - Receipts

### 26.1 Receipt Statistics
| Stat | Color | Description |
|------|-------|-------------|
| Total Receipts | Blue | Total receipt count |
| Expense Receipts | Red | Receipts for expenses |
| Revenue Receipts | Green | Receipts for revenue |
| AI Scanned | Purple | Receipts processed by OCR |

### 26.2 Receipt Archive
Grid view showing receipt thumbnails:
- Receipt image preview (icon: file-image or file-pdf)
- Receipt ID
- Date
- Type badge (Expense/Revenue)
- Amount

Features:
- Filter button
- AI Scan Receipt button
- Export Selected button
- Grid/List view toggle
- Checkbox selection for bulk actions

### 26.3 Receipt Preview Modal
- Full-screen receipt view
- Zoom and pan capability
- Download Receipt button

### 26.4 AI Scan Receipt
Using Google Cloud Vision API:
- Upload or capture receipt image
- OCR extracts:
  - Vendor name
  - Date
  - Total amount
  - Line items (if readable)
  - Tax amount
- User confirms/edits extracted data
- Auto-creates expense or revenue record
- Stores original receipt image

### 26.5 Filter Receipts Modal
Fields:
- Receipt Type (All/Expense/Revenue)
- Date From/To
- Min/Max Amount
- Source (All/Manual Upload/AI Scanned)
- File Type (All/Image/PDF)

---

## 27. Settings

### 27.1 Settings Tabs
Five tabs in settings modal:

#### General Tab
- Language (dropdown)
- Currency (dropdown)
- Date Format (MM/DD/YYYY, DD/MM/YYYY, YYYY-MM-DD)
- Time Zone (dropdown)
- Privacy: Anonymous data collection toggle

#### Features Tab
Toggle modules on/off:
- Invoices
- Payments
- Inventory
- Employees
- Rentals

(Disabled modules hide from sidebar)

#### Notifications Tab
Notification toggles:
- Low Stock Alert
- Invoice Overdue
- Payment Received
- Large Transaction Alert

#### Appearance Tab
- Theme (Light/Dark/System)
- Accent Colors: Blue, Green, Purple, Pink, Orange, Teal

#### Security Tab
- Change Password button
- Windows Hello toggle (Windows only)
- File Encryption toggle (AES-256)
- Auto-lock settings

---

## 28. Quick Actions

### 28.1 Header Quick Actions Menu
Searchable quick actions dropdown:

**Create & Add:**
- Create Invoice
- Record Expense
- Scan Receipt (AI)
- Add Customer
- Add Product
- Add Supplier
- Add Employee

**Transactions:**
- Create Rental Record
- Record Payment
- Create Purchase Order
- Stock Adjustment
- Stock Transfer
- Record Return

**Organization:**
- Add Category
- Add Department
- Add Location

### 28.2 Quick Action Behavior
Each action opens the corresponding modal directly, without navigating to the full page.

---

## 29. Notifications

### 29.1 Notification Panel
Slide-out panel from header showing:
- Notification list with timestamps
- Mark as read
- Clear all
- Notification settings link

### 29.2 Notification Types
- Low stock alerts
- Overdue invoices
- Payment received
- Rental due soon
- Rental overdue
- Large transaction alerts
- System updates

---

## 30. Help & Support

### 30.1 Help Panel
Slide-out panel with:
- Search help articles
- Getting Started guide
- Feature tutorials
- FAQ
- Contact Support link
- Report a Bug link
- Keyboard Shortcuts reference

---

## 31. Security

### 31.1 File Encryption
- Optional AES-256 encryption for company files
- Password protection
- Encryption status stored in file footer

### 31.2 Biometric Authentication
Platform-specific implementation:
- **Windows:** Windows Hello (fingerprint, face, PIN)
- **macOS:** Touch ID
- **Linux:** Password only
- **Browser:** Password only

Auto-detect OS and use appropriate system.

### 31.3 Auto-Lock
- Configurable auto-lock timeout
- Lock on system sleep
- Require authentication to unlock

### 31.4 Password Management
- Password change functionality
- Password requirements enforcement
- Secure password storage (hashed)

---

## 32. Import/Export

### 32.1 Import Data
**Supported Format:** Excel (.xlsx)

**Import Types:**
- Expense Transactions
- Revenue Transactions
- Products
- Customers
- Suppliers
- Employees

**Import Options:**
- Date format selection
- Skip header row checkbox
- Drag & drop file upload
- Download template file link

### 32.2 Export As
Two export modes:

**Backup File (.argobk):**
- Complete backup of all data
- Includes settings and attachments
- Optional attachment inclusion

**Spreadsheet Export:**
- Format: Excel (.xlsx), CSV (.csv), PDF (.pdf)
- Date range selection
- Data selection checkboxes:
  - Customers
  - Invoices
  - Expenses
  - Products
  - Inventory
  - Payments
  - Suppliers
- Select All option
- Record counts shown per data type

### 32.3 Backup Data
- Create .argobk backup file
- Automatic unique naming for duplicates
- ZIP compression for backup

---

## Appendix A: Recurring Invoice Behavior

When the application opens:
1. Check all recurring invoices
2. If any are due for generation:
   - Show a UI modal listing due invoices
   - User reviews and confirms which to generate
   - **No automatic generation without user confirmation**
3. User can:
   - Generate selected invoices
   - Skip this time
   - Edit recurring schedule

---

## Appendix B: Platform-Specific Features

| Feature | Windows | macOS | Linux | Browser |
|---------|---------|-------|-------|---------|
| Windows Hello | Yes | No | No | No |
| Touch ID | No | Yes | No | No |
| File System Access | Full | Full | Full | Limited (upload/download) |
| SMTP Email | Yes | Yes | Yes | No (use copy to clipboard) |
| Receipt Camera Capture | Yes | Yes | Yes | File upload only |
| System Theme Detection | Yes | Yes | Yes | Yes |

---

## Appendix C: Searchable Dropdown Behavior

All searchable dropdowns should:
1. Show filtered results as user types
2. Support keyboard navigation (arrow keys)
3. Select on Enter or click
4. Show "No results" message when empty
5. Allow clearing selection
6. Support creating new items where appropriate (e.g., "Add new customer...")
