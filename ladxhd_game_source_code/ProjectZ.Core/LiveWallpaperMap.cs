using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace ProjectZ
{
    public enum LiveWallpaperMapActorKind
    {
        Person,
        LegacyPerson,
        Dog,
        Grandmother,
        Raccoon,
        WeatherBird,
        Owl,
        Butterfly,
        Bird,
        BowWow,
        Frog,
        Mouse,
        BowWowSmall,
        Alligator,
        ChickenDude,
        Hippo,
        Painter,
        Tracy,
        LetterBoy,
        LetterGirl,
        LetterBird,
        PhotoMouse,
        Fisherman,
        Mermaid,
        Fairy
    }

    public enum LiveWallpaperMapEnemyKind
    {
        SeaUrchin,
        Octorok,
        Leever,
        Crab,
        Moblin,
        MoblinSword,
        RedZol,
        RiverZora,
        Ghini,
        Pincer
    }

    public enum LiveWallpaperMapTerrain
    {
        Ground,
        Water,
        DeepWater,
        Void
    }

    public readonly struct LiveWallpaperCollisionBounds
    {
        public LiveWallpaperCollisionBounds(
            float x, float y, float width, float height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public float X { get; }
        public float Y { get; }
        public float Width { get; }
        public float Height { get; }
        public float Right => X + Width;
        public float Bottom => Y + Height;
    }

    public readonly struct LiveWallpaperHoleContact
    {
        public LiveWallpaperHoleContact(
            float coverage, float directionX, float directionY)
        {
            Coverage = Math.Clamp(coverage, 0f, 1f);
            DirectionX = directionX;
            DirectionY = directionY;
        }

        public float Coverage { get; }
        public float DirectionX { get; }
        public float DirectionY { get; }
    }

    public readonly struct LiveWallpaperMapAnimatedTile
    {
        public LiveWallpaperMapAnimatedTile(
            string spriteId, int entityX, int entityY,
            int frameCount, int frameDurationMilliseconds)
        {
            SpriteId = spriteId;
            EntityX = entityX;
            EntityY = entityY;
            FrameCount = Math.Max(1, frameCount);
            FrameDurationMilliseconds = Math.Max(1, frameDurationMilliseconds);
        }

        public string SpriteId { get; }
        public int EntityX { get; }
        public int EntityY { get; }
        public int FrameCount { get; }
        public int FrameDurationMilliseconds { get; }

        // The overworld's three breaking-wave sprites are transparent overlays.
        // Their empty map cells use tileset0's canonical solid ocean tile below
        // them; otherwise Android's transparent cache exposes the black canvas.
        public bool RequiresOverworldOceanBase =>
            SpriteId is "wave_3" or "wave_4" or "wave_5";
    }

    public readonly struct LiveWallpaperMapLamp
    {
        public LiveWallpaperMapLamp(
            string animationPath, int pixelX, int pixelY,
            int entityX, int entityY, int rotation, bool playerLayer,
            string animationName = "idle")
        {
            AnimationPath = animationPath;
            AnimationName = animationName;
            AnimationKey = animationPath + "\n" + animationName;
            PixelX = pixelX;
            PixelY = pixelY;
            EntityX = entityX;
            EntityY = entityY;
            Rotation = Math.Clamp(rotation, 0, 3);
            PlayerLayer = playerLayer;
        }

        public string AnimationPath { get; }
        public string AnimationName { get; }
        public string AnimationKey { get; }
        public int PixelX { get; }
        public int PixelY { get; }
        public int EntityX { get; }
        public int EntityY { get; }
        public int Rotation { get; }
        public bool PlayerLayer { get; }
    }

    public readonly struct LiveWallpaperMapLight
    {
        public LiveWallpaperMapLight(
            int centerX, int centerY, int size,
            int red, int green, int blue, int alpha)
        {
            CenterX = centerX;
            CenterY = centerY;
            Size = Math.Max(1, size);
            Red = Math.Clamp(red, 0, 255);
            Green = Math.Clamp(green, 0, 255);
            Blue = Math.Clamp(blue, 0, 255);
            Alpha = Math.Clamp(alpha, 0, 255);
        }

        public int CenterX { get; }
        public int CenterY { get; }
        public int Size { get; }
        public int Red { get; }
        public int Green { get; }
        public int Blue { get; }
        public int Alpha { get; }
    }

    public readonly struct LiveWallpaperMapEnemy
    {
        public LiveWallpaperMapEnemy(
            LiveWallpaperMapEnemyKind kind, int pixelX, int pixelY,
            int entityX, int entityY,
            int bodyX, int bodyY, int bodyWidth, int bodyHeight)
        {
            Kind = kind;
            PixelX = pixelX;
            PixelY = pixelY;
            EntityX = entityX;
            EntityY = entityY;
            BodyX = bodyX;
            BodyY = bodyY;
            BodyWidth = bodyWidth;
            BodyHeight = bodyHeight;
        }

        public LiveWallpaperMapEnemyKind Kind { get; }
        public int PixelX { get; }
        public int PixelY { get; }
        public int EntityX { get; }
        public int EntityY { get; }
        public int BodyX { get; }
        public int BodyY { get; }
        public int BodyWidth { get; }
        public int BodyHeight { get; }
    }

    public readonly struct LiveWallpaperMapDecoration
    {
        public LiveWallpaperMapDecoration(
            string spriteId, int entityX, int entityY,
            bool playerLayer = true, bool topLeft = false,
            string atlasName = "objects", bool stoneLayout = false,
            int drawOffsetX = 0, int drawOffsetY = 0,
            int sourceOffsetX = 0)
        {
            SpriteId = spriteId;
            EntityX = entityX;
            EntityY = entityY;
            PlayerLayer = playerLayer;
            TopLeft = topLeft;
            AtlasName = atlasName;
            AssetKey = atlasName + "\n" + spriteId;
            StoneLayout = stoneLayout;
            DrawOffsetX = drawOffsetX;
            DrawOffsetY = drawOffsetY;
            SourceOffsetX = sourceOffsetX;
        }

        public string SpriteId { get; }
        public int EntityX { get; }
        public int EntityY { get; }
        public bool PlayerLayer { get; }
        public bool TopLeft { get; }
        public string AtlasName { get; }
        public string AssetKey { get; }
        public bool StoneLayout { get; }
        public int DrawOffsetX { get; }
        public int DrawOffsetY { get; }
        public int SourceOffsetX { get; }

        // Same renderer anchor and conservative margin, shared with regression
        // tests. Resolve moved-block positions before calling; atlas origins and
        // stone offsets are still applied by the original drawing helpers.
        public bool TryGetDrawAnchor(LiveWallpaperMapViewport viewport,
            float entityX, float entityY, out float anchorX, out float anchorY)
        {
            var scale = viewport.TileSize / 16f;
            anchorX = viewport.Left +
                ((entityX + DrawOffsetX) / 16f - viewport.OriginX) * viewport.TileSize;
            anchorY = viewport.Top +
                ((entityY + DrawOffsetY) / 16f - viewport.OriginY) * viewport.TileSize;
            return !(anchorX < viewport.Left - 64f * scale ||
                anchorX > viewport.Left + viewport.Columns * viewport.TileSize + 64f * scale ||
                anchorY < viewport.Top - 64f * scale ||
                anchorY > viewport.Top + viewport.Rows * viewport.TileSize + 64f * scale);
        }
    }

    public readonly struct LiveWallpaperMapObject
    {
        public LiveWallpaperMapObject(
            string template, int pixelX, int pixelY, string[] arguments)
        {
            Template = template;
            PixelX = pixelX;
            PixelY = pixelY;
            Arguments = arguments ?? [];
        }

        public string Template { get; }
        public int PixelX { get; }
        public int PixelY { get; }
        public IReadOnlyList<string> Arguments { get; }
    }

    public readonly struct LiveWallpaperMapActor
    {
        public LiveWallpaperMapActor(
            LiveWallpaperMapActorKind kind, int pixelX, int pixelY,
            string animationId = null, string animationName = null,
            int bodyX = 0, int bodyY = 0, int bodyWidth = 0, int bodyHeight = 0,
            int spriteOffsetX = 0, int spriteOffsetY = 0,
            int triggerX = 0, int triggerY = 0,
            int triggerWidth = 0, int triggerHeight = 0,
            int owlMode = 0, bool owlHoverMode = false)
        {
            Kind = kind;
            PixelX = pixelX;
            PixelY = pixelY;
            AnimationId = animationId;
            AnimationName = animationName;
            BodyX = bodyX;
            BodyY = bodyY;
            BodyWidth = bodyWidth;
            BodyHeight = bodyHeight;
            SpriteOffsetX = spriteOffsetX;
            SpriteOffsetY = spriteOffsetY;
            TriggerX = triggerX;
            TriggerY = triggerY;
            TriggerWidth = triggerWidth;
            TriggerHeight = triggerHeight;
            OwlMode = owlMode;
            OwlHoverMode = owlHoverMode;
        }

        public LiveWallpaperMapActorKind Kind { get; }
        public int PixelX { get; }
        public int PixelY { get; }
        public string AnimationId { get; }
        public string AnimationName { get; }
        public int BodyX { get; }
        public int BodyY { get; }
        public int BodyWidth { get; }
        public int BodyHeight { get; }
        public int SpriteOffsetX { get; }
        public int SpriteOffsetY { get; }
        public int TriggerX { get; }
        public int TriggerY { get; }
        public int TriggerWidth { get; }
        public int TriggerHeight { get; }
        public int OwlMode { get; }
        public bool OwlHoverMode { get; }
    }

    public readonly struct LiveWallpaperMapPortal
    {
        public LiveWallpaperMapPortal(
            int pixelX, int pixelY, int width, int height,
            int direction, int mode,
            string entryId = null, string nextMap = null,
            string exitId = null, bool is2dDoor = false,
            bool isHoleTeleporter = false)
        {
            PixelX = pixelX;
            PixelY = pixelY;
            Width = Math.Max(1, width);
            Height = Math.Max(1, height);
            Direction = Math.Clamp(direction, 0, 3);
            Mode = Math.Max(0, mode);
            EntryId = entryId;
            NextMap = nextMap;
            ExitId = exitId;
            Is2DDoor = is2dDoor;
            IsHoleTeleporter = isHoleTeleporter;
        }

        public int PixelX { get; }
        public int PixelY { get; }
        public int Width { get; }
        public int Height { get; }
        public int Direction { get; }
        public int Mode { get; }
        public string EntryId { get; }
        public string NextMap { get; }
        public string ExitId { get; }
        public bool Is2DDoor { get; }
        public bool IsHoleTeleporter { get; }
        public bool HasDestination =>
            !string.IsNullOrWhiteSpace(NextMap) &&
            !string.IsNullOrWhiteSpace(ExitId);

        public bool ShouldActivateAt(
            float linkPixelX, float linkPixelY,
            float inputMoveY, int linkDirection,
            bool is2dMap = false, bool grounded = true)
        {
            if (IsHoleTeleporter)
                return false;
            if (is2dMap)
                return TouchesSideViewTrigger(linkPixelX, linkPixelY) &&
                    (!Is2DDoor || grounded && inputMoveY < 0);
            if (Mode == 1)
            {
                // ObjDoor activates grounded Link on collision with the inset
                // stair trigger, not on proximity to a grid-snapped route node.
                if (!grounded && !is2dMap)
                    return false;
                var trigger = DoorGameplayGeometry.GetTrigger(
                    PixelX, PixelY, Width, Height, Mode, is2dMap);
                return linkPixelX - 4f < trigger.Right &&
                       linkPixelX + 4f > trigger.Left &&
                       linkPixelY - 10f < trigger.Bottom &&
                       linkPixelY > trigger.Top;
            }
            // ObjDoor2d waits for upward input after Link has entered its narrow
            // collider. The wallpaper can reach the exact terminal point in one
            // update, at which point movement is zero but the retained direction
            // still records that upward input.
            if (Is2DDoor && inputMoveY >= -0.1f && linkDirection != 1)
                return false;
            var deltaX = linkPixelX - LinkTargetX;
            var deltaY = linkPixelY - LinkTargetY;
            return deltaX * deltaX + deltaY * deltaY <= 2.25f;
        }

        public bool TouchesSideViewTrigger(float x, float y)
        {
            var trigger = Is2DDoor
                ? new Microsoft.Xna.Framework.Rectangle(PixelX + 6, PixelY, 4, Height)
                : DoorGameplayGeometry.GetTrigger(PixelX, PixelY, Width, Height, Mode, true);
            return x - 4 < trigger.Right && x + 4 > trigger.Left &&
                   y - 10 < trigger.Bottom && y > trigger.Top;
        }

        // Mirrors ObjDoor's overworld transition target for Link's real 8x10 body.
        public float LinkTargetX => PixelX + Width / 2f;
        public float LinkTargetY => Is2DDoor
            ? PixelY + 16f
            : IsHoleTeleporter
                ? PixelY + Height / 2f
            : Mode == 0 && Direction == 1
            ? PixelY + 8f
            : Mode == 0 && Direction == 3
                ? PixelY + 16f
                : PixelY + Height / 2f + 5f;

        // Mirrors ObjDoor.PlacePlayer for Link's real 8x10 body. These are the
        // coordinates at which the destination map finishes walking Link in.
        public float GetLinkSpawnX(bool is2dMap)
        {
            if (Is2DDoor)
                return PixelX + 8f;
            if (Mode is 0 or 1)
                return GetWalkingSpawn(is2dMap).X;
            var offset = Mode == 1 && !is2dMap ? 4f : 0f;
            return Direction switch
            {
                0 => PixelX - 4f - offset,
                2 => PixelX + Width + 4f + offset,
                _ => PixelX + Width / 2f
            };
        }

        public float GetLinkSpawnY(bool is2dMap)
        {
            if (Is2DDoor)
                return PixelY + 16f;
            if (Mode is 0 or 1)
                return GetWalkingSpawn(is2dMap).Y;
            var offset = Mode == 1 && !is2dMap ? 4f : 0f;
            return Direction switch
            {
                1 => PixelY - offset,
                3 => PixelY + Height + 10f + offset,
                _ => PixelY + Height / 2f + 5f
            };
        }

        private Microsoft.Xna.Framework.Vector2 GetWalkingSpawn(bool is2dMap) =>
            DoorGameplayGeometry.GetWalkingSpawn(
                DoorGameplayGeometry.GetTrigger(
                    PixelX, PixelY, Width, Height, Mode, is2dMap),
                Direction, Mode, is2dMap, 8f, 10f);
    }

    public readonly struct LiveWallpaperMapHookshotTarget
    {
        public LiveWallpaperMapHookshotTarget(
            int x, int y, int width, int height)
        {
            X = x;
            Y = y;
            Width = Math.Max(1, width);
            Height = Math.Max(1, height);
        }

        public int X { get; }
        public int Y { get; }
        public int Width { get; }
        public int Height { get; }
        public float CenterX => X + Width / 2f;
        public float CenterY => Y + Height / 2f;
    }

    public sealed partial class LiveWallpaperMap
    {
        private const int MaximumWidth = 512;
        private const int MaximumHeight = 512;
        private const int MaximumDepth = 8;
        private const int MaximumTileIndex = 1_000_000;
        private const int MaximumObjectTemplates = 4_096;
        private const int MaximumObjects = 100_000;
        private const int TileSize = 16;
        private readonly int[,,] _tiles;
        private readonly List<CollisionRectangle>[,] _collisionGrid;
        private readonly LiveWallpaperMapTerrain[,] _terrain;
        private IReadOnlyDictionary<int, Microsoft.Xna.Framework.Vector2> _navigationMovedBlocks;
        private IReadOnlySet<int> _navigationRelocatedBlocks;

        // A route-only view: installed geometry/assets are shared unchanged.
        // Created once after a push, never per frame. Moved blocks are solid:
        // ObjMoveStone's moved state cannot be pushed again until it resets.
        public LiveWallpaperMap WithMovedBlocksForNavigation(
            IReadOnlyDictionary<int, Microsoft.Xna.Framework.Vector2> positions,
            IReadOnlySet<int> relocated)
        {
            if (relocated.Count == 0)
                return this;
            var navigation = (LiveWallpaperMap)MemberwiseClone();
            navigation._navigationMovedBlocks = new Dictionary<int, Microsoft.Xna.Framework.Vector2>(positions);
            navigation._navigationRelocatedBlocks = new HashSet<int>(relocated);
            return navigation;
        }

        private LiveWallpaperMap(
            string tilesetPath, int mapOffsetX, int mapOffsetY,
            int width, int height, int depth, int[,,] tiles,
            List<CollisionRectangle>[,] collisionGrid,
            LiveWallpaperMapTerrain[,] terrain,
            int collisionCount,
            int hazardCount,
            int npcWallCount,
            LiveWallpaperMapActor[] actors,
            LiveWallpaperMapPortal[] portals,
            LiveWallpaperMapHookshotTarget[] hookshotTargets,
            LiveWallpaperMapEnemy[] enemies,
            LiveWallpaperMapDecoration[] decorations,
            LiveWallpaperMapAnimatedTile[] animatedTiles,
            LiveWallpaperMapLamp[] lamps,
            LiveWallpaperMapLight[] lights,
            LiveWallpaperMapObject[] objects)
        {
            TilesetPath = tilesetPath;
            MapOffsetX = mapOffsetX;
            MapOffsetY = mapOffsetY;
            Width = width;
            Height = height;
            Depth = depth;
            _tiles = tiles;
            _collisionGrid = collisionGrid;
            _terrain = terrain;
            CollisionCount = collisionCount;
            HazardCount = hazardCount;
            NpcWallCount = npcWallCount;
            Actors = actors ?? [];
            Portals = portals ?? [];
            HookshotTargets = hookshotTargets ?? [];
            Enemies = enemies ?? [];
            Decorations = decorations ?? [];
            AnimatedTiles = animatedTiles ?? [];
            Lamps = lamps ?? [];
            Lights = lights ?? [];
            Objects = objects ?? [];
            SceneEffects = LiveWallpaperSceneEffects.Create(this);
            IsHouse = Objects.Any(mapObject =>
                string.Equals(mapObject.Template, "houseObject",
                    StringComparison.OrdinalIgnoreCase));
            Is2DMap = Objects.Any(mapObject =>
                string.Equals(mapObject.Template, "link2dspawner",
                    StringComparison.OrdinalIgnoreCase));
        }

        public string TilesetPath { get; }
        public int MapOffsetX { get; }
        public int MapOffsetY { get; }
        public int Width { get; }
        public int Height { get; }
        public int Depth { get; }
        public int DrawableDepth => Math.Max(1, Depth - 1);
        public const int OverworldOceanTileIndex = 235;
        public int CollisionCount { get; }
        public int HazardCount { get; }
        public int NpcWallCount { get; }
        public IReadOnlyList<LiveWallpaperMapActor> Actors { get; }
        public IReadOnlyList<LiveWallpaperMapPortal> Portals { get; }
        public IReadOnlyList<LiveWallpaperMapHookshotTarget> HookshotTargets { get; }
        public IReadOnlyList<LiveWallpaperMapEnemy> Enemies { get; }
        public IReadOnlyList<LiveWallpaperMapDecoration> Decorations { get; }
        public IReadOnlyList<LiveWallpaperMapAnimatedTile> AnimatedTiles { get; }
        public IReadOnlyList<LiveWallpaperMapLamp> Lamps { get; }
        public IReadOnlyList<LiveWallpaperMapLight> Lights { get; }
        public IReadOnlyList<LiveWallpaperMapObject> Objects { get; }
        public LiveWallpaperSceneEffects SceneEffects { get; }

        public bool IntersectsHoleTeleporter(
            float x, float y, float width, float height)
        {
            foreach (var portal in Portals)
            {
                if (!portal.IsHoleTeleporter)
                    continue;
                if (x < portal.PixelX + portal.Width &&
                    x + width > portal.PixelX &&
                    y < portal.PixelY + portal.Height &&
                    y + height > portal.PixelY)
                    return true;
            }
            return false;
        }
        public bool IsHouse { get; }
        public bool Is2DMap { get; }

        public int GetTile(int x, int y, int layer)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height ||
                layer < 0 || layer >= Depth)
                return -1;
            return _tiles[x, y, layer];
        }

        public bool HasDrawableTile(int x, int y)
        {
            for (var layer = 0; layer < DrawableDepth; layer++)
            {
                if (GetTile(x, y, layer) >= 0)
                    return true;
            }
            return false;
        }

        public bool NeedsOverworldOceanBase(
            LiveWallpaperMapAnimatedTile animatedTile)
        {
            if (!string.Equals(
                    TilesetPath, "tileset0.png",
                    StringComparison.OrdinalIgnoreCase) ||
                !animatedTile.RequiresOverworldOceanBase)
                return false;
            var tileX = (int)MathF.Floor(animatedTile.EntityX / (float)TileSize);
            var tileY = (int)MathF.Floor(animatedTile.EntityY / (float)TileSize);
            return tileX >= 0 && tileX < Width && tileY >= 0 && tileY < Height &&
                   !HasDrawableTile(tileX, tileY);
        }

        public LiveWallpaperMapTerrain GetTerrain(int tileX, int tileY)
        {
            if (tileX < 0 || tileX >= Width || tileY < 0 || tileY >= Height)
                return LiveWallpaperMapTerrain.Void;
            return _terrain?[tileX, tileY] ?? LiveWallpaperMapTerrain.Ground;
        }

        public bool IsWaterAt(float pixelX, float pixelY)
        {
            var terrain = GetTerrain(
                (int)MathF.Floor(pixelX / TileSize),
                (int)MathF.Floor(pixelY / TileSize));
            return terrain is LiveWallpaperMapTerrain.Water or
                LiveWallpaperMapTerrain.DeepWater;
        }

        public bool IsDeepWaterAt(float pixelX, float pixelY) =>
            GetTerrain((int)MathF.Floor(pixelX / TileSize),
                (int)MathF.Floor(pixelY / TileSize)) ==
            LiveWallpaperMapTerrain.DeepWater;

        public bool IntersectsVoid(float x, float y, float width, float height)
        {
            if (width <= 0 || height <= 0)
                return false;
            if (x < 0 || y < 0 || x + width > Width * TileSize ||
                y + height > Height * TileSize)
                return true;
            var startX = Math.Clamp((int)MathF.Floor(x / TileSize), 0, Width - 1);
            var startY = Math.Clamp((int)MathF.Floor(y / TileSize), 0, Height - 1);
            var endX = Math.Clamp((int)MathF.Floor((x + width - 0.001f) / TileSize),
                0, Width - 1);
            var endY = Math.Clamp((int)MathF.Floor((y + height - 0.001f) / TileSize),
                0, Height - 1);
            for (var tileY = startY; tileY <= endY; tileY++)
            for (var tileX = startX; tileX <= endX; tileX++)
            {
                if (GetTerrain(tileX, tileY) == LiveWallpaperMapTerrain.Void)
                    return true;
            }
            return false;
        }

        public float GetBlockingOverlapArea(
            float x, float y, float width, float height, bool includeHoles,
            bool includeBushes = true,
            IReadOnlySet<int> ignoredBushes = null,
            bool includeStones = true,
            IReadOnlySet<int> ignoredStones = null,
            bool includeMoveStones = true,
            IReadOnlySet<int> ignoredMoveStones = null)
        {
            var bodyArea = width * height;
            if (bodyArea <= 0f)
                return 0f;

            var mapRight = Width * TileSize;
            var mapBottom = Height * TileSize;
            var insideWidth = MathF.Max(
                0f, MathF.Min(x + width, mapRight) - MathF.Max(x, 0f));
            var insideHeight = MathF.Max(
                0f, MathF.Min(y + height, mapBottom) - MathF.Max(y, 0f));
            var overlapArea = bodyArea - insideWidth * insideHeight;
            if (insideWidth <= 0f || insideHeight <= 0f)
                return bodyArea;

            var startX = Math.Clamp((int)MathF.Floor(MathF.Max(x, 0f) / TileSize),
                0, Width - 1);
            var startY = Math.Clamp((int)MathF.Floor(MathF.Max(y, 0f) / TileSize),
                0, Height - 1);
            var endX = Math.Clamp((int)MathF.Floor(
                (MathF.Min(x + width, mapRight) - 0.001f) / TileSize),
                0, Width - 1);
            var endY = Math.Clamp((int)MathF.Floor(
                (MathF.Min(y + height, mapBottom) - 0.001f) / TileSize),
                0, Height - 1);
            var seen = new HashSet<(int X, int Y, int Width, int Height,
                CollisionKind Kind)>();
            for (var tileY = startY; tileY <= endY; tileY++)
            for (var tileX = startX; tileX <= endX; tileX++)
            {
                if (GetTerrain(tileX, tileY) == LiveWallpaperMapTerrain.Void)
                    overlapArea += GetIntersectionArea(
                        x, y, width, height,
                        tileX * TileSize, tileY * TileSize, TileSize, TileSize);

                var entries = _collisionGrid?[tileX, tileY];
                if (entries == null)
                    continue;
                foreach (var entry in entries)
                {
                    if (!seen.Add((entry.X, entry.Y, entry.Width, entry.Height,
                            entry.Kind)) || entry.Kind is CollisionKind.NpcWall or CollisionKind.Ladder or CollisionKind.LadderTop ||
                        entry.Kind == CollisionKind.Hole && !includeHoles ||
                        entry.Kind == CollisionKind.Bush &&
                        (!includeBushes || ignoredBushes?.Contains(
                            GetBushKey(entry.X, entry.Y)) == true) ||
                        entry.Kind == CollisionKind.Stone &&
                        (!includeStones || ignoredStones?.Contains(
                            GetStoneKey(entry.X, entry.Y)) == true) ||
                        entry.Kind == CollisionKind.MoveStone &&
                        ((!includeMoveStones && IsPushableMoveStone(
                              GetMoveStoneKey(entry.X, entry.Y))) ||
                         ignoredMoveStones?.Contains(
                             GetMoveStoneKey(entry.X, entry.Y)) == true))
                        continue;
                    overlapArea += GetIntersectionArea(
                        x, y, width, height,
                        entry.X, entry.Y, entry.Width, entry.Height);
                }
            }
            return MathF.Min(overlapArea, bodyArea);
        }

        public bool TryGetBlockingCollisionBounds(
            float x, float y, float width, float height, bool includeHoles,
            out LiveWallpaperCollisionBounds bounds,
            bool includeBushes = true,
            IReadOnlySet<int> ignoredBushes = null,
            bool includeStones = true,
            IReadOnlySet<int> ignoredStones = null,
            bool includeMoveStones = true,
            IReadOnlySet<int> ignoredMoveStones = null)
        {
            bounds = default;
            if (width <= 0f || height <= 0f)
                return false;
            var mapRight = Width * TileSize;
            var mapBottom = Height * TileSize;
            if (x < 0f)
            {
                bounds = new LiveWallpaperCollisionBounds(-TileSize, 0f,
                    TileSize, mapBottom);
                return true;
            }
            if (x + width > mapRight)
            {
                bounds = new LiveWallpaperCollisionBounds(mapRight, 0f,
                    TileSize, mapBottom);
                return true;
            }
            if (y < 0f)
            {
                bounds = new LiveWallpaperCollisionBounds(0f, -TileSize,
                    mapRight, TileSize);
                return true;
            }
            if (y + height > mapBottom)
            {
                bounds = new LiveWallpaperCollisionBounds(0f, mapBottom,
                    mapRight, TileSize);
                return true;
            }

            var startX = Math.Clamp((int)MathF.Floor(x / TileSize), 0, Width - 1);
            var startY = Math.Clamp((int)MathF.Floor(y / TileSize), 0, Height - 1);
            var endX = Math.Clamp((int)MathF.Floor((x + width - 0.001f) / TileSize),
                0, Width - 1);
            var endY = Math.Clamp((int)MathF.Floor((y + height - 0.001f) / TileSize),
                0, Height - 1);
            for (var tileY = startY; tileY <= endY; tileY++)
            for (var tileX = startX; tileX <= endX; tileX++)
            {
                if (GetTerrain(tileX, tileY) == LiveWallpaperMapTerrain.Void)
                {
                    bounds = new LiveWallpaperCollisionBounds(
                        tileX * TileSize, tileY * TileSize, TileSize, TileSize);
                    return true;
                }
                var entries = _collisionGrid?[tileX, tileY];
                if (entries == null)
                    continue;
                foreach (var entry in entries)
                {
                    var ignoredMoveStone =
                        entry.Kind == CollisionKind.MoveStone &&
                        (!includeMoveStones && IsPushableMoveStone(
                             GetMoveStoneKey(entry.X, entry.Y)) ||
                         ignoredMoveStones?.Contains(
                             GetMoveStoneKey(entry.X, entry.Y)) == true);
                    if (entry.Kind is CollisionKind.NpcWall or CollisionKind.Ladder or CollisionKind.LadderTop ||
                        entry.Kind == CollisionKind.Hole && !includeHoles ||
                        entry.Kind == CollisionKind.Bush &&
                        (!includeBushes || ignoredBushes?.Contains(
                            GetBushKey(entry.X, entry.Y)) == true) ||
                        entry.Kind == CollisionKind.Stone &&
                        (!includeStones || ignoredStones?.Contains(
                            GetStoneKey(entry.X, entry.Y)) == true) ||
                        ignoredMoveStone ||
                        !entry.Intersects(x, y, width, height))
                        continue;
                    bounds = new LiveWallpaperCollisionBounds(
                        entry.X, entry.Y, entry.Width, entry.Height);
                    return true;
                }
            }
            return false;
        }

        private static float GetIntersectionArea(
            float x, float y, float width, float height,
            float otherX, float otherY, float otherWidth, float otherHeight)
        {
            var intersectionWidth = MathF.Min(x + width, otherX + otherWidth) -
                                    MathF.Max(x, otherX);
            var intersectionHeight = MathF.Min(y + height, otherY + otherHeight) -
                                     MathF.Max(y, otherY);
            return intersectionWidth > 0f && intersectionHeight > 0f
                ? intersectionWidth * intersectionHeight
                : 0f;
        }

        /// <summary>
        /// Tests a pixel-space body rectangle against the collision and hole objects stored
        /// after the visual tile layers in the original map file.
        /// </summary>
        public bool IntersectsCollision(
            float x, float y, float width, float height, bool includeHoles,
            bool includeBushes = true,
            IReadOnlySet<int> ignoredBushes = null,
            bool includeStones = true,
            IReadOnlySet<int> ignoredStones = null,
            bool includeMoveStones = true,
            IReadOnlySet<int> ignoredMoveStones = null)
        {
            if (width <= 0 || height <= 0)
                return false;
            if (x < 0 || y < 0 || x + width > Width * TileSize ||
                y + height > Height * TileSize)
                return true;
            if (_collisionGrid == null)
                return false;

            if (_navigationMovedBlocks != null)
            {
                foreach (var position in _navigationMovedBlocks.Values)
                {
                    if (x < position.X + TileSize && x + width > position.X &&
                        y < position.Y + TileSize && y + height > position.Y)
                        return true;
                }
            }

            var startX = Math.Clamp((int)MathF.Floor(x / TileSize), 0, Width - 1);
            var startY = Math.Clamp((int)MathF.Floor(y / TileSize), 0, Height - 1);
            var endX = Math.Clamp((int)MathF.Floor((x + width - 0.001f) / TileSize),
                0, Width - 1);
            var endY = Math.Clamp((int)MathF.Floor((y + height - 0.001f) / TileSize),
                0, Height - 1);
            for (var tileY = startY; tileY <= endY; tileY++)
            {
                for (var tileX = startX; tileX <= endX; tileX++)
                {
                    var entries = _collisionGrid[tileX, tileY];
                    if (entries == null)
                        continue;
                    foreach (var entry in entries)
                    {
                        if (entry.Kind == CollisionKind.Bush &&
                            (!includeBushes || ignoredBushes?.Contains(
                                GetBushKey(entry.X, entry.Y)) == true))
                            continue;
                        if (entry.Kind == CollisionKind.Stone &&
                            (!includeStones || ignoredStones?.Contains(
                                GetStoneKey(entry.X, entry.Y)) == true))
                            continue;
                        if (entry.Kind == CollisionKind.MoveStone &&
                            ((!includeMoveStones && IsPushableMoveStone(
                                  GetMoveStoneKey(entry.X, entry.Y))) ||
                             _navigationRelocatedBlocks?.Contains(
                                 GetMoveStoneKey(entry.X, entry.Y)) == true ||
                             ignoredMoveStones?.Contains(
                                 GetMoveStoneKey(entry.X, entry.Y)) == true))
                            continue;
                        if (entry.Kind is not (CollisionKind.NpcWall or CollisionKind.Ladder or CollisionKind.LadderTop) &&
                            (entry.Kind != CollisionKind.Hole || includeHoles) && entry.Intersects(
                                x, y, width, height))
                            return true;
                    }
                }
            }
            return false;
        }

        public bool TryGetBushKey(
            float x, float y, float width, float height, out int bushKey,
            IReadOnlySet<int> ignoredBushes = null)
        {
            bushKey = -1;
            if (width <= 0 || height <= 0 || _collisionGrid == null ||
                x < 0 || y < 0 || x + width > Width * TileSize ||
                y + height > Height * TileSize)
                return false;
            var startX = Math.Clamp((int)MathF.Floor(x / TileSize), 0, Width - 1);
            var startY = Math.Clamp((int)MathF.Floor(y / TileSize), 0, Height - 1);
            var endX = Math.Clamp((int)MathF.Floor((x + width - 0.001f) / TileSize),
                0, Width - 1);
            var endY = Math.Clamp((int)MathF.Floor((y + height - 0.001f) / TileSize),
                0, Height - 1);
            for (var tileY = startY; tileY <= endY; tileY++)
            for (var tileX = startX; tileX <= endX; tileX++)
            {
                var entries = _collisionGrid[tileX, tileY];
                if (entries == null)
                    continue;
                foreach (var entry in entries)
                {
                    if (entry.Kind != CollisionKind.Bush ||
                        !entry.Intersects(x, y, width, height))
                        continue;
                    var candidateKey = GetBushKey(entry.X, entry.Y);
                    if (ignoredBushes?.Contains(candidateKey) == true)
                        continue;
                    bushKey = candidateKey;
                    return true;
                }
            }
            return false;
        }

        public bool TryGetBushKeyAlongMovement(
            float x, float y, float width, float height,
            float movementX, float movementY, out int bushKey,
            IReadOnlySet<int> ignoredBushes = null)
        {
            // Link collision resolves X and Y separately. A diagonal endpoint can
            // be clear even though one of those intermediate axis steps clips a
            // bush corner, so probe in the exact same order as movement.
            if (movementX != 0f &&
                TryGetBushKey(x + movementX, y, width, height, out bushKey,
                    ignoredBushes))
                return true;
            if (movementY != 0f &&
                TryGetBushKey(x, y + movementY, width, height, out bushKey,
                    ignoredBushes))
                return true;
            return TryGetBushKey(
                x + movementX, y + movementY, width, height, out bushKey,
                ignoredBushes);
        }

        public int GetBushKey(int pixelX, int pixelY) =>
            pixelY / TileSize * Width + pixelX / TileSize;

        /// <summary>
        /// Finds an ObjBush sword target. Unlike bushes, the game's gras templates have no
        /// collision component, so they must be read from the object-decoration layer rather
        /// than the collision grid.
        /// </summary>
        public bool TryGetCuttableVegetationKey(
            float x, float y, float width, float height, out int vegetationKey,
            IReadOnlySet<int> ignoredVegetation = null)
        {
            if (TryGetBushKey(
                    x, y, width, height, out vegetationKey,
                    ignoredVegetation))
                return true;
            foreach (var decoration in Decorations)
            {
                if (!IsGrassSprite(decoration.SpriteId))
                    continue;
                var left = decoration.EntityX - 8;
                var top = decoration.EntityY - 8;
                if (x >= left + TileSize || x + width <= left ||
                    y >= top + TileSize || y + height <= top)
                    continue;
                var candidate = GetBushKey(left, top);
                if (ignoredVegetation?.Contains(candidate) == true)
                    continue;
                vegetationKey = candidate;
                return true;
            }
            vegetationKey = -1;
            return false;
        }

        public static bool IsGrassSprite(string spriteId) =>
            spriteId is "grass_0" or "grass_0_0" or "grass_0_1" or
                "grass_0_2" or "grass_0_3" or "grass_1" or "grass_2";

        public static bool IsBushSprite(string spriteId) =>
            spriteId is "bush_0" or "bush_1";

        public bool TryGetStoneKey(
            float x, float y, float width, float height, out int stoneKey,
            IReadOnlySet<int> ignoredStones = null)
        {
            stoneKey = -1;
            if (width <= 0 || height <= 0 || _collisionGrid == null ||
                x < 0 || y < 0 || x + width > Width * TileSize ||
                y + height > Height * TileSize)
                return false;
            var startX = Math.Clamp((int)MathF.Floor(x / TileSize), 0, Width - 1);
            var startY = Math.Clamp((int)MathF.Floor(y / TileSize), 0, Height - 1);
            var endX = Math.Clamp((int)MathF.Floor((x + width - 0.001f) / TileSize),
                0, Width - 1);
            var endY = Math.Clamp((int)MathF.Floor((y + height - 0.001f) / TileSize),
                0, Height - 1);
            for (var tileY = startY; tileY <= endY; tileY++)
            for (var tileX = startX; tileX <= endX; tileX++)
            {
                var entries = _collisionGrid[tileX, tileY];
                if (entries == null)
                    continue;
                foreach (var entry in entries)
                {
                    if (entry.Kind != CollisionKind.Stone ||
                        !entry.Intersects(x, y, width, height))
                        continue;
                    var candidateKey = GetStoneKey(entry.X, entry.Y);
                    if (ignoredStones?.Contains(candidateKey) == true)
                        continue;
                    stoneKey = candidateKey;
                    return true;
                }
            }
            return false;
        }

        public bool TryGetStoneKeyAlongMovement(
            float x, float y, float width, float height,
            float movementX, float movementY, out int stoneKey,
            IReadOnlySet<int> ignoredStones = null)
        {
            if (movementX != 0f &&
                TryGetStoneKey(x + movementX, y, width, height, out stoneKey,
                    ignoredStones))
                return true;
            if (movementY != 0f &&
                TryGetStoneKey(x, y + movementY, width, height, out stoneKey,
                    ignoredStones))
                return true;
            return TryGetStoneKey(
                x + movementX, y + movementY, width, height, out stoneKey,
                ignoredStones);
        }

        public int GetStoneKey(int pixelX, int pixelY) =>
            pixelY / TileSize * Width + pixelX / TileSize;

        public static bool IsMoveStoneSprite(string spriteId) =>
            spriteId is "movestone_0" or "movestone_1" or
                "movestone_2" or "movestone_3";

        public int GetMoveStoneKey(int pixelX, int pixelY) =>
            pixelY / TileSize * Width + pixelX / TileSize;

        public int GetMoveStoneKey(LiveWallpaperMapDecoration decoration) =>
            IsMoveStoneSprite(decoration.SpriteId)
                ? GetMoveStoneKey(decoration.EntityX, decoration.EntityY)
                : -1;

        public bool TryGetMoveStone(
            int moveStoneKey, out float pixelX, out float pixelY,
            out int allowedDirections)
        {
            foreach (var mapObject in Objects)
            {
                if (!IsMoveStoneTemplate(mapObject.Template) ||
                    GetMoveStoneKey(mapObject.PixelX, mapObject.PixelY) !=
                    moveStoneKey)
                    continue;
                pixelX = mapObject.PixelX;
                pixelY = mapObject.PixelY;
                allowedDirections = GetMoveStoneDirections(mapObject);
                return true;
            }
            pixelX = 0f;
            pixelY = 0f;
            allowedDirections = 0;
            return false;
        }

        public bool TryGetMoveStoneAt(
            float x, float y, float width, float height,
            out int moveStoneKey,
            IReadOnlySet<int> ignoredMoveStones = null)
        {
            moveStoneKey = -1;
            if (width <= 0 || height <= 0 || _collisionGrid == null)
                return false;
            var minX = Math.Clamp((int)MathF.Floor(x / TileSize), 0, Width - 1);
            var minY = Math.Clamp((int)MathF.Floor(y / TileSize), 0, Height - 1);
            var maxX = Math.Clamp((int)MathF.Floor((x + width - .001f) / TileSize), 0, Width - 1);
            var maxY = Math.Clamp((int)MathF.Floor((y + height - .001f) / TileSize), 0, Height - 1);
            for (var tileY = minY; tileY <= maxY; tileY++)
            for (var tileX = minX; tileX <= maxX; tileX++)
            {
                var entries = _collisionGrid[tileX, tileY];
                if (entries == null) continue;
                foreach (var entry in entries)
                {
                    if (entry.Kind != CollisionKind.MoveStone ||
                        !entry.Intersects(x, y, width, height))
                        continue;
                    var key = GetMoveStoneKey(entry.X, entry.Y);
                    if (ignoredMoveStones?.Contains(key) == true ||
                        _navigationRelocatedBlocks?.Contains(key) == true)
                        continue;
                    moveStoneKey = key;
                    return true;
                }
            }
            return false;
        }

        public bool IsPushableMoveStone(int moveStoneKey) =>
            TryGetMoveStone(moveStoneKey, out _, out _, out var directions) &&
            directions != 0;

        public bool CanPushMoveStone(int key, int direction)
        {
            if (_navigationRelocatedBlocks?.Contains(key) == true ||
                !TryGetMoveStone(key, out var x, out var y, out var allowed) ||
                (allowed != -1 && (allowed & (1 << direction)) == 0))
                return false;
            x += direction == 0 ? -TileSize : direction == 2 ? TileSize : 0;
            y += direction == 1 ? -TileSize : direction == 3 ? TileSize : 0;
            // ObjMoveStone.OnPush tests the complete destination tile against
            // Normal | Passageway collision. Holes and water permit a push.
            if (IntersectsVoid(x, y, TileSize, TileSize) ||
                IntersectsCollision(x, y, TileSize, TileSize, includeHoles: false))
                return false;
            foreach (var portal in Portals)
            {
                if (!portal.Is2DDoor && !portal.IsHoleTeleporter &&
                    x < portal.PixelX + portal.Width && x + TileSize > portal.PixelX &&
                    y < portal.PixelY + portal.Height && y + TileSize > portal.PixelY)
                    return false;
            }
            return true;
        }

        private static bool IsMoveStoneTemplate(string template) =>
            template is "moveStone" or "moveStoneCave" or
                "moveStoneFrogHouse" or "moveStoneD3";

        private static int GetMoveStoneDirections(LiveWallpaperMapObject mapObject)
        {
            if (mapObject.Arguments.Count == 0 ||
                string.IsNullOrWhiteSpace(mapObject.Arguments[0]))
                return 15;
            return int.TryParse(
                mapObject.Arguments[0], NumberStyles.Integer,
                CultureInfo.InvariantCulture, out var directions)
                ? directions
                : 15;
        }

        public int GetChestKey(int pixelX, int pixelY) =>
            pixelY / TileSize * Width + pixelX / TileSize;

        public int GetStoneKey(LiveWallpaperMapDecoration decoration)
        {
            if (!decoration.StoneLayout)
                return -1;
            var mapPosition = GameObjectVisualLayout.GetStoneMapPosition(
                decoration.EntityX, decoration.EntityY);
            // ObjStone's installed collision rectangle is (x, y + 1, 16, 13).
            // The +1 matters for the few valid overworld stones placed at y%16=15.
            return GetStoneKey(
                (int)mapPosition.X, (int)mapPosition.Y + 1);
        }

        public bool TryGetStoneMapPosition(
            int stoneKey, out float pixelX, out float pixelY)
        {
            foreach (var decoration in Decorations)
            {
                if (!decoration.StoneLayout ||
                    GetStoneKey(decoration) != stoneKey)
                    continue;
                var mapPosition = GameObjectVisualLayout.GetStoneMapPosition(
                    decoration.EntityX, decoration.EntityY);
                pixelX = mapPosition.X;
                pixelY = mapPosition.Y;
                return true;
            }
            pixelX = 0f;
            pixelY = 0f;
            return false;
        }

        public bool IntersectsNpcWall(float x, float y, float width, float height) =>
            IntersectsKind(x, y, width, height, CollisionKind.NpcWall);

        public bool IntersectsHole(float x, float y, float width, float height) =>
            IntersectsKind(x, y, width, height, CollisionKind.Hole);

        public float GetLinkHoleCoverage(float x, float y, float width, float height) =>
            GetLinkHoleContact(x, y, width, height).Coverage;

        public LiveWallpaperHoleContact GetLinkHoleContact(
            float x, float y, float width, float height)
        {
            if (width <= 0f || height <= 0f || _collisionGrid == null ||
                x < 0f || y < 0f || x + width > Width * TileSize ||
                y + height > Height * TileSize)
                return default;
            var startX = Math.Clamp((int)MathF.Floor(x / TileSize), 0, Width - 1);
            var startY = Math.Clamp((int)MathF.Floor(y / TileSize), 0, Height - 1);
            var endX = Math.Clamp((int)MathF.Floor((x + width - 0.001f) / TileSize),
                0, Width - 1);
            var endY = Math.Clamp((int)MathF.Floor((y + height - 0.001f) / TileSize),
                0, Height - 1);
            var seen = new HashSet<(int X, int Y, int Width, int Height)>();
            var coveredArea = 0f;
            var collisionCenterX = 0f;
            var collisionCenterY = 0f;
            var bodyArea = width * height;
            var bodyCenterX = x + width / 2f;
            var bodyCenterY = y + height / 2f;
            for (var tileY = startY; tileY <= endY; tileY++)
            for (var tileX = startX; tileX <= endX; tileX++)
            {
                var entries = _collisionGrid[tileX, tileY];
                if (entries == null)
                    continue;
                foreach (var entry in entries)
                {
                    if (entry.Kind != CollisionKind.Hole ||
                        !seen.Add((entry.X, entry.Y, entry.Width, entry.Height)))
                        continue;
                    var left = MathF.Max(x, entry.X);
                    var top = MathF.Max(y, entry.Y);
                    var right = MathF.Min(x + width, entry.X + entry.Width);
                    var bottom = MathF.Min(y + height, entry.Y + entry.Height);
                    var intersectionWidth = right - left;
                    var intersectionHeight = bottom - top;
                    if (intersectionWidth <= 0f || intersectionHeight <= 0f)
                        continue;
                    // SystemBody.UpdateHole gives Link the original 2.5-pixel
                    // bottom-edge leniency before computing absorption area.
                    var yDifference = bodyCenterY -
                                      (entry.Y + entry.Height / 2f);
                    if (yDifference > 0f)
                        intersectionHeight = MathF.Max(
                            0f, intersectionHeight -
                                Math.Clamp(yDifference, 0f, 2.5f));
                    var intersectionArea = intersectionWidth * intersectionHeight;
                    if (intersectionArea <= 0f || float.IsNaN(intersectionArea))
                        continue;
                    var totalArea = coveredArea + intersectionArea;
                    collisionCenterX =
                        collisionCenterX * (coveredArea / totalArea) +
                        (left + intersectionWidth / 2f) *
                        (intersectionArea / totalArea);
                    collisionCenterY =
                        collisionCenterY * (coveredArea / totalArea) +
                        (top + intersectionHeight / 2f) *
                        (intersectionArea / totalArea);
                    coveredArea = totalArea;
                }
            }
            var coverage = Math.Clamp(coveredArea / bodyArea, 0f, 1f);
            if (coveredArea <= 0f)
                return new LiveWallpaperHoleContact(coverage, 0f, 0f);

            // This is SystemBody.UpdateHole's exact center-of-mass direction:
            // the hole intersection pulls away from the remaining body mass.
            var nonCollisionCenterX = bodyCenterX +
                (bodyCenterX - collisionCenterX) * (coveredArea / bodyArea);
            var nonCollisionCenterY = bodyCenterY +
                (bodyCenterY - collisionCenterY) * (coveredArea / bodyArea);
            var directionX = collisionCenterX - nonCollisionCenterX;
            var directionY = collisionCenterY - nonCollisionCenterY;
            var directionLength = MathF.Sqrt(
                directionX * directionX + directionY * directionY);
            if (directionLength > 0f)
            {
                directionX /= directionLength;
                directionY /= directionLength;
            }
            return new LiveWallpaperHoleContact(
                coverage, directionX, directionY);
        }

        public bool IntersectsActor(
            float x, float y, float width, float height,
            int ignoredActorIndex = -1, bool ignoreOwl = false)
        {
            if (width <= 0 || height <= 0)
                return false;
            for (var index = 0; index < Actors.Count; index++)
            {
                if (index == ignoredActorIndex)
                    continue;
                var actor = Actors[index];
                if (ignoreOwl && actor.Kind == LiveWallpaperMapActorKind.Owl)
                    continue;
                if (actor.BodyWidth <= 0 || actor.BodyHeight <= 0)
                    continue;
                if (x < actor.BodyX + actor.BodyWidth && x + width > actor.BodyX &&
                    y < actor.BodyY + actor.BodyHeight && y + height > actor.BodyY)
                    return true;
            }
            return false;
        }

        public bool IntersectsEnemy(
            float x, float y, float width, float height, int ignoredEnemyIndex = -1)
        {
            if (width <= 0 || height <= 0)
                return false;
            for (var index = 0; index < Enemies.Count; index++)
            {
                if (index == ignoredEnemyIndex)
                    continue;
                var enemy = Enemies[index];
                if (x < enemy.BodyX + enemy.BodyWidth && x + width > enemy.BodyX &&
                    y < enemy.BodyY + enemy.BodyHeight && y + height > enemy.BodyY)
                    return true;
            }
            return false;
        }

        public static bool TryLoad(TextReader reader, out LiveWallpaperMap map)
        {
            map = null;
            if (reader == null || !TryReadInt(reader, out var version) || version is < 1 or > 3)
                return false;

            var mapOffsetX = 0;
            var mapOffsetY = 0;
            if (version > 2 &&
                (!TryReadInt(reader, out mapOffsetX) ||
                 !TryReadInt(reader, out mapOffsetY)))
                return false;

            var tilesetPath = reader.ReadLine()?.Trim();
            if (!LiveWallpaperAnimation.TryNormalizeRelativePath(tilesetPath, out tilesetPath) ||
                !tilesetPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                return false;
            if (!TryReadInt(reader, out var width) || width is <= 0 or > MaximumWidth ||
                !TryReadInt(reader, out var height) || height is <= 0 or > MaximumHeight ||
                !TryReadInt(reader, out var depth) || depth is <= 0 or > MaximumDepth)
                return false;

            var tiles = new int[width, height, depth];
            for (var layer = 0; layer < depth; layer++)
            {
                for (var y = 0; y < height; y++)
                {
                    var values = reader.ReadLine()?.Split(',');
                    if (values == null || values.Length < width)
                        return false;
                    for (var x = 0; x < width; x++)
                    {
                        if (string.IsNullOrWhiteSpace(values[x]))
                        {
                            tiles[x, y, layer] = -1;
                            continue;
                        }
                        if (!int.TryParse(values[x], NumberStyles.Integer,
                                CultureInfo.InvariantCulture, out var tile) ||
                            tile is < 0 or > MaximumTileIndex)
                            return false;
                        tiles[x, y, layer] = tile;
                    }
                }
            }

            var terrain = new LiveWallpaperMapTerrain[width, height];
            var drawableDepth = Math.Max(1, depth - 1);
            for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                terrain[x, y] = LiveWallpaperMapTerrain.Void;
                for (var layer = 0; layer < drawableDepth; layer++)
                {
                    if (tiles[x, y, layer] < 0)
                        continue;
                    terrain[x, y] = LiveWallpaperMapTerrain.Ground;
                    break;
                }
            }

            if (!TryLoadCollisionObjects(reader, width, height, terrain,
                    out var collisionGrid, out var collisionCount, out var hazardCount,
                    out var npcWallCount, out var actors, out var portals,
                    out var hookshotTargets,
                    out var enemies, out var decorations, out var animatedTiles,
                    out var lamps, out var lights,
                    out var objects))
                return false;

            map = new LiveWallpaperMap(
                tilesetPath, mapOffsetX, mapOffsetY, width, height, depth, tiles,
                collisionGrid, terrain, collisionCount, hazardCount, npcWallCount,
                actors, portals, hookshotTargets, enemies, decorations,
                animatedTiles, lamps, lights, objects);
            return true;
        }

        private static bool TryLoadCollisionObjects(
            TextReader reader,
            int width,
            int height,
            LiveWallpaperMapTerrain[,] terrain,
            out List<CollisionRectangle>[,] collisionGrid,
            out int collisionCount,
            out int hazardCount,
            out int npcWallCount,
            out LiveWallpaperMapActor[] actors,
            out LiveWallpaperMapPortal[] portals,
            out LiveWallpaperMapHookshotTarget[] hookshotTargets,
            out LiveWallpaperMapEnemy[] enemies,
            out LiveWallpaperMapDecoration[] decorations,
            out LiveWallpaperMapAnimatedTile[] animatedTiles,
            out LiveWallpaperMapLamp[] lamps,
            out LiveWallpaperMapLight[] lights,
            out LiveWallpaperMapObject[] objects)
        {
            collisionGrid = null;
            collisionCount = 0;
            hazardCount = 0;
            npcWallCount = 0;
            actors = [];
            portals = [];
            hookshotTargets = [];
            enemies = [];
            decorations = [];
            animatedTiles = [];
            lamps = [];
            lights = [];
            objects = [];
            var templateCountLine = reader.ReadLine();
            if (templateCountLine == null)
                return true;
            if (!int.TryParse(templateCountLine, NumberStyles.Integer,
                    CultureInfo.InvariantCulture, out var templateCount) ||
                templateCount is < 0 or > MaximumObjectTemplates)
                return false;

            var templates = new string[templateCount];
            for (var index = 0; index < templateCount; index++)
            {
                templates[index] = reader.ReadLine();
                if (string.IsNullOrWhiteSpace(templates[index]) ||
                    templates[index].Length > 256)
                    return false;
            }
            if (!TryReadInt(reader, out var objectCount) ||
                objectCount is < 0 or > MaximumObjects)
                return false;

            collisionGrid = new List<CollisionRectangle>[width, height];
            var parsedActors = new List<LiveWallpaperMapActor>();
            var parsedPortals = new List<LiveWallpaperMapPortal>();
            var parsedHookshotTargets =
                new List<LiveWallpaperMapHookshotTarget>();
            var parsedEnemies = new List<LiveWallpaperMapEnemy>();
            var parsedDecorations = new List<LiveWallpaperMapDecoration>();
            var parsedAnimatedTiles = new List<LiveWallpaperMapAnimatedTile>();
            var parsedLamps = new List<LiveWallpaperMapLamp>();
            var parsedLights = new List<LiveWallpaperMapLight>();
            var parsedObjects = new List<LiveWallpaperMapObject>();
            for (var index = 0; index < objectCount; index++)
            {
                var line = reader.ReadLine();
                var parts = line?.Split(';');
                if (parts == null || parts.Length < 3 ||
                    !int.TryParse(parts[0], NumberStyles.Integer,
                        CultureInfo.InvariantCulture, out var templateIndex) ||
                    templateIndex < 0 || templateIndex >= templates.Length ||
                    !TryParseInt(parts[1], out var positionX) ||
                    !TryParseInt(parts[2], out var positionY))
                    return false;

                var arguments = new string[Math.Max(0, parts.Length - 3)];
                if (arguments.Length > 0)
                    Array.Copy(parts, 3, arguments, 0, arguments.Length);
                parsedObjects.Add(new LiveWallpaperMapObject(
                    templates[templateIndex], positionX, positionY, arguments));

                AddObjectCollision(
                    collisionGrid, width, height, templates[templateIndex], parts,
                    positionX, positionY, ref collisionCount, ref hazardCount,
                    ref npcWallCount);
                TryAddMapActor(
                    parsedActors, templates[templateIndex], parts, positionX, positionY);
                TryAddMapPortal(
                    parsedPortals, templates[templateIndex], parts, positionX, positionY);
                TryAddHookshotTarget(
                    parsedHookshotTargets, templates[templateIndex],
                    positionX, positionY);
                TryAddMapEnemy(
                    parsedEnemies, templates[templateIndex], parts, positionX, positionY);
                TryAddMapDecoration(
                    parsedDecorations, templates[templateIndex], parts,
                    positionX, positionY);
                ApplyMapTerrain(
                    terrain, width, height, templates[templateIndex], positionX, positionY);
                TryAddAnimatedTile(
                    parsedAnimatedTiles, templates[templateIndex], positionX, positionY);
                TryAddLamp(
                    parsedLamps, parsedObjects[^1]);
                TryAddLight(
                    parsedLights, templates[templateIndex], positionX, positionY);
            }
            actors = parsedActors.ToArray();
            portals = parsedPortals.ToArray();
            hookshotTargets = parsedHookshotTargets.ToArray();
            enemies = parsedEnemies.ToArray();
            // ComponentDrawPool orders the game's objects by draw layer and then
            // CPosition.Y. Preserve that exact painter order for overlapping map
            // sprites such as the two-tile overworld trees.
            decorations = parsedDecorations
                .OrderBy(decoration => decoration.PlayerLayer ? 1 : 0)
                .ThenBy(decoration => decoration.EntityY)
                .ToArray();
            animatedTiles = parsedAnimatedTiles.ToArray();
            lamps = parsedLamps.ToArray();
            lights = parsedLights.ToArray();
            objects = parsedObjects.ToArray();
            return true;
        }

        private static void TryAddLamp(
            List<LiveWallpaperMapLamp> lamps,
            LiveWallpaperMapObject mapObject)
        {
            var positionX = mapObject.PixelX;
            var positionY = mapObject.PixelY;
            if (mapObject.Template == "overworldTeleporter")
            {
                // ObjOverworldTeleporter uses CPosition(posX,posY) with an
                // AnimationComponent offset of (8,8). holeTeleporter.ani then
                // supplies its exact per-frame -7/-9 sprite offsets.
                lamps.Add(new LiveWallpaperMapLamp("Objects/holeTeleporter.ani",
                    positionX + 8, positionY + 8, positionX, positionY, 0, false));
                return;
            }
            if (!LiveWallpaperSceneEffects.TryResolve(mapObject, out var definition, out var p) ||
                definition.ObjectType != typeof(InGame.GameObjects.Things.ObjLamp) ||
                !LiveWallpaperAnimation.TryNormalizeRelativePath((string)p[0] + ".ani", out var path))
                return;

            // ObjLamp uses CPosition(posX, posY + 8), a sprite center of (8,8),
            // and AnimationComponent offset (8,0). That leaves the unrotated
            // animation's top-left at the map placement and rotates around its
            // exact 8x8 centre.
            lamps.Add(new LiveWallpaperMapLamp(
                path, positionX, positionY, positionX + 8, positionY + 8,
                (int)p[1], (bool)p[2],
                (bool)p[3] || !string.IsNullOrEmpty(p[4] as string) ? "dead" : "idle"));
        }

        private static void TryAddLight(
            List<LiveWallpaperMapLight> lights,
            string template, int positionX, int positionY)
        {
            if (template != "doorLight")
                return;

            // GameObjectTemplates.doorLight passes these exact ObjLight values.
            lights.Add(new LiveWallpaperMapLight(
                positionX + 8, positionY + 8,
                128, 255, 255, 255, 100));
        }

        private static void ApplyMapTerrain(
            LiveWallpaperMapTerrain[,] terrain, int width, int height,
            string template, int positionX, int positionY)
        {
            if (terrain == null || template is not ("water" or "waterDeep"))
                return;
            var tileX = positionX / TileSize;
            var tileY = positionY / TileSize;
            if (tileX >= 0 && tileX < width && tileY >= 0 && tileY < height)
                terrain[tileX, tileY] = template == "waterDeep"
                    ? LiveWallpaperMapTerrain.DeepWater
                    : LiveWallpaperMapTerrain.Water;
        }

        private static void TryAddAnimatedTile(
            List<LiveWallpaperMapAnimatedTile> animatedTiles,
            string template, int positionX, int positionY)
        {
            var animation = template switch
            {
                "wave1" => ("water_0", 8, 125),
                "wave2" => ("water_2", 8, 125),
                "wave3" => ("wave_3", 8, 125),
                "wave4" => ("wave_4", 8, 125),
                "wave5" => ("wave_5", 8, 125),
                "wave6" => ("wave_6", 8, 125),
                "pondWoods" => ("water_1", 8, 125),
                "water1" => ("water_3", 8, 125),
                "water2" => ("water_4", 8, 150),
                "water3" => ("water_5", 8, 150),
                "water4" => ("water_6", 8, 150),
                "water5" => ("water_7", 8, 150),
                "waterFall" => ("water_8", 4, 100),
                "waterLeft" => ("water_left", 4, 100),
                "waterUp" => ("water_up", 4, 100),
                "waterRight" => ("water_right", 4, 100),
                "waterDown" => ("water_down", 4, 100),
                "flower" => ("flower_0", 4, 250),
                "flowerforest" => ("flower_1", 4, 250),
                "flowerforest2" => ("flower_2", 4, 250),
                "flower2" => ("flower_3", 4, 250),
                "flower3" => ("flower_4", 4, 250),
                "flower4" => ("flower_5", 4, 250),
                "flowerswamp" => ("flower_6", 4, 250),
                "sand1" => ("sand_0", 4, 175),
                "sand2" => ("sand_1", 4, 175),
                "sand3" => ("sand_2", 4, 175),
                _ => ((string)null, 0, 0)
            };
            if (animation.Item1 == null)
                return;
            animatedTiles.Add(new LiveWallpaperMapAnimatedTile(
                animation.Item1, positionX, positionY,
                animation.Item2, animation.Item3));
        }

        private static void TryAddMapEnemy(
            List<LiveWallpaperMapEnemy> enemies,
            string template,
            string[] parts,
            int positionX,
            int positionY)
        {
            var enemyTemplate = template == "enemy_respawner" && parts.Length > 3
                ? parts[3]?.Trim()
                : template;
            var kind = enemyTemplate switch
            {
                "e1" => LiveWallpaperMapEnemyKind.SeaUrchin,
                "e2" => LiveWallpaperMapEnemyKind.Octorok,
                "e3" => LiveWallpaperMapEnemyKind.Leever,
                "e4" => LiveWallpaperMapEnemyKind.Crab,
                "e5" => LiveWallpaperMapEnemyKind.Moblin,
                "moblinSword" => LiveWallpaperMapEnemyKind.MoblinSword,
                "e15" => LiveWallpaperMapEnemyKind.RedZol,
                "e18" => LiveWallpaperMapEnemyKind.RiverZora,
                "e21" => LiveWallpaperMapEnemyKind.Ghini,
                "e23" => LiveWallpaperMapEnemyKind.Pincer,
                _ => (LiveWallpaperMapEnemyKind?)null
            };
            if (!kind.HasValue)
                return;

            var body = kind.Value switch
            {
                LiveWallpaperMapEnemyKind.SeaUrchin => (16, -8, -16, 16, 16),
                LiveWallpaperMapEnemyKind.Octorok => (12, -7, -12, 14, 12),
                LiveWallpaperMapEnemyKind.Leever => (16, -7, -12, 14, 12),
                LiveWallpaperMapEnemyKind.Crab => (16, -7, -10, 14, 10),
                LiveWallpaperMapEnemyKind.Moblin => (16, -6, -10, 12, 10),
                LiveWallpaperMapEnemyKind.MoblinSword => (16, -7, -14, 14, 14),
                LiveWallpaperMapEnemyKind.RedZol => (13, -6, -10, 12, 10),
                LiveWallpaperMapEnemyKind.RiverZora => (6, -6, -5, 12, 10),
                LiveWallpaperMapEnemyKind.Ghini => (16, -6, -12, 12, 12),
                _ => (8, -6, -6, 12, 12)
            };
            var entityX = positionX + 8;
            var entityY = positionY + body.Item1;
            enemies.Add(new LiveWallpaperMapEnemy(
                kind.Value, positionX, positionY, entityX, entityY,
                entityX + body.Item2, entityY + body.Item3,
                body.Item4, body.Item5));
        }

        private static void TryAddMapDecoration(
            List<LiveWallpaperMapDecoration> decorations,
            string template,
            string[] parts,
            int positionX,
            int positionY)
        {
            // These values are the sprite ids, entity offsets and layers declared by
            // GameObjectTemplates for the same installed-map objects.
            if (template == "aquaticPlant")
            {
                decorations.Add(new LiveWallpaperMapDecoration(
                    "aquatic_plant_top", positionX + 1, positionY,
                    playerLayer: false, topLeft: true));
                decorations.Add(new LiveWallpaperMapDecoration(
                    "aquatic_plant_bottom", positionX + 7, positionY + 10,
                    playerLayer: false, topLeft: true));
                return;
            }
            if (template == "alligator")
            {
                // ObjAlligator.SpawnBanana places the default banana bunch at
                // (EntityX - 8, EntityY + 20), before the trade is complete.
                decorations.Add(new LiveWallpaperMapDecoration(
                    "bananas", positionX, positionY + 36));
                return;
            }
            if (template is "armosStatue" or "armosDarkStatue")
            {
                decorations.Add(new LiveWallpaperMapDecoration(
                    template == "armosStatue" ? "armos" : "armos dark",
                    positionX + 8, positionY + 14,
                    atlasName: "enemies"));
                return;
            }
            if (template == "dungeonSixEntry")
            {
                decorations.Add(new LiveWallpaperMapDecoration(
                    "dungeonSixEntry", positionX, positionY,
                    playerLayer: false, topLeft: true,
                    atlasName: "objects animated"));
                return;
            }
            if (template is "stone" or "stoneWoods" or "stoneSkull" or
                "pot" or "pot2" or "pot2D" or "d6Statue")
            {
                var entity = GameObjectVisualLayout.GetStoneEntityPosition(
                    positionX, positionY);
                var spriteId = template switch
                {
                    "stone" => "stone_0",
                    "stoneWoods" => "stone_1",
                    "stoneSkull" => "skull",
                    "pot" => "pot_0",
                    "pot2" => "pot_1",
                    "pot2D" => "pot_2",
                    _ => "d6_statue"
                };
                decorations.Add(new LiveWallpaperMapDecoration(
                    spriteId,
                    (int)entity.X, (int)entity.Y,
                    playerLayer: true, stoneLayout: true));
                return;
            }
            if (template is "moveStone" or "moveStoneCave" or
                "moveStoneFrogHouse" or "moveStoneD3")
            {
                // ObjMoveStone anchors its entity at (posX, posY + 16) and
                // DrawSpriteComponent applies (0, -sprite height), placing the
                // canonical 16x16 block exactly on its map cell.
                var spriteId = template switch
                {
                    "moveStone" => "movestone_0",
                    "moveStoneCave" => "movestone_1",
                    "moveStoneFrogHouse" => "movestone_2",
                    _ => "movestone_3"
                };
                decorations.Add(new LiveWallpaperMapDecoration(
                    spriteId, positionX, positionY,
                    playerLayer: false, topLeft: true));
                return;
            }
            if (template is "caveCrystal" or "crystalD4" or "hardCrystal")
            {
                // ObjCrystal draws at EntityPosition(pos + 8, posY + 16)
                // with (-8, -16), so the atlas sprite is cell-aligned.
                var spriteId = template switch
                {
                    "caveCrystal" => "crystal_0",
                    "crystalD4" => "crystal_1",
                    _ => "crystal_hard"
                };
                decorations.Add(new LiveWallpaperMapDecoration(
                    spriteId, positionX, positionY,
                    playerLayer: true, topLeft: true));
                return;
            }
            if (template == "chest")
            {
                // ObjChest is two player-layer sprites at the same canonical
                // entity depth. The back uses CSprite's -12.9 draw offset,
                // which pixel-snaps to -13, while chest_front keeps its atlas
                // origin at entity y + 13.
                var sourceOffsetX = Math.Max(0,
                    GetOptionalInt(parts, 6, 0)) * 32;
                decorations.Add(new LiveWallpaperMapDecoration(
                    "chest_back", positionX, positionY + 13,
                    playerLayer: true, topLeft: true, drawOffsetY: -13,
                    sourceOffsetX: sourceOffsetX));
                decorations.Add(new LiveWallpaperMapDecoration(
                    "chest_front", positionX, positionY + 13,
                    playerLayer: true, sourceOffsetX: sourceOffsetX));
                return;
            }
            var topLeftSprite = template switch
            {
                "castleDoor" => ("castle_door", true),
                "dungeonEntrance" => ("dungeon_entrance", false),
                _ => ((string)null, false)
            };
            if (topLeftSprite.Item1 != null)
            {
                decorations.Add(new LiveWallpaperMapDecoration(
                    topLeftSprite.Item1, positionX, positionY,
                    topLeftSprite.Item2, topLeft: true));
                return;
            }
            var sprite = template switch
            {
                "tree0" => ("tree_0", 16, 24, true),
                "treeWoods" => ("tree_7", 16, 24, true),
                "treeWoods2" => ("tree_6", 16, 24, true),
                "tree1" => ("tree_1", 16, 24, true),
                "tree2" => ("tree_2", 16, 24, true),
                "tree3" => ("tree_3", 16, 24, true),
                "tree4" => ("tree_4", 16, 24, true),
                "tree5" => ("tree_5", 16, 24, true),
                "tree9" => ("tree_9", 16, 24, true),
                "stree" => ("tree_8", 8, 24, true),
                "phonehouse" => ("tree_phonehouse", 24, 24, true),
                "seashell_house" => ("seashell_house", 24, 24, true),
                "strandplant" => ("strandPlant", 8, 12, true),
                "strandshell" => ("strandShell", 8, 12, true),
                "gravejardfence" => ("gravejardFence", 7, 12, true),
                "desertpillar" => ("desertPillar", 8, 28, true),
                "armosStatue" => ("armos", 8, 14, true),
                "armosDarkStatue" => ("armos dark", 8, 14, true),
                "statueD3" => ("statue_d3", 8, 30, true),
                "mountainStone" => ("stone_mountain_0", 8, 13, true),
                "dungeonStatue" => ("dungeonStatue_0", 8, 13, true),
                "dungeonStatueGrey" => ("dungeonStatue_1", 8, 13, true),
                "dungeon3Head" => ("dungeon3Head", 8, 12, true),
                "dungeon7_keyhole" => ("dungeon7_keyhole", 8, 14, true),
                "overworldDonut" => ("overworldDonut", 8, 0, true),
                "owl_statue" => ("owl_statue", 0, 12, true),
                "mermaid_statue" => ("mermaid_statue", 8, 28, true),
                // ObjBush anchors every bush and grass sprite at the centre of
                // its 16x16 map cell, not at the cell's lower edge.
                "bush" => ("bush_0", 8, 8, true),
                "bushForest" => ("bush_1", 8, 8, true),
                "gras" => ("grass_0", 8, 8, false),
                "gras0" => ("grass_0_0", 8, 8, false),
                "gras1" => ("grass_0_1", 8, 8, false),
                "gras2" => ("grass_0_2", 8, 8, false),
                "gras3" => ("grass_0_3", 8, 8, false),
                "grasForest" => ("grass_1", 8, 8, false),
                "grasSwamp" => ("grass_2", 8, 8, false),
                "gravestone" => ("gravestone", 8, 16, true),
                "cactus" => ("cactus", 8, 16, true),
                "signpost" => ("signpost_0", 8, 16, true),
                "signpostWoods" => ("signpost_1", 8, 16, true),
                "castle_roof_0" => ("castle_roof_0", 0, 17, true),
                "castle_roof_1" => ("castle_roof_1", 0, 17, true),
                "castle_roof_2" => ("castle_roof_2", 0, 17, true),
                "castle_roof_3" => ("castle_roof_3", 0, 0, true),
                "castle_roof_4" => ("castle_roof_4", 0, 16, true),
                "castle_roof_5" => ("castle_roof_5", 0, 16, true),
                "roof01" => ("roof_0", 0, 18, true),
                "roof02" => ("roof_1", 0, 18, true),
                "roof03" => ("roof_2", 0, 18, true),
                "roof04" => ("roof_3", 0, 16, true),
                "roof05" => ("roof_4", 0, 18, true),
                "roof06" => ("roof_5", 0, 18, true),
                "d5_entry" => ("d5_entry", 0, 0, true),
                "witch_house" => ("witch_house", 0, 30, true),
                // ObjPainting anchors the installed painting sprite at
                // CPosition(posX + 8, posY + 16).
                "painting" => ("painting", 8, 16, true),
                "itemShop" => ("itemShop", 8, 14, true),
                _ => ((string)null, 0, 0, true)
            };
            if (sprite.Item1 == null)
            {
                TryAddFenceDecorations(
                    decorations, template, positionX, positionY);
                return;
            }
            decorations.Add(new LiveWallpaperMapDecoration(
                sprite.Item1, positionX + sprite.Item2, positionY + sprite.Item3,
                sprite.Item4));
        }

        private static void TryAddFenceDecorations(
            List<LiveWallpaperMapDecoration> decorations,
            string template, int positionX, int positionY)
        {
            var placement = template switch
            {
                "fence" => 15,
                "fenceUL" => 14,
                "fenceU" => 12,
                "fenceUR" => 13,
                "fenceL" => 10,
                "fenceR" => 5,
                "fenceDL" => 11,
                "fenceD" => 3,
                "fenceDR" => 7,
                "fenceTR" => 4,
                "fenceTL" => 8,
                "fenceBR" => 1,
                "fenceBL" => 2,
                _ => 0
            };
            for (var index = 0; index < 4; index++)
            {
                if ((placement & 0x08) > 1)
                {
                    decorations.Add(new LiveWallpaperMapDecoration(
                        "fence",
                        positionX + 4 + index % 2 * 8,
                        positionY + 5 + index / 2 * 8));
                }
                placement <<= 1;
            }
        }

        private static void TryAddMapActor(
            List<LiveWallpaperMapActor> actors,
            string template,
            string[] parts,
            int positionX,
            int positionY)
        {
            var kind = template switch
            {
                "personNew" => LiveWallpaperMapActorKind.Person,
                "person" => LiveWallpaperMapActorKind.LegacyPerson,
                "dogo" => LiveWallpaperMapActorKind.Dog,
                "grandmother" => LiveWallpaperMapActorKind.Grandmother,
                "raccoon" => LiveWallpaperMapActorKind.Raccoon,
                "weatherBird" => LiveWallpaperMapActorKind.WeatherBird,
                "owl" => LiveWallpaperMapActorKind.Owl,
                "butterfly" => LiveWallpaperMapActorKind.Butterfly,
                "bird" => LiveWallpaperMapActorKind.Bird,
                "BowWow" => LiveWallpaperMapActorKind.BowWow,
                "frog" => LiveWallpaperMapActorKind.Frog,
                "mouse" => LiveWallpaperMapActorKind.Mouse,
                "bobWowSmall" => LiveWallpaperMapActorKind.BowWowSmall,
                "alligator" => LiveWallpaperMapActorKind.Alligator,
                "chickenDude" => LiveWallpaperMapActorKind.ChickenDude,
                "hippo" => LiveWallpaperMapActorKind.Hippo,
                "painter" => LiveWallpaperMapActorKind.Painter,
                "tracy" => LiveWallpaperMapActorKind.Tracy,
                "letterBoy" => LiveWallpaperMapActorKind.LetterBoy,
                "letterGirl" => LiveWallpaperMapActorKind.LetterGirl,
                "letterBird" or "letterBirdGreen" =>
                    LiveWallpaperMapActorKind.LetterBird,
                "photoMouse" => LiveWallpaperMapActorKind.PhotoMouse,
                "fisherman" => LiveWallpaperMapActorKind.Fisherman,
                "mermaid" => LiveWallpaperMapActorKind.Mermaid,
                "fairy" => LiveWallpaperMapActorKind.Fairy,
                _ => (LiveWallpaperMapActorKind?)null
            };
            if (!kind.HasValue)
                return;

            // ObjTracy deliberately hides the table duplicate at entity (72,64).
            if (kind == LiveWallpaperMapActorKind.Tracy &&
                positionX + 8 == 72 && positionY + 16 == 64)
                return;
            // ObjPhotoMouse starts inactive when a spawn condition is supplied.
            // The wallpaper does not run or read gameplay saves, so retain that
            // canonical default instead of fabricating a scripted sequence state.
            if (kind == LiveWallpaperMapActorKind.PhotoMouse &&
                !string.IsNullOrWhiteSpace(GetOptionalString(parts, 3)))
                return;

            string animationId = null;
            string animationName = null;
            if (kind == LiveWallpaperMapActorKind.Person)
            {
                animationId = parts.Length > 4 ? parts[4]?.Trim() : null;
                animationName = parts.Length > 6 ? parts[6]?.Trim() : null;
                if (string.IsNullOrWhiteSpace(animationId) ||
                    !LiveWallpaperAnimation.TryNormalizeRelativePath(
                        "NPCs/" + animationId + ".ani", out _))
                    return;
            }
            else if (kind == LiveWallpaperMapActorKind.LegacyPerson)
            {
                animationId = GetOptionalString(parts, 3);
                animationName = GetOptionalString(parts, 6);
                if (string.IsNullOrWhiteSpace(animationId) ||
                    !LiveWallpaperAnimation.TryNormalizeRelativePath(
                        "NPCs/" + animationId + ".ani", out _))
                    return;
            }
            else if (kind == LiveWallpaperMapActorKind.LetterBird)
            {
                animationId = template == "letterBirdGreen"
                    ? "letterBirdGreen"
                    : "letterBird";
            }
            var legacyBody = kind == LiveWallpaperMapActorKind.LegacyPerson
                ? GetOptionalDottedRectangle(parts, 4,
                    new LocalRectangle(0, 0, 14, 10))
                : default;
            var body = kind.Value switch
            {
                LiveWallpaperMapActorKind.Person =>
                    new LocalRectangle(positionX + 1, positionY + 6, 14, 10),
                LiveWallpaperMapActorKind.LegacyPerson =>
                    new LocalRectangle(
                        positionX + 8 + legacyBody.X - legacyBody.Width / 2,
                        positionY + 16 + legacyBody.Y - legacyBody.Height,
                        legacyBody.Width, legacyBody.Height),
                LiveWallpaperMapActorKind.Dog =>
                    new LocalRectangle(positionX + 2, positionY + 8, 12, 8),
                LiveWallpaperMapActorKind.Grandmother =>
                    new LocalRectangle(positionX + 1, positionY + 4, 14, 12),
                LiveWallpaperMapActorKind.Raccoon =>
                    new LocalRectangle(positionX + 1, positionY + 6, 14, 10),
                LiveWallpaperMapActorKind.WeatherBird =>
                    new LocalRectangle(positionX + 1, positionY + 20, 14, 12),
                LiveWallpaperMapActorKind.Owl =>
                    new LocalRectangle(positionX + 2, positionY + 8, 12, 8),
                LiveWallpaperMapActorKind.Bird =>
                    new LocalRectangle(positionX + 2, positionY + 8, 12, 8),
                LiveWallpaperMapActorKind.BowWow =>
                    new LocalRectangle(positionX - 7, positionY + 6, 14, 10),
                LiveWallpaperMapActorKind.Frog =>
                    new LocalRectangle(positionX + 2, positionY + 8, 12, 8),
                LiveWallpaperMapActorKind.Mouse =>
                    new LocalRectangle(positionX + 3, positionY + 4, 10, 8),
                LiveWallpaperMapActorKind.BowWowSmall =>
                    new LocalRectangle(positionX + 3, positionY + 8, 10, 8),
                LiveWallpaperMapActorKind.Alligator =>
                    new LocalRectangle(positionX - 4, positionY, 20, 16),
                LiveWallpaperMapActorKind.ChickenDude =>
                    new LocalRectangle(positionX + 3, positionY + 6, 10, 10),
                LiveWallpaperMapActorKind.Hippo or
                LiveWallpaperMapActorKind.Painter =>
                    new LocalRectangle(positionX - 1, positionY + 3, 18, 12),
                LiveWallpaperMapActorKind.Tracy =>
                    new LocalRectangle(positionX, positionY + 5, 15, 11),
                LiveWallpaperMapActorKind.LetterBoy or
                LiveWallpaperMapActorKind.LetterGirl =>
                    new LocalRectangle(positionX, positionY + 3, 16, 12),
                LiveWallpaperMapActorKind.LetterBird =>
                    new LocalRectangle(positionX + 2, positionY + 8, 12, 8),
                LiveWallpaperMapActorKind.PhotoMouse =>
                    new LocalRectangle(positionX + 1, positionY + 3, 14, 12),
                LiveWallpaperMapActorKind.Fisherman =>
                    new LocalRectangle(positionX, positionY + 5, 15, 11),
                LiveWallpaperMapActorKind.Mermaid =>
                    new LocalRectangle(positionX + 1, positionY + 6, 14, 10),
                LiveWallpaperMapActorKind.Fairy =>
                    new LocalRectangle(positionX + 3, positionY, 10, 16),
                _ => new LocalRectangle(0, 0, 0, 0)
            };
            var spriteOffset = kind == LiveWallpaperMapActorKind.LegacyPerson
                ? GetOptionalDottedPoint(parts, 5)
                : (X: 0, Y: 0);
            var owlTrigger = kind == LiveWallpaperMapActorKind.Owl
                ? GetOptionalDottedRectangle(
                    parts, 4, new LocalRectangle(-16, 32, 48, 32))
                : default;
            var owlMode = kind == LiveWallpaperMapActorKind.Owl
                ? GetOptionalInt(parts, 7, 0)
                : 0;
            var owlHoverMode = kind == LiveWallpaperMapActorKind.Owl &&
                               GetOptionalBool(parts, 5, false);
            actors.Add(new LiveWallpaperMapActor(
                kind.Value, positionX, positionY, animationId, animationName,
                body.X, body.Y, body.Width, body.Height,
                spriteOffset.X, spriteOffset.Y,
                positionX + 8 + owlTrigger.X,
                positionY + 8 + owlTrigger.Y,
                owlTrigger.Width, owlTrigger.Height,
                owlMode, owlHoverMode));
        }

        private static LocalRectangle GetOptionalDottedRectangle(
            IReadOnlyList<string> parts, int index, LocalRectangle fallback)
        {
            if (parts == null || index < 0 || index >= parts.Count ||
                string.IsNullOrWhiteSpace(parts[index]))
                return fallback;
            var values = parts[index].Split('.');
            if (values.Length != 4 ||
                !TryParseInt(values[0], out var x) ||
                !TryParseInt(values[1], out var y) ||
                !TryParseInt(values[2], out var width) ||
                !TryParseInt(values[3], out var height) ||
                width <= 0 || height <= 0)
                return fallback;
            return new LocalRectangle(x, y, width, height);
        }

        private static (int X, int Y) GetOptionalDottedPoint(
            IReadOnlyList<string> parts, int index)
        {
            if (parts == null || index < 0 || index >= parts.Count ||
                string.IsNullOrWhiteSpace(parts[index]))
                return (0, 0);
            var values = parts[index].Split('.');
            return values.Length == 2 &&
                   TryParseInt(values[0], out var x) &&
                   TryParseInt(values[1], out var y)
                ? (x, y)
                : (0, 0);
        }

        private static void TryAddMapPortal(
            List<LiveWallpaperMapPortal> portals,
            string template,
            string[] parts,
            int positionX,
            int positionY)
        {
            if (template == "holeTeleporter")
            {
                portals.Add(new LiveWallpaperMapPortal(
                    positionX, positionY, 16, 16, 0, 0,
                    nextMap: GetOptionalString(parts, 3),
                    exitId: GetOptionalString(parts, 4),
                    isHoleTeleporter: true));
                return;
            }
            if (template is not ("door" or "door2d"))
                return;
            var width = GetOptionalPositiveInt(parts, 3, 16);
            var height = GetOptionalPositiveInt(parts, 4, 16);
            var is2dDoor = template == "door2d";
            var direction = is2dDoor
                ? 1
                : Math.Clamp(GetOptionalInt(parts, 8, 0), 0, 3);
            var mode = is2dDoor ? 0 : Math.Max(0, GetOptionalInt(parts, 9, 0));
            portals.Add(new LiveWallpaperMapPortal(
                positionX, positionY, width, height, direction, mode,
                GetOptionalString(parts, 5), GetOptionalString(parts, 6),
                GetOptionalString(parts, 7), is2dDoor));
        }

        private static string GetOptionalString(
            IReadOnlyList<string> parts, int index)
        {
            if (parts == null || index < 0 || index >= parts.Count)
                return null;
            var value = parts[index]?.Trim();
            return string.IsNullOrWhiteSpace(value) ||
                   string.Equals(value, "null", StringComparison.OrdinalIgnoreCase)
                ? null
                : value;
        }

        private static void TryAddHookshotTarget(
            List<LiveWallpaperMapHookshotTarget> targets,
            string template,
            int positionX,
            int positionY)
        {
            // These are the exact Values.CollisionTypes.Hookshot rectangles from
            // GameObjectTemplates. Hookshot-only grips do not block Link's body.
            var rectangle = template switch
            {
                "hookshotGrip" => new LocalRectangle(0, 0, 16, 16),
                "mountainStone" => new LocalRectangle(0, 2, 16, 12),
                "overworldDonut" => new LocalRectangle(0, 0, 16, 16),
                _ => new LocalRectangle(0, 0, 0, 0)
            };
            if (rectangle.Width <= 0 || rectangle.Height <= 0)
                return;
            targets.Add(new LiveWallpaperMapHookshotTarget(
                positionX + rectangle.X,
                positionY + rectangle.Y,
                rectangle.Width,
                rectangle.Height));
        }

        private static void AddObjectCollision(
            List<CollisionRectangle>[,] grid,
            int mapWidth,
            int mapHeight,
            string template,
            string[] parts,
            int positionX,
            int positionY,
            ref int collisionCount,
            ref int hazardCount,
            ref int npcWallCount)
        {
            if (template is "dungeonLadder" or "dungeonLadderTop")
            {
                var top = template == "dungeonLadderTop";
                var bounds = SideViewGameplayMotion.LadderBounds(positionX, positionY, top);
                AddCollision(grid, mapWidth, mapHeight,
                    new CollisionRectangle(bounds.X, bounds.Y, bounds.Width, bounds.Height,
                        top ? CollisionKind.LadderTop : CollisionKind.Ladder),
                    ref collisionCount, ref hazardCount, ref npcWallCount);
                return;
            }

            if (template is "hole" or "visiblehole" or "fullHole")
            {
                var fullTile = template == "fullHole";
                var width = GetOptionalPositiveInt(parts, 3, fullTile ? 16 : 14);
                var height = GetOptionalPositiveInt(parts, 4, fullTile ? 16 : 14);
                var offsetX = GetOptionalInt(parts, 6, fullTile ? 0 : 1);
                var offsetY = GetOptionalInt(parts, 7, fullTile ? 0 : 1);
                AddCollision(grid, mapWidth, mapHeight,
                    new CollisionRectangle(positionX + offsetX, positionY + offsetY,
                        width, height, CollisionKind.Hole), ref collisionCount,
                    ref hazardCount, ref npcWallCount);
                return;
            }

            if (template is "caveBreakingFloor" or "caveBreakingFloor2" or
                "caveBreakingFloor3" or "dungeonHole" or
                "breakingFloorCastle" or "dungeon5BreakingFloor" or
                "dungeon2BreakingFloor" or "dungeon8BreakingFloor" or
                "breakingFloorHouse")
            {
                // ObjBreakingFloor owns an ObjHole with this exact rectangle.
                // The lightweight map displays the underlying open-pit tile,
                // so it must expose the owned hole instead of treating that
                // visible pit as ordinary ground.
                AddCollision(grid, mapWidth, mapHeight,
                    new CollisionRectangle(
                        positionX, positionY + 1, 16, 14, CollisionKind.Hole),
                    ref collisionCount, ref hazardCount, ref npcWallCount);
                return;
            }

            if (template == "enemyWall")
            {
                AddCollision(grid, mapWidth, mapHeight,
                    new CollisionRectangle(
                        positionX, positionY, 16, 16, CollisionKind.NpcWall),
                    ref collisionCount, ref hazardCount, ref npcWallCount);
                return;
            }

            if (TryAddFenceCollision(
                    grid, mapWidth, mapHeight, template, parts,
                    positionX, positionY, ref collisionCount, ref hazardCount,
                    ref npcWallCount))
                return;

            LocalRectangle[] rectangles = template switch
            {
                "c1" or "lowCollider16" or "lowerLevelCollider" or "c1PushIgnore" =>
                    [new LocalRectangle(0, 0, 16, 16)],
                "c2" or "lowCollider0" or "lowerLevelCollider1" =>
                    [new LocalRectangle(0, 8, 16, 8)],
                "c5" or "lowCollider1" =>
                    [new LocalRectangle(0, 0, 16, 8)],
                "c3" or "lowCollider2" or "lowerLevelCollider2" =>
                    [new LocalRectangle(0, 0, 8, 16)],
                "c4" or "lowCollider3" =>
                    [new LocalRectangle(8, 0, 8, 16)],
                "c13" => [new LocalRectangle(0, 0, 8, 8)],
                "c6" => [new LocalRectangle(8, 0, 8, 8)],
                "c7" => [new LocalRectangle(0, 8, 8, 8)],
                "c8" => [new LocalRectangle(8, 8, 8, 8)],
                "colliderL0" or "c9" =>
                    [new LocalRectangle(0, 8, 8, 8), new LocalRectangle(0, 0, 16, 8)],
                "colliderL1" or "c10" =>
                    [new LocalRectangle(8, 8, 8, 8), new LocalRectangle(0, 0, 16, 8)],
                "colliderL2" or "c11" =>
                    [new LocalRectangle(0, 0, 8, 8), new LocalRectangle(0, 8, 16, 8)],
                "colliderL3" or "c12" =>
                    [new LocalRectangle(8, 0, 8, 8), new LocalRectangle(0, 8, 16, 8)],
                "blockDoor_Seg1" => [new LocalRectangle(0, 8, 7, 8)],
                "blockDoor_Seg2" => [new LocalRectangle(7, 9, 1, 7)],
                "blockDoor_Seg3" => [new LocalRectangle(8, 10, 8, 6)],
                "oneWayBridge2" => [new LocalRectangle(15, 0, 1, 16)],
                "oneWayBridge0" => [new LocalRectangle(0, 0, 1, 16)],
                "oneWayFlatTop" => [new LocalRectangle(0, 0, 16, 1)],
                "oneWayFlatTop-14" => [new LocalRectangle(1, 0, 14, 1)],
                "tree0" or "treeWoods" or "tree1" or "tree2" or "tree3" or
                    "tree4" or "tree5" or "tree9" =>
                    [new LocalRectangle(0, 4, 32, 28)],
                "treeWoods2" => [new LocalRectangle(0, 4, 32, 27)],
                "stree" => [new LocalRectangle(0, 16, 16, 16)],
                "phonehouse" or "seashell_house" =>
                    [new LocalRectangle(0, 4, 48, 12)],
                // ObjAlligator's spawned banana ObjSprite is repositioned to
                // (posX, posY + 36) and keeps its (-8,-14,16,14) collision.
                "alligator" => [new LocalRectangle(-8, 22, 16, 14)],
                "strandplant" or "gravejardfence" =>
                    [new LocalRectangle(0, 4, 15, 12)],
                "bush" or "bushForest" =>
                    [new LocalRectangle(0, 1, 16, 14)],
                "stone" or "stoneWoods" or "stoneSkull" or "pot" or "pot2" or
                    "pot2D" or "d6Statue" =>
                    [new LocalRectangle(0, 1, 16, 13)],
                // ObjCrystal uses a 14x14 soft body; the hard crystal uses its
                // separate full-width Pegasus-smash collision.
                "caveCrystal" or "crystalD4" =>
                    [new LocalRectangle(1, 2, 14, 14)],
                "hardCrystal" => [new LocalRectangle(0, 4, 16, 12)],
                // ObjSprite collision rectangles from GameObjectTemplates.
                "dungeonStatue" or "dungeonStatueGrey" =>
                    [new LocalRectangle(0, 3, 16, 13)],
                "dungeon3Head" => [new LocalRectangle(0, 4, 16, 12)],
                "signpost" or "signpostWoods" =>
                    [new LocalRectangle(0, 4, 16, 12)],
                "painting" => [new LocalRectangle(0, 4, 16, 12)],
                // GameObjectTemplates.itemShop constructs ObjSprite at
                // (posX + 8, posY + 14) with collision (-8,-10,16,12).
                "itemShop" => [new LocalRectangle(0, 4, 16, 12)],
                // ObjShopkeeper uses entity (posX + 8, posY + 16) and a
                // BodyCollisionComponent backed by body (-7,-10,14,10).
                "shopkeeper" => [new LocalRectangle(1, 6, 14, 10)],
                // ObjChest's BoxCollisionComponent uses
                // CBox(posX, posY + 3, 0, 16, 11, 12).
                "chest" => [new LocalRectangle(0, 3, 16, 11)],
                // GameObjectTemplates.lamp constructs ObjLamp with hasCollision=true;
                // its CBox is the full 16x16 placement cell.
                "lamp" => [new LocalRectangle(0, 0, 16, 16)],
                "gravestone" => [new LocalRectangle(0, 4, 16, 12)],
                "moveStone" or "moveStoneCave" or "moveStoneFrogHouse" or "moveStoneD3" =>
                    [new LocalRectangle(0, 0, 16, 16)],
                "mountainStone" => [new LocalRectangle(0, 2, 16, 12)],
                // GameObjectTemplates.overworldDonut: entity (+8, 0),
                // collider (-8, 0, 16, 16).
                "overworldDonut" => [new LocalRectangle(0, 0, 16, 16)],
                "cactus" => [new LocalRectangle(3, 3, 10, 12)],
                _ => null
            };
            if (rectangles == null)
                return;
            foreach (var rectangle in rectangles)
            {
                var kind = template is "bush" or "bushForest"
                    ? CollisionKind.Bush
                    : template is "stone" or "stoneWoods" or "stoneSkull" or
                        "pot" or "pot2" or "pot2D" or "d6Statue"
                        ? CollisionKind.Stone
                        : template is "moveStone" or "moveStoneCave" or
                            "moveStoneFrogHouse" or "moveStoneD3"
                            ? CollisionKind.MoveStone
                        : CollisionKind.Normal;
                AddCollision(grid, mapWidth, mapHeight,
                    new CollisionRectangle(
                        positionX + rectangle.X, positionY + rectangle.Y,
                        rectangle.Width, rectangle.Height, kind,
                        template == "oneWayBridge0" ? 0 : template == "oneWayBridge2" ? 2 :
                        template is "oneWayFlatTop" or "oneWayFlatTop-14" ? 3 : -1),
                    ref collisionCount, ref hazardCount, ref npcWallCount);
            }
        }

        private static bool TryAddFenceCollision(
            List<CollisionRectangle>[,] grid,
            int mapWidth,
            int mapHeight,
            string template,
            string[] parts,
            int positionX,
            int positionY,
            ref int collisionCount,
            ref int hazardCount,
            ref int npcWallCount)
        {
            var defaultPlacement = template switch
            {
                "fence" => 15,
                "fenceUL" => 14,
                "fenceU" => 12,
                "fenceUR" => 13,
                "fenceL" => 10,
                "fenceR" => 5,
                "fenceDL" => 11,
                "fenceD" => 3,
                "fenceDR" => 7,
                "fenceTR" => 4,
                "fenceTL" => 8,
                "fenceBR" => 1,
                "fenceBL" => 2,
                _ => -1
            };
            if (defaultPlacement < 0)
                return false;
            var placement = Math.Clamp(
                GetOptionalInt(parts, 3, defaultPlacement), 0, 15);
            for (var index = 0; index < 4; index++)
            {
                if ((placement & (1 << (3 - index))) == 0)
                    continue;
                AddCollision(grid, mapWidth, mapHeight,
                    new CollisionRectangle(
                        positionX + 1 + index % 2 * 8,
                        positionY + index / 2 * 8,
                        6, 6, CollisionKind.Normal),
                    ref collisionCount, ref hazardCount, ref npcWallCount);
            }
            return true;
        }

        private static void AddCollision(
            List<CollisionRectangle>[,] grid,
            int mapWidth,
            int mapHeight,
            CollisionRectangle rectangle,
            ref int collisionCount,
            ref int hazardCount,
            ref int npcWallCount)
        {
            if (rectangle.Width <= 0 || rectangle.Height <= 0 ||
                rectangle.X + rectangle.Width <= 0 || rectangle.Y + rectangle.Height <= 0 ||
                rectangle.X >= mapWidth * TileSize || rectangle.Y >= mapHeight * TileSize)
                return;
            var startX = Math.Clamp(rectangle.X / TileSize, 0, mapWidth - 1);
            var startY = Math.Clamp(rectangle.Y / TileSize, 0, mapHeight - 1);
            var endX = Math.Clamp((rectangle.X + rectangle.Width - 1) / TileSize,
                0, mapWidth - 1);
            var endY = Math.Clamp((rectangle.Y + rectangle.Height - 1) / TileSize,
                0, mapHeight - 1);
            for (var y = startY; y <= endY; y++)
            {
                for (var x = startX; x <= endX; x++)
                {
                    grid[x, y] ??= [];
                    grid[x, y].Add(rectangle);
                }
            }
            switch (rectangle.Kind)
            {
                case CollisionKind.Hole:
                    hazardCount++;
                    break;
                case CollisionKind.NpcWall:
                    npcWallCount++;
                    break;
                default:
                    collisionCount++;
                    break;
            }
        }

        private bool IntersectsKind(
            float x, float y, float width, float height, CollisionKind kind)
        {
            if (width <= 0 || height <= 0 || _collisionGrid == null)
                return false;
            if (x < 0 || y < 0 || x + width > Width * TileSize ||
                y + height > Height * TileSize)
                return true;
            var startX = Math.Clamp((int)MathF.Floor(x / TileSize), 0, Width - 1);
            var startY = Math.Clamp((int)MathF.Floor(y / TileSize), 0, Height - 1);
            var endX = Math.Clamp((int)MathF.Floor((x + width - 0.001f) / TileSize),
                0, Width - 1);
            var endY = Math.Clamp((int)MathF.Floor((y + height - 0.001f) / TileSize),
                0, Height - 1);
            for (var tileY = startY; tileY <= endY; tileY++)
            {
                for (var tileX = startX; tileX <= endX; tileX++)
                {
                    var entries = _collisionGrid[tileX, tileY];
                    if (entries == null)
                        continue;
                    foreach (var entry in entries)
                    {
                        if (entry.Kind == kind && entry.Intersects(x, y, width, height))
                            return true;
                    }
                }
            }
            return false;
        }

        private static int GetOptionalPositiveInt(
            string[] parts, int index, int fallback)
        {
            var value = GetOptionalInt(parts, index, fallback);
            return value is > 0 and <= 512 ? value : fallback;
        }

        private static int GetOptionalInt(string[] parts, int index, int fallback) =>
            index < parts.Length && TryParseInt(parts[index], out var value)
                ? value
                : fallback;

        private static bool GetOptionalBool(
            string[] parts, int index, bool fallback) =>
            index < parts.Length && bool.TryParse(parts[index], out var value)
                ? value
                : fallback;

        private static bool TryParseInt(string value, out int result) =>
            int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture,
                out result);

        private readonly struct LocalRectangle
        {
            public LocalRectangle(int x, int y, int width, int height)
            {
                X = x;
                Y = y;
                Width = width;
                Height = height;
            }

            public int X { get; }
            public int Y { get; }
            public int Width { get; }
            public int Height { get; }
        }

        private readonly struct CollisionRectangle
        {
            public CollisionRectangle(
                int x, int y, int width, int height, CollisionKind kind,
                int direction = -1)
            {
                X = x;
                Y = y;
                Width = width;
                Height = height;
                Kind = kind;
                Direction = direction;
            }

            public int X { get; }
            public int Y { get; }
            public int Width { get; }
            public int Height { get; }
            public CollisionKind Kind { get; }
            public int Direction { get; }

            public bool Intersects(float x, float y, float width, float height) =>
                x < X + Width && x + width > X &&
                y < Y + Height && y + height > Y;
        }

        private enum CollisionKind
        {
            Normal,
            Bush,
            Stone,
            MoveStone,
            Hole,
            NpcWall,
            Ladder,
            LadderTop
        }

        private static bool TryReadInt(TextReader reader, out int value) =>
            int.TryParse(reader.ReadLine(), NumberStyles.Integer,
                CultureInfo.InvariantCulture, out value);
    }
}
