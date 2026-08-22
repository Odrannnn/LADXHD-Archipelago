using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.Controls;
using ProjectZ.InGame.Interface;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.Pages
{
    class VideoSettingsPage : InterfacePage
    {
        private readonly InterfaceListLayout _videoSettingsLayout;
        private readonly InterfaceListLayout _contentLayout;
        private readonly InterfaceListLayout _bottomBar;
        private readonly InterfaceSlider     _sliderGameScale;
        private readonly InterfaceSlider     _sliderUIScale;
        private readonly InterfaceSlider     _sliderFullscreen;
        private readonly InterfaceListLayout _toggleVerticalSync;
        private readonly InterfaceListLayout _toggleOpaqueHudBg;
        private readonly InterfaceListLayout _togglePixelSnapping;
        private readonly InterfaceListLayout _togglePixelGrid;
        private readonly InterfaceListLayout _toggleEpilepsySafe;

        List<string> _tooltips = new List<string>();
        private bool _showTooltip;

        public void SetGameScaleValue(int value) { ((InterfaceSlider)_sliderGameScale).CurrentStep = value; }
        public void SetUserInterfaceScale(int value) { ((InterfaceSlider)_sliderUIScale).CurrentStep = value; }
        public void SetFullscreenMode(int value) { ((InterfaceSlider)_sliderFullscreen).CurrentStep = value; ((InterfaceSlider)_sliderFullscreen).UpdateLanguageText(); }
        public void SetVerticalSync(bool state) { ((InterfaceToggle)_toggleVerticalSync.Elements[1]).ToggleState = state; Game1.FpsSettingChanged = true; }
        public void SetOpaqueHudBg(bool state) => ((InterfaceToggle)_toggleOpaqueHudBg.Elements[1]).ToggleState = state;
        public void SetPixelSnapping(bool state) { ((InterfaceToggle)_togglePixelSnapping.Elements[1]).ToggleState = state; }
        public void SetPixelGrid(bool state) { ((InterfaceToggle)_togglePixelGrid.Elements[1]).ToggleState = state; }
        public void SetEpilepsySafe(bool state) => ((InterfaceToggle)_toggleEpilepsySafe.Elements[1]).ToggleState = state;

        public VideoSettingsPage(int width, int height)
        {
            EnableTooltips = true;
            var buttonWidth = 320;
            var buttonHeight = 13;
            var sliderHeight = 11;

            // Graphics Settings Layout
            _videoSettingsLayout = new InterfaceListLayout { Size = new Point(width, height - 12), Selectable = true };
            _videoSettingsLayout.AddElement(new InterfaceLabel(Resources.GameHeaderFont, "settings_video_header",
                new Point(buttonWidth, (int)(height * Values.MenuHeaderSize)), new Point(0, 0)));
            _contentLayout = new InterfaceListLayout { Size = new Point(width, (int)(height * Values.MenuContentSize) - 12), Selectable = true, ContentAlignment = InterfaceElement.Gravities.Top };

            // Slider: Game Scale
            _sliderGameScale = new InterfaceSlider("settings_video_game_scale",
                buttonWidth, sliderHeight, new Point(1, 2), -3, Game1.MaxGameScale + 1, 1, GameSettings.GameScale, 
                number => { GameSettings.GameScale = number; Game1.ScaleChanged = true; })
                { SetString = number => GameScaleSliderAdjustmentString(number) };
            _contentLayout.AddElement(_sliderGameScale);
            _tooltips.Add("tooltip_video_game_scale");

            // Slider: UI Scale
            _sliderUIScale = new InterfaceSlider("settings_video_ui_scale",
                buttonWidth, sliderHeight, new Point(1, 2), 1, 11, 1, GameSettings.UiScale-1,
                number => { GameSettings.UiScale = number; Game1.ScaleChanged = true; })
                { SetString = (number) => UIScaleSliderAdjustmentString(number) };
            _contentLayout.AddElement(_sliderUIScale);
            _tooltips.Add("tooltip_video_ui_scale");

            if (Game1.PlatformWindow.SupportsFullscreenConfiguration)
            {
                // Slider: Screen Mode
                _sliderFullscreen = new InterfaceSlider("settings_video_fullscreen",
                    buttonWidth, sliderHeight, new Point(1, 2), 0, 2, 1, GameSettings.ScreenMode,
                    number => { FullscreenSliderAdjustment(number); })
                    { SetString = number => FullscreenSliderText(number) };
                _contentLayout.AddElement(_sliderFullscreen);
                _tooltips.Add("tooltip_video_fullscreen");
            }

            // Toggle: Vertical Sync
            _toggleVerticalSync = InterfaceToggle.GetToggleButton(
                new Point(buttonWidth, buttonHeight), new Point(5, 2),
                "settings_video_fps_lock", GameSettings.VerticalSync,
                newState => { GameSettings.VerticalSync = newState; Game1.FpsSettingChanged = true; });
            _contentLayout.AddElement(_toggleVerticalSync);
            _tooltips.Add("tooltip_video_fps_lock");

            // Toggle: Disable UI Blur
            _toggleOpaqueHudBg = InterfaceToggle.GetToggleButton(
                new Point(buttonWidth, buttonHeight), new Point(5, 2),
                "settings_video_opaquehud", GameSettings.OpaqueHudBg,
                newState => { GameSettings.OpaqueHudBg = newState; });
            _contentLayout.AddElement(_toggleOpaqueHudBg);
            _tooltips.Add("tooltip_video_opaquehud");

            // Toggle: Pixel Snapping
            _togglePixelSnapping = InterfaceToggle.GetToggleButton(
                new Point(buttonWidth, buttonHeight), new Point(5, 2),
                "settings_video_pixelsnap", GameSettings.PixelSnapping,
                newState => { GameSettings.PixelSnapping = newState; _togglePixelGrid.ToggleElementColors(GameSettings.PixelSnapping); });
            _contentLayout.AddElement(_togglePixelSnapping);
            _tooltips.Add("tooltip_video_pixelsnap");

            // Toggle: Pixel Grid
            _togglePixelGrid = InterfaceToggle.GetToggleButton(
                new Point(buttonWidth, buttonHeight), new Point(5, 2),
                "settings_video_pixelgrid", GameSettings.PixelGrid,
                newState => { GameSettings.PixelGrid = newState; });
            _contentLayout.AddElement(_togglePixelGrid);
            _tooltips.Add("tooltip_video_pixelgrid");

            // Toggle: Epilepsy Safe
            _toggleEpilepsySafe = InterfaceToggle.GetToggleButton(
                new Point(buttonWidth, buttonHeight), new Point(5, 2),
                "settings_video_epilepsysafe", GameSettings.EpilepsySafe,
                newState => { GameSettings.EpilepsySafe = newState; });
            _contentLayout.AddElement(_toggleEpilepsySafe);
            _tooltips.Add("tooltip_video_epilepsysafe");

            // Bottom Bar / Back Button:
            _bottomBar = new InterfaceListLayout { Size = new Point(width, (int)(height * Values.MenuFooterSize)), Selectable = true, HorizontalMode = true };
            _bottomBar.AddElement(new InterfaceButton(new Point(100, 18), new Point(2, 4), "settings_menu_back", element => { Game1.UiPageManager.PopPage(); }));
            _videoSettingsLayout.AddElement(_contentLayout);
            _videoSettingsLayout.AddElement(_bottomBar);
            PageLayout = _videoSettingsLayout;

            // Set the initial state of the pixel grid.
            _togglePixelGrid.ToggleElementColors(GameSettings.PixelSnapping);
        }

        public override void OnLoad(Dictionary<string, object> intent)
        {
            // the left button is always the first one selected
            _bottomBar.Deselect(false);
            _bottomBar.Select(InterfaceElement.Directions.Left, false);
            _bottomBar.Deselect(false);

            PageLayout.Deselect(false);
            PageLayout.Select(InterfaceElement.Directions.Top, false);
        }

        public override void Update(CButtons pressedButtons, GameTime gameTime)
        {
            base.Update(pressedButtons, gameTime);

            UpdateGameScaleSlider();

            // The back button was pressed.
            if (ControlHandler.ButtonPressed(ControlHandler.CancelButton))
                Game1.UiPageManager.PopPage();

            // The tooltip button was pressed.
            if (ControlHandler.ButtonPressed(CButtons.Y))
            {
                _showTooltip = !_showTooltip;
                if (_showTooltip)
                    Game1.AudioManager.PlaySoundEffect("D360-21-15");
            }
            // Hide the tooltip when pressing anything.
            else if (ControlHandler.AnyButtonPressed())
                _showTooltip = false;
        }

        public override void OnResize(int newWidth, int newHeight)  {  }

        private string GameScaleSliderAdjustmentString(int number)
        {
            // Get the maximum scale and add 1 for auto-scale.
            int maxScale = Game1.MaxGameScale + 1;

            // Translate values below 1x and when autoscale is set.
            return number == maxScale
                ? " " + Game1.LanguageManager.GetString("settings_video_autodetect", "error")
                : number switch
                {
                     0 => " 50%",
                    -1 => " 33%",
                    -2 => " 25%",
                    -3 => " 20%",
                    _  => " " + number + "x"
                };
        }

        private string UIScaleSliderAdjustmentString(int number)
        {
            if (number == 11)
                return " " + Game1.LanguageManager.GetString("settings_video_autodetect", "error");
            return " " + number + "x";
        }

        private string FullscreenSliderText(int number)
        {
            return " " + Game1.LanguageManager.GetString("settings_video_fullscreen_0" + (number + 1).ToString(), "error");
        }

        private void FullscreenSliderAdjustment(int number)
        {
            GameSettings.ScreenMode = number;
            Game1.ToggleFullscreen();
            Game1.ScaleChanged = true;
        }

        private void UpdateGameScaleSlider()
        {
            // The step starts at 0 and ends at max. Add the amount it goes negative.
            _sliderGameScale.CurrentStep = GameSettings.GameScale + 3;
        }

        public override void Draw(SpriteBatch spriteBatch, Vector2 position, float height, float alpha)
        {
            // Always draw the menu even when not showing tooltips.
            base.Draw(spriteBatch, position, height, alpha);

            // If the user pressed the top most face button, show the tooltip window.
            if (_showTooltip)
            {
                string tooltipText = PageTooltip.GetTooltipIndex(_videoSettingsLayout, _contentLayout, _tooltips);
                PageTooltip.Draw(spriteBatch, tooltipText);
            }
        }
    }
}
