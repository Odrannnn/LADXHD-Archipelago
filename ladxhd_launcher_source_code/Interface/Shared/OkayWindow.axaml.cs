using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;

namespace LADXHD_Launcher
{
    public partial class OkayWindow : Window, IControllerDialog
    {
        private DispatcherTimer? _timer;

        public OkayWindow()
        {
            InitializeComponent();
            Opened += OnOpened;
        }

        private void OnOpened(object? sender, System.EventArgs e)
        {
            if (App.MainWindowInstance.Classes.Contains("nav-active"))
            {
                Classes.Add("nav-active");
                Dispatcher.UIThread.Post(FocusDefault, DispatcherPriority.Background);
            }
        }

        public void FocusDefault() => OkButton.Focus(Avalonia.Input.NavigationMethod.Directional);

        public static async Task ShowAsync(string title, string message, int timeoutSeconds = 0, bool altSound = false)
        {
            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                var window = new OkayWindow();
                window.Display(title, message, timeoutSeconds);
                SoundPlayer.PlayWAVSound(altSound 
                    ? "avares://Launcher/Resources/success.wav" 
                    : "avares://Launcher/Resources/beep.wav");
                await window.ShowDialog(App.MainWindowInstance);
            });
        }

        public void Display(string title, string message, int timeoutSeconds = 0)
        {
            Title = title;
            MessageLabel.Text = message;

            if (timeoutSeconds > 0)
            {
                _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(timeoutSeconds) };
                _timer.Tick += (_, _) => Close();
                _timer.Start();
            }
        }

        private void OkButton_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            _timer?.Stop();
            Close();
        }
    }
}