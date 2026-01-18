# Security

Argo Books implements multiple layers of security to protect sensitive business data.

## Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                    DIAGRAM PLACEHOLDER                          │
│                                                                 │
│  Security Architecture (Security.svg)                           │
│                                                                 │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │                  Security Layers                         │   │
│  │                                                          │   │
│  │  ┌────────────────────────────────────────────────────┐ │   │
│  │  │  Application Layer                                  │ │   │
│  │  │  ├── Password Authentication                        │ │   │
│  │  │  ├── Biometric Support (Windows Hello)             │ │   │
│  │  │  └── Auto-Lock Timeout                             │ │   │
│  │  └────────────────────────────────────────────────────┘ │   │
│  │  ┌────────────────────────────────────────────────────┐ │   │
│  │  │  Data Layer                                         │ │   │
│  │  │  ├── AES-256 Encryption                            │ │   │
│  │  │  ├── Key Derivation (PBKDF2)                       │ │   │
│  │  │  └── Secure Memory Handling                        │ │   │
│  │  └────────────────────────────────────────────────────┘ │   │
│  │  ┌────────────────────────────────────────────────────┐ │   │
│  │  │  Licensing Layer                                    │ │   │
│  │  │  └── License Validation                            │ │   │
│  │  └────────────────────────────────────────────────────┘ │   │
│  └─────────────────────────────────────────────────────────┘   │
│                                                                 │
│  Show: Layered security architecture                            │
│  Include: All security components                               │
└─────────────────────────────────────────────────────────────────┘
```

## Encryption Service

AES-256 encryption for all sensitive data.

| Component | Location |
|-----------|----------|
| Interface | `ArgoBooks.Core/Services/Security/IEncryptionService.cs` |
| Implementation | `ArgoBooks.Core/Services/Security/EncryptionService.cs` |

### Features
- AES-256-GCM encryption
- Password-based encryption
- Initialization vector (IV) generation
- Authenticated encryption

### Encryption Flow

```
┌─────────────────────────────────────────────────────────────────┐
│                    DIAGRAM PLACEHOLDER                          │
│                                                                 │
│  Encryption Flow (EncryptionFlow.svg)                           │
│                                                                 │
│  Encrypt:                                                       │
│  ┌──────────┐    ┌────────────┐    ┌──────────┐               │
│  │ Password │───▶│    Key     │───▶│  AES-256 │               │
│  └──────────┘    │ Derivation │    │  Encrypt │               │
│                  │  (PBKDF2)  │    └────┬─────┘               │
│                  └────────────┘         │                      │
│                                         ▼                      │
│  ┌──────────┐                    ┌──────────┐                 │
│  │Plaintext │───────────────────▶│Ciphertext│                 │
│  │  Data    │                    │  + IV    │                 │
│  └──────────┘                    └──────────┘                 │
│                                                                 │
│  Decrypt:                                                       │
│  ┌──────────┐    ┌────────────┐    ┌──────────┐               │
│  │ Password │───▶│    Key     │───▶│  AES-256 │               │
│  └──────────┘    │ Derivation │    │  Decrypt │               │
│                  │  (PBKDF2)  │    └────┬─────┘               │
│                  └────────────┘         │                      │
│                                         ▼                      │
│  ┌──────────┐                    ┌──────────┐                 │
│  │Ciphertext│───────────────────▶│Plaintext │                 │
│  │  + IV    │                    │  Data    │                 │
│  └──────────┘                    └──────────┘                 │
│                                                                 │
│  Show: Symmetric encryption/decryption process                  │
│  Include: Key derivation step                                   │
└─────────────────────────────────────────────────────────────────┘
```

## Key Derivation

Secure key generation from passwords.

| Component | Location |
|-----------|----------|
| Service | `ArgoBooks.Core/Services/Security/KeyDerivation.cs` |

### Features
- PBKDF2 algorithm
- Configurable iterations (high iteration count)
- Salt generation
- Derives 256-bit keys from passwords

### Key Derivation Process

```
┌─────────────────────────────────────────────────────────────────┐
│                    DIAGRAM PLACEHOLDER                          │
│                                                                 │
│  Key Derivation (KeyDerivation.svg)                             │
│                                                                 │
│  ┌──────────┐    ┌──────────┐                                  │
│  │ Password │    │   Salt   │                                  │
│  └────┬─────┘    └────┬─────┘                                  │
│       │               │                                         │
│       └───────┬───────┘                                        │
│               ▼                                                 │
│       ┌──────────────┐                                         │
│       │    PBKDF2    │                                         │
│       │  (100,000+   │                                         │
│       │  iterations) │                                         │
│       └──────┬───────┘                                         │
│              │                                                  │
│              ▼                                                  │
│       ┌──────────────┐                                         │
│       │  256-bit Key │                                         │
│       │  (for AES)   │                                         │
│       └──────────────┘                                         │
│                                                                 │
│  Show: PBKDF2 key derivation                                    │
│  Include: Inputs (password + salt) and output (key)             │
└─────────────────────────────────────────────────────────────────┘
```

## Password Validation

Enforces password strength requirements.

| Component | Location |
|-----------|----------|
| Service | `ArgoBooks.Core/Services/Security/PasswordValidator.cs` |

### Requirements
- Minimum length
- Uppercase letters
- Lowercase letters
- Numbers
- Special characters (optional)

### Validation Rules

```
┌─────────────────────────────────────────────────────────────────┐
│                    DIAGRAM PLACEHOLDER                          │
│                                                                 │
│  Password Validation Rules (PasswordRules.svg)                  │
│                                                                 │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │                 Password Validation                      │   │
│  │                                                          │   │
│  │  ☐ Minimum 8 characters                                 │   │
│  │  ☐ At least 1 uppercase (A-Z)                           │   │
│  │  ☐ At least 1 lowercase (a-z)                           │   │
│  │  ☐ At least 1 number (0-9)                              │   │
│  │  ☐ At least 1 special character (!@#$%...)              │   │
│  │                                                          │   │
│  │  Password Strength: [████████░░] Strong                 │   │
│  └─────────────────────────────────────────────────────────┘   │
│                                                                 │
│  Show: Password validation checklist                            │
│  Include: Visual strength indicator                             │
└─────────────────────────────────────────────────────────────────┘
```

## Security Settings

Per-company security configuration.

| Component | Location |
|-----------|----------|
| Model | `ArgoBooks.Core/Models/Settings/SecuritySettings.cs` |

### Options
- Enable/disable encryption
- Auto-lock timeout duration
- Biometric authentication toggle
- Password change requirements

## License Service

License validation and feature gating.

| Component | Location |
|-----------|----------|
| Service | `ArgoBooks.Core/Services/LicenseService.cs` |

### Features
- License key validation
- Feature unlock verification
- Trial period management
- Online validation (optional)

### License Validation Flow

```
┌─────────────────────────────────────────────────────────────────┐
│                    DIAGRAM PLACEHOLDER                          │
│                                                                 │
│  License Validation Flow (LicenseValidation.svg)                │
│                                                                 │
│  ┌────────────┐    ┌─────────────────┐    ┌───────────────┐   │
│  │License Key │───▶│  Validate Key   │───▶│ Check Expiry  │   │
│  └────────────┘    │  (Format/Sign)  │    │               │   │
│                    └─────────────────┘    └───────┬───────┘   │
│                                                   │            │
│                                    ┌──────────────┴──────┐    │
│                                    ▼                     ▼    │
│                             ┌──────────┐          ┌──────────┐│
│                             │  Valid   │          │ Invalid  ││
│                             └────┬─────┘          └────┬─────┘│
│                                  │                     │      │
│                                  ▼                     ▼      │
│                          ┌────────────┐        ┌────────────┐ │
│                          │  Unlock    │        │  Show      │ │
│                          │  Features  │        │  Trial/Buy │ │
│                          └────────────┘        └────────────┘ │
│                                                                │
│  Show: License validation process                              │
│  Include: Valid/invalid paths                                  │
└─────────────────────────────────────────────────────────────────┘
```

## Authentication Flow

User authentication on file open:

```
┌─────────────────────────────────────────────────────────────────┐
│                    DIAGRAM PLACEHOLDER                          │
│                                                                 │
│  Authentication Flow (AuthFlow.svg)                             │
│                                                                 │
│  ┌────────────┐                                                │
│  │ Open File  │                                                │
│  └─────┬──────┘                                                │
│        │                                                        │
│        ▼                                                        │
│  ┌─────────────────┐    No    ┌─────────────────┐             │
│  │ Is Encrypted?   │─────────▶│   Load File     │             │
│  └────────┬────────┘          └─────────────────┘             │
│           │ Yes                                                 │
│           ▼                                                     │
│  ┌─────────────────┐                                           │
│  │ Show Password   │                                           │
│  │    Prompt       │                                           │
│  └────────┬────────┘                                           │
│           │                                                     │
│           ▼                                                     │
│  ┌─────────────────┐                                           │
│  │ Biometric       │◀─── Windows Hello (if enabled)            │
│  │ Available?      │                                           │
│  └────────┬────────┘                                           │
│           │                                                     │
│           ▼                                                     │
│  ┌─────────────────┐   Fail   ┌─────────────────┐             │
│  │ Verify Password │─────────▶│  Access Denied  │             │
│  └────────┬────────┘          └─────────────────┘             │
│           │ Success                                             │
│           ▼                                                     │
│  ┌─────────────────┐                                           │
│  │  Decrypt File   │                                           │
│  └────────┬────────┘                                           │
│           │                                                     │
│           ▼                                                     │
│  ┌─────────────────┐                                           │
│  │   Load Data     │                                           │
│  └─────────────────┘                                           │
│                                                                 │
│  Show: Complete authentication workflow                         │
│  Include: Encryption check, biometric option, error handling    │
└─────────────────────────────────────────────────────────────────┘
```

## Security Best Practices

| Practice | Implementation |
|----------|----------------|
| **Encryption at Rest** | All `.argo` files encrypted with AES-256 |
| **No Plaintext Passwords** | Passwords never stored, only derived keys |
| **Secure Memory** | Sensitive data cleared after use |
| **Auto-Lock** | Configurable timeout for idle sessions |
| **Local Storage** | Data never sent to cloud without consent |
