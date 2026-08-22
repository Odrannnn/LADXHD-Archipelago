using System;
using System.Diagnostics;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace LADXHD_Launcher;

public partial class MainWindow : Window
{
    // The various windows.
    private HomeView _homeView;
    private ModsView _modsView;
    private SettingsView _settingsView;

    // Public references to the various windows.
    public HomeView HomeView => _homeView;
    public ModsView ModsView => _modsView;
    public SettingsView SettingsView => _settingsView;

    // Timers used on Mods window when loading the window or saving options.
    private DispatcherTimer _loadingTimer;
    private DispatcherTimer _savedTimer;

    // Notifications when saving or resetting options.
    public enum NotificationType { Save, Reset }

    // Used in navigator to move back a page.
    public UserControl? CurrentPage => (UserControl?)PageContent.Content;

    public void NavigateTo(UserControl page)
    {
        UiNavigator.InvalidateCandidates();
        PageContent.Content = page;
        Dispatcher.UIThread.Post(() => (page as IControllerPage)?.FocusInitial(), DispatcherPriority.Loaded);
    }

    public MainWindow()
    {
        // Initialize window, config, sound player, and attach UI navigator.
        InitializeComponent();
        Config.Initialize();
        Config.LoadLauncherConfig();
        SoundPlayer.Initialize();

        // Supress sounds before creating windows then enable them again.
        SoundPlayer.SuppressSound = true;
        _homeView = new HomeView(this);
        _settingsView = new SettingsView(this);
        _modsView = new ModsView(this);
        PageContent.Content = _homeView;
        SoundPlayer.SuppressSound = false;
        Dispatcher.UIThread.Post(() => _homeView.FocusInitial(), DispatcherPriority.Loaded);

        // Locks the size of the window.
        Height = Math.Clamp(App.SavedWindowHeight, 400, 768);

        // Add event handlers to the dialog window.
        this.SizeChanged += (s, e) => Config.SaveLauncherConfig();
        this.KeyDown += MainWindow_KeyDown;
        this.PropertyChanged += PropertyChanged_ForceWindowMode;
        this.AddHandler(PointerPressedEvent, (_, _) => Classes.Remove("nav-active"), RoutingStrategies.Tunnel);
        this.AddHandler(KeyDownEvent, KeyDown_HideNavigation, RoutingStrategies.Tunnel);

        // Add global event handlers for specific controls.
        CheckBox.IsCheckedChangedEvent.AddClassHandler<CheckBox>(
            (cb, e) => SoundPlayer.PlayXnbSound(SoundPlayer.SoundClick));
        ComboBox.SelectionChangedEvent.AddClassHandler<ComboBox>(
            (cb, e) => SoundPlayer.PlayXnbSound(SoundPlayer.SoundSelect));
        NumericUpDown.ValueChangedEvent.AddClassHandler<NumericUpDown>(
            (cb, e) => SoundPlayer.PlayXnbSound(SoundPlayer.SoundClick));
        NumericUpDown.PointerWheelChangedEvent.AddClassHandler<NumericUpDown>(
            NumericUpDown_PointerWheelChanged, RoutingStrategies.Bubble, true);

        // Check if the Achievement images are missing.
        var achievementPath = Path.Combine(Config.DataPath, "Achievements");
        if (!achievementPath.TestPath() || achievementPath.GetFiles("*", true).Count < Config.AchievementCount)
            Config.InstallAchievementImages(false);

        // Check online for an update now that the window has been fully constructed.
        Config.CheckForUpdateAsync();
        ControllerInput.Start();
        ControllerInput.RegisterWindow(this);
    }

    private void PropertyChanged_ForceWindowMode(object sender, AvaloniaPropertyChangedEventArgs e)
    {
        // If the window is maximized force it back to a window.
        if (e.Property == WindowStateProperty && WindowState == WindowState.Maximized)
            WindowState = WindowState.Normal;
    }

    private void NumericUpDown_PointerWheelChanged(NumericUpDown nud, PointerWheelEventArgs e)
    {
        // Validate we're working with a scrolling wheel.
        if (e is not PointerWheelEventArgs pw)
            return;
        if (pw.Delta.Y == 0)
            return;

        // Each notch moves by the control's own increment.
        decimal step    = nud.Increment == 0 ? 1 : nud.Increment;
        decimal current = nud.Value ?? 0;
        decimal next    = current + (pw.Delta.Y > 0 ? step : -step);

        // Respect the control's configured bounds.
        nud.Value = Math.Clamp(next, nud.Minimum, nud.Maximum);

        // Stop the page's ScrollViewer from also scrolling while we're adjusting.
        pw.Handled = true;
    }

    private void KeyDown_HideNavigation(object? sender, KeyEventArgs e)
    {
        // The selection keys are excempt from hiding the selection.
        var k = e.Key;
        bool isNavStandIn = k is Key.NumPad8 or Key.NumPad2
            or Key.NumPad4 or Key.NumPad6 or Key.NumPad5
            or Key.NumPad0 or Key.Add or Key.Subtract;

        // Hide the navigator selection when pressing most keys.
        if (!isNavStandIn)
            Classes.Remove("nav-active");
    }

    private void MainWindow_KeyDown(object sender, KeyEventArgs e)
    {
        // Forces an update by pressing Shift + F1.
        if (e.Key == Key.F1 && e.KeyModifiers == KeyModifiers.Shift)
            _homeView.ForceUpdate();

        // Opens the saves folder by pressing Shift + F2.
        if (e.Key == Key.F2 && e.KeyModifiers == KeyModifiers.Shift)
        {
            // Same location the game writes saves to, on every platform.
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Zelda_LA");

            // If the path doesn't exist then just exit.
            if (!Directory.Exists(path))
                return;

            // Each OS has its own way to open the folder.
            try
            {
                var psi = new ProcessStartInfo { UseShellExecute = false };
            #if WINDOWS
                psi.FileName = "explorer.exe";
            #elif LINUX
                psi.FileName = "xdg-open";
            #elif MACOS
                psi.FileName = "open";
            #endif
                psi.ArgumentList.Add(path);
                Process.Start(psi);
            }
            catch (Exception ex) { }
        }
    }

    public void ShowLoadingMessage()
    {
        LoadingNotification.Opacity = 1.0;
    }

    public void HideLoadingMessage()
    {
        double opacity = 1.0;
        _loadingTimer?.Stop();
        _loadingTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        _loadingTimer.Tick += (s, e) =>
        {
            opacity -= 0.05;
            if (opacity <= 0)
            {
                opacity = 0;
                _loadingTimer.Stop();
            }
            LoadingNotification.Opacity = opacity;
        };
        _loadingTimer.Start();
    }

    public void HideNotifications()
    {
        _savedTimer?.Stop();
        SavedNotification.Opacity = 0;
        ResetNotification.Opacity = 0;
    }

    public void ShowNotification(NotificationType type)
    {
        // Stop any existing saved timer.
        _savedTimer?.Stop();

        // Show the proper notification type.
        if (type == NotificationType.Save)
        {
            ResetNotification.Opacity = 0;
            SavedNotification.Opacity = 1.0;
        }
        else if (type == NotificationType.Reset)
        {
            ResetNotification.Opacity = 1.0;
            SavedNotification.Opacity = 0;
        }
        // Used to fade out the notification.
        double opacity = 1.0;
        int holdFrames = 60;
        int frameCount = 0;

        // Timer that fades out the message.
        _savedTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        _savedTimer.Tick += (s, e) =>
        {
            // Increment the framecount every 16 milliseconds.
            frameCount++;
            if (frameCount < holdFrames)
                return;

            // Reduce the opacity.
            opacity -= 0.02;
            if (opacity <= 0)
            {
                opacity = 0;
                _savedTimer.Stop();
            }
            // Fade out the notification.
            if (type == NotificationType.Save)
                SavedNotification.Opacity = opacity;
            else if (type == NotificationType.Reset)
                ResetNotification.Opacity = opacity;
        };
        // Start the timer.
        _savedTimer.Start();
    }
}