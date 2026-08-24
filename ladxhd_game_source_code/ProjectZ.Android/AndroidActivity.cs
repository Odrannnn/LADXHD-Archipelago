using System;
using System.IO;
using Android.App;
using Android.Content.PM;
using Android.Content.Res;
using Android.OS;
using Android.Views;
using Android.Widget;
using ProjectZ.InGame.Controls;
using Microsoft.Xna.Framework;
using MonoGame.Extended.Content.ContentReaders;

namespace ProjectZ.Android
{
    [Activity(
        Label = "@string/app_name",
        MainLauncher = false ,
        Theme = "@style/Theme.Game",
        LaunchMode = LaunchMode.SingleInstance,
        ScreenOrientation = ScreenOrientation.FullSensor,
        ConfigurationChanges =
            ConfigChanges.Orientation |
            ConfigChanges.ScreenSize |
            ConfigChanges.KeyboardHidden |
            ConfigChanges.UiMode)]

    public class MainActivity : AndroidGameActivity
    {
        public const string ExtraLaunchSource = "com.zelda.ladxhd.archipelago.extra.LAUNCH_SOURCE";

        private AndroidPlatformInput _platformInput;
        private AndroidMagpieTrackerService _magpieTrackerService;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            var window = Window;
            if (window != null)
            {
                window.AddFlags(WindowManagerFlags.Fullscreen);
                window.AddFlags(WindowManagerFlags.LayoutNoLimits);
                window.ClearFlags(WindowManagerFlags.ForceNotFullscreen);

                if (OperatingSystem.IsAndroidVersionAtLeast(28) && window.Attributes is { } attributes)
                    attributes.LayoutInDisplayCutoutMode = LayoutInDisplayCutoutMode.ShortEdges;
            }

            base.OnCreate(savedInstanceState);
            VolumeControlStream = global::Android.Media.Stream.Music;

            BitmapFontContentReader.Register();

            var root = Application.Context.GetExternalFilesDir(null)!.AbsolutePath;
            if (!AndroidAssetInstallation.TryGetActiveRoot(root, out var installedAssetRoot, out _))
            {
                StartActivity(new global::Android.Content.Intent(this, typeof(AssetSetupActivity)));
                Finish();
                return;
            }

            // Ensure the writable user-data layout exists before the game starts.
            var externalMods = Path.Combine(root, "Mods");
            Directory.CreateDirectory(externalMods);
            Directory.CreateDirectory(Path.Combine(externalMods, "Animations"));
            Directory.CreateDirectory(Path.Combine(externalMods, "Dungeon"));
            Directory.CreateDirectory(Path.Combine(externalMods, "Graphics"));
            Directory.CreateDirectory(Path.Combine(externalMods, "Music"));
            Directory.CreateDirectory(Path.Combine(externalMods, "Languages"));
            Directory.CreateDirectory(Path.Combine(externalMods, "LAHDMods"));
            Directory.CreateDirectory(Path.Combine(externalMods, "Maps"));
            Directory.CreateDirectory(Path.Combine(externalMods, "SoundEffects"));
            Directory.CreateDirectory(Path.Combine(root, "SaveFiles"));

            // Get real display size for proper fullscreen rendering.
            var surfaceWidth = 0;
            var surfaceHeight = 0;

            if (OperatingSystem.IsAndroidVersionAtLeast(30))
            {
                var metrics = WindowManager?.CurrentWindowMetrics;
                var bounds = metrics?.Bounds;
                if (bounds != null)
                {
                    surfaceWidth = bounds.Width();
                    surfaceHeight = bounds.Height();
                }
            }
            else
            {
                var display = WindowManager?.DefaultDisplay;
                if (display != null)
                {
                    var size = new global::Android.Graphics.Point();
                #pragma warning disable CS0618
                    display.GetRealSize(size);
                #pragma warning restore CS0618
                    surfaceWidth = size.X;
                    surfaceHeight = size.Y;
                }
            }

            if (surfaceWidth > 0 && surfaceHeight > 0)
            {
                // Ensure landscape orientation (wider dimension first).
                if (surfaceWidth < surfaceHeight)
                {
                    var swap = surfaceWidth;
                    surfaceWidth = surfaceHeight;
                    surfaceHeight = swap;
                }
            }

            // construct your real game here:
            var game = new Game1(
                editorMode: false,
                loadSave: false,
                loadSlot: 0
            );
            var gameRoot = new FrameLayout(this);
            game.Content = new ExternalContentManager(game.Services, Path.Combine(installedAssetRoot, "Content"));
            game.Services.AddService(typeof(AssetManager), Assets);
            game.Services.AddService(typeof(IPlatformDisplayConfiguration), new PlatformDisplayConfiguration(surfaceWidth, surfaceHeight));
            game.Services.AddService(typeof(IPlatformFileSystem), new AndroidPlatformFileSystem(Assets, installedAssetRoot));
            game.Services.AddService(typeof(IUserDataPaths), new AndroidUserDataPaths(root));
            game.Services.AddService(typeof(ISharedSaveService), new AndroidSharedSaveService());
            _platformInput = new AndroidPlatformInput();
            game.Services.AddService(typeof(IPlatformInput), _platformInput);
            game.Services.AddService(typeof(ITextInputService), new NullTextInputService());
            game.Services.AddService(typeof(IPlatformWindow), new AndroidPlatformWindow());
            game.Services.AddService(typeof(IGraphicsCapabilities), new GraphicsCapabilities(
                usePresentationParametersForSize: true,
                canCreateGraphicsResourcesOnWorkerThread: false,
                supportsBlendFunctionMax: false,
                useAnisotropicFiltering: false));
            game.Services.AddService(typeof(IPlatformPresentation), new PlatformPresentation(240, true, true, 1));
            game.Services.AddService(typeof(IFileDialogService), new UnavailableFileDialogService());
            game.Services.AddService(typeof(IDiagnosticsSettingsService), new AndroidDiagnosticsSettingsService(this));
            game.Services.AddService(typeof(IArchipelagoSetupService), new AndroidArchipelagoSetupService(this));
            _magpieTrackerService = new AndroidMagpieTrackerService(this, gameRoot);
            game.Services.AddService(typeof(IMagpieTrackerService), _magpieTrackerService);

            var launchSource = Intent?.GetStringExtra(ExtraLaunchSource) ?? "direct";
            AndroidTelemetry.Initialize(this, root, launchSource);

            var view = (View)game.Services.GetService(typeof(View))!;
            var matchParent = new ViewGroup.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.MatchParent);
            view.LayoutParameters = matchParent;
            gameRoot.AddView(view, matchParent);
            SetContentView(gameRoot, matchParent);

            ApplyFullscreenFlags();

            view.Focusable = true;
            view.FocusableInTouchMode = true;
            view.RequestFocus();
            game.Run();
        }

        protected override void OnPause()
        {
            AndroidTelemetry.OnPause();
            base.OnPause();
        }

        protected override void OnDestroy()
        {
            _magpieTrackerService?.Hide();
            if (IsFinishing)
                AndroidTelemetry.OnFinishing();
            AndroidTelemetry.Shutdown();
            base.OnDestroy();
        }

        private void ApplyFullscreenFlags()
        {
            var window = Window;
            if (window == null)
                return;

            if (OperatingSystem.IsAndroidVersionAtLeast(30))
            {
                // Android 35 enforces edge-to-edge and obsoletes this API.
                if (!OperatingSystem.IsAndroidVersionAtLeast(35))
                    window.SetDecorFitsSystemWindows(false);

                var controller = window.InsetsController;
                if (controller != null)
                {
                    controller.Hide(WindowInsets.Type.StatusBars() |
                                   WindowInsets.Type.NavigationBars());
                    controller.SystemBarsBehavior =
                        (int)WindowInsetsControllerBehavior.ShowTransientBarsBySwipe;
                }
            }
            else
            {
                var decorView = window.DecorView;
                if (decorView == null)
                    return;

            #pragma warning disable CS0618
                decorView.SystemUiVisibility =
                    (StatusBarVisibility)(
                        SystemUiFlags.LayoutStable |
                        SystemUiFlags.LayoutHideNavigation |
                        SystemUiFlags.LayoutFullscreen |
                        SystemUiFlags.HideNavigation |
                        SystemUiFlags.Fullscreen |
                        SystemUiFlags.ImmersiveSticky);
            #pragma warning restore CS0618
            }
        }

        public override void OnWindowFocusChanged(bool hasFocus)
        {
            base.OnWindowFocusChanged(hasFocus);
            if (hasFocus)
                ApplyFullscreenFlags();
        }

        public override bool DispatchKeyEvent(KeyEvent e)
        {
            if (e == null)
                return base.DispatchKeyEvent(e);

            // Volume keys: handle them here, before the MonoGame view swallows them.
            // The game SurfaceView returns true from OnKeyDown for these, which kills the
            // system's default volume handling. Drive STREAM_MUSIC directly + show the HUD.
            if (e.KeyCode == Keycode.VolumeUp || e.KeyCode == Keycode.VolumeDown || e.KeyCode == Keycode.VolumeMute)
            {
                if (e.Action == KeyEventActions.Down &&
                    GetSystemService(AudioService) is global::Android.Media.AudioManager audio)
                {
                    var direction = e.KeyCode switch
                    {
                        Keycode.VolumeUp   => global::Android.Media.Adjust.Raise,
                        Keycode.VolumeDown => global::Android.Media.Adjust.Lower,
                        _                  => global::Android.Media.Adjust.ToggleMute
                    };
                    audio.AdjustStreamVolume(global::Android.Media.Stream.Music, direction, global::Android.Media.VolumeNotificationFlags.ShowUi);
                }
                return true;
            }
            // Ignore key repeats - we do our own held-state tracking via BeginFrame().
            if (e.RepeatCount > 0)
                return base.DispatchKeyEvent(e);

            bool isDown = e.Action == KeyEventActions.Down;
            bool isUp   = e.Action == KeyEventActions.Up;
            if (!isDown && !isUp)
                return base.DispatchKeyEvent(e);

            // Map Android keycodes to CButtons for devices that route physical
            // buttons as KeyEvents instead of through the GamePad API.
            CButtons? mapped = e.KeyCode switch
            {
                Keycode.ButtonA      => CButtons.A,
                Keycode.ButtonB      => CButtons.B,
                Keycode.ButtonX      => CButtons.X,
                Keycode.ButtonY      => CButtons.Y,
                Keycode.ButtonL1     => CButtons.LB,
                Keycode.ButtonR1     => CButtons.RB,
                Keycode.ButtonL2     => CButtons.LT,
                Keycode.ButtonR2     => CButtons.RT,
                Keycode.ButtonStart  => CButtons.Start,
                Keycode.DpadUp       => CButtons.Up,
                Keycode.DpadDown     => CButtons.Down,
                Keycode.DpadLeft     => CButtons.Left,
                Keycode.DpadRight    => CButtons.Right,
                Keycode.ButtonThumbl => CButtons.LS,
                Keycode.ButtonThumbr => CButtons.RS,
                _                    => null
            };

            if (mapped.HasValue)
            {
                // Pass through to MonoGame for proper gamepads, unless it's a device
                // known to send buttons as KeyEvents despite detected as "SOURCE_GAMEPAD".
                // - GameMT E5 Ultra with "CH32V203".

                bool isKnownKeyEventDevice = e.Device != null &&
                    e.Device.Name.Contains("CH32V203", StringComparison.OrdinalIgnoreCase);

                if ((e.Source & InputSourceType.Gamepad) != 0 && !isKnownKeyEventDevice)
                    return base.DispatchKeyEvent(e);

                _platformInput?.SetButton(mapped.Value, isDown);
                return true;
            }

            // Legacy select/back handling. Also feeds the new system so ButtonDown(Select) works.
            if (e.KeyCode == Keycode.Back        ||
                e.KeyCode == Keycode.ButtonSelect ||
                e.KeyCode == Keycode.ButtonMode  ||
                e.KeyCode == Keycode.Menu        ||
                e.KeyCode == Keycode.Escape)
            {
                _platformInput?.SetButton(CButtons.Select, isDown);
                if (isDown)
                    _platformInput?.SetSelectPressed();
                return true;
            }
            return base.DispatchKeyEvent(e);
        }

        public override bool DispatchGenericMotionEvent(MotionEvent e)
        {
            if (e == null)
                return base.DispatchGenericMotionEvent(e);

            // Read right stick axes directly for devices that report them via 
            // motion events but whose source flags MonoGame doesn't recognize.
            // AXIS_Z (11) = right stick X, AXIS_RZ (14) = right stick Y.
            float x = e.GetAxisValue(Axis.Z);
            float y = e.GetAxisValue(Axis.Rz);

            if (Math.Abs(x) > 0.05f || Math.Abs(y) > 0.05f || 
                _platformInput?.RightStick != Vector2.Zero)
            {
                _platformInput?.SetRightStick(x, y);
            }
            return base.DispatchGenericMotionEvent(e);
        }

        public override void OnBackPressed()
        {
            if (_magpieTrackerService?.TryHandleBackPressed() == true)
                return;

            _platformInput?.SetSelectPressed();
            _platformInput?.SetButton(CButtons.Select, true);
        }
    }
}
