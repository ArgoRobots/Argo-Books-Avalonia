#!/bin/bash
set -euo pipefail

# Package Argo Books as a signed, notarized macOS .app bundle in a .zip.
#
# Usage:
#   ./packaging/macos/build-app.sh [version]
#
# Signing and notarization are driven by two environment variables. With neither
# set the script still produces a working bundle, but only for local testing:
# anything downloaded through a browser is quarantined and macOS will refuse to
# open an unsigned app.
#
#   APPLE_SIGN_IDENTITY   "Developer ID Application: Your Name (TEAMID)"
#                         List available ones with: security find-identity -v -p codesigning
#   APPLE_NOTARY_PROFILE  keychain profile name, created once with:
#                           xcrun notarytool store-credentials "argo-notary" \
#                             --apple-id you@example.com --team-id TEAMID \
#                             --password <app-specific-password>
#
# Prerequisites: .NET 10 SDK (arm64) and Xcode Command Line Tools.

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd "$SCRIPT_DIR/../.." && pwd)"
RID="osx-arm64"
PUBLISH_DIR="$ROOT_DIR/publish/$RID"
# Finished artefacts sit in publish/, alongside the per-RID folders holding the
# raw compile output this script consumes as input.
OUT_DIR="$ROOT_DIR/publish"
APP_NAME="Argo Books"

# Determine version. BSD grep has no -PCRE, so this uses sed rather than the
# grep -oP the Linux script gets away with on a GNU userland.
if [ -n "${1:-}" ]; then
    VERSION="$1"
elif [ -f "$ROOT_DIR/Directory.Build.props" ]; then
    VERSION=$(sed -n 's/.*<Version>\([^<]*\)<\/Version>.*/\1/p' "$ROOT_DIR/Directory.Build.props" | head -1)
    if [ -z "$VERSION" ]; then
        echo "Error: Could not read <Version> from Directory.Build.props. Pass it instead: ./build-app.sh 2.0.15"
        exit 1
    fi
else
    echo "Error: No version provided and Directory.Build.props not found."
    exit 1
fi

echo "Packaging $APP_NAME v${VERSION} for ${RID}..."

# --- Build -------------------------------------------------------------------

if [ -f "$ROOT_DIR/ArgoBooks.Desktop/ArgoBooks.Desktop.csproj" ] && command -v dotnet &> /dev/null; then
    echo "Building .NET application..."
    # net10.0, not the net10.0-windows TFM. PublishReadyToRun applies here too,
    # which is why this is a publish and not a build (see docs/Publishing.md).
    dotnet publish "$ROOT_DIR/ArgoBooks.Desktop" \
        -c Release \
        -f net10.0 \
        -r "$RID" \
        --self-contained \
        -o "$PUBLISH_DIR"
elif [ ! -d "$PUBLISH_DIR" ]; then
    echo "Error: No published output found at $PUBLISH_DIR"
    exit 1
else
    echo "Using pre-built output from $PUBLISH_DIR"
fi

if [ ! -f "$PUBLISH_DIR/$APP_NAME" ]; then
    echo "Error: Expected executable '$PUBLISH_DIR/$APP_NAME' not found."
    echo "The bundle's CFBundleExecutable must match AssemblyName in ArgoBooks.Desktop.csproj."
    exit 1
fi

# --- Assemble the bundle -----------------------------------------------------

mkdir -p "$OUT_DIR"
APP_BUNDLE="$OUT_DIR/$APP_NAME.app"
rm -rf "$APP_BUNDLE"
mkdir -p "$APP_BUNDLE/Contents/MacOS"
mkdir -p "$APP_BUNDLE/Contents/Resources"

echo "Copying published files into the bundle..."
cp -R "$PUBLISH_DIR"/* "$APP_BUNDLE/Contents/MacOS/"
chmod +x "$APP_BUNDLE/Contents/MacOS/$APP_NAME"

sed "s/__VERSION__/$VERSION/g" "$SCRIPT_DIR/Info.plist" > "$APP_BUNDLE/Contents/Info.plist"

# --- Icon --------------------------------------------------------------------

SOURCE_ICON="$ROOT_DIR/ArgoBooks/Assets/argo-logo.png"
if [ ! -f "$SOURCE_ICON" ]; then
    echo "Error: Source icon not found at $SOURCE_ICON"
    exit 1
fi

ICON_WIDTH=$(sips -g pixelWidth "$SOURCE_ICON" | sed -n 's/.*pixelWidth: *//p')
if [ "${ICON_WIDTH:-0}" -lt 1024 ]; then
    echo "Warning: source icon is ${ICON_WIDTH}px wide. macOS renders app icons up to 1024px,"
    echo "         so anything smaller will look soft in the Dock and Finder."
fi

echo "Generating argo-logo.icns..."
ICONSET="$OUT_DIR/argo-logo.iconset"
rm -rf "$ICONSET"
mkdir -p "$ICONSET"
for SIZE in 16 32 128 256 512; do
    sips -z $SIZE $SIZE           "$SOURCE_ICON" --out "$ICONSET/icon_${SIZE}x${SIZE}.png"      > /dev/null
    sips -z $((SIZE*2)) $((SIZE*2)) "$SOURCE_ICON" --out "$ICONSET/icon_${SIZE}x${SIZE}@2x.png" > /dev/null
done
iconutil -c icns "$ICONSET" -o "$APP_BUNDLE/Contents/Resources/argo-logo.icns"
rm -rf "$ICONSET"

# --- Sign --------------------------------------------------------------------

OUTPUT_FILE="$OUT_DIR/ArgoBooks-${VERSION}-${RID}.zip"
rm -f "$OUTPUT_FILE"

if [ -z "${APPLE_SIGN_IDENTITY:-}" ]; then
    echo ""
    echo "APPLE_SIGN_IDENTITY is not set, so the bundle is UNSIGNED."
    echo "It will run locally, but a downloaded copy is rejected as damaged on any other Mac."
    ditto -c -k --keepParent "$APP_BUNDLE" "$OUTPUT_FILE"
    echo "Created (unsigned): $OUTPUT_FILE"
    exit 0
fi

# Copying from a non-macOS filesystem can leave extended attributes behind, and
# codesign fails on a bundle that has them.
xattr -cr "$APP_BUNDLE"

# Sign inside out. `codesign --deep` is documented as unsuitable for anything but
# the simplest bundles and silently misses nested .NET native libraries, so each
# one is signed explicitly before the bundle itself.
echo "Signing native libraries..."
find "$APP_BUNDLE/Contents/MacOS" \( -name "*.dylib" -o -name "*.so" \) -print0 \
    | xargs -0 -I {} codesign --force --timestamp --options runtime \
        --sign "$APPLE_SIGN_IDENTITY" {}

echo "Signing the bundle..."
codesign --force --timestamp --options runtime \
    --entitlements "$SCRIPT_DIR/entitlements.plist" \
    --sign "$APPLE_SIGN_IDENTITY" \
    "$APP_BUNDLE"

codesign --verify --deep --strict --verbose=2 "$APP_BUNDLE"

# --- Notarize ----------------------------------------------------------------

if [ -z "${APPLE_NOTARY_PROFILE:-}" ]; then
    echo ""
    echo "APPLE_NOTARY_PROFILE is not set, so the bundle is signed but NOT notarized."
    echo "Gatekeeper still blocks it on a first launch from a download."
    ditto -c -k --keepParent "$APP_BUNDLE" "$OUTPUT_FILE"
    echo "Created (signed, un-notarized): $OUTPUT_FILE"
    exit 0
fi

NOTARIZE_ZIP="$OUT_DIR/notarize-upload.zip"
rm -f "$NOTARIZE_ZIP"
ditto -c -k --keepParent "$APP_BUNDLE" "$NOTARIZE_ZIP"

echo "Submitting to Apple for notarization (usually a few minutes)..."
xcrun notarytool submit "$NOTARIZE_ZIP" --keychain-profile "$APPLE_NOTARY_PROFILE" --wait
rm -f "$NOTARIZE_ZIP"

# Staple the ticket into the bundle so a first launch works without internet.
echo "Stapling the notarization ticket..."
xcrun stapler staple "$APP_BUNDLE"
xcrun stapler validate "$APP_BUNDLE"

# Re-zip AFTER stapling. Stapling rewrites the bundle, so a zip made before this
# point has different bytes, and the NetSparkle signature generated from it would
# fail verification on every user's machine.
ditto -c -k --keepParent "$APP_BUNDLE" "$OUTPUT_FILE"

echo ""
echo "Created: $OUTPUT_FILE"
echo "Size: $(du -h "$OUTPUT_FILE" | cut -f1)"
echo ""
echo "Next: generate the NetSparkle signature for THIS file and put it on the"
echo "sparkle:os=\"macos\" enclosure in the website repo's avalonia-update.xml."
