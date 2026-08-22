using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace LADXHD_Launcher
{
    public partial class App : Application
    {
        public static MainWindow? MainWindowInstance { get; private set; }
        public static double SavedWindowHeight { get; set; } = 768;

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var mainWindow = new MainWindow();
                MainWindowInstance = mainWindow;
                desktop.MainWindow = mainWindow;
                desktop.Exit += (_, _) => ControllerInput.Stop();
            }
            base.OnFrameworkInitializationCompleted();
        }
    }
}