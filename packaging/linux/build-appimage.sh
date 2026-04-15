#!/bin/bash
set -euo pipefail

# Build and package Argo Books as a Linux AppImage.
#
# Prerequisites:
#   - .NET 10 SDK
#   - appimagetool (https://github.com/AppImage/appimagetool)
#   - A 256x256 PNG icon at packaging/linux/com.argobooks.ArgoBooks.png
#
# Usage:
#   cd <repo-root>
#   chmod +x packaging/linux/build-appimage.sh
#   ./packaging/linux/build-appimage.sh [version]
#
# If version is not provided, it reads from Directory.Build.props.

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
ARCH="x86_64"

# Determine version
if [ -n "${1:-}" ]; then
    VERSION="$1"
else
    VERSION=$(grep -oP '<Version>\K[^<]+' "$REPO_ROOT/Directory.Build.props" | head -1)
    if [ -z "$VERSION" ]; then
        echo "Error: Could not determine version from Directory.Build.props"
        exit 1
    fi
fi

echo "Building Argo Books v${VERSION} AppImage for ${ARCH}..."

# Publish the application
PUBLISH_DIR="$REPO_ROOT/publish/linux-x64"
echo "Publishing .NET application..."
dotnet publish "$REPO_ROOT/ArgoBooks.Desktop" \
    -c Release \
    -f net10.0 \
    -r linux-x64 \
    --self-contained \
    -o "$PUBLISH_DIR"

# Create AppDir structure
APPDIR="$REPO_ROOT/AppDir"
rm -rf "$APPDIR"
mkdir -p "$APPDIR/usr/bin"
mkdir -p "$APPDIR/usr/share/applications"
mkdir -p "$APPDIR/usr/share/mime/packages"
mkdir -p "$APPDIR/usr/share/icons/hicolor/256x256/apps"

# Copy published files
echo "Copying published files to AppDir..."
cp -r "$PUBLISH_DIR"/* "$APPDIR/usr/bin/"

# Copy desktop entry and MIME type
cp "$SCRIPT_DIR/com.argobooks.ArgoBooks.desktop" "$APPDIR/"
cp "$SCRIPT_DIR/com.argobooks.ArgoBooks.desktop" "$APPDIR/usr/share/applications/"
cp "$SCRIPT_DIR/com.argobooks.ArgoBooks.xml" "$APPDIR/usr/share/mime/packages/"

# Copy icon (must be a PNG file)
ICON_FILE="$SCRIPT_DIR/com.argobooks.ArgoBooks.png"
if [ ! -f "$ICON_FILE" ]; then
    echo "Error: Icon file not found at $ICON_FILE"
    echo "Please provide a 256x256 PNG icon at: packaging/linux/com.argobooks.ArgoBooks.png"
    exit 1
fi
cp "$ICON_FILE" "$APPDIR/"
cp "$ICON_FILE" "$APPDIR/usr/share/icons/hicolor/256x256/apps/"

# Create AppRun symlink
ln -sf usr/bin/"Argo Books" "$APPDIR/AppRun"

# Make the main executable actually executable
chmod +x "$APPDIR/usr/bin/Argo Books"

# Build the AppImage
OUTPUT_FILE="$REPO_ROOT/ArgoBooks-${VERSION}-linux-x64.AppImage"
echo "Building AppImage..."
ARCH="$ARCH" appimagetool "$APPDIR" "$OUTPUT_FILE"

# Clean up
rm -rf "$APPDIR" "$PUBLISH_DIR"

echo ""
echo "AppImage created: $OUTPUT_FILE"
echo "Size: $(du -h "$OUTPUT_FILE" | cut -f1)"
