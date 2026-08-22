using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;

namespace LADXHD_Migrater
{
    public partial class OkayWindow : Window
    {
        private DispatcherTimer? _timer;

        public OkayWindow()
        {
            InitializeComponent();
        }

        public static async Task ShowAsync(string title, string message, int timeoutSeconds = 0, bool altSound = false)
        {
            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                var window = new OkayWindow();
                window.Display(title, message, timeoutSeconds);
                SoundPlayer.Play(altSound 
                    ? "avares://Migrater/Resources/success.wav" 
                    : "avares://Migrater/Resources/beep.wav");
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