
# LADXHD Archipelago

Native Archipelago integration and an assetless Android distribution for the community recreation
of *The Legend of Zelda: Link's Awakening DX*. The Android app can be installed alongside the
original LADXHD package and prepares its game data locally from a user-supplied, untouched v1.0.0
ZIP.

> [!IMPORTANT]
> This repository and its releases do **not** include the original game's `Content` or `Data`
> assets. You must legally obtain and supply the untouched Links Awakening DX HD v1.0.0 archive.
> The archive is verified and transformed entirely on your Android device; it is never uploaded.

[**Releases**](https://github.com/Odrannnn/LADXHD-Archipelago/releases) ·
[**Android setup**](ANDROID.md) ·
[**Archipelago guide**](ARCHIPELAGO.md) ·
[**Telemetry & privacy**](TELEMETRY.md) ·
[**Release policy**](RELEASING.md) ·
[**Upstream project**](https://gitlab.com/bighead.0/ladxhd_updated)

## What this fork adds

- A native Archipelago 0.6.7-compatible client with live item/location synchronization,
  reconnect handling, multi-seed profiles, received-item presentation, and per-save AP metadata.
- A `Links Awakening DX HD` APWorld and `.apladxhd` seed-manifest workflow.
- An Android seed importer that accepts shared files and connection details without ADB, plus an
  in-app setup screen that can choose seeds and edit installed room connections without a companion.
- A phone-native, transactional game-data builder: select the original v1.0.0 ZIP on first launch,
  then let the app verify, patch, stage, and activate the Android assets locally.
- A separate Android identity, `com.zelda.ladxhd.archipelago`, so this build can coexist with the
  upstream/original LADXHD installation.

## Android quick start

1. Download the APK and `ladxhd.apworld` from the latest GitHub release.
2. Install the APK. On first launch, choose your untouched `Links Awakening DX HD v1.0.0.zip`.
3. Wait for source and generated-data verification; the game opens automatically when complete.
4. Install `ladxhd.apworld` into Archipelago 0.6.7 and generate a room. In LADXHD, open
   **Settings → Archipelago**, choose the generated `.apladxhd`, enter the server address and port,
   optional password, and target save. Sharing/opening the seed file from another app remains
   supported but is no longer required.

Updates install over the existing app. Code-only releases reuse the installed assets. When the
asset format changes, the app reopens setup, reuses the previously granted ZIP when available,
backs up saves and AP profiles, and retains the previous verified asset version for recovery.

## Copyright and trademark disclaimer

This is a free, unofficial, non-commercial fan project. It is not affiliated with, authorized,
sponsored, or endorsed by Nintendo. *The Legend of Zelda*, *Link's Awakening*, related names,
characters, artwork, music, and trademarks belong to their respective owners. No ROM, commercial
game, or original v1.0.0 game-data tree is distributed here. Binary delta patches are useful only
with files supplied by the user.

## AI-generated and AI-assisted content disclaimer

The Archipelago integration, Android builder, tests, and documentation in this fork include code
and text produced with assistance from OpenAI Codex/ChatGPT. AI-assisted contributions were
reviewed, compiled, and tested by the maintainer, including end-to-end tests on Android, but may
still contain errors. Please report reproducible problems through GitHub Issues. This disclosure
does not describe or reclassify the upstream game recreation or third-party projects.

## Support

If this fork is useful to you, you can support its continued development on
[Ko-fi](https://ko-fi.com/odrannnn). Contributions are voluntary and do not purchase or provide Nintendo-owned
content.

## Upstream and attribution

This repository is a clean public snapshot derived from
[bighead.0/ladxhd_updated](https://gitlab.com/bighead.0/ladxhd_updated). The upstream repository's
history is not republished here, but its attribution and documentation are preserved below. The
Archipelago work is maintained independently in this fork.

---

## Preserved upstream documentation

## TLoZ: LADXHD Updated

- This fork requires the user to provide the assets from the original v1.0.0 release.<br>
- I have created tooling to make migrating everything to the latest version much easier.<br>
- Supports Windows (DX11/OGL), Android (x86/Arm64), Linux (x86/Arm64), and MacOS (x86/Arm64).

---

## Project Status

Please continue to [report issues](https://gitlab.com/bighead.0/ladxhd_updated/-/work_items)! I have mentioned that v2.0.0 would be my last version ever, but I made that statement when under the impression that the project was under attack and about to be wiped from the internet completely. When the Github repo was taken down, I immediately went into panic mode and put out whatever I had in the pipeline that was meant for v1.9.8 because I was afraid that I would not get another chance to fix what few issues I knew about at the time.

If you think something is an issue, report it! Worst case scenario is I don't accept it as an issue, it's something I can't fix, it's not worth fixing, or the issue was actually a change that was intended. As always, I am interested in anything that results in broken gameplay, minor bugs, or just inaccuracies from the original game (within reason). Some inaccuracies are intentional, others can't be helped due to differences in the engine, but for the most part, I try to do what I can to fix differences in behaviors.

---

## Version Milestones

- As of v1.1.0, the game is in a really good state and the "feel" is really close to the original game.  
- As of v1.2.0, all obvious bugs have been fixed and features from the [Redux romhack](https://github.com/ShadowOne333/Links-Awakening-Redux) were implemented.
- As of v1.3.0, I consider the work that I've done to be "feature complete" and everything from this point is gravy.
- As of v1.4.0, the gravy train never stopped and much work has been done to make this port more accurate.
- As of v1.5.0, it has evolved into something I never dreamed of. Hundreds of issues fixed with tons of features.
- As of v1.6.0, just about every small detail from the original game has been restored and/or replicated.
- As of v1.7.0, it's been ported to multiple platforms and every single (known) bug since v1.0.0 has been fixed.
- As of v1.8.0, modding support has been greatly enhanced and some of the most obscure bugs have been fixed.
- As of v1.9.0, everything is cross platform, a pixel grid shader, cheats, animation fixes, the game is near perfect.
- As of v2.0.0, whatever the state of the game, I am done. My main repo was destroyed and I am tired of this project.

---

## Classic Camera: Screen Based Scrolling

This section was added because over the past year I have seen far too many people say that they will never even try this version because it lacks "screen based scrolling". But this port has had this for several months! It was introduced way back in v1.4.3, yet I still see comments from time to time saying this version does not have it. It does, it's called **Classic Camera** and can be found in the "Camera" menu. So if you see people spread this misinformation, kindly correct them and point them to this page. The camera scrolls faster in this port by default, but the transition speed can be slowed down to match the original via the Launcher.

At this point in development, it's possible to almost completely replicate the original experience. That is aside from having a minimum of four inventory buttons instead of two (with the possibility to increase it to six) and a few other modern enhancements. By choosing the **Purist** preset from the "Presets" menu, the game can get extremely close to the original experience. It's obviously not a replacement to the original game, but it is there for those who enjoyed the original but don't like many of the "modern features" of this port.

---

## Patching to the Latest Version

To download the latest update, there is a patcher on the [Releases](https://gitlab.com/bighead.0/ladxhd_updated/-/releases) page.<br>
If you wish to build the game yourself, see **Personal Build / Publishing**.

- Find the v1.0.0 release originally from itch.io.
- It's a good idea to keep a <ins>backup</ins> of v1.0.0.
- Download the patcher from the releases page that matches your OS.
- Drop it into the **root folder** of v1.0.0 (or v1.1.4+).
- Open the patcher. Select the desired **Platform** and **Target**.
- Press the "Patch" button. It will take a bit to finish.
- When it is done, the patcher can be deleted.

Please see [ANDROID.md](ANDROID.md), [LINUX.md](LINUX.md), and/or [MACOS.md](MACOS.md) for more information regarding these operating systems.

### Headless Mode: Command Line Patching

The patcher supports headless mode for automated installations and scripts:

```
LADXHD.Patcher.exe --headless
```

| Option | Description |
|--------|-------------|
| `--headless` | Run without GUI prompts |
| `--platform <value>` | Target platform (default: windows)<br>Values: windows, android, linux-x86, linux-arm64, macos-x86, macos-arm64 |
| `--graphics <value>` | Target graphics API<br>Default: directx (windows), opengl (all others)<br>Values: directx, opengl |
| `--help`, `-h` | Show help message |

| Exit Code | Meaning |
|-----------|---------|
| 0 | Success |
| 1 | Game executable not found |
| 2 | Patching failed |
| 3 | Invalid arguments |

---

## Updating via Launcher

Since v1.8.4, the Launcher now has a feature to detect when a new version of the game has been released. A button will appear in the top left corner of the Launcher window alerting the user a new version is available. This button can be clicked to update the game to the latest version. The game must be updated with the Patcher from the [Releases](https://gitlab.com/bighead.0/ladxhd_updated/-/releases) page at least once to v1.8.4+ for this feature to be available.

---

## Creating Android APK

The patcher is able to create an APK for Android. This is much easier on Windows since the patcher requires no additional software. On Linux and MacOS, creating an APK does require additional software that can not be reasonably included in the patcher. See the [Android Guide](https://gitlab.com/bighead.0/ladxhd_updated/-/blob/main/ANDROID.md) which covers just about everything that's needed to know.

---

## About This Repository

A few years back, an anonymous user posted a PC Port of Link's Awakening on itch.io built with MonoGame. It wasn't long before the game was taken down, fortunately the release contained the source code. This is a continuation of that PC Port but with the assets stripped away to avoid copyright issues. 

This section explains the files and folders found in the base of this respository.<br>
All software is Windows only aside from the game which has been ported to Android and Linux.

- **assets_original**: This is where the **"Content"** and **"Data"** folders from v1.0.0 should go.
- **assets_patches**: Contains xdelta3 patches that are the difference of assets from v1.0.0 to the latest updates.
- **ladxhd_game_source_code**: Source code for The Legend of Zelda: Link's Awakening DX HD.
- **ladxhd_migrate_source_code**: Source code for the migration tool which can apply/create assets patches.
- **ladxhd_modmaker_source_code**: Source code for the modmaker which can create mod installers.
- **ladxhd_patcher_source_code**: Source code for the patcher to update the game to the latest version.
- **LADXHD_Migrater.exe**: This is the migration tool used to apply or create patches to the assets.
- **Unblock-All-Files.ps1**: This script can be used to unblock all files automatically for Visual Studio.

The game is built with the latest version of [MonoGame](https://monogame.net/).

---

## About This Fork

I am a terrible programmer, but I have a love for this game. A ton of forks popped up, some with fixes, but nowhere were they all centralized. This fork attempted to find and implement all the various fixes and improvements spread across the other various forks. Once that was done, I started tackling the issues from the repository this was cloned from. And after that was done, I worked on anything else I could find that would make the game feel more like the original game.

Feel free to commit any potential fixes as a PR. There are no coding guidelines and any style is welcome as long as the code either fixes something broken or makes the game behave closer to the original. But do try to at least keep it neat.
