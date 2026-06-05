# Publishing Argo Books

## Before You Build

1. Update the version number in `Directory.Build.props`
2. Run all tests: `dotnet test ArgoBooks.Tests`

## Windows

### Build

In Rider, set the configuration to **Release** and the target to **Desktop (Windows)**, then build.

Alternatively from the command line:

```bash
dotnet publish ArgoBooks.Desktop -c Release -f net10.0-windows10.0.17763.0 -r win-x64 --self-contained -o publish/win-x64
```

### Package

The Windows `.exe` installer is built using [Advanced Installer Professional Edition](https://www.advancedinstaller.com/). Point it at the `ArgoBooks.Desktop\bin\Release\net10.0-windows10.0.17763.0` output folder.

## Linux

The Linux distribution is packaged as an [AppImage](https://appimage.org/). The build runs in the cloud via GitHub Actions, so no Linux VM is needed.

### Build and package (GitHub Actions)

1. Make sure the version branch with your changes is pushed to GitHub.
2. Go to the repo's **Actions** tab on github.com and select **Build Linux AppImage** in the left sidebar.
3. Click **Run workflow**, choose the branch to build from, and click the green **Run workflow** button.
4. Wait for the run to finish (about 5 minutes), then open the run's **Summary** page (not the job log) and scroll to the **Artifacts** section at the bottom. The artifact is a `.zip`; extract it to get `ArgoBooks-X.X.X-linux-x64.AppImage`.
5. Upload the AppImage to the website (e.g. via FileZilla) as usual.

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

## Before Going Live

After building the installers/AppImage (steps above) but before uploading them to the website to make the release live:

1. Run the freshly built Argo Books on all operating systems and test a couple major features such as the AI receipt scanner to ensure things work.
