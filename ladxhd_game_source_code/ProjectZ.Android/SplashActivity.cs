using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Microsoft.Xna.Framework;

namespace ProjectZ.Android
{
    [Activity(
        Label = "@string/app_name",
        Theme = "@style/Theme.Splash",
        MainLauncher = true,
        NoHistory = true,
        ScreenOrientation = ScreenOrientation.FullSensor,
        ConfigurationChanges =
            ConfigChanges.Orientation |
            ConfigChanges.ScreenSize |
            ConfigChanges.KeyboardHidden |
            ConfigChanges.UiMode)]

    public class SplashActivity : AndroidGameActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.splash_layout);
            var nextActivity = AndroidAssetInstallation.TryGetActiveRoot(this, out _, out _)
                ? typeof(MainActivity)
                : typeof(AssetSetupActivity);
            var nextIntent = new Intent(this, nextActivity);
            nextIntent.PutExtra(MainActivity.ExtraLaunchSource,
                Intent?.GetStringExtra(MainActivity.ExtraLaunchSource) ?? "direct");
            StartActivity(nextIntent);
            Finish();
        }
    }
}
