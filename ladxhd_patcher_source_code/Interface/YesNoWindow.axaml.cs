using System.Threading.Tasks;
using Avalonia.Controls;

namespace LADXHD_Patcher
{
    public partial class YesNoWindow : Window
    {
        public bool Result { get; private set; } = false;

        public YesNoWindow()
        {
            InitializeComponent();
        }

        public static async Task<bool> ShowAsync(string title, string message)
        {
            return await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
            {
                var window = new YesNoWindow();
                window.Display(title, message);
                SoundPlayer.Play("avares://Patcher/Resources/beep.wav");
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