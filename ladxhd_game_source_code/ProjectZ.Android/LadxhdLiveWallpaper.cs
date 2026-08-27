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
                ?.Edit()?.PutInt(FrameRateKey, value <= 15 ? 15 : 30)?.Apply();

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

            var sceneLabel = new TextView(this)
            {
                Text = "Wallpaper location",
                TextSize = 17f,
                Enabled = assetReady
            };
            layout.AddView(sceneLabel);
            var scene = new Spinner(this) { Enabled = assetReady };
            int[] sceneValues =
                [1, 2, 3, 5, 6, 7, LiveWallpaperSceneSelection.RotationSelection];
            var sceneAdapter = new ArrayAdapter<string>(this,
                global::Android.Resource.Layout.SimpleSpinnerItem,
                ["Mabe Village", "Toronbo Shores", "Mysterious Forest", "Kanalet Castle",
                 "Animal Village", "Wind Fish's Egg", "Rotate locations"]);
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

            var tapLabel = new TextView(this)
            {
                Text = "Wallpaper tap action",
                TextSize = 17f
            };
            layout.AddView(tapLabel);
            var tapAction = new Spinner(this);
            var tapAdapter = new ArrayAdapter<string>(this,
                global::Android.Resource.Layout.SimpleSpinnerItem,
                ["No action", "Cycle featured character", "Switch location"]);
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
                            LadxhdWallpaperPreferences.GetLinkActivity(_service),
                            LadxhdWallpaperPreferences.GetWildlifeSchedule(_service),
                            LadxhdWallpaperPreferences.GetCharacterPosition(_service));
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
        private SpriteAsset _marin;
        private SpriteAsset _bowWowLeft;
        private SpriteAsset _bowWowRight;
        private SpriteAsset _roosterLeft;
        private SpriteAsset _roosterRight;
        private SpriteAsset _butterfly;
        private SpriteAsset _owl;
        private AtlasSpriteAsset _marinNote;
        private AtlasSpriteAsset _bowWowChain;
        private AtlasSpriteAsset _roosterParticleLarge;
        private AtlasSpriteAsset _roosterParticleMedium;
        private AtlasSpriteAsset _roosterParticleSmall;
        private MapAsset _overworldMap;

        public LadxhdWallpaperScene(Context context)
        {
            for (var direction = 0; direction < 4; direction++)
            {
                _linkWalking[direction] = LoadSprite(
                    context, "link0.ani", [$"walk_{direction}"]);
                _linkStanding[direction] = LoadSprite(
                    context, "link0.ani", [$"stand_{direction}"]);
                _linkJumping[direction] = LoadSprite(
                    context, "link0.ani", [$"jump_{direction}"]);
            }
            _marin = LoadSprite(context, "NPCs/marin.ani", ["sing"]);
            _bowWowLeft = LoadSprite(context, "NPCs/BowWow.ani", ["walk_2"]);
            _bowWowRight = LoadSprite(context, "NPCs/BowWow.ani", ["walk_3"]);
            _roosterLeft = LoadSprite(context, "NPCs/cock.ani", ["stand_2"]);
            _roosterRight = LoadSprite(context, "NPCs/cock.ani", ["stand_3"]);
            _butterfly = LoadSprite(context, "NPCs/butterfly.ani", ["idle"]);
            _owl = LoadSprite(context, "NPCs/owl.ani", ["fly", "hover", "idle"]);
            _marinNote = LoadAtlasSprite(context, "npcs", "note");
            _bowWowChain = LoadAtlasSprite(context, "npcs", "bowwow chain");
            _roosterParticleLarge = LoadAtlasSprite(context, "npcs", "cock_particle_0");
            _roosterParticleMedium = LoadAtlasSprite(context, "npcs", "cock_particle_1");
            _roosterParticleSmall = LoadAtlasSprite(context, "npcs", "cock_particle_2");
            _overworldMap = LoadOverworldMap(context);
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
            int characterPosition)
        {
            var width = canvas.Width;
            var height = canvas.Height;
            if (width <= 0 || height <= 0)
                return;
            var time = animated ? elapsed : 0L;
            var unit = Math.Max(1f, Math.Min(width, height) / 240f);
            var phase = LiveWallpaperLighting.Resolve(timeOfDay, DateTime.Now.Hour);
            var wildlife = LiveWallpaperWildlife.Resolve(wildlifeSchedule, phase);
            canvas.DrawColor(Color.Black);

            // Defensively restore fully opaque bitmap rendering at the start of every frame.
            _bitmapPaint.Color = Color.White;
            _bitmapPaint.Alpha = 255;

            var resolvedScene = LiveWallpaperSceneSelection.Resolve(
                scene, elapsed, _overworldMap != null);
            if (resolvedScene <= 0)
                return;
            var groundY = DrawInstalledMap(
                canvas, width, height, resolvedScene, xOffset, out var viewport);
            if (showIslandLife && wildlife.ShowOwl)
                DrawOwl(canvas, width, height, time, unit, animated);
            if (showIslandLife)
            {
                DrawFeaturedCharacter(canvas, width, groundY, time, xOffset, unit,
                    animated, featuredCharacter, resolvedScene, characterPosition);
                if (wildlife.ShowButterflies)
                    DrawButterflies(canvas, width, groundY, time, unit, animated);
            }
            DrawLink(canvas, viewport, resolvedScene, elapsed, unit, animated, linkActivity);
            DrawLightingOverlay(canvas, width, height, phase);
            DrawSceneTransition(canvas, width, height, scene, elapsed);
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
            out LiveWallpaperMapViewport viewport)
        {
            const int tileSize = 16;
            const int atlasStride = tileSize + 2;
            var map = _overworldMap.Map;
            var tileset = _overworldMap.Bitmap;
            if (!LiveWallpaperMapViewport.TryCreate(
                    width, height, map.Height, scene, xOffset, out viewport))
                return height * 0.72f;
            var tilesPerRow = tileset.Width / atlasStride;
            if (tilesPerRow <= 0)
                return viewport.GroundY;

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
                        var source = new Rect(sourceX, sourceY,
                            sourceX + tileSize, sourceY + tileSize);
                        var destination = new RectF(
                            viewport.Left + x * viewport.TileSize,
                            viewport.Top + y * viewport.TileSize,
                            viewport.Left + (x + 1) * viewport.TileSize,
                            viewport.Top + (y + 1) * viewport.TileSize);
                        canvas.DrawBitmap(tileset, source, destination, _bitmapPaint);
                    }
                }
            }

            return viewport.GroundY;
        }

        private void DrawLink(
            Canvas canvas,
            LiveWallpaperMapViewport viewport,
            int scene,
            long elapsed,
            float unit,
            bool animated,
            int activity)
        {
            var state = LiveWallpaperLinkActivity.Resolve(activity, elapsed, animated);
            if (!state.Visible)
                return;
            var route = LiveWallpaperLinkRoute.Resolve(scene, state.Journey, state.Walking);
            var direction = route.Direction;
            var asset = route.Action == LiveWallpaperLinkRouteAction.FeatherJump
                ? _linkJumping[direction]
                : route.Action == LiveWallpaperLinkRouteAction.Walk
                    ? _linkWalking[direction]
                    : _linkStanding[direction];
            asset ??= _linkStanding[direction] ?? _linkWalking[direction];
            if (asset == null)
                return;
            var scale = Math.Max(2f, unit * 2.2f);
            var centerX = viewport.Left +
                          (route.MapX - viewport.OriginX) * viewport.TileSize;
            var bottomY = viewport.Top +
                          (route.MapY - viewport.OriginY) * viewport.TileSize -
                          route.JumpHeight * viewport.TileSize * 1.15f;
            DrawSpriteAt(canvas, asset, elapsed, centerX, bottomY, scale, animated);
        }

        private void DrawFeaturedCharacter(
            Canvas canvas,
            int width,
            float groundY,
            long elapsed,
            float xOffset,
            float unit,
            bool animated,
            int featuredCharacter,
            int scene,
            int characterPosition)
        {
            var selection = LiveWallpaperCharacterSelection.Resolve(
                featuredCharacter, scene, elapsed);
            var motion = LiveWallpaperCharacterMotion.Resolve(selection, elapsed, animated);
            var baseX = width * LiveWallpaperSceneLayouts.ResolveFeaturedXRatio(
                characterPosition, scene) - (xOffset - 0.5f) * 20f * unit;
            var movementRadius = selection switch
            {
                1 => 18f,
                2 => 14f,
                _ => 0f
            };
            var centerX = baseX + motion.HorizontalOffset * movementRadius * unit;
            var lift = selection == 2 ? 13f : selection == 1 ? 5f : 0f;
            var bottomY = groundY - motion.Lift * lift * unit;
            var asset = selection switch
            {
                1 => motion.FacingRight ? _bowWowRight : _bowWowLeft,
                2 => motion.FacingRight ? _roosterRight : _roosterLeft,
                _ => _marin
            };
            if (asset == null)
                return;
            if (selection == 1)
                DrawBowWowChain(canvas, baseX, groundY, centerX, bottomY, unit);
            else if (selection == 2 && motion.Lift > 0.35f)
                DrawRoosterParticles(canvas, centerX, bottomY, elapsed, unit);
            var scale = selection == 0 ? 2.05f : 1.9f;
            DrawSpriteAt(canvas, asset, elapsed, centerX, bottomY,
                Math.Max(2f, unit * scale), animated);
            if (selection == 0 && motion.ShowNotes)
                DrawMarinNotes(canvas, centerX, groundY, elapsed, unit);
        }

        private void DrawMarinNotes(
            Canvas canvas, float centerX, float groundY, long elapsed, float unit)
        {
            if (_marinNote == null)
                return;
            for (var index = 0; index < 3; index++)
            {
                var phase = ((elapsed + index * 900L) % 2_700L) / 2_700f;
                var noteX = centerX + (8f + index * 4f) * unit +
                            MathF.Sin(phase * MathF.PI * 2f) * 4f * unit;
                var noteY = groundY - (20f + phase * 22f) * unit;
                DrawAtlasSpriteAt(canvas, _marinNote, noteX, noteY,
                    Math.Max(1.5f, unit * 1.25f));
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
            bool engineDriven = false)
        {
            if (asset?.Bitmap == null || asset.Animation == null)
                return;
            var frame = engineDriven
                ? asset.EngineAnimation.Advance(elapsed, animated: true)
                : asset.Animation.GetFrame(elapsed);
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

        public void Dispose()
        {
            foreach (var bitmap in _spriteSheets.Values)
                bitmap.Dispose();
            _spriteSheets.Clear();
            _bitmapPaint.Dispose();
            _overlayPaint.Dispose();
        }
    }
}
