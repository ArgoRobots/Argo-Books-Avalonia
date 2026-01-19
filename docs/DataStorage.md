# Data Storage

Argo Books uses a file-based storage system with encrypted `.argo` files instead of a traditional database.

## CompanyManager

Central orchestrator for all company file operations.

- Company file lifecycle management
- Temporary directory management
- Save/load coordination
- Encryption coordination
- Auto-save functionality
- File locking

### CompanyManager Operations

Load File:

![Security Architecture](diagrams/data-storage/company-manager-load-file.svg)

Save File:

![Security Architecture](diagrams/data-storage/company-manager-save-file.svg)

## `.argo` File Format

![Security Architecture](diagrams/data-storage/argo-file-format.svg)

## Global Settings

Application-wide settings stored separately.

- Recent files list
- User preferences
- Application state persistence

## Benefits of File-Based Storage

| Benefit | Description |
|---------|-------------|
| **Portability** | Files can be copied, emailed, backed up easily |
| **No Database** | No server or database installation required |
| **Performance** | All data in memory = fast operations |
| **Privacy** | Data stays local, encrypted on disk |
| **Simplicity** | Single file per company |
