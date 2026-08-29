# Advanced Installer project setup

Recovery guide for the Windows installer project. The `.aip` file lives at
`C:\Users\<you>\Documents\Advanced Installer\Projects\Argo Books\Argo Books.aip`.
The `.aip` is backed up, so this doc is mostly here for reference. A full rebuild from scratch (about an hour) is only needed if both the working copy and the backups are lost.

> FYI: "AI" in this doc always means **Advanced Installer**, not artificial intelligence.

## Step 1: Install required tooling

Skip any you already have.

### Advanced Installer
Download from https://www.advancedinstaller.com/download.html, install, and activate using your Caphyon license key (**Help → Activate License**).

Pricing on https://www.advancedinstaller.com/purchase.html is **$399 USD/user/year** for the Professional tier (which is what this project needs). I originally bought it as a perpetual one-time license, but that option is no longer shown on the public pricing page.

### Windows SDK (provides `signtool.exe`)
Download from https://developer.microsoft.com/windows/downloads/windows-sdk/. During setup you only need the **Windows SDK Signing Tools for Desktop Apps** component. Advanced Installer auto-resolves `<AI_SIGNTOOL_FOLDER>signtool.exe` to whatever SDK version is installed.

### .NET 10 SDK (for `dotnet publish`)
Download from https://dotnet.microsoft.com/download. Verify in a new terminal with `dotnet --version`.

### Microsoft Trusted Signing CLI tools
Follow https://learn.microsoft.com/azure/trusted-signing/how-to-signing-integrations to download the client tools package, then extract to `C:\Users\<you>\AppData\Local\Microsoft\MicrosoftTrustedSigningClientTools\`. Confirm `Azure.CodeSigning.Dlib.dll` lands in that folder.

### Azure Trusted Signing account
Azure Trusted Signing needs to be set up. See `Argo-Books-Avalonia/docs/setup/AzureSetup.md` for the full walkthrough.

### Trusted Signing metadata JSON
It should already exist at `Argo-Books-Avalonia/packaging/windows/trusted-signing-metadata.json`. Double-check it's there. If missing, see Step 13 in `Argo-Books-Avalonia/docs/setup/AzureSetup.md` for the contents and how to recreate it.

### Avalonia release build
Run `dotnet publish`, do not use a Rider build:

```bash
dotnet publish ArgoBooks.Desktop -c Release -f net10.0-windows10.0.17763.0 -r win-x64 --self-contained -o publish/win-x64
```

The project sets `PublishReadyToRun`, which precompiles the app to native code and cuts cold start from roughly 6.5 seconds to 3.7 seconds. That setting only takes effect during `publish`. A Rider Release build silently ignores it and produces a working but noticeably slower installer. See `Argo-Books-Avalonia/docs/Publishing.md` for the full publishing flow.

Run the command from the solution root (`Argo-Books-Avalonia`), because `-o` is relative to the current directory rather than to the project folder.

Publish output ends up at `Argo-Books-Avalonia/publish/win-x64/`, alongside `ArgoBooks.Desktop`, not inside it. The folder is gitignored. Advanced Installer's synchronized folder (set up in Step 4) points at this path.

### Icon and logo files
Should already exist at `C:\Users\<you>\Desktop\Argo logos\Third\`. Double-check these three are there:

- `Argo Books icon.ico`
- `Argo Books icon transparent.png`
- `Argo Books icon white background.png`

## Step 2: Create the project

1. Open Advanced Installer. Make sure **MSI Installer** is selected in the left sidebar.
2. Click **.NET Application**.
3. On the right, set **Project Language** to **English**.
4. Click **Create New Project**. A "**New .NET Application project**" wizard opens.

The wizard has nine screens. Click **Next** between each.

**Screen 1 - Enter details about your product:**
- Product name: `Argo Books`
- Organization (manufacturer): `Argo Books`

**Screen 2 - Select a distribution type:**
- Choose **EXE setup file** (a single `.exe` with everything bundled inside). This is the format argorobots.com serves to users.

**Screen 3 - Add files to your project:**
- **Browse** to the Avalonia publish output folder: `C:\Users\<you>\Desktop\Argo-Books-Avalonia\publish\win-x64`.
- This must be the **publish** folder, not `bin\Release\...`. Both produce a working installer, so pointing at the wrong one fails silently: the only symptom is every user waiting an extra 2.5 seconds at launch.
- Check **Synchronized folder: recheck the folder and update the package every time the project is loaded or built**. This makes AI re-sync new builds automatically so you don't have to re-add files after every release.

**Screen 4 - Create shortcuts for your applications:**
- Keep **Argo Books.exe** checked. Leave **createdump.exe** unchecked.
- Keep both **Create shortcut on Desktop** and **Create shortcut in Programs group from Start menu** checked too.

**Screen 5 - Configure .NET assemblies in your package:**
- Leave the default **Install as a regular file (no registration)** selected.

**Screen 6 - Configure Launch Conditions:**
- Keep **Add launch conditions** checked.
- **Minimum .NET version**: `.NET Framework 4.0` (this is for the installer UI itself; Argo Books's own .NET 10 runtime is bundled separately by `dotnet publish`).
- **Supported Operating Systems**: leave all **64-bit Windows versions** checked (Win 7 x64 through Win 11 x64).

**Screen 7 - Configure Prerequisites:**
- Keep **Add .NET Framework prerequisite** checked.
- **.NET Framework**: change the dropdown to **.NET Framework 4.0** (matches the minimum version you set on Screen 6).
- **Location**: leave the default **Download single file from URL**. The installer won't bundle .NET Framework into the `.exe` (keeps the download smaller; modern Win 10/11 already has .NET Framework so the download rarely actually runs).
- Leave **Silent Install** unchecked.

**Screen 8 - Application execution:**
- Check **Launch application after install** so users see a pre-checked "Launch Argo Books now?" option on the final install screen.
- Select `Argo Books.exe`.
- Leave the default radio **Optional launch when pressing Finish button** selected.

**Screen 9 - Configure installation UI:**
- **Main installation dialog**: leave the default **Browse installation folder dialog** selected.
- **Dialog Theme**: pick **App Installer**.
- Click **Finish**.

When prompted to save, save the project as `Argo Books.aip`.

## Step 3: Product Details

In the **Product Details** page:

| Field | Value |
|---|---|
| Name | `Argo Books` |
| Product Version | current release (e.g. `2.0.7`) |
| Publisher | `Argo Books` |
| Support link | `https://argorobots.com/documentation/` |
| Contact | `https://argorobots.com/contact-us/` |
| Control Panel Icon | `C:\Users\<you>\Desktop\Argo logos\Third\Argo Books icon.ico` (this is the icon Windows shows in Apps & features / Add or Remove Programs) |

| **Upgrade Code** | `{56B4BFD1-ED8C-4FBE-9562-14EB8B82623C}`. This GUID is how Windows recognizes Argo Books across versions. Use this exact value so future installs can upgrade existing copies. |

## Step 4: Verify Files and Folders

The wizard in Step 2 already added the synchronized folder. Just sanity-check it landed in the right place:

1. Go to **Resources → Files and Folders** in the left sidebar.
2. Expand the tree on the left. Under **Application Folder** you should see your build output synced in, with `Argo Books.exe`, `ArgoBooks.dll`, the Avalonia DLLs, etc. listed on the right.

If it's missing, or right-click **Application Folder** → **Add Folder** and point at the Avalonia build path.

## Step 5: Shortcut icons

The shortcuts need their icons pointed at the Argo Books `.ico`:

1. In **Resources → Files and Folders**, look at the tree on the left.
2. Click **Application Shortcut Folder**. In the file list on the right, right-click the `Argo Books` shortcut → **Properties** → set the **Icon** to `Argo Books icon.ico`.
3. Click **Desktop** in the tree. Do the same for the shortcut here.

## Step 6: AppInstaller theme logos

Set the two logos that the AppInstaller theme shows on the install dialog:

1. Go to **User Interface → Themes** in the left sidebar.
2. On the right side of the page you'll see two fields: **App Logo Icon** and **App Logo Icon Dark**.
3. Click the **...** button next to **App Logo Icon** and pick `C:\Users\<you>\Desktop\Argo logos\Third\Argo Books icon transparent.png`.
4. Click the **...** button next to **App Logo Icon Dark** and pick `C:\Users\<you>\Desktop\Argo logos\Third\Argo Books icon white background.png`.

## Step 7: Custom Action `WriteInstallToken`

On the website admin page there's a **funnel** that tracks each user through their journey: first website visit → download → install → using the app → paying for Premium. This custom action is what plugs in the **install** part.

How it works: when a user downloads the installer from argorobots.com, the website server renames the `.exe` to embed a unique code in the filename (e.g. `Argo Books Installer V.2.0.7_6548910d.exe`). This custom action reads that code during install and saves it to disk. On first launch, Argo Books sends the code back to the server, so we know that specific download became an install.

If the user renames the installer before running it (for whatever reason), the code in the filename gets lost and we can't tie the install back to the original download. The install still works fine; the funnel just logs it as an unattributed installation.

1. Go to **Custom Behavior → Custom Actions** and click **New Custom Action**.
2. In the wizard, select: `Execute a VB, Java or PowerShell script` → `PowerShell` → `Script source embedded directly in the project (script inline)`.
3. Name: `WriteInstallToken`.
4. **Parameter values** field at top of script editor:
   ```
   "[AI_SETUPEXEPATH]"
   ```
5. **Script body**:
   ```powershell
   Param([string]$setupExePath)

   try {
       if ($setupExePath -match '_([0-9a-f]{8})\.(exe|msi)$') {
           $token = $matches[1]
           $appDataDir = Join-Path $env:LOCALAPPDATA 'ArgoBooks'
           New-Item -ItemType Directory -Force -Path $appDataDir | Out-Null
           Set-Content -Path (Join-Path $appDataDir 'install_token.txt') `
                       -Value $token -Encoding ASCII -Force
       }
   }
   catch {
       # Never fail the install over a missing token
   }
   ```
6. **Execution Time**: `When the system is being modified (deferred)`.
7. **Execution Options**:
   - Leave `Run under the LocalSystem account with full privileges` **unchecked**. Needs to run as the installing user so `$env:LOCALAPPDATA` resolves to *their* AppData, not the SYSTEM profile.
   - Leave `Wait for custom action to finish before proceeding` **checked**.
   - **Uncheck** `Fail installation if custom action returns an error`. Never break an install over a telemetry feature.
8. **Execution Stage Condition**: 
- Install: **checked**
- Uninstall: **unchecked**
- Maintenance: **unchecked**

## Step 8: Build settings

In the left sidebar under **Package Definition**:

1. Open **Install Parameters** and check **Run as administrator** (so Windows prompts for UAC elevation; required because Argo Books installs to `C:\Program Files`).
2. Open **Builds → DefaultBuild** and set:

| Setting | Value |
|---|---|
| Package type | Single EXE setup (resources inside) |
| EXE icon | `Argo Books icon.ico` |

## Step 9: Digital Signature

In **Digital Signature**:

1. **Enable signing: checked**
2. **Sign Tool**: `Custom`
3. **Path**: `<AI_SIGNTOOL_FOLDER>signtool.exe`
4. **Command line** (single line, no breaks):
   ```
   sign /fd SHA256 /tr "http://timestamp.acs.microsoft.com" /td SHA256 /dlib "C:\Users\<you>\AppData\Local\Microsoft\MicrosoftTrustedSigningClientTools\Azure.CodeSigning.Dlib.dll" /dmdf "C:\Users\<you>\Desktop\Argo-Books-Avalonia\packaging\windows\trusted-signing-metadata.json" /d "[|ProductName]"
   ```

This signs against the Azure-hosted Microsoft Trusted Signing certificate so SmartScreen reputation accrues against the publisher identity over time.

## Step 10: Verify a build

1. Save the project and click **Build**.
2. Output lands at `C:\Users\<you>\Documents\Advanced Installer\Projects\Argo Books\Setup Files\Argo Books Installer V.{version}.exe`.
3. Rename to add a fake token, e.g. `Argo Books Installer V.{version}_abcdef12.exe`.
4. Delete `%LOCALAPPDATA%\ArgoBooks\first_run_reported.marker` if present (the desktop app skips reporting if the marker exists).
5. Run the installer.
6. Check `%LOCALAPPDATA%\ArgoBooks\install_token.txt`. Should contain `abcdef12`.
7. Launch the app then wait a few seconds.
8. Re-check `%LOCALAPPDATA%\ArgoBooks\first_run_reported.marker`. Should contain `reason=token`. If it says `reason=no_token`, the custom action either didn't run or `AI_SETUPEXEPATH` came back empty.

## Rotating the token secret

Only rotate `REFERRAL_TOKEN_SECRET` in the website's `.env` if it leaks (e.g. accidentally committed to git). Do it during a low-traffic period: any download that's already out there stops being attributable for about 2 weeks after the rotation.

## Troubleshooting

**If the token file isn't created during install:** The custom action may not be running, or `AI_SETUPEXEPATH` may be substituting as empty. Temporarily extend the PowerShell script to write a debug log alongside the token file:

```powershell
Set-Content -Path (Join-Path $appDataDir 'install_token_debug.txt') `
            -Value "setupExePath=$setupExePath" -Encoding ASCII -Force
```

Reinstall, then inspect `%LOCALAPPDATA%\ArgoBooks\install_token_debug.txt`. Most failures come from the installer being renamed without a `_xxxxxxxx` suffix.

**If the token file exists but `FirstRunReporter` doesn't report it:** Check `%LOCALAPPDATA%\ArgoBooks\first_run_reported.marker`. If it exists, the app has already attempted reporting and won't retry. Delete the marker to force a re-report on next app launch.

## Notes

- For details on the desktop-app side of the token flow (`FirstRunReporter`, marker file, retry logic), see `ArgoBooks.Core/Services/FirstRunReporter.cs` in the Avalonia repo.
- For the server side of how `_xxxxxxxx` tokens are generated, see `referral_install_token()` in [`track_referral_event.php`](../../track_referral_event.php). It is the single shared recipe: [`get_avalonia_installer.php`](../../get_avalonia_installer.php) embeds the token in the served filename and `api/track-app-event.php` verifies it on first run.
