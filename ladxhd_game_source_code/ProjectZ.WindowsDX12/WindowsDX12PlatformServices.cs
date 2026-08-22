using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using NativeFileDialogSharp;
using ProjectZ.Base;
using ProjectZ.InGame.Things;

namespace ProjectZ
{
    internal sealed class WindowsDx12PlatformWindow : IPlatformWindow
    {
        private bool _restoreWindowPosition;
        private bool _hasWindowedPosition;
        private Point _windowedPosition;

        public bool SupportsFullscreen => true;
        public bool SupportsFullscreenConfiguration => true;
        public bool SupportsInactiveWindowInput => true;
        public bool ForceFullscreen => false;

        // Not OpenGL: render targets are not vertically flipped, so the blur is sampled as-is.
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
            // Remember where the window was before leaving windowed mode so it can be
            // put back in the same spot, then let Game1.ToggleFullscreen do the work.
            if (screenMode > 0 && !Game1.FullScreen)
            {
                _windowedPosition = game.Window.Position;
                _hasWindowedPosition = true;
            }
            else if (screenMode == 0 && Game1.FullScreen && _hasWindowedPosition)
            {
                _restoreWindowPosition = true;
            }

            return false;
        }

        public void OnGraphicsDeviceReset(Game game)
        {
            // Nothing to do: ApplyPendingChanges re-checks the back buffer every frame,
            // so this does not depend on the backend raising DeviceReset at all.
        }

        public void ApplyPendingChanges(Game game)
        {
            if (_restoreWindowPosition)
            {
                _restoreWindowPosition = false;
                if (GameSettings.ScreenMode == 0)
                    game.Window.Position = _windowedPosition;
            }

            var graphics = Game1.Graphics;

            // Exclusive fullscreen owns the back buffer (it is set to the display mode),
            // so leave it alone. Windowed and borderless both track the client area.
            if (graphics.IsFullScreen && graphics.HardwareModeSwitch)
                return;

            var client = game.Window.ClientBounds;
            if (client.Width <= 0 || client.Height <= 0)
                return;

            // The native backend does not update PreferredBackBuffer when the user resizes
            // the window, and Game1.Update calls ApplyChanges() every frame - which would
            // otherwise force the back buffer back to its old size and freeze the UI scale.
            if (graphics.PreferredBackBufferWidth == client.Width &&
                graphics.PreferredBackBufferHeight == client.Height)
                return;

            graphics.PreferredBackBufferWidth = client.Width;
            graphics.PreferredBackBufferHeight = client.Height;
            graphics.ApplyChanges();
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