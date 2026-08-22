using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using SDL2;
using static LADXHD_Launcher.UiNavigator;

namespace LADXHD_Launcher
{
    public interface IControllerPage
    {
        void FocusInitial();
        void OnCancel();
        void FocusBack();
    }

    public interface IControllerDialog
    {
        void FocusDefault();
    }

    public static class ControllerInput
    {
        // SDL controller constants.
        private const int TRIGGER_THRESHOLD = 16000;
        private const int DEADZONE = 16000;
        private const uint SDL_INIT_GAMECONTROLLER = 0x00002000;

        // SDL controller button constants.
        private const SDL.SDL_GameControllerButton A      = SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_A;
        private const SDL.SDL_GameControllerButton B      = SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_B;
        private const SDL.SDL_GameControllerButton X      = SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_X;
        private const SDL.SDL_GameControllerButton Y      = SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_Y;
        private const SDL.SDL_GameControllerButton LB     = SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_LEFTSHOULDER;
        private const SDL.SDL_GameControllerButton RB     = SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_RIGHTSHOULDER;
        private const SDL.SDL_GameControllerButton DUP    = SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_UP;
        private const SDL.SDL_GameControllerButton DDOWN  = SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_DOWN;
        private const SDL.SDL_GameControllerButton DLEFT  = SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_LEFT;
        private const SDL.SDL_GameControllerButton DRIGHT = SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_RIGHT;

        private const SDL.SDL_GameControllerAxis AXIS_LX  = SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_LEFTX;
        private const SDL.SDL_GameControllerAxis AXIS_LY  = SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_LEFTY;
        private const SDL.SDL_GameControllerAxis AXIS_RY  = SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_RIGHTY;
        private const SDL.SDL_GameControllerAxis AXIS_LT  = SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_TRIGGERLEFT;
        private const SDL.SDL_GameControllerAxis AXIS_RT  = SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_TRIGGERRIGHT;

        // How often the background loop runs.
        private const int PollMs = 16, InitialDelayMs = 350, RepeatMs = 110;

        // How often we rescan for newly connected / disconnected controllers.
        private const int ScanMs = 250;

        // Rate that right analog stick scrolls the window.
        private const double MaxScrollPerTick = 22.0;

        // Controller input runs on a thread.
        private static Thread? _thread;
        private static volatile bool _running;

        // Per-controller edge-detection state, keyed by SDL joystick instance ID
        // (stable across reconnects/index-shuffles, unlike device index).
        private sealed class PadState
        {
            public IntPtr Handle;
            public readonly bool[] PrevButtons = new bool[16];
            public bool PrevLT, PrevRT;
            public NavAction? HeldDir;
            public long NextFire;
        }

        private static readonly Dictionary<int, PadState> _pads = new();

        static ControllerInput() => NativeLibrary.SetDllImportResolver(typeof(ControllerInput).Assembly, Resolve);

        private static IntPtr Resolve(string name, Assembly asm, DllImportSearchPath? path)
        {
            if (name != "SDL2") return IntPtr.Zero;
            string[] candidates = OperatingSystem.IsWindows() ? new[] { "SDL2.dll" }
                : OperatingSystem.IsMacOS()                   ? new[] { "libSDL2-2.0.0.dylib", "libSDL2.dylib" }
                                                              : new[] { "libSDL2-2.0.so.0", "libSDL2.so" };
            foreach (var c in candidates)
                if (NativeLibrary.TryLoad(c, out var h)) return h;
            return IntPtr.Zero;
        }

        public static void Start()
        {
            if (_running) return;
            _running = true;
            _thread = new Thread(Loop) { IsBackground = true, Name = "ControllerInput" };
            _thread.Start();
        }

        public static void Stop() => _running = false;

        private static void Loop()
        {
            SDL.SDL_SetHint(SDL.SDL_HINT_JOYSTICK_THREAD, "1");
            SDL.SDL_SetHint(SDL.SDL_HINT_JOYSTICK_ALLOW_BACKGROUND_EVENTS, "1");
            try { if (SDL.SDL_Init(SDL_INIT_GAMECONTROLLER) != 0) { Debug.WriteLine("SDL_Init: " + SDL.SDL_GetError()); return; } }
            catch (Exception ex) { Debug.WriteLine("SDL native failed: " + ex); return; }

            var sw = Stopwatch.StartNew();
            long nextScan = 0;

            while (_running)
            {
                long now = sw.ElapsedMilliseconds;

                if (now >= nextScan)
                {
                    RefreshPads();
                    nextScan = now + ScanMs;
                }

                SDL.SDL_PumpEvents();
                SDL.SDL_GameControllerUpdate();

                foreach (var state in _pads.Values)
                    PollPad(state, now);

                Thread.Sleep(PollMs);
            }

            foreach (var state in _pads.Values)
                SDL.SDL_GameControllerClose(state.Handle);
            _pads.Clear();

            SDL.SDL_Quit();
        }

        // Opens any newly connected controllers and drops any that unplugged.
        private static void RefreshPads()
        {
            if (_pads.Count > 0)
            {
                var stale = new List<int>();
                foreach (var kv in _pads)
                    if (SDL.SDL_GameControllerGetAttached(kv.Value.Handle) == SDL.SDL_bool.SDL_FALSE)
                        stale.Add(kv.Key);

                foreach (var id in stale)
                {
                    SDL.SDL_GameControllerClose(_pads[id].Handle);
                    _pads.Remove(id);
                    Debug.WriteLine($"pad {id} disconnected");
                }
            }

            int n = SDL.SDL_NumJoysticks();
            for (int i = 0; i < n; i++)
            {
                if (SDL.SDL_IsGameController(i) != SDL.SDL_bool.SDL_TRUE) continue;

                int instanceId = SDL.SDL_JoystickGetDeviceInstanceID(i);
                if (_pads.ContainsKey(instanceId)) continue;

                IntPtr handle = SDL.SDL_GameControllerOpen(i);
                if (handle == IntPtr.Zero) continue;

                _pads[instanceId] = new PadState { Handle = handle };
                Debug.WriteLine($"opened pad {i} (instance {instanceId})");
            }
        }

        // Reads one controller's state for this tick and fires nav actions off of it.
        private static void PollPad(PadState state, long now)
        {
            IntPtr pad = state.Handle;

            // Directional: d-pad or left stick, one axis at a time, with hold-to-repeat.
            NavAction? dir = ReadDirection(pad);
            if (dir != state.HeldDir)
            {
                state.HeldDir = dir;
                if (dir is NavAction d) { Fire(d); state.NextFire = now + InitialDelayMs; }
            }
            else if (dir is NavAction d2 && now >= state.NextFire)
            {
                Fire(d2); state.NextFire = now + RepeatMs;
            }

            // Face/shoulder buttons: edge-triggered (fire once per press).
            if (GameSettings.SwapButtons)
            {
                Edge(pad, B,  state.PrevButtons, NavAction.Accept);
                Edge(pad, A,  state.PrevButtons, NavAction.Cancel);
            }
            else
            {
                Edge(pad, A,  state.PrevButtons, NavAction.Accept);
                Edge(pad, B,  state.PrevButtons, NavAction.Cancel);
            }
            Edge(pad, X,  state.PrevButtons, NavAction.FocusBack);
            Edge(pad, Y,  state.PrevButtons, NavAction.ShowTip);
            Edge(pad, RB, state.PrevButtons, NavAction.AdjustUp);
            Edge(pad, LB, state.PrevButtons, NavAction.AdjustDown);

            // Triggers are analog; edge-trigger a one-shot category skip on threshold crossing.
            bool ltDown = SDL.SDL_GameControllerGetAxis(pad, AXIS_LT) > TRIGGER_THRESHOLD;
            bool rtDown = SDL.SDL_GameControllerGetAxis(pad, AXIS_RT) > TRIGGER_THRESHOLD;
            if (rtDown && !state.PrevRT) Fire(NavAction.CategoryNext);
            if (ltDown && !state.PrevLT) Fire(NavAction.CategoryPrev);
            state.PrevRT = rtDown;
            state.PrevLT = ltDown;

            // Right stick: analog free-scroll of the active page, like a mouse wheel.
            int ry = SDL.SDL_GameControllerGetAxis(pad, AXIS_RY);
            if (Math.Abs(ry) > DEADZONE)
            {
                double norm  = (Math.Abs(ry) - DEADZONE) / (32767.0 - DEADZONE);
                double delta = Math.Sign(ry) * norm * MaxScrollPerTick;
                PostScroll(delta);
            }
        }

        private static NavAction? ReadDirection(IntPtr pad)
        {
            bool up    = Btn(pad, DUP),    down  = Btn(pad, DDOWN);
            bool left  = Btn(pad, DLEFT),  right = Btn(pad, DRIGHT);
            short lx = SDL.SDL_GameControllerGetAxis(pad, AXIS_LX);
            short ly = SDL.SDL_GameControllerGetAxis(pad, AXIS_LY);
            if (lx < -DEADZONE) left = true; else if (lx > DEADZONE) right = true;
            if (ly < -DEADZONE) up = true;   else if (ly > DEADZONE) down = true;

            if (up && !down)    return NavAction.Up;
            if (down && !up)    return NavAction.Down;
            if (left && !right) return NavAction.Left;
            if (right && !left) return NavAction.Right;
            return null;
        }
        private static void Edge(IntPtr pad, SDL.SDL_GameControllerButton btn, bool[] prev, NavAction action)
        {
            bool down = Btn(pad, btn);
            if (down && !prev[(int)btn]) Fire(action);
            prev[(int)btn] = down;
        }

        private static bool Btn(IntPtr pad, SDL.SDL_GameControllerButton b) => SDL.SDL_GameControllerGetButton(pad, b) == 1;

        private static void Fire(NavAction action) => Dispatcher.UIThread.Post(() =>
        {
            var w = ActiveWindow();
            if (w is null) return;

            bool wasShown = w.Classes.Contains("nav-active");
            if (!wasShown)
            {
                w.Classes.Add("nav-active");

                if (w is IControllerDialog dlg)
                    Dispatcher.UIThread.Post(() => dlg.FocusDefault(), DispatcherPriority.Background);

                return;
            }
            Dispatch(action, w);
        });

        private static void PostScroll(double delta) => Dispatcher.UIThread.Post(() =>
        {
            var w = ActiveWindow();
            if (w is not null) UiNavigator.Scroll(w, delta);
        });

        private static Window? ActiveWindow()
        {
            if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime d)
                return null;

            foreach (var w in d.Windows)
                if (w.IsActive) 
                    return w;

            if (d.Windows.Count == 1) 
                return d.Windows[0];

            return _lastKnown;
        }

        private static readonly List<Window> _windowStack = new();
        private static Window? _lastKnown;

        public static void RegisterWindow(Window window)
        {
            window.Opened += (_, _) => { lock (_windowStack) { _windowStack.Remove(window); _windowStack.Add(window); _lastKnown = window; } };
            window.Closed += (_, _) => { lock (_windowStack) { _windowStack.Remove(window); _lastKnown = _windowStack.Count > 0 ? _windowStack[^1] : null; } };
            window.Activated += (_, _) => { lock (_windowStack) { _windowStack.Remove(window); _windowStack.Add(window); _lastKnown = window; } };
        }
    }
}