using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.Controls;
using ProjectZ.InGame.Interface;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.Pages
{
    class CameraSettingsPage : InterfacePage
    {
        private readonly InterfaceListLayout _cameraOptionsList;
        private readonly InterfaceListLayout _contentLayout;
        private readonly InterfaceListLayout _bottomBar;
        private readonly InterfaceSlider     _sliderCameraType;
        private readonly InterfaceSlider     _sliderCameraBorder;
        private readonly InterfaceSlider     _sliderBorderOpacity;
        private readonly InterfaceSlider     _sliderBorderBias;
        private readonly InterfaceListLayout _toggleClassicScaling;
        private readonly InterfaceListLayout _toggleCameraLock;
        private readonly InterfaceListLayout _toggleCameraSmooth;

        List<string> _tooltips = new List<string>();
        private bool _showTooltip;

        public void SetCameraType(int value) { ((InterfaceSlider)_sliderCameraType).CurrentStep = value; }
        public void SetClassicCamBorder(int value) { ((InterfaceSlider)_sliderCameraBorder).CurrentStep = value; }
        public void SetClassicBorderAlpha(int value) { ((InterfaceSlider)_sliderBorderOpacity).CurrentStep = value; }
        public void SetClassicBorderBias(int value) { ((InterfaceSlider)_sliderBorderBias).CurrentStep = value; }
        public void SetClassicScaleLock(bool state) => ((InterfaceToggle)_toggleClassicScaling.Elements[1]).ToggleState = state;
        public void SetCameraLock(bool state) => ((InterfaceToggle)_toggleCameraLock.Elements[1]).ToggleState = state; 
        public void SetCameraSmoothCam(bool state) => ((InterfaceToggle)_toggleCameraSmooth.Elements[1]).ToggleState = state;

        public CameraSettingsPage(int width, int height)
        {
            EnableTooltips = true;
            var buttonWidth = 320;
            var buttonHeight = 12;
            var sliderHeight = 10;

            // Camera Settings Layout
            _cameraOptionsList = new InterfaceListLayout { Size = new Point(width, height - 12), Selectable = true };
            _cameraOptionsList.AddElement(new InterfaceLabel(Resources.GameHeaderFont, "settings_camera_header",
                new Point(buttonWidth, (int)(height * Values.MenuHeaderSize)), new Point(0, 0)));
            _contentLayout = new InterfaceListLayout { Size = new Point(width, (int)(height * Values.MenuContentSize) - 12), Selectable = true, ContentAlignment = InterfaceElement.Gravities.Top };

            // Slider: Camera Type
            _sliderCameraType = new InterfaceSlider("settings_camera_cameratype",
                buttonWidth, sliderHeight, new Point(1, 2), 0, 2, 1, GameSettings.CameraMode, 
                number => { GameSettings.CameraMode = number; Game1.ScaleChanged = true; Camera.SnapCameraTimer = 20f; }) 
                { SetString = number => SetCameraSetting(number) };
            _contentLayout.AddElement(_sliderCameraType);
            _tooltips.Add("tooltip_camera_cameratype");

            // Button: Custom Camera Settings
            _contentLayout.AddElement(new InterfaceButton(
                new Point(buttonWidth, buttonHeight), new Point(1, 2), 
                "settings_camera_custommenu", element => { Game1.UiPageManager.ChangePage(typeof(CameraCustomPage)); }));
            _tooltips.Add("tooltip_camera_custommenu");

            // Slider: Classic Camera Border
            _sliderCameraBorder = new InterfaceSlider("settings_camera_camborder",
                buttonWidth, sliderHeight, new Point(1, 2), 0, 2, 1, GameSettings.ClassicBorder, 
                number => { GameSettings.ClassicBorder = number; Game1.ScaleChanged = true; Camera.SnapCameraTimer = 20f; }) 
                { SetString = number => ClassicBorderAdjustment(number) };
            _contentLayout.AddElement(_sliderCameraBorder);
            _tooltips.Add("tooltip_camera_camborder");

            // Slider: Classic Border Blackout Amount
            _sliderBorderOpacity = new InterfaceSlider("settings_camera_blackpercent",
                buttonWidth, sliderHeight, new Point(1, 2), 0, 100, 5, (int)(GameSettings.ClassicAlpha * 100),
                number => { GameSettings.ClassicAlpha = (float)(number * 0.01); })
                { SetString = number => SetClassicBorderOpacity(number) };
            _contentLayout.AddElement(_sliderBorderOpacity);
            _tooltips.Add("tooltip_camera_blackpercent");

            // Slider: Classic Border Bias
            _sliderBorderBias = new InterfaceSlider("settings_camera_bias",
                buttonWidth, sliderHeight, new Point(1, 2), 0, 2, 1, GameSettings.ClassicBias,
                number => { GameSettings.ClassicBias = number; })
                { SetString = number => ClassicBorderBiasAdjustment(number) };
            _contentLayout.AddElement(_sliderBorderBias);
            _tooltips.Add("tooltip_camera_bias");

            // Toggle: Classic Scale Lock
            _toggleClassicScaling = InterfaceToggle.GetToggleButton(
                new Point(buttonWidth, buttonHeight), new Point(5, 2),
                "settings_camera_classicscaling", GameSettings.ClassicScaling, 
                newState => { GameSettings.ClassicScaling = newState; Game1.ScaleChanged = true; Camera.SnapCameraTimer = 10f; });
            _contentLayout.AddElement(_toggleClassicScaling);
            _tooltips.Add("tooltip_camera_classicscaling");

            // Toggle: Camera Lock
            _toggleCameraLock = InterfaceToggle.GetToggleButton(
                new Point(buttonWidth, buttonHeight), new Point(5, 2),
                "settings_camera_cameralock", GameSettings.CameraLock, 
                newState => { GameSettings.CameraLock = newState; ReloadVirtualController(); });
            _contentLayout.AddElement(_toggleCameraLock);
            _tooltips.Add("tooltip_camera_cameralock");

            // Toggle: Smooth Camera
            _toggleCameraSmooth = InterfaceToggle.GetToggleButton(
                new Point(buttonWidth, buttonHeight), new Point(5, 2),
                "settings_camera_smoothcamera", GameSettings.SmoothCamera, 
                newState => { GameSettings.SmoothCamera = newState; });
            _contentLayout.AddElement(_toggleCameraSmooth);
            _tooltips.Add("tooltip_camera_smoothcamera");

            // Bottom Bar / Back Button:
            _bottomBar = new InterfaceListLayout() { Size = new Point(width, (int)(height * Values.MenuFooterSize)), Selectable = true, HorizontalMode = true };
            _bottomBar.AddElement(new InterfaceButton(new Point(100, 18), new Point(2, 4), "settings_menu_back", element => { Game1.UiPageManager.PopPage(); }));
            _cameraOptionsList.AddElement(_contentLayout);
            _cameraOptionsList.AddElement(_bottomBar);
            PageLayout = _cameraOptionsList;

            // Update button colors.
            UpdateInterfaceColors();
        }

        public override void Update(CButtons pressedButtons, GameTime gameTime)
        {
            base.Update(pressedButtons, gameTime);

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

        public override void OnLoad(Dictionary<string, object> intent)
        {
            // the left button is always the first one selected
            _bottomBar.Deselect(false);
            _bottomBar.Select(InterfaceElement.Directions.Left, false);
            _bottomBar.Deselect(false);

            PageLayout.Deselect(false);
            PageLayout.Select(InterfaceElement.Directions.Top, false);
        }

        public void ReloadVirtualController()
        {
            if (Game1.PlatformInput.HasTouchInput)
                VirtualController.Initialize(Game1.WindowWidth, Game1.WindowHeight, true);
        }

        private string SetCameraSetting(int number)
        {
            // The camera has changed so the game scale must also be upated.
            Game1.ScaleChanged = true;

            // Toggling classic camera "grays out" some options depending on its state.
            UpdateInterfaceColors();

            // Set the text to the camera selected.
            return ": " + number switch
            {
                0 => Game1.LanguageManager.GetString("settings_camera_cameratypeA", "error"),
                1 => Game1.LanguageManager.GetString("settings_camera_cameratypeB", "error"),
                2 => Game1.LanguageManager.GetString("settings_camera_cameratypeC", "error"),
                _ => throw new System.Runtime.CompilerServices.SwitchExpressionException()
            };
        }

        public void UpdateInterfaceColors()
        {
            // Gray out options based on camera. No options get grayed out when set to "Customize Camera".
            bool modern = GameSettings.CameraMode != 1;
            bool classic = GameSettings.CameraMode != 0;

            // The only option that grays out when set to "Classic".
            _toggleCameraLock.ToggleElementColors(modern);

            // These options get grayed out when set to "Modern".
            _toggleClassicScaling.ToggleElementColors(classic);
            _sliderCameraBorder.ToggleSliderColors(classic);
            _sliderBorderOpacity.ToggleSliderColors(classic);
            _sliderBorderBias.ToggleSliderColors(classic);
        }

        private string ClassicBorderAdjustment(int number)
        {
            return ": " + number switch
            {
                0 => Game1.LanguageManager.GetString("tooltip_camera_camborderA", "error"),
                1 => Game1.LanguageManager.GetString("tooltip_camera_camborderB", "error"),
                2 => Game1.LanguageManager.GetString("tooltip_camera_camborderC", "error"),
                _ => Game1.LanguageManager.GetString("tooltip_camera_camborderA", "error")
            };
        }

        private string SetClassicBorderOpacity(int number)
        {
            return ": " + number + "%";
        }

        private string ClassicBorderBiasAdjustment(int number)
        {
            return ": " + number switch
            {
                0 => Game1.LanguageManager.GetString("tooltip_camera_biasA", "error"),
                1 => Game1.LanguageManager.GetString("tooltip_camera_biasB", "error"),
                2 => Game1.LanguageManager.GetString("tooltip_camera_biasC", "error"),
                _ => Game1.LanguageManager.GetString("tooltip_camera_biasA", "error")
            };
        }

        public override void Draw(SpriteBatch spriteBatch, Vector2 position, float height, float alpha)
        {
            // Always draw the menu even when not showing tooltips.
            base.Draw(spriteBatch, position, height, alpha);

            // If the user pressed the top most face button, show the tooltip window.
            if (_showTooltip)
            {
                string tooltipText = PageTooltip.GetTooltipIndex(_cameraOptionsList, _contentLayout, _tooltips);
                PageTooltip.Draw(spriteBatch, tooltipText);
            }
        }
    }
}
