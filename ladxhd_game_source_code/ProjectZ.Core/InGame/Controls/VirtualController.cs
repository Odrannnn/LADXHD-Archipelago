using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Pages;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.Controls
{
    public static class VirtualController
    {
        private static readonly List<VirtualButton> _buttons = new List<VirtualButton>();
        private static VirtualButton _controllerButton;
        private static VirtualStick _leftStick;
        private static VirtualStick _rightStick;

        private static DictAtlasEntry _dPadSprite;
        private static Rectangle _dPadBounds;

        private static float _controllerHideTimer;
        private const float ControllerHideDelay = 5000f;
        private static bool _controllerButtonHidden;

        private static float ControlsScale => GameSettings.TouchScaling * 0.25f;

        public static float DPadButtonAlpha = GameSettings.TouchOpacity * 0.01f;
        public static float DPadShadowAlpha = GameSettings.ShadowOpacity * 0.01f;

        private static bool menu = true;

        public static VirtualStick GetLeftStick() => _leftStick;
        public static VirtualStick GetRightStick() => _rightStick;
        public static List<VirtualButton> GetButtons() => _buttons;

        public static Vector2 GetLeftStickOutput() => _leftStick != null ? _leftStick.Output : Vector2.Zero;
        public static Vector2 GetRightStickOutput() => _rightStick != null ? _rightStick.Output : Vector2.Zero;

        private static Point GetShadowOffset()
        {
            int offset = (int)(3 * ControlsScale);
            return new Point(offset, offset);
        }

        public static void Initialize(int screenWidth, int screenHeight, bool? isMenu)
        {
            // Store the current states so button states survive reinitialization.
            var previousStates = new Dictionary<CButtons, (bool IsDown, bool WasDown, int? TouchId)>();
            foreach (var b in _buttons)
                previousStates[b.Button] = (b.IsDown, b.WasDown, b.TouchId);

            var previousControllerState = _controllerButton != null
                ? (_controllerButton.IsDown, _controllerButton.WasDown, _controllerButton.TouchId)
                : ((bool, bool, int?)?)null;

            _dPadSprite = null;
            _leftStick = null;
            _rightStick = null;
            _buttons.Clear();
            _controllerButton = null;
            _controllerHideTimer = 0f;
            _controllerButtonHidden = false;
            if (isMenu != null) menu = isMenu.Value;

            float scale = ControlsScale;
            int buttonSize = (int)(40 * scale);
            int margin = (int)(16 * scale);
            int spacing = (int)(8 * scale);

            // ------------------------------------------------------------------------------------------------------------------------
            // LEFT SIDE: DPAD 
            // ------------------------------------------------------------------------------------------------------------------------
            int leftX = margin;
            int leftY = screenHeight - margin;

            if (GameSettings.TouchMovement is 0 or 2 || (GameSettings.TouchMovement == 1 && menu))
            {
                _dPadSprite = Resources.GetSprite("button_dpad");
                _dPadBounds = new Rectangle(leftX, leftY - (buttonSize * 3) - (spacing * 2), buttonSize * 3 + spacing * 2, buttonSize * 3 + spacing * 2);

                Rectangle rectLeft = new Rectangle(leftX, leftY - (buttonSize * 2) - spacing, buttonSize, buttonSize);
                Rectangle rectUp = new Rectangle(leftX + buttonSize + spacing, leftY - (buttonSize * 3) - (spacing * 2), buttonSize, buttonSize);
                Rectangle rectRight = new Rectangle(leftX + (buttonSize * 2) + (spacing * 2), leftY - (buttonSize * 2) - spacing, buttonSize, buttonSize);
                Rectangle rectDown = new Rectangle(leftX + buttonSize + spacing, leftY - buttonSize, buttonSize, buttonSize);

                _buttons.Add(new VirtualButton("null", CButtons.Left, rectLeft));
                _buttons.Add(new VirtualButton("null", CButtons.Up, rectUp));
                _buttons.Add(new VirtualButton("null", CButtons.Right, rectRight));
                _buttons.Add(new VirtualButton("null", CButtons.Down, rectDown));

                Rectangle rectUpLeft = new Rectangle(leftX, leftY - (buttonSize * 3) - (spacing * 2), buttonSize, buttonSize);
                Rectangle rectUpRight = new Rectangle(leftX + (buttonSize * 2) + (spacing * 2), leftY - (buttonSize * 3) - (spacing * 2), buttonSize, buttonSize);
                Rectangle rectDownLeft = new Rectangle(leftX, leftY - buttonSize, buttonSize, buttonSize);
                Rectangle rectDownRight = new Rectangle(leftX + (buttonSize * 2) + (spacing * 2), leftY - buttonSize, buttonSize, buttonSize);

                _buttons.Add(new VirtualButton("null", CButtons.Up | CButtons.Left, rectUpLeft));
                _buttons.Add(new VirtualButton("null", CButtons.Up | CButtons.Right, rectUpRight));
                _buttons.Add(new VirtualButton("null", CButtons.Down | CButtons.Left, rectDownLeft));
                _buttons.Add(new VirtualButton("null", CButtons.Down | CButtons.Right, rectDownRight));
            }
            // ------------------------------------------------------------------------------------------------------------------------
            // RIGHT SIDE: X / Y / B / A
            // ------------------------------------------------------------------------------------------------------------------------
            int rightX = screenWidth - margin;
            int rightY = screenHeight - margin;

            Rectangle rectX = new Rectangle(rightX - (buttonSize * 3) - (spacing * 2), rightY - (buttonSize * 2) - spacing, buttonSize, buttonSize);
            Rectangle rectY = new Rectangle(rightX - (buttonSize * 2) - spacing, rightY - (buttonSize * 3) - (spacing * 2), buttonSize, buttonSize);
            Rectangle rectB = new Rectangle(rightX - buttonSize, rightY - (buttonSize * 2) - spacing, buttonSize, buttonSize);
            Rectangle rectA = new Rectangle(rightX - (buttonSize * 2) - spacing, rightY - buttonSize, buttonSize, buttonSize);

            _buttons.Add(new VirtualButton("button_x", CButtons.X, rectX));
            _buttons.Add(new VirtualButton("button_y", CButtons.Y, rectY));
            _buttons.Add(new VirtualButton("button_b", CButtons.B, rectB));
            _buttons.Add(new VirtualButton("button_a", CButtons.A, rectA));

            // ------------------------------------------------------------------------------------------------------------------------
            // STICK BUTTONS
            // ------------------------------------------------------------------------------------------------------------------------
            if (GameSettings.TouchSticks)
            {
                int extraButtonLift = GameSettings.SixButtons ? buttonSize + spacing : spacing;
                Rectangle rectExtraR = new Rectangle(rectY.X, rectY.Y - buttonSize - spacing - extraButtonLift, buttonSize, buttonSize);
                _buttons.Add(new VirtualButton("button_rc", CButtons.RS, rectExtraR));

                int dpadCenterX = _dPadBounds.X + (_dPadBounds.Width / 2) - (buttonSize / 2);
                Rectangle rectExtraL = new Rectangle(dpadCenterX, _dPadBounds.Y - (buttonSize * 2) - spacing, buttonSize, buttonSize);
                _buttons.Add(new VirtualButton("button_lc", CButtons.LS, rectExtraL));
            }
            // ------------------------------------------------------------------------------------------------------------------------
            // ANALOG STICKS
            // ------------------------------------------------------------------------------------------------------------------------
            float stickRadius = 40f * scale;
            float clusterWidth = buttonSize * 3 + spacing * 2;
            float stickGap = spacing * 2 + stickRadius;
            int stickLift = (int)(20 * scale);

            if (GameSettings.TouchMovement == 0)
            {
                Vector2 leftStickCenter = new Vector2(leftX + clusterWidth + stickGap, screenHeight - margin - stickRadius - stickLift);
                _leftStick = new VirtualStick("button_ls", leftStickCenter, stickRadius);
            }
            else if ((GameSettings.TouchMovement == 1 && !menu) || GameSettings.TouchMovement == 3)
            {
                Vector2 leftStickCenter = new Vector2(leftX + stickGap, screenHeight - margin - stickRadius - stickLift);
                _leftStick = new VirtualStick("button_ls", leftStickCenter, stickRadius);
            }

            if (!GameSettings.CameraLock)
            {
                Vector2 rightStickCenter = new Vector2(rightX - clusterWidth - stickGap, screenHeight - margin - stickRadius - stickLift);
                _rightStick = new VirtualStick("button_rs", rightStickCenter, stickRadius);
            }

            // ------------------------------------------------------------------------------------------------------------------------
            // TOP SHOULDER BUTTONS / SIX BUTTON LAYOUT
            // ------------------------------------------------------------------------------------------------------------------------
            int topY = margin;
            if (GameSettings.SixButtons)
            {
                Rectangle rectLB6 = new Rectangle(rectX.X, rectY.Y - buttonSize - spacing, buttonSize, buttonSize);
                Rectangle rectRB6 = new Rectangle(rectB.X, rectY.Y - buttonSize - spacing, buttonSize, buttonSize);

                _buttons.Add(new VirtualButton("button_lb", CButtons.LB, rectLB6));
                _buttons.Add(new VirtualButton("button_rb", CButtons.RB, rectRB6));

                if (GameSettings.TriggersScale)
                {
                    _buttons.Add(new VirtualButton("button_rt", CButtons.RT, new Rectangle(screenWidth - margin - buttonSize, topY, buttonSize, buttonSize)));
                    _buttons.Add(new VirtualButton("button_lt", CButtons.LT, new Rectangle(margin, topY, buttonSize, buttonSize)));
                }
            }
            else if (GameSettings.TriggersScale)
            {
                _buttons.Add(new VirtualButton("button_rb", CButtons.RB, new Rectangle(screenWidth - margin - (buttonSize * 2) - (spacing * 2), topY, buttonSize, buttonSize)));
                _buttons.Add(new VirtualButton("button_lb", CButtons.LB, new Rectangle(margin + buttonSize + (spacing * 2), topY, buttonSize, buttonSize)));
            }
            // ------------------------------------------------------------------------------------------------------------------------
            // SELECT / START
            // ------------------------------------------------------------------------------------------------------------------------
            int centerX = screenWidth / 2;
            int bottomY = screenHeight - margin - buttonSize;

            if (GameSettings.TouchTopMiddle)
            {
                _buttons.Add(new VirtualButton("button_share", CButtons.Select, new Rectangle(centerX - (buttonSize * 2) - spacing, topY, buttonSize, buttonSize)));
                _controllerButton = new VirtualButton("button_controller", CButtons.None, new Rectangle(centerX - (buttonSize / 2), topY, buttonSize, buttonSize));
                _buttons.Add(new VirtualButton("button_menu", CButtons.Start, new Rectangle(centerX + buttonSize + spacing, topY, buttonSize, buttonSize)));
            }
            else
            {
                _buttons.Add(new VirtualButton("button_share", CButtons.Select, new Rectangle(centerX - (buttonSize * 2) - spacing, bottomY, buttonSize, buttonSize)));
                _controllerButton = new VirtualButton("button_controller", CButtons.None, new Rectangle(centerX - (buttonSize / 2), bottomY, buttonSize, buttonSize));
                _buttons.Add(new VirtualButton("button_menu", CButtons.Start, new Rectangle(centerX + buttonSize + spacing, bottomY, buttonSize, buttonSize)));
            }
            // Restore any touch that was already down before the rebuild.
            foreach (var button in _buttons)
            {
                if (previousStates.TryGetValue(button.Button, out var state))
                {
                    button.IsDown = state.IsDown;
                    button.WasDown = state.WasDown;
                    button.TouchId = state.TouchId;
                }
            }
            if (_controllerButton != null && previousControllerState.HasValue)
            {
                _controllerButton.IsDown = previousControllerState.Value.Item1;
                _controllerButton.WasDown = previousControllerState.Value.Item2;
                _controllerButton.TouchId = previousControllerState.Value.Item3;
            }
        }

        public static void UpdateButtonsAlpha()
        {
            foreach (var button in _buttons)
            {
                button.DisplayAlpha = UpdateButtonAlpha(button.DisplayAlpha, button.IsDown);
                button.ShadowAlpha  = UpdateShadowAlpha(button.ShadowAlpha, button.IsDown);
            }
            if (_controllerButton != null)
            {
                _controllerButton.DisplayAlpha = UpdateButtonAlpha(_controllerButton.DisplayAlpha, _controllerButton.IsDown, true);
                _controllerButton.ShadowAlpha  = UpdateShadowAlpha(_controllerButton.ShadowAlpha, _controllerButton.IsDown, true);
            }
            if (_leftStick != null)
            {
                _leftStick.DisplayAlpha = UpdateButtonAlpha(_leftStick.DisplayAlpha, _leftStick.IsDown);
                _leftStick.ShadowAlpha  = UpdateShadowAlpha(_leftStick.ShadowAlpha, _leftStick.IsDown);
            }
            if (_rightStick != null)
            {
                _rightStick.DisplayAlpha = UpdateButtonAlpha(_rightStick.DisplayAlpha, _rightStick.IsDown);
                _rightStick.ShadowAlpha  = UpdateShadowAlpha(_rightStick.ShadowAlpha, _rightStick.IsDown);
            }
            bool dPadActive = DPadIsActive();
            DPadButtonAlpha = UpdateButtonAlpha(DPadButtonAlpha, dPadActive);
            DPadShadowAlpha = UpdateShadowAlpha(DPadShadowAlpha, dPadActive);
        }

        private static float UpdateButtonAlpha(float currentAlpha, bool isActive, bool isControllerButton = false)
        {
            float buttonMaxAlpha = 1.00f;
            float targetAlpha;

            if (isControllerButton)
            {
                if (GameSettings.TouchControls == 0 && _controllerButtonHidden)
                    targetAlpha = 0f;
                else
                    targetAlpha = isActive ? buttonMaxAlpha : GameSettings.TouchOpacity * 0.01f;
            }
            else if (GameSettings.TouchControls == 0)
                targetAlpha = 0f;
            else if (GameSettings.TouchControls == 2)
                targetAlpha = buttonMaxAlpha;
            else
                targetAlpha = isActive ? buttonMaxAlpha : GameSettings.TouchOpacity * 0.01f;

            float speed = 0.01f * Game1.DeltaTime;
            return MathHelper.Lerp(currentAlpha, targetAlpha, MathHelper.Clamp(speed, 0f, 1f));
        }

        private static float UpdateShadowAlpha(float currentAlpha, bool isActive, bool isControllerButton = false)
        {
            float targetAlpha;

            if (isControllerButton)
            {
                if (GameSettings.TouchControls == 0 && _controllerButtonHidden)
                    targetAlpha = 0f;
                else
                    targetAlpha = GameSettings.ShadowOpacity * 0.01f;
            }
            else if (GameSettings.TouchControls == 0)
                targetAlpha = 0f;
            else
                targetAlpha = GameSettings.ShadowOpacity * 0.01f;

            float speed = 0.01f * Game1.DeltaTime;
            return MathHelper.Lerp(currentAlpha, targetAlpha, MathHelper.Clamp(speed, 0f, 1f));
        }

        public static void Update()
        {
            foreach (var button in _buttons)
                button.BeginUpdate();

            _controllerButton?.BeginUpdate();
            _leftStick?.BeginUpdate();
            _rightStick?.BeginUpdate();

            var touches = Game1.PlatformInput.Touches;

            int holdPadding = (int)(12 * ControlsScale);
            bool anyTouchActive = false;

            for (int i = 0; i < touches.Count; i++)
            {
                PlatformTouch touch = touches[i];

                if (touch.State == PlatformTouchState.Pressed ||
                    touch.State == PlatformTouchState.Moved)
                {
                    anyTouchActive = true;
                    break;
                }
            }

            // --------------------------------------------------------------------------------------------------------------------
            // CONTROLLER BUTTON AUTO-HIDE
            // --------------------------------------------------------------------------------------------------------------------
            if (GameSettings.TouchControls == 0)
            {
                if (anyTouchActive)
                {
                    _controllerHideTimer = 0f;
                    _controllerButtonHidden = false;
                }
                else
                {
                    _controllerHideTimer += Game1.DeltaTime;

                    if (_controllerHideTimer >= ControllerHideDelay)
                        _controllerButtonHidden = true;
                }
            }
            else
            {
                _controllerHideTimer = 0f;
                _controllerButtonHidden = false;
            }

            // --------------------------------------------------------------------------------------------------------------------
            // KEEP CONTROLLER BUTTON ALIVE
            // --------------------------------------------------------------------------------------------------------------------
            if (_controllerButton != null && _controllerButton.TouchId != null)
            {
                bool foundTouch = false;

                for (int i = 0; i < touches.Count; i++)
                {
                    PlatformTouch touch = touches[i];

                    if (touch.Id != _controllerButton.TouchId.Value)
                        continue;

                    if (touch.State == PlatformTouchState.Released ||
                        touch.State == PlatformTouchState.Invalid)
                        break;

                    foundTouch = true;

                    if (_controllerButton.ContainsExpanded(touch.Position.ToPoint(), holdPadding))
                        _controllerButton.IsDown = true;

                    break;
                }

                if (!foundTouch || !_controllerButton.IsDown)
                    _controllerButton.TouchId = null;
            }

            // --------------------------------------------------------------------------------------------------------------------
            // TOUCH CONTROLS DISABLED
            // Only the controller button is allowed to work here.
            // --------------------------------------------------------------------------------------------------------------------
            if (GameSettings.TouchControls == 0)
            {
                if (_controllerButton != null &&
                    !_controllerButtonHidden &&
                    _controllerButton.TouchId == null)
                {
                    for (int i = 0; i < touches.Count; i++)
                    {
                        PlatformTouch touch = touches[i];

                        if (touch.State != PlatformTouchState.Pressed)
                            continue;

                        if (_controllerButton.Contains(touch.Position.ToPoint()))
                        {
                            _controllerButton.IsDown = true;
                            _controllerButton.TouchId = touch.Id;
                            break;
                        }
                    }
                }

                foreach (var button in _buttons)
                    button.TouchId = null;

                if (_leftStick != null)
                    _leftStick.TouchId = null;

                if (_rightStick != null)
                    _rightStick.TouchId = null;

                UpdateButtonsAlpha();

                if (ControllerButtonPressed())
                {
                    GameSettings.TouchControls = 1;

                    if (Game1.UiPageManager.InsideElement.TryGetValue(typeof(ControlOnScreenPage), out var onScreenControlsPage))
                    {
                        var onScreenSettingsPage = (ControlOnScreenPage)onScreenControlsPage;
                        onScreenSettingsPage.SetOnScreenControlsSlider(GameSettings.TouchControls);
                    }
                }

                return;
            }

            // --------------------------------------------------------------------------------------------------------------------
            // KEEP NORMAL BUTTONS ALIVE
            // --------------------------------------------------------------------------------------------------------------------
            foreach (var button in _buttons)
            {
                if (button.TouchId == null)
                    continue;

                bool foundTouch = false;

                for (int i = 0; i < touches.Count; i++)
                {
                    PlatformTouch touch = touches[i];

                    if (touch.Id != button.TouchId.Value)
                        continue;

                    if (touch.State == PlatformTouchState.Released ||
                        touch.State == PlatformTouchState.Invalid)
                        break;

                    foundTouch = true;

                    if (button.ContainsExpanded(touch.Position.ToPoint(), holdPadding))
                        button.IsDown = true;

                    break;
                }

                if (!foundTouch || !button.IsDown)
                    button.TouchId = null;
            }

            // --------------------------------------------------------------------------------------------------------------------
            // KEEP LEFT STICK ALIVE
            // --------------------------------------------------------------------------------------------------------------------
            if (_leftStick != null && _leftStick.TouchId != null)
            {
                bool foundTouch = false;

                for (int i = 0; i < touches.Count; i++)
                {
                    PlatformTouch touch = touches[i];

                    if (touch.Id != _leftStick.TouchId.Value)
                        continue;

                    if (touch.State == PlatformTouchState.Released ||
                        touch.State == PlatformTouchState.Invalid)
                        break;

                    foundTouch = true;
                    _leftStick.IsDown = true;
                    _leftStick.SetTouchPosition(touch.Position);
                    break;
                }

                if (!foundTouch || !_leftStick.IsDown)
                    _leftStick.TouchId = null;
            }

            // --------------------------------------------------------------------------------------------------------------------
            // KEEP RIGHT STICK ALIVE
            // --------------------------------------------------------------------------------------------------------------------
            if (_rightStick != null && _rightStick.TouchId != null)
            {
                bool foundTouch = false;

                for (int i = 0; i < touches.Count; i++)
                {
                    PlatformTouch touch = touches[i];

                    if (touch.Id != _rightStick.TouchId.Value)
                        continue;

                    if (touch.State == PlatformTouchState.Released ||
                        touch.State == PlatformTouchState.Invalid)
                        break;

                    foundTouch = true;
                    _rightStick.IsDown = true;
                    _rightStick.SetTouchPosition(touch.Position);
                    break;
                }

                if (!foundTouch || !_rightStick.IsDown)
                    _rightStick.TouchId = null;
            }

            // --------------------------------------------------------------------------------------------------------------------
            // CLAIM NEW TOUCHES
            // --------------------------------------------------------------------------------------------------------------------
            for (int i = 0; i < touches.Count; i++)
            {
                PlatformTouch touch = touches[i];

                // We need both "Pressed" and "Moved" here to smoothly move between buttons.
                if (touch.State != PlatformTouchState.Pressed &&
                    touch.State != PlatformTouchState.Moved)
                    continue;

                bool alreadyUsed = false;

                if (_controllerButton != null && _controllerButton.TouchId == touch.Id)
                    alreadyUsed = true;

                if (!alreadyUsed)
                {
                    foreach (var button in _buttons)
                    {
                        if (button.TouchId == touch.Id)
                        {
                            alreadyUsed = true;
                            break;
                        }
                    }
                }

                if (!alreadyUsed && _leftStick != null && _leftStick.TouchId == touch.Id)
                    alreadyUsed = true;

                if (!alreadyUsed && _rightStick != null && _rightStick.TouchId == touch.Id)
                    alreadyUsed = true;

                if (alreadyUsed)
                    continue;

                Point point = touch.Position.ToPoint();

                // Try controller button first.
                if (_controllerButton != null && _controllerButton.TouchId == null && _controllerButton.Contains(point))
                {
                    _controllerButton.IsDown = true;
                    _controllerButton.TouchId = touch.Id;
                    continue;
                }

                // Try buttons next.
                bool claimed = false;

                foreach (var button in _buttons)
                {
                    if (button.TouchId != null)
                        continue;

                    if (button.Contains(point))
                    {
                        button.IsDown = true;
                        button.TouchId = touch.Id;
                        claimed = true;
                        break;
                    }
                }

                if (claimed)
                    continue;

                // Then try left stick.
                if (_leftStick != null && _leftStick.TouchId == null && _leftStick.Contains(point))
                {
                    _leftStick.IsDown = true;
                    _leftStick.TouchId = touch.Id;
                    _leftStick.SetTouchPosition(touch.Position);
                    continue;
                }

                // Then try right stick.
                if (_rightStick != null && _rightStick.TouchId == null && _rightStick.Contains(point))
                {
                    _rightStick.IsDown = true;
                    _rightStick.TouchId = touch.Id;
                    _rightStick.SetTouchPosition(touch.Position);
                }
            }

            UpdateButtonsAlpha();

            if (ControllerButtonPressed())
            {
                GameSettings.TouchControls = 0;

                if (Game1.UiPageManager.InsideElement.TryGetValue(typeof(ControlOnScreenPage), out var onScreenControlsPage))
                {
                    var onScreenSettingsPage = (ControlOnScreenPage)onScreenControlsPage;
                    onScreenSettingsPage.SetOnScreenControlsSlider(GameSettings.TouchControls);
                }
            }
        }

        public static bool ButtonDown(CButtons button)
        {
            for (int i = 0; i < _buttons.Count; i++)
            {
                if ((_buttons[i].Button & button) != 0 && _buttons[i].IsDown)
                    return true;
            }
            return false;
        }

        public static bool ButtonPressed(CButtons button)
        {
            for (int i = 0; i < _buttons.Count; i++)
            {
                if ((_buttons[i].Button & button) != 0 && _buttons[i].Pressed())
                    return true;
            }
            return false;
        }

        public static bool ButtonReleased(CButtons button)
        {
            for (int i = 0; i < _buttons.Count; i++)
            {
                if ((_buttons[i].Button & button) != 0 && _buttons[i].Released())
                    return true;
            }
            return false;
        }

        public static bool ControllerButtonDown()
        {
            return _controllerButton != null && _controllerButton.IsDown;
        }

        public static bool ControllerButtonPressed()
        {
            return _controllerButton != null && _controllerButton.Pressed();
        }

        public static bool ControllerButtonReleased()
        {
            return _controllerButton != null && _controllerButton.Released();
        }

        private static bool DPadIsActive()
        {
            return ButtonDown(CButtons.Left) || ButtonDown(CButtons.Up) || ButtonDown(CButtons.Right) || ButtonDown(CButtons.Down);
        }

        public static void Draw(SpriteBatch spriteBatch)
        {
            if (_controllerButton != null)
            {
                float alpha = _controllerButton.DisplayAlpha;

                if (_controllerButton.Sprite != null)
                {
                    float shadowAlpha = _controllerButton.ShadowAlpha;
                    Point shadowOffset = GetShadowOffset();

                    Rectangle src = _controllerButton.Sprite.ScaledRectangle;
                    Vector2 position = new Vector2(_controllerButton.Bounds.X, _controllerButton.Bounds.Y);
                    Vector2 shadowPosition = new Vector2(_controllerButton.Bounds.X + shadowOffset.X, _controllerButton.Bounds.Y + shadowOffset.Y);

                    float scaleX = _controllerButton.Bounds.Width / (float)src.Width;
                    float scaleY = _controllerButton.Bounds.Height / (float)src.Height;
                    Vector2 scale = new Vector2(scaleX, scaleY);

                    spriteBatch.Draw(_controllerButton.Sprite.Texture, shadowPosition, src, Color.Black * shadowAlpha, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                    spriteBatch.Draw(_controllerButton.Sprite.Texture, position, src, Color.White * alpha, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                }
                else if (_controllerButton.SpriteName != "null")
                {
                    spriteBatch.Draw(Resources.SprWhite, _controllerButton.Bounds, Color.White * alpha);
                }
            }

            if (GameSettings.TouchControls == 0)
                return;

            if (_dPadSprite != null)
            {
                float alpha = DPadButtonAlpha;
                float shadowAlpha = DPadShadowAlpha;
                Point shadowOffset = GetShadowOffset();

                Rectangle src = _dPadSprite.ScaledRectangle;
                Vector2 position = new Vector2(_dPadBounds.X, _dPadBounds.Y);
                Vector2 shadowPosition = new Vector2(_dPadBounds.X + shadowOffset.X, _dPadBounds.Y + shadowOffset.Y);

                float scaleX = _dPadBounds.Width / (float)src.Width;
                float scaleY = _dPadBounds.Height / (float)src.Height;
                Vector2 scale = new Vector2(scaleX, scaleY);

                spriteBatch.Draw(_dPadSprite.Texture, shadowPosition, src, Color.Black * shadowAlpha, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                spriteBatch.Draw(_dPadSprite.Texture, position, src, Color.White * alpha, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            }

            DrawStick(spriteBatch, _leftStick);
            DrawStick(spriteBatch, _rightStick);

            for (int i = 0; i < _buttons.Count; i++)
            {
                var button = _buttons[i];
                float alpha = button.DisplayAlpha;

                if (button.Sprite != null)
                {
                    float shadowAlpha = button.ShadowAlpha;
                    Point shadowOffset = GetShadowOffset();

                    Rectangle src = button.Sprite.ScaledRectangle;
                    Vector2 position = new Vector2(button.Bounds.X, button.Bounds.Y);
                    Vector2 shadowPosition = new Vector2(button.Bounds.X + shadowOffset.X, button.Bounds.Y + shadowOffset.Y);

                    float scaleX = button.Bounds.Width / (float)src.Width;
                    float scaleY = button.Bounds.Height / (float)src.Height;
                    Vector2 scale = new Vector2(scaleX, scaleY);

                    spriteBatch.Draw(button.Sprite.Texture, shadowPosition, src, Color.Black * shadowAlpha, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                    spriteBatch.Draw(button.Sprite.Texture, position, src, Color.White * alpha, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                }
                else if (button.SpriteName != "null")
                {
                    spriteBatch.Draw(Resources.SprWhite, button.Bounds, Color.White * alpha);
                }
            }
        }

        private static void DrawStick(SpriteBatch spriteBatch, VirtualStick stick)
        {
            if (stick == null)
                return;

            int baseSize = (int)(stick.Radius * 2f);
            int knobSize = (int)(stick.Radius * 1.1f);

            Rectangle baseRect = new Rectangle((int)(stick.Center.X - stick.Radius), (int)(stick.Center.Y - stick.Radius), baseSize, baseSize);
            Rectangle knobRect = new Rectangle((int)(stick.KnobPosition.X - knobSize / 2f), (int)(stick.KnobPosition.Y - knobSize / 2f), knobSize, knobSize);

            float baseAlpha = stick.DisplayAlpha;
            float knobAlpha = stick.DisplayAlpha;

            Point shadowOffset = GetShadowOffset();

            if (stick.BaseSprite != null)
            {
                Rectangle src = stick.BaseSprite.ScaledRectangle;
                float scaleX = baseRect.Width / (float)src.Width;
                float scaleY = baseRect.Height / (float)src.Height;
                Vector2 scale = new Vector2(scaleX, scaleY);

                Vector2 basePosition = new Vector2(baseRect.X, baseRect.Y);
                Vector2 baseShadowPosition = new Vector2(baseRect.X + shadowOffset.X, baseRect.Y + shadowOffset.Y);

                spriteBatch.Draw(stick.BaseSprite.Texture, baseShadowPosition, src, Color.Black * stick.ShadowAlpha, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                spriteBatch.Draw(stick.BaseSprite.Texture, basePosition, src, Color.White * baseAlpha, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            }
            else
            {
                spriteBatch.Draw(Resources.SprWhite, baseRect, Color.White * baseAlpha);
            }
            // Draw moving head texture
            if (stick.HeadSprite != null)
            {
                Rectangle src = stick.HeadSprite.ScaledRectangle;
                float scaleX = knobRect.Width / (float)src.Width;
                float scaleY = knobRect.Height / (float)src.Height;
                Vector2 scale = new Vector2(scaleX, scaleY);

                Vector2 knobPosition = new Vector2(knobRect.X, knobRect.Y);
                Vector2 knobShadowPosition = new Vector2(knobRect.X + shadowOffset.X, knobRect.Y + shadowOffset.Y);

                spriteBatch.Draw(stick.HeadSprite.Texture, knobShadowPosition, src, Color.Black * stick.ShadowAlpha, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                spriteBatch.Draw(stick.HeadSprite.Texture, knobPosition, src, Color.White * knobAlpha, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            }
            else
            {
                spriteBatch.Draw(Resources.SprWhite, knobRect, Color.White * knobAlpha);
            }
        }
    }
}
