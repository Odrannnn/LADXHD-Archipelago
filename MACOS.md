# 🍎 Patching and building on MacOS

## Using the patcher

The patcher available on the [Releases](https://gitlab.com/bighead.0/ladxhd_updated/-/releases) page runs natively on macOS — no Wine required.

> [!Important]
> Because the patcher is not notarized by Apple, macOS will block it from running by default. To fix this, please use the `fix-permissions.command` script included in the ZIP file.
>
> If macOS refuses to run the script itself, you can easily override the warning by following these steps:
>
> * Open **System Settings** on your Mac.
> * Navigate to **Privacy & Security**.
> * Scroll down to the **Security** section and click **Open Anyway**.
>
> You can also run the script from the command-line to avoid the system's quarantine:
>
> ```bash
> $ sh fix-permissions.sh
> ```

When [running the patcher](https://gitlab.com/bighead.0/ladxhd_updated/#patching-to-the-latest-version) choose `MacOS` as the platform, and `OpenGL` as the target. The patcher should take care of signing the binaries and making them executable, as well as creating .app bundles ready to launch or move to `/Applications`:

```
📁 "Link's Awakening DX HD.app"            # game app
📁 "Link's Awakening DX HD Launcher.app".  # launcher app
⚙️ "Link's Awakening DX HD"                # game binary
⚙️ Launcher                                # launcher binary
```

> [!Note]
> The launcher .app already contains the game, if you choose to use the launcher you can ignore the other .app.

If the patcher fails to perform these steps and the resulting files aren't executable, you can fix them manually (if you also want to create the app bundles, check [Creating .app bundles manually](#creating-app-bundles-manually)):

```bash
# sign / make executable the game binary
$ codesign --force --sign - "Link's Awakening DX HD"
$ chmod +x "Link's Awakening DX HD"

# same for the launcher binary
$ codesign --force --sign - Launcher
$ chmod +x Launcher

# dynamic libraries should also be signed
$ codesign --force --sign - *.dylib
```

You are good to go!

## Building from source

### Requirements
* [dotnet9 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/9.0) (`$ dotnet --list-sdks` should return a `9.0.*` version).
* [Wine](https://www.winehq.org/) (required to compile MonoGame effects/shaders).

### Setup
* [Setup MonoGame effects compilation](https://docs.monogame.net/articles/getting_started/1_setting_up_your_os_for_development_macos.html?tabs=android#setup-wine-for-effect-compilation):
```bash
$ wget -qO- https://monogame.net/downloads/net9_mgfxc_wine_setup.sh | bash
```
* Update game assets following the [README instructions](https://gitlab.com/bighead.0/ladxhd_updated/-/wikis/Building-&-Contributing).

### Building
From the `ladxhd_game_source_code` directory, publish the DesktopGL host with the macOS profile for your hardware architecture:
```bash
# arm64 / Apple Silicon
$ dotnet publish ProjectZ.DesktopGL/ProjectZ.DesktopGL.csproj -c Release -r osx-arm64 -p:PublishProfile=FolderProfile_MacOS-Arm64

# x64 / Intel
$ dotnet publish ProjectZ.DesktopGL/ProjectZ.DesktopGL.csproj -c Release -r osx-x64 -p:PublishProfile=FolderProfile_MacOS-x86_64
```

The resulting ready-to-run binaries will be available in `ladxhd_game_source_code/_Publish/MacOS-Arm64` or `ladxhd_game_source_code/_Publish/MacOS-x86_64`.

## Generating an application bundle

The build project accepts a `CreateAppBundle` parameter that will yield a full-fledged application ready to be moved into the `/Applications` directory.

> [!Note]
> Since the application is not signed / notarized, it won't be usable outside the host where it's been built without removing the macOS quarantine flag.

```bash
# arm64 / Apple Silicon
$ dotnet publish ProjectZ.DesktopGL/ProjectZ.DesktopGL.csproj -c Release -r osx-arm64 -p:PublishProfile=FolderProfile_MacOS-Arm64 -p:CreateAppBundle=true

# x64 / Intel
$ dotnet publish ProjectZ.DesktopGL/ProjectZ.DesktopGL.csproj -c Release -r osx-x64 -p:PublishProfile=FolderProfile_MacOS-x86_64 -p:CreateAppBundle=true
```

The resulting application will be available in `ladxhd_game_source_code/_Publish/MacOS-Arm64` or `ladxhd_game_source_code/_Publish/MacOS-x86_64`.


## Creating .app bundles manually

The patcher will generate ready to use .app bundles for game and launcher when run on macOS. When that setup is not possible and the game is patched from a different platform, it is still possible to create macOS apps once the files are available on a macOS host. Here is the script used by the patcher slightly adapted to be executed manually. The script takes the path to the `Links Awakening DX HD` directory as a single parameter, or can be called without parameters when invoked from inside that directory (defaults to `.`).

```bash
#!/bin/sh

set -e

# Change to sync with patcher / game version.
VERSION="2.0.6"

TMP_DIR=$(mktemp -d 2>/dev/null || mktemp -d -t 'ladxhd-app-bundle')
BASE=$(realpath "${1:-.}")

GAME="Link's Awakening DX HD"
GAME_BUNDLE="$BASE/$GAME.app"
LAUNCHER_BUNDLE="$BASE/$GAME Launcher.app"

GAME_BUNDLE_TMP="$TMP_DIR/$GAME.app"
LAUNCHER_BUNDLE_TMP="$TMP_DIR/$GAME Launcher.app"

cleanup() {
    rm -rf "$TMP_DIR"
}

trap cleanup EXIT

# Set executable bit on the binaries.
chmod +x "$BASE/$GAME" "$BASE/Launcher"

# Ad-hoc codesign executable files (binary and dylibs).
codesign --sign - --force "$BASE/$GAME" "$BASE/Launcher" "$BASE"/*.dylib

# If files have been patched on a different host and later copied,
# we need to remove the quarantine attribute from binaries.
xattr -c "$BASE/$GAME" "$BASE/Launcher" "$BASE"/*.dylib

# Create bundle directory structure inside temp directory.
# The bundle is only moved into BASE once all steps succeed , so a failure
# at any point leaves nothing malformed in the game directory.
mkdir -p "$GAME_BUNDLE_TMP/Contents/MacOS"
mkdir -p "$GAME_BUNDLE_TMP/Contents/Resources"

# Copy the signed binary and dylibs into Contents/MacOS/ preserving permissions.
cp -p "$BASE/$GAME" "$GAME_BUNDLE_TMP/Contents/MacOS/"
cp -p "$BASE/Launcher" "$GAME_BUNDLE_TMP/Contents/MacOS/"
for dylib in "$BASE"/*.dylib; do
    [ -e "$dylib" ] || continue
    cp -p "$dylib" "$GAME_BUNDLE_TMP/Contents/MacOS/"
done

# Copy Data, Content, and Mods into Contents/MacOS/.
cp -Rp "$BASE/Data" "$GAME_BUNDLE_TMP/Contents/MacOS/"
cp -Rp "$BASE/Content" "$GAME_BUNDLE_TMP/Contents/MacOS/"
[ -d "$BASE/Mods" ] && cp -rp "$BASE/Mods" "$GAME_BUNDLE_TMP/Contents/MacOS/"

# MonoGame expects Content to be placed inside Contents/Resources, while game code
# expects Content to be placed alongside the binary.
# Create a Content symlink so MonoGame and game code finds assets via both search paths.
ln -sf "../MacOS/Content" "$GAME_BUNDLE_TMP/Contents/Resources/"

# Download / create bundle-specific resources.
curl -sL -o "$GAME_BUNDLE_TMP/Contents/Resources/Icon.icns" \
    "https://github.com/BigheadSMZ/Zelda-LA-DX-HD-Updated/raw/refs/heads/main/icon/macos/Icon.icns"

cat <<EOF >"$GAME_BUNDLE_TMP/Contents/Info.plist"
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleExecutable</key>
    <string>${GAME}</string>
    <key>CFBundleIdentifier</key>
    <string>com.zelda.ladxhd</string>
    <key>CFBundleIconFile</key>
    <string>Icon</string>
    <key>CFBundlePackageType</key>
    <string>APPL</string>
    <key>CFBundleShortVersionString</key>
    <string>${VERSION}</string>
    <key>CFBundleVersion</key>
    <string>${VERSION}</string>
</dict>
</plist>
EOF

# Launcher bundle is the same, but changing the executable
cp -Rp "$GAME_BUNDLE_TMP" "$LAUNCHER_BUNDLE_TMP"
sed -i '' \
    -e "s|<string>${GAME}</string>|<string>Launcher</string>|g" \
    -e "s|com.zelda.ladxhd|com.zelda.ladxhd.launcher|g" \
    "$LAUNCHER_BUNDLE_TMP/Contents/Info.plist"

# Codesign the app bundles.
codesign --sign - --force --deep "$GAME_BUNDLE_TMP"
codesign --sign - --force --deep "$LAUNCHER_BUNDLE_TMP"

# Atomically move the completed bundles into BASE, replacing any stale copy.
rm -rf "$GAME_BUNDLE"
mv "$GAME_BUNDLE_TMP" "$GAME_BUNDLE"
rm -rf "$LAUNCHER_BUNDLE"
mv "$LAUNCHER_BUNDLE_TMP" "$LAUNCHER_BUNDLE"
```
