# LADXHD Migration Tool
The LADXHD Migration Tool can be used to take v1.0.0 assets and update them to the latest versions that this repo uses. Patches are in the `.vcdiff` format and can be found in the `assets_patches` folder. A Windows build of the migration tool can be found in the root folder of the repo, and builds for other OS can be found in `migration_tool_builds`.

## Prerequisites
You will need the original v1.0.0 release that includes the source code. I can not tell you where to get it, but it can be found in various places around the internet. This situation is just an unfortunate reality to keep this project somewhat safe.

- Unzip the v1.0.0 release. You should see `Content`/`Data` folders, `source.7z`, and `Link's Awakening DX HD.exe`.
- Copy the `Data` folder from here to the `assets_original` folder.
- Extract `source.7z`. Inside you should find a `Content` folder. Copy to the `assets_original` folder.
- You do  **NOT**  want the  `Content`  folder from the game folder as the files are already compiled.
- At this point you should have  `Content`  and  `Data`  folders inside the `assets_original` folder from v1.0.0.
-  Just to recap: `Content` comes from the **source code folder**, `Data` comes from the **game folder**.

## Migrating Assets From v1.0.0
This step is required to obtain the latest assets that the game uses before it's possible to build the game.

- Launch the included migration tool `LADXHD_Migrater.exe`.
- Click `Migrate Assets From v1.0.0` button to start applying patches.
- Wait for it to finish. That is all it takes to migrate the assets!
- To verify if it worked, navigate to: `.\ladxhd_game_source_code\ProjectZ.Content\Content\Fonts`.
- You should see fonts and files like `smallFont_redux.png` and `smallFont_chn.fnt`.

## Generating Patches of Updated Assets
If wanting to contribute a pull request with updates assets, use the patches and not the assets themselves.

- Click the button `Create Patches of Updated Assets`.
- Wait for it to finish. This replaces all patches in `assets_patches` with updated assets.

## Building the Game and Launcher
Building the Windows version on Windows shouldn't require anything more than the [.NET 9.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/9.0).  Building Linux and MacOS while on Windows is slightly more difficult and requires setting up WSL with Wine, and configuring it so that WSL path is in the source code folder. 

- Select the OS via `Platform` and select the Graphics API via `Target`.
- Click the `Create a new Build` button.
- If it was successful, it can be found in the `_Publish` folder in the same path as the migration tool.

## Cleaning Build Files
This is just a feature to wipe out all the `_Publish`, `bin`, and `obj` folder across the entire project. It will clean them from every single project within the source code folder, including:

- ladxhd_game_source_code
- ladxhd_launcher_source_code
- ladxhd_migrate_source_code
- ladxhd_patcher_source_code
- vcdiff_source_code
