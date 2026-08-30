using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.OS;
using Android.Service.Wallpaper;
using Android.Views;
using Android.Widget;
using FilePath = System.IO.Path;

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
        private const string WildlifeScheduleKey = "wildlife_schedule";
        private const string CharacterPositionKey = "character_position";
        private const string FrameRateKey = "frame_rate";
        private const string FollowLoadingZonesKey = "follow_loading_zones";

        public static bool IsAnimated(Context context) =>
            context.GetSharedPreferences(PreferencesName, FileCreationMode.Private)
                ?.GetBoolean(AnimateKey, true) ?? true;

        public static int GetFrameRate(Context context)
        {
            var value = context.GetSharedPreferences(PreferencesName, FileCreationMode.Private)
                ?.GetInt(FrameRateKey, 30) ?? 30;
            return value == 60 ? 60 : value <= 15 ? 15 : 30;
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
            return Math.Clamp(value, 0, 4);
        }

        public static void SetFeaturedCharacter(Context context, int value) =>
            context.GetSharedPreferences(PreferencesName, FileCreationMode.Private)
                ?.Edit()?.PutInt(FeaturedCharacterKey, Math.Clamp(value, 0, 4))?.Apply();

        public static int GetScene(Context context)
        {
            var value = context.GetSharedPreferences(PreferencesName, FileCreationMode.Private)
                ?.GetInt(SceneKey, 1) ?? 1;
            return value <= 0
                ? 1
                : Math.Clamp(value, 1, LiveWallpaperSceneSelection.MaximumSelection);
        }

        public static void SetScene(Context context, int value) =>
            context.GetSharedPreferences(PreferencesName, FileCreationMode.Private)
                ?.Edit()?.PutInt(SceneKey,
                    Math.Clamp(value, 1, LiveWallpaperSceneSelection.MaximumSelection))?.Apply();

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

        public static int GetWildlifeSchedule(Context context)
        {
            var value = context.GetSharedPreferences(PreferencesName, FileCreationMode.Private)
                ?.GetInt(WildlifeScheduleKey, 0) ?? 0;
            return Math.Clamp(value, 0, 1);
        }

        public static void SetWildlifeSchedule(Context context, int value) =>
            context.GetSharedPreferences(PreferencesName, FileCreationMode.Private)
                ?.Edit()?.PutInt(WildlifeScheduleKey, Math.Clamp(value, 0, 1))?.Apply();

        public static int GetCharacterPosition(Context context)
        {
            var value = context.GetSharedPreferences(PreferencesName, FileCreationMode.Private)
                ?.GetInt(CharacterPositionKey, 0) ?? 0;
            return Math.Clamp(value, 0, 3);
        }

        public static void SetCharacterPosition(Context context, int value) =>
            context.GetSharedPreferences(PreferencesName, FileCreationMode.Private)
                ?.Edit()?.PutInt(CharacterPositionKey, Math.Clamp(value, 0, 3))?.Apply();

        public static void SetFrameRate(Context context, int value) =>
            context.GetSharedPreferences(PreferencesName, FileCreationMode.Private)
                ?.Edit()?.PutInt(FrameRateKey, value == 60 ? 60 : value <= 15 ? 15 : 30)?.Apply();

        public static bool FollowLoadingZones(Context context) =>
            context.GetSharedPreferences(PreferencesName, FileCreationMode.Private)
                ?.GetBoolean(FollowLoadingZonesKey, false) ?? false;

        public static void SetFollowLoadingZones(Context context, bool value) =>
            context.GetSharedPreferences(PreferencesName, FileCreationMode.Private)
                ?.Edit()?.PutBoolean(FollowLoadingZonesKey, value)?.Apply();

        public static bool ApplyPreset(Context context, int preset)
        {
            if (!LiveWallpaperPresets.TryResolve(preset, out var value))
                return false;
            var editor = context.GetSharedPreferences(
                PreferencesName, FileCreationMode.Private)?.Edit();
            if (editor == null)
                return false;
            editor.PutBoolean(AnimateKey, true);
            editor.PutBoolean(IslandLifeKey, true);
            editor.PutInt(SceneKey, value.Scene);
            editor.PutInt(TimeOfDayKey, value.TimeOfDay);
            editor.PutInt(FeaturedCharacterKey, value.FeaturedCharacter);
            editor.PutInt(CharacterPositionKey, value.CharacterPosition);
            editor.PutInt(LinkActivityKey, value.LinkActivity);
            editor.PutInt(WildlifeScheduleKey, value.WildlifeSchedule);
            return editor.Commit();
        }
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
                    : $"Game data unavailable: {reason} The wallpaper remains blank until the original game data is prepared locally.",
                TextSize = 15f
            };
            layout.AddView(status);

            var presetLabel = new TextView(this)
            {
                Text = "Quick preset",
                TextSize = 17f,
                Enabled = assetReady
            };
            var presetLabelParams = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
            presetLabelParams.SetMargins(0, Dp(20), 0, 0);
            layout.AddView(presetLabel, presetLabelParams);
            var preset = new Spinner(this) { Enabled = assetReady };
            var presetAdapter = new ArrayAdapter<string>(this,
                global::Android.Resource.Layout.SimpleSpinnerItem,
                ["Custom", "Mabe Sunset", "Forest Night", "Island Journey"]);
            presetAdapter.SetDropDownViewResource(
                global::Android.Resource.Layout.SimpleSpinnerDropDownItem);
            preset.Adapter = presetAdapter;
            preset.ItemSelected += (_, args) =>
            {
                if (args.Position > 0 && LadxhdWallpaperPreferences.ApplyPreset(this, args.Position))
                    Recreate();
            };
            var presetParams = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
            presetParams.SetMargins(0, 0, 0, Dp(12));
            layout.AddView(preset, presetParams);

            var animate = new global::Android.Widget.Switch(this)
            {
                Text = "Animate Link, island characters, and wildlife",
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

            var wildlifeLabel = new TextView(this)
            {
                Text = "Wildlife schedule",
                TextSize = 17f,
                Enabled = assetReady
            };
            layout.AddView(wildlifeLabel);
            var wildlifeSchedule = new Spinner(this) { Enabled = assetReady };
            var wildlifeAdapter = new ArrayAdapter<string>(this,
                global::Android.Resource.Layout.SimpleSpinnerItem,
                ["Follow day and night", "Always show butterflies and owl"]);
            wildlifeAdapter.SetDropDownViewResource(
                global::Android.Resource.Layout.SimpleSpinnerDropDownItem);
            wildlifeSchedule.Adapter = wildlifeAdapter;
            wildlifeSchedule.SetSelection(LadxhdWallpaperPreferences.GetWildlifeSchedule(this));
            wildlifeSchedule.ItemSelected += (_, args) =>
                LadxhdWallpaperPreferences.SetWildlifeSchedule(this, args.Position);
            var wildlifeParams = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
            wildlifeParams.SetMargins(0, 0, 0, Dp(12));
            layout.AddView(wildlifeSchedule, wildlifeParams);

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
                ["Marin", "BowWow", "Rooster", "Rotate automatically", "Match location"]);
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

            var positionLabel = new TextView(this)
            {
                Text = "Featured character position",
                TextSize = 17f,
                Enabled = assetReady
            };
            layout.AddView(positionLabel);
            var characterPosition = new Spinner(this) { Enabled = assetReady };
            var positionAdapter = new ArrayAdapter<string>(this,
                global::Android.Resource.Layout.SimpleSpinnerItem,
                ["Match location", "Left", "Center", "Right"]);
            positionAdapter.SetDropDownViewResource(
                global::Android.Resource.Layout.SimpleSpinnerDropDownItem);
            characterPosition.Adapter = positionAdapter;
            characterPosition.SetSelection(LadxhdWallpaperPreferences.GetCharacterPosition(this));
            characterPosition.ItemSelected += (_, args) =>
                LadxhdWallpaperPreferences.SetCharacterPosition(this, args.Position);
            var positionParams = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
            positionParams.SetMargins(0, 0, 0, Dp(12));
            layout.AddView(characterPosition, positionParams);

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

            var followLoadingZones = new global::Android.Widget.Switch(this)
            {
                Text = "Follow Link through overworld loading zones",
                Checked = LadxhdWallpaperPreferences.FollowLoadingZones(this),
                Enabled = assetReady
            };
            followLoadingZones.CheckedChange += (_, args) =>
                LadxhdWallpaperPreferences.SetFollowLoadingZones(this, args.IsChecked);
            var followLoadingZonesParams = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
            followLoadingZonesParams.SetMargins(0, 0, 0, Dp(12));
            layout.AddView(followLoadingZones, followLoadingZonesParams);

            var sceneLabel = new TextView(this)
            {
                Text = "Wallpaper location / starting point for island exploration",
                TextSize = 17f,
                Enabled = assetReady
            };
            layout.AddView(sceneLabel);
            var scene = new Spinner(this) { Enabled = assetReady };
            int[] sceneValues =
                [1, 2, 3, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15,
                 LiveWallpaperSceneSelection.RotationSelection];
            var sceneAdapter = new ArrayAdapter<string>(this,
                global::Android.Resource.Layout.SimpleSpinnerItem,
                ["Mabe Village", "Toronbo Shores", "Mysterious Forest", "Kanalet Castle",
                 "Animal Village", "Wind Fish's Egg", "Martha's Bay", "Ukuku Prairie",
                 "Cemetery", "Goponga Swamp", "Rapids Ride", "Eastern Tal Tal Heights",
                 "Yarna Desert", "Face Shrine", "Rotate locations"]);
            sceneAdapter.SetDropDownViewResource(
                global::Android.Resource.Layout.SimpleSpinnerDropDownItem);
            scene.Adapter = sceneAdapter;
            var selectedSceneIndex = Array.IndexOf(
                sceneValues, LadxhdWallpaperPreferences.GetScene(this));
            scene.SetSelection(Math.Max(0, selectedSceneIndex));
            scene.ItemSelected += (_, args) =>
                LadxhdWallpaperPreferences.SetScene(this, sceneValues[args.Position]);
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

            var tapHint = new TextView(this)
            {
                Text = "Tap anywhere on the wallpaper to send Link there.",
                TextSize = 15f
            };
            var tapParams = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
            tapParams.SetMargins(0, 0, 0, Dp(12));
            layout.AddView(tapHint, tapParams);

            var rateLabel = new TextView(this) { Text = "Animation frame rate", TextSize = 17f };
            layout.AddView(rateLabel);
            var rates = new RadioGroup(this) { Orientation = Orientation.Vertical };
            var saver = new RadioButton(this) { Text = "Battery saver (15 FPS)", Id = View.GenerateViewId() };
            var smooth = new RadioButton(this) { Text = "Smooth (30 FPS)", Id = View.GenerateViewId() };
            var high = new RadioButton(this) { Text = "High FPS (60 FPS)", Id = View.GenerateViewId() };
            rates.AddView(saver);
            rates.AddView(smooth);
            rates.AddView(high);
            var selectedFrameRate = LadxhdWallpaperPreferences.GetFrameRate(this);
            rates.Check(selectedFrameRate == 60 ? high.Id : selectedFrameRate <= 15 ? saver.Id : smooth.Id);
            rates.CheckedChange += (_, args) =>
                LadxhdWallpaperPreferences.SetFrameRate(
                    this, args.CheckedId == high.Id ? 60 : args.CheckedId == saver.Id ? 15 : 30);
            layout.AddView(rates);

            var frameRateWarning = new TextView(this)
            {
                Text = "Battery warning: High FPS uses significantly more power and may make your phone warmer. The wallpaper still pauses when it is not visible.",
                TextSize = 14f
            };
            frameRateWarning.SetTypeface(null, TypefaceStyle.Bold);
            var warningParams = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
            warningParams.SetMargins(0, Dp(4), 0, 0);
            layout.AddView(frameRateWarning, warningParams);

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
            out string reason) =>
            TryResolveMap(context, "overworld.map", out map, out tilesetPath,
                out reason);

        public static bool TryResolveMap(
            Context context,
            string mapName,
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
                mapName = mapName?.Trim();
                if (string.IsNullOrWhiteSpace(mapName) ||
                    !mapName.EndsWith(".map", StringComparison.OrdinalIgnoreCase) ||
                    mapName.IndexOfAny([
                        FilePath.DirectorySeparatorChar,
                        FilePath.AltDirectorySeparatorChar
                    ]) >= 0 ||
                    mapName.Contains("..", StringComparison.Ordinal))
                    throw new InvalidDataException("The wallpaper map name is invalid.");
                var dataRoot = FilePath.GetFullPath(FilePath.Combine(root, "Data"));
                var mapPath = FilePath.Combine(dataRoot, "Maps", mapName);
                using var reader = File.OpenText(mapPath);
                if (!LiveWallpaperMap.TryLoad(reader, out map))
                    throw new InvalidDataException("The installed wallpaper map is unavailable.");

                var tilesetRoot = FilePath.GetFullPath(
                    FilePath.Combine(dataRoot, "Maps", "Tilesets"));
                var candidate = FilePath.GetFullPath(FilePath.Combine(tilesetRoot,
                    map.TilesetPath.Replace('/', FilePath.DirectorySeparatorChar)));
                var rootPrefix = tilesetRoot.TrimEnd(FilePath.DirectorySeparatorChar) +
                                 FilePath.DirectorySeparatorChar;
                if (!candidate.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase) ||
                    !File.Exists(candidate))
                    throw new FileNotFoundException("The installed wallpaper tileset is unavailable.");

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

        public static bool TryResolveAtlasSprite(
            Context context,
            string atlasName,
            string spriteId,
            out LiveWallpaperAtlasEntry entry,
            out string spritePath,
            out string reason)
        {
            entry = default;
            spritePath = null;
            reason = null;
            if (!AndroidAssetInstallation.TryGetActiveRoot(context, out var root, out reason))
                return false;

            try
            {
                if (!LiveWallpaperAnimation.TryNormalizeRelativePath(
                        atlasName, out var normalizedAtlasName) ||
                    normalizedAtlasName.Contains(".", StringComparison.Ordinal))
                    throw new InvalidDataException("The wallpaper atlas name is invalid.");
                var objectsRoot = FilePath.GetFullPath(
                    FilePath.Combine(root, "Data", "Map Objects"));
                var atlasPath = FilePath.GetFullPath(FilePath.Combine(
                    objectsRoot, normalizedAtlasName + ".atlas"));
                var candidateSpritePath = FilePath.GetFullPath(FilePath.Combine(
                    objectsRoot, normalizedAtlasName + ".png"));
                var rootPrefix = objectsRoot.TrimEnd(FilePath.DirectorySeparatorChar) +
                                 FilePath.DirectorySeparatorChar;
                if (!atlasPath.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase) ||
                    !candidateSpritePath.StartsWith(rootPrefix,
                        StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("The wallpaper atlas path is invalid.");
                using var reader = File.OpenText(atlasPath);
                if (!LiveWallpaperAtlas.TryLoad(reader, spriteId, out entry))
                    throw new InvalidDataException("The requested wallpaper atlas sprite is unavailable.");
                if (!File.Exists(candidateSpritePath))
                    throw new FileNotFoundException("The wallpaper atlas image is unavailable.");
                spritePath = candidateSpritePath;
                return true;
            }
            catch (Exception exception)
            {
                entry = default;
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
        Process = ":wallpaper",
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
            private int _xPixelOffset;
            private int _yPixelOffset;
            private long _startedAt;
            private long _nextFrameDeadline;
            private int _publishedColorRevision = -1;

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
                _nextFrameDeadline = 0L;
                _handler.RemoveCallbacks(_drawRunnable);
                base.OnSurfaceDestroyed(holder);
            }

            public override void OnOffsetsChanged(
                float xOffset, float yOffset, float xOffsetStep, float yOffsetStep,
                int xPixelOffset, int yPixelOffset)
            {
                _xOffset = Math.Clamp(xOffset, 0f, 1f);
                _xPixelOffset = xPixelOffset;
                _yPixelOffset = yPixelOffset;
                ScheduleFrame(immediate: true);
            }

            public override void OnTouchEvent(MotionEvent e)
            {
                if (e?.Action == MotionEventActions.Down)
                {
                    _scene?.TrySetLinkDestination(
                        e.GetX(), e.GetY());
                    ScheduleFrame(immediate: true);
                }
                base.OnTouchEvent(e);
            }

            public override WallpaperColors OnComputeColors() =>
                OperatingSystem.IsAndroidVersionAtLeast(27)
                    ? _scene?.ComputeWallpaperColors()
                    : null;

            public override void OnDestroy()
            {
                _visible = false;
                _surfaceReady = false;
                _nextFrameDeadline = 0L;
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
                    if (OperatingSystem.IsAndroidVersionAtLeast(26))
                    {
                        try
                        {
                            canvas = SurfaceHolder?.LockHardwareCanvas();
                        }
                        catch
                        {
                            // Some wallpaper hosts expose the API but do not
                            // provide a hardware surface. Keep the software path.
                            canvas = null;
                        }
                    }
                    canvas ??= SurfaceHolder?.LockCanvas();
                    if (canvas != null)
                    {
                        var elapsed = SystemClock.ElapsedRealtime() - _startedAt;
                        _scene.Draw(canvas, elapsed, _xOffset,
                            LadxhdWallpaperPreferences.IsAnimated(_service),
                            LadxhdWallpaperPreferences.ShowIslandLife(_service),
                            LadxhdWallpaperPreferences.GetFeaturedCharacter(_service),
                            LadxhdWallpaperPreferences.GetScene(_service),
                            LadxhdWallpaperPreferences.GetTimeOfDay(_service),
                            LadxhdWallpaperPreferences.GetLinkActivity(_service),
                            LadxhdWallpaperPreferences.GetWildlifeSchedule(_service),
                            LadxhdWallpaperPreferences.GetCharacterPosition(_service),
                            LadxhdWallpaperPreferences.FollowLoadingZones(_service));
                    }
                }
                finally
                {
                    if (canvas != null)
                        SurfaceHolder?.UnlockCanvasAndPost(canvas);
                }
                if (OperatingSystem.IsAndroidVersionAtLeast(27) &&
                    _scene != null &&
                    _publishedColorRevision != _scene.WallpaperColorRevision)
                {
                    _publishedColorRevision = _scene.WallpaperColorRevision;
                    NotifyColorsChanged();
                }
                ScheduleFrame(immediate: false);
            }

            private void ScheduleFrame(bool immediate)
            {
                _handler.RemoveCallbacks(_drawRunnable);
                if (!_visible || !_surfaceReady)
                {
                    _nextFrameDeadline = 0L;
                    return;
                }
                var now = SystemClock.ElapsedRealtime();
                long delay;
                if (immediate)
                {
                    _nextFrameDeadline = now;
                    delay = 0L;
                }
                else
                {
                    delay = LiveWallpaperFrameScheduler.GetCompensatedDelayMilliseconds(
                        now,
                        _nextFrameDeadline,
                        LadxhdWallpaperPreferences.IsAnimated(_service),
                        LadxhdWallpaperPreferences.GetFrameRate(_service),
                        out _nextFrameDeadline);
                }
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
                EngineAnimation = animation.CreateEngineAnimation();
            }

            public Bitmap Bitmap { get; }
            public LiveWallpaperAnimation Animation { get; }
            public LiveWallpaperEngineAnimation EngineAnimation { get; }
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

        private sealed class AtlasSpriteAsset
        {
            public AtlasSpriteAsset(Bitmap bitmap, LiveWallpaperAtlasEntry entry)
            {
                Bitmap = bitmap;
                Entry = entry;
            }

            public Bitmap Bitmap { get; }
            public LiveWallpaperAtlasEntry Entry { get; }
        }

        private sealed class EnemyAssetSet
        {
            public SpriteAsset[] Walk { get; } = new SpriteAsset[4];
            public SpriteAsset[] Idle { get; } = new SpriteAsset[4];
            public SpriteAsset[] Attack { get; } = new SpriteAsset[4];
            public SpriteAsset[] Spawn { get; } = new SpriteAsset[4];
            public SpriteAsset[] Leave { get; } = new SpriteAsset[4];

            public SpriteAsset Resolve(LiveWallpaperEnemyState state)
            {
                var direction = Math.Clamp(state.Direction, 0, 3);
                var preferred = state.Action switch
                {
                    LiveWallpaperEnemyAction.Attack => Attack,
                    LiveWallpaperEnemyAction.Spawn => Spawn,
                    LiveWallpaperEnemyAction.Leave => Leave,
                    LiveWallpaperEnemyAction.Walk => Walk,
                    _ => Idle
                };
                return preferred[direction] ?? Walk[direction] ?? Idle[direction] ??
                       preferred.FirstOrDefault(asset => asset != null) ??
                       Walk.FirstOrDefault(asset => asset != null) ??
                       Idle.FirstOrDefault(asset => asset != null);
            }
        }

        private enum PlayerLayerDrawKind
        {
            Decoration,
            Lamp,
            Actor,
            Enemy,
            EnemyProjectile,
            Link
        }

        private readonly struct PlayerLayerDrawEntry : IComparable<PlayerLayerDrawEntry>
        {
            public PlayerLayerDrawEntry(
                PlayerLayerDrawKind kind, int index, float positionY, int sequence)
            {
                Kind = kind;
                Index = index;
                PositionY = positionY;
                Sequence = sequence;
            }

            public PlayerLayerDrawKind Kind { get; }
            public int Index { get; }
            public float PositionY { get; }
            private int Sequence { get; }

            public int CompareTo(PlayerLayerDrawEntry other)
            {
                var comparison = PositionY.CompareTo(other.PositionY);
                return comparison != 0 ? comparison : Sequence.CompareTo(other.Sequence);
            }
        }

        // Keep bitmap opacity independent from the translucent lighting/fade overlays. Android
        // Paint retains its alpha between frames, so sharing one Paint can make all game art dark.
        private readonly Paint _bitmapPaint = new Paint
        {
            AntiAlias = false,
            FilterBitmap = false,
            Color = Color.White,
            Alpha = 255
        };
        private readonly Paint _overlayPaint = new Paint
        {
            AntiAlias = false,
            FilterBitmap = false
        };
        private readonly Dictionary<string, Bitmap> _spriteSheets =
            new Dictionary<string, Bitmap>(StringComparer.OrdinalIgnoreCase);
        private readonly SpriteAsset[] _linkWalking = new SpriteAsset[4];
        private readonly SpriteAsset[] _linkStanding = new SpriteAsset[4];
        private readonly SpriteAsset[] _linkJumping = new SpriteAsset[4];
        private readonly SpriteAsset[] _linkSwimming = new SpriteAsset[4];
        private readonly SpriteAsset[] _linkPushing = new SpriteAsset[4];
        private readonly SpriteAsset[] _linkCarrying = new SpriteAsset[4];
        private readonly SpriteAsset[] _linkLifting = new SpriteAsset[4];
        private readonly SpriteAsset[] _linkThrowing = new SpriteAsset[4];
        private readonly long _stoneThrowAnimationMilliseconds;
        private readonly SpriteAsset _linkFalling;
        private readonly long _holeFallAnimationMilliseconds;
        private readonly SpriteAsset[] _linkFlying = new SpriteAsset[4];
        private readonly SpriteAsset[] _linkAttacking = new SpriteAsset[4];
        private readonly SpriteAsset[] _linkUsingItem = new SpriteAsset[4];
        private readonly SpriteAsset[] _linkShowingItem = new SpriteAsset[3];
        private readonly SpriteAsset[] _linkSwords = new SpriteAsset[4];
        private readonly SpriteAsset[] _enemySpears = new SpriteAsset[4];
        private SpriteAsset _enemyOctorokShot;
        private SpriteAsset _enemyFireball;
        private LiveWallpaperLinkRouteAction _lastLinkAction =
            LiveWallpaperLinkRouteAction.Stand;
        private readonly SpriteAsset[] _roosterDirections = new SpriteAsset[4];
        private SpriteAsset _marin;
        private SpriteAsset _bowWowLeft;
        private SpriteAsset _bowWowRight;
        private SpriteAsset _roosterLeft;
        private SpriteAsset _roosterRight;
        private readonly SpriteAsset[] _mapDogDirections = new SpriteAsset[2];
        private readonly SpriteAsset[] _mapGrandmotherDirections = new SpriteAsset[2];
        private SpriteAsset _mapRaccoonIdle;
        private SpriteAsset _mapRaccoonLaugh;
        private SpriteAsset _mapWeatherBird;
        private SpriteAsset _mapOwl;
        private SpriteAsset _mapOwlFlying;
        private readonly SpriteAsset[] _mapBirdIdle = new SpriteAsset[2];
        private readonly SpriteAsset[] _mapBirdWalking = new SpriteAsset[2];
        private readonly SpriteAsset[] _mapBowWowDirections = new SpriteAsset[4];
        private readonly SpriteAsset[] _mapFrogSitting = new SpriteAsset[4];
        private readonly SpriteAsset[] _mapFrogJumping = new SpriteAsset[4];
        private readonly SpriteAsset[] _mapMouseStanding = new SpriteAsset[2];
        private readonly SpriteAsset[] _mapMouseWalking = new SpriteAsset[2];
        private readonly SpriteAsset[] _mapBowWowSmall = new SpriteAsset[2];
        private SpriteAsset _mapAlligator;
        private readonly SpriteAsset[] _mapChickenDudeIdle = new SpriteAsset[2];
        private readonly SpriteAsset[] _mapChickenDudePowder = new SpriteAsset[2];
        private readonly SpriteAsset[] _mapHippoStanding = new SpriteAsset[2];
        private readonly SpriteAsset[] _mapHippoEmbarrassed = new SpriteAsset[2];
        private SpriteAsset _mapPainterIdle;
        private readonly SpriteAsset[] _mapPainterTalk = new SpriteAsset[2];
        private SpriteAsset _mapTracyIdle;
        private readonly SpriteAsset[] _mapTracySides = new SpriteAsset[2];
        private SpriteAsset _mapLetterBoyIdle;
        private readonly SpriteAsset[] _mapLetterBoyLook = new SpriteAsset[2];
        private SpriteAsset _mapLetterGirlIdle;
        private readonly SpriteAsset[] _mapLetterGirlLook = new SpriteAsset[2];
        private readonly SpriteAsset[] _mapLetterBird = new SpriteAsset[2];
        private readonly SpriteAsset[] _mapLetterBirdGreen = new SpriteAsset[2];
        private SpriteAsset _mapPhotoMouse;
        private SpriteAsset _mapFishermanStand;
        private SpriteAsset _mapFishermanTalk;
        private SpriteAsset _mapMermaid;
        private SpriteAsset _mapFairy;
        private readonly Dictionary<string, SpriteAsset[]> _mapPeople =
            new Dictionary<string, SpriteAsset[]>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<LiveWallpaperMapEnemyKind, EnemyAssetSet> _mapEnemies =
            new Dictionary<LiveWallpaperMapEnemyKind, EnemyAssetSet>();
        private readonly Dictionary<string, AtlasSpriteAsset> _mapDecorations =
            new Dictionary<string, AtlasSpriteAsset>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, AtlasSpriteAsset> _chestItems =
            new Dictionary<string, AtlasSpriteAsset>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, AtlasSpriteAsset> _mapAnimatedTiles =
            new Dictionary<string, AtlasSpriteAsset>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, SpriteAsset> _mapLamps =
            new Dictionary<string, SpriteAsset>(StringComparer.OrdinalIgnoreCase);
        private SpriteAsset _butterfly;
        private SpriteAsset _owl;
        private AtlasSpriteAsset _marinNote;
        private AtlasSpriteAsset _bowWowChain;
        private AtlasSpriteAsset _roosterParticleLarge;
        private AtlasSpriteAsset _roosterParticleMedium;
        private AtlasSpriteAsset _roosterParticleSmall;
        private AtlasSpriteAsset _vegetationLeaf;
        private AtlasSpriteAsset _stoneParticle;
        private SpriteAsset _stoneSplash;
        private SpriteAsset _stoneFall;
        private AtlasSpriteAsset _vegetationHeart;
        private AtlasSpriteAsset _vegetationRupee;
        private AtlasSpriteAsset _hookshotChain;
        private AtlasSpriteAsset _hookshotHook;
        private SpriteAsset _pegasusDust;
        private MapAsset _overworldMap;
        private readonly Context _context;
        private string _activeMapName = "overworld.map";
        private float _activeMapCameraFocusX;
        private float _activeMapCameraFocusY;
        private readonly LiveWallpaperLinkSimulation _linkSimulation = new();
        private readonly LiveWallpaperFollowerSimulation _followerSimulation = new();
        private readonly LiveWallpaperEnemySimulation.Session _enemySimulation = new();
        private readonly LiveWallpaperActorSimulation.Session _actorSimulation = new();
        private readonly List<PlayerLayerDrawEntry> _playerLayerDrawEntries = new();
        private LiveWallpaperActorState[] _mapActorStates = [];
        private LiveWallpaperEnemyState[] _mapEnemyStates = [];
        private bool[] _activeMapActors = [];
        private bool[] _activeMapEnemies = [];
        private readonly Dictionary<int, int> _grandmotherDirections = new();
        private LiveWallpaperMapViewport? _followedViewport;
        private int _followedScene = -1;
        private int _followedSceneSetting = -1;
        private float _cameraTargetOriginX;
        private float _cameraTargetOriginY;
        private long? _cameraLastElapsed;
        private LiveWallpaperMapViewport? _lastDrawnViewport;
        private Bitmap _mapTileCache;
        private Canvas _mapTileCacheCanvas;
        private MapAsset _mapTileCacheMap;
        private int _mapTileCacheOriginX = -1;
        private int _mapTileCacheOriginY = -1;
        private int _mapTileCacheColumns;
        private int _mapTileCacheRows;
        private float _mapTileCacheTileSize;
        private int _wallpaperColorRevision;
        private LiveWallpaperTimePhase? _wallpaperColorPhase;

        public int WallpaperColorRevision => _wallpaperColorRevision;

        public LadxhdWallpaperScene(Context context)
        {
            _context = context.ApplicationContext ?? context;
            for (var direction = 0; direction < 4; direction++)
            {
                _linkWalking[direction] = LoadSprite(
                    context, "link0.ani", [$"walk_{direction}"]);
                _linkStanding[direction] = LoadSprite(
                    context, "link0.ani", [$"stand_{direction}"]);
                _linkJumping[direction] = LoadSprite(
                    context, "link0.ani", [$"jump_{direction}"]);
                _linkSwimming[direction] = LoadSprite(
                    context, "link0.ani", [$"swim_{direction}"]);
                _linkPushing[direction] = LoadSprite(
                    context, "link0.ani", [$"push_{direction}"]);
                _linkCarrying[direction] = LoadSprite(
                    context, "link0.ani", [$"standc_{direction}", $"stand_{direction}"]);
                _linkLifting[direction] = LoadSprite(
                    context, "link0.ani", [$"pull_{direction}", $"grab_{direction}"]);
                _linkThrowing[direction] = LoadSprite(
                    context, "link0.ani", [$"throw_{direction}", $"stand_{direction}"]);
                _linkFlying[direction] = LoadSprite(
                    context, "link0.ani", [$"flying_{direction}"]);
                _linkAttacking[direction] = LoadSprite(
                    context, "link0.ani", [$"attack_{direction}"]);
                _linkUsingItem[direction] = LoadSprite(
                    context, "link0.ani", [$"powder_{direction}"]);
                _linkSwords[direction] = LoadSprite(
                    context, "Objects/sword.ani", [$"attack_{direction}"]);
                _enemySpears[direction] = LoadSprite(
                    context, "Objects/spear.ani", [direction.ToString()]);
                _roosterDirections[direction] = LoadSprite(
                    context, "NPCs/cock.ani", [$"stand_{direction}"]);
            }
            _linkFalling = LoadSprite(context, "link0.ani", ["fall"]);
            _linkShowingItem[1] = LoadSprite(context, "link0.ani", ["show1"]);
            _linkShowingItem[2] = LoadSprite(context, "link0.ani", ["show2"]);
            _holeFallAnimationMilliseconds =
                _linkFalling?.Animation?.DurationMilliseconds ?? 850L;
            _stoneThrowAnimationMilliseconds = _linkThrowing
                .Where(asset => asset?.Animation != null &&
                                asset.Animation.AnimationId.StartsWith(
                                    "throw_", StringComparison.OrdinalIgnoreCase))
                .Select(asset => asset.Animation.DurationMilliseconds)
                .DefaultIfEmpty(StoneGameplayMotion.ThrowFlightMilliseconds)
                .Max();
            _marin = LoadSprite(context, "NPCs/marin.ani", ["sing"]);
            _bowWowLeft = LoadSprite(context, "NPCs/BowWow.ani", ["walk_0"]);
            _bowWowRight = LoadSprite(context, "NPCs/BowWow.ani", ["walk_2"]);
            _roosterLeft = _roosterDirections[0];
            _roosterRight = _roosterDirections[2];
            _enemyOctorokShot = LoadSprite(
                context, "Enemies/octorok shot.ani", ["idle"]);
            _enemyFireball = LoadSprite(
                context, "Enemies/fireball.ani", ["idle"]);
            for (var direction = 0; direction < 2; direction++)
            {
                _mapDogDirections[direction] = LoadSprite(
                    context, "NPCs/dog.ani", [$"idle_{direction}"]);
                _mapBirdIdle[direction] = LoadSprite(
                    context, "NPCs/bird.ani", [$"idle_{direction}"]);
                _mapBirdWalking[direction] = LoadSprite(
                    context, "NPCs/bird.ani", [$"walk_{direction}"]);
                _mapMouseStanding[direction] = LoadSprite(
                    context, "NPCs/mouse.ani", [$"stand_{direction}"]);
                _mapMouseWalking[direction] = LoadSprite(
                    context, "NPCs/mouse.ani", [$"walk_{direction}"]);
                _mapBowWowSmall[direction] = LoadSprite(
                    context, "NPCs/bowWowSmall.ani", [$"walk_{direction}"]);
                var signedDirection = direction == 0 ? -1 : 1;
                _mapChickenDudeIdle[direction] = LoadSprite(
                    context, "NPCs/npc_chicken_dude.ani",
                    [$"idle_{signedDirection}"]);
                _mapChickenDudePowder[direction] = LoadSprite(
                    context, "NPCs/npc_chicken_dude.ani",
                    [$"powder_{signedDirection}"]);
                _mapHippoStanding[direction] = LoadSprite(
                    context, "NPCs/npc_hippo.ani",
                    [$"stand_{signedDirection}"]);
                _mapHippoEmbarrassed[direction] = LoadSprite(
                    context, "NPCs/npc_hippo.ani",
                    [$"idle_{signedDirection}"]);
                _mapPainterTalk[direction] = LoadSprite(
                    context, "NPCs/npc_painter.ani",
                    [$"talk_{signedDirection}"]);
                _mapLetterBoyLook[direction] = LoadSprite(
                    context, "NPCs/npc_letter_boy.ani",
                    [$"look_{signedDirection}"]);
                _mapLetterGirlLook[direction] = LoadSprite(
                    context, "NPCs/npc_letter_girl.ani",
                    [$"look_{signedDirection}"]);
                _mapLetterBird[direction] = LoadSprite(
                    context, "NPCs/letterBird.ani", [$"idle_{direction}"]);
                _mapLetterBirdGreen[direction] = LoadSprite(
                    context, "NPCs/letterBirdGreen.ani", [$"idle_{direction}"]);
            }
            for (var direction = 0; direction < 4; direction++)
            {
                _mapBowWowDirections[direction] = LoadSprite(
                    context, "NPCs/BowWow.ani", [$"walk_{direction}"]);
                _mapFrogSitting[direction] = LoadSprite(
                    context, "NPCs/frog.ani", [$"sit_{direction}"]);
                _mapFrogJumping[direction] = LoadSprite(
                    context, "NPCs/frog.ani", [$"jump_{direction}"]);
            }
            _mapAlligator = LoadSprite(
                context, "NPCs/alligator.ani", ["idle"]);
            _mapPainterIdle = LoadSprite(
                context, "NPCs/npc_painter.ani", ["idle"]);
            _mapTracyIdle = LoadSprite(
                context, "NPCs/npc_tracy.ani", ["idle"]);
            _mapTracySides[0] = LoadSprite(
                context, "NPCs/npc_tracy.ani", ["left"]);
            _mapTracySides[1] = LoadSprite(
                context, "NPCs/npc_tracy.ani", ["right"]);
            _mapLetterBoyIdle = LoadSprite(
                context, "NPCs/npc_letter_boy.ani", ["idle"]);
            _mapLetterGirlIdle = LoadSprite(
                context, "NPCs/npc_letter_girl.ani", ["idle"]);
            _mapPhotoMouse = LoadSprite(
                context, "NPCs/photo_mouse.ani", ["stand_0"]);
            _mapGrandmotherDirections[0] = LoadSprite(
                context, "NPCs/npc_woman_broom.ani", ["stand_-1"]);
            _mapGrandmotherDirections[1] = LoadSprite(
                context, "NPCs/npc_woman_broom.ani", ["stand_1"]);
            _mapRaccoonIdle = LoadSprite(
                context, "NPCs/raccoon.ani", ["idle"]);
            _mapRaccoonLaugh = LoadSprite(
                context, "NPCs/raccoon.ani", ["laugh", "idle"]);
            _mapWeatherBird = LoadSprite(context, "Objects/weatherBird.ani", ["IDLE"]);
            _mapOwl = LoadSprite(context, "NPCs/owl.ani", ["idle"]);
            _mapOwlFlying = LoadSprite(context, "NPCs/owl.ani", ["fly"]);
            _mapFishermanStand = LoadSprite(
                context, "NPCs/npc_fisherman.ani", ["stand"]);
            _mapFishermanTalk = LoadSprite(
                context, "NPCs/npc_fisherman.ani", ["talk", "stand"]);
            _mapMermaid = LoadSprite(
                context, "NPCs/npc_mermaid.ani", ["idle", "sit_-1"]);
            _mapFairy = LoadSprite(context, "NPCs/fairy.ani", ["idle"]);
            _butterfly = LoadSprite(context, "NPCs/butterfly.ani", ["idle"]);
            _owl = LoadSprite(context, "NPCs/owl.ani", ["fly", "hover", "idle"]);
            _marinNote = LoadAtlasSprite(context, "npcs", "note");
            _bowWowChain = LoadAtlasSprite(context, "npcs", "bowwow chain");
            _roosterParticleLarge = LoadAtlasSprite(context, "npcs", "cock_particle_0");
            _roosterParticleMedium = LoadAtlasSprite(context, "npcs", "cock_particle_1");
            _roosterParticleSmall = LoadAtlasSprite(context, "npcs", "cock_particle_2");
            _vegetationLeaf = LoadAtlasSprite(context, "objects", "leaf");
            _stoneParticle = LoadAtlasSprite(
                context, "objects", "stone_particle");
            _stoneSplash = LoadSprite(
                context, "Particles/fishingSplash.ani", ["idle"]);
            _stoneFall = LoadSprite(
                context, "Particles/fall.ani", ["idle"]);
            _vegetationHeart = LoadAtlasSprite(context, "items", "heart");
            _vegetationRupee = LoadAtlasSprite(context, "items", "rubyBlue");
            _hookshotChain = LoadAtlasSprite(
                context, "objects", "hookshot_chain");
            _hookshotHook = LoadAtlasSprite(
                context, "objects", "hookshot_hook");
            _pegasusDust = LoadSprite(
                context, "Particles/run.ani", ["spawn"]);
            _overworldMap = LoadOverworldMap(context);
            if (_overworldMap != null)
            {
                _mapActorStates = new LiveWallpaperActorState[
                    _overworldMap.Map.Actors.Count];
                _mapEnemyStates = new LiveWallpaperEnemyState[
                    _overworldMap.Map.Enemies.Count];
                _activeMapActors = new bool[_overworldMap.Map.Actors.Count];
                _activeMapEnemies = new bool[_overworldMap.Map.Enemies.Count];
                foreach (var decoration in _overworldMap.Map.Decorations)
                {
                    var key = decoration.AtlasName + "\n" + decoration.SpriteId;
                    if (!_mapDecorations.ContainsKey(key))
                        _mapDecorations[key] = LoadAtlasSprite(
                            context, decoration.AtlasName, decoration.SpriteId);
                }
                foreach (var animatedTile in _overworldMap.Map.AnimatedTiles)
                {
                    if (!_mapAnimatedTiles.ContainsKey(animatedTile.SpriteId))
                        _mapAnimatedTiles[animatedTile.SpriteId] = LoadAtlasSprite(
                            context, "objects", animatedTile.SpriteId);
                }
                foreach (var lamp in _overworldMap.Map.Lamps)
                {
                    if (!_mapLamps.ContainsKey(lamp.AnimationPath))
                        _mapLamps[lamp.AnimationPath] = LoadSprite(
                            context, lamp.AnimationPath, ["idle"]);
                }
                foreach (var enemy in _overworldMap.Map.Enemies)
                {
                    if (!_mapEnemies.ContainsKey(enemy.Kind))
                        _mapEnemies[enemy.Kind] = LoadEnemyAssetSet(context, enemy.Kind);
                }
                foreach (var actor in _overworldMap.Map.Actors)
                {
                    if (actor.Kind is not (LiveWallpaperMapActorKind.Person or
                        LiveWallpaperMapActorKind.LegacyPerson))
                        continue;
                    var animationName = string.IsNullOrWhiteSpace(actor.AnimationName)
                        ? "stand_3"
                        : actor.AnimationName;
                    var key = actor.AnimationId + "\n" + animationName;
                    if (!_mapPeople.ContainsKey(key))
                    {
                        var directions = new SpriteAsset[4];
                        for (var direction = 0; direction < directions.Length; direction++)
                        {
                            directions[direction] = LoadSprite(
                                context, "NPCs/" + actor.AnimationId + ".ani",
                                [$"stand_{direction}", animationName, "stand_3"]);
                        }
                        _mapPeople[key] = directions;
                    }
                }
                PrepareChestItemAssets(context);
            }
        }

        private void PrepareActiveMapAssets(Context context)
        {
            if (_overworldMap?.Map == null)
                return;
            _mapActorStates = new LiveWallpaperActorState[
                _overworldMap.Map.Actors.Count];
            _mapEnemyStates = new LiveWallpaperEnemyState[
                _overworldMap.Map.Enemies.Count];
            _activeMapActors = new bool[_overworldMap.Map.Actors.Count];
            _activeMapEnemies = new bool[_overworldMap.Map.Enemies.Count];
            foreach (var decoration in _overworldMap.Map.Decorations)
            {
                var key = decoration.AtlasName + "\n" + decoration.SpriteId;
                if (!_mapDecorations.ContainsKey(key))
                    _mapDecorations[key] = LoadAtlasSprite(
                        context, decoration.AtlasName, decoration.SpriteId);
            }
            foreach (var animatedTile in _overworldMap.Map.AnimatedTiles)
            {
                if (!_mapAnimatedTiles.ContainsKey(animatedTile.SpriteId))
                    _mapAnimatedTiles[animatedTile.SpriteId] = LoadAtlasSprite(
                        context, "objects", animatedTile.SpriteId);
            }
            foreach (var lamp in _overworldMap.Map.Lamps)
            {
                if (!_mapLamps.ContainsKey(lamp.AnimationPath))
                    _mapLamps[lamp.AnimationPath] = LoadSprite(
                        context, lamp.AnimationPath, ["idle"]);
            }
            foreach (var enemy in _overworldMap.Map.Enemies)
            {
                if (!_mapEnemies.ContainsKey(enemy.Kind))
                    _mapEnemies[enemy.Kind] = LoadEnemyAssetSet(context, enemy.Kind);
            }
            foreach (var actor in _overworldMap.Map.Actors)
            {
                if (actor.Kind is not (LiveWallpaperMapActorKind.Person or
                    LiveWallpaperMapActorKind.LegacyPerson))
                    continue;
                var animationName = string.IsNullOrWhiteSpace(actor.AnimationName)
                    ? "stand_3"
                    : actor.AnimationName;
                var key = actor.AnimationId + "\n" + animationName;
                if (_mapPeople.ContainsKey(key))
                    continue;
                var directions = new SpriteAsset[4];
                for (var direction = 0; direction < directions.Length; direction++)
                {
                    directions[direction] = LoadSprite(
                        context, "NPCs/" + actor.AnimationId + ".ani",
                        [$"stand_{direction}", animationName, "stand_3"]);
                }
                _mapPeople[key] = directions;
            }
            PrepareChestItemAssets(context);
        }

        private void PrepareChestItemAssets(Context context)
        {
            if (_overworldMap?.Map == null)
                return;
            foreach (var mapObject in _overworldMap.Map.Objects)
            {
                if (!string.Equals(
                        mapObject.Template, "chest", StringComparison.Ordinal) ||
                    mapObject.Arguments.Count == 0 ||
                    !LiveWallpaperChestItem.TryResolve(
                        mapObject.Arguments[0], out var visual) ||
                    _chestItems.ContainsKey(visual.SpriteId))
                    continue;
                _chestItems[visual.SpriteId] = LoadAtlasSprite(
                    context, "items", visual.SpriteId);
            }
        }

        private bool TryFollowLinkThroughPortal(
            LiveWallpaperSimulatedLinkState link,
            int width, int height, float xOffset)
        {
            if (_overworldMap?.Map == null)
                return false;
            var sourceMap = _overworldMap.Map;
            var linkPixelX = link.MapX * 16f;
            var linkPixelY = link.MapY * 16f;
            foreach (var portal in sourceMap.Portals)
            {
                if (!portal.HasDestination || portal.Mode is not (0 or 1))
                    continue;
                var deltaX = linkPixelX - portal.LinkTargetX;
                var deltaY = linkPixelY - portal.LinkTargetY;
                if (portal.IsHoleTeleporter)
                {
                    // ObjLink plays link0/fall before OnHoleReset changes to
                    // ObjHoleTeleporter's room/entry. Follow late enough to
                    // retain that presentation, but before a 15 Hz frame can
                    // advance past the 850 ms terminal frame and local reset.
                    var bodyX = linkPixelX - 4f;
                    var bodyY = linkPixelY - 10f;
                    if (link.Action != LiveWallpaperLinkRouteAction.Falling ||
                        link.ActionProgress < 0.75f ||
                        bodyX >= portal.PixelX + portal.Width ||
                        bodyX + 8f <= portal.PixelX ||
                        bodyY >= portal.PixelY + portal.Height ||
                        bodyY + 10f <= portal.PixelY)
                        continue;
                }
                else
                {
                    if (!portal.ShouldActivateAt(
                            linkPixelX, linkPixelY,
                            link.Input.Move.Y, link.Direction))
                        continue;
                }

                var nextMap = LoadMap(_context, portal.NextMap);
                if (nextMap?.Map == null)
                    return false;
                LiveWallpaperMapPortal destination = default;
                var foundDestination = false;
                foreach (var candidate in nextMap.Map.Portals)
                {
                    if (!string.Equals(candidate.EntryId, portal.ExitId,
                            StringComparison.Ordinal))
                        continue;
                    destination = candidate;
                    foundDestination = true;
                    break;
                }
                if (!foundDestination)
                    return false;

                var spawnX = destination.GetLinkSpawnX(nextMap.Map.Is2DMap);
                var spawnY = destination.GetLinkSpawnY(nextMap.Map.Is2DMap);
                _overworldMap = nextMap;
                _activeMapName = portal.NextMap;
                _activeMapCameraFocusX = spawnX;
                _activeMapCameraFocusY = spawnY;
                PrepareActiveMapAssets(_context);
                _linkSimulation.EnterMap(
                    spawnX, spawnY, destination.EntryId);
                if (LiveWallpaperMapViewport.TryCreateCentered(
                        width, height, nextMap.Map.Width, nextMap.Map.Height,
                        spawnX, spawnY, xOffset, out var viewport))
                {
                    _followedViewport = viewport;
                    _cameraTargetOriginX = viewport.CameraOriginX;
                    _cameraTargetOriginY = viewport.CameraOriginY;
                }
                _cameraLastElapsed = null;
                _lastDrawnViewport = null;
                return true;
            }
            return false;
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
            int linkActivity,
            int wildlifeSchedule,
            int characterPosition,
            bool followLoadingZones)
        {
            var width = canvas.Width;
            var height = canvas.Height;
            if (width <= 0 || height <= 0)
                return;
            var time = animated ? elapsed : 0L;
            var unit = Math.Max(1f, Math.Min(width, height) / 240f);
            var phase = LiveWallpaperLighting.Resolve(timeOfDay, DateTime.Now.Hour);
            if (_wallpaperColorPhase != phase)
            {
                _wallpaperColorPhase = phase;
                _wallpaperColorRevision++;
            }
            canvas.DrawColor(Color.Black);

            // Defensively restore fully opaque bitmap rendering at the start of every frame.
            _bitmapPaint.Color = Color.White;
            _bitmapPaint.Alpha = 255;

            var resolvedScene = LiveWallpaperSceneSelection.Resolve(
                scene, elapsed, _overworldMap != null);
            if (resolvedScene <= 0)
                return;
            if (followLoadingZones)
            {
                // The selected wallpaper location is an exploration starting point.
                // In Rotate mode Resolve() changes every 45 seconds; treating that as
                // a new selection reset the followed journey to the curated scenes.
                if (_followedScene < 0 || _followedSceneSetting != scene)
                {
                    _followedViewport = null;
                    _cameraLastElapsed = null;
                    _followedScene = resolvedScene;
                    _followedSceneSetting = scene;
                }
                else
                {
                    resolvedScene = _followedScene;
                }
            }
            else
            {
                _followedViewport = null;
                _cameraLastElapsed = null;
                _followedScene = resolvedScene;
                _followedSceneSetting = scene;
            }
            var groundY = DrawInstalledMap(
                canvas, width, height, resolvedScene, xOffset,
                _followedViewport, out var viewport);
            _lastDrawnViewport = viewport;
            DrawInstalledMapAnimatedTiles(canvas, viewport, time, animated);
            LiveWallpaperSimulatedLinkState? simulatedLink = null;
            if (linkActivity != 3)
            {
                var followOverworldLoadingZones = followLoadingZones &&
                    string.Equals(_activeMapName, "overworld.map",
                        StringComparison.OrdinalIgnoreCase);
                simulatedLink = _linkSimulation.UpdateJourney(
                    resolvedScene, linkActivity, elapsed, animated,
                    _overworldMap?.Map, viewport, allowIslandLife: true,
                    followOverworldLoadingZones,
                    _stoneThrowAnimationMilliseconds,
                    allowViewportFollow: followLoadingZones,
                    holeFallAnimationMilliseconds:
                        _holeFallAnimationMilliseconds);
                if (followLoadingZones &&
                    TryUpdateFollowCamera(
                        viewport, simulatedLink.Value, elapsed, out var nextViewport))
                    _followedViewport = nextViewport;
                if (simulatedLink.Value.Action == LiveWallpaperLinkRouteAction.Attack)
                    simulatedLink = AttachSwordAttackBox(simulatedLink.Value);
            }
            _linkSimulation.BeginLiveStateFrame(_overworldMap?.Map);
            PrepareInstalledMapActors(viewport, time, animated, simulatedLink);
            PrepareInstalledMapEnemies(viewport, time, animated, simulatedLink);
            DrawInstalledMapBottomDecorations(
                canvas, viewport, time, animated, simulatedLink);
            // ComponentDrawPool renders every Values.LayerPlayer component in one
            // CPosition.Y order. Keep map sprites, actors, enemies, projectiles and
            // Link in that same player-layer order instead of type-based passes.
            DrawInstalledMapPlayerLayer(
                canvas, viewport, elapsed, animated, simulatedLink);
            DrawLightingOverlay(canvas, width, height, phase);
            DrawSceneTransition(canvas, width, height, scene, elapsed);
            if (followLoadingZones && simulatedLink.HasValue)
                TryFollowLinkThroughPortal(
                    simulatedLink.Value, width, height, xOffset);
        }

        public bool TrySetLinkDestination(float canvasX, float canvasY)
        {
            if (_overworldMap?.Map == null || !_lastDrawnViewport.HasValue)
                return false;
            var viewport = _lastDrawnViewport.Value;
            var targetPixelX = (viewport.OriginX +
                                (canvasX - viewport.Left) / viewport.TileSize) * 16f;
            var targetPixelY = (viewport.OriginY +
                                (canvasY - viewport.Top) / viewport.TileSize) * 16f;
            return _linkSimulation.TryWalkTo(
                _overworldMap.Map, viewport, targetPixelX, targetPixelY);
        }

        private void DrawLightingOverlay(
            Canvas canvas, int width, int height, LiveWallpaperTimePhase phase)
        {
            if (phase == LiveWallpaperTimePhase.Day)
                return;
            _overlayPaint.Color = phase == LiveWallpaperTimePhase.Night
                ? Color.Argb(82, 7, 16, 50)
                : Color.Argb(22, 116, 45, 59);
            canvas.DrawRect(0, 0, width, height, _overlayPaint);
        }

        private void DrawSceneTransition(
            Canvas canvas, int width, int height, int scene, long elapsed)
        {
            var opacity = LiveWallpaperSceneSelection.GetRotationTransitionOpacity(scene, elapsed);
            if (opacity <= 0f)
                return;
            _overlayPaint.Color = Color.Argb((int)(255f * opacity), 4, 8, 18);
            canvas.DrawRect(0, 0, width, height, _overlayPaint);
        }

        private float DrawInstalledMap(
            Canvas canvas,
            int width,
            int height,
            int scene,
            float xOffset,
            LiveWallpaperMapViewport? followedViewport,
            out LiveWallpaperMapViewport viewport)
        {
            const int tileSize = 16;
            const int atlasStride = tileSize + 2;
            var map = _overworldMap.Map;
            var tileset = _overworldMap.Bitmap;
            var created = string.Equals(
                    _activeMapName, "overworld.map",
                    StringComparison.OrdinalIgnoreCase)
                ? LiveWallpaperMapViewport.TryCreate(
                    width, height, map.Height, scene, xOffset, out viewport)
                : LiveWallpaperMapViewport.TryCreateCentered(
                    width, height, map.Width, map.Height,
                    _activeMapCameraFocusX, _activeMapCameraFocusY,
                    xOffset, out viewport);
            if (!created)
                return height * 0.72f;
            if (followedViewport.HasValue)
                viewport = viewport.WithCameraOrigin(
                    followedViewport.Value.CameraOriginX,
                    followedViewport.Value.CameraOriginY,
                    map.Width, map.Height);
            var tilesPerRow = tileset.Width / atlasStride;
            if (tilesPerRow <= 0)
                return viewport.GroundY;

            EnsureMapTileCache(viewport, tilesPerRow);
            if (_mapTileCache != null)
                canvas.DrawBitmap(
                    _mapTileCache, viewport.Left, viewport.Top, _bitmapPaint);

            return viewport.GroundY;
        }

        private void EnsureMapTileCache(
            LiveWallpaperMapViewport viewport, int tilesPerRow)
        {
            const int tileSize = 16;
            const int atlasStride = tileSize + 2;
            var map = _overworldMap.Map;
            var tileset = _overworldMap.Bitmap;
            var cacheWidth = Math.Max(
                1, (int)MathF.Ceiling(viewport.Columns * viewport.TileSize));
            var cacheHeight = Math.Max(
                1, (int)MathF.Ceiling(viewport.Rows * viewport.TileSize));
            var sizeChanged = _mapTileCache == null ||
                              _mapTileCache.Width != cacheWidth ||
                              _mapTileCache.Height != cacheHeight;
            var contentChanged = !ReferenceEquals(_mapTileCacheMap, _overworldMap) ||
                                 _mapTileCacheOriginX != viewport.OriginX ||
                                 _mapTileCacheOriginY != viewport.OriginY ||
                                 _mapTileCacheColumns != viewport.Columns ||
                                 _mapTileCacheRows != viewport.Rows ||
                                 MathF.Abs(_mapTileCacheTileSize -
                                           viewport.TileSize) > 0.001f;
            if (!sizeChanged && !contentChanged)
                return;

            if (sizeChanged)
            {
                _mapTileCacheCanvas?.Dispose();
                _mapTileCache?.Dispose();
                _mapTileCache = Bitmap.CreateBitmap(
                    cacheWidth, cacheHeight, Bitmap.Config.Argb8888);
                _mapTileCacheCanvas = new Canvas(_mapTileCache);
            }
            else
            {
                _mapTileCache.EraseColor(Color.Transparent);
            }

            var source = new Rect();
            var destination = new RectF();

            for (var layer = 0; layer < map.DrawableDepth; layer++)
            {
                for (var y = 0; y < viewport.Rows; y++)
                {
                    for (var x = 0; x < viewport.Columns; x++)
                    {
                        var tile = map.GetTile(
                            viewport.OriginX + x, viewport.OriginY + y, layer);
                        if (tile < 0)
                            continue;
                        var sourceX = tile % tilesPerRow * atlasStride + 1;
                        var sourceY = tile / tilesPerRow * atlasStride + 1;
                        if (sourceX + tileSize > tileset.Width ||
                            sourceY + tileSize > tileset.Height)
                            continue;
                        source.Set(sourceX, sourceY,
                            sourceX + tileSize, sourceY + tileSize);
                        destination.Set(
                            x * viewport.TileSize,
                            y * viewport.TileSize,
                            (x + 1) * viewport.TileSize,
                            (y + 1) * viewport.TileSize);
                        _mapTileCacheCanvas.DrawBitmap(
                            tileset, source, destination, _bitmapPaint);
                    }
                }
            }

            // wave_3/4/5 are the game's transparent white-water overlays. The
            // overworld stores them on empty cells and expects tileset0's exact
            // solid-blue ocean tile underneath.
            foreach (var animatedTile in map.AnimatedTiles)
            {
                if (!map.NeedsOverworldOceanBase(animatedTile))
                    continue;
                var mapTileX = animatedTile.EntityX / tileSize;
                var mapTileY = animatedTile.EntityY / tileSize;
                var cacheX = mapTileX - viewport.OriginX;
                var cacheY = mapTileY - viewport.OriginY;
                if (cacheX < 0 || cacheX >= viewport.Columns ||
                    cacheY < 0 || cacheY >= viewport.Rows)
                    continue;
                var oceanTile = LiveWallpaperMap.OverworldOceanTileIndex;
                var sourceX = oceanTile % tilesPerRow * atlasStride + 1;
                var sourceY = oceanTile / tilesPerRow * atlasStride + 1;
                if (sourceX + tileSize > tileset.Width ||
                    sourceY + tileSize > tileset.Height)
                    continue;
                source.Set(sourceX, sourceY,
                    sourceX + tileSize, sourceY + tileSize);
                destination.Set(
                    cacheX * viewport.TileSize,
                    cacheY * viewport.TileSize,
                    (cacheX + 1) * viewport.TileSize,
                    (cacheY + 1) * viewport.TileSize);
                _mapTileCacheCanvas.DrawBitmap(
                    tileset, source, destination, _bitmapPaint);
            }

            source.Dispose();
            destination.Dispose();
            _mapTileCacheMap = _overworldMap;
            _mapTileCacheOriginX = viewport.OriginX;
            _mapTileCacheOriginY = viewport.OriginY;
            _mapTileCacheColumns = viewport.Columns;
            _mapTileCacheRows = viewport.Rows;
            _mapTileCacheTileSize = viewport.TileSize;
            _wallpaperColorRevision++;
        }

        public WallpaperColors ComputeWallpaperColors()
        {
            if (!OperatingSystem.IsAndroidVersionAtLeast(27) ||
                _mapTileCache == null || _mapTileCache.IsRecycled)
                return null;
            // System UI needs the luminance behind the status bar, not the
            // full island average. A letterboxed viewport leaves that strip
            // black; otherwise sample the first two visible tile rows.
            var sampleMap = !_lastDrawnViewport.HasValue ||
                            _lastDrawnViewport.Value.Top <= 0.5f;
            long red = 0;
            long green = 0;
            long blue = 0;
            var count = 0;
            if (sampleMap)
            {
                var sampleHeight = Math.Min(
                    _mapTileCache.Height,
                    Math.Max(1, (int)MathF.Ceiling(
                        _lastDrawnViewport.Value.TileSize * 2f)));
                var stepX = Math.Max(1, _mapTileCache.Width / 24);
                var stepY = Math.Max(1, sampleHeight / 6);
                for (var y = 0; y < sampleHeight; y += stepY)
                for (var x = 0; x < _mapTileCache.Width; x += stepX)
                {
                    var pixel = _mapTileCache.GetPixel(x, y);
                    red += pixel >> 16 & 0xff;
                    green += pixel >> 8 & 0xff;
                    blue += pixel & 0xff;
                    count++;
                }
            }
            var averageRed = count > 0 ? (int)(red / count) : 0;
            var averageGreen = count > 0 ? (int)(green / count) : 0;
            var averageBlue = count > 0 ? (int)(blue / count) : 0;
            if (_wallpaperColorPhase == LiveWallpaperTimePhase.Night)
                BlendWallpaperColor(
                    ref averageRed, ref averageGreen, ref averageBlue,
                    7, 16, 50, 82);
            else if (_wallpaperColorPhase == LiveWallpaperTimePhase.Sunset)
                BlendWallpaperColor(
                    ref averageRed, ref averageGreen, ref averageBlue,
                    116, 45, 59, 22);
            using var sample = Bitmap.CreateBitmap(1, 1, Bitmap.Config.Argb8888);
            sample.EraseColor(Color.Rgb(
                averageRed, averageGreen, averageBlue));
            return WallpaperColors.FromBitmap(sample);
        }

        private static void BlendWallpaperColor(
            ref int red, ref int green, ref int blue,
            int overlayRed, int overlayGreen, int overlayBlue, int alpha)
        {
            red = (red * (255 - alpha) + overlayRed * alpha) / 255;
            green = (green * (255 - alpha) + overlayGreen * alpha) / 255;
            blue = (blue * (255 - alpha) + overlayBlue * alpha) / 255;
        }

        private bool TryUpdateFollowCamera(
            LiveWallpaperMapViewport viewport,
            LiveWallpaperSimulatedLinkState link,
            long elapsed,
            out LiveWallpaperMapViewport nextViewport)
        {
            nextViewport = viewport;
            if (_overworldMap?.Map == null)
                return false;

            var deltaMilliseconds = _cameraLastElapsed.HasValue
                ? Math.Clamp(elapsed - _cameraLastElapsed.Value, 0L, 100L)
                : 0L;
            _cameraLastElapsed = elapsed;
            var atTarget = MathF.Abs(
                               viewport.CameraOriginX - _cameraTargetOriginX) < 0.001f &&
                           MathF.Abs(
                               viewport.CameraOriginY - _cameraTargetOriginY) < 0.001f;
            if (!_followedViewport.HasValue)
            {
                _cameraTargetOriginX = viewport.CameraOriginX;
                _cameraTargetOriginY = viewport.CameraOriginY;
                atTarget = true;
            }
            if (atTarget)
            {
                var roomBasedCamera = _activeMapName?.StartsWith(
                    "dungeon", StringComparison.OrdinalIgnoreCase) == true;
                var hasScrollTarget = roomBasedCamera
                    ? viewport.TryGetRoomScrollTarget(
                        link.MapX * 16f, link.MapY * 16f,
                        _overworldMap.Map.MapOffsetX,
                        _overworldMap.Map.MapOffsetY,
                        _overworldMap.Map.Width, _overworldMap.Map.Height,
                        out var targetX, out var targetY)
                    : viewport.TryGetEdgeScrollTarget(
                        link.MapX * 16f, link.MapY * 16f,
                        link.Input.Move.X, link.Input.Move.Y,
                        _overworldMap.Map.Width, _overworldMap.Map.Height,
                        out targetX, out targetY);
                if (hasScrollTarget)
                {
                    _cameraTargetOriginX = targetX;
                    _cameraTargetOriginY = targetY;
                }
            }

            const float transitionSeconds = 0.65f;
            var seconds = deltaMilliseconds / 1000f;
            var cameraX = MoveTowards(
                viewport.CameraOriginX, _cameraTargetOriginX,
                10f / transitionSeconds * seconds);
            var cameraY = MoveTowards(
                viewport.CameraOriginY, _cameraTargetOriginY,
                8f / transitionSeconds * seconds);
            nextViewport = viewport.WithCameraOrigin(
                cameraX, cameraY,
                _overworldMap.Map.Width, _overworldMap.Map.Height);
            return true;
        }

        private static float MoveTowards(float current, float target, float distance)
        {
            if (current < target)
                return MathF.Min(current + distance, target);
            if (current > target)
                return MathF.Max(current - distance, target);
            return current;
        }

        private void DrawInstalledMapBottomDecorations(
            Canvas canvas, LiveWallpaperMapViewport viewport,
            long elapsed, bool animated,
            LiveWallpaperSimulatedLinkState? link)
        {
            if (_overworldMap?.Map == null)
                return;
            for (var decorationIndex = 0;
                 decorationIndex < _overworldMap.Map.Decorations.Count;
                 decorationIndex++)
            {
                if (_overworldMap.Map.Decorations[decorationIndex].PlayerLayer)
                    continue;
                DrawInstalledMapDecoration(
                    canvas, viewport, decorationIndex, link);
            }
            for (var lampIndex = 0;
                 lampIndex < _overworldMap.Map.Lamps.Count;
                 lampIndex++)
            {
                if (_overworldMap.Map.Lamps[lampIndex].PlayerLayer)
                    continue;
                DrawInstalledMapLamp(
                    canvas, viewport, elapsed, animated, lampIndex);
            }
        }

        private void DrawInstalledMapLamp(
            Canvas canvas, LiveWallpaperMapViewport viewport,
            long elapsed, bool animated, int lampIndex)
        {
            if (_overworldMap?.Map == null ||
                lampIndex < 0 || lampIndex >= _overworldMap.Map.Lamps.Count)
                return;
            var lamp = _overworldMap.Map.Lamps[lampIndex];
            if (!_mapLamps.TryGetValue(lamp.AnimationPath, out var asset) ||
                asset == null)
                return;
            var scale = viewport.TileSize / 16f;
            var left = viewport.Left +
                       (lamp.PixelX / 16f - viewport.OriginX) * viewport.TileSize;
            var top = viewport.Top +
                      (lamp.PixelY / 16f - viewport.OriginY) * viewport.TileSize;
            if (!IsNearViewport(
                    viewport, lamp.EntityX, lamp.EntityY, 32f))
                return;
            var save = canvas.Save();
            if (lamp.Rotation != 0)
            {
                canvas.Rotate(
                    lamp.Rotation * 90f,
                    left + 8f * scale,
                    top + 8f * scale);
            }
            DrawSpriteAt(
                canvas, asset, elapsed, left, top, scale,
                engineDriven: true, animated: animated);
            canvas.RestoreToCount(save);
        }

        private void DrawInstalledMapDecoration(
            Canvas canvas, LiveWallpaperMapViewport viewport, int decorationIndex,
            LiveWallpaperSimulatedLinkState? link)
        {
            if (_overworldMap?.Map == null ||
                decorationIndex < 0 ||
                decorationIndex >= _overworldMap.Map.Decorations.Count)
                return;
            var decoration = _overworldMap.Map.Decorations[decorationIndex];
            if (link?.CutBushes != null &&
                (LiveWallpaperMap.IsBushSprite(decoration.SpriteId) ||
                 LiveWallpaperMap.IsGrassSprite(decoration.SpriteId)) &&
                link.Value.CutBushes.Contains(
                    _overworldMap.Map.GetBushKey(
                        decoration.EntityX - 8, decoration.EntityY - 8)))
                return;
            if (link?.LiftedStones != null && decoration.StoneLayout)
            {
                if (link.Value.LiftedStones.Contains(
                        _overworldMap.Map.GetStoneKey(decoration)))
                    return;
            }
            var entityX = (float)decoration.EntityX;
            var entityY = (float)decoration.EntityY;
            if (LiveWallpaperMap.IsMoveStoneSprite(decoration.SpriteId))
            {
                var moveStoneKey = _overworldMap.Map.GetMoveStoneKey(decoration);
                if (link?.FallenMoveStones?.Contains(moveStoneKey) == true)
                    return;
                if (link?.MoveStones != null &&
                    link.Value.MoveStones.TryGetValue(
                        moveStoneKey, out var movedPosition))
                {
                    entityX = movedPosition.X;
                    entityY = movedPosition.Y;
                }
            }
            var key = decoration.AtlasName + "\n" + decoration.SpriteId;
            if (!_mapDecorations.TryGetValue(key, out var asset) || asset == null)
                return;
            var sourceOffsetX = decoration.SourceOffsetX;
            if (link?.OpenedChests != null &&
                decoration.SpriteId is "chest_back" or "chest_front" &&
                link.Value.OpenedChests.Contains(
                    _overworldMap.Map.GetChestKey(
                        decoration.EntityX, decoration.EntityY - 13)))
                sourceOffsetX += 16;
            var scale = viewport.TileSize / 16f;
            var anchorX = viewport.Left +
                          ((entityX + decoration.DrawOffsetX) / 16f -
                           viewport.OriginX) *
                          viewport.TileSize;
            var anchorY = viewport.Top +
                          ((entityY + decoration.DrawOffsetY) / 16f -
                           viewport.OriginY) *
                          viewport.TileSize;
            if (anchorX < viewport.Left - 64f * scale ||
                anchorX > viewport.Left + viewport.Columns * viewport.TileSize +
                64f * scale ||
                anchorY < viewport.Top - 64f * scale ||
                anchorY > viewport.Top + viewport.Rows * viewport.TileSize +
                64f * scale)
                return;
            if (decoration.TopLeft)
                DrawAtlasTopLeftAt(
                    canvas, asset, anchorX, anchorY, scale,
                    sourceOffsetX: sourceOffsetX);
            else if (decoration.StoneLayout)
                DrawAtlasStoneAt(canvas, asset, anchorX, anchorY, scale);
            else
                DrawAtlasObjectAt(
                    canvas, asset, anchorX, anchorY, scale,
                    sourceOffsetX);
        }

        private void DrawInstalledMapAnimatedTiles(
            Canvas canvas, LiveWallpaperMapViewport viewport,
            long elapsed, bool animated)
        {
            if (_overworldMap?.Map == null)
                return;
            var scale = viewport.TileSize / 16f;
            foreach (var tile in _overworldMap.Map.AnimatedTiles)
            {
                if (!_mapAnimatedTiles.TryGetValue(tile.SpriteId, out var asset) ||
                    asset == null)
                    continue;
                var anchorX = viewport.Left +
                              (tile.EntityX / 16f - viewport.OriginX) * viewport.TileSize;
                var anchorY = viewport.Top +
                              (tile.EntityY / 16f - viewport.OriginY) * viewport.TileSize;
                if (anchorX < viewport.Left - viewport.TileSize ||
                    anchorX > viewport.Left + viewport.Columns * viewport.TileSize ||
                    anchorY < viewport.Top - viewport.TileSize ||
                    anchorY > viewport.Top + viewport.Rows * viewport.TileSize)
                    continue;
                var frame = animated
                    ? (int)(elapsed / tile.FrameDurationMilliseconds % tile.FrameCount)
                    : 0;
                DrawAtlasAnimatedTileAt(canvas, asset, anchorX, anchorY, scale, frame);
            }
        }

        private void PrepareInstalledMapEnemies(
            LiveWallpaperMapViewport viewport,
            long elapsed,
            bool animated,
            LiveWallpaperSimulatedLinkState? link)
        {
            if (_overworldMap?.Map == null)
                return;
            if (_mapEnemyStates.Length != _overworldMap.Map.Enemies.Count)
                _mapEnemyStates = new LiveWallpaperEnemyState[
                    _overworldMap.Map.Enemies.Count];
            if (_activeMapEnemies.Length != _overworldMap.Map.Enemies.Count)
                _activeMapEnemies = new bool[_overworldMap.Map.Enemies.Count];
            else
                Array.Clear(_activeMapEnemies, 0, _activeMapEnemies.Length);
            for (var enemyIndex = 0;
                 enemyIndex < _overworldMap.Map.Enemies.Count;
                 enemyIndex++)
            {
                var enemy = _overworldMap.Map.Enemies[enemyIndex];
                if (!IsNearViewport(
                        viewport, enemy.EntityX, enemy.EntityY, 128f))
                    continue;
                var state = _enemySimulation.Resolve(
                    _overworldMap.Map, enemyIndex, animated ? elapsed : 0L, link);
                _mapEnemyStates[enemyIndex] = state;
                _activeMapEnemies[enemyIndex] = true;
                _linkSimulation.UpdateLiveEnemyState(
                    _overworldMap.Map, enemyIndex, state);
                if (animated && state.LinkHit.Valid)
                    _linkSimulation.ApplyEnemyHit(state.LinkHit, elapsed);
            }
        }

        private void DrawInstalledMapPlayerLayer(
            Canvas canvas, LiveWallpaperMapViewport viewport, long elapsed,
            bool animated, LiveWallpaperSimulatedLinkState? link)
        {
            if (_overworldMap?.Map == null)
                return;

            _playerLayerDrawEntries.Clear();
            var sequence = 0;
            for (var decorationIndex = 0;
                 decorationIndex < _overworldMap.Map.Decorations.Count;
                 decorationIndex++)
            {
                var decoration = _overworldMap.Map.Decorations[decorationIndex];
                if (!decoration.PlayerLayer ||
                    !IsNearViewport(
                        viewport, decoration.EntityX, decoration.EntityY, 64f))
                    continue;
                _playerLayerDrawEntries.Add(new PlayerLayerDrawEntry(
                    PlayerLayerDrawKind.Decoration, decorationIndex,
                    decoration.EntityY, sequence++));
            }
            for (var lampIndex = 0;
                 lampIndex < _overworldMap.Map.Lamps.Count;
                 lampIndex++)
            {
                var lamp = _overworldMap.Map.Lamps[lampIndex];
                if (!lamp.PlayerLayer || !IsNearViewport(
                        viewport, lamp.EntityX, lamp.EntityY, 32f))
                    continue;
                _playerLayerDrawEntries.Add(new PlayerLayerDrawEntry(
                    PlayerLayerDrawKind.Lamp, lampIndex,
                    lamp.EntityY, sequence++));
            }
            for (var actorIndex = 0;
                 actorIndex < _overworldMap.Map.Actors.Count &&
                 actorIndex < _mapActorStates.Length;
                 actorIndex++)
            {
                if (actorIndex >= _activeMapActors.Length ||
                    !_activeMapActors[actorIndex])
                    continue;
                var actor = _overworldMap.Map.Actors[actorIndex];
                var state = _mapActorStates[actorIndex];
                if (!state.Visible)
                    continue;
                var positionY = actor.Kind == LiveWallpaperMapActorKind.WeatherBird
                    ? actor.PixelY + 30f
                    : state.EntityY;
                if (!IsNearViewport(viewport, state.EntityX, positionY, 64f))
                    continue;
                _playerLayerDrawEntries.Add(new PlayerLayerDrawEntry(
                    PlayerLayerDrawKind.Actor, actorIndex, positionY, sequence++));
            }
            for (var enemyIndex = 0;
                 enemyIndex < _overworldMap.Map.Enemies.Count &&
                 enemyIndex < _mapEnemyStates.Length;
                 enemyIndex++)
            {
                if (enemyIndex >= _activeMapEnemies.Length ||
                    !_activeMapEnemies[enemyIndex])
                    continue;
                var state = _mapEnemyStates[enemyIndex];
                if (state.Projectile.Visible && IsNearViewport(
                        viewport, state.Projectile.PixelX,
                        state.Projectile.PixelY, 32f))
                    _playerLayerDrawEntries.Add(new PlayerLayerDrawEntry(
                        PlayerLayerDrawKind.EnemyProjectile, enemyIndex,
                        state.Projectile.PixelY, sequence++));
                if (!state.Visible || !IsNearViewport(
                        viewport, state.PixelX, state.PixelY, 32f))
                    continue;
                _playerLayerDrawEntries.Add(new PlayerLayerDrawEntry(
                    PlayerLayerDrawKind.Enemy, enemyIndex,
                    state.PixelY, sequence++));
            }
            if (link.HasValue && IsNearViewport(
                    viewport, link.Value.MapX * 16f, link.Value.MapY * 16f, 64f))
                _playerLayerDrawEntries.Add(new PlayerLayerDrawEntry(
                    PlayerLayerDrawKind.Link, 0,
                    link.Value.MapY * 16f, sequence));

            _playerLayerDrawEntries.Sort();
            foreach (var entry in _playerLayerDrawEntries)
            {
                switch (entry.Kind)
                {
                    case PlayerLayerDrawKind.Decoration:
                        DrawInstalledMapDecoration(
                            canvas, viewport, entry.Index, link);
                        break;
                    case PlayerLayerDrawKind.Lamp:
                        DrawInstalledMapLamp(
                            canvas, viewport, elapsed, animated, entry.Index);
                        break;
                    case PlayerLayerDrawKind.Actor:
                        DrawInstalledMapActor(
                            canvas, viewport, elapsed, animated, link, entry.Index);
                        break;
                    case PlayerLayerDrawKind.Enemy:
                        DrawInstalledMapEnemy(
                            canvas, viewport, elapsed, animated, entry.Index);
                        break;
                    case PlayerLayerDrawKind.EnemyProjectile:
                        DrawEnemyProjectile(
                            canvas, viewport, elapsed, animated,
                            _mapEnemyStates[entry.Index].Projectile,
                            viewport.TileSize / 16f);
                        break;
                    case PlayerLayerDrawKind.Link:
                        DrawLink(canvas, viewport, elapsed, animated, link.Value);
                        DrawHookshot(canvas, viewport, link.Value);
                        DrawCutVegetationEffects(
                            canvas, viewport, elapsed, link.Value);
                        DrawStoneImpactEffects(
                            canvas, viewport, elapsed, animated, link.Value);
                        break;
                }
            }
        }

        private static bool IsNearViewport(
            LiveWallpaperMapViewport viewport, float pixelX, float pixelY,
            float margin)
        {
            var left = viewport.OriginX * 16f - margin;
            var top = viewport.OriginY * 16f - margin;
            var right = (viewport.OriginX + viewport.Columns) * 16f + margin;
            var bottom = (viewport.OriginY + viewport.Rows) * 16f + margin;
            return pixelX >= left && pixelX <= right &&
                   pixelY >= top && pixelY <= bottom;
        }

        private void DrawInstalledMapEnemy(
            Canvas canvas, LiveWallpaperMapViewport viewport, long elapsed,
            bool animated, int enemyIndex)
        {
            if (_overworldMap?.Map == null ||
                enemyIndex < 0 ||
                enemyIndex >= _overworldMap.Map.Enemies.Count ||
                enemyIndex >= _mapEnemyStates.Length)
                return;
            var enemy = _overworldMap.Map.Enemies[enemyIndex];
            var state = _mapEnemyStates[enemyIndex];
            if (!state.Visible || !_mapEnemies.TryGetValue(enemy.Kind, out var set))
                return;
            var asset = set.Resolve(state);
            if (asset == null)
                return;
            var scale = viewport.TileSize / 16f;
            var anchorX = viewport.Left +
                          (state.PixelX / 16f - viewport.OriginX) *
                          viewport.TileSize;
            var anchorY = viewport.Top +
                          (state.PixelY / 16f - viewport.OriginY) *
                          viewport.TileSize;
            // Use the AnimationComponent offset declared by the actual enemy class.
            // ANI frame offsets are applied inside DrawSpriteAt; these are the separate
            // per-object offsets passed to AnimationComponent by each constructor.
            var componentOffset = enemy.Kind switch
            {
                LiveWallpaperMapEnemyKind.Octorok => (-8f, -15f),
                LiveWallpaperMapEnemyKind.RedZol => (-6f, -16f),
                LiveWallpaperMapEnemyKind.RiverZora => (-8f, -8f),
                LiveWallpaperMapEnemyKind.Ghini => (0f, 0f),
                LiveWallpaperMapEnemyKind.Pincer => (-8f, -8f),
                _ => (-8f, -16f)
            };
            anchorX += componentOffset.Item1 * scale;
            anchorY += componentOffset.Item2 * scale;
            if (anchorX < viewport.Left - 32f * scale ||
                anchorX > viewport.Left + viewport.Columns * viewport.TileSize +
                32f * scale ||
                anchorY < viewport.Top - 32f * scale ||
                anchorY > viewport.Top + viewport.Rows * viewport.TileSize +
                32f * scale)
                return;
            if (enemy.Kind == LiveWallpaperMapEnemyKind.Pincer &&
                state.Action == LiveWallpaperEnemyAction.Attack)
                DrawPincerTail(canvas, asset.Bitmap, viewport, enemy, state, scale);
            DrawSpriteAt(canvas, asset, elapsed, anchorX, anchorY, scale,
                engineDriven: true, animated: animated);
        }

        private void DrawEnemyProjectile(
            Canvas canvas, LiveWallpaperMapViewport viewport,
            long elapsed, bool animated,
            LiveWallpaperEnemyProjectileState projectile, float scale)
        {
            if (!projectile.Visible)
                return;
            SpriteAsset asset;
            var componentOffsetX = 0f;
            var componentOffsetY = -projectile.Height;
            switch (projectile.Kind)
            {
                case LiveWallpaperEnemyProjectileKind.OctorokShot:
                    asset = _enemyOctorokShot;
                    componentOffsetX = -5f;
                    componentOffsetY += -10f;
                    break;
                case LiveWallpaperEnemyProjectileKind.Spear:
                    asset = _enemySpears[projectile.Direction];
                    break;
                case LiveWallpaperEnemyProjectileKind.Fireball:
                    asset = _enemyFireball;
                    componentOffsetX = -5f;
                    componentOffsetY += -5f;
                    break;
                default:
                    return;
            }
            if (asset == null)
                return;
            var anchorX = viewport.Left +
                          (projectile.PixelX / 16f - viewport.OriginX) *
                          viewport.TileSize + componentOffsetX * scale;
            var anchorY = viewport.Top +
                          (projectile.PixelY / 16f - viewport.OriginY) *
                          viewport.TileSize + componentOffsetY * scale;
            DrawSpriteAt(canvas, asset, elapsed, anchorX, anchorY, scale,
                engineDriven: true, animated: animated);
        }

        private void DrawPincerTail(
            Canvas canvas, Bitmap bitmap, LiveWallpaperMapViewport viewport,
            LiveWallpaperMapEnemy enemy, LiveWallpaperEnemyState state, float scale)
        {
            if (bitmap == null || bitmap.Width < 192 || bitmap.Height < 132)
                return;
            var source = new Rect(184, 124, 192, 132);
            for (var index = 0; index < 3; index++)
            {
                var progress = 0.2f + index / 2f * 0.5f;
                var pixelX = enemy.EntityX + (state.PixelX - enemy.EntityX) * progress - 4f;
                var pixelY = enemy.EntityY + (state.PixelY - enemy.EntityY) * progress - 4f;
                var left = viewport.Left +
                           (pixelX / 16f - viewport.OriginX) * viewport.TileSize;
                var top = viewport.Top +
                          (pixelY / 16f - viewport.OriginY) * viewport.TileSize;
                canvas.DrawBitmap(bitmap, source,
                    new RectF(left, top, left + 8f * scale, top + 8f * scale),
                    _bitmapPaint);
            }
        }

        private void PrepareInstalledMapActors(
            LiveWallpaperMapViewport viewport,
            long elapsed,
            bool animated,
            LiveWallpaperSimulatedLinkState? link)
        {
            if (_overworldMap?.Map == null)
                return;
            if (_mapActorStates.Length != _overworldMap.Map.Actors.Count)
                _mapActorStates = new LiveWallpaperActorState[
                    _overworldMap.Map.Actors.Count];
            if (_activeMapActors.Length != _overworldMap.Map.Actors.Count)
                _activeMapActors = new bool[_overworldMap.Map.Actors.Count];
            else
                Array.Clear(_activeMapActors, 0, _activeMapActors.Length);
            for (var actorIndex = 0;
                 actorIndex < _overworldMap.Map.Actors.Count;
                 actorIndex++)
            {
                var actor = _overworldMap.Map.Actors[actorIndex];
                if (!IsNearViewport(
                        viewport, actor.PixelX, actor.PixelY, 128f))
                    continue;
                var actorState = _actorSimulation.Resolve(
                    _overworldMap.Map, actorIndex,
                    animated ? elapsed : 0L, link);
                _mapActorStates[actorIndex] = actorState;
                _activeMapActors[actorIndex] = true;
                _linkSimulation.UpdateLiveActorState(
                    _overworldMap.Map, actorIndex, actorState);
            }
        }

        private void DrawInstalledMapActor(
            Canvas canvas, LiveWallpaperMapViewport viewport, long elapsed,
            bool animated, LiveWallpaperSimulatedLinkState? link, int actorIndex)
        {
            if (_overworldMap?.Map == null ||
                actorIndex < 0 ||
                actorIndex >= _overworldMap.Map.Actors.Count ||
                actorIndex >= _mapActorStates.Length)
                return;
            var actor = _overworldMap.Map.Actors[actorIndex];
            var actorState = _mapActorStates[actorIndex];
            if (!actorState.Visible)
                return;
            var facingDirection = actorIndex == link?.InteractionActorIndex
                ? ResolveActorFacingDirection(actor, link.Value)
                : -1;
            var asset = ResolveMapActorAsset(
                actor, actorState, facingDirection, actorIndex, link);
            if (asset == null)
                return;

            var anchorPixelX = (float)actor.PixelX;
            var anchorPixelY = (float)actor.PixelY;
            var mobile = actor.Kind is LiveWallpaperMapActorKind.Dog or
                LiveWallpaperMapActorKind.Butterfly or
                LiveWallpaperMapActorKind.Bird or
                LiveWallpaperMapActorKind.BowWow or
                LiveWallpaperMapActorKind.Frog or
                LiveWallpaperMapActorKind.Mouse or
                LiveWallpaperMapActorKind.BowWowSmall or
                LiveWallpaperMapActorKind.LetterBird or
                LiveWallpaperMapActorKind.Owl;
            if (mobile)
            {
                anchorPixelX = actorState.EntityX;
                anchorPixelY = actorState.EntityY - actorState.Height;
            }
            switch (actor.Kind)
            {
                case LiveWallpaperMapActorKind.Person:
                case LiveWallpaperMapActorKind.Grandmother:
                case LiveWallpaperMapActorKind.Fisherman:
                case LiveWallpaperMapActorKind.Mermaid:
                    anchorPixelX += 8;
                    anchorPixelY += 16;
                    break;
                case LiveWallpaperMapActorKind.LegacyPerson:
                    var legacyWidth = asset.Animation.Frames.Max(
                        frame => frame.Width);
                    var legacyHeight = asset.Animation.Frames.Max(
                        frame => frame.Height);
                    anchorPixelX += 8 - legacyWidth / 2f + actor.SpriteOffsetX;
                    anchorPixelY += 16 - legacyHeight + actor.SpriteOffsetY;
                    break;
                case LiveWallpaperMapActorKind.Alligator:
                    // ObjAlligator: entity (+8,+16), component offset (-13,-23).
                    anchorPixelX -= 5;
                    anchorPixelY -= 7;
                    break;
                case LiveWallpaperMapActorKind.ChickenDude:
                case LiveWallpaperMapActorKind.Hippo:
                case LiveWallpaperMapActorKind.Painter:
                case LiveWallpaperMapActorKind.Tracy:
                case LiveWallpaperMapActorKind.LetterBoy:
                case LiveWallpaperMapActorKind.LetterGirl:
                case LiveWallpaperMapActorKind.PhotoMouse:
                    anchorPixelX += 8;
                    anchorPixelY += 16;
                    break;
                case LiveWallpaperMapActorKind.Frog:
                    // ObjFrog's AnimationComponent offset is (-7,-12).
                    anchorPixelX -= 7;
                    anchorPixelY -= 12;
                    break;
                case LiveWallpaperMapActorKind.Mouse:
                    // ObjMouse uses (-9,-Animator.FrameHeight), which is 14.
                    anchorPixelX -= 9;
                    anchorPixelY -= 14;
                    break;
                case LiveWallpaperMapActorKind.LetterBird:
                    // ObjLetterBird's AnimationComponent offset is (-8,-16).
                    anchorPixelX -= 8;
                    anchorPixelY -= 16;
                    break;
                case LiveWallpaperMapActorKind.Fairy:
                    anchorPixelX += 8;
                    anchorPixelY += 16 -
                        LiveWallpaperActorSimulation.ResolveFairyHeight(
                            elapsed, animated);
                    break;
                case LiveWallpaperMapActorKind.Raccoon:
                    // ObjRaccoon: entity (+8,+16), sprite offset (-8,-16).
                    break;
                case LiveWallpaperMapActorKind.WeatherBird:
                    // ObjWeatherBird: entity (+1,+30), sprite offset (0,-30).
                    anchorPixelX += 1;
                    break;
            }

            var scale = viewport.TileSize / 16f;
            var anchorX = viewport.Left +
                          (anchorPixelX / 16f - viewport.OriginX) *
                          viewport.TileSize;
            var anchorY = viewport.Top +
                          (anchorPixelY / 16f - viewport.OriginY) *
                          viewport.TileSize;
            if (anchorX < viewport.Left - 64f * scale ||
                anchorX > viewport.Left + viewport.Columns * viewport.TileSize +
                64f * scale ||
                anchorY < viewport.Top - 64f * scale ||
                anchorY > viewport.Top + viewport.Rows * viewport.TileSize +
                64f * scale)
                return;
            if (actor.Kind == LiveWallpaperMapActorKind.BowWow)
                DrawInstalledBowWowChain(
                    canvas, viewport, actor, actorState, scale);
            DrawSpriteAt(canvas, asset, elapsed, anchorX, anchorY, scale,
                engineDriven: true, animated: animated);
            if (actor.Kind == LiveWallpaperMapActorKind.Person &&
                string.Equals(
                    actor.AnimationId, "marin",
                    StringComparison.OrdinalIgnoreCase) &&
                actor.AnimationName?.Contains(
                    "sing", StringComparison.OrdinalIgnoreCase) == true)
                DrawMarinNotes(canvas, anchorX, anchorY, elapsed, scale);
        }

        private SpriteAsset ResolveMapActorAsset(
            LiveWallpaperMapActor actor,
            LiveWallpaperActorState state,
            int facingDirection,
            int actorIndex,
            LiveWallpaperSimulatedLinkState? link)
        {
            var horizontalDirection = facingDirection switch
            {
                0 => 0,
                2 => 1,
                _ => state.Direction == 0 ? 0 : 1
            };
            var fourWayDirection = facingDirection >= 0
                ? Math.Clamp(facingDirection, 0, 3)
                : state.Direction;
            return actor.Kind switch
            {
                LiveWallpaperMapActorKind.Person => ResolveMapPersonAsset(
                    actor, facingDirection),
                LiveWallpaperMapActorKind.LegacyPerson => ResolveMapPersonAsset(
                    actor, ResolveLegacyPersonDirection(actor, link)),
                LiveWallpaperMapActorKind.Dog =>
                    _mapDogDirections[horizontalDirection],
                LiveWallpaperMapActorKind.Grandmother =>
                    ResolveGrandmotherAsset(actor, actorIndex, link),
                LiveWallpaperMapActorKind.Raccoon =>
                    LiveWallpaperActorSimulation.ShouldRaccoonLaugh(actor, link)
                        ? _mapRaccoonLaugh
                        : _mapRaccoonIdle,
                LiveWallpaperMapActorKind.WeatherBird => _mapWeatherBird,
                LiveWallpaperMapActorKind.Owl =>
                    state.Action == LiveWallpaperActorAction.Fly
                        ? _mapOwlFlying
                        : _mapOwl,
                LiveWallpaperMapActorKind.Butterfly => _butterfly,
                LiveWallpaperMapActorKind.Bird =>
                    state.Action == LiveWallpaperActorAction.Walk
                        ? _mapBirdWalking[horizontalDirection]
                        : _mapBirdIdle[horizontalDirection],
                LiveWallpaperMapActorKind.BowWow =>
                    _mapBowWowDirections[fourWayDirection],
                LiveWallpaperMapActorKind.Frog =>
                    state.Action == LiveWallpaperActorAction.Walk
                        ? _mapFrogJumping[fourWayDirection]
                        : _mapFrogSitting[fourWayDirection],
                LiveWallpaperMapActorKind.Mouse =>
                    state.Action == LiveWallpaperActorAction.Walk
                        ? _mapMouseWalking[horizontalDirection]
                        : _mapMouseStanding[horizontalDirection],
                LiveWallpaperMapActorKind.BowWowSmall =>
                    _mapBowWowSmall[horizontalDirection],
                LiveWallpaperMapActorKind.Alligator => _mapAlligator,
                LiveWallpaperMapActorKind.ChickenDude =>
                    state.Action == LiveWallpaperActorAction.Walk
                        ? _mapChickenDudePowder[state.Direction == 0 ? 0 : 1]
                        : _mapChickenDudeIdle[state.Direction == 0 ? 0 : 1],
                LiveWallpaperMapActorKind.Hippo =>
                    state.Action == LiveWallpaperActorAction.Walk
                        ? _mapHippoEmbarrassed[state.Direction == 0 ? 0 : 1]
                        : _mapHippoStanding[state.Direction == 0 ? 0 : 1],
                LiveWallpaperMapActorKind.Painter =>
                    ResolvePainterAsset(actor, actorIndex, link),
                LiveWallpaperMapActorKind.Tracy =>
                    ResolveTracyAsset(actor, link),
                LiveWallpaperMapActorKind.LetterBoy =>
                    state.Action == LiveWallpaperActorAction.Walk
                        ? _mapLetterBoyLook[state.Direction == 0 ? 0 : 1]
                        : _mapLetterBoyIdle,
                LiveWallpaperMapActorKind.LetterGirl =>
                    state.Action == LiveWallpaperActorAction.Walk
                        ? _mapLetterGirlLook[state.Direction == 0 ? 0 : 1]
                        : _mapLetterGirlIdle,
                LiveWallpaperMapActorKind.LetterBird =>
                    string.Equals(actor.AnimationId, "letterBirdGreen",
                        StringComparison.OrdinalIgnoreCase)
                        ? _mapLetterBirdGreen[horizontalDirection]
                        : _mapLetterBird[horizontalDirection],
                LiveWallpaperMapActorKind.PhotoMouse => _mapPhotoMouse,
                LiveWallpaperMapActorKind.Fisherman =>
                    LiveWallpaperActorSimulation.IsInteraction(link, actorIndex)
                        ? _mapFishermanTalk
                        : _mapFishermanStand,
                LiveWallpaperMapActorKind.Mermaid => _mapMermaid,
                LiveWallpaperMapActorKind.Fairy => _mapFairy,
                _ => null
            };
        }

        private static int ResolveLegacyPersonDirection(
            LiveWallpaperMapActor actor,
            LiveWallpaperSimulatedLinkState? link)
        {
            if (!string.IsNullOrWhiteSpace(actor.AnimationName) ||
                !link.HasValue)
                return -1;
            var actorX = actor.PixelX + 8f;
            var actorY = actor.PixelY + 16f;
            var linkX = link.Value.MapX * 16f;
            var linkY = link.Value.MapY * 16f;
            var deltaX = linkX - actorX;
            var deltaY = linkY - (actorY - 4f);
            if (deltaX * deltaX + deltaY * deltaY >= 32f * 32f)
                return 3;
            return ResolveActorFacingDirection(actor, link.Value);
        }

        private SpriteAsset ResolvePainterAsset(
            LiveWallpaperMapActor actor,
            int actorIndex,
            LiveWallpaperSimulatedLinkState? link)
        {
            if (!LiveWallpaperActorSimulation.IsInteraction(link, actorIndex))
                return _mapPainterIdle;
            return actor.PixelX + 8f < link.Value.MapX * 16f
                ? _mapPainterTalk[1]
                : _mapPainterTalk[0];
        }

        private SpriteAsset ResolveTracyAsset(
            LiveWallpaperMapActor actor,
            LiveWallpaperSimulatedLinkState? link)
        {
            if (!link.HasValue)
                return _mapTracyIdle;
            var deltaX = link.Value.MapX * 16f - (actor.PixelX + 8f);
            var deltaY = link.Value.MapY * 16f - (actor.PixelY + 16f);
            if (MathF.Abs(deltaX) <= MathF.Abs(deltaY))
                return _mapTracyIdle;
            return _mapTracySides[deltaX < 0 ? 0 : 1];
        }

        private SpriteAsset ResolveGrandmotherAsset(
            LiveWallpaperMapActor actor,
            int actorIndex,
            LiveWallpaperSimulatedLinkState? link)
        {
            var direction = _grandmotherDirections.TryGetValue(
                actorIndex, out var storedDirection)
                    ? storedDirection
                    : -1;
            direction = LiveWallpaperActorSimulation.ResolveGrandmotherDirection(
                actor, direction, link);
            _grandmotherDirections[actorIndex] = direction;
            return _mapGrandmotherDirections[direction < 0 ? 0 : 1];
        }

        private void DrawInstalledBowWowChain(
            Canvas canvas,
            LiveWallpaperMapViewport viewport,
            LiveWallpaperMapActor actor,
            LiveWallpaperActorState state,
            float scale)
        {
            if (_bowWowChain == null)
                return;
            var anchorX = viewport.Left +
                ((actor.PixelX + 8f) / 16f - viewport.OriginX) *
                viewport.TileSize;
            var anchorY = viewport.Top +
                ((actor.PixelY + 8f) / 16f - viewport.OriginY) *
                viewport.TileSize;
            var goalX = viewport.Left +
                (state.EntityX / 16f - viewport.OriginX) *
                viewport.TileSize;
            var goalY = viewport.Top +
                ((state.EntityY - state.Height - 4f) / 16f - viewport.OriginY) *
                viewport.TileSize;
            for (var index = 1; index <= 5; index++)
            {
                var progress = index / 6f;
                var linkX = anchorX + (goalX - anchorX) * progress;
                var linkY = anchorY + (goalY - anchorY) * progress +
                            MathF.Sin(progress * MathF.PI) * 2.5f * scale;
                DrawAtlasSpriteAt(
                    canvas, _bowWowChain, linkX, linkY, scale);
            }
        }

        private SpriteAsset ResolveMapPersonAsset(
            LiveWallpaperMapActor actor, int facingDirection)
        {
            var key = actor.AnimationId + "\n" +
                      (string.IsNullOrWhiteSpace(actor.AnimationName)
                          ? "stand_3"
                          : actor.AnimationName);
            if (!_mapPeople.TryGetValue(key, out var directions))
                return null;
            var direction = facingDirection >= 0
                ? Math.Clamp(facingDirection, 0, 3)
                : ResolveAnimationDirection(actor.AnimationName, 3);
            return directions[direction] ?? directions[3] ?? directions[0];
        }

        private static int ResolveAnimationDirection(string animationName, int fallback)
        {
            if (string.IsNullOrWhiteSpace(animationName))
                return fallback;
            var separator = animationName.LastIndexOf('_');
            return separator >= 0 &&
                   int.TryParse(animationName[(separator + 1)..], out var direction)
                ? Math.Clamp(direction, 0, 3)
                : fallback;
        }

        private static int ResolveActorFacingDirection(
            LiveWallpaperMapActor actor,
            LiveWallpaperSimulatedLinkState link)
        {
            var actorX = actor.BodyX + actor.BodyWidth / 2f;
            var actorY = actor.BodyY + actor.BodyHeight / 2f;
            var linkX = link.MapX * 16f;
            var linkY = link.MapY * 16f;
            var deltaX = linkX - actorX;
            var deltaY = linkY - actorY;
            if (MathF.Abs(deltaX) >= MathF.Abs(deltaY))
                return deltaX < 0 ? 0 : 2;
            return deltaY < 0 ? 1 : 3;
        }

        private void DrawLink(
            Canvas canvas,
            LiveWallpaperMapViewport viewport,
            long elapsed,
            bool animated,
            LiveWallpaperSimulatedLinkState simulated)
        {
            if (simulated.Action == LiveWallpaperLinkRouteAction.Hidden)
                return;
            var damageVisible = _linkSimulation.IsDamageVisible(elapsed);
            var direction = simulated.Direction;
            var asset = simulated.Action switch
            {
                LiveWallpaperLinkRouteAction.FeatherJump => _linkJumping[direction],
                // ObjLink.UpdateAnimation keeps the boots-running walk cycle
                // active while Link is airborne; it does not switch to the
                // ordinary feather-jump pose.
                LiveWallpaperLinkRouteAction.PegasusJump => _linkWalking[direction],
                LiveWallpaperLinkRouteAction.Swim => _linkSwimming[direction],
                LiveWallpaperLinkRouteAction.Pushing => _linkPushing[direction],
                LiveWallpaperLinkRouteAction.LiftStone => _linkLifting[direction],
                LiveWallpaperLinkRouteAction.CarryStone => _linkCarrying[direction],
                LiveWallpaperLinkRouteAction.ThrowStone => _linkThrowing[direction],
                LiveWallpaperLinkRouteAction.RoosterPickup =>
                    simulated.ActionProgress <
                    RoosterGameplayMotion.PullMilliseconds /
                    (float)RoosterGameplayMotion.PickupSequenceMilliseconds
                        ? _linkLifting[direction]
                        : _linkFlying[direction],
                LiveWallpaperLinkRouteAction.RoosterFly => _linkFlying[direction],
                LiveWallpaperLinkRouteAction.RoosterThrow => _linkThrowing[direction],
                LiveWallpaperLinkRouteAction.Falling => _linkFalling,
                LiveWallpaperLinkRouteAction.Attack => _linkAttacking[direction],
                LiveWallpaperLinkRouteAction.Hookshot => _linkUsingItem[direction],
                LiveWallpaperLinkRouteAction.ShowItem =>
                    _linkShowingItem[simulated.ChestItemShowAnimation],
                LiveWallpaperLinkRouteAction.PegasusCharge =>
                    _linkWalking[direction],
                LiveWallpaperLinkRouteAction.PegasusDash =>
                    _linkWalking[direction],
                LiveWallpaperLinkRouteAction.Walk => _linkWalking[direction],
                _ => _linkStanding[direction]
            };
            asset ??= _linkStanding[direction] ?? _linkWalking[direction];
            if (asset == null)
                return;
            var placement = LiveWallpaperLinkPlacement.Resolve(viewport, simulated);
            var attacking = simulated.Action == LiveWallpaperLinkRouteAction.Attack;
            var sword = attacking ? _linkSwords[direction] : null;
            var pegasus = simulated.Action is
                LiveWallpaperLinkRouteAction.PegasusCharge or
                LiveWallpaperLinkRouteAction.PegasusDash or
                LiveWallpaperLinkRouteAction.PegasusJump;
            var animationSpeed = pegasus
                ? LinkGameplayMotion.PegasusBootsSpeed
                : 1f;
            if (attacking && _lastLinkAction != LiveWallpaperLinkRouteAction.Attack)
            {
                asset.EngineAnimation.Restart(elapsed);
                sword?.EngineAnimation.Restart(elapsed);
            }
            if (simulated.Action is LiveWallpaperLinkRouteAction.LiftStone or
                    LiveWallpaperLinkRouteAction.CarryStone or
                    LiveWallpaperLinkRouteAction.ThrowStone &&
                _lastLinkAction != simulated.Action)
                asset.EngineAnimation.Restart(elapsed);
            if (simulated.Action == LiveWallpaperLinkRouteAction.Falling &&
                _lastLinkAction != LiveWallpaperLinkRouteAction.Falling)
                asset.EngineAnimation.Restart(elapsed);
            if (simulated.Action == LiveWallpaperLinkRouteAction.ShowItem &&
                _lastLinkAction != LiveWallpaperLinkRouteAction.ShowItem)
                asset.EngineAnimation.Restart(elapsed);
            var drawActiveStone = damageVisible &&
                                  simulated.ActiveLiftedStoneKey >= 0 &&
                                  simulated.StoneImpactKind ==
                                      LiveWallpaperStoneImpactKind.None;
            var drawStoneBeforeLink = drawActiveStone &&
                                      LiveWallpaperLinkPlacement
                                          .DrawActiveStoneBeforeLink(simulated);
            if (drawStoneBeforeLink)
                DrawLiftedStone(canvas, viewport, simulated);
            // ObjLink draws its body behind the sword when facing left/down, and
            // in front when facing up/right. Both animators use the same
            // EntityPosition + (-7,-16) base point.
            if (damageVisible && (!attacking || direction is 0 or 3))
                DrawSpriteAt(canvas, asset, elapsed,
                    placement.AnchorX, placement.AnchorY, placement.Scale,
                    engineDriven: true, animated: animated,
                    speedMultiplier: animationSpeed);
            if (damageVisible && sword != null)
                DrawSpriteAt(canvas, sword, elapsed,
                    placement.AnchorX, placement.AnchorY, placement.Scale,
                    engineDriven: true, animated: animated);
            if (damageVisible && attacking && direction is 1 or 2)
                DrawSpriteAt(canvas, asset, elapsed,
                    placement.AnchorX, placement.AnchorY, placement.Scale,
                    engineDriven: true, animated: animated,
                    speedMultiplier: animationSpeed);
            if (drawActiveStone && !drawStoneBeforeLink)
                DrawLiftedStone(canvas, viewport, simulated);
            DrawChestItem(canvas, viewport, simulated);
            _lastLinkAction = simulated.Action;
            if (pegasus && _pegasusDust?.Animation != null)
            {
                var particleElapsed = elapsed %
                    (long)LinkGameplayMotion.PegasusBootsParticleMilliseconds;
                var entityX = viewport.Left +
                    (simulated.MapX - viewport.OriginX) * viewport.TileSize;
                var entityY = viewport.Top +
                    (simulated.MapY - viewport.OriginY) * viewport.TileSize;
                DrawSpriteAt(
                    canvas, _pegasusDust, particleElapsed, entityX, entityY,
                    viewport.TileSize / 16f);
            }
            DrawJourneyRooster(canvas, viewport, elapsed, animated, simulated);
        }

        private void DrawChestItem(
            Canvas canvas,
            LiveWallpaperMapViewport viewport,
            LiveWallpaperSimulatedLinkState link)
        {
            if (link.ActiveChestKey < 0 ||
                string.IsNullOrWhiteSpace(link.ChestItemSpriteId) ||
                !_chestItems.TryGetValue(
                    link.ChestItemSpriteId, out var item) ||
                item?.Bitmap == null)
                return;
            if (link.Action is not (LiveWallpaperLinkRouteAction.OpenChest or
                    LiveWallpaperLinkRouteAction.ShowItem))
                return;

            var scale = viewport.TileSize / 16f;
            var linkX = viewport.Left +
                        (link.MapX - viewport.OriginX) * viewport.TileSize;
            var linkY = viewport.Top +
                        (link.MapY - viewport.OriginY) * viewport.TileSize -
                        link.Height * scale;
            var itemX = linkX - item.Entry.Width * scale * 0.5f;
            float itemY;
            if (link.Action == LiveWallpaperLinkRouteAction.OpenChest)
            {
                // ObjChest.OpeningTick raises the spawned item along this exact
                // sine curve before ObjLink transitions to its pickup pose.
                var itemHeight = ChestGameplayPresentation.ResolveItemHeight(
                    link.ActionProgress);
                itemY = linkY +
                    (-10f - item.Entry.Height - itemHeight) * scale;
            }
            else
            {
                if (link.ChestItemShowAnimation == 2)
                    itemX -= 4f * scale;
                itemY = linkY + (-15f - item.Entry.Height) * scale;
            }
            DrawAtlasTopLeftAt(canvas, item, itemX, itemY, scale);
        }

        private void DrawHookshot(
            Canvas canvas,
            LiveWallpaperMapViewport viewport,
            LiveWallpaperSimulatedLinkState link)
        {
            if (!link.HookshotVisible || _hookshotChain == null ||
                _hookshotHook == null)
                return;
            var scale = viewport.TileSize / 16f;
            var linkX = viewport.Left +
                        (link.MapX - viewport.OriginX) * viewport.TileSize;
            var linkY = viewport.Top +
                        (link.MapY - viewport.OriginY) * viewport.TileSize;
            var hookX = viewport.Left +
                        (link.HookshotMapX - viewport.OriginX) *
                        viewport.TileSize;
            var hookY = viewport.Top +
                        (link.HookshotMapY - viewport.OriginY) *
                        viewport.TileSize;
            var handOffset = link.Direction switch
            {
                0 => (-5f, -4f),
                1 => (-3f, -12f),
                2 => (5f, -4f),
                _ => (3f, 0f)
            };
            var handX = linkX + handOffset.Item1 * scale;
            var handY = linkY + handOffset.Item2 * scale;
            var deltaX = hookX - handX;
            var deltaY = hookY - handY;
            var previousAlpha = _bitmapPaint.Alpha;
            _bitmapPaint.Alpha = 128;
            for (var index = 0; index < 3; index++)
            {
                var progress = (index + 0.75f) / 4f;
                DrawAtlasTopLeft(
                    canvas, _hookshotChain,
                    handX - 2f * scale + deltaX * progress,
                    handY - 2f * scale + deltaY * progress,
                    scale);
            }
            _bitmapPaint.Alpha = previousAlpha;
            DrawAtlasTopLeft(
                canvas, _hookshotHook,
                hookX - 7f * scale, hookY - 7f * scale, scale);
        }

        private void DrawLiftedStone(
            Canvas canvas,
            LiveWallpaperMapViewport viewport,
            LiveWallpaperSimulatedLinkState link)
        {
            if (_overworldMap?.Map == null || link.ActiveLiftedStoneKey < 0)
                return;
            foreach (var decoration in _overworldMap.Map.Decorations)
            {
                if (!decoration.StoneLayout)
                    continue;
                if (_overworldMap.Map.GetStoneKey(decoration) !=
                    link.ActiveLiftedStoneKey)
                    continue;
                var assetKey = decoration.AtlasName + "\n" + decoration.SpriteId;
                if (_mapDecorations.TryGetValue(assetKey, out var stone) &&
                    stone != null)
                {
                    var scale = viewport.TileSize / 16f;
                    var entityX = viewport.Left +
                                  (link.ActiveStoneEntityX / 16f -
                                   viewport.OriginX) * viewport.TileSize;
                    var entityY = viewport.Top +
                                  (link.ActiveStoneEntityY / 16f -
                                   viewport.OriginY) * viewport.TileSize -
                                  link.ActiveStoneHeight * scale;
                    // ObjStone keeps the same CSprite offset while it is
                    // pulled, carried, and thrown.  Do not switch to the
                    // generic atlas origin after hiding the map decoration.
                    DrawAtlasStoneAt(canvas, stone, entityX, entityY, scale);
                }
                return;
            }
        }

        private void DrawCutVegetationEffects(
            Canvas canvas,
            LiveWallpaperMapViewport viewport,
            long elapsed,
            LiveWallpaperSimulatedLinkState link)
        {
            if (_vegetationLeaf?.Bitmap == null ||
                link.CutVegetationTimes == null ||
                _overworldMap?.Map == null)
                return;
            var scale = viewport.TileSize / 16f;
            foreach (var decoration in _overworldMap.Map.Decorations)
            {
                var grass = LiveWallpaperMap.IsGrassSprite(decoration.SpriteId);
                if (!grass && !LiveWallpaperMap.IsBushSprite(decoration.SpriteId))
                    continue;
                var left = decoration.EntityX - 8;
                var top = decoration.EntityY - 8;
                var key = _overworldMap.Map.GetBushKey(left, top);
                if (!link.CutVegetationTimes.TryGetValue(key, out var cutAt))
                    continue;
                var effectElapsed = elapsed - cutAt;
                for (var leafIndex = 0; leafIndex < 4; leafIndex++)
                {
                    if (!GameObjectVisualLayout.TryGetClassicLeafState(
                            leafIndex, effectElapsed, out var offset,
                            out var flipX, out var flipY, out var fade))
                        continue;
                    var drawX = viewport.Left +
                                ((left + offset.X) / 16f - viewport.OriginX) *
                                viewport.TileSize;
                    var drawY = viewport.Top +
                                ((top + offset.Y) / 16f - viewport.OriginY) *
                                viewport.TileSize;
                    DrawAtlasTopLeftAt(
                        canvas, _vegetationLeaf, drawX, drawY, scale,
                        flipX, flipY, fade * (grass ? 0.5f : 0.9f));
                }
                if (link.VegetationDrops == null ||
                    !link.VegetationDrops.TryGetValue(key, out var dropKind))
                    continue;
                var drop = dropKind switch
                {
                    LiveWallpaperVegetationDropKind.Heart => _vegetationHeart,
                    LiveWallpaperVegetationDropKind.Rupee => _vegetationRupee,
                    _ => null
                };
                if (drop?.Bitmap == null)
                    continue;
                // ObjItem anchors at map (+8,+11) and draws the source rectangle
                // centered horizontally with its bottom on that entity point.
                var entityX = viewport.Left +
                              ((left + 8f) / 16f - viewport.OriginX) *
                              viewport.TileSize;
                var entityY = viewport.Top +
                              ((top + 11f) / 16f - viewport.OriginY) *
                              viewport.TileSize;
                if (link.VegetationDropDirections != null &&
                    link.VegetationDropDirections.TryGetValue(
                        key, out var dropDirection))
                {
                    var motion = DroppedItemMotion.Resolve(
                        dropDirection, Math.Max(0L, effectElapsed));
                    entityX += motion.Offset.X * scale;
                    entityY += (motion.Offset.Y - motion.Height) * scale;
                }
                var alpha = 1f;
                if (link.CollectedVegetationDropTimes != null &&
                    link.CollectedVegetationDropTimes.TryGetValue(
                        key, out var collectedAt))
                {
                    DroppedItemMotion.ResolveCollectedVisual(
                        elapsed - collectedAt, out var collectionOffset,
                        out alpha, out var visible);
                    if (!visible)
                        continue;
                    entityY += collectionOffset * scale;
                }
                DrawAtlasTopLeftAt(
                    canvas, drop,
                    entityX - drop.Entry.Width * scale * 0.5f,
                    entityY - drop.Entry.Height * scale,
                    scale, alpha: alpha);
            }
        }

        private void DrawStoneImpactEffects(
            Canvas canvas,
            LiveWallpaperMapViewport viewport,
            long elapsed,
            bool animated,
            LiveWallpaperSimulatedLinkState link)
        {
            if (link.StoneImpactKind == LiveWallpaperStoneImpactKind.None)
                return;
            var effectElapsed = Math.Max(0L, elapsed - link.StoneImpactStartedAt);
            var scale = viewport.TileSize / 16f;
            var anchorX = viewport.Left +
                          (link.StoneImpactX / 16f - viewport.OriginX) *
                          viewport.TileSize;
            var anchorY = viewport.Top +
                          (link.StoneImpactY / 16f - viewport.OriginY) *
                          viewport.TileSize;
            if (link.StoneImpactKind == LiveWallpaperStoneImpactKind.Water)
            {
                if (_stoneSplash?.Animation == null ||
                    effectElapsed >= _stoneSplash.Animation.DurationMilliseconds)
                    return;
                DrawSpriteAt(canvas, _stoneSplash, effectElapsed,
                    anchorX, anchorY, scale, animated: animated);
                return;
            }
            if (link.StoneImpactKind == LiveWallpaperStoneImpactKind.Hole)
            {
                if (_stoneFall?.Animation == null ||
                    effectElapsed >= _stoneFall.Animation.DurationMilliseconds)
                    return;
                DrawSpriteAt(canvas, _stoneFall, effectElapsed,
                    anchorX - 5f * scale, anchorY - 5f * scale,
                    scale, animated: animated);
                return;
            }
            if (_stoneParticle?.Bitmap == null || effectElapsed >= 350L)
                return;

            var frames = effectElapsed / (1000f / 60f);
            var throwVelocityX = link.Direction switch
            {
                0 => -3f,
                2 => 3f,
                _ => 0f
            };
            var throwVelocityY = link.Direction switch
            {
                1 => -3f,
                3 => 3f,
                _ => 0f
            };
            var directions = new[]
            {
                (-1f, -1f, -2f, -10f, true),
                (-1f, 0f, -1f, -5f, true),
                (1f, -1f, 3f, -10f, false),
                (1f, 0f, 2f, -5f, false)
            };
            for (var index = 0; index < directions.Length; index++)
            {
                var value = directions[index];
                var random = 50f +
                             Math.Abs(link.StoneImpactSerial * 17 + index * 11) % 25;
                var velocityX = throwVelocityX * 0.125f +
                                value.Item1 * random / 200f;
                var velocityY = throwVelocityY * 0.125f +
                                value.Item2 * random / 200f;
                var x = anchorX +
                        (value.Item3 + velocityX * frames) * scale;
                var y = anchorY +
                        (value.Item4 + velocityY * frames -
                         (1.25f * frames - 0.15f * frames *
                          Math.Max(0f, frames - 1f) * 0.5f)) * scale;
                var alpha = effectElapsed <= 275L
                    ? 1f
                    : Math.Clamp(1f - (effectElapsed - 275L) / 75f, 0f, 1f);
                DrawAtlasTopLeftAt(
                    canvas, _stoneParticle, x, y, scale,
                    flipX: value.Item5, alpha: alpha);
            }
        }

        private LiveWallpaperSimulatedLinkState AttachSwordAttackBox(
            LiveWallpaperSimulatedLinkState link)
        {
            var sword = _linkSwords[Math.Clamp(link.Direction, 0, 3)];
            if (sword?.Animation == null)
                return link;
            var attackElapsed = (long)MathF.Round(link.ActionProgress * 233f);
            if (!sword.Animation.TryGetOneShotCollisionRectangle(
                    attackElapsed, out var collision))
                return link;
            return link.WithAttackBox(new LiveWallpaperAttackBox(
                collision.X + link.MapX * 16f - 7f,
                collision.Y + link.MapY * 16f - link.Height - 16f,
                collision.Width, collision.Height));
        }

        private void DrawJourneyRooster(
            Canvas canvas,
            LiveWallpaperMapViewport viewport,
            long elapsed,
            bool animated,
            LiveWallpaperSimulatedLinkState link)
        {
            if (!link.RoosterVisible)
                return;
            var direction = Math.Clamp(link.Direction, 0, 3);
            var asset = _roosterDirections[direction] ??
                        (direction is 0 or 1 ? _roosterLeft : _roosterRight);
            if (asset == null)
                return;
            var scale = viewport.TileSize / 16f;
            var anchorX = viewport.Left +
                          (link.RoosterMapX - viewport.OriginX) * viewport.TileSize;
            var anchorY = viewport.Top +
                          (link.RoosterMapY - viewport.OriginY) * viewport.TileSize -
                          link.RoosterHeight * scale;
            DrawSpriteAt(canvas, asset, elapsed, anchorX, anchorY, scale,
                engineDriven: true, animated: animated);
            if (link.CarryingRooster)
                DrawRoosterParticles(canvas, anchorX, anchorY, elapsed, scale);
        }

        private void DrawFeaturedCharacter(
            Canvas canvas,
            int width,
            float groundY,
            LiveWallpaperMapViewport viewport,
            long elapsed,
            float xOffset,
            float unit,
            bool animated,
            int featuredCharacter,
            int scene,
            int characterPosition,
            bool suppressRooster)
        {
            var selection = LiveWallpaperCharacterSelection.Resolve(
                featuredCharacter, scene, elapsed);
            if (suppressRooster && selection == 2)
                return;
            var motion = LiveWallpaperCharacterMotion.Resolve(selection, elapsed, animated);
            var baseX = width * LiveWallpaperSceneLayouts.ResolveFeaturedXRatio(
                characterPosition, scene) - (xOffset - 0.5f) * 20f * unit;
            var movementRadius = selection switch
            {
                1 => 18f,
                2 => 14f,
                _ => 0f
            };
            var mapScale = viewport.TileSize / 16f;
            var anchorMapX = (viewport.OriginX +
                              (baseX - viewport.Left) / viewport.TileSize) * 16f;
            var anchorMapY = (viewport.OriginY +
                              (groundY - viewport.Top) / viewport.TileSize) * 16f;
            var follower = _followerSimulation.Update(
                selection, motion.HorizontalOffset * movementRadius, elapsed, animated,
                _overworldMap?.Map, anchorMapX, anchorMapY, 1f);
            var centerX = baseX + follower.HorizontalOffset * mapScale;
            var bottomY = groundY - follower.Height * mapScale;
            var asset = selection switch
            {
                1 => follower.FacingRight ? _bowWowRight : _bowWowLeft,
                2 => follower.FacingRight ? _roosterRight : _roosterLeft,
                _ => _marin
            };
            if (asset == null)
                return;
            if (selection == 1)
                DrawBowWowChain(canvas, baseX, groundY, centerX, bottomY, mapScale);
            else if (selection == 2 && follower.Height > 3f)
                DrawRoosterParticles(canvas, centerX, bottomY, elapsed, mapScale);
            var spriteOffsetX = selection == 1 ? -8f * mapScale : 0f;
            var spriteOffsetY = selection == 1 ? -16f * mapScale : 0f;
            DrawSpriteAt(canvas, asset, elapsed,
                centerX + spriteOffsetX, bottomY + spriteOffsetY, mapScale,
                engineDriven: true, animated: animated);
            if (selection == 0 && motion.ShowNotes)
                DrawMarinNotes(canvas, centerX, groundY, elapsed, mapScale);
        }

        private void DrawMarinNotes(
            Canvas canvas, float entityX, float entityY, long elapsed, float unit)
        {
            if (_marinNote == null)
                return;
            const int cycleTime = 1000;
            for (var index = 0; index < 2; index++)
            {
                var directionX = index == 0 ? -0.4f : 0.4f;
                var time = (elapsed + index * (cycleTime / 2L)) % cycleTime;
                var noteX = entityX +
                    ((index == 0 ? -8f : 8f) -
                     _marinNote.Entry.Width / 2f) * unit;
                var noteY = entityY +
                    (-16f - _marinNote.Entry.Height / 2f) * unit;
                noteX += (directionX * time * 0.02f -
                          directionX * MathF.Sin(time * 0.015f) * 1.25f) * unit;
                noteY += (-time * 0.02f -
                          MathF.Sin(time * 0.015f) * 1.25f) * unit;
                var alpha = time > cycleTime - 100
                    ? (cycleTime - time) / 100f
                    : time < 100
                        ? time / 100f
                        : 1f;
                DrawAtlasTopLeftAt(
                    canvas, _marinNote,
                    noteX - _marinNote.Entry.OriginX * unit,
                    noteY - _marinNote.Entry.OriginY * unit,
                    unit, alpha: alpha);
            }
        }

        private void DrawBowWowChain(
            Canvas canvas,
            float anchorX,
            float anchorY,
            float bowWowX,
            float bowWowY,
            float unit)
        {
            if (_bowWowChain == null)
                return;
            for (var index = 1; index <= 5; index++)
            {
                var progress = index / 6f;
                var linkX = anchorX + (bowWowX - anchorX) * progress;
                var linkY = anchorY + (bowWowY - anchorY) * progress +
                            MathF.Sin(progress * MathF.PI) * 2.5f * unit;
                DrawAtlasSpriteAt(canvas, _bowWowChain, linkX, linkY,
                    Math.Max(1.5f, unit * 1.15f));
            }
        }

        private void DrawRoosterParticles(
            Canvas canvas, float centerX, float bottomY, long elapsed, float unit)
        {
            var sway = MathF.Sin(elapsed / 230f) * 2f * unit;
            DrawAtlasSpriteAt(canvas, _roosterParticleLarge,
                centerX - 9f * unit + sway, bottomY - 5f * unit,
                Math.Max(1.5f, unit));
            DrawAtlasSpriteAt(canvas, _roosterParticleMedium,
                centerX + 8f * unit - sway, bottomY - 10f * unit,
                Math.Max(1.5f, unit));
            DrawAtlasSpriteAt(canvas, _roosterParticleSmall,
                centerX - 3f * unit - sway, bottomY - 15f * unit,
                Math.Max(1.5f, unit));
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
            float scale,
            bool engineDriven = false,
            bool animated = true,
            float speedMultiplier = 1f)
        {
            if (asset?.Bitmap == null || asset.Animation == null)
                return;
            var frame = engineDriven
                ? asset.EngineAnimation.Advance(
                    elapsed, animated, speedMultiplier)
                : asset.Animation.GetFrame(elapsed);
            if (frame.Width == 0 && frame.Height == 0)
                return;
            if (frame.X < 0 || frame.Y < 0 ||
                frame.X + frame.Width > asset.Bitmap.Width ||
                frame.Y + frame.Height > asset.Bitmap.Height)
                return;

            var source = new Rect(frame.X, frame.Y, frame.X + frame.Width, frame.Y + frame.Height);
            var placement = asset.Animation.GetPlacement(frame, centerX, bottomY, scale);
            var destination = new RectF(
                placement.Left, placement.Top, placement.Right, placement.Bottom);
            var save = canvas.Save();
            if (frame.MirroredHorizontally)
                canvas.Scale(-1f, 1f, destination.CenterX(), destination.CenterY());
            if (frame.MirroredVertically)
                canvas.Scale(1f, -1f, destination.CenterX(), destination.CenterY());
            canvas.DrawBitmap(asset.Bitmap, source, destination, _bitmapPaint);
            canvas.RestoreToCount(save);
        }

        private void DrawAtlasTopLeft(
            Canvas canvas,
            AtlasSpriteAsset asset,
            float left,
            float top,
            float scale)
        {
            if (asset?.Bitmap == null)
                return;
            var entry = asset.Entry;
            var source = new Rect(
                entry.X, entry.Y,
                entry.X + entry.Width, entry.Y + entry.Height);
            canvas.DrawBitmap(
                asset.Bitmap, source,
                new RectF(
                    left, top,
                    left + entry.Width * scale,
                    top + entry.Height * scale),
                _bitmapPaint);
        }

        private void DrawAtlasSpriteAt(
            Canvas canvas,
            AtlasSpriteAsset asset,
            float centerX,
            float bottomY,
            float scale)
        {
            if (asset?.Bitmap == null)
                return;
            var entry = asset.Entry;
            if (entry.X < 0 || entry.Y < 0 || entry.Width <= 0 || entry.Height <= 0 ||
                entry.X + entry.Width > asset.Bitmap.Width ||
                entry.Y + entry.Height > asset.Bitmap.Height)
                return;
            var source = new Rect(
                entry.X, entry.Y, entry.X + entry.Width, entry.Y + entry.Height);
            var width = entry.Width * scale;
            var height = entry.Height * scale;
            var destination = new RectF(
                centerX - width * 0.5f,
                bottomY - height,
                centerX + width * 0.5f,
                bottomY);
            canvas.DrawBitmap(asset.Bitmap, source, destination, _bitmapPaint);
        }

        private void DrawAtlasObjectAt(
            Canvas canvas,
            AtlasSpriteAsset asset,
            float entityX,
            float entityY,
            float scale,
            int sourceOffsetX = 0)
        {
            if (asset?.Bitmap == null)
                return;
            var entry = asset.Entry;
            var sourceX = entry.X + sourceOffsetX;
            if (sourceX < 0 || entry.Y < 0 || entry.Width <= 0 || entry.Height <= 0 ||
                sourceX + entry.Width > asset.Bitmap.Width ||
                entry.Y + entry.Height > asset.Bitmap.Height)
                return;
            var source = new Rect(
                sourceX, entry.Y, sourceX + entry.Width, entry.Y + entry.Height);
            var left = entityX - entry.OriginX * scale;
            var top = entityY - entry.OriginY * scale;
            var destination = new RectF(
                left, top,
                left + entry.Width * scale,
                top + entry.Height * scale);
            canvas.DrawBitmap(asset.Bitmap, source, destination, _bitmapPaint);
        }

        private void DrawAtlasStoneAt(
            Canvas canvas,
            AtlasSpriteAsset asset,
            float entityX,
            float entityY,
            float scale)
        {
            if (asset?.Bitmap == null)
                return;
            var entry = asset.Entry;
            if (entry.X < 0 || entry.Y < 0 || entry.Width <= 0 || entry.Height <= 0 ||
                entry.X + entry.Width > asset.Bitmap.Width ||
                entry.Y + entry.Height > asset.Bitmap.Height)
                return;
            var offset = GameObjectVisualLayout.GetStoneSpriteOffset(
                entry.Width, entry.Height);
            var left = entityX + offset.X * scale;
            var top = entityY + offset.Y * scale;
            canvas.DrawBitmap(
                asset.Bitmap,
                new Rect(entry.X, entry.Y,
                    entry.X + entry.Width, entry.Y + entry.Height),
                new RectF(left, top,
                    left + entry.Width * scale,
                    top + entry.Height * scale),
                _bitmapPaint);
        }

        private void DrawAtlasTopLeftAt(
            Canvas canvas, AtlasSpriteAsset asset,
            float left, float top, float scale,
            bool flipX = false, bool flipY = false, float alpha = 1f,
            int sourceOffsetX = 0)
        {
            if (asset?.Bitmap == null)
                return;
            var entry = asset.Entry;
            var sourceX = entry.X + sourceOffsetX;
            if (sourceX < 0 || entry.Y < 0 || entry.Width <= 0 || entry.Height <= 0 ||
                sourceX + entry.Width > asset.Bitmap.Width ||
                entry.Y + entry.Height > asset.Bitmap.Height)
                return;
            var source = new Rect(
                sourceX, entry.Y, sourceX + entry.Width, entry.Y + entry.Height);
            var destination = new RectF(left, top,
                left + entry.Width * scale, top + entry.Height * scale);
            var save = canvas.Save();
            if (flipX)
                canvas.Scale(-1f, 1f, destination.CenterX(), destination.CenterY());
            if (flipY)
                canvas.Scale(1f, -1f, destination.CenterX(), destination.CenterY());
            var oldAlpha = _bitmapPaint.Alpha;
            _bitmapPaint.Alpha = (int)(255f * Math.Clamp(alpha, 0f, 1f));
            canvas.DrawBitmap(asset.Bitmap, source, destination, _bitmapPaint);
            _bitmapPaint.Alpha = oldAlpha;
            canvas.RestoreToCount(save);
        }

        private void DrawAtlasAnimatedTileAt(
            Canvas canvas, AtlasSpriteAsset asset,
            float entityX, float entityY, float scale, int frame)
        {
            if (asset?.Bitmap == null)
                return;
            var entry = asset.Entry;
            var sourceX = entry.X + Math.Max(0, frame) * entry.Width;
            if (sourceX < 0 || entry.Y < 0 || entry.Width <= 0 || entry.Height <= 0 ||
                sourceX + entry.Width > asset.Bitmap.Width ||
                entry.Y + entry.Height > asset.Bitmap.Height)
                return;
            var source = new Rect(
                sourceX, entry.Y, sourceX + entry.Width, entry.Y + entry.Height);
            var destination = new RectF(
                entityX, entityY,
                entityX + entry.Width * scale,
                entityY + entry.Height * scale);
            canvas.DrawBitmap(asset.Bitmap, source, destination, _bitmapPaint);
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

        private EnemyAssetSet LoadEnemyAssetSet(
            Context context, LiveWallpaperMapEnemyKind kind)
        {
            var set = new EnemyAssetSet();
            var path = kind switch
            {
                LiveWallpaperMapEnemyKind.SeaUrchin => "Enemies/sea urchin.ani",
                LiveWallpaperMapEnemyKind.Octorok => "Enemies/octorok.ani",
                LiveWallpaperMapEnemyKind.Leever => "Enemies/leever.ani",
                LiveWallpaperMapEnemyKind.Crab => "Enemies/crab.ani",
                LiveWallpaperMapEnemyKind.Moblin => "Enemies/moblin.ani",
                LiveWallpaperMapEnemyKind.MoblinSword => "Enemies/moblin sword.ani",
                LiveWallpaperMapEnemyKind.RedZol => "Enemies/red zol.ani",
                LiveWallpaperMapEnemyKind.RiverZora => "Enemies/river zora.ani",
                LiveWallpaperMapEnemyKind.Ghini => "Enemies/ghini.ani",
                _ => "Enemies/pincer.ani"
            };
            for (var direction = 0; direction < 4; direction++)
            {
                string[] walk;
                string[] idle;
                string[] attack;
                string[] spawn;
                string[] leave;
                switch (kind)
                {
                    case LiveWallpaperMapEnemyKind.SeaUrchin:
                        walk = idle = attack = spawn = leave = ["IDLE"];
                        break;
                    case LiveWallpaperMapEnemyKind.Octorok:
                    case LiveWallpaperMapEnemyKind.Moblin:
                    case LiveWallpaperMapEnemyKind.MoblinSword:
                        walk = [$"walk_{direction}", $"stand_{direction}"];
                        idle = [$"stand_{direction}", $"walk_{direction}"];
                        attack = idle;
                        spawn = leave = idle;
                        break;
                    case LiveWallpaperMapEnemyKind.Leever:
                        walk = idle = ["MOVE", "SPAWN"];
                        attack = walk;
                        spawn = ["SPAWN", "MOVE"];
                        leave = ["LEAVE", "MOVE"];
                        break;
                    case LiveWallpaperMapEnemyKind.Crab:
                        walk = idle = attack = spawn = leave = ["walk"];
                        break;
                    case LiveWallpaperMapEnemyKind.RedZol:
                        walk = ["WALK", "IDLE"];
                        idle = ["IDLE", "WALK"];
                        attack = walk;
                        spawn = leave = idle;
                        break;
                    case LiveWallpaperMapEnemyKind.RiverZora:
                        walk = idle = ["IDLE", "SPAWN"];
                        attack = ["ATTACK", "IDLE"];
                        spawn = ["SPAWN", "IDLE"];
                        leave = idle;
                        break;
                    case LiveWallpaperMapEnemyKind.Ghini:
                        var fly = direction is 0 or 1 ? "fly_-1" : "fly_1";
                        walk = idle = attack = spawn = leave = [fly];
                        break;
                    default:
                        var pincerDirection = (direction * 2).ToString();
                        walk = [pincerDirection, "eyes"];
                        idle = ["eyes", pincerDirection];
                        attack = [pincerDirection, "eyes"];
                        spawn = attack;
                        leave = idle;
                        break;
                }
                set.Walk[direction] = LoadSprite(context, path, walk);
                set.Idle[direction] = LoadSprite(context, path, idle);
                set.Attack[direction] = LoadSprite(context, path, attack);
                set.Spawn[direction] = LoadSprite(context, path, spawn);
                set.Leave[direction] = LoadSprite(context, path, leave);
            }
            return set;
        }

        private AtlasSpriteAsset LoadAtlasSprite(
            Context context, string atlasName, string spriteId)
        {
            if (!LadxhdWallpaperAssets.TryResolveAtlasSprite(
                    context, atlasName, spriteId, out var entry, out var spritePath, out _))
                return null;
            try
            {
                if (!_spriteSheets.TryGetValue(spritePath, out var bitmap))
                {
                    bitmap = BitmapFactory.DecodeFile(spritePath) ??
                             throw new InvalidDataException(
                                 "The wallpaper atlas image could not be decoded.");
                    _spriteSheets.Add(spritePath, bitmap);
                }
                return new AtlasSpriteAsset(bitmap, entry);
            }
            catch
            {
                return null;
            }
        }

        private MapAsset LoadOverworldMap(Context context)
            => LoadMap(context, "overworld.map");

        private MapAsset LoadMap(Context context, string mapName)
        {
            if (!LadxhdWallpaperAssets.TryResolveMap(
                    context, mapName, out var map, out var tilesetPath, out _))
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

        public void Dispose()
        {
            _mapTileCacheCanvas?.Dispose();
            _mapTileCacheCanvas = null;
            _mapTileCache?.Dispose();
            _mapTileCache = null;
            foreach (var bitmap in _spriteSheets.Values)
                bitmap.Dispose();
            _spriteSheets.Clear();
            _bitmapPaint.Dispose();
            _overlayPaint.Dispose();
        }
    }
}
