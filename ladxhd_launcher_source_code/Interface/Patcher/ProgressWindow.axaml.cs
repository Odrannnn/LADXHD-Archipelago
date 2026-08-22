using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using static LADXHD_Launcher.Patcher_Functions;

namespace LADXHD_Launcher
{
    public partial class ProgressWindow : Window, IProgressWindow
    {
        private static ProgressWindow? _instance;

        public ProgressWindow()
        {
            InitializeComponent();
        }

        public void UpdateProgressBar(int value)
        {
            Dispatcher.UIThread.Post(() => 
            {
                ProgressBar.Value = value; 
                PercentLabel.Text = $"{value}%"; 
            });
        }

        public static async Task<ProgressWindow> ShowAsync(string title, string status)
        {
            return await Dispatcher.UIThread.InvokeAsync(() =>
            {
                var window = new ProgressWindow();
                window.Title = title;
                window.StatusLabel.Text = status;
                _instance = window;
                window.Show(App.MainWindowInstance);
                return window;
            });
        }

        public void UpdateStatus(string status)
        {
            Dispatcher.UIThread.Post(() => StatusLabel.Text = status);
        }

        public void Finish()
        {
            Dispatcher.UIThread.Post(() =>
            {
                ProgressBar.Value = 100;
                PercentLabel.Text = "100%";
            });
        }

        public void CloseWindow()
        {
            Dispatcher.UIThread.Post(() => Close());
        }
    }
}