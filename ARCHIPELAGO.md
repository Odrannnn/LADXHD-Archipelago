# LADXHD Archipelago Port

This branch contains the first native Archipelago integration slice for LADXHD. It keeps the original game assets out of source control and uses the maintained Archipelago Link's Awakening rules as the randomizer foundation.

## Implemented

- Archipelago.MultiClient.Net 6.7.1 in the cross-platform core project.
- `.apladxhd` JSON seed manifests with format, game, seed, and slot validation.
- Four independent Archipelago profiles, one for each in-game save position.
- Per-save seed/slot binding, received-item index persistence, and immediate saves after received items.
- Background network callbacks with all game-state mutation moved to the MonoGame update thread.
- Automatic reconnect, replay de-duplication, offline check recovery, and goal reporting.
- An Android pause-menu Magpie Tracker page plus a WebSocket bridge for inventory and check autotracking.
- Central AP-to-LADXHD item translation, including progressive equipment and dungeon-bounded items.
- Randomized interception for keyed chests, persistent loose items, scripted rewards,
  shops, trade-sequence rewards, and event-backed checks.
- An Archipelago-only **Warp to Start** command in the in-game menu, which saves and returns the
  player to the starting house through the file-select screen.
- A companion `Links Awakening DX HD` APWorld that inherits the maintained LADX logic and emits `.apladxhd` files instead of ROM patches.
- A complete stable-key mapping for all 220 checks enabled by the supported default settings.
- Runtime and offline source-location catalog tools that do not commit copyrighted map data.

## Configure the client

The user-data root is the same root LADXHD already uses for saves. Each save position has its own directory:

```text
Archipelago/Profiles/Save1/connection.json
Archipelago/Profiles/Save1/seed.apladxhd
...
Archipelago/Profiles/Save4/connection.json
Archipelago/Profiles/Save4/seed.apladxhd
```

For desktop setup, copy and edit [`archipelago_world/connection.example.json`](archipelago_world/connection.example.json) in the matching profile directory. `save_slot` is zero-based (`0` through `3`) and must match that directory. A relative `seed_file` is resolved inside the profile directory. The original single `Archipelago/connection.json` plus `seed.apladxhd` layout remains supported as a legacy fallback.

Creating a new save permanently binds its seed and player name. Existing vanilla saves are not converted automatically. Switching saves disconnects the previous session and loads the selected save's own manifest, server, port, password, and received-item index.

Android already declares `android.permission.INTERNET`; the network client lives in `ProjectZ.Core`, so the same protocol code is shared by Android and desktop builds.

### Magpie Tracker autotracking

On Android, pause an active Archipelago save and choose **Magpie Tracker** to open the official
tracker page inside LADXHD. The page is preconfigured for Archipelago logic and connects to the
game's WebSocket bridge through `127.0.0.1:17026`; no address setup is required. Opening the
page starts the bridge on demand even when the profile option below is disabled. The bridge
implements Magpie's item and check features, including full resynchronization after either side
reconnects. It also sends the seed's non-secret slot options; the Archipelago password is never
exposed. Closing the page returns to the paused game.

The embedded page requires internet access to load `magpietracker.us` and is third-party web
content, so the tracker host receives normal web-request metadata. Autotracker messages remain on
the device through `127.0.0.1`, and navigation away from the tracker host is blocked. Android
WebView storage keeps normal Magpie settings between visits.

Enable **Keep Magpie autotracker bridge enabled** while importing or editing a profile when an
external tracker should connect before the embedded page has been opened. This keeps the same
port `17026` listener running whenever that bound save is active.

The listener accepts only connections from the same device by default. Enable **Allow Magpie
connections from the local network** when Magpie runs on a computer and LADXHD runs on Android,
then set Magpie's alternate autotracker IP to the Android device's local IP address. Only use the
LAN option on a trusted network: any device on that network can otherwise read the current seed's
tracker state from port `17026`. GPS and entrance tracking are not included in this first bridge.

Desktop profiles can set the same behavior directly in `connection.json`:

```json
"magpie_tracker_enabled": true,
"magpie_tracker_allow_lan": false
```

### First Android launch and app updates

The distributable Android APK does not embed the original `Content` or `Data` folders. On first
launch, select the untouched Links Awakening DX HD v1.0.0 ZIP in the setup screen. The phone
verifies its fixed SHA-256, applies the bundled VCDIFF patch set locally, validates a canonical
958-file output fingerprint, and only then launches the game. See [`ANDROID.md`](ANDROID.md) for
the exact setup and transactional update behavior.

APK updates retain installed assets and user data as long as they use the same application ID
and signing certificate. A changed asset-format version automatically returns to setup. The app
can reuse Android's persisted read grant for the previously selected ZIP or let the user choose
it again. No copyrighted game data is included in the published APK or repository.

### Android import (no ADB required)

From the file-select screen, open **Settings → Archipelago**. Choose the generated `.apladxhd`
file from Android's document picker, enter the Archipelago server and port, optional password,
target save position, and optional external Magpie settings, then launch. The player slot is displayed from
the seed manifest and is not silently overridden, because each player's manifest contains that
slot's placements.

The same screen lists every valid installed profile. Selecting one lets the user change its
server address/port or password and relaunch without choosing the seed again. This makes room
port changes and normal reconnect setup independent of the companion app.

Opening the `.apladxhd` from Android's Files app or sharing it to **Import LADXHD Archipelago
Seed** remains supported. Both entry points validate the manifest, write the selected profile
into scoped app storage, and relaunch the game. Replaced imports are kept as `.previous` backups.
Importing another position does not touch existing profiles.

Re-importing the same seed into the same position updates its connection details without
resetting that save's item-receive progress. This is also how to change a server port. Importing
a different seed over a position requires creating a new in-game save there; otherwise the saved
seed binding deliberately reports a mismatch instead of mixing two seeds.

If Android does not offer LADXHD from a direct tap because the sending app does not preserve
the filename, use that app's **Share** action instead. A solo randomizer still needs a local
Archipelago server because item delivery uses the normal Archipelago protocol.

### Android connection intent

A companion app or automation can attach connection hints to the same explicit `ACTION_SEND`
intent as the `.apladxhd` content URI. The importer pre-fills these values but always presents
the confirmation screen before changing a profile:

```text
package: com.zelda.ladxhd.archipelago
component: com.zelda.ladxhd.archipelago/.ArchipelagoImportActivity
type: application/x-apladxhd
android.intent.extra.STREAM: content URI for the .apladxhd
com.zelda.ladxhd.archipelago.extra.SERVER: host:port string
com.zelda.ladxhd.archipelago.extra.PASSWORD: optional string (an explicit empty string clears it)
com.zelda.ladxhd.archipelago.extra.SAVE_SLOT: optional zero-based integer, 0 through 3
```

The sender must put the content URI in `ClipData`, grant `FLAG_GRANT_READ_URI_PERMISSION`, and
use `Intent.EXTRA_STREAM`. Ordinary file managers do not know the room credentials and
therefore share only the seed file. Passwords should be sent as explicit intent extras rather
than placed in a deep-link query string where they may be retained in browser history or logs.

## Build the APWorld

Run:

```text
python tools/build_apworld.py
```

The output is `dist/ladxhd.apworld`. Install it into Archipelago's `custom_worlds` directory, restart Archipelago, and generate a player using [`archipelago_world/player.example.yaml`](archipelago_world/player.example.yaml) as a starting point.

## Current boundary

The supported default configuration generates a complete 220-location manifest with no
unmapped checks. An active client can still record discovered source keys for diagnostics to:

```text
<user-data>/Archipelago/location-catalog.jsonl
```

For maintenance against another asset revision, `tools/catalog_ladxhd_sources.py` can build
an offline inventory from the migrated `ProjectZ.Core/Data` directory. Scripted full-item
grants use `script:<escaped-script-key>:<action-index>` and shop checks use `shop:<price>`.

Entrance shuffle, dungeon shuffle, non-normal logic, modified overworlds, non-instrument goals,
trade-quest shuffle, roosterless routes, non-vanilla warps/boots/quickswap/Trendy Game behavior,
and disabled stealing are rejected by the APWorld MVP. These settings require ROM/map or control
patches that the native HD runtime does not currently apply.

## Verification

The core project and smoke-test project can be verified with:

```text
dotnet build ladxhd_game_source_code/ProjectZ.Core/ProjectZ.Core.csproj
dotnet run --project ladxhd_game_source_code/ProjectZ.Archipelago.SmokeTests/ProjectZ.Archipelago.SmokeTests.csproj
python -m py_compile archipelago_world/ladxhd/__init__.py tools/build_apworld.py tools/catalog_ladxhd_sources.py
python tools/build_apworld.py
```

Game assets must still be supplied by the user through the Android first-run importer or the
upstream desktop workflow. Before distributing binaries or publishing a hosted fork, confirm
the upstream source-distribution terms with its maintainer; no explicit source license file is
present in the upstream repository. Release checks must verify that the APK contains
`assets/Bootstrap/*` but no `assets/Content/*` or `assets/Data/*` entries.
The maintainer signing/version procedure and publication checklist are in
[`RELEASING.md`](RELEASING.md); `tools/verify_assetless_apk.py` provides the mandatory APK guard.
