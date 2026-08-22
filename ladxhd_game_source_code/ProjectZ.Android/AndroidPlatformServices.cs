using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Content.Res;
using Android.Provider;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Input.Touch;
using ProjectZ.InGame.Controls;
using ProjectZ.InGame.Things;
using AndroidEnv = Android.OS.Environment;
using AndroidNet = Android.Net;

namespace ProjectZ.Android
{
    internal sealed class AndroidPlatformFileSystem : IPlatformFileSystem
    {
        private readonly AssetManager _assets;
        private readonly string _installedAssetRoot;

        public AndroidPlatformFileSystem(AssetManager assets, string installedAssetRoot)
        {
            _assets = assets;
            _installedAssetRoot = installedAssetRoot;
        }

        public bool PackagedAssetExists(string path)
        {
            try
            {
                using var stream = OpenPackagedAsset(path);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool PackagedAssetDirectoryExists(string path)
        {
            var external = ResolveInstalledPath(path);
            if (external != null && Directory.Exists(external))
                return true;
            try
            {
                using var stream = _assets.Open(path.Trim('/', '\\'));
                return false;
            }
            catch
            {
                // AssetManager returns an empty array for missing directories, so only a
                // directory containing packaged entries should select the internal mod root.
                return _assets.List(path.Trim('/', '\\')) is { Length: > 0 };
            }
        }

        public Stream OpenPackagedAsset(string path)
        {
            var external = ResolveInstalledPath(path);
            if (external != null && File.Exists(external))
                return File.OpenRead(external);
            return TitleContainer.OpenStream(path.TrimStart('/', '\\'));
        }

        public string[] ListPackagedAssets(string directory)
        {
            var external = ResolveInstalledPath(directory);
            if (external != null && Directory.Exists(external))
                return Directory.EnumerateFileSystemEntries(external)
                    .Select(Path.GetFileName)
                    .Where(name => !string.IsNullOrEmpty(name))
                    .ToArray();
            try
            {
                return _assets.List(directory.Trim('/', '\\')) ?? [];
            }
            catch
            {
                return [];
            }
        }

        private string ResolveInstalledPath(string path)
        {
            if (string.IsNullOrWhiteSpace(_installedAssetRoot))
                return null;
            var relative = path.Replace('\\', '/').Trim('/');
            if (!(relative.Equals("Data", StringComparison.OrdinalIgnoreCase) ||
                  relative.StartsWith("Data/", StringComparison.OrdinalIgnoreCase) ||
                  relative.Equals("Content", StringComparison.OrdinalIgnoreCase) ||
                  relative.StartsWith("Content/", StringComparison.OrdinalIgnoreCase)))
                return null;
            if (relative.Split('/').Any(segment => segment is "" or "." or ".."))
                return null;
            var root = Path.GetFullPath(_installedAssetRoot).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            var resolved = Path.GetFullPath(Path.Combine(_installedAssetRoot, relative.Replace('/', Path.DirectorySeparatorChar)));
            return resolved.StartsWith(root, StringComparison.OrdinalIgnoreCase) ? resolved : null;
        }
    }

    internal sealed class ExternalContentManager : ContentManager
    {
        private readonly string _contentRoot;

        public ExternalContentManager(IServiceProvider serviceProvider, string contentRoot)
            : base(serviceProvider, contentRoot)
        {
            _contentRoot = Path.GetFullPath(contentRoot);
        }

        protected override Stream OpenStream(string assetName)
        {
            var relative = assetName.Replace('\\', '/').Trim('/');
            if (relative.EndsWith(".xnb", StringComparison.OrdinalIgnoreCase))
                relative = relative[..^4];
            if (relative.Split('/').Any(segment => segment is "" or "." or ".."))
                throw new ContentLoadException($"Unsafe content asset name '{assetName}'.");
            var root = _contentRoot.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            var path = Path.GetFullPath(Path.Combine(_contentRoot,
                (relative + ".xnb").Replace('/', Path.DirectorySeparatorChar)));
            if (!path.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                throw new ContentLoadException($"Content asset '{assetName}' escapes the installed content root.");

            using var file = File.OpenRead(path);
            var memory = new MemoryStream(checked((int)file.Length));
            file.CopyTo(memory);
            memory.Position = 0;
            return memory;
        }
    }

    internal sealed class AndroidUserDataPaths : IUserDataPaths
    {
        public bool ShouldCreateModsDirs => Values.ResolvedMods == Values.PathMods;
        public AndroidUserDataPaths(string userDataRoot)
        {
            UserDataRoot = userDataRoot;
        }
        public string UserDataRoot { get; }
        public string ModsRoot => Path.Combine(UserDataRoot, "Mods");
        public string InternalModsRoot => "Mods";
        public string SaveDirectory => Path.Combine(UserDataRoot, "SaveFiles");
        public string SettingsFilePath => Path.Combine(UserDataRoot, "settings");
        public string AdvancedFilePath => Path.Combine(UserDataRoot, "advanced");
        public string AchievementsFilePath => Path.Combine(UserDataRoot, "achievements");
    }

    internal sealed class AndroidSharedSaveService : ISharedSaveService
    {
        private const string ExternalRootName = "LADXHD";
        private const string ExternalSavesSubfolder = "SaveFiles";

        public bool IsSupported => true;

        public bool HasAccess => OperatingSystem.IsAndroidVersionAtLeast(30)
            ? AndroidEnv.IsExternalStorageManager
            : Application.Context.CheckSelfPermission(global::Android.Manifest.Permission.WriteExternalStorage) == Permission.Granted;

        public string SharedRootDirectory => Path.Combine(AndroidEnv.ExternalStorageDirectory.AbsolutePath, ExternalRootName);

        public string SharedSaveDirectory => Path.Combine(AndroidEnv.ExternalStorageDirectory.AbsolutePath, ExternalRootName, ExternalSavesSubfolder);

        public void RequestAccess()
        {
            try
            {
                Intent intent;
                if (OperatingSystem.IsAndroidVersionAtLeast(30))
                {
                    // Deep-link to this app's "All files access" toggle.
                    intent = new Intent(Settings.ActionManageAppAllFilesAccessPermission);
                    intent.SetData(AndroidNet.Uri.Parse("package:" + Application.Context.PackageName));
                }
                else
                {
                    intent = new Intent(Settings.ActionApplicationDetailsSettings);
                    intent.SetData(AndroidNet.Uri.Parse("package:" + Application.Context.PackageName));
                }

                intent.AddFlags(ActivityFlags.NewTask);
                Application.Context.StartActivity(intent);
            }
            catch { }
        }

        public void CopyFile(string sourcePath, string destinationPath)
        {
            // Keep the previous shared save intact until the Java I/O copy completes.
            // Java streams avoid the Samsung/SELinux issue with .NET file APIs here.
            var temporaryPath = destinationPath + ".tmp";
            JavaStreamCopy(sourcePath, temporaryPath);

            var destination = new Java.IO.File(destinationPath);
            if (destination.Exists() && !destination.Delete())
                throw new IOException($"Could not replace shared save '{destinationPath}'.");

            var temporary = new Java.IO.File(temporaryPath);
            if (!temporary.RenameTo(destination))
                throw new IOException($"Could not finalize shared save '{destinationPath}'.");
        }

        private static void JavaStreamCopy(string sourcePath, string destinationPath)
        {
            using var input = new Java.IO.FileInputStream(sourcePath);
            using var output = new Java.IO.FileOutputStream(destinationPath);
            var buffer = new byte[8192];
            int read;
            while ((read = input.Read(buffer)) > 0)
                output.Write(buffer, 0, read);
        }

        public bool FileExists(string path) => File.Exists(path);
        public void DeleteFile(string path) => File.Delete(path);
        public void EnsureDirectory(string path) => Directory.CreateDirectory(path);
        public DateTime GetLastWriteTimeUtc(string path) => File.GetLastWriteTimeUtc(path);
    }

    internal sealed class AndroidPlatformInput : IPlatformInput
    {
        // Button state used to derive held, pressed, and released edges each frame.
        private volatile int _down;
        private volatile int _last;

        // Legacy one-shot Select latch, kept separate from held button state.
        private volatile int _selectPressed;

        // Right-stick state supplied by Android motion events.
        private volatile float _rightStickX;
        private volatile float _rightStickY;

        // Touch state captured from MonoGame at the start of each frame.
        private readonly List<PlatformTouch> _touches = [];

        public bool HasTouchInput => true;
        public IReadOnlyList<PlatformTouch> Touches => _touches;
        public Vector2 RightStick => new Vector2(_rightStickX, _rightStickY);
        public bool IsButtonDown(CButtons button) => (_down & (int)button) != 0;
        public bool WasButtonPressed(CButtons button) => (_down & (int)button) != 0 && (_last & (int)button) == 0;
        public bool WasButtonReleased(CButtons button) => (_down & (int)button) == 0 && (_last & (int)button) != 0;
        public bool ConsumeSelectPressed() => Interlocked.Exchange(ref _selectPressed, 0) != 0;
        public void BeginFrame()
        {
            _last = _down;
            _touches.Clear();
            foreach (var touch in TouchPanel.GetState())
            {
                var state = touch.State switch
                {
                    TouchLocationState.Pressed => PlatformTouchState.Pressed,
                    TouchLocationState.Moved => PlatformTouchState.Moved,
                    TouchLocationState.Released => PlatformTouchState.Released,
                    _ => PlatformTouchState.Invalid
                };
                _touches.Add(new PlatformTouch(touch.Id, touch.Position, state));
            }
        }

        public void SetButton(CButtons button, bool down)
        {
            if (down)
                Interlocked.Or(ref _down, (int)button);
            else
                Interlocked.And(ref _down, ~(int)button);
        }

        public void SetRightStick(float x, float y)
        {
            _rightStickX = x;
            _rightStickY = y;
        }

        public void SetSelectPressed() => Interlocked.Exchange(ref _selectPressed, 1);
    }

    internal sealed class AndroidPlatformWindow : IPlatformWindow
    {
        public bool SupportsFullscreen => true;
        public bool SupportsFullscreenConfiguration => false;
        public bool SupportsInactiveWindowInput => false;
        public bool ForceFullscreen => true;
        public bool VerticalFlipBlur => true;
        public void Initialize(Game game) { }
        public void OnGraphicsDeviceReset(Game game) { }
        public void ApplyPendingChanges(Game game) { }
        public bool TrySetFullscreen(Game game, int screenMode) => false;
        // MainActivity is single-instance, so Game.Exit would leave a disposed game
        // behind for the next launcher intent. The explicit in-game exit must end it.
        public void Exit(Game game) => global::Android.OS.Process.KillProcess(global::Android.OS.Process.MyPid());
    }
}
