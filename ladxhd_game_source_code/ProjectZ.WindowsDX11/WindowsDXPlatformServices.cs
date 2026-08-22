using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Microsoft.Xna.Framework.Input;
using NativeFileDialogSharp;
using ProjectZ.Base;
using ProjectZ.InGame.Things;
using XnaGame = Microsoft.Xna.Framework.Game;
using XnaKeys = Microsoft.Xna.Framework.Input.Keys;

namespace ProjectZ
{
    internal sealed class WindowsDxPlatformWindow : IPlatformWindow
    {
        private Form _form;
        private Rectangle _lastBounds;
        private int _lastClientWidth;
        private int _lastClientHeight;
        private bool _isBorderless;

        public bool SupportsFullscreen => true;
        public bool SupportsFullscreenConfiguration => true;
        public bool SupportsInactiveWindowInput => true;
        public bool ForceFullscreen => false;
        public bool VerticalFlipBlur => false;

        public void Initialize(XnaGame game)
        {
            _form = Form.FromHandle(game.Window.Handle) as Form;
            if (_form == null)
                return;

            // DirectX requires this WinForms workaround to set the window icon.
            var iconPath = Path.Combine("Data", "Icon", "Icon.ico");
            if (File.Exists(iconPath))
                _form.Icon = new Icon(iconPath);
            _form.ShowIcon = true;
            _lastBounds = _form.Bounds;

            // Keep backend-specific window events out of the shared game assembly.
            if (game is Game1 projectZGame)
            {
                game.Window.KeyDown += (_, e) => HandleKeyDown(projectZGame, e.Key);
                game.Window.TextInput += (_, e) => InputHandler.ReceiveTextInput(e.Character);
            }
        }

        public bool TrySetFullscreen(XnaGame game, int screenMode)
        {
            if (_form == null)
                return false;

            var graphics = Game1.Graphics;

            // Borderless fullscreen
            if (screenMode == 1)
            {
                // Only save bounds when coming from windowed mode, not from exclusive fullscreen.
                if (!_isBorderless && !Game1.WasExclusive)
                {
                    _lastClientWidth = graphics.PreferredBackBufferWidth;
                    _lastClientHeight = graphics.PreferredBackBufferHeight;
                    _lastBounds = _form.Bounds;
                }

                // If coming from exclusive fullscreen, exit it first.
                if (Game1.WasExclusive)
                {
                    graphics.IsFullScreen = false;
                    graphics.ApplyChanges();
                }

                var bounds = Screen.GetBounds(_form);

                _form.FormBorderStyle = FormBorderStyle.None;
                _form.WindowState = FormWindowState.Normal;
                _form.Bounds = bounds;

                graphics.PreferredBackBufferWidth = bounds.Width;
                graphics.PreferredBackBufferHeight = bounds.Height;
                graphics.ApplyChanges();

                Game1.WindowWidth = bounds.Width;
                Game1.WindowHeight = bounds.Height;
                Game1.ScaleChanged = true;
                _isBorderless = true;
                Game1.WasExclusive = false;
                Game1.FullScreen = true;
            }

            // Exclusive fullscreen
            else if (screenMode == 2)
            {
                // Only save bounds when coming from windowed mode, not from borderless fullscreen.
                if (!_isBorderless && !Game1.WasExclusive)
                {
                    _lastClientWidth = graphics.PreferredBackBufferWidth;
                    _lastClientHeight = graphics.PreferredBackBufferHeight;
                    _lastBounds = _form.Bounds;
                }

                // If coming from borderless fullscreen, restore the WinForms window first.
                if (_isBorderless)
                {
                    _form.FormBorderStyle = FormBorderStyle.Sizable;
                    _form.Bounds = RestoreBounds();
                    _isBorderless = false;
                }

                var display = graphics.GraphicsDevice.Adapter.CurrentDisplayMode;
                graphics.PreferredBackBufferWidth = display.Width;
                graphics.PreferredBackBufferHeight = display.Height;
                graphics.HardwareModeSwitch = true;
                graphics.IsFullScreen = true;
                graphics.ApplyChanges();

                Game1.WasExclusive = true;
                Game1.FullScreen = true;
            }

            // Windowed mode
            else
            {
                GameSettings.ScreenMode = 0;
                _form.FormBorderStyle = FormBorderStyle.Sizable;
                _form.Bounds = RestoreBounds();
                _isBorderless = false;

                // Use the saved client size, not the outer window bounds with its title bar and borders.
                var width = _lastClientWidth > 0 ? _lastClientWidth : Values.MinWidth * 3;
                var height = _lastClientHeight > 0 ? _lastClientHeight : Values.MinHeight * 3;

                graphics.PreferredBackBufferWidth = width;
                graphics.PreferredBackBufferHeight = height;
                graphics.IsFullScreen = false;
                graphics.ApplyChanges();

                Game1.WindowWidth = width;
                Game1.WindowHeight = height;
                Game1.ScaleChanged = true;
                Game1.WasExclusive = false;
                Game1.FullScreen = false;
            }

            Game1.GameManager?.UpdateRenderTargets();
            return true;
        }

        public void OnGraphicsDeviceReset(XnaGame game) { }
        public void ApplyPendingChanges(XnaGame game) { }

        public void Exit(XnaGame game) => game.Exit();

        private static void HandleKeyDown(Game1 game, XnaKeys key)
        {
            // F11 toggles fullscreen without requiring a modifier.
            if (key == XnaKeys.F11)
            {
                game.HandleFullscreenHotkey();
                return;
            }

            var keyState = Keyboard.GetState();
            bool altDown = keyState.IsKeyDown(XnaKeys.LeftAlt) || keyState.IsKeyDown(XnaKeys.RightAlt);
            if (!altDown)
                return;

            // DirectX does not reliably process Alt+F4 itself, so reset input and exit explicitly.
            if (key == XnaKeys.F4)
            {
                InputHandler.ResetInputState();
                game.Exit();
            }

            // Alt+Enter is the alternate fullscreen shortcut.
            else if (key == XnaKeys.Enter)
            {
                game.HandleFullscreenHotkey();
            }
        }

        private Rectangle RestoreBounds() => _lastBounds.Width > 0 && _lastBounds.Height > 0
            ? _lastBounds
            : new Rectangle(100, 100, Values.MinWidth * 3, Values.MinHeight * 3);
    }

    internal sealed class NativeFileDialogService : IFileDialogService
    {
        public bool TryOpen(string extension, string defaultPath, out string path)
        {
            var result = Dialog.FileOpen(extension, defaultPath);
            path = result.IsOk ? result.Path : null;
            return result.IsOk;
        }

        public bool TrySave(string extension, string defaultPath, out string path)
        {
            var result = Dialog.FileSave(extension, defaultPath);
            path = result.IsOk ? result.Path : null;
            return result.IsOk;
        }

        public IReadOnlyList<string> OpenMultiple(string extension)
        {
            var result = Dialog.FileOpenMultiple(extension);
            return result.IsOk ? result.Paths : [];
        }
    }
}
