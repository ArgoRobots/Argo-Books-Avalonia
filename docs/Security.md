# Security

Argo Books implements multiple layers of security to protect sensitive business data.

## Overview

![Security Architecture](diagrams/security/security-architecture.svg)

## Encryption Service

AES-256 encryption for all sensitive data.

- AES-256-GCM encryption
- Password-based encryption
- Initialization vector (IV) generation
- Authenticated encryption

### Encryption Flow

![Security Architecture](diagrams/security/encryption-flow.svg)

### Decryption Flow

![Security Architecture](diagrams/security/decryption-flow.svg)

## Key Derivation

Secure key generation from passwords.

- PBKDF2 algorithm
- Configurable iterations (high iteration count)
- Salt generation
- Derives 256-bit keys from passwords

### Key Derivation Process

![Security Architecture](diagrams/security/key-derivation.svg)

### Options
- Enable/disable encryption
- Auto-lock timeout duration
- Biometric authentication toggle
- Password change requirements

## Authentication Flow On File Open

![Security Architecture](diagrams/security/authentication-flow.svg)

## Security Best Practices

| Practice | Implementation |
|----------|----------------|
| **No Plaintext Passwords** | Passwords never stored, only derived keys |
| **Secure Memory** | Sensitive data cleared after use |
| **Auto-Lock** | Configurable timeout for idle sessions |
| **Local Storage** | Data never sent to cloud without consent |
