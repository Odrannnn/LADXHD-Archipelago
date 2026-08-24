using System;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.OS;
using Android.Views;
using Android.Webkit;
using Android.Widget;
using ProjectZ.InGame.Archipelago;

namespace ProjectZ.Android
{
    [Activity(
        Name = "com.zelda.ladxhd.archipelago.MagpieTrackerActivity",
        Label = "Magpie Tracker",
        Theme = "@style/Theme.Game",
        Exported = false,
        ScreenOrientation = ScreenOrientation.FullSensor,
        ConfigurationChanges =
            ConfigChanges.Orientation |
            ConfigChanges.ScreenSize |
            ConfigChanges.KeyboardHidden |
            ConfigChanges.UiMode)]
    public sealed class MagpieTrackerActivity : Activity
    {
        private WebView _webView;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Window?.ClearFlags(WindowManagerFlags.LayoutNoLimits);

            var root = new LinearLayout(this)
            {
                Orientation = Orientation.Vertical,
                Background = new global::Android.Graphics.Drawables.ColorDrawable(Color.Rgb(20, 20, 20))
            };

            var toolbar = new LinearLayout(this)
            {
                Orientation = Orientation.Horizontal,
                Background = new global::Android.Graphics.Drawables.ColorDrawable(Color.Rgb(35, 35, 35))
            };
            toolbar.SetGravity(GravityFlags.CenterVertical);
            toolbar.SetPadding(Dp(8), Dp(4), Dp(8), Dp(4));

            var closeButton = new Button(this) { Text = "Back to game" };
            closeButton.Click += (_, _) => Finish();
            toolbar.AddView(closeButton, new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent));

            var title = new TextView(this)
            {
                Text = "Magpie Tracker",
                TextSize = 18,
                Gravity = GravityFlags.Center,
            };
            title.SetTextColor(Color.White);
            toolbar.AddView(title, new LinearLayout.LayoutParams(0,
                ViewGroup.LayoutParams.MatchParent, 1));

            var reloadButton = new Button(this) { Text = "Reload" };
            reloadButton.Click += (_, _) => _webView?.Reload();
            toolbar.AddView(reloadButton, new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent));

            root.AddView(toolbar, new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent));

            _webView = new WebView(this);
            WebView.SetWebContentsDebuggingEnabled(false);
            _webView.SetBackgroundColor(Color.Rgb(20, 20, 20));
            _webView.Settings.JavaScriptEnabled = true;
            _webView.Settings.DomStorageEnabled = true;
            _webView.Settings.AllowFileAccess = false;
            _webView.Settings.AllowContentAccess = false;
            _webView.Settings.JavaScriptCanOpenWindowsAutomatically = false;
            _webView.Settings.SetSupportMultipleWindows(false);
            _webView.Settings.MediaPlaybackRequiresUserGesture = true;
            _webView.Settings.BuiltInZoomControls = true;
            _webView.Settings.DisplayZoomControls = false;
            _webView.Settings.UseWideViewPort = true;
            _webView.Settings.LoadWithOverviewMode = true;
            _webView.Settings.MixedContentMode = MixedContentHandling.AlwaysAllow;
            _webView.SetWebViewClient(new MagpieWebViewClient());

            CookieManager.Instance?.SetAcceptThirdPartyCookies(_webView, false);

            root.AddView(_webView, new LinearLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent, 0, 1));
            SetContentView(root);

            _webView.LoadUrl(MagpieTrackerProtocol.CreateEmbeddedTrackerUri().AbsoluteUri);
        }

        public override void OnBackPressed()
        {
            if (_webView?.CanGoBack() == true)
                _webView.GoBack();
            else
                Finish();
        }

        protected override void OnDestroy()
        {
            if (_webView != null)
            {
                _webView.StopLoading();
                _webView.RemoveAllViews();
                _webView.Destroy();
                _webView = null;
            }
            base.OnDestroy();
        }

        private int Dp(int value) => (int)(value * Resources.DisplayMetrics.Density + 0.5f);

        private sealed class MagpieWebViewClient : WebViewClient
        {
            public override bool ShouldOverrideUrlLoading(WebView view, string url) =>
                !IsAllowedTrackerUrl(url);

            public override bool ShouldOverrideUrlLoading(WebView view, IWebResourceRequest request) =>
                !IsAllowedTrackerUrl(request?.Url?.ToString());

            private static bool IsAllowedTrackerUrl(string value)
            {
                if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
                    !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
                    return false;

                return string.Equals(uri.Host, "magpietracker.us", StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(uri.Host, "www.magpietracker.us", StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    internal sealed class AndroidMagpieTrackerService : IMagpieTrackerService
    {
        private readonly WeakReference<Activity> _activity;

        public AndroidMagpieTrackerService(Activity activity)
        {
            _activity = new WeakReference<Activity>(activity);
        }

        public bool IsAvailable => true;

        public void Show()
        {
            if (!_activity.TryGetTarget(out var activity))
                return;

            activity.RunOnUiThread(() =>
                activity.StartActivity(new Intent(activity, typeof(MagpieTrackerActivity))));
        }
    }
}
