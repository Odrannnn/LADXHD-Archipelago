using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using ProjectZ.Base;
using ProjectZ.Base.UI;
using ProjectZ.InGame.GameObjects;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.Pages;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Screens;
using ProjectZ.InGame.Things;

namespace ProjectZ.Editor
{
    public class EditorManager : IEditorManager
    {
        private string _lastGameScreen = Values.ScreenNameGame;
        private string _lastEditorScreen = Values.ScreenNameEditor;

        private int _currentFrameTimeIndex;
        private double[] _debugFrameTimes =
        {
            1000 / 30.0,
            1000 / 60.0,
            1000 / 90.0,
            1000 / 120.0,
            1000 / 144.0,
            1000 / 288.0,
            1
        };

        private Game1 _game;

        public EditorManager(Game1 game)
        {
            _game = game;
        }

        public void OffsetObjects(Map map, int offsetX, int offsetY)
        {
            ObjectEditorScreen.OffsetObjects(map, offsetX, offsetY);
        }

        public void RegisterEditorScreens(List<Screen> screens)
        {
            screens.Add(new MapEditorScreen(Values.ScreenNameEditor));
            screens.Add(new TilesetEdit(Values.ScreenNameEditorTileset));
            screens.Add(new TileExtractor(Values.ScreenNameEditorTilesetExtractor));
            screens.Add(new AnimationScreen(Values.ScreenNameEditorAnimation));
            screens.Add(new SpriteAtlasScreen(Values.ScreenNameSpriteAtlasEditor));
        }

        public void PopulateEditorObjectTemplates(Dictionary<string, GameObjectTemplate> templates, Func<object[], Map, int, int, object[]> addPositionFunc)
        {
            var editorMap = Map.CreateEmptyMap();

            foreach (var template in templates)
            {
                if (template.Value == null)
                    continue;

                if (template.Value.ObjectType.GetConstructor(Type.EmptyTypes) != null)
                {
                    ObjectEditorScreen.EditorObjectTemplates.Add(
                        template.Key,
                        (GameObject)Activator.CreateInstance(template.Value.ObjectType));
                }
                else
                {
                    ObjectEditorScreen.EditorObjectTemplates.Add(
                        template.Key,
                        ObjectManager.GetGameObject(
                            editorMap,
                            template.Key,
                            addPositionFunc(template.Value.Parameter, editorMap, 0, 0)
                        )
                    );
                }
            }
        }

        public void EditorUpdate(GameTime gameTime)
        {
            if (InputHandler.KeyPressed(Keys.N))
                Game1.DebugStepper = !Game1.DebugStepper;
            if (Game1.ScreenManager.CurrentScreenId != Values.ScreenNameGame)
                Game1.DebugStepper = false;

            if (Game1.DebugStepper && InputHandler.KeyPressed(Keys.M))
            {
                Game1.TimeMultiplier = _game.TargetElapsedTime.Ticks / 166667f;
                Game1.DeltaTime = (float)_game.TargetElapsedTime.TotalMilliseconds;

                Game1.TotalGameTimeLast = Game1.TotalTime;
                Game1.TotalTime += _game.TargetElapsedTime.Milliseconds;
                Game1.TotalGameTime += _game.TargetElapsedTime.Milliseconds;
            }
            if (InputHandler.KeyPressed(Keys.Q))
                Game1.GameManager.MapManager.ReloadMap();
            if (InputHandler.KeyPressed(Keys.Add))
                Game1.DebugTimeScale += 0.125f;
            if (InputHandler.KeyPressed(Keys.Subtract) && Game1.DebugTimeScale > 0)
                Game1.DebugTimeScale -= 0.125f;

            if (InputHandler.KeyPressed(Game1.DebugShadowKey))
                GameSettings.EnableShadows = !GameSettings.EnableShadows;

            if (Game1.ScreenManager.CurrentScreenId != Values.ScreenNameEditor &&
                Game1.ScreenManager.CurrentScreenId != Values.ScreenNameEditorTileset &&
                Game1.ScreenManager.CurrentScreenId != Values.ScreenNameEditorTilesetExtractor &&
                Game1.ScreenManager.CurrentScreenId != Values.ScreenNameEditorAnimation &&
                Game1.ScreenManager.CurrentScreenId != Values.ScreenNameSpriteAtlasEditor)
            {
                if (InputHandler.KeyPressed(Keys.D0))
                    _game.TriggerFpsSettings();

                if (InputHandler.KeyPressed(Keys.D1))
                {
                    _currentFrameTimeIndex--;
                    if (_currentFrameTimeIndex < 0)
                        _currentFrameTimeIndex = _debugFrameTimes.Length - 1;
                    _game.TargetElapsedTime = new TimeSpan((long)Math.Ceiling(_debugFrameTimes[_currentFrameTimeIndex] * 10000));
                }
                if (InputHandler.KeyPressed(Keys.D2))
                {
                    _currentFrameTimeIndex = (_currentFrameTimeIndex + 1) % _debugFrameTimes.Length;
                    _game.TargetElapsedTime = new TimeSpan((long)Math.Ceiling(_debugFrameTimes[_currentFrameTimeIndex] * 10000));
                }
            }

            if (InputHandler.KeyPressed(Keys.Escape))
            {
                if (Game1.ScreenManager.CurrentScreenId != Values.ScreenNameEditor &&
                    Game1.ScreenManager.CurrentScreenId != Values.ScreenNameEditorTileset &&
                    Game1.ScreenManager.CurrentScreenId != Values.ScreenNameEditorTilesetExtractor &&
                    Game1.ScreenManager.CurrentScreenId != Values.ScreenNameEditorAnimation &&
                    Game1.ScreenManager.CurrentScreenId != Values.ScreenNameSpriteAtlasEditor)
                {
                    InputHandler.EnableTextInput();
                    Game1.UiPageManager.PopAllPages(PageManager.TransitionAnimation.TopToBottom, PageManager.TransitionAnimation.TopToBottom);
                    _lastGameScreen = Game1.ScreenManager.CurrentScreenId;
                    Game1.ScreenManager.ChangeScreen(_lastEditorScreen);
                    Game1.EditorOpen = true;
                }
                else
                {
                    InputHandler.DisableTextInput();
                    _lastEditorScreen = Game1.ScreenManager.CurrentScreenId;
                    Game1.ScreenManager.ChangeScreen(_lastGameScreen);

                    var editorScreen = (MapEditorScreen)Game1.ScreenManager.GetScreen(Values.ScreenNameEditor);

                    if (_lastEditorScreen == Values.ScreenNameEditor)
                        MapManager.ObjLink.SetPosition(new Vector2(editorScreen.MousePixelPosition.X, editorScreen.MousePixelPosition.Y));
                    Game1.EditorOpen = false;
                }
            }

            if (InputHandler.KeyPressed(Game1.DebugToggleMode))
                Game1.DebugMode = !Game1.DebugMode;

            if (InputHandler.KeyPressed(Game1.DebugBox))
                Game1.DebugBoxMode = (Game1.DebugBoxMode + 1) % 6;

            if (InputHandler.KeyPressed(Game1.DebugSaveKey))
            {
                MapManager.ObjLink.SaveMap = Game1.GameManager.MapManager.CurrentMap.MapName;
                MapManager.ObjLink.SavePosition = MapManager.ObjLink.Position;
                MapManager.ObjLink.SaveDirection = MapManager.ObjLink.Direction;

                SaveGameSaveLoad.SaveGame(Game1.GameManager, true);
            }
            if (InputHandler.KeyPressed(Game1.DebugLoadKey))
                Game1.GameManager.LoadSaveFile(Game1.GameManager.SaveSlot);
        }

        public void SetUpEditorUi()
        {
            var strScreen = $"{Values.EditorUiObjectEditor}:" +
                            $"{Values.EditorUiObjectSelection}:" +
                            $"{Values.EditorUiTileEditor}:" +
                            $"{Values.EditorUiTileSelection}:" +
                            $"{Values.EditorUiDigTileEditor}:" +
                            $"{Values.EditorUiMusicTileEditor}:" +
                            $"{Values.EditorUiTileExtractor}:" +
                            $"{Values.EditorUiTilesetEditor}:" +
                            $"{Values.EditorUiAnimation}:" +
                            $"{Values.EditorUiSpriteAtlas}";

            Game1.UiManager.AddElement(new UiRectangle(new Rectangle(0, 0, Game1.WindowWidth, Values.ToolBarHeight),
                "top", strScreen, Values.ColorBackgroundDark, Color.White,
                ui => { ui.Rectangle = new Rectangle(0, 0, Game1.WindowWidth, Values.ToolBarHeight); }));

            var pos = 0;
            Game1.UiManager.AddElement(new UiButton(new Rectangle(0, 0, 200, Values.ToolBarHeight), Resources.EditorFont,
                "Editor", "bt1", strScreen,
                ui => { ((UiButton)ui).Marked = Game1.ScreenManager.CurrentScreenId == Values.ScreenNameEditor; },
                element => { Game1.ScreenManager.ChangeScreen(Values.ScreenNameEditor); }));

            Game1.UiManager.AddElement(new UiButton(new Rectangle(pos += 205, 0, 200, Values.ToolBarHeight), Resources.EditorFont,
                "Tileset Editor", "bt1", strScreen,
                ui => { ((UiButton)ui).Marked = Game1.ScreenManager.CurrentScreenId == Values.ScreenNameEditorTileset; },
                element => { Game1.ScreenManager.ChangeScreen(Values.ScreenNameEditorTileset); }));

            Game1.UiManager.AddElement(new UiButton(new Rectangle(pos += 205, 0, 200, Values.ToolBarHeight), Resources.EditorFont,
                "Tileset Extractor", "bt1", strScreen,
                ui => { ((UiButton)ui).Marked = Game1.ScreenManager.CurrentScreenId == Values.ScreenNameEditorTilesetExtractor; },
                element => { Game1.ScreenManager.ChangeScreen(Values.ScreenNameEditorTilesetExtractor); }));

            Game1.UiManager.AddElement(new UiButton(new Rectangle(pos += 205, 0, 200, Values.ToolBarHeight), Resources.EditorFont,
                "Animation Editor", "bt1", strScreen,
                ui => { ((UiButton)ui).Marked = Game1.ScreenManager.CurrentScreenId == Values.ScreenNameEditorAnimation; },
                element => { Game1.ScreenManager.ChangeScreen(Values.ScreenNameEditorAnimation); }));

            Game1.UiManager.AddElement(new UiButton(new Rectangle(pos += 205, 0, 200, Values.ToolBarHeight), Resources.EditorFont,
                "Sprite Atlas Editor", "bt1", strScreen,
                ui => { ((UiButton)ui).Marked = Game1.ScreenManager.CurrentScreenId == Values.ScreenNameSpriteAtlasEditor; },
                element => { Game1.ScreenManager.ChangeScreen(Values.ScreenNameSpriteAtlasEditor); }));
        }
    }
}
