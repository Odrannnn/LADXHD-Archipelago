using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using Android.Content;
using Android.Content.Res;
using AndroidNet = Android.Net;
using ProjectZ.InGame.Assets;

namespace ProjectZ.Android
{
    internal sealed class AndroidAssetManifest
    {
        public int FormatVersion { get; set; } = 1;
        public string SourceVersion { get; set; }
        public string AssetVersion { get; set; }
        public int PatchVersion { get; set; }
        public string SourceArchiveSha256 { get; set; }
        public string TreeSha256 { get; set; }
        public int FileCount { get; set; }
        public long TotalBytes { get; set; }
        public string DirectoryName { get; set; }
        public DateTime InstalledUtc { get; set; }
    }

    internal static class AndroidAssetInstallation
    {
        private const string PreferencesName = "asset_installer";
        private const string SourceUriPreference = "source_uri";
        private const string AssetRootName = "GameAssets";
        private const string VersionsName = "versions";
        private const string StagingName = "staging";
        private const string ActiveManifestName = "active.json";
        private const string InstallManifestName = "install.json";
        private const long MaximumSourceArchiveBytes = 64L * 1024 * 1024;
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

        public static string GetUserDataRoot(Context context) =>
            context.GetExternalFilesDir(null)?.AbsolutePath ?? context.FilesDir?.AbsolutePath ??
            throw new InvalidOperationException("Android did not provide an app data directory.");

        public static string GetSavedSourceUri(Context context) =>
            context.GetSharedPreferences(PreferencesName, FileCreationMode.Private)?.GetString(SourceUriPreference, null);

        public static void SaveSourceUri(Context context, AndroidNet.Uri uri)
        {
            context.GetSharedPreferences(PreferencesName, FileCreationMode.Private)
                ?.Edit()?.PutString(SourceUriPreference, uri?.ToString())?.Apply();
        }

        public static bool TryGetActiveRoot(Context context, out string activeRoot, out string reason) =>
            TryGetActiveRoot(GetUserDataRoot(context), out activeRoot, out reason);

        public static bool TryGetActiveRoot(string userDataRoot, out string activeRoot, out string reason)
        {
            activeRoot = null;
            reason = null;
            try
            {
                var assetRoot = Path.Combine(userDataRoot, AssetRootName);
                var versionsRoot = Path.Combine(assetRoot, VersionsName);
                var activePath = Path.Combine(assetRoot, ActiveManifestName);
                if (!File.Exists(activePath))
                {
                    if (TryRecoverPreviousVersion(assetRoot, versionsRoot, out activeRoot))
                        return true;
                    reason = "The Android game data has not been installed yet.";
                    return false;
                }

                var manifest = JsonSerializer.Deserialize<AndroidAssetManifest>(File.ReadAllText(activePath), JsonOptions);
                if (!IsCompatible(manifest))
                {
                    if (TryRecoverPreviousVersion(assetRoot, versionsRoot, out activeRoot))
                        return true;
                    reason = "The installed game data belongs to an older app version and needs to be rebuilt.";
                    return false;
                }

                if (!TryResolveManifest(versionsRoot, manifest, out var candidate))
                {
                    if (TryRecoverPreviousVersion(assetRoot, versionsRoot, out activeRoot, manifest.DirectoryName))
                        return true;
                    reason = "The installed game data is incomplete and needs to be rebuilt.";
                    return false;
                }

                activeRoot = candidate;
                return true;
            }
            catch (Exception exception)
            {
                reason = "The installed game data could not be read: " + exception.Message;
                return false;
            }
        }

        public static AndroidAssetManifest Install(
            Context context,
            AndroidNet.Uri sourceUri,
            IProgress<GameAssetMigrationProgress> progress,
            CancellationToken cancellationToken)
        {
            if (sourceUri == null)
                throw new ArgumentNullException(nameof(sourceUri));

            var userDataRoot = GetUserDataRoot(context);
            var assetRoot = Path.Combine(userDataRoot, AssetRootName);
            var versionsRoot = Path.Combine(assetRoot, VersionsName);
            var stagingRoot = Path.Combine(assetRoot, StagingName);
            Directory.CreateDirectory(versionsRoot);
            Directory.CreateDirectory(stagingRoot);
            RemoveStaleStaging(stagingRoot);

            var operationName = Guid.NewGuid().ToString("N");
            var operationRoot = Path.Combine(stagingRoot, operationName);
            var outputRoot = Path.Combine(operationRoot, "game");
            var bootstrapRoot = Path.Combine(operationRoot, "bootstrap");
            var sourceArchive = Path.Combine(operationRoot, "source.zip");
            Directory.CreateDirectory(operationRoot);

            try
            {
                progress?.Report(new GameAssetMigrationProgress("Copying the selected v1.0.0 ZIP", 0, 1));
                CopyContentUri(context, sourceUri, sourceArchive, cancellationToken);
                CopyBootstrap(context.Assets, bootstrapRoot, cancellationToken);

                var result = GameAssetMigrator.Migrate(
                    sourceArchive, outputRoot, bootstrapRoot, progress, cancellationToken);
                BackupUserData(userDataRoot, progress, cancellationToken);

                var directoryName = $"{GameAssetMigrator.AssetVersion}-{result.TreeSha256[..12].ToLowerInvariant()}-{operationName[..8]}";
                var manifest = new AndroidAssetManifest
                {
                    SourceVersion = GameAssetMigrator.SourceVersion,
                    AssetVersion = GameAssetMigrator.AssetVersion,
                    PatchVersion = GameAssetMigrator.PatchVersion,
                    SourceArchiveSha256 = result.SourceArchiveSha256,
                    TreeSha256 = result.TreeSha256,
                    FileCount = result.FileCount,
                    TotalBytes = result.TotalBytes,
                    DirectoryName = directoryName,
                    InstalledUtc = DateTime.UtcNow
                };

                File.WriteAllText(Path.Combine(outputRoot, InstallManifestName),
                    JsonSerializer.Serialize(manifest, JsonOptions));
                var finalRoot = GetSafeChild(versionsRoot, directoryName);
                Directory.Move(outputRoot, finalRoot);
                WriteAtomic(Path.Combine(assetRoot, ActiveManifestName), JsonSerializer.Serialize(manifest, JsonOptions));
                SaveSourceUri(context, sourceUri);
                PruneOldVersions(versionsRoot, directoryName);
                progress?.Report(new GameAssetMigrationProgress("Installation complete", 1, 1));
                return manifest;
            }
            finally
            {
                TryDeleteDirectory(operationRoot);
            }
        }

        private static bool IsCompatible(AndroidAssetManifest manifest) =>
            manifest != null &&
            manifest.FormatVersion == 1 &&
            manifest.SourceVersion == GameAssetMigrator.SourceVersion &&
            manifest.AssetVersion == GameAssetMigrator.AssetVersion &&
            manifest.PatchVersion == GameAssetMigrator.PatchVersion &&
            string.Equals(manifest.SourceArchiveSha256, GameAssetMigrator.ExpectedSourceArchiveSha256, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(manifest.TreeSha256, GameAssetMigrator.ExpectedTreeSha256, StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(manifest.DirectoryName);

        private static bool TryResolveManifest(
            string versionsRoot,
            AndroidAssetManifest manifest,
            out string versionRoot)
        {
            versionRoot = null;
            if (!IsCompatible(manifest))
                return false;
            var candidate = GetSafeChild(versionsRoot, manifest.DirectoryName);
            var installManifestPath = Path.Combine(candidate, InstallManifestName);
            if (!File.Exists(installManifestPath) || !HasRequiredFiles(candidate))
                return false;
            var installed = JsonSerializer.Deserialize<AndroidAssetManifest>(
                File.ReadAllText(installManifestPath), JsonOptions);
            if (!IsCompatible(installed) || installed.DirectoryName != manifest.DirectoryName ||
                !string.Equals(installed.TreeSha256, manifest.TreeSha256, StringComparison.OrdinalIgnoreCase))
                return false;
            versionRoot = candidate;
            return true;
        }

        private static bool TryRecoverPreviousVersion(
            string assetRoot,
            string versionsRoot,
            out string recoveredRoot,
            string excludedDirectoryName = null)
        {
            recoveredRoot = null;
            if (!Directory.Exists(versionsRoot))
                return false;
            foreach (var directory in Directory.EnumerateDirectories(versionsRoot)
                         .OrderByDescending(Directory.GetLastWriteTimeUtc))
            {
                var directoryName = Path.GetFileName(directory);
                if (directoryName.Equals(excludedDirectoryName, StringComparison.OrdinalIgnoreCase))
                    continue;
                try
                {
                    var path = Path.Combine(directory, InstallManifestName);
                    if (!File.Exists(path))
                        continue;
                    var manifest = JsonSerializer.Deserialize<AndroidAssetManifest>(File.ReadAllText(path), JsonOptions);
                    if (manifest?.DirectoryName != directoryName ||
                        !TryResolveManifest(versionsRoot, manifest, out var candidate))
                        continue;
                    WriteAtomic(Path.Combine(assetRoot, ActiveManifestName), JsonSerializer.Serialize(manifest, JsonOptions));
                    recoveredRoot = candidate;
                    return true;
                }
                catch
                {
                    // Continue through older verified installations.
                }
            }
            return false;
        }

        private static bool HasRequiredFiles(string root)
        {
            foreach (var relative in new[]
                     {
                         "Content/Fonts/smallFont.xnb", "Content/Shader/EffectBlur.xnb",
                         "Data/scripts.zScript", "Data/Maps/overworld.map", "Data/Music/awakening.gbs",
                         "Data/Buttons/buttons.atlas", "Data/Buttons/buttons.png"
                     })
            {
                var file = Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));
                if (!File.Exists(file) || new FileInfo(file).Length == 0)
                    return false;
            }
            return true;
        }

        private static void CopyContentUri(Context context, AndroidNet.Uri uri, string destination, CancellationToken token)
        {
            using var input = context.ContentResolver?.OpenInputStream(uri) ??
                              throw new FileNotFoundException("Android could not open the selected file.");
            using var output = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            var buffer = new byte[128 * 1024];
            long totalBytes = 0;
            int read;
            while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                token.ThrowIfCancellationRequested();
                totalBytes = checked(totalBytes + read);
                if (totalBytes > MaximumSourceArchiveBytes)
                    throw new InvalidDataException("The selected ZIP is larger than the supported v1.0.0 archive limit.");
                output.Write(buffer, 0, read);
            }
        }

        private static void CopyBootstrap(AssetManager assets, string destinationRoot, CancellationToken token)
        {
            Directory.CreateDirectory(destinationRoot);
            foreach (var name in new[] { "patches_android.zip", "d3map", "d3mapdata", "android_buttons.zip" })
            {
                token.ThrowIfCancellationRequested();
                using var input = assets.Open("Bootstrap/" + name);
                using var output = new FileStream(Path.Combine(destinationRoot, name), FileMode.CreateNew, FileAccess.Write, FileShare.None);
                CopyStream(input, output, token);
            }
        }

        private static void CopyStream(Stream input, Stream output, CancellationToken token)
        {
            var buffer = new byte[128 * 1024];
            int read;
            while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                token.ThrowIfCancellationRequested();
                output.Write(buffer, 0, read);
            }
        }

        private static void BackupUserData(string userDataRoot, IProgress<GameAssetMigrationProgress> progress, CancellationToken token)
        {
            var sources = new[] { "SaveFiles", "Archipelago" }
                .Select(name => (Name: name, Path: Path.Combine(userDataRoot, name)))
                .Where(item => Directory.Exists(item.Path))
                .ToArray();
            if (sources.Length == 0)
                return;

            progress?.Report(new GameAssetMigrationProgress("Backing up saves and Archipelago profiles", 0, sources.Length));
            var backupRoot = Path.Combine(userDataRoot, "UpdateBackups", DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff"));
            Directory.CreateDirectory(backupRoot);
            for (var i = 0; i < sources.Length; i++)
            {
                token.ThrowIfCancellationRequested();
                CopyDirectory(sources[i].Path, Path.Combine(backupRoot, sources[i].Name), token);
                progress?.Report(new GameAssetMigrationProgress("Backing up saves and Archipelago profiles", i + 1, sources.Length));
            }
            File.WriteAllText(Path.Combine(backupRoot, "backup.json"), JsonSerializer.Serialize(new
            {
                format_version = 1,
                created_utc = DateTime.UtcNow,
                asset_version = GameAssetMigrator.AssetVersion,
                folders = sources.Select(source => source.Name).ToArray()
            }, JsonOptions));
        }

        private static void CopyDirectory(string source, string destination, CancellationToken token)
        {
            Directory.CreateDirectory(destination);
            foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.TopDirectoryOnly))
            {
                token.ThrowIfCancellationRequested();
                File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), overwrite: false);
            }
            foreach (var directory in Directory.EnumerateDirectories(source, "*", SearchOption.TopDirectoryOnly))
            {
                token.ThrowIfCancellationRequested();
                CopyDirectory(directory, Path.Combine(destination, Path.GetFileName(directory)), token);
            }
        }

        private static void WriteAtomic(string destination, string contents)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            var temporary = destination + ".tmp";
            File.WriteAllText(temporary, contents);
            File.Move(temporary, destination, overwrite: true);
        }

        private static string GetSafeChild(string parent, string child)
        {
            if (string.IsNullOrWhiteSpace(child) || child.IndexOfAny(new[] { '/', '\\', ':' }) >= 0 || child is "." or "..")
                throw new InvalidDataException("The installed game-data manifest contains an unsafe directory name.");
            var fullParent = Path.GetFullPath(parent).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            var fullChild = Path.GetFullPath(Path.Combine(parent, child));
            if (!fullChild.StartsWith(fullParent, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("The installed game-data manifest escapes its storage directory.");
            return fullChild;
        }

        private static void RemoveStaleStaging(string stagingRoot)
        {
            foreach (var directory in Directory.EnumerateDirectories(stagingRoot))
                TryDeleteDirectory(directory);
        }

        private static void PruneOldVersions(string versionsRoot, string activeDirectoryName)
        {
            var oldVersions = Directory.EnumerateDirectories(versionsRoot)
                .Where(path => !Path.GetFileName(path).Equals(activeDirectoryName, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(Directory.GetLastWriteTimeUtc)
                .Skip(1);
            foreach (var oldVersion in oldVersions)
                TryDeleteDirectory(oldVersion);
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
                // Staging and inactive versions can be retried during a later setup run.
            }
        }
    }
}
