using System;
using System.Collections.Generic;
using System.IO;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.OS;
using Android.Service.Wallpaper;
using Android.Views;
using Android.Widget;
using FilePath = System.IO.Path;
using GraphicsPath = Android.Graphics.Path;

namespace ProjectZ.Android
{
    internal static class LadxhdWallpaperPreferences
    {
        private const string PreferencesName = "ladxhd_live_wallpaper";
        private const string AnimateKey = "animate";
        private const string IslandLifeKey = "island_life";
        private const string FeaturedCharacterKey = "featured_character";
        private const string SceneKey = "scene";
        private const string TimeOfDayKey = "time_of_day";
        private const string TapActionKey = "tap_action";
        private const string LinkActivityKey = "link_activity";
        private const string FrameRateKey = "frame_rate";

        public static bool IsAnimated(Context context) =>
            context.GetSharedPreferences(PreferencesName, FileCreationMode.Private)
                ?.GetBoolean(AnimateKey, true) ?? true;

        public static int GetFrameRate(Context context)
        {
            var value = context.GetSharedPreferences(PreferencesName, FileCreationMode.Private)
                ?.GetInt(FrameRateKey, 30) ?? 30;
            return value <= 15 ? 15 : 30;
        }

        public static void SetAnimated(Context context, bool value) =>
            context.GetSharedPreferences(PreferencesName, FileCreationMode.Private)
                ?.Edit()?.PutBoolean(AnimateKey, value)?.Apply();

        public static bool ShowIslandLife(Context context) =>
            context.GetSharedPreferences(PreferencesName, FileCreationMode.Private)
                ?.GetBoolean(IslandLifeKey, true) ?? true;

        public static void SetShowIslandLife(Context context, bool value) =>
            context.GetSharedPreferences(PreferencesName, FileCreationMode.Private)
                ?.Edit()?.PutBoolean(IslandLifeKey, value)?.Apply();

        public static int GetFeaturedCharacter(Context context)
        {
            var value = context.GetSharedPreferences(PreferencesName, FileCreationMode.Private)
                ?.GetInt(FeaturedCharacterKey, 0) ?? 0;
            return Math.Clamp(value, 0, 3);
        }

        public static void SetFeaturedCharacter(Context context, int value) =>
            context.GetSharedPreferences(PreferencesName, FileCreationMode.Private)
                ?.Edit()?.PutInt(FeaturedCharacterKey, Math.Clamp(value, 0, 3))?.Apply();

        public static int GetScene(Context context)
        {
            var value = context.GetSharedPreferences(PreferencesName, FileCreationMode.Private)
                ?.GetInt(SceneKey, 0) ?? 0;
            return Math.Clamp(value, 0, 4);
        }

        public static void SetScene(Context context, int value) =>
            context.GetSharedPreferences(PreferencesName, FileCreationMode.Private)
                ?.Edit()?.PutInt(SceneKey, Math.Clamp(value, 0, 4))?.Apply();

        public static int GetTimeOfDay(Context context)
        {
            var value = context.GetSharedPreferences(PreferencesName, FileCreationMode.Private)
                ?.GetInt(TimeOfDayKey, 0) ?? 0;
            return Math.Clamp(value, 0, 3);
        }

        public static void SetTimeOfDay(Context context, int value) =>
            context.GetSharedPreferences(PreferencesName, FileCreationMode.Private)
                ?.Edit()?.PutInt(TimeOfDayKey, Math.Clamp(value, 0, 3))?.Apply();

        public static int GetTapAction(Context context)
        {
            var value = context.GetSharedPreferences(PreferencesName, FileCreationMode.Private)
                ?.GetInt(TapActionKey, 0) ?? 0;
            return Math.Clamp(value, 0, 2);
        }

        public static void SetTapAction(Context context, int value) =>
            context.GetSharedPreferences(PreferencesName, FileCreationMode.Private)
                ?.Edit()?.PutInt(TapActionKey, Math.Clamp(value, 0, 2))?.Apply();

        public static int GetLinkActivity(Context context)
        {
            var value = context.GetSharedPreferences(PreferencesName, FileCreationMode.Private)
                ?.GetInt(LinkActivityKey, 0) ?? 0;
            return Math.Clamp(value, 0, 3);
        }

        public static void SetLinkActivity(Context context, int value) =>
            context.GetSharedPreferences(PreferencesName, FileCreationMode.Private)
                ?.Edit()?.PutInt(LinkActivityKey, Math.Clamp(value, 0, 3))?.Apply();

        public static void SetFrameRate(Context context, int value) =>
            context.GetSharedPreferences(PreferencesName, FileCreationMode.Private)
                ?.Edit()?.PutInt(FrameRateKey, value <= 15 ? 15 : 30)?.Apply();
    }

    internal static class LadxhdWallpaperLauncher
    {
        public static void ShowPreview(Context context)
        {
            var component = new ComponentName(
                context, Java.Lang.Class.FromType(typeof(LadxhdWallpaperService)));
            var intent = new Intent(WallpaperManager.ActionChangeLiveWallpaper);
            intent.PutExtra(WallpaperManager.ExtraLiveWallpaperComponent, component);
            if (context is not Activity)
                intent.AddFlags(ActivityFlags.NewTask);
            try
            {
                context.StartActivity(intent);
            }
            catch (ActivityNotFoundException)
            {
                var chooser = new Intent(WallpaperManager.ActionLiveWallpaperChooser);
                if (context is not Activity)
                    chooser.AddFlags(ActivityFlags.NewTask);
                context.StartActivity(chooser);
            }
        }
    }

    internal sealed class AndroidLiveWallpaperService : ILiveWallpaperService
    {
        private readonly Activity _activity;

        public AndroidLiveWallpaperService(Activity activity) => _activity = activity;

        public bool IsAvailable => true;

        public void Show() =>
            _activity.StartActivity(new Intent(_activity, typeof(LadxhdWallpaperSettingsActivity)));
    }

    [Activity(
        Name = "com.zelda.ladxhd.archipelago.LadxhdWallpaperSettingsActivity",
        Label = "@string/wallpaper_settings_name",
        Theme = "@android:style/Theme.DeviceDefault.NoActionBar",
        Exported = true,
        ScreenOrientation = ScreenOrientation.Unspecified)]
    public sealed class LadxhdWallpaperSettingsActivity : Activity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            BuildInterface();
        }

        private void BuildInterface()
        {
            var density = Resources?.DisplayMetrics?.Density ?? 1f;
            int Dp(int value) => (int)(value * density + 0.5f);

            var scroll = new ScrollView(this);
            var layout = new LinearLayout(this) { Orientation = Orientation.Vertical };
            layout.SetPadding(Dp(24), Dp(32), Dp(24), Dp(32));
            scroll.AddView(layout, new ViewGroup.LayoutParams(
                ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent));

            var title = new TextView(this) { Text = "LADXHD Live Wallpaper", TextSize = 28f };
            title.SetTypeface(null, TypefaceStyle.Bold);
            layout.AddView(title);

            var explanation = new TextView(this)
            {
                Text = "A silent, battery-aware Koholint scene. It uses Link, island characters, and wildlife animations from your locally generated game data without starting gameplay, saves, or Archipelago networking.",
                TextSize = 17f
            };
            var textParams = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
            textParams.SetMargins(0, Dp(16), 0, Dp(20));
            layout.AddView(explanation, textParams);

            var assetReady = LadxhdWallpaperAssets.TryResolve(
                this, out _, out _, out var reason);
            var status = new TextView(this)
            {
                Text = assetReady
                    ? "Game data ready: the wallpaper will use installed LADXHD character and wildlife sprites."
                    : $"Game data unavailable: {reason} The scenery will still render, but game characters will appear after setup.",
                TextSize = 15f
            };
            layout.AddView(status);

            var animate = new global::Android.Widget.Switch(this)
            {
                Text = "Animate Link, water, clouds, and touch effects",
                Checked = LadxhdWallpaperPreferences.IsAnimated(this)
            };
            animate.CheckedChange += (_, args) =>
                LadxhdWallpaperPreferences.SetAnimated(this, args.IsChecked);
            var animateParams = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
            animateParams.SetMargins(0, Dp(20), 0, Dp(12));
            layout.AddView(animate, animateParams);

            var islandLife = new global::Android.Widget.Switch(this)
            {
                Text = "Show featured character and Koholint wildlife",
                Checked = LadxhdWallpaperPreferences.ShowIslandLife(this),
                Enabled = assetReady
            };
            islandLife.CheckedChange += (_, args) =>
                LadxhdWallpaperPreferences.SetShowIslandLife(this, args.IsChecked);
            var islandLifeParams = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
            islandLifeParams.SetMargins(0, 0, 0, Dp(12));
            layout.AddView(islandLife, islandLifeParams);

            var characterLabel = new TextView(this)
            {
                Text = "Featured island character",
                TextSize = 17f,
                Enabled = assetReady
            };
            layout.AddView(characterLabel);
            var character = new Spinner(this) { Enabled = assetReady };
            var characterAdapter = new ArrayAdapter<string>(this,
                global::Android.Resource.Layout.SimpleSpinnerItem,
                ["Marin", "BowWow", "Rooster", "Rotate automatically"]);
            characterAdapter.SetDropDownViewResource(
                global::Android.Resource.Layout.SimpleSpinnerDropDownItem);
            character.Adapter = characterAdapter;
            character.SetSelection(LadxhdWallpaperPreferences.GetFeaturedCharacter(this));
            character.ItemSelected += (_, args) =>
                LadxhdWallpaperPreferences.SetFeaturedCharacter(this, args.Position);
            var characterParams = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
            characterParams.SetMargins(0, 0, 0, Dp(12));
            layout.AddView(character, characterParams);

            var linkLabel = new TextView(this)
            {
                Text = "Link activity",
                TextSize = 17f,
                Enabled = assetReady
            };
            layout.AddView(linkLabel);
            var linkActivity = new Spinner(this) { Enabled = assetReady };
            var linkAdapter = new ArrayAdapter<string>(this,
                global::Android.Resource.Layout.SimpleSpinnerItem,
                ["Walk across scene", "Stand in scene", "Alternate travel and rest", "Hide Link"]);
            linkAdapter.SetDropDownViewResource(
                global::Android.Resource.Layout.SimpleSpinnerDropDownItem);
            linkActivity.Adapter = linkAdapter;
            linkActivity.SetSelection(LadxhdWallpaperPreferences.GetLinkActivity(this));
            linkActivity.ItemSelected += (_, args) =>
                LadxhdWallpaperPreferences.SetLinkActivity(this, args.Position);
            var linkParams = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
            linkParams.SetMargins(0, 0, 0, Dp(12));
            layout.AddView(linkActivity, linkParams);

            var sceneLabel = new TextView(this)
            {
                Text = "Wallpaper scenery",
                TextSize = 17f,
                Enabled = assetReady
            };
            layout.AddView(sceneLabel);
            var scene = new Spinner(this) { Enabled = assetReady };
            var sceneAdapter = new ArrayAdapter<string>(this,
                global::Android.Resource.Layout.SimpleSpinnerItem,
                ["Stylized Koholint coast", "Installed Mabe Village", "Installed Toronbo Shores",
                 "Installed Mysterious Forest", "Rotate installed locations"]);
            sceneAdapter.SetDropDownViewResource(
                global::Android.Resource.Layout.SimpleSpinnerDropDownItem);
            scene.Adapter = sceneAdapter;
            scene.SetSelection(LadxhdWallpaperPreferences.GetScene(this));
            scene.ItemSelected += (_, args) =>
                LadxhdWallpaperPreferences.SetScene(this, args.Position);
            var sceneParams = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
            sceneParams.SetMargins(0, 0, 0, Dp(12));
            layout.AddView(scene, sceneParams);

            var timeLabel = new TextView(this)
            {
                Text = "Time of day",
                TextSize = 17f
            };
            layout.AddView(timeLabel);
            var timeOfDay = new Spinner(this);
            var timeAdapter = new ArrayAdapter<string>(this,
                global::Android.Resource.Layout.SimpleSpinnerItem,
                ["Follow system time", "Day", "Sunset", "Night"]);
            timeAdapter.SetDropDownViewResource(
                global::Android.Resource.Layout.SimpleSpinnerDropDownItem);
            timeOfDay.Adapter = timeAdapter;
            timeOfDay.SetSelection(LadxhdWallpaperPreferences.GetTimeOfDay(this));
            timeOfDay.ItemSelected += (_, args) =>
                LadxhdWallpaperPreferences.SetTimeOfDay(this, args.Position);
            var timeParams = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
            timeParams.SetMargins(0, 0, 0, Dp(12));
            layout.AddView(timeOfDay, timeParams);

            var tapLabel = new TextView(this)
            {
                Text = "Wallpaper tap action",
                TextSize = 17f
            };
            layout.AddView(tapLabel);
            var tapAction = new Spinner(this);
            var tapAdapter = new ArrayAdapter<string>(this,
                global::Android.Resource.Layout.SimpleSpinnerItem,
                ["Show ripple", "Cycle featured character", "Switch scenery"]);
            tapAdapter.SetDropDownViewResource(
                global::Android.Resource.Layout.SimpleSpinnerDropDownItem);
            tapAction.Adapter = tapAdapter;
            tapAction.SetSelection(LadxhdWallpaperPreferences.GetTapAction(this));
            tapAction.ItemSelected += (_, args) =>
                LadxhdWallpaperPreferences.SetTapAction(this, args.Position);
            var tapParams = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
            tapParams.SetMargins(0, 0, 0, Dp(12));
            layout.AddView(tapAction, tapParams);

            var rateLabel = new TextView(this) { Text = "Animation frame rate", TextSize = 17f };
            layout.AddView(rateLabel);
            var rates = new RadioGroup(this) { Orientation = Orientation.Horizontal };
            var saver = new RadioButton(this) { Text = "Battery saver (15 FPS)", Id = View.GenerateViewId() };
            var smooth = new RadioButton(this) { Text = "Smooth (30 FPS)", Id = View.GenerateViewId() };
            rates.AddView(saver);
            rates.AddView(smooth);
            rates.Check(LadxhdWallpaperPreferences.GetFrameRate(this) <= 15 ? saver.Id : smooth.Id);
            rates.CheckedChange += (_, args) =>
                LadxhdWallpaperPreferences.SetFrameRate(this, args.CheckedId == saver.Id ? 15 : 30);
            layout.AddView(rates);

            var preview = new Button(this) { Text = "Preview and set wallpaper" };
            preview.Click += (_, _) => LadxhdWallpaperLauncher.ShowPreview(this);
            var previewParams = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
            previewParams.SetMargins(0, Dp(24), 0, Dp(8));
            layout.AddView(preview, previewParams);

            if (!assetReady)
            {
                var setup = new Button(this) { Text = "Set up LADXHD game data" };
                setup.Click += (_, _) => StartActivity(new Intent(this, typeof(AssetSetupActivity)));
                layout.AddView(setup);
            }

            SetContentView(scroll);
        }
    }

    internal static class LadxhdWallpaperAssets
    {
        private static readonly string[] PreferredLinkAnimations =
            ["walk_2", "walk_0", "walk_1", "walk_3", "stand_2", "stand_0"];

        public static bool TryResolve(
            Context context,
            out LiveWallpaperAnimation animation,
            out string spritePath,
            out string reason) =>
            TryResolve(context, "link0.ani", PreferredLinkAnimations,
                out animation, out spritePath, out reason);

        public static bool TryResolve(
            Context context,
            string relativeAnimationPath,
            IEnumerable<string> preferredAnimations,
            out LiveWallpaperAnimation animation,
            out string spritePath,
            out string reason)
        {
            animation = null;
            spritePath = null;
            reason = null;
            if (!AndroidAssetInstallation.TryGetActiveRoot(context, out var root, out reason))
                return false;

            try
            {
                var dataRoot = FilePath.GetFullPath(FilePath.Combine(root, "Data"));
                if (!LiveWallpaperAnimation.TryNormalizeRelativePath(
                        relativeAnimationPath, out var normalizedAnimationPath))
                    throw new InvalidDataException("The wallpaper animation path is invalid.");
                var animationsRoot = FilePath.GetFullPath(
                    FilePath.Combine(dataRoot, "Animations"));
                var animationPath = FilePath.GetFullPath(FilePath.Combine(animationsRoot,
                    normalizedAnimationPath.Replace('/', FilePath.DirectorySeparatorChar)));
                var animationRootPrefix = animationsRoot.TrimEnd(FilePath.DirectorySeparatorChar) +
                                          FilePath.DirectorySeparatorChar;
                if (!animationPath.StartsWith(animationRootPrefix, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("The wallpaper animation path is invalid.");
                using var reader = File.OpenText(animationPath);
                if (!LiveWallpaperAnimation.TryLoad(reader, preferredAnimations, out animation))
                    throw new InvalidDataException("The requested wallpaper animation is unavailable.");

                if (!LiveWallpaperAnimation.TryGetSpriteRelativeCandidates(
                        animation.SpritePath, out var relativeCandidates))
                    throw new InvalidDataException("The wallpaper sprite path is invalid.");
                foreach (var relativeCandidate in relativeCandidates)
                {
                    var candidate = FilePath.Combine(dataRoot,
                        relativeCandidate.Replace('/', FilePath.DirectorySeparatorChar));
                    var fullCandidate = FilePath.GetFullPath(candidate);
                    var rootPrefix = dataRoot.TrimEnd(FilePath.DirectorySeparatorChar) +
                                     FilePath.DirectorySeparatorChar;
                    if (fullCandidate.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase) &&
                        File.Exists(fullCandidate))
                    {
                        spritePath = fullCandidate;
                        return true;
                    }
                }

                throw new FileNotFoundException("The wallpaper sprite is unavailable.");
            }
            catch (Exception exception)
            {
                animation = null;
                spritePath = null;
                reason = exception.Message;
                return false;
            }
        }

        public static bool TryResolveOverworldMap(
            Context context,
            out LiveWallpaperMap map,
            out string tilesetPath,
            out string reason)
        {
            map = null;
            tilesetPath = null;
            reason = null;
            if (!AndroidAssetInstallation.TryGetActiveRoot(context, out var root, out reason))
                return false;

            try
            {
                var dataRoot = FilePath.GetFullPath(FilePath.Combine(root, "Data"));
                var mapPath = FilePath.Combine(dataRoot, "Maps", "overworld.map");
                using var reader = File.OpenText(mapPath);
                if (!LiveWallpaperMap.TryLoad(reader, out map))
                    throw new InvalidDataException("The installed overworld map is unavailable.");

                var tilesetRoot = FilePath.GetFullPath(
                    FilePath.Combine(dataRoot, "Maps", "Tilesets"));
                var candidate = FilePath.GetFullPath(FilePath.Combine(tilesetRoot,
                    map.TilesetPath.Replace('/', FilePath.DirectorySeparatorChar)));
                var rootPrefix = tilesetRoot.TrimEnd(FilePath.DirectorySeparatorChar) +
                                 FilePath.DirectorySeparatorChar;
                if (!candidate.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase) ||
                    !File.Exists(candidate))
                    throw new FileNotFoundException("The installed overworld tileset is unavailable.");

                tilesetPath = candidate;
                return true;
            }
            catch (Exception exception)
            {
                map = null;
                tilesetPath = null;
                reason = exception.Message;
                return false;
            }
        }
    }

    [Service(
        Name = "com.zelda.ladxhd.archipelago.LadxhdWallpaperService",
        Label = "@string/wallpaper_name",
        Permission = "android.permission.BIND_WALLPAPER",
        Exported = true)]
    [IntentFilter(new[] { "android.service.wallpaper.WallpaperService" })]
    [MetaData("android.service.wallpaper", Resource = "@xml/ladxhd_wallpaper")]
    public sealed class LadxhdWallpaperService : WallpaperService
    {
        public override Engine OnCreateEngine() => new LadxhdWallpaperEngine(this);

        [global::Android.Runtime.Register(
            "com/zelda/ladxhd/archipelago/LadxhdWallpaperEngine",
            DoNotGenerateAcw = true)]
        private sealed class LadxhdWallpaperEngine : Engine
        {
            private readonly LadxhdWallpaperService _service;
            private readonly Handler _handler = new Handler(Looper.MainLooper);
            private readonly DrawRunnable _drawRunnable;
            private LadxhdWallpaperScene _scene;
            private bool _visible;
            private bool _surfaceReady;
            private float _xOffset = 0.5f;
            private long _startedAt;

            public LadxhdWallpaperEngine(LadxhdWallpaperService service) : base(service)
            {
                _service = service;
                _drawRunnable = new DrawRunnable(DrawFrame);
                _scene = new LadxhdWallpaperScene(service);
                _startedAt = SystemClock.ElapsedRealtime();
                SetTouchEventsEnabled(true);
            }

            public override void OnVisibilityChanged(bool visible)
            {
                _visible = visible;
                ScheduleFrame(immediate: visible);
            }

            public override void OnSurfaceCreated(ISurfaceHolder holder)
            {
                base.OnSurfaceCreated(holder);
                _surfaceReady = true;
                ScheduleFrame(immediate: true);
            }

            public override void OnSurfaceChanged(ISurfaceHolder holder, Format format, int width, int height)
            {
                base.OnSurfaceChanged(holder, format, width, height);
                ScheduleFrame(immediate: true);
            }

            public override void OnSurfaceDestroyed(ISurfaceHolder holder)
            {
                _surfaceReady = false;
                _handler.RemoveCallbacks(_drawRunnable);
                base.OnSurfaceDestroyed(holder);
            }

            public override void OnOffsetsChanged(
                float xOffset, float yOffset, float xOffsetStep, float yOffsetStep,
                int xPixelOffset, int yPixelOffset)
            {
                _xOffset = Math.Clamp(xOffset, 0f, 1f);
                ScheduleFrame(immediate: true);
            }

            public override void OnTouchEvent(MotionEvent e)
            {
                if (e?.Action == MotionEventActions.Down)
                {
                    _scene.OnTouch(e.GetX(), e.GetY(), SystemClock.ElapsedRealtime() - _startedAt);
                    switch (LadxhdWallpaperPreferences.GetTapAction(_service))
                    {
                        case 1:
                            LadxhdWallpaperPreferences.SetFeaturedCharacter(_service,
                                LiveWallpaperInteraction.NextFeaturedCharacter(
                                    LadxhdWallpaperPreferences.GetFeaturedCharacter(_service)));
                            break;
                        case 2:
                            LadxhdWallpaperPreferences.SetScene(_service,
                                LiveWallpaperInteraction.NextScene(
                                    LadxhdWallpaperPreferences.GetScene(_service)));
                            break;
                    }
                    ScheduleFrame(immediate: true);
                }
                base.OnTouchEvent(e);
            }

            public override void OnDestroy()
            {
                _visible = false;
                _surfaceReady = false;
                _handler.RemoveCallbacks(_drawRunnable);
                _scene?.Dispose();
                _scene = null;
                base.OnDestroy();
            }

            private void DrawFrame()
            {
                if (!_visible || !_surfaceReady || _scene == null)
                    return;

                Canvas canvas = null;
                try
                {
                    canvas = SurfaceHolder?.LockCanvas();
                    if (canvas != null)
                    {
                        var elapsed = SystemClock.ElapsedRealtime() - _startedAt;
                        _scene.Draw(canvas, elapsed, _xOffset,
                            LadxhdWallpaperPreferences.IsAnimated(_service),
                            LadxhdWallpaperPreferences.ShowIslandLife(_service),
                            LadxhdWallpaperPreferences.GetFeaturedCharacter(_service),
                            LadxhdWallpaperPreferences.GetScene(_service),
                            LadxhdWallpaperPreferences.GetTimeOfDay(_service),
                            LadxhdWallpaperPreferences.GetLinkActivity(_service));
                    }
                }
                finally
                {
                    if (canvas != null)
                        SurfaceHolder?.UnlockCanvasAndPost(canvas);
                }
                ScheduleFrame(immediate: false);
            }

            private void ScheduleFrame(bool immediate)
            {
                _handler.RemoveCallbacks(_drawRunnable);
                if (!_visible || !_surfaceReady)
                    return;
                var delay = immediate ? 0L : LiveWallpaperFrameScheduler.GetDelayMilliseconds(
                    LadxhdWallpaperPreferences.IsAnimated(_service),
                    LadxhdWallpaperPreferences.GetFrameRate(_service));
                _handler.PostDelayed(_drawRunnable, delay);
            }
        }

        private sealed class DrawRunnable : Java.Lang.Object, Java.Lang.IRunnable
        {
            private readonly Action _action;
            public DrawRunnable(Action action) => _action = action;
            public void Run() => _action();
        }
    }

    internal sealed class LadxhdWallpaperScene : IDisposable
    {
        private sealed class SpriteAsset
        {
            public SpriteAsset(Bitmap bitmap, LiveWallpaperAnimation animation)
            {
                Bitmap = bitmap;
                Animation = animation;
            }

            public Bitmap Bitmap { get; }
            public LiveWallpaperAnimation Animation { get; }
        }

        private sealed class MapAsset
        {
            public MapAsset(Bitmap bitmap, LiveWallpaperMap map)
            {
                Bitmap = bitmap;
                Map = map;
            }

            public Bitmap Bitmap { get; }
            public LiveWallpaperMap Map { get; }
        }

        private readonly Paint _paint = new Paint { AntiAlias = false, FilterBitmap = false };
        private readonly Paint _smoothPaint = new Paint { AntiAlias = true };
        private readonly Random _random = new Random(0x4C415844);
        private readonly List<(float X, float Y, float Phase)> _stars = [];
        private readonly Dictionary<string, Bitmap> _spriteSheets =
            new Dictionary<string, Bitmap>(StringComparer.OrdinalIgnoreCase);
        private SpriteAsset _linkWalking;
        private SpriteAsset _linkStanding;
        private SpriteAsset _marin;
        private SpriteAsset _bowWow;
        private SpriteAsset _rooster;
        private SpriteAsset _butterfly;
        private SpriteAsset _owl;
        private MapAsset _overworldMap;
        private float _touchX;
        private float _touchY;
        private long _touchAt = long.MinValue;

        public LadxhdWallpaperScene(Context context)
        {
            for (var index = 0; index < 42; index++)
                _stars.Add(((float)_random.NextDouble(), (float)_random.NextDouble() * 0.48f,
                    (float)_random.NextDouble() * MathF.PI * 2f));
            _linkWalking = LoadSprite(context, "link0.ani",
                ["walk_2", "walk_0", "walk_1", "walk_3"]);
            _linkStanding = LoadSprite(context, "link0.ani",
                ["stand_2", "stand_0", "stand_1", "stand_3"]);
            _marin = LoadSprite(context, "NPCs/marin.ani",
                ["sing", "idle", "stand_0", "stand_2"]);
            _bowWow = LoadSprite(context, "NPCs/BowWow.ani",
                ["walk_2", "walk_0", "walk_1", "walk_3"]);
            _rooster = LoadSprite(context, "NPCs/cock.ani",
                ["stand_3", "stand_2", "stand_0", "spawn"]);
            _butterfly = LoadSprite(context, "NPCs/butterfly.ani", ["idle"]);
            _owl = LoadSprite(context, "NPCs/owl.ani", ["fly", "hover", "idle"]);
            _overworldMap = LoadOverworldMap(context);
        }

        public void OnTouch(float x, float y, long elapsed)
        {
            _touchX = x;
            _touchY = y;
            _touchAt = elapsed;
        }

        public void Draw(
            Canvas canvas,
            long elapsed,
            float xOffset,
            bool animated,
            bool showIslandLife,
            int featuredCharacter,
            int scene,
            int timeOfDay,
            int linkActivity)
        {
            var width = canvas.Width;
            var height = canvas.Height;
            if (width <= 0 || height <= 0)
                return;
            var time = animated ? elapsed : 0L;
            var unit = Math.Max(1f, Math.Min(width, height) / 240f);
            var phase = LiveWallpaperLighting.Resolve(timeOfDay, DateTime.Now.Hour);

            DrawSky(canvas, width, height, time, xOffset, unit, phase);
            if (showIslandLife)
                DrawOwl(canvas, width, height, time, unit, animated);
            var resolvedScene = LiveWallpaperSceneSelection.Resolve(
                scene, elapsed, _overworldMap != null);
            var groundY = resolvedScene > 0
                ? DrawInstalledMap(canvas, width, height, resolvedScene)
                : DrawIsland(canvas, width, height, time, xOffset, unit);
            if (showIslandLife)
            {
                DrawFeaturedCharacter(canvas, width, groundY, time, xOffset, unit,
                    featuredCharacter);
                DrawButterflies(canvas, width, groundY, time, unit, animated);
            }
            DrawLink(canvas, width, groundY, elapsed, unit, animated, linkActivity);
            DrawLightingOverlay(canvas, width, height, phase);
            DrawTouchEffect(canvas, time, unit, animated);
        }

        private void DrawSky(
            Canvas canvas, int width, int height, long elapsed, float xOffset, float unit,
            LiveWallpaperTimePhase phase)
        {
            var horizon = height * 0.58f;
            var sky = phase switch
            {
                LiveWallpaperTimePhase.Day =>
                    (Color.Rgb(77, 154, 211), Color.Rgb(136, 199, 229), Color.Rgb(238, 220, 159)),
                LiveWallpaperTimePhase.Night =>
                    (Color.Rgb(9, 14, 36), Color.Rgb(25, 32, 67), Color.Rgb(57, 61, 94)),
                _ =>
                    (Color.Rgb(28, 35, 74), Color.Rgb(79, 78, 122), Color.Rgb(231, 126, 103))
            };
            Fill(canvas, sky.Item1, 0, 0, width, horizon * 0.45f);
            Fill(canvas, sky.Item2, 0, horizon * 0.45f, width, horizon * 0.72f);
            Fill(canvas, sky.Item3, 0, horizon * 0.72f, width, horizon);

            if (phase != LiveWallpaperTimePhase.Day)
            {
                var starPulse = elapsed / 700f;
                var phaseAlpha = phase == LiveWallpaperTimePhase.Night ? 1f : 0.55f;
                foreach (var star in _stars)
                {
                    var alpha = (int)((120 + 100 *
                        (MathF.Sin(starPulse + star.Phase) * 0.5f + 0.5f)) * phaseAlpha);
                    _paint.Color = Color.Argb(alpha, 255, 244, 196);
                    var size = unit * (star.Phase > MathF.PI ? 1.5f : 1f);
                    canvas.DrawRect(star.X * width, star.Y * height, star.X * width + size,
                        star.Y * height + size, _paint);
                }
            }

            _smoothPaint.Color = phase == LiveWallpaperTimePhase.Night
                ? Color.Rgb(218, 224, 224)
                : Color.Rgb(255, 211, 119);
            var celestialY = phase == LiveWallpaperTimePhase.Day ? horizon * 0.38f : horizon * 0.66f;
            canvas.DrawCircle(width * 0.74f, celestialY, 22f * unit, _smoothPaint);

            var cloudTravel = (elapsed / 50f) % (width + 160f * unit);
            DrawCloud(canvas, width * 0.2f + cloudTravel * 0.18f - 60f * unit - xOffset * 20f * unit,
                height * 0.18f, 1f * unit);
            DrawCloud(canvas, width - cloudTravel * 0.12f - xOffset * 35f * unit,
                height * 0.31f, 0.7f * unit);

            var mountainShift = (xOffset - 0.5f) * 42f * unit;
            _paint.Color = Color.Rgb(43, 49, 83);
            var mountains = new GraphicsPath();
            mountains.MoveTo(-60f * unit - mountainShift, horizon);
            mountains.LineTo(width * 0.12f - mountainShift, horizon * 0.55f);
            mountains.LineTo(width * 0.30f - mountainShift, horizon);
            mountains.LineTo(width * 0.48f - mountainShift, horizon * 0.46f);
            mountains.LineTo(width * 0.70f - mountainShift, horizon);
            mountains.LineTo(width * 0.86f - mountainShift, horizon * 0.62f);
            mountains.LineTo(width + 60f * unit, horizon);
            mountains.Close();
            canvas.DrawPath(mountains, _paint);
        }

        private void DrawLightingOverlay(
            Canvas canvas, int width, int height, LiveWallpaperTimePhase phase)
        {
            if (phase == LiveWallpaperTimePhase.Day)
                return;
            _paint.Color = phase == LiveWallpaperTimePhase.Night
                ? Color.Argb(82, 7, 16, 50)
                : Color.Argb(22, 116, 45, 59);
            canvas.DrawRect(0, 0, width, height, _paint);
        }

        private float DrawIsland(
            Canvas canvas, int width, int height, long elapsed, float xOffset, float unit)
        {
            var waterTop = height * 0.58f;
            Fill(canvas, Color.Rgb(32, 93, 132), 0, waterTop, width, height);
            var waveShift = (elapsed / 45f) % (20f * unit);
            _paint.Color = Color.Argb(120, 143, 225, 218);
            for (var row = 0; row < 7; row++)
            {
                var y = waterTop + (row * 18f + 7f) * unit;
                for (var x = -40f * unit; x < width + 40f * unit; x += 40f * unit)
                {
                    var shifted = x + (row % 2 == 0 ? waveShift : -waveShift);
                    canvas.DrawRect(shifted, y, shifted + 18f * unit, y + 2f * unit, _paint);
                }
            }

            var shoreTop = height * 0.69f;
            Fill(canvas, Color.Rgb(238, 196, 114), -20f * unit, shoreTop, width + 20f * unit, height);
            Fill(canvas, Color.Rgb(79, 155, 88), -20f * unit, height * 0.76f, width + 20f * unit, height);
            _paint.Color = Color.Rgb(111, 184, 102);
            var grassShift = (xOffset - 0.5f) * 12f * unit;
            for (var y = height * 0.77f; y < height; y += 12f * unit)
                for (var x = -12f * unit; x < width + 12f * unit; x += 16f * unit)
                    canvas.DrawRect(x + grassShift, y, x + grassShift + 5f * unit, y + 3f * unit, _paint);

            DrawPalm(canvas, width * 0.13f - (xOffset - 0.5f) * 28f * unit,
                height * 0.77f, unit * 1.25f);
            DrawPalm(canvas, width * 0.88f - (xOffset - 0.5f) * 28f * unit,
                height * 0.79f, unit * 0.9f);
            return height * 0.78f;
        }

        private float DrawInstalledMap(Canvas canvas, int width, int height, int scene)
        {
            const int columns = 10;
            const int rows = 8;
            const int tileSize = 16;
            const int atlasStride = tileSize + 2;
            if (!LiveWallpaperSceneSelection.TryGetTileOrigin(
                    scene, out var originTileX, out var originTileY))
                return height * 0.78f;
            var map = _overworldMap.Map;
            var tileset = _overworldMap.Bitmap;
            var destinationTileSize = MathF.Ceiling(width / (float)columns);
            var top = height - rows * destinationTileSize;
            var tilesPerRow = tileset.Width / atlasStride;
            if (tilesPerRow <= 0)
                return height * 0.78f;

            for (var layer = 0; layer < map.DrawableDepth; layer++)
            {
                for (var y = 0; y < rows; y++)
                {
                    for (var x = 0; x < columns; x++)
                    {
                        var tile = map.GetTile(originTileX + x, originTileY + y, layer);
                        if (tile < 0)
                            continue;
                        var sourceX = tile % tilesPerRow * atlasStride + 1;
                        var sourceY = tile / tilesPerRow * atlasStride + 1;
                        if (sourceX + tileSize > tileset.Width ||
                            sourceY + tileSize > tileset.Height)
                            continue;
                        var source = new Rect(sourceX, sourceY,
                            sourceX + tileSize, sourceY + tileSize);
                        var destination = new RectF(
                            x * destinationTileSize,
                            top + y * destinationTileSize,
                            (x + 1) * destinationTileSize,
                            top + (y + 1) * destinationTileSize);
                        canvas.DrawBitmap(tileset, source, destination, _paint);
                    }
                }
            }

            return top + destinationTileSize * 5.6f;
        }

        private void DrawLink(
            Canvas canvas, int width, float groundY, long elapsed, float unit, bool animated,
            int activity)
        {
            var state = LiveWallpaperLinkActivity.Resolve(activity, elapsed, animated);
            if (!state.Visible)
                return;
            var asset = state.Walking
                ? _linkWalking ?? _linkStanding
                : _linkStanding ?? _linkWalking;
            if (asset == null)
                return;
            var scale = Math.Max(2f, unit * 2.2f);
            var frame = asset.Animation.GetFrame(elapsed);
            var spriteWidth = frame.Width * scale;
            var centerX = -spriteWidth * 0.5f + state.Journey * (width + spriteWidth);
            DrawSpriteAt(canvas, asset, elapsed, centerX, groundY, scale);
        }

        private void DrawFeaturedCharacter(
            Canvas canvas,
            int width,
            float groundY,
            long elapsed,
            float xOffset,
            float unit,
            int featuredCharacter)
        {
            var selection = Math.Clamp(featuredCharacter, 0, 3);
            if (selection == 3)
                selection = (int)((elapsed / 30000L) % 3L);
            var asset = selection switch
            {
                1 => _bowWow,
                2 => _rooster,
                _ => _marin
            };
            if (asset == null)
                return;
            var centerX = width * 0.78f - (xOffset - 0.5f) * 20f * unit;
            var scale = selection == 0 ? 2.05f : 1.9f;
            DrawSpriteAt(canvas, asset, elapsed, centerX, groundY,
                Math.Max(2f, unit * scale));
        }

        private void DrawButterflies(
            Canvas canvas, int width, float groundY, long elapsed, float unit, bool animated)
        {
            if (_butterfly == null)
                return;
            for (var index = 0; index < 3; index++)
            {
                var phase = index * 2.1f;
                var motion = animated ? elapsed / 850f + phase : phase;
                var centerX = width * (0.28f + index * 0.17f) + MathF.Sin(motion) * 12f * unit;
                var centerY = groundY - (14f + index * 6f) * unit +
                              MathF.Cos(motion * 1.3f) * 7f * unit;
                DrawSpriteAt(canvas, _butterfly, elapsed + index * 90L,
                    centerX, centerY, Math.Max(1.5f, unit * 1.35f));
            }
        }

        private void DrawOwl(
            Canvas canvas, int width, int height, long elapsed, float unit, bool animated)
        {
            if (_owl == null)
                return;
            var frame = _owl.Animation.GetFrame(elapsed);
            var scale = Math.Max(1.5f, unit * 1.4f);
            var spriteWidth = frame.Width * scale;
            var journey = animated ? (elapsed % 22000L) / 22000f : 0.68f;
            var centerX = width + spriteWidth * 0.5f - journey * (width + spriteWidth);
            var centerY = height * 0.29f + MathF.Sin(elapsed / 750f) * 8f * unit;
            DrawSpriteAt(canvas, _owl, elapsed, centerX, centerY, scale);
        }

        private void DrawSpriteAt(
            Canvas canvas,
            SpriteAsset asset,
            long elapsed,
            float centerX,
            float bottomY,
            float scale)
        {
            if (asset?.Bitmap == null || asset.Animation == null)
                return;
            var frame = asset.Animation.GetFrame(elapsed);
            if (frame.X < 0 || frame.Y < 0 ||
                frame.X + frame.Width > asset.Bitmap.Width ||
                frame.Y + frame.Height > asset.Bitmap.Height)
                return;

            var spriteWidth = frame.Width * scale;
            var spriteHeight = frame.Height * scale;
            var source = new Rect(frame.X, frame.Y, frame.X + frame.Width, frame.Y + frame.Height);
            var destination = new RectF(
                centerX - spriteWidth * 0.5f - frame.OffsetX * scale,
                bottomY - spriteHeight - frame.OffsetY * scale,
                centerX + spriteWidth * 0.5f - frame.OffsetX * scale,
                bottomY - frame.OffsetY * scale);
            var save = canvas.Save();
            if (frame.MirroredHorizontally)
                canvas.Scale(-1f, 1f, destination.CenterX(), destination.CenterY());
            if (frame.MirroredVertically)
                canvas.Scale(1f, -1f, destination.CenterX(), destination.CenterY());
            canvas.DrawBitmap(asset.Bitmap, source, destination, _paint);
            canvas.RestoreToCount(save);
        }

        private void DrawTouchEffect(Canvas canvas, long elapsed, float unit, bool animated)
        {
            if (!animated || _touchAt == long.MinValue)
                return;
            var age = elapsed - _touchAt;
            if (age is < 0 or > 1100)
                return;
            var progress = age / 1100f;
            _smoothPaint.SetStyle(Paint.Style.Stroke);
            _smoothPaint.StrokeWidth = Math.Max(1f, 2f * unit * (1f - progress));
            _smoothPaint.Color = Color.Argb((int)(220 * (1f - progress)), 255, 236, 129);
            canvas.DrawCircle(_touchX, _touchY, (8f + 42f * progress) * unit, _smoothPaint);
            _smoothPaint.SetStyle(Paint.Style.Fill);
            for (var index = 0; index < 8; index++)
            {
                var angle = index * MathF.PI / 4f + progress;
                var radius = (10f + progress * 34f) * unit;
                canvas.DrawCircle(_touchX + MathF.Cos(angle) * radius,
                    _touchY + MathF.Sin(angle) * radius, 1.5f * unit, _smoothPaint);
            }
        }

        private void DrawCloud(Canvas canvas, float x, float y, float scale)
        {
            _paint.Color = Color.Argb(165, 255, 224, 211);
            canvas.DrawRect(x, y + 7f * scale, x + 48f * scale, y + 15f * scale, _paint);
            canvas.DrawRect(x + 8f * scale, y + 3f * scale, x + 33f * scale, y + 15f * scale, _paint);
            canvas.DrawRect(x + 17f * scale, y, x + 27f * scale, y + 15f * scale, _paint);
        }

        private void DrawPalm(Canvas canvas, float x, float groundY, float scale)
        {
            _paint.Color = Color.Rgb(113, 72, 51);
            canvas.DrawRect(x - 3f * scale, groundY - 45f * scale,
                x + 4f * scale, groundY, _paint);
            _paint.Color = Color.Rgb(39, 111, 73);
            canvas.DrawRect(x - 24f * scale, groundY - 52f * scale,
                x + 26f * scale, groundY - 45f * scale, _paint);
            canvas.DrawRect(x - 13f * scale, groundY - 62f * scale,
                x + 15f * scale, groundY - 48f * scale, _paint);
            canvas.DrawRect(x - 4f * scale, groundY - 68f * scale,
                x + 6f * scale, groundY - 50f * scale, _paint);
        }

        private SpriteAsset LoadSprite(
            Context context,
            string relativeAnimationPath,
            IEnumerable<string> preferredAnimations)
        {
            if (!LadxhdWallpaperAssets.TryResolve(context, relativeAnimationPath,
                    preferredAnimations, out var animation, out var spritePath, out _))
                return null;
            try
            {
                if (!_spriteSheets.TryGetValue(spritePath, out var bitmap))
                {
                    bitmap = BitmapFactory.DecodeFile(spritePath) ??
                             throw new InvalidDataException(
                                 "The wallpaper sprite sheet could not be decoded.");
                    _spriteSheets.Add(spritePath, bitmap);
                }
                return new SpriteAsset(bitmap, animation);
            }
            catch
            {
                return null;
            }
        }

        private MapAsset LoadOverworldMap(Context context)
        {
            if (!LadxhdWallpaperAssets.TryResolveOverworldMap(
                    context, out var map, out var tilesetPath, out _))
                return null;
            try
            {
                if (!_spriteSheets.TryGetValue(tilesetPath, out var bitmap))
                {
                    bitmap = BitmapFactory.DecodeFile(tilesetPath) ??
                             throw new InvalidDataException(
                                 "The wallpaper tileset could not be decoded.");
                    _spriteSheets.Add(tilesetPath, bitmap);
                }
                return new MapAsset(bitmap, map);
            }
            catch
            {
                return null;
            }
        }

        private void Fill(Canvas canvas, Color color, float left, float top, float right, float bottom)
        {
            _paint.Color = color;
            canvas.DrawRect(left, top, right, bottom, _paint);
        }

        public void Dispose()
        {
            foreach (var bitmap in _spriteSheets.Values)
                bitmap.Dispose();
            _spriteSheets.Clear();
            _paint.Dispose();
            _smoothPaint.Dispose();
        }
    }
}
