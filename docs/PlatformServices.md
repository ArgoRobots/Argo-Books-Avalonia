# Platform Services

Platform Services provide cross-platform abstraction for Windows, macOS, Linux, and WebAssembly.

## Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                    DIAGRAM PLACEHOLDER                          │
│                                                                 │
│  Platform Abstraction Layer (PlatformServices.svg)              │
│                                                                 │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │                    Application Code                      │   │
│  │                                                          │   │
│  │         Uses: IPlatformService interface                 │   │
│  └────────────────────────┬────────────────────────────────┘   │
│                           │                                     │
│                           ▼                                     │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │              IPlatformService Interface                  │   │
│  │  • GetPlatform()     • OpenFileDialog()                 │   │
│  │  • SaveFileDialog()  • GetAppDataPath()                 │   │
│  │  • HasBiometrics()   • OpenUrl()                        │   │
│  └────────────────────────┬────────────────────────────────┘   │
│                           │                                     │
│       ┌───────────────────┼───────────────────┐                │
│       ▼                   ▼                   ▼                 │
│  ┌─────────┐       ┌──────────┐        ┌───────────┐          │
│  │ Windows │       │   Mac    │        │   Linux   │          │
│  │ Service │       │ Service  │        │  Service  │          │
│  └─────────┘       └──────────┘        └───────────┘          │
│       │                 │                    │                  │
│       ▼                 ▼                    ▼                  │
│  ┌─────────┐       ┌──────────┐        ┌───────────┐          │
│  │Windows  │       │  macOS   │        │  Linux    │          │
│  │  APIs   │       │   APIs   │        │   APIs    │          │
│  └─────────┘       └──────────┘        └───────────┘          │
│                                                                 │
│                           ┌───────────┐                        │
│                           │  Browser  │                        │
│                           │  Service  │                        │
│                           └─────┬─────┘                        │
│                                 ▼                               │
│                           ┌───────────┐                        │
│                           │    WASM   │                        │
│                           │   + JS    │                        │
│                           └───────────┘                        │
│                                                                 │
│  Show: Interface-based platform abstraction                     │
│  Include: All four platform implementations                     │
└─────────────────────────────────────────────────────────────────┘
```

## IPlatformService Interface

Common interface for platform-specific operations.

| Component | Location |
|-----------|----------|
| Interface | `ArgoBooks.Core/Services/Platform/IPlatformService.cs` |

### Methods

| Method | Description |
|--------|-------------|
| `GetPlatform()` | Returns current OS identifier |
| `OpenFileDialog()` | Shows native file picker |
| `SaveFileDialog()` | Shows native save dialog |
| `GetAppDataPath()` | Returns platform-specific app data directory |
| `HasBiometrics()` | Checks if biometric auth is available |
| `AuthenticateBiometric()` | Triggers biometric authentication |
| `OpenUrl()` | Opens URL in default browser |
| `GetClipboard()` | Clipboard read access |
| `SetClipboard()` | Clipboard write access |

## Windows Platform Service

Windows-specific implementation.

| Component | Location |
|-----------|----------|
| Implementation | `ArgoBooks.Core/Services/Platform/WindowsPlatformService.cs` |

### Features
- Windows Hello biometric support
- Native Win32 file dialogs
- Windows registry access
- AppData folder handling

### Windows-Specific Flow

```
┌─────────────────────────────────────────────────────────────────┐
│                    DIAGRAM PLACEHOLDER                          │
│                                                                 │
│  Windows Hello Flow (WindowsHello.svg)                          │
│                                                                 │
│  ┌─────────────────┐                                           │
│  │ Auth Request    │                                           │
│  └────────┬────────┘                                           │
│           │                                                     │
│           ▼                                                     │
│  ┌─────────────────┐    No    ┌─────────────────┐             │
│  │ HasBiometrics() │─────────▶│ Fall back to    │             │
│  └────────┬────────┘          │ Password        │             │
│           │ Yes               └─────────────────┘             │
│           ▼                                                     │
│  ┌─────────────────┐                                           │
│  │ Windows Hello   │                                           │
│  │ API             │                                           │
│  └────────┬────────┘                                           │
│           │                                                     │
│    ┌──────┴──────┐                                             │
│    ▼             ▼                                             │
│ ┌───────┐   ┌───────┐                                         │
│ │Success│   │Failed │                                         │
│ └───┬───┘   └───┬───┘                                         │
│     │           │                                               │
│     ▼           ▼                                               │
│ ┌───────┐   ┌───────┐                                         │
│ │Unlock │   │Retry/ │                                         │
│ │File   │   │Cancel │                                         │
│ └───────┘   └───────┘                                         │
│                                                                 │
│  Show: Windows Hello biometric authentication                   │
│  Include: Fallback path                                         │
└─────────────────────────────────────────────────────────────────┘
```

## macOS Platform Service

macOS-specific implementation.

| Component | Location |
|-----------|----------|
| Implementation | `ArgoBooks.Core/Services/Platform/MacPlatformService.cs` |

### Features
- Touch ID support (where available)
- Native NSOpenPanel/NSSavePanel dialogs
- Application Support folder handling
- macOS keychain integration

## Linux Platform Service

Linux-specific implementation.

| Component | Location |
|-----------|----------|
| Implementation | `ArgoBooks.Core/Services/Platform/LinuxPlatformService.cs` |

### Features
- GTK file dialogs (via Zenity/kdialog)
- XDG directory specification compliance
- freedesktop.org standards
- Multiple desktop environment support

## Browser Platform Service

WebAssembly/Browser-specific implementation.

| Component | Location |
|-----------|----------|
| Implementation | `ArgoBooks.Core/Services/Platform/BrowserPlatformService.cs` |
| Entry Point | `ArgoBooks.Browser/` project |

### Features
- JavaScript interop for file operations
- IndexedDB for local storage
- Browser download API for file saves
- No biometric support (browser limitation)

### Browser File Operations

```
┌─────────────────────────────────────────────────────────────────┐
│                    DIAGRAM PLACEHOLDER                          │
│                                                                 │
│  Browser File Operations (BrowserFileOps.svg)                   │
│                                                                 │
│  Open File:                                                     │
│  ┌───────────────┐    ┌──────────────┐    ┌─────────────────┐ │
│  │ User clicks   │───▶│ JS Interop   │───▶│ <input          │ │
│  │ "Open"        │    │              │    │  type="file">   │ │
│  └───────────────┘    └──────────────┘    └────────┬────────┘ │
│                                                    │           │
│                                                    ▼           │
│                                           ┌─────────────────┐ │
│                                           │ Read via        │ │
│                                           │ FileReader API  │ │
│                                           └────────┬────────┘ │
│                                                    │           │
│                                                    ▼           │
│                                           ┌─────────────────┐ │
│                                           │ Return bytes    │ │
│                                           │ to WASM         │ │
│                                           └─────────────────┘ │
│                                                                │
│  Save File:                                                    │
│  ┌───────────────┐    ┌──────────────┐    ┌─────────────────┐ │
│  │ User clicks   │───▶│ JS Interop   │───▶│ Create Blob     │ │
│  │ "Save"        │    │              │    │ & Download Link │ │
│  └───────────────┘    └──────────────┘    └────────┬────────┘ │
│                                                    │           │
│                                                    ▼           │
│                                           ┌─────────────────┐ │
│                                           │ Trigger         │ │
│                                           │ Download        │ │
│                                           └─────────────────┘ │
│                                                                │
│  Show: Browser file I/O via JS interop                         │
│  Include: FileReader and Download APIs                         │
└─────────────────────────────────────────────────────────────────┘
```

## Entry Points

Each platform has its own entry point project.

| Platform | Project | Entry Point |
|----------|---------|-------------|
| Desktop | `ArgoBooks.Desktop/` | Windows, macOS, Linux |
| Browser | `ArgoBooks.Browser/` | WebAssembly |

### Desktop Startup

```
┌─────────────────────────────────────────────────────────────────┐
│                    DIAGRAM PLACEHOLDER                          │
│                                                                 │
│  Desktop Startup Flow (DesktopStartup.svg)                      │
│                                                                 │
│  ┌─────────────────┐                                           │
│  │ Program.Main()  │                                           │
│  └────────┬────────┘                                           │
│           │                                                     │
│           ▼                                                     │
│  ┌─────────────────┐                                           │
│  │ Detect Platform │                                           │
│  └────────┬────────┘                                           │
│           │                                                     │
│    ┌──────┼──────┐                                             │
│    ▼      ▼      ▼                                             │
│ ┌─────┐┌─────┐┌─────┐                                         │
│ │Win  ││Mac  ││Linux│                                         │
│ └──┬──┘└──┬──┘└──┬──┘                                         │
│    │      │      │                                              │
│    └──────┼──────┘                                             │
│           ▼                                                     │
│  ┌─────────────────┐                                           │
│  │ Register        │                                           │
│  │ Platform Service│                                           │
│  └────────┬────────┘                                           │
│           │                                                     │
│           ▼                                                     │
│  ┌─────────────────┐                                           │
│  │ Build Avalonia  │                                           │
│  │ App             │                                           │
│  └────────┬────────┘                                           │
│           │                                                     │
│           ▼                                                     │
│  ┌─────────────────┐                                           │
│  │ Run MainWindow  │                                           │
│  └─────────────────┘                                           │
│                                                                 │
│  Show: Platform detection and service registration              │
│  Include: Platform-specific branching                           │
└─────────────────────────────────────────────────────────────────┘
```

### Browser Startup

```
┌─────────────────────────────────────────────────────────────────┐
│                    DIAGRAM PLACEHOLDER                          │
│                                                                 │
│  Browser Startup Flow (BrowserStartup.svg)                      │
│                                                                 │
│  ┌─────────────────┐                                           │
│  │ index.html      │                                           │
│  │ loads           │                                           │
│  └────────┬────────┘                                           │
│           │                                                     │
│           ▼                                                     │
│  ┌─────────────────┐                                           │
│  │ Load WASM       │                                           │
│  │ Runtime         │                                           │
│  └────────┬────────┘                                           │
│           │                                                     │
│           ▼                                                     │
│  ┌─────────────────┐                                           │
│  │ Initialize      │                                           │
│  │ .NET Runtime    │                                           │
│  └────────┬────────┘                                           │
│           │                                                     │
│           ▼                                                     │
│  ┌─────────────────┐                                           │
│  │ Register        │                                           │
│  │ BrowserPlatform │                                           │
│  │ Service         │                                           │
│  └────────┬────────┘                                           │
│           │                                                     │
│           ▼                                                     │
│  ┌─────────────────┐                                           │
│  │ Render App      │                                           │
│  │ in Canvas       │                                           │
│  └─────────────────┘                                           │
│                                                                 │
│  Show: WebAssembly initialization flow                          │
│  Include: .NET runtime bootstrap                                │
└─────────────────────────────────────────────────────────────────┘
```

## Platform Capabilities Matrix

```
┌─────────────────────────────────────────────────────────────────┐
│                    DIAGRAM PLACEHOLDER                          │
│                                                                 │
│  Platform Capabilities Matrix (PlatformMatrix.svg)              │
│                                                                 │
│  ┌──────────────────┬─────────┬───────┬───────┬─────────┐     │
│  │ Feature          │ Windows │ macOS │ Linux │ Browser │     │
│  ├──────────────────┼─────────┼───────┼───────┼─────────┤     │
│  │ File Dialogs     │   ✓     │   ✓   │   ✓   │   ✓*    │     │
│  │ Biometrics       │   ✓     │   ✓   │   ✗   │   ✗     │     │
│  │ Native Menus     │   ✓     │   ✓   │   ✓   │   ✗     │     │
│  │ System Tray      │   ✓     │   ✓   │   ✓   │   ✗     │     │
│  │ Local Storage    │   ✓     │   ✓   │   ✓   │   ✓**   │     │
│  │ Clipboard        │   ✓     │   ✓   │   ✓   │   ✓     │     │
│  │ Drag & Drop      │   ✓     │   ✓   │   ✓   │   ✓     │     │
│  │ Print            │   ✓     │   ✓   │   ✓   │   ✓     │     │
│  └──────────────────┴─────────┴───────┴───────┴─────────┘     │
│                                                                 │
│  *  Via browser file APIs                                       │
│  ** Via IndexedDB with size limits                              │
│                                                                 │
│  Show: Feature availability across platforms                    │
│  Include: Legend for partial support                            │
└─────────────────────────────────────────────────────────────────┘
```

## Dependency Injection

Platform services are registered at startup:

```csharp
// Desktop (Windows example)
services.AddSingleton<IPlatformService, WindowsPlatformService>();

// Browser
services.AddSingleton<IPlatformService, BrowserPlatformService>();
```

The rest of the application only depends on `IPlatformService`, making it platform-agnostic.
