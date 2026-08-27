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
                Text = "A silent, battery-aware Koholint scene. It uses Link's animation from your locally generated game data without starting gameplay, saves, or Archipelago networking.",
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
                    ? "Game data ready: the wallpaper will use the LADXHD Link sprite."
                    : $"Game data unavailable: {reason} The scenery will still render, but Link will appear after setup.",
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
        private static readonly string[] PreferredAnimations =
            ["walk_2", "walk_0", "walk_1", "walk_3", "stand_2", "stand_0"];

        public static bool TryResolve(
            Context context,
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
                var animationPath = FilePath.Combine(dataRoot, "Animations", "link0.ani");
                using var reader = File.OpenText(animationPath);
                if (!LiveWallpaperAnimation.TryLoad(reader, PreferredAnimations, out animation))
                    throw new InvalidDataException("Link's wallpaper animation is unavailable.");

                if (!LiveWallpaperAnimation.TryGetSpriteRelativeCandidates(
                        animation.SpritePath, out var relativeCandidates))
                    throw new InvalidDataException("Link's wallpaper sprite path is invalid.");
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

                throw new FileNotFoundException("Link's wallpaper sprite is unavailable.");
            }
            catch (Exception exception)
            {
                animation = null;
                spritePath = null;
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
                            LadxhdWallpaperPreferences.IsAnimated(_service));
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
                var delay = immediate ? 0L : 1000L / LadxhdWallpaperPreferences.GetFrameRate(_service);
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
        private readonly Paint _paint = new Paint { AntiAlias = false, FilterBitmap = false };
        private readonly Paint _smoothPaint = new Paint { AntiAlias = true };
        private readonly Random _random = new Random(0x4C415844);
        private readonly List<(float X, float Y, float Phase)> _stars = [];
        private Bitmap _linkBitmap;
        private LiveWallpaperAnimation _linkAnimation;
        private string _assetError;
        private float _touchX;
        private float _touchY;
        private long _touchAt = long.MinValue;

        public LadxhdWallpaperScene(Context context)
        {
            for (var index = 0; index < 42; index++)
                _stars.Add(((float)_random.NextDouble(), (float)_random.NextDouble() * 0.48f,
                    (float)_random.NextDouble() * MathF.PI * 2f));
            LoadLinkAnimation(context);
        }

        public void OnTouch(float x, float y, long elapsed)
        {
            _touchX = x;
            _touchY = y;
            _touchAt = elapsed;
        }

        public void Draw(Canvas canvas, long elapsed, float xOffset, bool animated)
        {
            var width = canvas.Width;
            var height = canvas.Height;
            if (width <= 0 || height <= 0)
                return;
            var time = animated ? elapsed : 0L;
            var unit = Math.Max(1f, Math.Min(width, height) / 240f);

            DrawSky(canvas, width, height, time, xOffset, unit);
            DrawIsland(canvas, width, height, time, xOffset, unit);
            DrawLink(canvas, width, height, time, unit, animated);
            DrawTouchEffect(canvas, time, unit, animated);
        }

        private void DrawSky(Canvas canvas, int width, int height, long elapsed, float xOffset, float unit)
        {
            var horizon = height * 0.58f;
            Fill(canvas, Color.Rgb(28, 35, 74), 0, 0, width, horizon * 0.45f);
            Fill(canvas, Color.Rgb(79, 78, 122), 0, horizon * 0.45f, width, horizon * 0.72f);
            Fill(canvas, Color.Rgb(231, 126, 103), 0, horizon * 0.72f, width, horizon);

            var starPulse = elapsed / 700f;
            foreach (var star in _stars)
            {
                var alpha = 120 + (int)(100 * (MathF.Sin(starPulse + star.Phase) * 0.5f + 0.5f));
                _paint.Color = Color.Argb(alpha, 255, 244, 196);
                var size = unit * (star.Phase > MathF.PI ? 1.5f : 1f);
                canvas.DrawRect(star.X * width, star.Y * height, star.X * width + size,
                    star.Y * height + size, _paint);
            }

            _smoothPaint.Color = Color.Rgb(255, 211, 119);
            canvas.DrawCircle(width * 0.74f, horizon * 0.66f, 22f * unit, _smoothPaint);

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

        private void DrawIsland(Canvas canvas, int width, int height, long elapsed, float xOffset, float unit)
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
        }

        private void DrawLink(
            Canvas canvas, int width, int height, long elapsed, float unit, bool animated)
        {
            if (_linkBitmap == null || _linkAnimation == null)
                return;
            var frame = _linkAnimation.GetFrame(elapsed);
            if (frame.X < 0 || frame.Y < 0 || frame.X + frame.Width > _linkBitmap.Width ||
                frame.Y + frame.Height > _linkBitmap.Height)
                return;

            var scale = Math.Max(2f, unit * 2.2f);
            var spriteWidth = frame.Width * scale;
            var spriteHeight = frame.Height * scale;
            var journey = animated ? (elapsed % 14000L) / 14000f : 0.5f;
            var x = -spriteWidth + journey * (width + spriteWidth * 2f);
            var y = height * 0.78f - spriteHeight - frame.OffsetY * scale;
            var source = new Rect(frame.X, frame.Y, frame.X + frame.Width, frame.Y + frame.Height);
            var destination = new RectF(
                x - frame.OffsetX * scale, y,
                x - frame.OffsetX * scale + spriteWidth, y + spriteHeight);
            var save = canvas.Save();
            if (frame.MirroredHorizontally)
                canvas.Scale(-1f, 1f, destination.CenterX(), destination.CenterY());
            if (frame.MirroredVertically)
                canvas.Scale(1f, -1f, destination.CenterX(), destination.CenterY());
            canvas.DrawBitmap(_linkBitmap, source, destination, _paint);
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

        private void LoadLinkAnimation(Context context)
        {
            if (!LadxhdWallpaperAssets.TryResolve(
                    context, out _linkAnimation, out var spritePath, out _assetError))
                return;
            try
            {
                _linkBitmap = BitmapFactory.DecodeFile(spritePath) ??
                    throw new InvalidDataException("Link's wallpaper sprite could not be decoded.");
            }
            catch (Exception exception)
            {
                _linkAnimation = null;
                _linkBitmap?.Dispose();
                _linkBitmap = null;
                _assetError = exception.Message;
            }
        }

        private void Fill(Canvas canvas, Color color, float left, float top, float right, float bottom)
        {
            _paint.Color = color;
            canvas.DrawRect(left, top, right, bottom, _paint);
        }

        public void Dispose()
        {
            _linkBitmap?.Dispose();
            _paint.Dispose();
            _smoothPaint.Dispose();
        }
    }
}
