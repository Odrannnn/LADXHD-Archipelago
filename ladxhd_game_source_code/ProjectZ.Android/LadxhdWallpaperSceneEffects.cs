using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Android.Content;
using Android.Graphics;
using ProjectZ;
using Path = System.IO.Path;
using GameRectangle = Microsoft.Xna.Framework.Rectangle;

namespace ProjectZ.Android
{
    internal sealed partial class LadxhdWallpaperScene
    {
        private readonly record struct ShadowDraw(Bitmap Bitmap, int X, int Y, int Width, int Height,
            float Left, float Top, float HeightScale, float Rotation, bool Flip, float Alpha,
            float DrawWidth, float DrawHeight);
        private readonly record struct EffectView(LiveWallpaperMap Map, int X, int Y,
            int Columns, int Rows, float Scale, int Bushes, int Stones,
            float SunHeight, float SunRotation);

        private const int EffectPadding = 32;
        private readonly Dictionary<string, AtlasSpriteAsset> _shadowAssets = new();
        private readonly Dictionary<string, Bitmap> _lightTextures = new();
        private readonly Dictionary<string, AtlasSpriteAsset> _lightSprites = new();
        private readonly List<ShadowDraw> _shadowCommands = new();
        private readonly List<ShadowDraw> _previousShadowCommands = new();
        private readonly List<ShadowDraw> _previousStaticShadowCommands = new();
        private readonly List<int> _visibleStaticShadowIndices = new();
        private bool[] _matchedStaticShadows = [];
        private readonly Paint _shadowPaint = new() { AntiAlias = false, FilterBitmap = true };
        private readonly Paint _effectPaint = new() { AntiAlias = false, FilterBitmap = true };
        private readonly Paint _sunShadowPaint = new() { AntiAlias = false, FilterBitmap = true };
        private readonly Paint _lampPaint = new() { AntiAlias = false, FilterBitmap = true };
        private readonly Dictionary<uint, LightingColorFilter> _lampFilters = new();
        private LightingColorFilter _sunShadowFilter;
        private readonly LiveWallpaperSunlightCache _sunlightCache = new();
        private LiveWallpaperSunlight _sunlight;
        private LiveWallpaperSunlight? _lightSunlight;
        private bool _sunlightEnabled;
        private bool _sunShadowAvailable;
        private bool _lightHasSunShadow;
        private bool _lightingApplied;
        private readonly GameRectangle[] _shadowDirtyRegions = new GameRectangle[4 + GameSceneEffects.MaxShadowDirtyRegions];
        private readonly GameRectangle[] _lightScrollRegions = new GameRectangle[4];
        private int _shadowDirtyRegionCount;
        private int _shadowScrollRegionCount;
        private SceneEffectScroll _shadowScroll;
        private Bitmap _scrollCopy;
        private readonly Canvas _scrollCopyCanvas = new();
        private readonly Canvas _scrollTargetCanvas = new();
        private readonly Paint _scrollPaint = new() { AntiAlias = false, FilterBitmap = false };
        private readonly PorterDuffXfermode _scrollSourceMode = new(PorterDuff.Mode.Src);
        private readonly Matrix _shadowMatrix = new();
        private readonly PorterDuffXfermode _lightMultiply = new(PorterDuff.Mode.Multiply);
        private readonly float[] _shadowMatrixValues = new float[9];
        private Bitmap _staticShadowMask;
        private Bitmap _shadowMask;
        private Bitmap _rawShadowMask;
        private Bitmap _lightMap;
        private Canvas _staticShadowCanvas;
        private Canvas _shadowCanvas;
        private Canvas _lightCanvas;
        private float[] _shadowScratch = [];
        private EffectView? _staticShadowView;
        private EffectView? _lightView;
        private LiveWallpaperMapViewport? _lightViewport;
        private bool _collectingShadows;
        private LiveWallpaperMapViewport _shadowViewport;
        private Bitmap _groundShadowSheet;

        private void ConfigureSunlight(int mode, double localMinutes, int sunrise, int sunset)
        {
            if (!_sunlightCache.Update(mode, localMinutes, sunrise, sunset, _activeMapName))
                return;
            var enabled = _sunlightCache.Enabled;
            var sunlight = _sunlightCache.Value;
            if (_sunlightEnabled == enabled && _sunlight == sunlight)
                return;
            _sunlightEnabled = enabled;
            _sunlight = sunlight;
            _wallpaperColorRevision++;
            var ambient = sunlight.AtOcclusion(1f);
            var filter = new LightingColorFilter(Color.Black, Color.Rgb(ambient.R, ambient.G, ambient.B));
            _sunShadowPaint.SetColorFilter(filter);
            _sunShadowFilter?.Dispose();
            _sunShadowFilter = filter;
        }

        private void PrepareSceneEffectAssets(Context context)
        {
            if (_overworldMap?.Map == null)
                return;
            foreach (var shadow in _overworldMap.Map.SceneEffects.Shadows)
                if (!_shadowAssets.ContainsKey(shadow.SpriteId))
                    _shadowAssets[shadow.SpriteId] = LoadAtlasSprite(context, "objects", shadow.SpriteId);
            _groundShadowSheet ??= LoadAtlasSprite(context, "items", "heart")?.Bitmap;
            if (!_lightTextures.ContainsKey("shadow"))
                _lightTextures["shadow"] = LoadInstalledLight(context, "shadow");
            if (!_lightTextures.ContainsKey("light"))
                _lightTextures["light"] = LoadInstalledLight(context, "light");
            foreach (var light in _overworldMap.Map.SceneEffects.Lights)
            {
                if (!_lampFilters.ContainsKey(light.Color.PackedValue))
                {
                    var color = light.Color;
                    int Straight(byte channel) => color.A == 0 ? 0 : Math.Min(255, channel * 255 / color.A);
                    _lampFilters[color.PackedValue] = new LightingColorFilter(
                        Color.Rgb(Straight(color.R), Straight(color.G), Straight(color.B)), Color.Black);
                }
                if (light.SpriteId == null || _lightSprites.ContainsKey(light.SpriteId))
                    continue;
                // Resources loads doorLight from Content/Light with Data/Light's
                // atlas; other sprite lights use the ordinary map object atlas.
                if (light.SpriteId == "doorLight" &&
                    AndroidAssetInstallation.TryGetActiveRoot(context, out var root, out _))
                {
                    var texture = LoadInstalledLight(context, "doorLight");
                    if (texture != null)
                    {
                        try
                        {
                            using var reader = File.OpenText(Path.Combine(root, "Data", "Light", "doorLight.atlas"));
                            if (LiveWallpaperAtlas.TryLoad(reader, "doorLight", out var entry))
                            {
                                _lightTextures["doorLight"] = texture;
                                _lightSprites[light.SpriteId] = new AtlasSpriteAsset(texture, entry);
                                continue;
                            }
                        }
                        catch (IOException) { }
                        texture.Dispose();
                    }
                }
                _lightSprites[light.SpriteId] = LoadAtlasSprite(context, "objects", light.SpriteId);
            }
            _staticShadowView = null;
            _lightView = null;
            _previousShadowCommands.Clear();
            _previousStaticShadowCommands.Clear();
        }

        private static Bitmap LoadInstalledLight(Context context, string name)
        {
            if (!AndroidAssetInstallation.TryGetActiveRoot(context, out var root, out _))
                return null;
            try
            {
                using var stream = File.OpenRead(Path.Combine(root, "Content", "Light", name + ".xnb"));
                if (!LiveWallpaperTexture.TryReadXnb(stream, out var width, out var height, out var pixels))
                    return null;
                return Bitmap.CreateBitmap(pixels, width, height, Bitmap.Config.Argb8888);
            }
            catch (IOException) { return null; }
        }

        private EffectView GetEffectView(LiveWallpaperMapViewport viewport,
            LiveWallpaperSimulatedLinkState? link = null) =>
            new(_overworldMap.Map, viewport.OriginX, viewport.OriginY,
                viewport.Columns, viewport.Rows,
                GameSceneEffects.ShadowRenderScale(viewport.TileSize / 16f),
                link?.CutBushes?.Count ?? 0, link?.LiftedStones?.Count ?? 0,
                _sunlightEnabled ? _sunlight.ShadowHeightMultiplier : 1f,
                _sunlightEnabled ? _sunlight.ShadowRotationOffset : 0f);

        private static bool TryScrollEffect(EffectView? previous, EffectView current,
            int width, int height, out SceneEffectScroll scroll)
        {
            scroll = default;
            return previous.HasValue &&
                (previous.Value with { X = current.X, Y = current.Y }) == current &&
                GameSceneEffects.TryGetEffectScroll(width, height,
                    (previous.Value.X - current.X) * 16f * current.Scale,
                    (previous.Value.Y - current.Y) * 16f * current.Scale, out scroll);
        }

        private void ScrollEffectBitmap(Bitmap bitmap, SceneEffectScroll scroll)
        {
            if (_scrollCopy == null || _scrollCopy.Width != bitmap.Width || _scrollCopy.Height != bitmap.Height)
            {
                _scrollCopyCanvas.SetBitmap(null);
                _scrollCopy?.Dispose();
                _scrollCopy = Bitmap.CreateBitmap(bitmap.Width, bitmap.Height, Bitmap.Config.Argb8888);
                _scrollCopyCanvas.SetBitmap(_scrollCopy);
            }
            // Never draw a bitmap onto itself with overlapping rectangles. SRC
            // copies transparent shadow pixels too, instead of accumulating alpha.
            _scrollPaint.SetXfermode(_scrollSourceMode);
            var source = scroll.Source;
            var destination = scroll.Destination;
            _drawSource.Set(source.Left, source.Top, source.Right, source.Bottom);
            _drawDestination.Set(source.Left, source.Top, source.Right, source.Bottom);
            _scrollCopyCanvas.DrawBitmap(bitmap, _drawSource, _drawDestination, _scrollPaint);
            _scrollTargetCanvas.SetBitmap(bitmap);
            _drawDestination.Set(destination.Left, destination.Top, destination.Right, destination.Bottom);
            _scrollTargetCanvas.DrawBitmap(_scrollCopy, _drawSource, _drawDestination, _scrollPaint);
            _scrollTargetCanvas.SetBitmap(null);
        }

        private static void TranslateShadowCommands(List<ShadowDraw> commands, float dx, float dy)
        {
            for (var i = 0; i < commands.Count; i++)
                commands[i] = commands[i] with { Left = commands[i].Left + dx, Top = commands[i].Top + dy };
        }

        private bool SameVisibleLights(LiveWallpaperMapViewport viewport)
        {
            if (!_lightViewport.HasValue)
                return false;
            foreach (var light in _overworldMap.Map.SceneEffects.Lights)
            {
                var margin = Math.Max(192, light.Size);
                if (IsNearViewport(_lightViewport.Value, light.X, light.Y, margin) !=
                    IsNearViewport(viewport, light.X, light.Y, margin))
                    return false;
            }
            return true;
        }

        private void DrawSceneShadows(Canvas canvas, LiveWallpaperMapViewport viewport,
            long elapsed, bool animated, LiveWallpaperSimulatedLinkState? link)
        {
            var scene = _overworldMap.Map.SceneEffects;
            _shadowDirtyRegionCount = 0;
            _shadowScrollRegionCount = 0;
            _sunShadowAvailable = false;
            if (!scene.UseShadows || _sunlightEnabled && !_sunlight.HasDirectLight)
                return;
            _sunShadowAvailable = _sunlightEnabled;
            var view = GetEffectView(viewport, link);
            var width = (int)MathF.Ceiling((viewport.Columns * 16 + EffectPadding * 2) * view.Scale);
            var height = (int)MathF.Ceiling((viewport.Rows * 16 + EffectPadding * 2) * view.Scale);
            if (_shadowMask == null || _shadowMask.Width != width || _shadowMask.Height != height)
            {
                _shadowCanvas?.Dispose();
                _staticShadowCanvas?.Dispose();
                _shadowMask?.Dispose();
                _rawShadowMask?.Dispose();
                _staticShadowMask?.Dispose();
                // BlurShadowMask reads and writes packed ARGB pixels through
                // Bitmap.Get/SetPixels, whose ALPHA_8 conversion is undefined.
                // Keep those two CPU-facing surfaces in their required format.
                _shadowMask = Bitmap.CreateBitmap(width, height, Bitmap.Config.Argb8888);
                _rawShadowMask = Bitmap.CreateBitmap(width, height, Bitmap.Config.Argb8888);
                // The static cache is only a coverage source drawn into the raw
                // mask. ALPHA_8 retains all 256 alpha levels at one byte/pixel.
                _staticShadowMask = Bitmap.CreateBitmap(width, height, Bitmap.Config.Alpha8);
                _shadowCanvas = new Canvas(_rawShadowMask);
                _staticShadowCanvas = new Canvas(_staticShadowMask);
                _shadowScratch = new float[width * height];
                _staticShadowView = null;
            }
            _shadowViewport = viewport;
            var rebuildStatic = _staticShadowView != view;
            var scrolled = TryScrollEffect(_staticShadowView, view, width, height, out _shadowScroll);
            if (scrolled)
            {
                ScrollEffectBitmap(_shadowMask, _shadowScroll);
                _shadowScrollRegionCount = _shadowScroll.WriteExposedRegions(_shadowDirtyRegions, width, height);
                _shadowDirtyRegionCount = _shadowScrollRegionCount;
                var dx = _shadowScroll.OffsetX / view.Scale;
                var dy = _shadowScroll.OffsetY / view.Scale;
                TranslateShadowCommands(_previousShadowCommands, dx, dy);
                TranslateShadowCommands(_previousStaticShadowCommands, dx, dy);
            }
            if (rebuildStatic)
            {
                _staticShadowCanvas.DrawColor(Color.Transparent, PorterDuff.Mode.Clear);
                _shadowCommands.Clear();
                CollectVisibleStaticShadowIndices(scene, viewport);
                foreach (var shadowIndex in _visibleStaticShadowIndices)
                {
                    var shadow = scene.Shadows[shadowIndex];
                    if (shadow.BushKey >= 0 && link?.CutBushes?.Contains(shadow.BushKey) == true ||
                        shadow.StoneKey >= 0 && link?.LiftedStones?.Contains(shadow.StoneKey) == true ||
                        !_shadowAssets.TryGetValue(shadow.SpriteId, out var asset) || asset == null)
                        continue;
                    var entry = asset.Entry;
                    var offset = shadow.StoneLayout
                        ? GameObjectVisualLayout.GetStoneSpriteOffset(entry.Width, entry.Height)
                        : new Microsoft.Xna.Framework.Vector2(-entry.OriginX, -entry.OriginY);
                    AddShadow(asset.Bitmap, entry.X, entry.Y, entry.Width, entry.Height,
                        shadow.EntityX + offset.X, shadow.EntityY + offset.Y + shadow.OffsetY,
                        scene.ShadowHeight, scene.ShadowRotation);
                }
                foreach (var command in _shadowCommands)
                    RasterizeShadow(_staticShadowCanvas, command, view.Scale);
                // Refresh the cheap raw static mask, but retain its expensive
                // blurred overlap. Changed viewport culling still invalidates
                // any newly added/removed static silhouette in the reused area.
                if (scrolled)
                    CollectChangedStaticShadowRegions(view.Scale, width, height);
                _previousStaticShadowCommands.Clear();
                _previousStaticShadowCommands.AddRange(_shadowCommands);
                _staticShadowView = view;
            }
            _shadowCommands.Clear();
            _collectingShadows = true;
            try
            {
                foreach (var actorIndex in _activeMapActorIndices)
                    DrawInstalledMapActor(
                        canvas, viewport, elapsed, animated, link, actorIndex);
                foreach (var enemyIndex in _activeMapEnemyIndices)
                    DrawInstalledMapEnemy(
                        canvas, viewport, elapsed, animated, enemyIndex);
                for (var i = 0; i < _overworldMap.Map.DungeonDoors.LooseKeys.Count; i++)
                {
                    var key = _overworldMap.Map.DungeonDoors.LooseKeys[i];
                    if (key.Visible && IsNearViewport(viewport, key.X, key.Y, 80f))
                        DrawLooseKey(canvas, viewport, i);
                }
                foreach (var drop in _overworldMap.Map.DungeonDoors.EnemyDrops)
                    if (drop.Visible && IsNearViewport(viewport, drop.X, drop.Y, 32f))
                        DrawPickup(canvas, viewport, drop);
                if (link.HasValue)
                {
                    DrawLink(canvas, viewport, elapsed, animated, link.Value);
                    DrawJourneyRooster(canvas, viewport, elapsed, animated, link.Value);
                }
            }
            finally { _collectingShadows = false; }
            if (rebuildStatic || !_shadowCommands.SequenceEqual(_previousShadowCommands))
            {
                if (rebuildStatic && !scrolled)
                {
                    _shadowDirtyRegions[0] = new GameRectangle(0, 0, width, height);
                    _shadowDirtyRegionCount = 1;
                }
                else
                    CollectChangedShadowRegions(_previousShadowCommands, view.Scale, width, height);
                for (var i = 0; i < _shadowDirtyRegionCount; i++)
                {
                    var dirty = _shadowDirtyRegions[i];
                    var sample = GameSceneEffects.ShadowSampleRegion(dirty, width, height);
                    var saved = _shadowCanvas.Save();
                    _shadowCanvas.ClipRect(sample.Left, sample.Top, sample.Right, sample.Bottom);
                    _shadowCanvas.DrawColor(Color.Transparent, PorterDuff.Mode.Clear);
                    _effectPaint.Alpha = 255;
                    _shadowCanvas.DrawBitmap(_staticShadowMask, 0, 0, _effectPaint);
                    foreach (var command in _shadowCommands)
                        if (GetShadowBounds(command, view.Scale).Intersects(sample))
                            RasterizeShadow(_shadowCanvas, command, view.Scale);
                    _shadowCanvas.RestoreToCount(saved);
                    // Android marshals the supplied array, not just the rectangle.
                    // Rent for this sample so a full refresh cannot leave subsequent
                    // tiny updates transferring a full-target buffer. GetPixels fills
                    // every sampled pixel; blur and bitmap writes ignore the pool tail.
                    var pixels = ArrayPool<int>.Shared.Rent(sample.Width * sample.Height);
                    try
                    {
                        _rawShadowMask.GetPixels(pixels, 0, sample.Width,
                            sample.X, sample.Y, sample.Width, sample.Height);
                        GameSceneEffects.BlurShadowMask(pixels, _shadowScratch, sample.Width, sample.Height);
                        _shadowMask.SetPixels(pixels,
                            (dirty.Y - sample.Y) * sample.Width + dirty.X - sample.X,
                            sample.Width, dirty.X, dirty.Y, dirty.Width, dirty.Height);
                    }
                    finally { ArrayPool<int>.Shared.Return(pixels); }
                }
                _previousShadowCommands.Clear();
                _previousShadowCommands.AddRange(_shadowCommands);
            }
            // Outdoors this mask attenuates the direct-light component in the
            // light map. Drawing a black overlay as well would double-darken it.
            if (!_sunlightEnabled)
                DrawEffectBitmap(canvas, _shadowMask, viewport, view.Scale);
        }

        private void CollectVisibleStaticShadowIndices(
            LiveWallpaperSceneEffects scene,
            LiveWallpaperMapViewport viewport)
        {
            _visibleStaticShadowIndices.Clear();
            var map = _overworldMap.Map;
            const int marginTiles = 6; // Exact 96-pixel IsNearViewport margin.
            var startX = Math.Clamp(
                viewport.OriginX - marginTiles, 0, map.Width - 1);
            var startY = Math.Clamp(
                viewport.OriginY - marginTiles, 0, map.Height - 1);
            var endX = Math.Clamp(
                viewport.OriginX + viewport.Columns + marginTiles,
                0, map.Width - 1);
            var endY = Math.Clamp(
                viewport.OriginY + viewport.Rows + marginTiles,
                0, map.Height - 1);
            for (var tileY = startY; tileY <= endY; tileY++)
            for (var tileX = startX; tileX <= endX; tileX++)
            foreach (var shadowIndex in scene.GetShadowIndicesAt(tileX, tileY))
            {
                var shadow = scene.Shadows[shadowIndex];
                if (IsNearViewport(
                        viewport, shadow.EntityX, shadow.EntityY, 96f))
                    _visibleStaticShadowIndices.Add(shadowIndex);
            }
            // Preserve the original installed-list order and alpha overlap.
            _visibleStaticShadowIndices.Sort();
        }

        private void CollectChangedShadowRegions(List<ShadowDraw> previous, float scale, int width, int height)
        {
            var count = _shadowDirtyRegionCount - _shadowScrollRegionCount;
            var regions = _shadowDirtyRegions.AsSpan(_shadowScrollRegionCount, GameSceneEffects.MaxShadowDirtyRegions);
            for (var i = 0; i < Math.Max(previous.Count, _shadowCommands.Count); i++)
            {
                if (i < previous.Count && i < _shadowCommands.Count && previous[i] == _shadowCommands[i])
                    continue;
                if (i < previous.Count)
                    GameSceneEffects.AddShadowDirtyRegion(regions, ref count,
                        GetShadowBounds(previous[i], scale), width, height);
                if (i < _shadowCommands.Count)
                    GameSceneEffects.AddShadowDirtyRegion(regions, ref count,
                        GetShadowBounds(_shadowCommands[i], scale), width, height);
            }
            _shadowDirtyRegionCount = _shadowScrollRegionCount + count;
        }

        private void CollectChangedStaticShadowRegions(float scale, int width, int height)
        {
            // Entering/leaving the culling margin changes list indices, not the
            // surviving trees. Match silhouettes (including duplicate counts)
            // rather than invalidating every object after an inserted entry.
            if (_matchedStaticShadows.Length < _shadowCommands.Count)
                _matchedStaticShadows = new bool[_shadowCommands.Count];
            Array.Clear(_matchedStaticShadows);
            var count = _shadowDirtyRegionCount - _shadowScrollRegionCount;
            var regions = _shadowDirtyRegions.AsSpan(_shadowScrollRegionCount, GameSceneEffects.MaxShadowDirtyRegions);
            foreach (var previous in _previousStaticShadowCommands)
            {
                var match = -1;
                for (var i = 0; i < _shadowCommands.Count; i++)
                    if (!_matchedStaticShadows[i] && previous == _shadowCommands[i])
                    {
                        match = i;
                        break;
                    }
                if (match >= 0)
                    _matchedStaticShadows[match] = true;
                else
                    GameSceneEffects.AddShadowDirtyRegion(regions, ref count,
                        GetShadowBounds(previous, scale), width, height);
            }
            for (var i = 0; i < _shadowCommands.Count; i++)
                if (!_matchedStaticShadows[i])
                    GameSceneEffects.AddShadowDirtyRegion(regions, ref count,
                        GetShadowBounds(_shadowCommands[i], scale), width, height);
            _shadowDirtyRegionCount = _shadowScrollRegionCount + count;
        }

        private static GameRectangle GetShadowBounds(ShadowDraw command, float scale)
        {
            var top = GameSceneEffects.ProjectShadow(0, 0, command.Height,
                command.HeightScale, command.Rotation);
            var x = command.Left + top.X;
            var y = command.Top + top.Y;
            var dx = -command.Rotation * command.DrawHeight;
            var dy = command.HeightScale * command.DrawHeight;
            // One extra raster pixel includes bilinear edge coverage.
            var left = (int)MathF.Floor((x + Math.Min(0, dx)) * scale) - 1;
            var right = (int)MathF.Ceiling((x + command.DrawWidth + Math.Max(0, dx)) * scale) + 1;
            var upper = (int)MathF.Floor((y + Math.Min(0, dy)) * scale) - 1;
            var bottom = (int)MathF.Ceiling((y + Math.Max(0, dy)) * scale) + 1;
            return new GameRectangle(left, upper, right - left, bottom - upper);
        }

        private void AddShadow(Bitmap bitmap, int x, int y, int width, int height,
            float left, float top, float heightScale, float rotation, bool flip = false, float alpha = 1f,
            float drawWidth = 0, float drawHeight = 0, bool directional = true)
        {
            if (bitmap == null || width <= 0 || height <= 0 || x < 0 || y < 0 ||
                x + width > bitmap.Width || y + height > bitmap.Height || alpha <= 0f)
                return;
            if (_sunlightEnabled && directional)
            {
                heightScale *= _sunlight.ShadowHeightMultiplier;
                rotation += _sunlight.ShadowRotationOffset;
            }
            _shadowCommands.Add(new(bitmap, x, y, width, height,
                left - _shadowViewport.OriginX * 16 + EffectPadding,
                top - _shadowViewport.OriginY * 16 + EffectPadding,
                heightScale, rotation, flip, Math.Clamp(alpha, 0f, 1f),
                drawWidth > 0 ? drawWidth : width, drawHeight > 0 ? drawHeight : height));
        }

        private void RasterizeShadow(Canvas canvas, ShadowDraw command, float scale)
        {
            var projected = GameSceneEffects.ProjectShadow(0, 0, command.Height,
                command.HeightScale, command.Rotation);
            var values = _shadowMatrixValues;
            values[0] = scale;
            values[1] = -command.Rotation * scale;
            values[2] = (command.Left + projected.X) * scale;
            values[3] = 0;
            values[4] = command.HeightScale * scale;
            values[5] = (command.Top + projected.Y) * scale;
            values[6] = values[7] = 0;
            values[8] = 1;
            _shadowMatrix.SetValues(values);
            var saved = canvas.Save();
            canvas.Concat(_shadowMatrix);
            if (command.Flip)
                canvas.Scale(-1, 1, command.DrawWidth / 2f, command.DrawHeight / 2f);
            _shadowPaint.Alpha = (int)(255f * command.Alpha);
            _drawSource.Set(command.X, command.Y, command.X + command.Width, command.Y + command.Height);
            _drawDestination.Set(0, 0, command.DrawWidth, command.DrawHeight);
            canvas.DrawBitmap(command.Bitmap, _drawSource, _drawDestination, _shadowPaint);
            canvas.RestoreToCount(saved);
        }

        private void AddAnimatedShadow(SpriteAsset asset, long elapsed, bool animated,
            float screenX, float screenY, float scale, float elevation = 0f,
            float? shadowHeight = null, float? shadowRotation = null, float animationSpeed = 1f)
        {
            var frame = asset.EngineAnimation.Advance(elapsed, animated, animationSpeed);
            var placement = asset.Animation.GetPlacement(frame, screenX, screenY, scale);
            var scene = _overworldMap.Map.SceneEffects;
            var left = (placement.Left - _shadowViewport.Left) / scale + _shadowViewport.OriginX * 16;
            var top = (placement.Top - _shadowViewport.Top) / scale + _shadowViewport.OriginY * 16;
            // Visible sprite already includes -Z. CSprite's shadow uses -Z/2-1.
            AddShadow(asset.Bitmap, frame.X, frame.Y, frame.Width, frame.Height,
                left, top + elevation + GameSceneEffects.SpriteShadowYOffset(elevation),
                shadowHeight ?? scene.ShadowHeight, shadowRotation ?? scene.ShadowRotation,
                frame.MirroredHorizontally, GameSceneEffects.SpriteShadowOpacity(elevation));
        }

        private void AddGroundShadow(float entityX, float entityY, float elevation,
            float bodyX, float bodyY, float bodyWidth, float bodyHeight,
            int width = 8, int height = 4)
        {
            if (elevation <= 0 || _groundShadowSheet == null)
                return;
            // BodyDrawShadowComponent's original SprItem rectangle and body anchor.
            // Width/height changes are applied as geometry, not a different sprite.
            var left = entityX + bodyX + bodyWidth / 2f - width / 2f;
            var top = entityY + bodyY + bodyHeight - height;
            AddShadow(_groundShadowSheet, 1, 218, 8, 4, left, top, 1f, 0,
                alpha: GameSceneEffects.GroundShadowOpacity(elevation), drawWidth: width, drawHeight: height,
                directional: false);
        }

        private void AddFloatingGroundShadow(float entityX, float entityY, float elevation) =>
            AddShadow(_groundShadowSheet, 1, 218, 8, 4,
                entityX - 4, entityY - 3.5f, 1f, 0,
                alpha: (elevation + 10f) / 20f, directional: false);

        private void DrawMapLighting(Canvas canvas, LiveWallpaperMapViewport viewport)
        {
            var scene = _overworldMap.Map.SceneEffects;
            _lightingApplied = false;
            // Without the actual installed falloff texture, keep the scene
            // readable; never fabricate light artwork or darken without lamps.
            if (!(scene.UseLighting || _sunlightEnabled) ||
                !_lightTextures.TryGetValue("light", out var glow) || glow == null)
                return;
            var view = GetEffectView(viewport);
            var width = (int)MathF.Ceiling((viewport.Columns * 16 + EffectPadding * 2) * view.Scale);
            var height = (int)MathF.Ceiling((viewport.Rows * 16 + EffectPadding * 2) * view.Scale);
            if (_lightMap == null || _lightMap.Width != width || _lightMap.Height != height)
            {
                _lightCanvas?.Dispose();
                _lightMap?.Dispose();
                _lightMap = Bitmap.CreateBitmap(width, height, Bitmap.Config.Argb8888);
                _lightCanvas = new Canvas(_lightMap);
                _lightView = null;
            }
            var rebuild = _lightView != view || _lightSunlight != _sunlight ||
                _lightHasSunShadow != _sunShadowAvailable;
            var scrollRegions = 0;
            if (_lightSunlight == _sunlight && _lightHasSunShadow == _sunShadowAvailable &&
                TryScrollEffect(_lightView, view, width, height, out var scroll) && SameVisibleLights(viewport))
            {
                ScrollEffectBitmap(_lightMap, scroll);
                // Solar-shadow strips already cover these exact cache borders.
                if (!_sunShadowAvailable || _shadowScrollRegionCount == 0 || scroll != _shadowScroll)
                    scrollRegions = scroll.WriteExposedRegions(_lightScrollRegions, width, height);
                rebuild = false;
            }
            var regionCount = rebuild ? 1 : scrollRegions + (_sunlightEnabled ? _shadowDirtyRegionCount : 0);
            for (var region = 0; region < regionCount; region++)
            {
                var dirty = rebuild ? new GameRectangle(0, 0, width, height) :
                    region < scrollRegions ? _lightScrollRegions[region] : _shadowDirtyRegions[region - scrollRegions];
                var lightCanvas = _lightCanvas;
                var clipped = lightCanvas.Save();
                lightCanvas.ClipRect(dirty.Left, dirty.Top, dirty.Right, dirty.Bottom);
                var baseLight = _sunlightEnabled ? _sunlight.AtOcclusion(0f) : scene.Ambient;
                lightCanvas.DrawColor(Color.Rgb(baseLight.R, baseLight.G, baseLight.B));
                if (_sunShadowAvailable && _shadowMask != null)
                {
                    // Equivalent to ambient + direct * (1 - shadowCoverage).
                    // The shadow texture supplies per-pixel coverage; ambient
                    // and local lamp light are not darkened by the sun mask.
                    lightCanvas.DrawBitmap(_shadowMask, 0, 0, _sunShadowPaint);
                }
                foreach (var light in scene.Lights)
                {
                    if (!IsNearViewport(viewport, light.X, light.Y, Math.Max(192, light.Size)))
                        continue;
                    var color = light.Color;
                    var paint = _lampPaint;
                    paint.Alpha = color.A;
                    paint.SetColorFilter(_lampFilters[color.PackedValue]);
                    var x = (light.X - viewport.OriginX * 16 + EffectPadding) * view.Scale;
                    var y = (light.Y - viewport.OriginY * 16 + EffectPadding) * view.Scale;
                    if (light.SpriteId == null)
                    {
                        var half = light.Size / 2 * view.Scale;
                        _drawDestination.Set(x - half, y - half, x - half + light.Size * view.Scale,
                            y - half + light.Size * view.Scale);
                        lightCanvas.DrawBitmap(glow, null, _drawDestination, paint);
                    }
                    else if (_lightSprites.TryGetValue(light.SpriteId, out var asset) && asset != null)
                    {
                        var e = asset.Entry;
                        // DictAtlasEntry keeps source pixels at texture scale,
                        // then ObjLightSprite draws at its reciprocal world scale.
                        var spriteScale = view.Scale / e.TextureScale;
                        var saved = lightCanvas.Save();
                        lightCanvas.Rotate(light.Rotation * 90f,
                            x + e.OriginX * spriteScale, y + e.OriginY * spriteScale);
                        _drawSource.Set(e.X, e.Y, e.X + e.Width, e.Y + e.Height);
                        _drawDestination.Set(x, y, x + e.Width * spriteScale, y + e.Height * spriteScale);
                        lightCanvas.DrawBitmap(asset.Bitmap, _drawSource, _drawDestination, paint);
                        lightCanvas.RestoreToCount(saved);
                    }
                }
                lightCanvas.RestoreToCount(clipped);
                _lightView = view;
                _lightSunlight = _sunlight;
                _lightHasSunShadow = _sunShadowAvailable;
            }
            _effectPaint.SetXfermode(_lightMultiply);
            DrawEffectBitmap(canvas, _lightMap, viewport, view.Scale);
            _effectPaint.SetXfermode(null);
            _lightViewport = viewport;
            _lightingApplied = true;
        }

        private int ApplySceneLightToSample(int pixel, float tileX, float tileY)
        {
            if (!_lightingApplied || _lightMap == null || !_lightView.HasValue)
                return pixel;
            var view = _lightView.Value;
            var x = Math.Clamp((int)(((tileX - view.X) * 16 + EffectPadding) * view.Scale), 0, _lightMap.Width - 1);
            var y = Math.Clamp((int)(((tileY - view.Y) * 16 + EffectPadding) * view.Scale), 0, _lightMap.Height - 1);
            var light = _lightMap.GetPixel(x, y);
            var red = ((pixel >> 16) & 255) * ((light >> 16) & 255) / 255;
            var green = ((pixel >> 8) & 255) * ((light >> 8) & 255) / 255;
            var blue = (pixel & 255) * (light & 255) / 255;
            return unchecked((int)0xFF000000) | red << 16 | green << 8 | blue;
        }

        private void DrawEffectBitmap(Canvas canvas, Bitmap bitmap,
            LiveWallpaperMapViewport viewport, float renderScale)
        {
            var scale = viewport.TileSize / 16f;
            var left = viewport.Left - EffectPadding * scale;
            var top = viewport.Top - EffectPadding * scale;
            _effectPaint.Alpha = 255;
            _drawDestination.Set(left, top,
                left + bitmap.Width / renderScale * scale,
                top + bitmap.Height / renderScale * scale);
            canvas.DrawBitmap(bitmap, null, _drawDestination, _effectPaint);
        }

        private void ReleaseSceneEffectRenderTargets()
        {
            _scrollCopyCanvas.SetBitmap(null);
            _scrollTargetCanvas.SetBitmap(null);
            _scrollCopy?.Dispose();
            _scrollCopy = null;
            _shadowCanvas?.Dispose();
            _shadowCanvas = null;
            _staticShadowCanvas?.Dispose();
            _staticShadowCanvas = null;
            _shadowMask?.Dispose();
            _shadowMask = null;
            _rawShadowMask?.Dispose();
            _rawShadowMask = null;
            _staticShadowMask?.Dispose();
            _staticShadowMask = null;
            _lightCanvas?.Dispose();
            _lightCanvas = null;
            _lightMap?.Dispose();
            _lightMap = null;
            _staticShadowView = null;
            _lightView = null;
            _lightViewport = null;
            _shadowScratch = [];
            _previousShadowCommands.Clear();
            _previousStaticShadowCommands.Clear();
            _lightingApplied = false;
        }

        private void DisposeSceneEffects()
        {
            ReleaseSceneEffectRenderTargets();
            _scrollCopyCanvas.Dispose();
            _scrollTargetCanvas.Dispose();
            _scrollPaint.Dispose();
            _scrollSourceMode.Dispose();
            foreach (var texture in _lightTextures.Values.Distinct())
                texture?.Dispose();
            _shadowPaint.Dispose();
            _effectPaint.Dispose();
            _shadowMatrix.Dispose();
            _lightMultiply.Dispose();
            _sunShadowPaint.Dispose();
            _sunShadowFilter?.Dispose();
            _lampPaint.Dispose();
            foreach (var filter in _lampFilters.Values)
                filter.Dispose();
        }
    }
}
