using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using static LADXHD_Launcher.VCDiff;

namespace LADXHD_Launcher
{
    internal class ModMaker_Functions
    {
        private static int FileCount;
        private static int TotalCount;

/*-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        FILE MAPPING CODE : NOT ALL FILES AND PATCHES ARE 1:1 FROM ORIGINAL GAME VERSION. NEW FILES NEED A "BASE" TO BE CREATED FROM USING A PATCH
       
-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------*/

        // SOME RESOURCES ARE USED TO CREATE MULTIPLE FILES OR PATCHES. EACH ARRAY BELOW HOLDS ALL VERSIONS OF A FILE THAT IS
        // BASED OFF OF ANOTHER FILE. THE "MASTER" FILE THAT CREATES THESE VERSIONS IS LINKED TO THEM IN THE DICTIONARY BELOW

        private static string[] langFiles  = new[] { "chn.lng", "deu.lng", "esp.lng", "fre.lng", "ind.lng", "ita.lng", "por.lng", "pte.lng", "rus.lng", "swe.lng" };
        private static string[] langAchiev = new[] { "achieve_chn.lng", "achieve_deu.lng", "achieve_eng.lng", "achieve_esp.lng", "achieve_fre.lng", "achieve_ind.lng", "achieve_ita.lng", "achieve_por.lng", "achieve_pte.lng", "achieve_rus.lng", "achieve_swe.lng" };
        private static string[] langDialog = new[] { "dialog_chn.lng", "dialog_deu.lng", "dialog_esp.lng", "dialog_fre.lng", "dialog_ind.lng", "dialog_ita.lng", "dialog_por.lng", "dialog_pte.lng", "dialog_rus.lng", "dialog_swe.lng" };
        private static string[] smallFonts = new[] { "smallFont_redux.png", "smallFont_vwf.png", "smallFont_vwf_redux.png", "smallFont_chn_0.png", "smallFont_chn_redux_0.png" };
        private static string[] backGround = new[] { "menuBackgroundB.png", "menuBackgroundC.png", "sgb_border.png" };
        private static string[] lighting   = new[] { "mamuLight.png" };
        private static string[] linkImages = new[] { "link1.png", "weapons.png" };
        private static string[] linkAtlas  = new[] { "weapons.atlas" };
        private static string[] npcImages  = new[] { "npcs_redux.png" };
        private static string[] itemImages = new[] { "items_chn.png", "items_deu.png", "items_esp.png", "items_fre.png", "items_ind.png", "items_ita.png", "items_por.png", "items_rus.png", "items_swe.png", "items_redux.png", 
                                                     "items_redux_chn.png", "items_redux_deu.png", "items_redux_esp.png", "items_redux_fre.png", "items_redux_ind.png", "items_redux_ita.png", "items_redux_por.png", "items_redux_rus.png", "items_redux_swe.png" };
        private static string[] introImage = new[] { "intro_chn.png", "intro_deu.png", "intro_esp.png", "intro_fre.png", "intro_ind.png", "intro_ita.png", "intro_por.png", "intro_rus.png", "intro_swe.png" };
        private static string[] introAtlas = new[] { "intro_chn.atlas" };
        private static string[] miniMapImg = new[] { "minimap_chn.png", "minimap_deu.png", "minimap_esp.png", "minimap_fre.png", "minimap_ind.png", "minimap_ita.png", "minimap_por.png", "minimap_rus.png", "minimap_swe.png" };
        private static string[] objectsImg = new[] { "objects_chn.png", "objects_deu.png", "objects_esp.png", "objects_fre.png", "objects_ind.png", "objects_ita.png", "objects_por.png", "objects_rus.png", "objects_swe.png" };
        private static string[] photograph = new[] { "photos_chn.png", "photos_deu.png", "photos_esp.png", "photos_fre.png",  "photos_ind.png", "photos_ita.png", "photos_por.png", "photos_rus.png", "photos_swe.png", "photos_redux.png", 
                                                     "photos_redux_chn.png", "photos_redux_deu.png", "photos_redux_esp.png", "photos_redux_fre.png", "photos_redux_ind.png", "photos_redux_ita.png", "photos_redux_por.png", "photos_redux_rus.png", "photos_redux_swe.png" };
        private static string[] uiImages   = new[] { "ui_chn.png", "ui_deu.png", "ui_esp.png", "ui_fre.png", "ui_ind.png", "ui_ita.png", "ui_por.png", "ui_rus.png", "ui_swe.png" };
        private static string[] musicTile  = new[] { "musicOverworldClassic.data" };
        private static string[] dungeon3M  = new[] { "dungeon3.map" };
        private static string[] dungeon3D  = new[] { "dungeon3.map.data" };
        private static string[] newBridgeM = new[] { "bridge_l_castle.map", "bridge_r_castle.map" };
        private static string[] newBridgeD = new[] { "bridge_l_castle.map.data", "bridge_r_castle.map.data" };
        private static string[] bowwowanim = new[] { "bowwow_water.ani" };
        private static string[] dungeonani = new[] { "mapDungeon.ani", "mapManboPond.ani", "mapTeleporter.ani" };
        private static string[] boomerang  = new[] { "boomerangOrig.ani" };
        private static string[] effectani  = new[] { "bushExplosion.ani", "fireballDeath.ani" };
        private static string[] shaderFile = new[] { "GBCColorCorrection.fx", "PixelGrid.fx" };

        // THE "KEY" IS THE MASTER FILE THAT CREATES OTHER FILES FROM IT. THE "VALUE" IS THE STRING ARRAY THAT HOLDS THOSE FILES

        private static readonly Dictionary<string, string[]> fileTargets = new Dictionary<string, string[]>
        {
            { "eng.lng",              langFiles.Concat(langAchiev).ToArray() },
            { "dialog_eng.lng",      langDialog },
            { "smallFont.png",       smallFonts },
            { "menuBackground.png",  backGround },
            { "ligth room.png",        lighting },
            { "link0.png",           linkImages },
            { "link0.atlas",          linkAtlas },
            { "npcs.png",             npcImages },
            { "items.png",           itemImages },
            { "intro.png",           introImage },
            { "intro.atlas",         introAtlas },
            { "minimap.png",         miniMapImg },
            { "objects.png",         objectsImg },
            { "photos.png",          photograph },
            { "ui.png",                uiImages },
            { "musicOverworld.data",  musicTile },
            { "dungeon3_1.map",       dungeon3M },
            { "dungeon3_1.map.data",  dungeon3D },
            { "bridge.map",          newBridgeM },
            { "bridge.map.data",     newBridgeD },
            { "BowWow.ani",          bowwowanim },
            { "mapPlayer.ani",       dungeonani },
            { "boomerang.ani",        boomerang },
            { "explosion0.ani",       effectani },
            { "ShockEffect.fx",      shaderFile }
        };

        // CREATE A REVERSE MAP OF THE DICTIONARY SO IT CAN EASILY BE SEARCHED IN EITHER DIRECTION

        private static readonly Dictionary<string, string> reverseFileTargets = BuildReverseMap();
        private static Dictionary<string, string> BuildReverseMap()
        {
            var reverse = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in fileTargets)
            {
                string shortName = kvp.Key;
                string[] longNames = kvp.Value;
                foreach (string longName in longNames)
                    reverse[longName] = shortName;
            }
            return reverse;
        }

/*-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        PROGRESS CODE : TRACK AND UPDATE THE PROGRESS BAR BY KEEPING TRACK OF THE FILE COUNTS.
       
-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------*/

        private static void ResetProgress()
        {
            // Reset the variables.
            FileCount = 0;
            TotalCount = 0;
            Config.ActiveWindow?.UpdateProgressBar(0);
        }

        private static void UpdateProgress()
        {
            // Update the current progress.
            FileCount++;
            int progress = (int)(FileCount * 100.0 / TotalCount);
            Config.ActiveWindow?.UpdateProgressBar(progress);
        }

/*-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        TEST IF FOLDER IS GAME FOLDER.
       
-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------*/

        private static bool VerifyGameFolder()
        {
            // Get the path to the game executable to verify we're in the correct folder.
            string GameExePath = Config.GetGameExecutable(Config.RootPath);

            // If we're not in the game folder do not try to apply patches.
            if (!GameExePath.TestPath())
                return false;

            // Otherwise apply patches.
            return true;
        }

/*-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        LAHDMODS / MUSIC: COPIES LAHDMODS AND MUSIC FROM ONE FOLDER TO ANOTHER.
       
-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------*/

        private static void CopyModFiles(string sourcePath, string destinationPath)
        {
            // If the source doesn't exist then skip it.
            if (!sourcePath.TestPath())
                return;

            // Create the destination if it doesn't exist.
            destinationPath.CreatePath(false);

            // Loop through the lahdmod files found in the directory.
            foreach (string file in sourcePath.GetFiles("*", true))
            {
                // Get the lahdmod file as a file item.
                FileItem lahdmodFile = new FileItem(file);

                // Use the relative path to the file to get the finalized output path.
                string relative = lahdmodFile.DirectoryName.Replace(sourcePath, "").TrimStart(Path.DirectorySeparatorChar);
                string destination = Path.Combine(destinationPath, relative).CreatePath();

                // Set the path to where the file should be copied.
                string targetPath = Path.Combine(destination, lahdmodFile.Name);

                // Copy the lahdmod file to the destination.
                lahdmodFile.FullName.CopyPath(targetPath, true);
            }
        }


        private static void MergeLanguageFiles(string sourcePath, string destinationPath)
        {
            // If the source doesn't exist then skip it.
            if (!sourcePath.TestPath())
                return;

            // Create the destination if it doesn't exist.
            destinationPath.CreatePath(false);

            // Loop through all language files in the folder.
            foreach (string file in sourcePath.GetFiles("*.lng", true))
            {
                // Get the file as a file item.
                FileItem sourceFile = new FileItem(file);

                // Use the relative path to the file to get the finalized output path.
                string relative = sourceFile.DirectoryName.Replace(sourcePath, "").TrimStart(Path.DirectorySeparatorChar);
                string destination = Path.Combine(destinationPath, relative).CreatePath();

                // Set the path to where the file should be copied.
                string targetPath = Path.Combine(destination, sourceFile.Name);

                // Create a dictionary to store the merged key-value pairs.
                var merged = new Dictionary<string, string>();

                // Load existing destination file into the dictionary.
                if (File.Exists(targetPath))
                {
                    foreach (string line in File.ReadAllLines(targetPath))
                    {
                        // Get the separator index and skip if the line doesn't contain one.
                        int separator = line.IndexOf(' ');
                        if (separator <= 0 || line.TrimStart().StartsWith("//")) continue;

                        // Store the key value pair in the merged dictionary.
                        string key = line[..separator];
                        string value = line[(separator + 1)..];
                        merged[key] = value;
                    }
                }
                // Overlay new mod values, overwriting any matching keys.
                foreach (string line in File.ReadAllLines(sourceFile.FullName))
                {
                    // Get the separator index and skip if the line doesn't contain one.
                    int separator = line.IndexOf(' ');
                    if (separator <= 0 || line.TrimStart().StartsWith("//")) continue;

                    // Store the key value pair in the merged dictionary which overwrites the old value if it exists.
                    string key = line[..separator];
                    string value = line[(separator + 1)..];
                    merged[key] = value;
                }
                // Write the merged result.
                File.WriteAllLines(targetPath, merged.Select(kvp => $"{kvp.Key} {kvp.Value}"));
            }
        }

        private static bool TryGetScriptCondition(string line, out string condition)
        {
            condition = null;

            // Ignore blank lines and full-line comments.
            string trimmed = line.TrimStart();
            if (trimmed.Length == 0 || trimmed.StartsWith("//"))
                return false;

            // The condition is everything before the first "->".
            int arrow = line.IndexOf("->", StringComparison.Ordinal);
            if (arrow < 0)
                return false;

            // Trim so "key:0 ->" and "key:0   ->" resolve to the same condition.
            condition = line.Substring(0, arrow).Trim();
            return condition.Length > 0;
        }

        private static void MergeScriptFiles(string sourcePath, string destinationPath)
        {
            // If the source script doesn't exist then skip it.
            if (!sourcePath.TestPath())
                return;

            // Make sure the destination folder exists.
            string destFolder = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(destFolder))
                destFolder.CreatePath(false);

            // Read the base (destination) lines, preserving comments, blank lines and order.
            var lines = File.Exists(destinationPath)
                ? new List<string>(File.ReadAllLines(destinationPath))
                : new List<string>();

            // Map each existing condition to the line index that defines it.
            // The condition is everything before the first "->" (e.g. "ice_block_show_message:has_rod:0").
            var index = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < lines.Count; i++)
            {
                if (TryGetScriptCondition(lines[i], out string condition))
                    index[condition] = i;
            }
            // Buffer comments / blank lines from the mod so they carry over with any new keys they describe.
            var pending = new List<string>();

            // Overlay the mod's script lines on top of the base.
            foreach (string modLine in File.ReadAllLines(sourcePath))
            {
                // Hold comments and blank lines until we know whether the next key is new.
                if (!TryGetScriptCondition(modLine, out string condition))
                {
                    pending.Add(modLine);
                    continue;
                }
                // If this condition already exists, overwrite it in place and drop the buffered comments.
                if (index.TryGetValue(condition, out int existing))
                {
                    lines[existing] = modLine;
                    pending.Clear();
                    continue;
                }
                foreach (string comment in pending)
                    lines.Add(comment);
                pending.Clear();

                lines.Add(modLine);
                index[condition] = lines.Count - 1;
            }
            // Write the merged result back to the destination.
            File.WriteAllLines(destinationPath, lines);
        }

/*-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        DICTIONARY LOOK UP TABLE: MINIMIZES TIME SPENT IN POINTLESS LOOPS
       
-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------*/

        private static Dictionary<string, string> _fileLookupDictionary;

        private static void BuildFileLookupDictionary()
        {
            _fileLookupDictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // Search the backup path first. We only want to match with ORIGINAL game files.
            if (Config.BackupPath.TestPath())
            {
                foreach (string file in Config.BackupPath.GetFiles("*", true))
                {
                    string name = Path.GetFileName(file);
                    if (!_fileLookupDictionary.ContainsKey(name))
                        _fileLookupDictionary[name] = file;
                }
            }
            // If it's not in the backup path, check the rest of the data folder.
            if (Config.DataPath.TestPath())
            {
                foreach (string file in Config.DataPath.GetFiles("*", true))
                {
                    if (file.IndexOf("Backup", StringComparison.OrdinalIgnoreCase) >= 0)
                        continue;

                    string name = Path.GetFileName(file);
                    if (!_fileLookupDictionary.ContainsKey(name))
                        _fileLookupDictionary[name] = file;
                }
            }
        }

        private static FileItem FindFileToPatch(string fileName)
        {
            string refName = fileName;

            // Find the correct file name that the mod is based off of. 
            if (reverseFileTargets.TryGetValue(fileName, out string target))
                refName = target;

            // Look up where the file is located.
            if (_fileLookupDictionary.TryGetValue(refName, out string fullPath))
                return new FileItem(fullPath);

            // Return null which will skip the file.
            return null;
        }

/*-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        CREATE PATCHES: CODE - CREATES VCDIFF PATCHES FROM MODS FILES.
       
-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------*/

        private static void InstallImageFile()
        {
            // If the user selected an image.
            if (Config.ImagePath != "" && Config.ImagePath.TestPath())
            {
                // Get the path to where it will be copied.
                FileItem imageItem = new FileItem(Config.ImagePath);
                string outImagePath = Path.Combine(Config.OutputPath, "Image" + imageItem.Extension);

                // Copy it to the destination.
                Config.ImagePath.CopyPath(outImagePath, true);
            }
        }

        private static void CreatePatchLoop()
        {
            // Create the necessary output paths.
            Config.TempPath = Path.Combine(Path.GetTempPath(), "LADXHD_ModInstall").CreatePath(true);
            Config.OutGraphicsMods.CreatePath(true);

            // Get all files found in the base folder recursively.
            var fileCollection = Config.GraphicsMods.GetFiles("*", true);

            // Get a count of how many files there are.
            TotalCount = fileCollection.Count;

            // Build a dictionary so that looping isn't insanely slow.
            BuildFileLookupDictionary();

            // Loop through all files in the collection.
            foreach (string file in fileCollection)
            {
                // We don't need a perfect representation of progress just a rough idea of the time left.
                UpdateProgress();

                // Get the modded file as "FileItem" and default original name to modded file name.
                FileItem modFileItem = new FileItem(file);
                string originalName = modFileItem.Name;

                // Use the relative path to the file to get the finalized output path.
                string relative = modFileItem.DirectoryName.Replace(Config.GraphicsMods, "").TrimStart(Path.DirectorySeparatorChar);
                string destination = Path.Combine(Config.OutGraphicsMods, relative).CreatePath();

                // If the file is not an "original" file from v1.0.0 then find the original it was spawned from.
                if (reverseFileTargets.TryGetValue(modFileItem.Name, out string shortName))
                    originalName = shortName;

                // Get the original file as a "FileItem" to create a patch against.
                FileItem originalFileItem = FindFileToPatch(originalName);

                // If the original file doesn't exist then copy the file to the output path.
                if (originalFileItem == null)
                {
                    string copyFileName = Path.Combine(destination, modFileItem.Name);
                    modFileItem.FullName.CopyPath(copyFileName,true);
                    continue;
                }
                // Set the output path to the patch file and create it.
                string outputPatch = Path.Combine(destination, modFileItem.Name + ".vcdiff");
                VCDiff.Execute(Operation.Create, originalFileItem.FullName, modFileItem.FullName, outputPatch);
            }
            // Copy any LAHDMOD or music files found in the mods folder.
            CopyModFiles(Config.AnimationMods, Config.OutAnimationMods);
            CopyModFiles(Config.DungeonMods, Config.OutDungeonMods);
            CopyModFiles(Config.MusicMods, Config.OutMusicMods);
            CopyModFiles(Config.LanguageMods, Config.OutLanguageMods);
            CopyModFiles(Config.MapsMods, Config.OutMapsMods);
            CopyModFiles(Config.SoundsMods, Config.OutSoundsMods);
            CopyModFiles(Config.LAHDModPath, Config.OutLAHDModPath);

            // Copy a "scripts.zScripts" file if it exists.
            if (Config.ZScripts.TestPath())
                Config.ZScripts.CopyPath(Config.OutZScripts, true);

            // Copy a "MapOverlay.data" file if it exists.
            if (Config.MapOverlay.TestPath())
                Config.MapOverlay.CopyPath(Config.OutMapOverlay, true);

            // Try to copy over the image file.
            InstallImageFile();

            // Remove the temporary folder.
            Config.TempPath.RemovePath();
        }

/*-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        CREATE PATCHES: INITIALIZE - STARTS THE PROCESS OF CREATING VCDIFF PATCHES.
       
-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------*/

        public static void CreateModPatches()
        {
            // Reset the progress in case the user runs it more than once.
            ResetProgress();

            // Verify that we're actually in the game folder.
            if (!VerifyGameFolder())
                return;

            // Create the patcher, create the patches, and remove the patcher.
            CreatePatchLoop();

            // Create the INI file.
            string IniPath = Path.Combine(Config.OutputPath, "LAHDMOD.ini");
            LADXHD_IniFile.Initialize(IniPath);
            LADXHD_IniFile.WriteINIValues();

            // Zip everything in the output folder.
            string zipPath = Config.OutputPath + ".zip";
            if (File.Exists(zipPath))
                File.Delete(zipPath);
            ZipFile.CreateFromDirectory(Config.OutputPath, zipPath);

            // Rename the output using the name of the mod.
            string outputPath = Config.OutputPath.Replace("_ModOutput","");
            string outputMod  = Path.Combine(outputPath, Config.ModName.RemoveIllegalCharacters().Replace(" ","_") + ".lahdpak");
            zipPath.RenamePath(outputMod, true);

            // Remove the output path.
            Config.OutputPath.RemovePath();
        }

/*-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        APPLY PATCHES: CODE - APPLIES VCDIFF PATCHES TO CREATE THE MOD.
       
-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------*/

        private static void ApplyPatchLoop()
        {
            // Create the output paths.
            Config.OutputPath.CreatePath(true);

            // Get all patch files found in the patches folder recursively.
            var fileCollection = Config.OutGraphicsMods.GetFiles("*.*", true);

            // Get a count of how many files there are.
            TotalCount = fileCollection.Count;

            // Build a dictionary so that looping isn't insanely slow.
            BuildFileLookupDictionary();

            // Loop through all files in the collection.
            foreach (string file in fileCollection)
            {
                // We don't need a perfect representation of progress just a rough idea of the time left.
                UpdateProgress();

                // Get the patch as a file item which gives us some cool properties to reference.
                FileItem patchFileItem = new FileItem(file);
                FileItem fileToPatchItem = FindFileToPatch(patchFileItem.BaseName);

                // Use the relative path to the file to get the finalized output path.
                string relative = patchFileItem.DirectoryName.Replace(Config.OutGraphicsMods, "").TrimStart(Path.DirectorySeparatorChar);
                string destination = Path.Combine(Config.OutputPath, relative).CreatePath();

                // If the file to patch doesn't exist then copy the file to the output path.
                if (fileToPatchItem == null)
                {
                    string copyFileName = Path.Combine(destination, patchFileItem.Name);
                    patchFileItem.FullName.CopyPath(copyFileName,true);
                    continue;
                }
                // Create the patched file at the output path.
                string patchedFile = Path.Combine(destination, patchFileItem.BaseName);
                VCDiff.Execute(Operation.Apply, fileToPatchItem.FullName, patchFileItem.FullName, patchedFile);
            }
            // Copy any LAHDMOD or music files found in the mods folder.
            CopyModFiles(Config.OutAnimationMods, Config.AnimationMods);
            CopyModFiles(Config.OutDungeonMods, Config.DungeonMods);
            CopyModFiles(Config.OutMusicMods, Config.MusicMods);
            CopyModFiles(Config.OutMapsMods, Config.MapsMods);
            CopyModFiles(Config.OutSoundsMods, Config.SoundsMods);
            CopyModFiles(Config.OutLAHDModPath, Config.LAHDModPath);

            // Language files "scripts.zScript" are merged together so they need their own functions.
            MergeLanguageFiles(Config.OutLanguageMods, Config.LanguageMods);
            MergeScriptFiles(Config.OutZScripts, Config.ZScripts);

            // Copy a "MapOverlay.data" file if it exists.
            if (Config.OutMapOverlay.TestPath())
                Config.OutMapOverlay.CopyPath(Config.MapOverlay, true);
        }

/*-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        APPLY PATCHES: INITIALIZE - STARTS THE PROCESS OF APPLYING VCDIFF PATCHES.
       
-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------*/

        public static void ApplyModPatches()
        {
            // Reset the progress in case the user runs it more than once.
            ResetProgress();

            // Verify that we're actually in the game folder.
            if (!VerifyGameFolder())
                return;

            // Set up the patches and output path.
            Config.UpdateOutputPaths_ApplyPatches();

            // Create the patcher, patch the files, and remove the patcher.
            ApplyPatchLoop();
        }
    }
}
