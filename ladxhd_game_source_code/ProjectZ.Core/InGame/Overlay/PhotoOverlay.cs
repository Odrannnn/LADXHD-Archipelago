using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.Controls;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.Overlay
{
    class PhotoOverlay
    {
        private DictAtlasEntry[] _spritePhotos;
        private DictAtlasEntry _spriteBook;
        private DictAtlasEntry _spriteCursor;
        private DictAtlasEntry _spriteNop;
        private DictAtlasEntry _spriteOk;
        private DictAtlasEntry _spriteButtonRed;
        private DictAtlasEntry _spriteButtonYellow;
        private DictAtlasEntry _spriteText_A;
        private DictAtlasEntry _spriteText_B;
        private DictAtlasEntry _spriteText_X;
        private DictAtlasEntry _spriteText_O;
        private DictAtlasEntry _spriteCancel;
        private DictAtlasEntry _spritePrint;

        public Color _textboxFontColor = new Color(248, 248, 136);

        private bool[] _unlockState = new bool[12];
        private int _cursorIndex;

        private float _transitionValue;
        private float _transitionCounter;
        private const float TransitionTimeOpen = 125;
        private const float TransitionTimeClose = 125;
        private bool _isShowingImage;

        private bool _hideButtons;

        private float _cursorState;
        private float _cursorCounter;
        private float _cursorTime = 200f;
        private bool _cursorPressed;

        private void LoadPhotoImages()
        {
            _spritePhotos = new DictAtlasEntry[12];
            for (var i = 0; i < 12; i++)
                _spritePhotos[i] = Resources.GetPhotoSprite("photo_" + (i + 1));

            _spriteNop = Resources.GetSprite("photo_no");
            _spriteOk = Resources.GetSprite("photo_ok");
            _spriteCancel = Resources.GetSprite("photo_cancel");
            _spritePrint = Resources.GetSprite("photo_print");
        }

        public void Load()
        {
            LoadPhotoImages();
            _spriteBook = Resources.GetSprite("photo_book");
            _spriteCursor = Resources.GetSprite("photo_cursor");
            _spriteButtonRed = Resources.GetSprite("photo_button_red");
            _spriteButtonYellow = Resources.GetSprite("photo_button_yellow");
            _spriteText_A = Resources.GetSprite("photo_text_a");
            _spriteText_B = Resources.GetSprite("photo_text_b");
            _spriteText_X = Resources.GetSprite("photo_text_x");
            _spriteText_O = Resources.GetSprite("photo_text_o");
        }

        public void Reload()
        {
            LoadPhotoImages();
        }

        public void OnOpen()
        {
            // check the state of the discovered photos
            _isShowingImage = false;
            _transitionCounter = 0;
            _transitionValue = 0;
            _cursorIndex = 0;
            _hideButtons = false;

            for (var i = 0; i < 12; i++)
                _unlockState[i] = !string.IsNullOrEmpty(Game1.GameManager.SaveManager.GetString("photo_" + (i + 1)));

            // set to alt image or not?
            var altPhoto = Game1.GameManager.SaveManager.GetString("photo_1_alt");
            var useAltPhoto = !string.IsNullOrEmpty(altPhoto);
            _spritePhotos[0] = Resources.GetPhotoSprite(useAltPhoto ? "photo_1_alt" : "photo_1");
        }

        public void Update()
        {
            // Convert the index into a 2D position.
            var cursorPoint = CursorPosition(_cursorIndex);

            if (!_isShowingImage)
            {
                // Show the image.
                if (ControlHandler.ButtonPressed(ControlHandler.ConfirmButton))
                {
                    _cursorPressed = true;

                    if (_cursorCounter > _cursorTime / 2)
                        _cursorCounter = _cursorTime - _cursorCounter;

                    if (_unlockState[_cursorIndex])
                    {
                        _isShowingImage = true;
                        Game1.AudioManager.PlaySoundEffect("D360-19-13");
                    }
                    else
                    {
                        Game1.AudioManager.PlaySoundEffect("D360-29-1D");
                    }
                }
                // Update the cursor position.
                else
                {
                    if (ControlHandler.ButtonPressed(CButtons.Left))
                        cursorPoint.X--;
                    if (ControlHandler.ButtonPressed(CButtons.Right))
                        cursorPoint.X++;
                    if (ControlHandler.ButtonPressed(CButtons.Up))
                        cursorPoint.Y--;
                    if (ControlHandler.ButtonPressed(CButtons.Down))
                        cursorPoint.Y++;

                    if (cursorPoint.X < 0)
                        cursorPoint.X += 4;
                    if (cursorPoint.X > 3)
                        cursorPoint.X -= 4;
                    if (cursorPoint.Y < 0)
                        cursorPoint.Y += 3;
                    if (cursorPoint.Y > 2)
                        cursorPoint.Y -= 3;
                }

                // Close the page.
                if (ControlHandler.ButtonPressed(ControlHandler.CancelButton))
                    Game1.GameManager.InGameOverlay.CloseOverlay();
            }
            else
            {
                // Show / Hide the buttons and labels.
                if (ControlHandler.ButtonPressed(ControlHandler.ConfirmButton))
                {
                    _hideButtons = !_hideButtons;
                    Game1.AudioManager.PlaySoundEffect("D360-21-15");
                }

                // Close the image.
                if (ControlHandler.ButtonPressed(ControlHandler.CancelButton))
                {
                    _isShowingImage = false;
                    _transitionCounter = TransitionTimeClose;
                    _hideButtons = false;
                    Game1.AudioManager.PlaySoundEffect("D360-19-13");
                }
            }

            // Update photo transition in and out.
            if (_isShowingImage && _transitionCounter < TransitionTimeOpen)
            {
                _transitionCounter += Game1.DeltaTime;
                if (_transitionCounter > TransitionTimeOpen)
                    _transitionCounter = TransitionTimeOpen;

                _transitionValue = Math.Clamp(_transitionCounter / TransitionTimeOpen, 0, 1);
            }
            else if (!_isShowingImage && _transitionCounter > 0)
            {
                _transitionCounter -= Game1.DeltaTime;
                if (_transitionCounter < 0)
                    _transitionCounter = 0;

                _transitionValue = _transitionCounter / TransitionTimeClose;
                _cursorState = MathF.Sin(_transitionValue * MathF.PI * 0.5f);
            }

            // Show cursor animation on button press.
            if (_cursorPressed)
            {
                _cursorCounter += Game1.DeltaTime;
                if (_cursorCounter >= _cursorTime)
                {
                    _cursorCounter = 0;
                    _cursorPressed = false;
                }
                _cursorState = MathF.Sin(_cursorCounter / _cursorTime * MathF.PI);
            }

            // Update cursor index.
            var cursorIndexNew = CursorIndex(cursorPoint);
            if (_cursorIndex != cursorIndexNew)
            {
                Game1.AudioManager.PlaySoundEffect("D360-10-0A");
                _cursorIndex = cursorIndexNew;
            }
        }

        private Point CursorPosition(int index)
        {
            return new Point(index % 2 + (index / 6) * 2, (index % 6) / 2);
        }

        private int CursorIndex(Point position)
        {
            return position.X % 2 + position.X / 2 * 6 + position.Y * 2;
        }

        public void Draw(SpriteBatch spriteBatch, float transparency)
        {
            var scale = Game1.UiScale + GameSettings.SeqScaleAmplify;
            var useGrid = GameSettings.PixelSnapping && GameSettings.PixelGrid && scale >= 2 && Resources.PixelGrid != null;
            var gridActive = false;

            // We aren't drawing to a render target so every sprite needs its own grid application.
            void SetGrid(bool enabled)
            {
                // Don't draw the grid if it's not enabled.
                if (!useGrid || gridActive == enabled)
                    return;

                // Reference the shader if enabled, null if disabled.
                var pixelGrid = enabled ? Resources.PixelGrid : null;

                spriteBatch.End();
                spriteBatch.Begin(enabled ? SpriteSortMode.Immediate : SpriteSortMode.Deferred, null, SamplerState.PointClamp, null, null, pixelGrid, Game1.GetMatrix);
                gridActive = enabled;
            }

            // Simple function to draw the sprite with our without the texture grid.
            void DrawSprite(DictAtlasEntry sprite, Vector2 position, Color color, Vector2 drawScale, Vector2 origin)
            {
                if (gridActive)
                {
                    var width  = (float)sprite.Texture.Width / sprite.TextureScale;
                    var height = (float)sprite.Texture.Height / sprite.TextureScale;
                    Resources.PixelGrid.Parameters["TextureSize"]?.SetValue(new Vector2(width, height));
                }
                spriteBatch.Draw(sprite.Texture, position, sprite.SourceRectangle, color, 0, origin, drawScale, SpriteEffects.None, 0);
            }

            // If the grid is enabled set up the shader parameters.
            if (useGrid)
            {
                Resources.PixelGrid.Parameters["GridOpacity"]?.SetValue(Game1.PixelGridAlpha);
                Resources.PixelGrid.Parameters["Offset"]?.SetValue(Vector2.Zero);
                SetGrid(true);
            }

            // Set the photobook position.
            var bookPosition = new Vector2(
                Game1.WindowWidth / 2 - (_spriteBook.SourceRectangle.Width * scale) / 2,
                Game1.WindowHeight / 2 - (_spriteBook.SourceRectangle.Height * scale) / 2);

            // Push the photobook up so it doesn't overlap the textbox.
            var textBoxY = Game1.GameManager.InGameOverlay.TextboxOverlay.DialogBoxTextBox.Y;
            var bookBottom = bookPosition.Y + _spriteBook.SourceRectangle.Height * scale;
            if (bookBottom > textBoxY - 6 * scale)
                bookPosition.Y -= bookBottom - (textBoxY - 6 * scale);

            // Draw the book. Keep it aligned to a whole number as the push above can land it on a fraction.
            bookPosition = new Vector2(MathF.Round(bookPosition.X), MathF.Round(bookPosition.Y));
            DrawSprite(_spriteBook, bookPosition, Color.White * transparency, new Vector2(scale), Vector2.Zero);

            // Draw the photo positions on the book.
            for (var i = 0; i < 12; i++)
            {
                // If the image is unlocked show the "OK" sprite.
                var imageSprite = _unlockState[i] ? _spriteOk : _spriteNop;

                // Don't even ask me what those numbers ae doing. 
                var posX = 27 + (i % 2) * 32 + (i / 6) * 88;
                var posY = 19 + ((i % 6) / 2) * 32;
                var vecP = new Vector2(posX, posY) * scale;
                var vecO = new Vector2(imageSprite.SourceRectangle.Width / 2, 0) * scale;
                var position = bookPosition + vecP - vecO;

                // Draw the photo position sprite.
                DrawSprite(imageSprite, position, Color.White * transparency, new Vector2(scale), Vector2.Zero);
            }
            // Draw the cursor. Hide when a photo is shown as the cursor border will
            // be slightly visible when a "right-most" photo is selected and shown.
            if (_transitionValue < 1)
            {
                // Calculate the cursor position and draw it.
                var cursorX = 12 + (_cursorIndex % 2) * 32 + (_cursorIndex / 6) * 88;
                var cursorY = 8 + ((_cursorIndex % 6) / 2) * 32;
                var cursorVecP = new Vector2(cursorX, cursorY) * scale;
                var cursorVecO1 = new Vector2(21, 21) * scale;
                var cursorVecO2 = new Vector2(2, 2) * scale * _cursorState;
                var cursorPosition = bookPosition + cursorVecP + cursorVecO1 - cursorVecO2;
                var cursorFinalPos = new Vector2(MathF.Round(cursorPosition.X), MathF.Round(cursorPosition.Y));

                // Draw the cursor sprite.
                DrawSprite(_spriteCursor, cursorFinalPos, Color.White * transparency, new Vector2(scale), Vector2.Zero);
            }
            // Draw the selected image.
            if (_transitionValue > 0)
            {
                // Draw the photograph.
                var photo    = _spritePhotos[_cursorIndex];
                var startPos = bookPosition + new Vector2(27 + (_cursorIndex % 2) * 32 + (_cursorIndex / 6) * 88, 27 + ((_cursorIndex % 6) / 2) * 32) * scale;

                // Snap the target onto the book's pixel lattice. Centring on the window instead puts the
                // photo's pixels out of phase with the book's, so the two grids do not line up.
                var target = new Vector2(Game1.WindowWidth / 2, Game1.WindowHeight / 2);
                target = bookPosition + new Vector2(
                    MathF.Round((target.X - bookPosition.X) / scale) * scale,
                    MathF.Round((target.Y - bookPosition.Y) / scale) * scale);

                var position = Vector2.Lerp(startPos, target, _transitionValue);
                var color    = Color.White * transparency * _transitionValue;
                var origin   = new Vector2(photo.SourceRectangle.Width / 2f, photo.SourceRectangle.Height / 2f);
                var vecscale = new Vector2(scale * (0.1f + _transitionValue * 0.9f));

                // Hide the grid while the photo is zooming in or out.
                var zooming = _transitionValue < 1;

                if (zooming)
                    SetGrid(false);
                else
                    origin = new Vector2(MathF.Round(origin.X), MathF.Round(origin.Y));

                // Draw the currently selected photograph.
                DrawSprite(photo, position, color, vecscale, origin);

                if (zooming)
                    SetGrid(true);

                // Hide the buttons and text until transition is finished.
                if (_transitionCounter >= 125 && !_hideButtons)
                {
                    // Set the button text based on the selected controller. 
                    var (buttonSpriteTop, buttonSpriteBot) = GameSettings.Controller switch
                    {
                        "XBox"        => (_spriteText_A, _spriteText_B),
                        "Nintendo"    => (_spriteText_B, _spriteText_A),
                        "Playstation" => (_spriteText_X, _spriteText_O),
                        _             => (_spriteText_A, _spriteText_B),
                    };
                    // Calculate center position.
                    var centerX = Game1.WindowWidth / 2;
                    var centerY = Game1.WindowHeight / 2;
                    var white = Color.White * transparency;

                    // Draw the red button and its text.
                    DrawSprite(_spriteButtonRed, new Vector2(centerX + 24 * scale, centerY + 32 * scale), white, new Vector2(scale), Vector2.Zero);
                    DrawSprite(buttonSpriteTop, new Vector2(centerX + 29 * scale, centerY + 35 * scale), white, new Vector2(scale), Vector2.Zero);

                    // Draw the yellow button and its text.
                    DrawSprite(_spriteButtonYellow, new Vector2(centerX + 24 * scale, centerY + 48 * scale), white, new Vector2(scale), Vector2.Zero);
                    DrawSprite(buttonSpriteBot, new Vector2(centerX + 29 * scale, centerY + 51 * scale), white, new Vector2(scale), Vector2.Zero);

                    // Draw the "Print" and "Cancel" text.
                    DrawSprite(_spriteCancel, new Vector2(centerX + 41 * scale, centerY + 50 * scale), white, new Vector2(scale), Vector2.Zero);
                    DrawSprite(_spritePrint, new Vector2(centerX + 41 * scale, centerY + 34 * scale), white, new Vector2(scale), Vector2.Zero);
                }
            }
            // Disable the grid as we don't want to draw it for the textbox.
            SetGrid(false);

            // After textbox overlay closes and a photo is not being shown.
            if (!Game1.GameManager.InGameOverlay.TextboxOverlay.IsOpen && !_isShowingImage)
            {
                // Draw the hint textbox background.
                var uiScale = Game1.UiScale;
                var textboxRef = Game1.GameManager.InGameOverlay.TextboxOverlay.DialogBoxTextBox;
                spriteBatch.Draw(Resources.SprWhite, textboxRef, Values.TextboxBackgroundColor * transparency);

                // Build the textbox string.
                var confirmString = Game1.LanguageManager.GetString("photo_book_select", "error");
                var cancelString = Game1.LanguageManager.GetString("photo_book_cancel", "error");
                var textBoxString =  confirmString + "\n" + cancelString;

                // Draw the hint text.
                var textX = textboxRef.X + 5 * uiScale;
                var textY = textboxRef.Y + 5 * uiScale;
                var textPos = new Vector2(textX, textY);
                DrawHelper.DrawString(spriteBatch, textBoxString, textPos, _textboxFontColor * transparency, 0, Vector2.Zero, uiScale, SpriteEffects.None, 0);
            }
        }
    }
}
