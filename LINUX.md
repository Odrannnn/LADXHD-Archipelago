# 🐧 Patching, building and emulating on Linux

## Using the patcher

The patcher available on the [Releases](https://gitlab.com/bighead.0/ladxhd_updated/-/releases) page runs natively on Linux — no Wine required.

When [running the patcher](https://gitlab.com/bighead.0/ladxhd_updated/#patching-to-the-latest-version) choose `Linux` as the platform, and `OpenGL` as the target. The patcher should take care of making the resulting binaries executable:

```
⚙️ "Link's Awakening DX HD"        # game binary
⚙️ Launcher                        # launcher binary
```

> [!Note]
> Launcher and game binaries need to be kept in the same directory for the launcher to work correctly.

If the patcher fails to perform these steps and the resulting files aren't executable, you can fix them manually:

```bash
$ chmod +x "Link's Awakening DX HD"
$ chmod +x Launcher
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
From the `ladxhd_game_source_code` directory, publish the DesktopGL host with the Linux profile for your hardware architecture:
```bash
# x64
$ dotnet publish ProjectZ.DesktopGL/ProjectZ.DesktopGL.csproj -c Release -r linux-x64 -p:PublishProfile=FolderProfile_Linux-x86_64

# arm64
$ dotnet publish ProjectZ.DesktopGL/ProjectZ.DesktopGL.csproj -c Release -r linux-arm64 -p:PublishProfile=FolderProfile_Linux-Arm64
```

The resulting ready-to-run binaries will be available in `ladxhd_game_source_code/_Publish/Linux-x86_64` or `ladxhd_game_source_code/_Publish/Linux-Arm64`

## Integrating with desktop environments (Gnome / KDE)

For the game to appear in application launchers / docks / application switchers, a `.desktop` entry needs to be created. Here is a template of a working one, just change the paths to match your setup:

```
[Desktop Entry]
Type=Application
Name=Link's Awakening DX HD
Exec="/path/to/Links Awakening DX HD/Link's Awakening DX HD"
Icon=/path/to/Links Awakening DX HD/Data/Icon/Icon.svg
Terminal=false
StartupWMClass=Link's Awakening DX HD
Categories=Game;
```

> [!WARNING]
> Only the `Exec` entry requires quoting if the path contains spaces! `Icon` might not work if quoted.

Save this file as `~/.local/share/applications/links-awakening-dx-hd.desktop` and the game should now show up in the usual spots in your desktop environment.

## Running on Linux with Proton

> [!WARNING]
> The game has moved to .NET 9 from .NET 6 since this guide was written, it is uncertain if this approach is still functional with the current version.

### ⚡ Quick Start (Recommended)

Use the automated installer for a hassle-free setup:

[zladxhd-installer](https://github.com/jslay88/zladxhd-installer)

---

### 🔧 Manual Setup

If you prefer to set things up yourself, follow these steps:

#### 1. Install Protontricks

Follow the [official installation guide](https://github.com/Matoking/protontricks?tab=readme-ov-file#installation).

<details>
<summary>⚠️ <strong>Flatpak users: You MUST set up aliases!</strong></summary>

```bash
echo "alias protontricks='flatpak run com.github.Matoking.protontricks'" >> ~/.bashrc
echo "alias protontricks-launch='flatpak run --command=protontricks-launch com.github.Matoking.protontricks'" >> ~/.bashrc
source ~/.bashrc
```

</details>

#### 2. Extract the Game

Extract your game archive to a location of your choice:
```
~/Games/LADXHD/
```

#### 3. Add to Steam

1. Open Steam → **Games** → **Add a Non-Steam Game to My Library**
2. Browse to and select `Link's Awakening DX HD.exe`

#### 4. Configure Proton

1. Right-click the game in your Steam Library → **Properties**
2. Go to **Compatibility** tab
3. Check **Force the use of a specific Steam Play compatibility tool**
4. Select **Proton Experimental** (or your preferred version)

#### 5. Create the Wine Prefix

1. Launch the game from Steam
2. You'll see an error about missing .NET - click **No** to close it
3. Close the game

#### 6. Install .NET Runtime

Run this command to install the required .NET Desktop Runtime:

```bash
protontricks $(protontricks -l | grep -oE "Link's Awakening.*\(([0-9]+)\)" | grep -oE "[0-9]+") -q dotnetdesktop6
```

#### 7. Apply the HD Patch

Navigate to your game folder and run the patcher:

```bash
cd ~/Games/LADXHD
protontricks-launch --appid $(protontricks -l | grep -oE "Link's Awakening.*\(([0-9]+)\)" | grep -oE "[0-9]+") LADXHD.Patcher.v1.5.2b.exe
```

<details>
<summary>🤖 <strong>Silent Mode (for scripts/automation)</strong></summary>

The patcher supports silent mode for automated installations:

```bash
protontricks-launch --appid $(protontricks -l | grep -oE "Link's Awakening.*\(([0-9]+)\)" | grep -oE "[0-9]+") LADXHD.Patcher.v1.5.2b.exe --silent
```

**Options:**
| Flag | Description |
|------|-------------|
| `--silent`, `-s` | Run without GUI prompts |
| `--help`, `-h` | Show help message |

**Exit codes:**
| Code | Meaning |
|------|---------|
| 0 | Success |
| 1 | Game executable not found |
| 2 | Patching failed |

</details>

#### 8. Play! 🎮

Launch the game through Steam and enjoy!

---

### ⚠️ Known Issues

| Issue | Workaround |
|-------|------------|
| Window resizing on KDE | Drag window borders manually |

---

### 💾 Save Data Location

Your saves and settings are stored in the Wine prefix:

```bash
~/.steam/steam/steamapps/compatdata/<APP_ID>/pfx/drive_c/users/steamuser/AppData/Local/Zelda_LA
```

To open the folder directly:

```bash
cd ~/.steam/steam/steamapps/compatdata/$(protontricks -l | grep -oE "Link's Awakening.*\(([0-9]+)\)" | grep -oE "[0-9]+")/pfx/drive_c/users/steamuser/AppData/Local/Zelda_LA
```
