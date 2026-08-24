using System.Collections.Generic;
using Microsoft.Xna.Framework;
using ProjectZ.InGame.Controls;
using ProjectZ.InGame.Interface;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.Pages
{
    class GameMenuPage : InterfacePage
    {
        private readonly InterfaceButton _warpToStartButton;
        private readonly InterfaceButton _magpieTrackerButton;
        private readonly InterfaceListLayout _contentLayout;
        private readonly InterfaceListLayout _footerLayout;
        private readonly int _footerHeight;

        public GameMenuPage(int width, int height)
        {
            // main layout
            var mainLayout = new InterfaceListLayout() { Size = new Point(width, height), Selectable = true };
            mainLayout.AddElement(new InterfaceLabel(Resources.GameHeaderFont, "game_menu_header", 
                new Point(150, (int)(height * Values.MenuHeaderSize)), new Point(0, 0)) 
                { TextColor = InterfaceElement.MainTextColor });

            // Size = new Point(width, (int)(height * Values.MenuContentSize))
            _contentLayout = new InterfaceListLayout { AutoSize = true, Selectable = true };
            _contentLayout.AddElement(new InterfaceButton(new Point(150, 25), Point.Zero, "game_menu_back_to_game", e => ClosePage()) { Margin = new Point(0, 2) });
            _contentLayout.AddElement(new InterfaceButton(new Point(150, 25), Point.Zero, "game_menu_save_continue", OnClickSaveContinue) { Margin = new Point(0, 2) });
            _warpToStartButton = new InterfaceButton(new Point(150, 25), Point.Zero, "", OnClickWarpToStart)
            {
                Margin = new Point(0, 2),
                Visible = false,
                Hidden = true,
                Selectable = false
            };
            _warpToStartButton.InsideLabel.OverrideText = "Warp to Start";
            _contentLayout.AddElement(_warpToStartButton);
            _magpieTrackerButton = new InterfaceButton(new Point(150, 25), Point.Zero, "", OnClickMagpieTracker)
            {
                Margin = new Point(0, 2),
                Visible = false,
                Hidden = true,
                Selectable = false
            };
            _magpieTrackerButton.InsideLabel.OverrideText = "Magpie Tracker";
            _contentLayout.AddElement(_magpieTrackerButton);
            _contentLayout.AddElement(new InterfaceButton(new Point(150, 25), Point.Zero, "game_menu_settings", OnClickSettings) { Margin = new Point(0, 2) });
            _contentLayout.AddElement(new InterfaceButton(new Point(150, 25), Point.Zero, "game_menu_exit_to_the_menu", OnClickBackToMenu) { Margin = new Point(0, 2) });
            _contentLayout.AddElement(new InterfaceButton(new Point(150, 25), Point.Zero, "game_menu_exit_the_game", OnClickExitGame) { Margin = new Point(0, 2) });

            mainLayout.AddElement(_contentLayout);
            _footerHeight = (int)(height * Values.MenuFooterSize);
            _footerLayout = new InterfaceListLayout { Size = new Point(width, _footerHeight) };
            mainLayout.AddElement(_footerLayout);

            PageLayout = mainLayout;
            PageLayout.Select(InterfaceElement.Directions.Top, false);
        }

        public override void OnLoad(Dictionary<string, object> intent)
        {
            Game1.AudioManager.PauseMusic();

            // This escape hatch is part of the randomizer experience. Keep the vanilla game
            // menu unchanged when the loaded save is not bound to Archipelago.
            var showWarpToStart = Game1.GameManager.ArchipelagoManager.IsActive;
            _warpToStartButton.Visible = showWarpToStart;
            _warpToStartButton.Hidden = !showWarpToStart;
            _warpToStartButton.Selectable = showWarpToStart;

            var showMagpieTracker = Game1.GameManager.ArchipelagoManager.CanShowEmbeddedTracker;
            _magpieTrackerButton.Visible = showMagpieTracker;
            _magpieTrackerButton.Hidden = !showMagpieTracker;
            _magpieTrackerButton.Selectable = showMagpieTracker;

            // The Android pause menu has one more AP command than the other compact menus.
            // Tighten only this button list while the embedded tracker is available.
            foreach (var child in _contentLayout.Elements)
            {
                if (child is not InterfaceButton button)
                    continue;
                button.Size = new Point(button.Size.X, showMagpieTracker ? 22 : 25);
                button.Margin = new Point(button.Margin.X, showMagpieTracker ? 1 : 2);
                button.ChangeUp = true;
            }
            _contentLayout.ChangeUp = true;

            // Six full-size menu buttons plus the normal decorative footer exceed the compact
            // Android menu height. Collapse only that empty footer while the AP command is shown.
            _footerLayout.Size = new Point(_footerLayout.Size.X, showWarpToStart ? 0 : _footerHeight);
            _footerLayout.ChangeUp = true;

            // select the "Back to Game" button
            PageLayout.Deselect(false);
            PageLayout.Select(InterfaceElement.Directions.Top, false);
        }

        public override void OnPop(Dictionary<string, object> intent)
        {
            Game1.AudioManager.ResumeMusic();
        }

        public override void Update(CButtons pressedButtons, GameTime gameTime)
        {
            base.Update(pressedButtons, gameTime);
            if (ControlHandler.ButtonPressed(CButtons.Start) || ControlHandler.ButtonPressed(ControlHandler.CancelButton))
                ClosePage();
        }

        private void ClosePage()
        {
            MapManager.ObjLink.DisableItems = true;
            MapManager.ObjLink.DisableItemCounter = 350;
            Game1.GameManager.InGameOverlay.CloseOverlay();
        }

        public void OnClickSaveContinue(InterfaceElement element)
        {
            MapManager.ObjLink.DisableItems = true;
            MapManager.ObjLink.DisableItemCounter = 350;
            SettingsSaveLoad.SaveSettings();
            SaveGameSaveLoad.SaveGame(Game1.GameManager, true);
            AchievementManager.Save();
            Game1.GameManager.InGameOverlay.CloseOverlay();
        }

        public void OnClickSettings(InterfaceElement element)
        {
            Game1.UiPageManager.ChangePage(typeof(SettingsPage));
        }

        public void OnClickWarpToStart(InterfaceElement element)
        {
            // Match the initial post-intro save point inside Marin and Tarin's house. The
            // randomizer may otherwise strand a player whose available progression cannot
            // return them to the normal overworld route.
            var link = MapManager.ObjLink;
            link.SaveMap = "house1.map";
            link.SavePosition = new Vector2(70, 70);
            link.SaveDirection = 3;

            if (Game1.GameManager.SaveManager.HistoryEnabled)
            {
                Game1.GameManager.SaveManager.RevertHistory();
                Game1.GameManager.SaveManager.DisableHistory();
            }

            SettingsSaveLoad.SaveSettings();
            SaveGameSaveLoad.SaveGame(Game1.GameManager, false);
            AchievementManager.Save();

            Game1.InProgress = false;
            MapManager.CameraOffset = Vector2.Zero;
            Game1.ScreenManager.ChangeScreen(Values.ScreenNameMenu);
        }

        public void OnClickMagpieTracker(InterfaceElement element)
        {
            // Remove the pause menu before adding Android's in-activity tracker panel so the
            // game loop and music continue behind it.
            ClosePage();
            Game1.GameManager.ArchipelagoManager.ShowEmbeddedTracker();
        }

        public void OnClickBackToMenu(InterfaceElement element)
        {
            Game1.UiPageManager.ChangePage(typeof(QuitGamePage));
        }

        public void OnClickExitGame(InterfaceElement element)
        {
            Game1.SaveAndExitGame = true;
            Game1.UiPageManager.ChangePage(typeof(ExitGamePage));
        }
    }
}
