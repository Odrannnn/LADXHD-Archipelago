using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using NativeFileDialogSharp;
using ProjectZ.Base;
using ProjectZ.InGame.Things;

namespace ProjectZ
{
    internal sealed class DesktopGLTextInputService : ITextInputService
    {
        public DesktopGLTextInputService()
        {
            // Resolve SDL explicitly so the text-input calls use MonoGame's native library.
            NativeLibrary.SetDllImportResolver(typeof(DesktopGLTextInputService).Assembly, ResolveSdl2);
        }

        [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)]
        private static extern void SDL_StopTextInput();

        [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)]
        private static extern void SDL_StartTextInput();

        public void SetEnabled(bool enabled)
        {
            if (enabled)
                SDL_StartTextInput();
            else
                SDL_StopTextInput();
        }

        public void OnGameActivated() => SetEnabled(InputHandler.WantsTextInput);

        private static IntPtr ResolveSdl2(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
        {
            if (libraryName != "SDL2")
                return IntPtr.Zero;

            // Pick the SDL library name used by the current operating system.
            string[] candidates =
                OperatingSystem.IsWindows() ? ["SDL2.dll"] :
                OperatingSystem.IsLinux() ? ["libSDL2-2.0.so.0", "libSDL2.so"] :
                OperatingSystem.IsMacOS() ? ["libSDL2-2.0.0.dylib", "libSDL2.dylib"] :
                [];

            foreach (var candidate in candidates)
                if (NativeLibrary.TryLoad(candidate, out var handle))
                    return handle;

            return IntPtr.Zero;
        }
    }

    internal sealed class DesktopGlPlatformWindow : IPlatformWindow
    {
        private bool _restoreBorderlessBackBuffer;
        private bool _restoreWindowPosition;
        private bool _hasWindowedPosition;
        private Point _windowedPosition;

        public bool SupportsFullscreen => true;
        public bool SupportsFullscreenConfiguration => true;
        public bool SupportsInactiveWindowInput => true;
        public bool ForceFullscreen => false;
        public bool VerticalFlipBlur => true;

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
            if (!OperatingSystem.IsWindows())
                return false;

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
            var graphics = Game1.Graphics;
            if (GameSettings.ScreenMode != 1 || !graphics.IsFullScreen || graphics.HardwareModeSwitch)
                return;

            var client = game.Window.ClientBounds;
            if (client.Width <= 0 || client.Height <= 0)
                return;

            // Desktop fullscreen uses the full SDL window size as its request. SDL then
            // derives the actual drawable viewport, including platform safe-area insets.
            _restoreBorderlessBackBuffer =
                graphics.PreferredBackBufferWidth != client.Width ||
                graphics.PreferredBackBufferHeight != client.Height;
        }

        public void ApplyPendingChanges(Game game)
        {
            if (_restoreWindowPosition)
            {
                _restoreWindowPosition = false;
                if (GameSettings.ScreenMode == 0)
                    game.Window.Position = _windowedPosition;
            }

            if (!_restoreBorderlessBackBuffer)
                return;

            _restoreBorderlessBackBuffer = false;

            var graphics = Game1.Graphics;
            if (GameSettings.ScreenMode != 1 || !graphics.IsFullScreen || graphics.HardwareModeSwitch)
                return;

            var client = game.Window.ClientBounds;
            if (client.Width <= 0 || client.Height <= 0)
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
