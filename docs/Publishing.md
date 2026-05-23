# Publishing Argo Books

## Before You Build

1. Update the version number in `Directory.Build.props`
2. Run all tests: `dotnet test ArgoBooks.Tests`

## Before You Publish

1. Run Argo Books on all operating systems and test a couple major features such as the AI receipt scanner to ensure things work.

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

### Step 1: Build (on Windows)

Run from the command line on your Windows machine. .NET cross-compiles, so this produces Linux binaries without needing a Linux machine:

```bash
dotnet publish ArgoBooks.Desktop -c Release -f net10.0 -r linux-x64 --self-contained -o publish/linux-x64
```

### Step 2: Copy to Linux VM

Copy these to your Linux VM (e.g. via shared folder, Google Drive, or USB):
- The `publish/linux-x64/` folder (the build output)
- The `packaging/linux/` folder (desktop entry, MIME type, build script)

### Step 3: Package as AppImage (on Linux VM)

The Linux distribution is packaged as an [AppImage](https://appimage.org/) using [appimagetool](https://github.com/AppImage/appimagetool).

#### One-time setup

Install FUSE (required to run AppImage tools) and appimagetool:

```bash
sudo apt install libfuse2
wget https://github.com/AppImage/appimagetool/releases/download/continuous/appimagetool-x86_64.AppImage
chmod +x appimagetool-x86_64.AppImage
sudo mv appimagetool-x86_64.AppImage /usr/local/bin/appimagetool
```

#### Build the AppImage

`cd` into the folder that contains both `publish/` and `packaging/`, then run the script with the version number:

```bash
cd ~/Downloads
chmod +x packaging/linux/build-appimage.sh
sed -i 's/\r$//' packaging/linux/*.sh packaging/linux/*.desktop packaging/linux/*.xml
/packaging/linux/build-appimage.sh 2.0.7
```

Replace `2.0.5` with the version from `Directory.Build.props`. This produces `ArgoBooks-2.0.5-linux-x64.AppImage`.

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
