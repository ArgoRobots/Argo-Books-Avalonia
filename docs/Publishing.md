# Publishing Argo Books

## Before You Build

1. Update the version number in `Directory.Build.props`
2. Run all tests: `dotnet test ArgoBooks.Tests`

## Windows

### Build

**Use `dotnet publish`, not a Rider build.** The project sets `PublishReadyToRun`, which precompiles IL to native code and cuts cold start from about 6.5 seconds to about 3.7 seconds. That property applies only to `publish`; a plain Rider Release build ignores it and ships the slower output.

Run it from the solution root (`Argo-Books-Avalonia`), since `-o` is relative to the current directory, not to the project:

```bash
dotnet publish ArgoBooks.Desktop -c Release -f net10.0-windows10.0.17763.0 -r win-x64 --self-contained -o publish/win-x64
```

Output lands at `Argo-Books-Avalonia\publish\win-x64` (already gitignored).

A Rider Release build is still fine for local testing, it just won't have the startup improvement.

### Package

The Windows `.exe` installer is built using [Advanced Installer Professional Edition](https://www.advancedinstaller.com/). Point its synchronized folder at `Argo-Books-Avalonia\publish\win-x64`.

Note this is the **publish** output, not `bin\Release\...`. Pointing it back at `bin\Release` still produces a working installer, so the mistake is silent: the only symptom is users waiting an extra 2.5 seconds on every launch.

The publish output is roughly 100MB larger than a plain build (about 508MB versus 400MB uncompressed) because of the precompiled native code. It also contains fewer files, since publish drops build artefacts that aren't needed at runtime.

## Linux

The Linux distribution is packaged as an [AppImage](https://appimage.org/). The build runs in the cloud via GitHub Actions, so no Linux VM is needed.

### Build and package (GitHub Actions)

1. Make sure the version branch with your changes is pushed to GitHub.
2. Go to the repo's **Actions** tab on github.com and select **Build Linux AppImage** in the left sidebar.
3. Click **Run workflow**, choose the branch to build from, and click the green **Run workflow** button.
4. Wait for the run to finish (about 5 minutes), then open the run's **Summary** page (not the job log) and scroll to the **Artifacts** section at the bottom. The artifact is a `.zip`; extract it to get `ArgoBooks-X.X.X-linux-x64.AppImage`.
5. To test it on a Linux VM, first make it executable:

   ```bash
   chmod +x ArgoBooks-X.X.X-linux-x64.AppImage
   ```

   Without this, double-clicking does nothing (silently). This only affects local testing; end users always have to mark downloaded AppImages executable regardless of how we build them, since browser downloads never preserve the executable bit.
6. Upload the AppImage to the website (e.g. via FileZilla) as usual.

The workflow reads the version number from `Directory.Build.props` automatically.

### Manual build (reference only)

The commands below are what the workflow runs. Use them only if you need to build without GitHub Actions.

#### Step 1: Build (on Windows)

.NET cross-compiles, so this produces Linux binaries without needing a Linux machine:

```bash
dotnet publish ArgoBooks.Desktop -c Release -f net10.0 -r linux-x64 --self-contained -o publish/linux-x64
```

#### Step 2: Copy to Linux VM

Copy these to your Linux VM (e.g. via shared folder, Google Drive, or USB):
- The `publish/linux-x64/` folder (the build output)
- The `packaging/linux/` folder (desktop entry, MIME type, build script)

#### Step 3: Package as AppImage (on Linux VM)

One-time setup: install FUSE (required to run AppImage tools) and [appimagetool](https://github.com/AppImage/appimagetool):

```bash
sudo apt install libfuse2
wget https://github.com/AppImage/appimagetool/releases/download/continuous/appimagetool-x86_64.AppImage
chmod +x appimagetool-x86_64.AppImage
sudo mv appimagetool-x86_64.AppImage /usr/local/bin/appimagetool
```

Then `cd` into the folder that contains both `publish/` and `packaging/`, and run the script with the version number from `Directory.Build.props`:

```bash
cd ~/Downloads
chmod +x packaging/linux/build-appimage.sh
sed -i 's/\r$//' packaging/linux/*.sh packaging/linux/*.desktop packaging/linux/*.xml
./packaging/linux/build-appimage.sh 2.0.8
```

This produces `ArgoBooks-2.0.8-linux-x64.AppImage`.

### Linux runtime dependencies

The AppImage is self-contained (.NET runtime is bundled), but it depends on system libraries that are pre-installed on all standard desktop Linux distros (Ubuntu, Fedora, Linux Mint, etc.). End users should not need to install anything.

If you're testing on a minimal VM that's missing packages:

```bash
sudo apt install libgtk-3-0 libwebkit2gtk-4.0-37 libsecret-tools policykit-1
```

## macOS

### Build

```bash
dotnet publish ArgoBooks.Desktop -c Release -f net10.0 -r osx-arm64 --self-contained -o publish/osx-arm64
```

For Intel Macs, use `-r osx-x64` instead.

### Package

The macOS `.dmg` installer is created using [create-dmg](https://github.com/create-dmg/create-dmg).

## Sign the Release Files

The app verifies an Ed25519 signature on every update it downloads, and refuses to install files that are unsigned or don't match. Every file referenced by the appcast must therefore be signed with our private key.

### One-time setup (already done)

- The signing key pair lives at `C:\Users\evand\AppData\Local\netsparkle`. **Back this folder up** (e.g. in a password manager). If the private key is lost, shipped versions of the app can't verify future updates; if it leaks, someone who also compromised the website could forge updates. It must never be committed to a repo.
- The matching public key is embedded in the app at `NetSparkleUpdateService.UpdatePublicKey`.
- The signing tool (already installed): `dotnet tool install --global NetSparkleUpdater.Tools.AppCastGenerator`

### Each release

1. After producing the **final** `.exe` and `.AppImage`, generate a signature for each file:

   ```powershell
   netsparkle-generate-appcast --generate-signature "C:\path\to\Argo Books Installer V.2.0.8.exe"
   netsparkle-generate-appcast --generate-signature "C:\path\to\ArgoBooks-2.0.8-linux-x64.AppImage"
   ```

   Each command prints a base64 signature string.

2. In the website repo, update `avalonia-update.xml`:
   - The version numbers
   - On each `<enclosure>`, add/update `sparkle:edSignature="<that file's signature>"`. The signature is the long base64 string that step 1's `--generate-signature` command printed for that file: the `.exe`'s signature goes on the `sparkle:os="windows"` enclosure, the `.AppImage`'s on the `sparkle:os="linux"` one.

   (The `length` attribute can stay `0`; the download server reports the real size automatically.)

3. Push the website repo so the appcast deploys, and upload the matching `.exe`/`.AppImage` via FileZilla.

**Important:** the signature covers the file's exact bytes. If a file is rebuilt for any reason, re-sign it and update the appcast. Signing the wrong build is equivalent to not signing at all: users' updates will be rejected.

To double-check a file before publishing, run `--verify` with that same file's signature string (the long base64 text printed by `--generate-signature`, not the public key):

```powershell
netsparkle-generate-appcast --verify "C:\path\to\ArgoBooks-2.0.8-linux-x64.AppImage" --signature "t4lRf5lP...8O9zCQ=="
```

## Before Going Live

After building the installers/AppImage (steps above) but before uploading them to the website to make the release live:

1. Run the freshly built Argo Books on all operating systems and test a couple major features such as the AI receipt scanner to ensure things work.
