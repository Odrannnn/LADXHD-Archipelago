using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using NativeFileDialogSharp;
using ProjectZ.Base;
using ProjectZ.InGame.Things;

namespace ProjectZ
{
    internal sealed class DesktopVKPlatformWindow : IPlatformWindow
    {
        private bool _restoreBorderlessBackBuffer;
        private bool _restoreWindowPosition;
        private bool _hasWindowedPosition;
        private bool _applyingChanges;
        private Point _windowedPosition;

        public bool SupportsFullscreen => true;
        public bool SupportsFullscreenConfiguration => true;
        public bool SupportsInactiveWindowInput => true;
        public bool ForceFullscreen => false;
        public bool VerticalFlipBlur => false;

        public void Initialize(Game game)
        {
            if (game is not Game1 projectZGame)
                return;

            game.Window.KeyDown += (_, e) =>
            {
                // F11 toggles fullscreen without requiring a modifier.
                if (e.Key == Keys.F11)
                {
                    projectZGame.HandleFullscreenHotkey();
                    return;
                }

                var keyState = Keyboard.GetState();
                bool altDown = keyState.IsKeyDown(Keys.LeftAlt) || keyState.IsKeyDown(Keys.RightAlt);
                if (!altDown)
                    return;

                // Handle Alt+F4 explicitly so input state is cleared before exiting.
                if (e.Key == Keys.F4)
                {
                    InputHandler.ResetInputState();
                    projectZGame.Exit();
                }

                // Alt+Enter is the alternate fullscreen shortcut.
                else if (e.Key == Keys.Enter)
                {
                    projectZGame.HandleFullscreenHotkey();
                }
            };

            game.Window.TextInput += (_, e) => InputHandler.ReceiveTextInput(e.Character);
        }

        public bool TrySetFullscreen(Game game, int screenMode)
        {
            // Remember where the window sat before going fullscreen so it can be put back.
            if (screenMode > 0 && !Game1.FullScreen)
            {
                _windowedPosition = game.Window.Position;
                _hasWindowedPosition = true;
            }
            else if (screenMode == 0 && Game1.FullScreen && _hasWindowedPosition)
            {
                _restoreWindowPosition = true;
            }

            if (screenMode == 1)
            {
                var display = Game1.Graphics.GraphicsDevice.Adapter.CurrentDisplayMode;
                Game1.Graphics.PreferredBackBufferWidth = display.Width;
                Game1.Graphics.PreferredBackBufferHeight = display.Height;
            }

            // Let Game1.ToggleFullscreen drive the actual mode change.
            return false;
        }

        public void OnGraphicsDeviceReset(Game game)
        {
            if (_applyingChanges)
                return;

            var graphics = Game1.Graphics;
            if (GameSettings.ScreenMode != 1 || !graphics.IsFullScreen || graphics.HardwareModeSwitch)
                return;

            var client = game.Window.ClientBounds;
            if (client.Width <= 0 || client.Height <= 0)
                return;

            _restoreBorderlessBackBuffer =
                graphics.PreferredBackBufferWidth != client.Width ||
                graphics.PreferredBackBufferHeight != client.Height;
        }

        public void ApplyPendingChanges(Game game)
        {
            if (_applyingChanges)
                return;

            var graphics = Game1.Graphics;

            if (_restoreWindowPosition)
            {
                _restoreWindowPosition = false;
                if (GameSettings.ScreenMode == 0)
                    game.Window.Position = _windowedPosition;
            }

            if (GameSettings.ScreenMode == 0 && !graphics.IsFullScreen)
            {
                var bounds = game.Window.ClientBounds;
                if (bounds.Width > 0 && bounds.Height > 0)
                {
                    // Match the minimum-size clamp in Game1.OnResize, or the two fight each other.
                    var width = Math.Max(bounds.Width, Values.MinWidth);
                    var height = Math.Max(bounds.Height, Values.MinHeight);

                    if (graphics.PreferredBackBufferWidth != width ||
                        graphics.PreferredBackBufferHeight != height)
                    {
                        _applyingChanges = true;
                        try
                        {
                            graphics.PreferredBackBufferWidth = width;
                            graphics.PreferredBackBufferHeight = height;
                            graphics.ApplyChanges();
                        }
                        finally
                        {
                            _applyingChanges = false;
                        }
                    }
                }
                return;
            }

            if (!_restoreBorderlessBackBuffer)
                return;

            _restoreBorderlessBackBuffer = false;

            if (GameSettings.ScreenMode != 1 || !graphics.IsFullScreen || graphics.HardwareModeSwitch)
                return;

            var client = game.Window.ClientBounds;
            if (client.Width <= 0 || client.Height <= 0)
                return;

            if (graphics.PreferredBackBufferWidth == client.Width &&
                graphics.PreferredBackBufferHeight == client.Height)
                return;

            _applyingChanges = true;
            try
            {
                graphics.PreferredBackBufferWidth = client.Width;
                graphics.PreferredBackBufferHeight = client.Height;
                graphics.ApplyChanges();
            }
            finally
            {
                _applyingChanges = false;
            }
        }

        public void Exit(Game game) => game.Exit();
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
