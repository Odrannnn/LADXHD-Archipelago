using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects;
using ProjectZ.InGame.GameObjects.Dungeon;
using ProjectZ.InGame.GameObjects.Things;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ
{
    public readonly record struct LiveWallpaperMapShadow(
        string SpriteId, float EntityX, float EntityY, bool StoneLayout,
        int BushKey = -1, int StoneKey = -1, float OffsetY = 0f);

    public readonly record struct LiveWallpaperSceneLight(
        string SpriteId, float X, float Y, int Size, Color Color,
        int Layer, int Rotation = 0);

    public sealed class LiveWallpaperSceneEffects
    {
        // Only inert metadata is needed. Asset-backed rectangles for unrelated
        // collision/animated-tile templates are not read or used by this class.
        private static readonly Lazy<Dictionary<string, GameObjectTemplate>> Definitions =
            new(() => GameObjectTemplates.CreateDefinitions(_ => Rectangle.Empty));
        private int _shadowGridWidth;
        private int _shadowGridHeight;
        private int[][] _shadowGrid = [];

        public bool UseShadows { get; private set; } = true;
        public float ShadowHeight { get; private set; } = Values.ShadowHeightDefault;
        public float ShadowRotation { get; private set; } = Values.ShadowRotationDefault;
        public bool UseLighting { get; private set; }
        public Color Ambient { get; private set; } = Color.White;
        public List<LiveWallpaperMapShadow> Shadows { get; } = new();
        public List<LiveWallpaperSceneLight> Lights { get; } = new();

        public ReadOnlySpan<int> GetShadowIndicesAt(int tileX, int tileY)
        {
            if (tileX < 0 || tileX >= _shadowGridWidth ||
                tileY < 0 || tileY >= _shadowGridHeight ||
                _shadowGrid.Length == 0)
                return ReadOnlySpan<int>.Empty;
            var cell = _shadowGrid[tileY * _shadowGridWidth + tileX];
            return cell == null ? ReadOnlySpan<int>.Empty : cell;
        }

        internal static bool TryResolve(LiveWallpaperMapObject mapObject,
            out GameObjectTemplate definition, out object[] parameters)
        {
            parameters = null;
            if (!Definitions.Value.TryGetValue(mapObject.Template, out definition) ||
                definition == null)
                return false;
            parameters = (object[])definition.Parameter.Clone();
            for (var i = 0; i < parameters.Length && i < mapObject.Arguments.Count; i++)
            {
                var value = mapObject.Arguments[i];
                if (string.IsNullOrEmpty(value))
                    continue;
                var type = parameters[i]?.GetType() ?? typeof(string);
                if (type == typeof(int) || type == typeof(float) ||
                    type == typeof(bool) || type == typeof(string))
                    parameters[i] = MapData.ConvertToObject(value, type);
            }
            return true;
        }

        public static LiveWallpaperSceneEffects Create(LiveWallpaperMap map)
        {
            var scene = new LiveWallpaperSceneEffects();
            foreach (var obj in map.Objects)
            {
                if (!TryResolve(obj, out var definition, out var p))
                    continue;
                var type = definition.ObjectType;
                var x = obj.PixelX;
                var y = obj.PixelY;
                if (type == typeof(ObjShadowDisabler))
                    scene.UseShadows = false;
                else if (type == typeof(ObjShadowSetter))
                {
                    if (float.IsFinite((float)p[0]) && float.IsFinite((float)p[1]))
                    {
                        scene.ShadowHeight = (float)p[0];
                        scene.ShadowRotation = (float)p[1];
                    }
                }
                else if (type == typeof(ObjDungeonBlacker))
                {
                    scene.UseLighting = true;
                    scene.Ambient = GameSceneEffects.AmbientLight(
                        (int)p[0], (int)p[1], (int)p[2], (int)p[3]);
                }
                else if (type == typeof(ObjLight))
                    scene.Lights.Add(new(null, x + 8, y + 8, (int)p[0],
                        GameSceneEffects.AmbientLight((int)p[1], (int)p[2], (int)p[3], (int)p[4]),
                        (int)p[5]));
                else if (type == typeof(ObjLightSprite) && p[0] is string lightId)
                    scene.Lights.Add(new(lightId, x, y, 0,
                        GameSceneEffects.AmbientLight((int)p[1], (int)p[2], (int)p[3], (int)p[4]),
                        (int)p[5], (int)p[6]));
                else if (type == typeof(ObjLamp))
                {
                    // No save state is consulted. An unkeyed ordinary lamp is
                    // lit; powder/key-controlled lamps are not silently lit.
                    if ((bool)p[5] && !(bool)p[3] && string.IsNullOrEmpty(p[4] as string))
                        scene.Lights.Add(new(null, x + 8, y + 16,
                            GameSceneEffects.LampSize,
                            new Color(GameSceneEffects.LampRed, GameSceneEffects.LampGreen,
                                GameSceneEffects.LampBlue), 0));
                }
                else if (type == typeof(ObjSprite) && p.Length >= 4 &&
                    p[1] is Vector2 offset && p[3] is string shadowId &&
                    !string.IsNullOrEmpty(shadowId))
                    scene.Shadows.Add(new(shadowId, x + offset.X, y + offset.Y, false));
                else if (type == typeof(ObjBush) && (bool)p[3] && (bool)p[2])
                    scene.Shadows.Add(new((string)p[1], x + 8, y + 8, false,
                        BushKey: map.GetBushKey(x, y), OffsetY: -1));
                else if (type == typeof(ObjStone))
                {
                    var entity = GameObjectVisualLayout.GetStoneEntityPosition(x, y);
                    scene.Shadows.Add(new((string)p[0], entity.X, entity.Y, true,
                        StoneKey: map.GetStoneKey(x, y + 1), OffsetY: -1));
                }
            }
            var sortedLights = scene.Lights.OrderBy(light => light.Layer).ToArray();
            scene.Lights.Clear();
            scene.Lights.AddRange(sortedLights);
            scene.BuildShadowGrid(map.Width, map.Height);
            return scene;
        }

        private void BuildShadowGrid(int width, int height)
        {
            _shadowGridWidth = width;
            _shadowGridHeight = height;
            var cells = new List<int>[checked(width * height)];
            for (var index = 0; index < Shadows.Count; index++)
            {
                var shadow = Shadows[index];
                var tileX = Math.Clamp(
                    (int)MathF.Floor(shadow.EntityX / 16f), 0, width - 1);
                var tileY = Math.Clamp(
                    (int)MathF.Floor(shadow.EntityY / 16f), 0, height - 1);
                var cellIndex = tileY * width + tileX;
                (cells[cellIndex] ??= new List<int>()).Add(index);
            }
            _shadowGrid = new int[cells.Length][];
            for (var index = 0; index < cells.Length; index++)
                if (cells[index] != null)
                    _shadowGrid[index] = cells[index].ToArray();
        }
    }
}
