using System.Threading.Tasks;
using Avalonia.Controls;

namespace LADXHD_Launcher
{
    public partial class YesNoWindow : Window, IControllerDialog
    {
        public bool Result { get; private set; } = false;

        public YesNoWindow()
        {
            InitializeComponent();
            Opened += OnOpened;
        }

        private void OnOpened(object? sender, System.EventArgs e)
        {
            if (App.MainWindowInstance.Classes.Contains("nav-active"))
            {
                Classes.Add("nav-active");
                Avalonia.Threading.Dispatcher.UIThread.Post(FocusDefault, Avalonia.Threading.DispatcherPriority.Background);
            }
        }

        public void FocusDefault() => YesButton.Focus(Avalonia.Input.NavigationMethod.Directional);

        public static async Task<bool> ShowAsync(string title, string message)
        {
            return await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
            {
                var window = new YesNoWindow();
                window.Display(title, message);
                SoundPlayer.PlayWAVSound("avares://Launcher/Resources/beep.wav");
                await window.ShowDialog(App.MainWindowInstance);
                return window.Result;
            });
        }

        public void Display(string title, string message)
        {
            Title = title;
            MessageLabel.Text = message;
        }

        private void YesButton_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            Result = true;
            Close();
        }

        private void NoButton_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            Result = false;
            Close();
        }
    }
}