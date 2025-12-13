# Argo Books - Data & File Structure Document

This document describes the data models and file storage structure for the Argo Books Avalonia application.

---

## Table of Contents

1. [File Format Overview](#1-file-format-overview)
2. [Company File Structure](#2-company-file-structure)
3. [Data Models](#3-data-models)
4. [Global Application Settings](#4-global-application-settings)
5. [File Operations](#5-file-operations)
6. [Encryption](#6-encryption)
7. [Migration & Versioning](#7-migration--versioning)

---

## 1. File Format Overview

### 1.1 Company File (.argo)

The company file is a custom format with the following structure:

```
┌─────────────────────────────────────┐
│  GZip Compressed TAR Archive        │
│  ┌─────────────────────────────┐    │
│  │  JSON/TXT Data Files        │    │
│  │  - appSettings.json         │    │
│  │  - sales.json               │    │
│  │  - purchases.json           │    │
│  │  - invoices.json            │    │
│  │  - ... (other data files)   │    │
│  │  - receipts/ (folder)       │    │
│  └─────────────────────────────┘    │
├─────────────────────────────────────┤
│  Optional: AES-256 Encryption       │
├─────────────────────────────────────┤
│  Footer (JSON)                      │
│  - Version                          │
│  - IsEncrypted                      │
│  - PasswordHash                     │
│  - Accountants[]                    │
│  - FooterLength                     │
└─────────────────────────────────────┘
```

### 1.2 File Extension Mapping

| Extension | Description |
|-----------|-------------|
| `.argo` | Company data file |
| `.argobk` | Backup file (ZIP containing .argo file) |
| `.argotemplate` | Report template file |

### 1.3 Process Flow

**Saving:**
```
Data Files (JSON/TXT)
    → Create TAR archive in memory
    → GZip compress
    → Optional: AES-256 encrypt
    → Append footer
    → Write to .argo file
```

**Opening:**
```
Read .argo file
    → Read and parse footer
    → Verify password if encrypted
    → Optional: AES-256 decrypt
    → GZip decompress
    → Extract TAR to temp directory
    → Load data files into memory
```

---

## 2. Company File Structure

### 2.1 Directory Structure (Inside TAR)

```
CompanyName/
├── appSettings.json         # Company settings
├── sales.json               # Revenue/Sales transactions
├── purchases.json           # Expense transactions
├── invoices.json            # Invoice records
├── payments.json            # Payment records
├── customers.json           # Customer data
├── products.json            # Products/Services
├── suppliers.json           # Supplier data
├── employees.json           # Employee data
├── departments.json         # Department data
├── categories.json          # All category types
├── rentalInventory.json     # Rental items
├── rentals.json             # Rental records
├── inventory.json           # Stock levels
├── locations.json           # Warehouse/storage locations
├── stockAdjustments.json    # Inventory adjustments
├── stockTransfers.json      # Inventory transfers
├── purchaseOrders.json      # Purchase orders
├── returns.json             # Return records
├── lostDamaged.json         # Lost/damaged records
├── recurringInvoices.json   # Recurring invoice schedules
├── accountants.json         # Accountant assignments
├── reportTemplates.json     # Custom report templates
└── receipts/                # Receipt images folder
    ├── RCP-001.jpg
    ├── RCP-002.png
    └── RCP-003.pdf
```

### 2.2 appSettings.json Format

```
{
  "appVersion": "1.0.0",
  "changesMade": false,
  
  "company": {
    "name": "My Business Inc",
    "address": "123 Main St, City, State"
  },
  
  "localization": {
    "language": "en-US",
    "currency": "USD",
    "dateFormat": "MM/DD/YYYY",
    "timeZone": "America/New_York"
  },
  
  "appearance": {
    "theme": "System",
    "accentColor": "Blue"
  },
  
  "enabledModules": {
    "invoices": true,
    "payments": true,
    "inventory": true,
    "employees": true,
    "rentals": true
  },
  
  "notifications": {
    "lowStockAlert": true,
    "invoiceOverdueAlert": true,
    "paymentReceivedAlert": true,
    "largeTransactionAlert": true,
    "largeTransactionThreshold": 10000
  },
  
  "security": {
    "autoLockEnabled": false,
    "autoLockMinutes": 5
  }
}

```

---

## 3. Data Models

### 3.1 Transaction Models

#### Sale (Revenue Transaction)
```json
{
  "id": "SAL-2024-00001",
  "date": "2024-12-01T10:30:00",
  "customerId": "CUS-001",
  "accountantId": "ACC-001",
  "lineItems": [
    {
      "productId": "PRD-001",
      "description": "Office Supplies Bundle",
      "quantity": 2,
      "unitPrice": 49.99,
      "taxRate": 0.08,
      "amount": 107.98
    }
  ],
  "subtotal": 99.98,
  "taxAmount": 8.00,
  "total": 107.98,
  "paymentMethod": "Credit Card",
  "paymentStatus": "Paid",
  "notes": "",
  "receiptId": "RCP-001",
  "createdAt": "2024-12-01T10:30:00",
  "updatedAt": "2024-12-01T10:30:00"
}
```

#### Purchase (Expense Transaction)
```json
{
  "id": "PUR-2024-00001",
  "date": "2024-12-01T14:00:00",
  "supplierId": "SUP-001",
  "accountantId": "ACC-001",
  "categoryId": "CAT-PUR-001",
  "description": "Office equipment purchase",
  "amount": 1245.00,
  "taxAmount": 99.60,
  "total": 1344.60,
  "paymentMethod": "Bank Transfer",
  "notes": "",
  "receiptId": "RCP-002",
  "createdAt": "2024-12-01T14:00:00",
  "updatedAt": "2024-12-01T14:00:00"
}
```

#### Invoice
```json
{
  "id": "INV-2024-00001",
  "invoiceNumber": "#INV-2024-001",
  "customerId": "CUS-001",
  "issueDate": "2024-12-01",
  "dueDate": "2024-12-15",
  "lineItems": [
    {
      "description": "Consulting Services",
      "quantity": 10,
      "rate": 150.00,
      "amount": 1500.00
    }
  ],
  "subtotal": 1500.00,
  "taxRate": 0.08,
  "taxAmount": 120.00,
  "total": 1620.00,
  "amountPaid": 0.00,
  "balance": 1620.00,
  "status": "Pending",
  "notes": "Payment due within 14 days",
  "recurringInvoiceId": null,
  "reminderSettings": {
    "day1": true,
    "day7": true,
    "day14": false,
    "day30": false
  },
  "history": [
    {
      "action": "Created",
      "timestamp": "2024-12-01T09:00:00",
      "details": null
    },
    {
      "action": "Sent",
      "timestamp": "2024-12-01T09:05:00",
      "details": "Sent to billing@acmecorp.com"
    },
    {
      "action": "Viewed",
      "timestamp": "2024-12-02T14:30:00",
      "details": "Opened by recipient"
    }
  ],
  "createdAt": "2024-12-01T09:00:00",
  "updatedAt": "2024-12-02T14:30:00"
}
```

#### Payment
```json
{
  "id": "PAY-2024-00001",
  "invoiceId": "INV-2024-00001",
  "customerId": "CUS-001",
  "date": "2024-12-10",
  "amount": 1620.00,
  "paymentMethod": "Bank Transfer",
  "referenceNumber": "TXN-123456",
  "notes": "",
  "createdAt": "2024-12-10T11:00:00"
}
```

#### Recurring Invoice
```json
{
  "id": "REC-INV-001",
  "customerId": "CUS-001",
  "amount": 1000.00,
  "description": "Monthly retainer",
  "frequency": "Monthly",
  "startDate": "2024-01-01",
  "endDate": null,
  "nextInvoiceDate": "2025-01-01",
  "paymentTerms": "Net 30",
  "autoSend": true,
  "status": "Active",
  "notes": "",
  "createdAt": "2024-01-01T00:00:00",
  "lastGeneratedAt": "2024-12-01T00:00:00"
}
```

### 3.2 Entity Models

#### Customer
```json
{
  "id": "CUS-001",
  "name": "Acme Corporation",
  "email": "billing@acmecorp.com",
  "phone": "(555) 123-4567",
  "companyName": "Acme Corp",
  "address": {
    "street": "123 Business Ave",
    "city": "New York",
    "state": "NY",
    "zipCode": "10001",
    "country": "USA"
  },
  "notes": "Preferred customer",
  "tags": ["VIP", "Enterprise"],
  "status": "Active",
  "totalPurchases": 45230.00,
  "lastTransactionDate": "2024-12-01",
  "createdAt": "2023-01-15T00:00:00",
  "updatedAt": "2024-12-01T00:00:00"
}
```

#### Product
```json
{
  "id": "PRD-001",
  "name": "Office Supplies Bundle",
  "sku": "SKU-001",
  "description": "Pens, paper, folders",
  "categoryId": "CAT-SAL-001",
  "unitPrice": 49.99,
  "costPrice": 25.00,
  "taxRate": 0.08,
  "trackInventory": true,
  "supplierId": "SUP-001",
  "imageUrl": null,
  "status": "Active",
  "createdAt": "2023-06-01T00:00:00",
  "updatedAt": "2024-11-15T00:00:00"
}
```

#### Supplier
```json
{
  "id": "SUP-001",
  "name": "Staples Inc.",
  "contactPerson": "John Smith",
  "email": "orders@staples.com",
  "phone": "(800) 123-4567",
  "address": {
    "street": "500 Staples Drive",
    "city": "Framingham",
    "state": "MA",
    "zipCode": "01702",
    "country": "USA"
  },
  "website": "https://www.staples.com",
  "paymentTerms": "Net 30",
  "notes": "",
  "createdAt": "2023-01-01T00:00:00",
  "updatedAt": "2024-06-01T00:00:00"
}
```

#### Employee
```json
{
  "id": "EMP-001",
  "firstName": "Jane",
  "lastName": "Doe",
  "email": "jane.doe@company.com",
  "phone": "(555) 987-6543",
  "dateOfBirth": "1990-05-15",
  "departmentId": "DEP-001",
  "position": "Senior Accountant",
  "hireDate": "2022-03-01",
  "employmentType": "Full-time",
  "salaryType": "Annual",
  "salaryAmount": 65000.00,
  "payFrequency": "Bi-weekly",
  "emergencyContact": {
    "name": "John Doe",
    "phone": "(555) 111-2222",
    "relationship": "Spouse"
  },
  "status": "Active",
  "createdAt": "2022-03-01T00:00:00",
  "updatedAt": "2024-01-15T00:00:00"
}
```

#### Department
```json
{
  "id": "DEP-001",
  "name": "Finance",
  "headEmployeeId": "EMP-001",
  "budget": 150000.00,
  "createdAt": "2022-01-01T00:00:00"
}
```

#### Category
```json
{
  "id": "CAT-SAL-001",
  "type": "Sales",
  "name": "Office Supplies",
  "parentId": null,
  "color": "#4A90D9",
  "icon": "box",
  "defaultTaxRate": 0.08,
  "createdAt": "2023-01-01T00:00:00"
}
```

#### Accountant
```json
{
  "id": "ACC-001",
  "name": "Robert Wilson",
  "email": "r.wilson@accounting.com",
  "phone": "(555) 333-4444",
  "assignedTransactions": 156,
  "createdAt": "2023-01-01T00:00:00"
}
```

### 3.3 Inventory Models

#### Inventory Item (Stock Level)
```json
{
  "id": "INV-ITM-001",
  "productId": "PRD-001",
  "sku": "SKU-001",
  "locationId": "LOC-001",
  "inStock": 245,
  "reserved": 12,
  "available": 233,
  "reorderPoint": 50,
  "overstockThreshold": 500,
  "unitCost": 12.50,
  "unitOfMeasure": "Each",
  "status": "InStock",
  "lastUpdated": "2024-12-05T14:30:00"
}
```

#### Location
```json
{
  "id": "LOC-001",
  "name": "Main Warehouse",
  "address": {
    "street": "100 Industrial Blvd",
    "city": "Chicago",
    "state": "IL",
    "zipCode": "60601",
    "country": "USA"
  },
  "contactPerson": "Mike Johnson",
  "phone": "(555) 444-5555",
  "capacity": 10000,
  "currentUtilization": 6500,
  "createdAt": "2023-01-01T00:00:00"
}
```

#### Stock Adjustment
```json
{
  "id": "ADJ-001",
  "inventoryItemId": "INV-ITM-001",
  "adjustmentType": "Add",
  "quantity": 50,
  "previousStock": 195,
  "newStock": 245,
  "reason": "Received from supplier",
  "referenceNumber": "PO-12345",
  "userId": "USR-001",
  "timestamp": "2024-12-05T14:30:00"
}
```

#### Stock Transfer
```json
{
  "id": "TRF-001",
  "inventoryItemId": "INV-ITM-001",
  "sourceLocationId": "LOC-001",
  "destinationLocationId": "LOC-002",
  "quantity": 25,
  "transferDate": "2024-12-01",
  "status": "Completed",
  "notes": "",
  "createdAt": "2024-12-01T09:00:00",
  "completedAt": "2024-12-01T15:00:00"
}
```

#### Purchase Order
```json
{
  "id": "PO-001",
  "poNumber": "#PO-2024-001",
  "supplierId": "SUP-001",
  "orderDate": "2024-12-01",
  "expectedDeliveryDate": "2024-12-10",
  "lineItems": [
    {
      "productId": "PRD-001",
      "quantity": 100,
      "unitCost": 12.50,
      "total": 1250.00
    }
  ],
  "subtotal": 1250.00,
  "shippingCost": 50.00,
  "total": 1300.00,
  "status": "Sent",
  "notes": "",
  "createdAt": "2024-12-01T10:00:00",
  "updatedAt": "2024-12-01T10:00:00"
}
```

### 3.4 Rental Models

#### Rental Item (Inventory)
```json
{
  "id": "RNT-ITM-001",
  "name": "Power Drill Set",
  "supplierId": "SUP-002",
  "totalQuantity": 10,
  "availableQuantity": 7,
  "rentedQuantity": 3,
  "dailyRate": 45.00,
  "weeklyRate": 250.00,
  "monthlyRate": 800.00,
  "securityDeposit": 150.00,
  "status": "Available",
  "notes": "Professional grade power drill set with 50+ accessories",
  "createdAt": "2023-06-01T00:00:00",
  "updatedAt": "2024-12-01T00:00:00"
}
```

#### Rental Record
```json
{
  "id": "RNT-001",
  "rentalItemId": "RNT-ITM-001",
  "customerId": "CUS-002",
  "accountantId": "ACC-001",
  "quantity": 1,
  "rateType": "Daily",
  "rateAmount": 45.00,
  "securityDeposit": 150.00,
  "startDate": "2024-11-25",
  "dueDate": "2024-12-05",
  "returnDate": null,
  "status": "Active",
  "totalCost": null,
  "depositRefunded": null,
  "notes": "",
  "createdAt": "2024-11-25T10:00:00",
  "updatedAt": "2024-11-25T10:00:00"
}
```

### 3.5 Tracking Models

#### Return
```json
{
  "id": "RET-001",
  "originalTransactionId": "SAL-2024-00001",
  "customerId": "CUS-001",
  "returnDate": "2024-12-05",
  "items": [
    {
      "productId": "PRD-001",
      "quantity": 1,
      "reason": "Defective"
    }
  ],
  "refundAmount": 49.99,
  "restockingFee": 0.00,
  "status": "Completed",
  "notes": "",
  "processedBy": "ACC-001",
  "createdAt": "2024-12-05T11:00:00"
}
```

#### Lost/Damaged Record
```json
{
  "id": "LOST-001",
  "productId": "PRD-003",
  "inventoryItemId": "INV-ITM-003",
  "quantity": 5,
  "reason": "Damaged",
  "dateDiscovered": "2024-12-01",
  "valueLost": 125.00,
  "notes": "Water damage from roof leak",
  "insuranceClaim": true,
  "createdAt": "2024-12-01T09:00:00"
}
```

#### Receipt
```json
{
  "id": "RCP-001",
  "transactionId": "PUR-2024-00001",
  "transactionType": "Expense",
  "fileName": "RCP-001.jpg",
  "fileType": "image/jpeg",
  "fileSize": 245000,
  "amount": 1245.00,
  "date": "2024-12-01",
  "vendor": "Office Depot",
  "source": "AI Scanned",
  "ocrData": {
    "extractedVendor": "Office Depot",
    "extractedDate": "2024-12-01",
    "extractedAmount": 1245.00,
    "extractedItems": [
      "Printer Paper x5 - $45.00",
      "Ink Cartridges x10 - $200.00"
    ],
    "confidence": 0.95
  },
  "createdAt": "2024-12-01T14:30:00"
}
```

### 3.6 Report Models

#### Report Template
```json
{
  "id": "TPL-001",
  "name": "Monthly Sales Report",
  "description": "Standard monthly sales summary",
  "pageSize": "Letter",
  "pageOrientation": "Portrait",
  "pageMargins": {
    "top": 40,
    "right": 40,
    "bottom": 40,
    "left": 40
  },
  "showHeader": true,
  "showFooter": true,
  "showPageNumbers": true,
  "title": "Monthly Sales Report",
  "backgroundColor": "#FFFFFF",
  "elements": [
    {
      "type": "Label",
      "x": 50,
      "y": 100,
      "width": 300,
      "height": 30,
      "text": "Sales Summary",
      "fontSize": 18,
      "fontWeight": "Bold",
      "alignment": "Left",
      "zOrder": 0,
      "isVisible": true
    },
    {
      "type": "Table",
      "x": 50,
      "y": 150,
      "width": 500,
      "height": 300,
      "dataSource": "Sales",
      "columns": ["Date", "Customer", "Amount"],
      "zOrder": 1,
      "isVisible": true
    },
    {
      "type": "Chart",
      "x": 50,
      "y": 480,
      "width": 500,
      "height": 200,
      "chartType": "Bar",
      "dataSource": "SalesByCategory",
      "zOrder": 2,
      "isVisible": true
    }
  ],
  "isBuiltIn": false,
  "createdAt": "2024-06-01T00:00:00",
  "updatedAt": "2024-11-15T00:00:00"
}
```

---

## 4. Global Application Settings

### 4.1 Location
Settings stored outside company files:

| Platform | Path |
|----------|------|
| Windows | `%AppData%\Argo\ArgoBooks\` |
| macOS | `~/Library/Application Support/ArgoBooks/` |
| Linux | `~/.config/ArgoBooks/` |

### 4.2 Global Settings File (globalSettings.json)
```json
{
  "welcome": {
    "showWelcomeForm": true,
    "eulaAccepted": true
  },

  "recentCompanies": [
    "/path/to/company1.argo",
    "/path/to/company2.argo"
  ],

  "updates": {
    "lastUpdateCheck": "2024-12-01T00:00:00",
    "autoOpenRecentAfterUpdate": true
  },

  "ui": {
    "sidebarCollapsed": false
  },

  "license": {
    "standardKey": "XXXX-XXXX-XXXX-XXXX",
    "premiumSubscriptionId": null,
    "premiumExpiryDate": null,
    "lastValidationDate": "2024-12-01T00:00:00"
  },

  "privacy": {
    "anonymousDataCollectionConsent": false,
    "consentDate": null
  }
}
```

### 4.3 Cache Directory Structure
```
cache/
├── logs/
│   └── app-2024-12-01.log
├── exchangeRates.json
├── translations.json
└── anonymousUserData.json
```

### 4.4 Password Manager File (passwords.json)
Stored in global settings directory, encrypted with AES-256:
```json
{
  "passwords": [
    {
      "id": "PWD-001",
      "name": "Supplier Portal",
      "username": "admin@company.com",
      "password": "encrypted-password-data",
      "website": "https://supplier.example.com",
      "category": "Suppliers",
      "notes": "Main supplier account",
      "createdAt": "2024-01-01T00:00:00",
      "updatedAt": "2024-06-15T00:00:00"
    }
  ],
  "categories": ["Suppliers", "Banking", "Services", "Other"]
}
```

---

## 5. File Operations

### 5.1 Save Process
```csharp
public void SaveCompany()
{
    // 1. Serialize all data to JSON files in temp directory
    SerializeAllDataToTempDirectory();

    // 2. Create TAR archive from temp directory
    using var tarStream = new MemoryStream();
    TarFile.CreateFromDirectory(tempDir, tarStream, includeBaseDirectory: true);
    tarStream.Seek(0, SeekOrigin.Begin);

    // 3. GZip compress
    using var gzipStream = new MemoryStream();
    using (var gzip = new GZipStream(gzipStream, CompressionLevel.Optimal, leaveOpen: true))
    {
        tarStream.CopyTo(gzip);
    }
    gzipStream.Seek(0, SeekOrigin.Begin);

    // 4. Optional: AES-256 encrypt
    Stream finalStream = gzipStream;
    if (Settings.EncryptFiles)
    {
        finalStream = EncryptStream(gzipStream, aesKey, aesIV);
    }

    // 5. Create footer
    var footer = new FooterData
    {
        Version = AppVersion,
        IsEncrypted = Settings.EncryptFiles,
        PasswordHash = hashedPassword,
        Accountants = accountantsList
    };

    // 6. Write to file
    using var fileStream = new FileStream(filePath, FileMode.Create);
    finalStream.CopyTo(fileStream);
    WriteFooter(fileStream, footer);
}
```

### 5.2 Open Process
```csharp
public void OpenCompany(string filePath)
{
    // 1. Read footer
    var footer = ReadFooter(filePath);

    // 2. Check version compatibility
    if (!IsVersionCompatible(footer.Version))
    {
        throw new VersionIncompatibleException();
    }

    // 3. If encrypted, verify password
    if (footer.IsEncrypted)
    {
        if (!VerifyPassword(inputPassword, footer.PasswordHash))
        {
            throw new InvalidPasswordException();
        }
    }

    // 4. Read content (excluding footer)
    var contentStream = ReadContentWithoutFooter(filePath);

    // 5. Optional: Decrypt
    if (footer.IsEncrypted)
    {
        contentStream = DecryptStream(contentStream, aesKey, aesIV);
    }

    // 6. GZip decompress
    using var gzipStream = new GZipStream(contentStream, CompressionMode.Decompress);
    using var tarStream = new MemoryStream();
    gzipStream.CopyTo(tarStream);
    tarStream.Seek(0, SeekOrigin.Begin);

    // 7. Extract TAR to temp directory
    TarFile.ExtractToDirectory(tarStream, tempDir, overwriteFiles: false);

    // 8. Load data from JSON files
    LoadAllDataFromTempDirectory();
}
```

### 5.3 Auto-Save Recovery
On startup:
1. Check temp directory for any company folders
2. For each folder, check `appSettings.json` for `ChangesMade:true`
3. If changes found, prompt user to recover
4. If user accepts, let them choose save location
5. If user declines, delete temp folder

---

## 6. Encryption

### 6.1 Algorithm
- **Cipher:** AES-256-GCM (authenticated encryption)
- **Key Derivation:** PBKDF2 with SHA-256, 100,000 iterations
- **Salt:** 32 bytes, randomly generated per file
- **IV:** 12 bytes, randomly generated per encryption

### 6.2 Password Storage
- Passwords are NEVER stored in plaintext
- Store only: `PBKDF2(password, salt, iterations)` hash in footer
- Salt stored in footer for verification

### 6.3 Footer Structure
```json
{
  "version": "1.0.0",
  "isEncrypted": true,
  "salt": "base64-encoded-salt",
  "passwordHash": "base64-encoded-hash",
  "accountants": ["Robert Wilson", "Maria Garcia"],
  "createdAt": "2024-01-01T00:00:00",
  "modifiedAt": "2024-12-01T10:30:00"
}
```

Footer is appended after content with a length marker for easy parsing.

---

## 7. Migration & Versioning

### 7.1 Version Compatibility
- Store app version in file footer
- On open, compare file version with app version
- If file version is newer than app, show error (cannot open)
- If file version is older than app, offer migration

### 7.2 Migration Strategy
Each version upgrade should have a migration function:
```csharp
public interface IMigration
{
    string FromVersion { get; }
    string ToVersion { get; }
    void Migrate(CompanyData data);
}

// Example migration
public class Migration_1_0_to_1_1 : IMigration
{
    public string FromVersion => "1.0.0";
    public string ToVersion => "1.1.0";

    public void Migrate(CompanyData data)
    {
        // Add new field with default value
        foreach (var invoice in data.Invoices)
        {
            invoice.ReminderSettings ??= new ReminderSettings
            {
                Day1 = true,
                Day7 = true,
                Day14 = false,
                Day30 = false
            };
        }
    }
}
```

### 7.3 Backup Before Migration
- Always create automatic backup before migration
- Store backup in same directory with `.backup` suffix
- If migration fails, restore from backup

---

## Appendix A: ID Generation

All IDs follow the pattern: `PREFIX-SEQUENCE`

| Entity | Prefix | Example |
|--------|--------|---------|
| Sale | SAL | SAL-2024-00001 |
| Purchase | PUR | PUR-2024-00001 |
| Invoice | INV | INV-2024-00001 |
| Payment | PAY | PAY-2024-00001 |
| Customer | CUS | CUS-001 |
| Product | PRD | PRD-001 |
| Supplier | SUP | SUP-001 |
| Employee | EMP | EMP-001 |
| Department | DEP | DEP-001 |
| Category | CAT-XXX | CAT-SAL-001 |
| Accountant | ACC | ACC-001 |
| Inventory Item | INV-ITM | INV-ITM-001 |
| Location | LOC | LOC-001 |
| Adjustment | ADJ | ADJ-001 |
| Transfer | TRF | TRF-001 |
| Purchase Order | PO | PO-001 |
| Rental Item | RNT-ITM | RNT-ITM-001 |
| Rental Record | RNT | RNT-001 |
| Return | RET | RET-001 |
| Lost/Damaged | LOST | LOST-001 |
| Receipt | RCP | RCP-001 |
| Template | TPL | TPL-001 |
| Recurring Invoice | REC-INV | REC-INV-001 |

---

## Appendix B: Status Enumerations

### Invoice Status
- Draft
- Pending
- Sent
- Viewed
- Partial (partial payment received)
- Paid
- Overdue
- Cancelled

### Rental Status
- Active
- Returned
- Overdue
- Cancelled

### Purchase Order Status
- Draft
- Sent
- PartiallyReceived
- FullyReceived
- Cancelled

### Stock Transfer Status
- Pending
- InTransit
- Completed
- Cancelled

### Return Status
- Pending
- Approved
- Completed
- Rejected

### Employee Status
- Active
- OnLeave
- Terminated

### Inventory Status
- InStock (above reorder point)
- LowStock (at or below reorder point)
- OutOfStock (zero quantity)
- Overstock (above overstock threshold)

---

## Appendix C: Supported File Types

### Receipt Attachments
- JPEG (.jpg, .jpeg)
- PNG (.png)
- PDF (.pdf)
- Maximum size: 10 MB per file

### Import Files
- Excel (.xlsx)

### Export Files
- Excel (.xlsx)
- CSV (.csv)
- PDF (.pdf)

### Company Logo
- JPEG (.jpg, .jpeg)
- PNG (.png)
- Maximum source size: 5 MB
- Automatically resized to 200x200 pixels on import
