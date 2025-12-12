# Android Development Setup Guide

This guide explains how to set up your Windows development environment to run the Avalonia Android app on an emulator.

## Prerequisites

- Windows 10/11

## Step 1: Install .NET 10 SDK

Download and install the .NET 10 SDK from https://dotnet.microsoft.com/download/dotnet/10.0

Or install via winget:
```powershell
winget install Microsoft.DotNet.SDK.10
```

Verify the installation:
```powershell
dotnet --version
```

## Step 2: Install JDK 21

Android SDK requires JDK 21. If you have a different version, the build will fail.

```powershell
winget install Microsoft.OpenJDK.21
```

## Step 3: Install Android Studio

1. Download Android Studio from https://developer.android.com/studio
2. Run the installer and follow the prompts
3. Let it download the default SDK components

## Step 4: Create an Android Virtual Device (Emulator)

1. Open **Android Studio**
2. On the welcome screen, click **More Actions** → **Virtual Device Manager**
3. Click **Create Device**
4. Select a phone model (e.g., **Pixel 6**) → Click **Next**
5. Download a system image:
   - Select **API 34** (or the latest available)
   - Click the **download icon** next to it
   - Wait for the download to complete
6. Select the downloaded image → Click **Next**
7. Give it a name (optional) → Click **Finish**

## Step 5: Start the Emulator

1. In the **Device Manager**, find your virtual device
2. Click the **Play ▶** button to start it
3. Wait for the phone to boot to the home screen (this may take a minute on first launch)

## Step 6: Run the App

You can run the app using **either** Rider **or** the command line. Choose whichever you prefer.

### Option A: Using Rider (Recommended)

This is the easiest method. No environment variables needed - Rider auto-detects the Android SDK.

1. Make sure the emulator is running
2. Open the solution in Rider
3. Select **AvaloniaUILoudnessMeter.Android** as the startup project (dropdown at top)
4. The emulator should appear in the device dropdown next to it
5. Click the **Run ▶** button

The app will build, install on the emulator, and launch automatically.

### Option B: Using Command Line

If you prefer the terminal or need this for CI/CD, you'll need to set the `ANDROID_HOME` environment variable so `dotnet` knows where the SDK is.

**Set the environment variable (per session):**
```powershell
$env:ANDROID_HOME = "$env:LOCALAPPDATA\Android\Sdk"
```

**Or make it permanent:**
1. Press `Win + R`, type `sysdm.cpl`, press Enter
2. Go to **Advanced** → **Environment Variables**
3. Under User variables, click **New**
4. Variable name: `ANDROID_HOME`
5. Variable value: `%LOCALAPPDATA%\Android\Sdk`
6. Click OK

**Build and run:**
```powershell
dotnet build -t:Install -t:Run AvaloniaUILoudnessMeter.Android/AvaloniaUILoudnessMeter.Android.csproj
```

This will:
1. Build the Android APK
2. Install it on the emulator
3. Launch the app automatically

## How It Works

When you run the app (from Rider or command line), it:
1. Builds an APK (Android Package)
2. Installs it on the emulator/device - just like installing from the Play Store
3. Launches the app

The app **stays installed** on the emulator even after you close Rider or stop debugging. You can:
- Find it in the app drawer (swipe up on the home screen)
- Launch it manually anytime
- Uninstall it like any normal app (long press → uninstall)

Each time you run from Rider, it reinstalls the app (updating it with your latest code changes).

## Troubleshooting

### "Cannot find aapt2.exe"

The Android SDK Build-Tools are missing. Install them:

1. Open Android Studio
2. Go to **Tools** → **SDK Manager**
3. Select **SDK Tools** tab
4. Check **Android SDK Build-Tools**
5. Click **Apply**

### "Building with JDK version X is not supported"

Install JDK 21:
```powershell
winget install Microsoft.OpenJDK.21
```

## Using a Physical Android Device

Instead of the emulator, you can use a real Android phone:

1. On your phone, go to **Settings** → **About Phone**
2. Tap **Build Number** 7 times to enable Developer Options
3. Go back to **Settings** → **Developer Options**
4. Enable **USB Debugging**
5. Connect your phone via USB
6. Accept the debugging prompt on your phone
7. Run from Rider or command line - it will deploy to your phone

## Notes

- First build may take a while as it downloads dependencies
- The emulator is slower than a physical device
- You can close Android Studio after starting the emulator - it will keep running
- Rider is easier than command line for day-to-day development
