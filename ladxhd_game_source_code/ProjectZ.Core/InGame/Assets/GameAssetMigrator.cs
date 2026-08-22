using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using VCDiff.Decoders;
using VCDiff.Includes;

namespace ProjectZ.InGame.Assets
{
    public sealed class GameAssetMigrationProgress
    {
        public GameAssetMigrationProgress(string stage, int completed, int total)
        {
            Stage = stage;
            Completed = completed;
            Total = total;
        }

        public string Stage { get; }
        public int Completed { get; }
        public int Total { get; }
    }

    public sealed class GameAssetMigrationResult
    {
        public string SourceArchiveSha256 { get; init; }
        public string TreeSha256 { get; init; }
        public int FileCount { get; init; }
        public long TotalBytes { get; init; }
    }

    public static class GameAssetMigrator
    {
        public const string SourceVersion = "1.0.0";
        public const string AssetVersion = "2.0.5-ap1";
        public const int PatchVersion = 1;
        public const string ExpectedSourceArchiveSha256 =
            "118A4ADFA782B4C0097867609CB79474ABAF9A95B3F684B04715A46D424BEB1C";

        // Filled from a complete, independently verified migration. Leaving this empty while
        // developing permits the migration smoke test to report the canonical digest.
        public const string ExpectedTreeSha256 = "D1150E5ADCA23A4D0DCC8A2A470630D12C45EC3D1838ADADA6EC7B0C1A3E3900";

        private const int MaximumArchiveEntries = 4096;
        private const long MaximumExpandedBytes = 1024L * 1024 * 1024;

        private static readonly string[] LanguageFiles =
            ["chn.lng", "deu.lng", "esp.lng", "fre.lng", "ind.lng", "ita.lng", "por.lng", "pte.lng", "rus.lng", "swe.lng"];
        private static readonly string[] LanguageAchievements =
            ["achieve_chn.lng", "achieve_deu.lng", "achieve_eng.lng", "achieve_esp.lng", "achieve_fre.lng", "achieve_ind.lng", "achieve_ita.lng", "achieve_por.lng", "achieve_pte.lng", "achieve_rus.lng", "achieve_swe.lng"];
        private static readonly string[] LanguageDialogs =
            ["dialog_chn.lng", "dialog_deu.lng", "dialog_esp.lng", "dialog_fre.lng", "dialog_ind.lng", "dialog_ita.lng", "dialog_por.lng", "dialog_pte.lng", "dialog_rus.lng", "dialog_swe.lng"];
        private static readonly string[] SmallFonts =
            ["smallFont_redux.xnb", "smallFont_vwf.xnb", "smallFont_vwf_redux.xnb", "smallFont_chn.xnb", "smallFont_chn_0.xnb", "smallFont_chn_redux.xnb", "smallFont_chn_redux_0.xnb"];
        private static readonly string[] Backgrounds = ["menuBackgroundB.xnb", "menuBackgroundC.xnb", "sgb_border.xnb"];
        private static readonly string[] Lighting = ["mamuLight.xnb"];
        private static readonly string[] LinkImages = ["link1.png", "weapons.png"];
        private static readonly string[] LinkAtlases = ["weapons.atlas"];
        private static readonly string[] NpcImages = ["npcs_redux.png"];
        private static readonly string[] ItemImages =
        [
            "items_chn.png", "items_deu.png", "items_esp.png", "items_fre.png", "items_ind.png", "items_ita.png", "items_por.png", "items_rus.png", "items_swe.png", "items_redux.png",
            "items_redux_chn.png", "items_redux_deu.png", "items_redux_esp.png", "items_redux_fre.png", "items_redux_ind.png", "items_redux_ita.png", "items_redux_por.png", "items_redux_rus.png", "items_redux_swe.png"
        ];
        private static readonly string[] IntroImages = ["intro_chn.png", "intro_deu.png", "intro_esp.png", "intro_fre.png", "intro_ind.png", "intro_ita.png", "intro_por.png", "intro_rus.png", "intro_swe.png"];
        private static readonly string[] IntroAtlases = ["intro_chn.atlas"];
        private static readonly string[] MinimapImages = ["minimap_chn.png", "minimap_deu.png", "minimap_esp.png", "minimap_fre.png", "minimap_ind.png", "minimap_ita.png", "minimap_por.png", "minimap_rus.png", "minimap_swe.png"];
        private static readonly string[] ObjectImages = ["objects_chn.png", "objects_deu.png", "objects_esp.png", "objects_fre.png", "objects_ind.png", "objects_ita.png", "objects_por.png", "objects_rus.png", "objects_swe.png"];
        private static readonly string[] Photographs =
        [
            "photos_chn.png", "photos_deu.png", "photos_esp.png", "photos_fre.png", "photos_ind.png", "photos_ita.png", "photos_por.png", "photos_rus.png", "photos_swe.png", "photos_redux.png",
            "photos_redux_chn.png", "photos_redux_deu.png", "photos_redux_esp.png", "photos_redux_fre.png", "photos_redux_ind.png", "photos_redux_ita.png", "photos_redux_por.png", "photos_redux_rus.png", "photos_redux_swe.png"
        ];
        private static readonly string[] UiImages = ["ui_chn.png", "ui_deu.png", "ui_esp.png", "ui_fre.png", "ui_ind.png", "ui_ita.png", "ui_por.png", "ui_rus.png", "ui_swe.png"];
        private static readonly string[] MusicTiles = ["musicOverworldClassic.data"];
        private static readonly string[] Dungeon3Maps = ["dungeon3.map"];
        private static readonly string[] Dungeon3Data = ["dungeon3.map.data"];
        private static readonly string[] BridgeMaps = ["bridge_l_castle.map", "bridge_r_castle.map"];
        private static readonly string[] BridgeData = ["bridge_l_castle.map.data", "bridge_r_castle.map.data"];
        private static readonly string[] BowWowAnimations = ["bowwow_water.ani"];
        private static readonly string[] DungeonAnimations = ["mapDungeon.ani", "mapManboPond.ani", "mapTeleporter.ani"];
        private static readonly string[] BoomerangAnimations = ["boomerangOrig.ani"];
        private static readonly string[] EffectAnimations = ["bushExplosion.ani", "fireballDeath.ani"];
        private static readonly string[] ShaderFiles = ["GBCColorCorrection.xnb", "PixelGrid.xnb"];

        private static readonly IReadOnlyDictionary<string, string[]> FileTargets =
            new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                ["eng.lng"] = LanguageFiles.Concat(LanguageAchievements).ToArray(),
                ["dialog_eng.lng"] = LanguageDialogs,
                ["smallFont.xnb"] = SmallFonts,
                ["menuBackground.xnb"] = Backgrounds,
                ["ligth room.xnb"] = Lighting,
                ["link0.png"] = LinkImages,
                ["link0.atlas"] = LinkAtlases,
                ["npcs.png"] = NpcImages,
                ["items.png"] = ItemImages,
                ["intro.png"] = IntroImages,
                ["intro.atlas"] = IntroAtlases,
                ["minimap.png"] = MinimapImages,
                ["objects.png"] = ObjectImages,
                ["photos.png"] = Photographs,
                ["ui.png"] = UiImages,
                ["musicOverworld.data"] = MusicTiles,
                ["dungeon3_1.map"] = Dungeon3Maps,
                ["dungeon3_1.map.data"] = Dungeon3Data,
                ["bridge.map"] = BridgeMaps,
                ["bridge.map.data"] = BridgeData,
                ["BowWow.ani"] = BowWowAnimations,
                ["mapPlayer.ani"] = DungeonAnimations,
                ["boomerang.ani"] = BoomerangAnimations,
                ["explosion0.ani"] = EffectAnimations,
                ["ShockEffect.xnb"] = ShaderFiles
            };

        private static readonly HashSet<string> TargetFiles =
            FileTargets.Values.SelectMany(value => value).ToHashSet(StringComparer.OrdinalIgnoreCase);

        public static GameAssetMigrationResult Migrate(
            string sourceArchivePath,
            string outputRoot,
            string bootstrapRoot,
            IProgress<GameAssetMigrationProgress> progress = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(sourceArchivePath) || !File.Exists(sourceArchivePath))
                throw new FileNotFoundException("The selected v1.0.0 ZIP could not be opened.", sourceArchivePath);
            if (string.IsNullOrWhiteSpace(bootstrapRoot) || !Directory.Exists(bootstrapRoot))
                throw new DirectoryNotFoundException("The Android migration resources are missing.");

            var sourceHash = ComputeFileSha256(sourceArchivePath, cancellationToken);
            if (!sourceHash.Equals(ExpectedSourceArchiveSha256, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException(
                    "This is not the supported untouched Links Awakening DX HD v1.0.0 ZIP. " +
                    $"Expected SHA-256 {ExpectedSourceArchiveSha256}, received {sourceHash}.");

            var parent = Path.GetDirectoryName(Path.GetFullPath(outputRoot)) ??
                         throw new InvalidOperationException("The output directory has no parent.");
            if (Directory.Exists(outputRoot))
                throw new IOException("The migration output directory already exists; a fresh staging directory is required.");
            var sourceRoot = Path.Combine(parent, $"source-{Guid.NewGuid():N}");
            Directory.CreateDirectory(sourceRoot);
            Directory.CreateDirectory(outputRoot);

            try
            {
                ExtractOriginalAssets(sourceArchivePath, sourceRoot, progress, cancellationToken);
                RestoreDungeon3Sources(sourceRoot, bootstrapRoot);
                PatchAssets(sourceRoot, outputRoot, Path.Combine(bootstrapRoot, "patches_android.zip"), progress, cancellationToken);
                ExtractSupplementalZip(Path.Combine(bootstrapRoot, "android_buttons.zip"), Path.Combine(outputRoot, "Data", "Buttons"), cancellationToken);
                RemoveObsoleteFiles(outputRoot);
                ValidateRequiredFiles(outputRoot);

                progress?.Report(new GameAssetMigrationProgress("Verifying installed game data", 0, 1));
                var result = ComputeTreeDigest(outputRoot, cancellationToken);
                if (!string.IsNullOrWhiteSpace(ExpectedTreeSha256) &&
                    !result.TreeSha256.Equals(ExpectedTreeSha256, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException(
                        "The locally generated game data did not match the expected release. " +
                        $"Expected {ExpectedTreeSha256}, received {result.TreeSha256}.");

                progress?.Report(new GameAssetMigrationProgress("Game data is ready", 1, 1));
                return new GameAssetMigrationResult
                {
                    SourceArchiveSha256 = sourceHash,
                    TreeSha256 = result.TreeSha256,
                    FileCount = result.FileCount,
                    TotalBytes = result.TotalBytes
                };
            }
            catch
            {
                TryDeleteDirectory(outputRoot);
                throw;
            }
            finally
            {
                TryDeleteDirectory(sourceRoot);
            }
        }

        public static GameAssetMigrationResult ComputeTreeDigest(string root, CancellationToken cancellationToken = default)
        {
            var files = Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
                .Select(path => new
                {
                    FullPath = path,
                    RelativePath = Path.GetRelativePath(root, path).Replace('\\', '/')
                })
                .OrderBy(file => file.RelativePath, StringComparer.Ordinal)
                .ToArray();

            using var aggregate = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            long totalBytes = 0;
            foreach (var file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var info = new FileInfo(file.FullPath);
                totalBytes += info.Length;
                AppendUtf8(aggregate, file.RelativePath);
                AppendUtf8(aggregate, "\n");
                AppendUtf8(aggregate, info.Length.ToString(System.Globalization.CultureInfo.InvariantCulture));
                AppendUtf8(aggregate, "\n");
                using var stream = File.OpenRead(file.FullPath);
                var fileHash = SHA256.HashData(stream);
                aggregate.AppendData(fileHash);
                AppendUtf8(aggregate, "\n");
            }

            return new GameAssetMigrationResult
            {
                TreeSha256 = Convert.ToHexString(aggregate.GetHashAndReset()),
                FileCount = files.Length,
                TotalBytes = totalBytes
            };
        }

        private static void ExtractOriginalAssets(
            string archivePath,
            string sourceRoot,
            IProgress<GameAssetMigrationProgress> progress,
            CancellationToken cancellationToken)
        {
            using var archive = ZipFile.OpenRead(archivePath);
            if (archive.Entries.Count > MaximumArchiveEntries)
                throw new InvalidDataException("The selected ZIP contains an unexpected number of entries.");

            var selected = new List<(ZipArchiveEntry Entry, string RelativePath)>();
            long expandedBytes = 0;
            foreach (var entry in archive.Entries)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var relative = GetGameAssetRelativePath(entry.FullName);
                if (relative == null || string.IsNullOrEmpty(entry.Name))
                    continue;
                if (ContainsPathSegment(relative, "Mods"))
                    continue;

                expandedBytes = checked(expandedBytes + entry.Length);
                if (expandedBytes > MaximumExpandedBytes)
                    throw new InvalidDataException("The selected ZIP expands beyond the supported size limit.");
                selected.Add((entry, relative));
            }

            if (!selected.Any(item => item.RelativePath.Equals("Data/scripts.zScript", StringComparison.OrdinalIgnoreCase)) ||
                !selected.Any(item => item.RelativePath.Equals("Content/Fonts/smallFont.xnb", StringComparison.OrdinalIgnoreCase)))
                throw new InvalidDataException("The selected ZIP does not contain the expected v1.0.0 Content and Data folders.");

            for (var index = 0; index < selected.Count; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                progress?.Report(new GameAssetMigrationProgress("Extracting original game data", index, selected.Count));
                var destination = GetSafeDestination(sourceRoot, selected[index].RelativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                using var input = selected[index].Entry.Open();
                using var output = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                input.CopyTo(output);
            }
        }

        private static void PatchAssets(
            string sourceRoot,
            string outputRoot,
            string patchArchivePath,
            IProgress<GameAssetMigrationProgress> progress,
            CancellationToken cancellationToken)
        {
            if (!File.Exists(patchArchivePath))
                throw new FileNotFoundException("The bundled Android patch set is missing.", patchArchivePath);

            using var patches = ZipFile.OpenRead(patchArchivePath);
            var patchEntries = patches.Entries
                .Where(entry => !string.IsNullOrEmpty(entry.Name))
                .ToDictionary(entry => entry.Name, StringComparer.OrdinalIgnoreCase);

            var sourceFiles = Directory.EnumerateFiles(sourceRoot, "*", SearchOption.AllDirectories)
                .OrderBy(path => Path.GetRelativePath(sourceRoot, path), StringComparer.Ordinal)
                .ToArray();

            for (var index = 0; index < sourceFiles.Length; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                progress?.Report(new GameAssetMigrationProgress("Applying Android game-data patches", index, sourceFiles.Length));
                var sourceFile = sourceFiles[index];
                var fileName = Path.GetFileName(sourceFile);
                if (TargetFiles.Contains(fileName))
                    continue;

                var relative = Path.GetRelativePath(sourceRoot, sourceFile);
                var destination = GetSafeDestination(outputRoot, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);

                if (patchEntries.TryGetValue(fileName + ".vcdiff", out var directPatch))
                    ApplyPatch(sourceFile, directPatch, destination, cancellationToken);
                else
                    File.Copy(sourceFile, destination, overwrite: false);

                if (!FileTargets.TryGetValue(fileName, out var targets))
                    continue;
                foreach (var target in targets)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (!patchEntries.TryGetValue(target + ".vcdiff", out var targetPatch))
                        continue;
                    var targetPath = Path.Combine(Path.GetDirectoryName(destination)!, target);
                    ApplyPatch(sourceFile, targetPatch, targetPath, cancellationToken);
                }
            }
        }

        private static void ApplyPatch(
            string sourcePath,
            ZipArchiveEntry patchEntry,
            string destinationPath,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
            var temporary = destinationPath + $".tmp-{Guid.NewGuid():N}";
            try
            {
                using var source = File.OpenRead(sourcePath);
                using var compressedPatch = patchEntry.Open();
                using var patch = new MemoryStream(capacity: checked((int)patchEntry.Length));
                compressedPatch.CopyTo(patch);
                patch.Position = 0;
                using (var output = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    var decoder = new VcDecoder(source, patch, output);
                    var result = decoder.Decode(out _);
                    if (result != VCDiffResult.SUCCESS)
                        throw new InvalidDataException($"VCDIFF failed for '{patchEntry.Name}' with result {result}.");
                }
                File.Move(temporary, destinationPath, overwrite: true);
            }
            finally
            {
                if (File.Exists(temporary))
                    File.Delete(temporary);
            }
        }

        private static void RestoreDungeon3Sources(string sourceRoot, string bootstrapRoot)
        {
            var maps = Path.Combine(sourceRoot, "Data", "Maps");
            Directory.CreateDirectory(maps);
            foreach (var name in new[] { "dungeon3.map", "dungeon3_1.map", "dungeon3.map.data", "dungeon3_1.map.data" })
            {
                var path = Path.Combine(maps, name);
                if (File.Exists(path))
                    File.Delete(path);
            }

            File.Copy(Path.Combine(bootstrapRoot, "d3map"), Path.Combine(maps, "dungeon3_1.map"));
            File.Copy(Path.Combine(bootstrapRoot, "d3mapdata"), Path.Combine(maps, "dungeon3_1.map.data"));
        }

        private static void ExtractSupplementalZip(string archivePath, string destinationRoot, CancellationToken cancellationToken)
        {
            using var archive = ZipFile.OpenRead(archivePath);
            foreach (var entry in archive.Entries)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (string.IsNullOrEmpty(entry.Name))
                    continue;
                var relative = ValidateRelativePath(entry.FullName);
                var destination = GetSafeDestination(destinationRoot, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                using var input = entry.Open();
                using var output = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None);
                input.CopyTo(output);
            }
        }

        private static void RemoveObsoleteFiles(string outputRoot)
        {
            var data = Path.Combine(outputRoot, "Data");
            var maps = Path.Combine(data, "Maps");
            foreach (var name in new[] { "three_1.txt", "three_2.txt", "three_3.txt" })
                TryDeleteFile(Path.Combine(data, "Dungeon", name));
            foreach (var name in new[]
                     {
                         "cave bird.map.data", "dungeon 7_2d.map.data", "dungeon_end.map.data",
                         "dungeon3_1.map", "dungeon3_1.map.data", "dungeon3_2.map", "dungeon3_2.map.data",
                         "dungeon3_3.map", "dungeon3_3.map.data", "dungeon3_4.map", "dungeon3_4.map.data"
                     })
                TryDeleteFile(Path.Combine(maps, name));
            if (Directory.Exists(maps))
                foreach (var file in Directory.EnumerateFiles(maps, "0 test map*", SearchOption.TopDirectoryOnly))
                    TryDeleteFile(file);
        }

        private static void ValidateRequiredFiles(string outputRoot)
        {
            var required = new[]
            {
                "Content/Fonts/smallFont.xnb",
                "Content/Shader/EffectBlur.xnb",
                "Data/scripts.zScript",
                "Data/Maps/overworld.map",
                "Data/Music/awakening.gbs",
                "Data/Buttons/buttons.atlas",
                "Data/Buttons/buttons.png"
            };
            foreach (var relative in required)
            {
                var path = GetSafeDestination(outputRoot, relative);
                if (!File.Exists(path) || new FileInfo(path).Length == 0)
                    throw new InvalidDataException($"The generated game data is missing required file '{relative}'.");
            }
        }

        private static string GetGameAssetRelativePath(string archiveEntry)
        {
            var normalized = archiveEntry.Replace('\\', '/').TrimStart('/');
            var content = FindRoot(normalized, "Content");
            if (content != null)
                return ValidateRelativePath(content);
            var data = FindRoot(normalized, "Data");
            return data == null ? null : ValidateRelativePath(data);
        }

        private static string FindRoot(string path, string rootName)
        {
            if (path.StartsWith(rootName + "/", StringComparison.OrdinalIgnoreCase))
                return path;
            var marker = "/" + rootName + "/";
            var index = path.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            return index < 0 ? null : path[(index + 1)..];
        }

        private static string ValidateRelativePath(string path)
        {
            var normalized = path.Replace('\\', '/').Trim('/');
            if (string.IsNullOrWhiteSpace(normalized) || normalized.IndexOf('\0') >= 0 ||
                Path.IsPathRooted(normalized) || normalized.Contains(':'))
                throw new InvalidDataException($"Unsafe archive path '{path}'.");
            var segments = normalized.Split('/');
            if (segments.Any(segment => string.IsNullOrEmpty(segment) || segment == "." || segment == ".."))
                throw new InvalidDataException($"Unsafe archive path '{path}'.");
            return string.Join('/', segments);
        }

        private static string GetSafeDestination(string root, string relativePath)
        {
            relativePath = ValidateRelativePath(relativePath);
            var fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            var destination = Path.GetFullPath(Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar)));
            if (!destination.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException($"Path '{relativePath}' escapes the game-data directory.");
            return destination;
        }

        private static bool ContainsPathSegment(string path, string segment) =>
            path.Replace('\\', '/').Split('/').Any(value => value.Equals(segment, StringComparison.OrdinalIgnoreCase));

        private static string ComputeFileSha256(string path, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var stream = File.OpenRead(path);
            return Convert.ToHexString(SHA256.HashData(stream));
        }

        private static void AppendUtf8(IncrementalHash hash, string value) =>
            hash.AppendData(Encoding.UTF8.GetBytes(value));

        private static void TryDeleteFile(string path)
        {
            if (File.Exists(path))
                File.Delete(path);
        }

        private static void TryDeleteDirectory(string path)
        {
            try
            {
                if (Directory.Exists(path))
                    Directory.Delete(path, recursive: true);
            }
            catch
            {
                // A staging folder is never activated until verification succeeds. A later setup
                // run can safely remove remnants that the OS kept locked during cancellation.
            }
        }
    }
}
