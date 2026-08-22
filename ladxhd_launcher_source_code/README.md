# LADXHD Launcher
The LADXHD Launcher was mainly created to configure the "Advanced" options found in the `Mods` menu that are normally only able to be configured via [LAHDMods](https://gitlab.com/bighead.0/ladxhd_updated/-/wikis/LAHDMods), though it was also convenient to allow it to configure the game's main settings. It has also had the LADXHD Mod Maker functionality merged into it which can also be accessed via the `Mods` menu.

## Functionality
The launcher can of course launch the game, but it also serves a few other purposes as well.
- Launch the game via the `Play` button.
- Configure the game's settings via the `Settings` button.
- Configure advanced settings via the `Mods` button.
- Create/Import `.lahdpak` mods via the `Mods` button.

## Creating Modpacks
Modpacks are created from any and all modded that are currently installed. This includes all files and folders that are found within the `Mods` folder: Animations, Dungeon, Graphics, LAHDMods, Languages, Maps, Music, and SoundEffects. There may be additional folders sometime after the writing of this document. To create a new modpack, a mod author should have all the modded files they want to create into a pack, and click the `Create LAHDPak` button. This will bring up a new window where information about the pack can be entered and the pack can be created from. 

When the mod pack is created, a `.lahdpak` file will be generated that can be shared with others. This is basically a zip file that contains the modded files, an INI file, and optionally an image. Graphics assets will have `.vcdiff` patches generated from them as opposed to sharing the potentially copyrighted files, making the packs somewhat safer to share. When the user imports the pack they will be patched back into `.png` images.

## Installing Modpacks
Installing a modpack is as simple as opening the `Mods` menu, pressing the `Install LAHDPak` button, and selecting a `packname.lahdpak` file. A new window will open up with information about the modpack, potentially an image file, and a button to install it. The user must have the `Data\Backup` folder present since graphics mods `.vcdiff` patches rely on the original v1.0.0 files which were used as the diff. The reason is to have a common base across all versions of the game so that updating the game does not break the pack. 

## Additional Info
This section will just talk about random information concerning it's development. The first thing to point out is that the launcher plays sound effects when performing certain actions. These sound effects are in-game sound effects and are not included with the launcher for the reason they are original assets. The launcher has a built-in sound player that accesses the game `Content\SoundEffects` folder, decodes the corresponding `.xnb` file to a `.wav` file, and plays it. Other sound effects used in the mod maker/installer are included since they are freeware sound effects.

The `Mods` page is mostly auto-generated from the `advanced` file that is included in the `Resources` folder. When the launcher is first launched by the user, this file will be copied to the user's save folder. If it already exists, the files are compared and the existing `advanced` file is updated with any values that are missing from the current file, retaining the user's settings. To add more options to the launcher means editing the `advanced` file with those options. Since these are all based off of LAHDMods, they need to be manually programmed into the game's source code.