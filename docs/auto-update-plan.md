# Cross-Platform Auto-Update System — Implementation Plan

## 1. Overview

Port the working auto-update system from the WinForms app to the Avalonia app, making it cross-platform (Windows, macOS, Linux). The Browser target is excluded since the browser handles its own updates.

### Current State in Avalonia

| What exists | Status |
|---|---|
| `NetSparkleUpdater.UI.Avalonia v2.3.0` NuGet ref | Referenced in `ArgoBooks.Desktop.csproj`, unused |
| `CheckForUpdateModalViewModel` | Stub — simulates checking with `Task.Delay(1500)` |
| `CheckForUpdateModal.axaml` | Fully designed UI with all states (checking, up-to-date, update available, error) |
| `IPlatformService.SupportsAutoUpdate` | Defined and implemented per platform |
| `AppInfo.Version` / `AppInfo.VersionNumber` | Working version string from assembly |

### What the WinForms System Does (reference implementation)

1. On startup, `NetSparkleUpdateManager.Initialize()` creates a `SparkleUpdater` pointed at `https://argorobots.com/update.xml`
2. When the main menu loads, `CheckForUpdates()` runs in background
3. If update found, `Updates_Form` opens showing the new version
4. User clicks "Update" → downloads installer `.exe` to temp dir via `HttpClient`
5. User clicks "Restart to apply" → saves data, launches installer with `/exenoui /norestart`, exits app
6. After restart, `AutoOpenRecentAfterUpdate` flag auto-opens the last company file

---

## 2. Architecture

### Layer Diagram

```
┌─────────────────────────────────────────────────────┐
│  CheckForUpdateModal.axaml  (existing UI - modify)  │  ArgoBooks (UI)
│  CheckForUpdateModalViewModel  (rewrite internals)  │
└──────────────────────┬──────────────────────────────┘
                       │ depends on
┌──────────────────────▼──────────────────────────────┐
│  IUpdateService  (new interface)                     │  ArgoBooks.Core
│  UpdateInfo, UpdateState  (new models)               │
└──────────────────────┬──────────────────────────────┘
                       │ implemented by
┌──────────────────────▼──────────────────────────────┐
│  NetSparkleUpdateService  (new, NetSparkle wrapper)  │  ArgoBooks.Desktop
│  Platform-specific install logic                     │
└─────────────────────────────────────────────────────┘
```

The key architectural choice: put the **interface** in `ArgoBooks.Core` (no NetSparkle dependency) and the **implementation** in `ArgoBooks.Desktop` (which already references `NetSparkleUpdater.UI.Avalonia`). This keeps the core and UI projects clean and testable.

---

## 3. Step-by-Step Implementation

### Step 1: Define the Update Service Interface and Models

**New file: `ArgoBooks.Core/Services/IUpdateService.cs`**

```csharp
public interface IUpdateService
{
    /// Checks the appcast for a newer version. Returns null if up-to-date.
    Task<UpdateInfo?> CheckForUpdateAsync();

    /// Downloads the update. Fires DownloadProgressChanged during download.
    Task DownloadUpdateAsync(UpdateInfo update);

    /// Applies the downloaded update and restarts the application.
    void ApplyUpdateAndRestart();

    /// Current state of the update system.
    UpdateState State { get; }

    event EventHandler<UpdateState>? StateChanged;
    event EventHandler<int>? DownloadProgressChanged;  // 0-100
}
```

**New file: `ArgoBooks.Core/Models/Common/UpdateInfo.cs`**

```csharp
public record UpdateInfo(
    string Version,
    string DownloadUrl,
    string? ReleaseNotesUrl,
    long? FileSizeBytes
);
```

**New file: `ArgoBooks.Core/Models/Common/UpdateState.cs`**

```csharp
public enum UpdateState
{
    Idle,
    Checking,
    UpdateAvailable,
    Downloading,
    ReadyToInstall,
    Installing,
    UpToDate,
    Error
}
```

### Step 2: Implement NetSparkleUpdateService in Desktop Project

**New file: `ArgoBooks.Desktop/Services/NetSparkleUpdateService.cs`**

This wraps `SparkleUpdater` following the same pattern as the WinForms `NetSparkleUpdateManager`, but adapted for cross-platform:

```
Initialize SparkleUpdater:
  - AppCast URL: https://argorobots.com/avalonia-update.xml (separate from WinForms)
  - DSAChecker with SecurityMode.UseIfPossible
  - RelaunchAfterUpdate = false
  - UserInteractionMode = silent (we use our own UI)

CheckForUpdateAsync():
  - Call _sparkle.CheckForUpdatesQuietly()
  - Parse result, compare versions using System.Version
  - Return UpdateInfo or null

DownloadUpdateAsync():
  - Use HttpClient (not NetSparkle's built-in downloader, same as WinForms)
  - Download to platform-appropriate temp directory
  - Report progress via DownloadProgressChanged event
  - Save installer path for later

ApplyUpdateAndRestart():
  - Save all user data (via event/callback)
  - Set AutoOpenRecentAfterUpdate flag in GlobalSettings
  - Platform-specific install launch (see Step 3)
  - Exit application
```

### Step 3: Platform-Specific Installation Logic

The appcast XML will contain platform-specific entries using NetSparkle's `os` attribute. The installer download and application differs per platform:

#### Windows
- **Package format**: `.exe` installer (same as WinForms — Inno Setup or similar)
- **Install command**: Launch installer with `/exenoui /norestart`, elevated with `Verb = "runas"`
- **Post-install**: Installer overwrites app files, app relaunches automatically

#### macOS
- **Package format**: `.dmg` or `.zip` containing the `.app` bundle
- **Install command**:
  1. Download `.zip` to temp
  2. Unzip to temp location
  3. Use `rsync` or file copy to replace the current `.app` bundle
  4. Relaunch via `Process.Start("open", "-n path/to/App.app")`
- **Alternative**: Use Sparkle's built-in macOS support (NetSparkle supports this natively)
- **No elevation needed**: macOS apps in `/Applications` owned by user can self-replace

#### Linux
- **Package format**: `.AppImage` (self-contained, easiest for auto-update)
- **Install command**:
  1. Download new `.AppImage` to temp
  2. Copy over the current AppImage file
  3. `chmod +x` the new file
  4. Relaunch via `Process.Start(newAppImagePath)`
- **Alternative formats**: For `.deb`/`.rpm`, open the package manager (less seamless, may just open download in browser)
- **No elevation needed** for AppImage (runs from user directory)

**Implementation approach**: Add a private method `LaunchInstaller()` in `NetSparkleUpdateService` with a platform switch:

```csharp
private void LaunchInstaller()
{
    if (OperatingSystem.IsWindows())
        LaunchWindowsInstaller();
    else if (OperatingSystem.IsMacOS())
        LaunchMacInstaller();
    else if (OperatingSystem.IsLinux())
        LaunchLinuxInstaller();
}
```

### Step 4: Create a Cross-Platform AppCast

**Server-side file: `https://argorobots.com/avalonia-update.xml`**

Use a separate appcast from the WinForms app since versions will diverge. NetSparkle's appcast format supports per-OS entries:

```xml
<?xml version="1.0" encoding="utf-8"?>
<rss version="2.0" xmlns:sparkle="http://www.andymatuschak.org/xml-namespaces/sparkle">
  <channel>
    <title>Argo Books Avalonia</title>
    <item>
      <title>Version 2.1.0</title>
      <sparkle:releaseNotesLink>
        https://argorobots.com/whats-new/
      </sparkle:releaseNotesLink>
      <pubDate>Mon, 01 Jun 2026 00:00:00 +0000</pubDate>

      <!-- Windows -->
      <enclosure
        url="https://argorobots.com/downloads/avalonia/ArgoBooks-2.1.0-win-x64.exe"
        sparkle:version="2.1.0"
        sparkle:os="windows"
        length="85000000"
        type="application/octet-stream" />

      <!-- macOS -->
      <enclosure
        url="https://argorobots.com/downloads/avalonia/ArgoBooks-2.1.0-osx-arm64.zip"
        sparkle:version="2.1.0"
        sparkle:os="macos"
        length="90000000"
        type="application/octet-stream" />

      <!-- Linux -->
      <enclosure
        url="https://argorobots.com/downloads/avalonia/ArgoBooks-2.1.0-linux-x64.AppImage"
        sparkle:version="2.1.0"
        sparkle:os="linux"
        length="95000000"
        type="application/octet-stream" />
    </item>
  </channel>
</rss>
```

### Step 5: Rewrite CheckForUpdateModalViewModel

Replace the stub implementation with real update logic. The existing properties (`IsChecking`, `HasUpdate`, `IsUpToDate`, `HasError`, `NewVersion`, `CurrentVersion`) already match the needed states.

**Changes:**

1. Accept `IUpdateService` as a constructor dependency
2. `CheckForUpdates()` → call `_updateService.CheckForUpdateAsync()` instead of `Task.Delay`
3. Add new states: `IsDownloading`, `DownloadProgress` (0-100), `IsReadyToInstall`
4. `DownloadUpdate()` → call `_updateService.DownloadUpdateAsync()`, show progress
5. Add `ApplyUpdate()` command → call `_updateService.ApplyUpdateAndRestart()`
6. Wire up `StateChanged` and `DownloadProgressChanged` events

### Step 6: Update the Modal AXAML

Add two new visual states to the existing `CheckForUpdateModal.axaml`:

1. **Downloading state**: Progress bar (0-100%) with cancel option, replaces the "Download Update" button
2. **Ready to install state**: "Restart to Apply Update" button (green, like WinForms)

The existing states (checking spinner, up-to-date checkmark, update available, error) remain as-is.

### Step 7: Wire Up Background Check on Startup

In `App.axaml.cs` → `OnFrameworkInitializationCompleted()`:

1. Create `NetSparkleUpdateService` instance (only on desktop, check `SupportsAutoUpdate`)
2. Pass it to `CheckForUpdateModalViewModel`
3. After the main window is shown, trigger a background check:
   ```csharp
   _ = Task.Run(async () =>
   {
       var update = await updateService.CheckForUpdateAsync();
       if (update != null)
       {
           Dispatcher.UIThread.Post(() =>
           {
               checkForUpdateVm.HasUpdate = true;
               checkForUpdateVm.NewVersion = $"V.{update.Version}";
               // Show notification or auto-open modal
           });
       }
   });
   ```

### Step 8: Post-Update Recovery

Port the `AutoOpenRecentAfterUpdate` logic from WinForms:

1. Add an `AutoOpenRecentAfterUpdate` boolean to `GlobalSettingsService`
2. In `ApplyUpdateAndRestart()`, set this flag to `true` before exiting
3. In `App.axaml.cs` startup, after loading settings:
   - Check if `AutoOpenRecentAfterUpdate` is true
   - If so, clear the flag, find the most recent company, and auto-open it
   - This gives users a seamless experience — they're right back where they left off

### Step 9: Version Compatibility Check (Optional, Lower Priority)

Port the `VersionCompatibilityChecker` from WinForms to prevent opening files saved by a newer version of the app. This is a data safety feature that prevents corruption if a user downgrades.

---

## 4. Files to Create/Modify

### New Files

| File | Project | Purpose |
|---|---|---|
| `ArgoBooks.Core/Services/IUpdateService.cs` | Core | Interface for update operations |
| `ArgoBooks.Core/Models/Common/UpdateInfo.cs` | Core | Update metadata record |
| `ArgoBooks.Core/Models/Common/UpdateState.cs` | Core | State enum |
| `ArgoBooks.Desktop/Services/NetSparkleUpdateService.cs` | Desktop | NetSparkle wrapper implementation |

### Modified Files

| File | Change |
|---|---|
| `ArgoBooks/ViewModels/CheckForUpdateModalViewModel.cs` | Replace stub with real IUpdateService calls; add download/install states |
| `ArgoBooks/Modals/CheckForUpdateModal.axaml` | Add downloading progress bar and restart-to-install states |
| `ArgoBooks/App.axaml.cs` | Create update service, wire to VM, trigger background check, post-update recovery |
| `ArgoBooks.Core/Services/GlobalSettingsService.cs` | Add `AutoOpenRecentAfterUpdate` flag |

### Server-Side (Manual)

| Item | Description |
|---|---|
| `avalonia-update.xml` | New appcast file hosted on argorobots.com |
| Platform installers | Build pipeline to produce `.exe`, `.zip`/`.dmg`, `.AppImage` per release |

---

## 5. Key Design Decisions

### Why use NetSparkle (not roll our own)?

NetSparkle is already referenced, handles appcast parsing, version comparison, and DSA signature verification. The WinForms app proves it works. We just wrap it behind our own interface for testability and to use our own UI.

### Why a separate appcast from WinForms?

The Avalonia app has a different version track (v2.x vs v1.x), different installer formats, and needs per-OS entries. A separate appcast avoids cross-contamination.

### Why download with HttpClient instead of NetSparkle's downloader?

Same approach as WinForms. It gives full control over download progress reporting, timeout configuration, and error handling without fighting NetSparkle's internal download pipeline.

### Why not use NetSparkle's built-in Avalonia UI?

The app already has a custom `CheckForUpdateModal` that matches the design system (themed borders, dynamic resources, localization). NetSparkle's generic UI would look out of place.

### Why AppImage for Linux?

AppImages are self-contained, don't require root to install, and support self-replacement. `.deb` and `.rpm` require `sudo` and package manager invocation, which is a poor UX for auto-update.

---

## 6. Implementation Order

1. **Core interfaces and models** (Step 1) — no dependencies, quick
2. **NetSparkleUpdateService** (Steps 2-3) — the bulk of the work
3. **ViewModel rewrite** (Step 5) — wire real service into existing VM
4. **AXAML updates** (Step 6) — add download/install UI states
5. **Startup wiring** (Step 7) — connect everything in App.axaml.cs
6. **Post-update recovery** (Step 8) — seamless restart experience
7. **Appcast and build pipeline** (Step 4) — server-side, done in parallel
8. **Version compatibility** (Step 9) — optional, can be done later

---

## 7. Testing Strategy

- **Unit tests**: Mock `IUpdateService` to test ViewModel state transitions
- **Integration test**: Point NetSparkleUpdateService at a local test appcast XML file
- **Manual testing per platform**: Verify download and install on Windows, macOS, Linux
- **Rollback test**: Verify the app still works if a download fails mid-way
- **Post-update test**: Verify AutoOpenRecentAfterUpdate works after a simulated update
