# LADXHD Archipelago Port

This branch contains the first native Archipelago integration slice for LADXHD. It keeps the original game assets out of source control and uses the maintained Archipelago Link's Awakening rules as the randomizer foundation.

## Implemented

- Archipelago.MultiClient.Net 6.7.1 in the cross-platform core project.
- `.apladxhd` JSON seed manifests with format, game, seed, and slot validation.
- Four independent Archipelago profiles, one for each in-game save position.
- Per-save seed/slot binding, received-item index persistence, and immediate saves after received items.
- Background network callbacks with all game-state mutation moved to the MonoGame update thread.
- Automatic reconnect, replay de-duplication, offline check recovery, and goal reporting.
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

Copy the generated `.apladxhd` file to Downloads, then open it from Android's Files app or
share it to **Import LADXHD Archipelago Seed**. The import screen validates the manifest, asks for
the Archipelago server, optional password, and target save position, writes that position's
profile into scoped app storage, and relaunches the game. Replaced imports are kept as
`.previous` backups. Importing another position does not touch existing profiles.

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
