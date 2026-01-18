# Data Storage

Argo Books uses a file-based storage system with encrypted `.argo` files instead of a traditional database.

## Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                    DIAGRAM PLACEHOLDER                          │
│                                                                 │
│  Data Storage Architecture (DataStorage.svg)                    │
│                                                                 │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │                    Application                           │   │
│  │  ┌──────────────────────────────────────────────────┐   │   │
│  │  │               CompanyManager                      │   │   │
│  │  │  (Orchestrates all file operations)              │   │   │
│  │  └─────────────────────┬────────────────────────────┘   │   │
│  │                        │                                 │   │
│  │       ┌────────────────┼────────────────┐               │   │
│  │       ▼                ▼                ▼               │   │
│  │  ┌─────────┐    ┌────────────┐   ┌─────────────┐       │   │
│  │  │Encryption│    │Compression │   │  FileService│       │   │
│  │  │ Service  │    │  Service   │   │             │       │   │
│  │  └─────────┘    └────────────┘   └─────────────┘       │   │
│  └─────────────────────────────────────────────────────────┘   │
│                        │                                        │
│                        ▼                                        │
│               ┌────────────────┐                               │
│               │   .argo file   │                               │
│               │  (Encrypted)   │                               │
│               └────────────────┘                               │
│                                                                 │
│  Show: How components work together for file I/O                │
│  Include: Encryption and compression layers                     │
└─────────────────────────────────────────────────────────────────┘
```

## CompanyManager

Central orchestrator for all company file operations.

| Component | Location |
|-----------|----------|
| Service | `ArgoBooks.Core/Services/CompanyManager.cs` |

### Responsibilities
- Company file lifecycle management
- Temporary directory management
- Save/load coordination
- Encryption coordination
- Auto-save functionality
- File locking

### CompanyManager Operations

```
┌─────────────────────────────────────────────────────────────────┐
│                    DIAGRAM PLACEHOLDER                          │
│                                                                 │
│  CompanyManager Operations (CompanyManagerOps.svg)              │
│                                                                 │
│  Load File:                                                     │
│  ┌────────┐    ┌──────────┐    ┌────────────┐    ┌──────────┐ │
│  │.argo   │───▶│ Decrypt  │───▶│Decompress  │───▶│CompanyData│ │
│  │file    │    │          │    │            │    │(In Memory)│ │
│  └────────┘    └──────────┘    └────────────┘    └──────────┘ │
│                                                                 │
│  Save File:                                                     │
│  ┌──────────┐    ┌────────────┐    ┌──────────┐    ┌────────┐ │
│  │CompanyData│───▶│ Compress   │───▶│ Encrypt  │───▶│.argo   │ │
│  │(In Memory)│    │            │    │          │    │file    │ │
│  └──────────┘    └────────────┘    └──────────┘    └────────┘ │
│                                                                 │
│  Show: Bidirectional file operations                            │
│  Include: Processing pipeline steps                             │
└─────────────────────────────────────────────────────────────────┘
```

## CompanyData

Main data container holding all business entities.

| Component | Location |
|-----------|----------|
| Model | `ArgoBooks.Core/Data/CompanyData.cs` |

### Structure

```
┌─────────────────────────────────────────────────────────────────┐
│                    DIAGRAM PLACEHOLDER                          │
│                                                                 │
│  CompanyData Structure (CompanyDataStructure.svg)               │
│                                                                 │
│  CompanyData                                                    │
│  ├── CompanySettings                                            │
│  │   ├── LocalizationSettings                                   │
│  │   ├── AppearanceSettings                                     │
│  │   ├── EnabledModulesSettings                                 │
│  │   ├── NotificationSettings                                   │
│  │   └── SecuritySettings                                       │
│  ├── Customers[]                                                │
│  ├── Suppliers[]                                                │
│  ├── Products[]                                                 │
│  ├── Categories[]                                               │
│  ├── Invoices[]                                                 │
│  ├── Payments[]                                                 │
│  ├── Expenses[]                                                 │
│  ├── Sales[]                                                    │
│  ├── PurchaseOrders[]                                           │
│  ├── RentalItems[]                                              │
│  ├── RentalRecords[]                                            │
│  ├── StockAdjustments[]                                         │
│  ├── Locations[]                                                │
│  ├── Departments[]                                              │
│  └── ...more collections                                        │
│                                                                 │
│  Show: Full CompanyData hierarchy                               │
│  Include: All major collection types                            │
└─────────────────────────────────────────────────────────────────┘
```

## File Service

Low-level file I/O operations.

| Component | Location |
|-----------|----------|
| Interface | `ArgoBooks.Core/Services/IFileService.cs` |
| Implementation | `ArgoBooks.Core/Services/FileService.cs` |

### Features
- File read/write operations
- Directory management
- File existence validation
- Path utilities

## Compression Service

Reduces file size for efficient storage.

| Component | Location |
|-----------|----------|
| Service | `ArgoBooks.Core/Services/CompressionService.cs` |

### Features
- GZip compression
- Transparent compression/decompression
- Size optimization

## File Format

The `.argo` file format:

```
┌─────────────────────────────────────────────────────────────────┐
│                    DIAGRAM PLACEHOLDER                          │
│                                                                 │
│  .argo File Format (ArgoFileFormat.svg)                         │
│                                                                 │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │                    .argo File                            │   │
│  ├─────────────────────────────────────────────────────────┤   │
│  │  Header                                                  │   │
│  │  ├── Magic bytes                                         │   │
│  │  ├── Version                                             │   │
│  │  └── Flags (encrypted, compressed)                       │   │
│  ├─────────────────────────────────────────────────────────┤   │
│  │  Encrypted Payload                                       │   │
│  │  └── Compressed JSON                                     │   │
│  │      └── CompanyData (serialized)                        │   │
│  ├─────────────────────────────────────────────────────────┤   │
│  │  Footer                                                  │   │
│  │  └── Checksum / Metadata                                 │   │
│  └─────────────────────────────────────────────────────────┘   │
│                                                                 │
│  Show: Binary file structure                                    │
│  Include: Header, payload, footer sections                      │
└─────────────────────────────────────────────────────────────────┘
```

## Footer Service

Manages file footer metadata.

| Component | Location |
|-----------|----------|
| Service | `ArgoBooks.Core/Services/FooterService.cs` |

### Features
- Version tracking
- File integrity validation
- Metadata storage

## In-Memory Data Model

All data is loaded into memory for fast operations:

```
┌─────────────────────────────────────────────────────────────────┐
│                    DIAGRAM PLACEHOLDER                          │
│                                                                 │
│  In-Memory Data Flow (InMemoryFlow.svg)                         │
│                                                                 │
│       ┌────────────┐                                           │
│       │ .argo File │                                           │
│       └─────┬──────┘                                           │
│             │ Load (startup)                                    │
│             ▼                                                   │
│  ┌─────────────────────────────────────────┐                   │
│  │           In-Memory                      │                   │
│  │  ┌─────────────────────────────────┐    │                   │
│  │  │         CompanyData             │    │                   │
│  │  │  (All entities loaded)          │    │                   │
│  │  └─────────────────────────────────┘    │                   │
│  │           ▲           │                  │                   │
│  │    Read   │           │ Write            │                   │
│  │           │           ▼                  │                   │
│  │  ┌─────────────────────────────────┐    │                   │
│  │  │      ViewModels / Services       │    │                   │
│  │  └─────────────────────────────────┘    │                   │
│  └─────────────────────────────────────────┘                   │
│             │ Save (manual/auto)                                │
│             ▼                                                   │
│       ┌────────────┐                                           │
│       │ .argo File │                                           │
│       └────────────┘                                           │
│                                                                 │
│  Show: Data lifecycle from file to memory and back              │
│  Include: Read/write operations from ViewModels                 │
└─────────────────────────────────────────────────────────────────┘
```

## Global Settings

Application-wide settings stored separately.

| Component | Location |
|-----------|----------|
| Interface | `ArgoBooks.Core/Services/IGlobalSettingsService.cs` |
| Implementation | `ArgoBooks.Core/Services/GlobalSettingsService.cs` |
| Model | `ArgoBooks.Core/Models/Settings/GlobalSettings.cs` |

### Features
- Recent files list
- User preferences
- Application state persistence
- Stored separately from company files

## Benefits of File-Based Storage

| Benefit | Description |
|---------|-------------|
| **Portability** | Files can be copied, emailed, backed up easily |
| **No Database** | No server or database installation required |
| **Performance** | All data in memory = fast operations |
| **Privacy** | Data stays local, encrypted on disk |
| **Simplicity** | Single file per company |
