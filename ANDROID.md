# 🤖 Android Information

----

## Phone-native setup for the Archipelago build

The public **LADXHD Archipelago** APK is intentionally assetless. It contains the game code,
Archipelago integration, and redistribution-safe VCDIFF migration data, but it does not contain
the original game's `Content` or `Data` files.

On first launch:

1. Keep the untouched **Links Awakening DX HD v1.0.0.zip** on the phone (Downloads is fine).
2. Tap **Choose v1.0.0 ZIP** and select it in Android's document picker.
3. Leave the app open while it verifies the exact source SHA-256, applies the Android patches,
   and verifies the complete generated-data fingerprint.
4. The game starts automatically when the local installation is valid.

The ZIP is read through Android's Storage Access Framework and is never uploaded. No ADB,
desktop patcher, storage-wide permission, APK generation, or on-phone APK signing is involved.
Generated files are private to this app under
`Android/data/com.zelda.ladxhd.archipelago/files/GameAssets`.

### Updating

Install a newer APK over the existing `com.zelda.ladxhd.archipelago` package. It must be signed
with the same release key. Saves, settings, Archipelago profiles, and generated assets stay in
place. If the new release uses the same asset format, it launches normally. If its asset version
changes, setup opens automatically and offers **Rebuild using previously selected ZIP** when
Android still grants access to that document. Otherwise, select the v1.0.0 ZIP again.

Migration is transactional: a new version is generated in staging, checked, then activated with
an atomic manifest replacement. The previous version remains usable until activation succeeds.
If the active pointer is lost or unreadable, startup searches retained compatible installations
and restores the newest verified manifest automatically.
Before activation, `SaveFiles` and `Archipelago` are copied to a timestamped `UpdateBackups`
folder. Interrupted or invalid migrations never replace the active version.

You can also open the v1.0.0 ZIP with **LADXHD Archipelago** from a file manager to request a
manual rebuild. Do not uninstall the app for an update: Android normally deletes app-private
data on uninstall.

### Starting an Archipelago seed without the companion

1. Generate the room with `ladxhd.apworld` and copy that player's `.apladxhd` file to the phone.
2. On the LADXHD file-select screen, open **Settings → Archipelago**.
3. Tap **Choose .apladxhd seed file** and select it with Android's document picker.
4. Review the seed and player slot, enter the server as `host:port`, add the room password when
   needed, and choose Save 1 through Save 4.
5. Tap **Import and launch**, then create a new game in that save position for a new seed.

While that Archipelago save is active, open the pause menu and choose **Magpie Tracker** to use
the web tracker in a right-side overlay. LADXHD keeps the pause screen open underneath the tracker,
automatically starts its local autotracker bridge, and opens the page with Archipelago logic
selected. Closing the panel returns to the pause screen. The page itself requires an internet
connection; game state is supplied to it locally and the room password is not shared.

The setup screen also lists installed profiles. Open one to change its server port or password
without importing the seed again. The player slot comes from the `.apladxhd`; credentials alone
are not enough because the native client needs that player's generated location mapping. Opening
or sharing a `.apladxhd` from another app remains available as an alternative.

### Using the live wallpaper

Open **Settings → Live wallpaper**, choose a starting location and Link activity, then tap
**Preview and set wallpaper** to open Android's system picker. Enable **Follow Link through
overworld loading zones** to let the camera follow Link beyond the starting view and through
supported doors, stairs, and interiors. With following disabled, the selected location or rotating
scene mode remains the backdrop. Tap an accessible place on the wallpaper to send Link toward it.

The lightweight simulation reads the installed map's objects and animation sheets. Supported
residents and enemies remain local to their map; Marin's notes, BowWow's chain, rooster motion,
combat, swimming, jumping, rock lifting/throwing, pushable blocks, bush drops, and chest
presentation reuse gameplay calculations and original sprites. Routes favor unexplored areas,
recover from stalled targets, and account for supported obstacles and item-assisted crossings.
Tapped destinations survive camera changes and visibility resumes. When blocked, Link retries the
same goal a bounded number of times before returning to exploration; real progress renews that
retry budget. Fallback searches reuse the already discovered reachable area instead of repeatedly
searching disconnected rooms. A missing route does not relocate Link to a preset position.
Side-view passages use gravity, feather jumps, swimming, ladders and directional platforms instead
of top-down routes. Their bounded route search replays button inputs through the same lightweight
physics used during movement; gravity, steering, jump and ladder calculations are shared with the
game. Ladder exits use the installed door triggers, including upward-input doors, and an arrival
latch prevents immediate return through the entrance. Taps request physically reachable routes;
an unreachable target does not turn air into walkable floor. Planning is incremental while visible,
and the physics advances at 60 Hz independently of the selected rendering frame rate.
Long side-view tap routes retain their destination across bounded planning sections. Unreachable
taps eventually return to autonomous navigation instead of disabling it for the rest of the room.
This is an ambient simulation, not a complete autonomous playthrough or a replacement for the game.

Choose **15 FPS** for lower power use, **30 FPS** for balanced motion, or **High FPS (60 FPS)** for
smoother motion with increased battery consumption and possible device heating. Animation timing
uses elapsed time rather than making gameplay run faster at the higher frame rate. Static tile
rendering is cached; animation-off mode redraws once per second while visible, with immediate
redraws for touch or launcher movement. Rendering stops when Android marks the wallpaper hidden.
The wallpaper supplies scene-color hints to Android for system-bar contrast; the launcher/system
decides whether to use them.

**Time of day → Follow system time** now drives spatial outdoor lighting: directional cast shadows
attenuate direct sunlight, cooler ambient light remains in shaded areas, and installed lamp light
textures illuminate their surroundings independently. This replaces the old screen-wide color
overlay. Sunrise and sunset produce longer shadows pointing in opposite directions; midday shadows
are shorter, and direct sunlight/shadows fade away at night. Use the **Sunrise** and **Sunset** time
buttons to configure local-clock hours without location access or internet. **Day**, **Sunrise**,
**Sunset** and **Night** modes hold that phase for preview; **Original map lighting** retains the
game's fixed shadow/light settings. Houses, caves and dungeons keep their own map lighting.

The sun cycle is an optional wallpaper extension, not an original game mechanic or a 3D relighting
system. It reuses installed shadow sprites, light textures and the game's projection rules; painted
highlights in the sprites remain unchanged. Automatic solar parameters update in ten-minute local-clock
intervals while visible; changing the lighting mode or sunrise/sunset settings takes effect immediately.
Settings are cached between changes, and sprite/shadow drawing reuses temporary rectangles.
Light maps remain cached; distant moving shadows update separate small regions with the original
blur margins, merging overlaps and falling back to one region in crowded scenes. The **Mabe Sunset**, **Forest
Night**, and **Island Journey** presets remain available. Camera scrolling reuses overlapping blurred
shadows and lighting where pixel alignment permits, refreshing exposed strips and blur borders;
map, zoom, static-object and solar-shadow changes still use a full refresh. Shadow pixel transfers
use pooled buffers sized to each sampled region, retaining the same blur and lighting output.
The blur reuses neighboring samples and row offsets without changing its filter or resolution.
Map-object drawing also caches sprite keys and atlas dimensions and rejects off-screen objects
before unnecessary lookups, preserving placement, draw order and moved/removed object state.
Pending camera scroll targets are constrained again after rotation or resizing, so an old target
outside the new viewport bounds cannot prevent later scrolling.

The wallpaper is silent and does not start a full hidden game engine,
write gameplay saves, grant Archipelago items, or open an Archipelago connection. Its collected
items, opened chests, and defeated enemies are simulation state only. It uses the locally prepared
game assets, so normal first-run ZIP setup is required; no original game-data tree is embedded in
the public APK.

----

## Using the legacy desktop patcher

The patcher can be found on the [Releases](https://gitlab.com/bighead.0/ladxhd_updated/-/releases) pages. 

Creating an APK can be as simple as patching to any other version of the game if on Windows. To create an APK on Linux or MacOS, additional packages are required. See the section below on how to set up your device to build an APK.

----

## Creating an APK on Linux and MacOS

Trying to create an APK on these operating systems may result in the following error message:\
*"The following tools are required for APK generation but were not found in your PATH: java, zipalign, 7z (or 7za,7zz)"*

There are three additional packages required:
- **Java Development Kit**: Provides the `java` application: [Oracle](https://www.oracle.com/java/technologies/downloads/), [Adoptium](https://adoptium.net/temurin/releases), [Red Hat](https://developers.redhat.com/products/openjdk/download), [Azul](https://www.azul.com/downloads/?package=jdk#zulu), [Amazon](https://aws.amazon.com/corretto/), [BellSoft](https://bell-sw.com/), etc.
- **Android Studio**: Provides the `zipalign` application. Download can be found [here](https://developer.android.com/studio).
- **7-Zip**: The package and the command depends on the OS. Make sure it provides `7z`, `7za`, or `7zz`.

All three apps must be on `PATH` for the patcher to access them. The flavor of Java shouldn't matter, just go with the vendor that looks good to you. As for 7-Zip, there are multiple packages available for Linux and MacOS. Just make sure that the application they provide is `7z`, `7za`, or `7zz`.

If all else fails, find a PC or laptop with Windows installed and create the APK via the patcher there. The patcher has all of these tools built-in for Windows, which isn't possible on other OS due to how they work. 

----

## Low Resolution Devices

If the UI, menus, and HUD appears small and does not increase in size with UI Scale, chances are, your device's screen does not meet the minimum resolution to scale up to 2x. The UI Scale is based on the resolution of the menus which is 380x256, except on Android where it is 380x240. That means the minimum resolution the screen needs to be to meet the 2x requirement is 760x480. If the screen has a horizonal resolution of 720 or lower for example, it is 40 pixels shy of being able to hit that 2x resolution requirement.

There is an imperfect workaround to this issue that allows scaling the UI elements with a decimal scale. This means you won't get perfectly square pixels on these elements, but it is hardly noticeable and the sacrifice to have a larger UI is usually worth it to most people. There exists a few [LAHDMods](https://gitlab.com/bighead.0/ladxhd_updated/-/wikis/LAHDMods) that can force a 2x scale to the HUD and scale the UI and menus using decimal values. 

The LAHDMods you will need are as follows:

| Name | Description | Download |
|----|----|----|
| HUD Overlay | Adjusts the size, scale, and color of the HUD overlay: items, rupees, hearts, and keys.  Only accepts integer values for custom scaling.| [HUDOverlay.zip](https://gitlab.com/bighead.0/ladxhd_updated/-/raw/main/_lahdmods/HUDOverlay.lahdmod) |
| Textbox Overlay | Adjusts textbox color and scaling. Decimal values are allowed but integers are highly recommended. | [TextboxOverlay.zip](https://gitlab.com/bighead.0/ladxhd_updated/-/raw/main/_lahdmods/TextboxOverlay.lahdmod) |
| Inventory Overlay | Allows forcing color and scale for the inventory. Decimal values are allowed but integers recommended. | [OverlayManager.zip](https://gitlab.com/bighead.0/ladxhd_updated/-/raw/main/_lahdmods/OverlayManager.lahdmod) |
| In-Game Menus | Allows forcing a scale for the in-game menus. Decimal values are allowed but integers are highly recommended. | [PageManager.zip](https://gitlab.com/bighead.0/ladxhd_updated/-/raw/main/_lahdmods/PageManager.lahdmod) |

Download the LAHDMods, unzip them, and drop them into the `..\<gamefolder>\Mods\LAHDMods` folder. From there, each one must be configured by editing them with a text editor. You may experiment with the values to get it to scale how you want, but I have provided some values below which should work on the majority of devices.

<details>
<summary><b>- LAHDMod Configuration -</b></summary>

**HUDOverlay.lahdmod**
```
custom_items_scale = 2
custom_heart_scale = 2
custom_rupee_scale = 2
custom_keys_scale = 2
custom_sicon_scale = 2
```

**TextboxOverlay.lahdmod**
```
textbox_scale = 1.85
```

**OverlayManager.lahdmod**
```
inventory_scale_override = 1.85
```

**OverlayManager.lahdmod**
```
menu_scale_override = 1.85
```
</details>

Note that `HUDOverlay.lahdmod` does not accept decimal values. Entering decimals will fail to load the values and you will see no change in-game, just use a value of `2` which will be a bit bigger than normal but fine enough. As for the other LAHDMods, using `1.85` is a safe value for most screens and you can try any value you want. Some users have had success using values like `1.90` and even up to `1.92`, just make sure that you keep it below `2.00` as that will cause the menus, inventory, and other UI elements to exceed the bounds of the screen. The hard limit is there for a reason!

After you have the LAHDMods set up the way you want, there is two ways you can make use of them:
- Plug your Android into your PC via USB, enable file sharing, and copy the `*.lahdmod` files to:\
`InternalStorage:\Android\data\com.zelda.ladxhd.archipelago\files\Mods\LAHDMods`
- When creating an APK with the patcher, check the option "**Pack installed mods into APK**".

Internal mods take priority over external mods. So if you have the same mods or LAHDMods built into the APK and in the scoped storage folder, the internal mods are what will be used. And that's it! Though it may seem daunting, it's actually fairly simple to set up once you understand what needs to be done.

----

## Save File Syncing Across Devices

The Android port has an option that is not found in other builds of the game to allow syncing saves across devices. Save file syncing requires a save folder that is outside of the game's scoped storage as Android will not allow other devices to have access to it. In order to sync save files across devices, you must give LADXHD permission to access shared storage. When enabling the option to save to shared storage, you will be prompted to give LADXHD access to the shared storage folder. After granting permission, you will have to enable the option one more time.

There are several applications that can sync across devices, the one I personally use is [Syncthing](https://syncthing.net/). The guide below is very basic, and assumes that both devices are connected to your home network. You may want to watch more advanced tutorials if the guide below does not work out for you.

<details>
<summary>- Syncthing Basic Setup Guide -</summary>

**Installation:**
- Install Syncthing on both your PC and Android devices. 
- The windows setup can be found [here](https://github.com/Bill-Stewart/SyncthingWindowsSetup/releases).
- The Google Play setup can be found [here](https://play.google.com/store/apps/details?id=com.github.catfriend1.syncthingandroid).

**PC Setup:**
- Install and open Syncthing. If your browser blocks it, add an exception.
- If it fails to create shortcuts, you can find the program installation at this location:
`C:\Users\UserName\AppData\Local\Programs\Syncthing`
- A page in your browser should load up. If it doesn't, load **ConfigurationPage.lnk** from the path above.
- Find **Folders** on the left and click the **+Add Folder** button.
- Give the folder a **Folder Label**. This is just a unique name for this configuration.
- Enter a simple **Folder ID** which is used on both devices to sync a folder (I use `ladxhd`).
- Enter the **Folder Path** to where your save files are. This is usually here:
`C:\Users\Bighead\AppData\Local\Zelda_LA\SaveFiles`
- Click on the **Sharing** tab, and check the Android device you wish to share with.
- The device must be on the network for it to be visible. It will appear under **Remote Devices** if connected.
- Click on the **Save** button to finish setting it up.

**Android Setup:**
- Load the game, open the **Game Settings** menu, and enable **Saves in Shared Storage**.
- You will be prompted to give the game access. Hit accept, and click the option one more time.
- This will create a save folder in shared storage named `LADXHD`. Close the game.
- Install and open Syncthing-fork and it will ask to grant permission to access all files. Toggle it to "on".
- Continuing with the Android setup, it will ask for permission for a few more things. Toggle "on" what you feel safe with.
- You should be at the main page now, click on **ADD FOLDER**.
- Give the folder a **Folder Label**. This is just a unique name for this configuration.
- Enter the same **Folder ID** used in the PC Setup (I use `ladxhd`).
- Click on **Directory** and select the path to save files in shared storage:
`/storage/emulated/0/LADXHD/SaveFiles`
- Under **Devices** select the PC you wish to share with. It must be on the network.
- Click the `SAVE` button at the top. 

Assuming everything worked out, the devices will then automatically start to sync the file. If there are conflicts, they will be copied and renamed. In my case, the Android saves were copied to the PC with "conflict" in the name. From here on out, the newest save file should always sync to the other device. 
</details>

----

## Frequently Asked Questions

**Q:** *Can the Archipelago build be prepared entirely on Android?*\
**A:** Yes. Its assetless APK uses Android's document picker and a managed VCDIFF decoder to
build versioned game data in the app's private storage. The original ZIP remains user-supplied;
neither it nor its extracted copyrighted assets are part of the distributed APK.

**Q:** *Why isn't the Launcher available on Android?*\
**A:** The Launcher is a separate application that modifies a configuration file that is shared by both the game and the launcher. Android's scoped storage does not allow apps to share access to files. Plus, some of the nuget packages that are used in the Launcher will not build for Android, meaning there is more than one limitation that prevents it. It is possible to copy the `advanced` configuration file the Launcher creates on desktop to Android and the game will make use of it.

Here is how this can be done:
- Navigate to the save folder where the `advanced` file is stored. On Windows, this is at:
`C:\Users\UserName\AppData\Local\Zelda_LA\advanced`
- Plug your Android into your PC via USB and enable file sharing. Copy the `advanced` file to:
`InternalStorage\Android\data\com.zelda.ladxhd.archipelago\files\advanced`
- You can also copy other save and configuration files from PC to Android via this method (or vice versa). 

**Q:** *Why is the UI and menus so small? Can it be fixed?*\
**A:** See the "Low Resolution Devices" section above this one. The amount of work to "fix" this issue would be astronomical as all 23 pages and their UI elements would need to be edited to work within a smaller resolution. Plus the various languages are already having trouble fitting in the UI elements, so some strings would need to be shortened somehow. Overall, this game was never designed to work on Android when it was created, so we can only be grateful that a port exists at all.

**Q:** *Is it possible to disable the on-screen controls?*\
**A:** Yes. Simply press the controller button icon in the center of the virtual controller. It is not possible to permanently disable the on-screen controller. The reason is that this could make it possible to accidentally disable the controls in the menu. And in the event a user does not have a bluetooth controller, they would be stuck. When the on-screen controls are disabled, the button will fade out after a few seconds, and touching the screen will make it reappear.

**Q:** *I have an issue that I don't see here. What do I do?*\
**A:** Report it and I'll see if I can fix it. You can submit a new issue [here](https://gitlab.com/bighead.0/ladxhd_updated/-/work_items).
