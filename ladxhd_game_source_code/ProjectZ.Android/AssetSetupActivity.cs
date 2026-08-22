using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Android.Widget;
using ProjectZ.InGame.Assets;
using AndroidNet = Android.Net;

namespace ProjectZ.Android
{
    [Activity(
        Name = "com.zelda.ladxhd.archipelago.AssetSetupActivity",
        Label = "Set up LADXHD Archipelago",
        Theme = "@android:style/Theme.DeviceDefault.NoActionBar",
        Exported = true,
        LaunchMode = LaunchMode.SingleTop,
        ScreenOrientation = ScreenOrientation.Unspecified)]
    [IntentFilter(
        new[] { Intent.ActionView },
        Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
        DataScheme = "content",
        DataMimeType = "application/zip")]
    [IntentFilter(
        new[] { Intent.ActionView },
        Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
        DataScheme = "content",
        DataMimeType = "application/octet-stream",
        DataPathPattern = @".*\.zip")]
    public sealed class AssetSetupActivity : Activity
    {
        private const int OpenArchiveRequest = 7301;
        private TextView _status;
        private ProgressBar _progress;
        private Button _selectButton;
        private Button _savedButton;
        private Button _cancelButton;
        private CancellationTokenSource _cancellation;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Window?.AddFlags(WindowManagerFlags.KeepScreenOn);
            BuildInterface();

            if (Intent?.Action == Intent.ActionView && Intent.Data != null)
                BeginInstall(Intent.Data);
        }

        protected override void OnNewIntent(Intent intent)
        {
            base.OnNewIntent(intent);
            Intent = intent;
            if (intent?.Action == Intent.ActionView && intent.Data != null)
                BeginInstall(intent.Data);
        }

        private void BuildInterface()
        {
            var density = Resources?.DisplayMetrics?.Density ?? 1f;
            int Dp(int value) => (int)(value * density + 0.5f);

            var scroll = new ScrollView(this);
            var layout = new LinearLayout(this)
            {
                Orientation = Orientation.Vertical
            };
            layout.SetPadding(Dp(24), Dp(32), Dp(24), Dp(32));
            scroll.AddView(layout, new ViewGroup.LayoutParams(
                ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent));

            var title = new TextView(this) { Text = "Set up LADXHD Archipelago", TextSize = 28f };
            title.SetTypeface(null, global::Android.Graphics.TypefaceStyle.Bold);
            layout.AddView(title);

            var explanation = new TextView(this)
            {
                Text = "Choose your untouched Links Awakening DX HD v1.0.0 ZIP. The app verifies it and builds the Android game data locally. Your ZIP is never uploaded, and saves are backed up before an asset update.",
                TextSize = 17f
            };
            var textParams = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
            textParams.SetMargins(0, Dp(16), 0, Dp(24));
            layout.AddView(explanation, textParams);

            _selectButton = new Button(this) { Text = "Choose v1.0.0 ZIP" };
            _selectButton.Click += (_, _) => OpenArchivePicker();
            layout.AddView(_selectButton);

            _savedButton = new Button(this) { Text = "Rebuild using previously selected ZIP" };
            _savedButton.Click += (_, _) => UseSavedArchive();
            _savedButton.Visibility = string.IsNullOrWhiteSpace(AndroidAssetInstallation.GetSavedSourceUri(this))
                ? ViewStates.Gone
                : ViewStates.Visible;
            layout.AddView(_savedButton);

            _progress = new ProgressBar(this, null, global::Android.Resource.Attribute.ProgressBarStyleHorizontal)
            {
                Max = 100,
                Progress = 0,
                Visibility = ViewStates.Gone
            };
            var progressParams = new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent, Dp(8));
            progressParams.SetMargins(0, Dp(24), 0, Dp(16));
            layout.AddView(_progress, progressParams);

            _status = new TextView(this)
            {
                Text = AndroidAssetInstallation.TryGetActiveRoot(this, out _, out var reason)
                    ? "The current game data is ready. Choose the ZIP only when an app update asks you to rebuild it."
                    : reason,
                TextSize = 16f
            };
            layout.AddView(_status);

            _cancelButton = new Button(this) { Text = "Cancel", Visibility = ViewStates.Gone };
            _cancelButton.Click += (_, _) => _cancellation?.Cancel();
            layout.AddView(_cancelButton);

            SetContentView(scroll);
        }

        private void OpenArchivePicker()
        {
            var intent = new Intent(Intent.ActionOpenDocument);
            intent.AddCategory(Intent.CategoryOpenable);
            intent.SetType("application/zip");
            intent.PutExtra(Intent.ExtraMimeTypes, new[] { "application/zip", "application/octet-stream" });
            intent.AddFlags(ActivityFlags.GrantReadUriPermission | ActivityFlags.GrantPersistableUriPermission);
#pragma warning disable CS0618
            StartActivityForResult(intent, OpenArchiveRequest);
#pragma warning restore CS0618
        }

        private void UseSavedArchive()
        {
            var saved = AndroidAssetInstallation.GetSavedSourceUri(this);
            if (string.IsNullOrWhiteSpace(saved))
                return;
            BeginInstall(AndroidNet.Uri.Parse(saved));
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
            if (requestCode != OpenArchiveRequest || resultCode != Result.Ok || data?.Data == null)
                return;

            try
            {
                var granted = data.Flags & (ActivityFlags.GrantReadUriPermission | ActivityFlags.GrantWriteUriPermission);
                ContentResolver?.TakePersistableUriPermission(data.Data, granted & ActivityFlags.GrantReadUriPermission);
            }
            catch
            {
                // Some document providers grant access without supporting persistent grants.
            }
            BeginInstall(data.Data);
        }

        private async void BeginInstall(AndroidNet.Uri uri)
        {
            if (_cancellation != null)
                return;

            _cancellation = new CancellationTokenSource();
            SetWorking(true);
            _status.Text = "Preparing local installation…";
            _progress.Progress = 0;
            var progress = new Progress<GameAssetMigrationProgress>(update =>
            {
                _status.Text = update.Stage;
                _progress.Progress = update.Total <= 0
                    ? 0
                    : Math.Clamp((int)Math.Round(update.Completed * 100d / update.Total), 0, 100);
            });

            try
            {
                await Task.Run(() => AndroidAssetInstallation.Install(this, uri, progress, _cancellation.Token));
                _status.Text = "Game data installed successfully. Starting LADXHD Archipelago…";
                _progress.Progress = 100;
                await Task.Delay(350);
                StartActivity(new Intent(this, typeof(MainActivity)));
                Finish();
            }
            catch (System.OperationCanceledException)
            {
                _status.Text = "Setup cancelled. The previous installed version, if any, was left unchanged.";
                SetWorking(false);
            }
            catch (Exception exception)
            {
                _status.Text = "Setup failed: " + exception.Message;
                SetWorking(false);
            }
            finally
            {
                _cancellation?.Dispose();
                _cancellation = null;
            }
        }

        private void SetWorking(bool working)
        {
            _selectButton.Enabled = !working;
            _savedButton.Enabled = !working;
            _progress.Visibility = working ? ViewStates.Visible : ViewStates.Gone;
            _cancelButton.Visibility = working ? ViewStates.Visible : ViewStates.Gone;
        }

        protected override void OnDestroy()
        {
            _cancellation?.Cancel();
            base.OnDestroy();
        }
    }
}
