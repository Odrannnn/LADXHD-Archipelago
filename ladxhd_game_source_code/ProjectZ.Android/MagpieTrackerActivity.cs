using System;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Views;
using Android.Webkit;
using Android.Widget;
using ProjectZ.InGame.Archipelago;
using ProjectZ.InGame.Controls;

namespace ProjectZ.Android
{
    internal sealed class AndroidMagpieTrackerService : IMagpieTrackerService
    {
        private readonly WeakReference<Activity> _activity;
        private readonly WeakReference<FrameLayout> _gameRoot;
        private MagpieTrackerOverlay _overlay;

        public AndroidMagpieTrackerService(Activity activity, FrameLayout gameRoot)
        {
            _activity = new WeakReference<Activity>(activity);
            _gameRoot = new WeakReference<FrameLayout>(gameRoot);
        }

        public bool IsAvailable => true;
        public bool IsVisible => _overlay?.Parent != null && _overlay.Visibility == ViewStates.Visible;

        public void Show()
        {
            if (!_activity.TryGetTarget(out var activity))
                return;

            activity.RunOnUiThread(() =>
            {
                if (!_gameRoot.TryGetTarget(out var gameRoot))
                    return;

                if (_overlay != null)
                {
                    if (_overlay.Parent == null)
                        AddOverlay(gameRoot, activity, _overlay);
                    _overlay.Visibility = ViewStates.Visible;
                    _overlay.BringToFront();
                    _overlay.RequestFocus();
                    return;
                }

                _overlay = new MagpieTrackerOverlay(activity, HideOnUiThread);
                AddOverlay(gameRoot, activity, _overlay);
                _overlay.BringToFront();
                _overlay.RequestFocus();
            });
        }

        public void Hide()
        {
            if (_activity.TryGetTarget(out var activity))
                activity.RunOnUiThread(HideOnUiThread);
        }

        public bool TryHandleBackPressed()
        {
            if (!IsVisible)
                return false;

            if (_overlay.TryNavigateBack())
                return true;

            HideOnUiThread();
            return true;
        }

        public bool TryHandleControllerClose(CButtons? button, bool isKeyDown, int repeatCount)
        {
            if (!MagpieTrackerProtocol.ShouldCloseEmbeddedTracker(
                    IsVisible, isKeyDown, repeatCount, button))
                return false;

            HideOnUiThread();
            return true;
        }

        public void Destroy()
        {
            if (_activity.TryGetTarget(out var activity))
                activity.RunOnUiThread(DestroyOnUiThread);
        }

        private static void AddOverlay(FrameLayout gameRoot, Activity activity, MagpieTrackerOverlay overlay)
        {
            var screenWidth = gameRoot.Width;
            if (screenWidth <= 0)
            {
                if (OperatingSystem.IsAndroidVersionAtLeast(30))
                    screenWidth = activity.WindowManager?.CurrentWindowMetrics?.Bounds.Width() ?? 0;
                else
                    screenWidth = activity.Resources?.DisplayMetrics?.WidthPixels ?? 0;
            }

            var overlayWidth = MagpieTrackerProtocol.CalculateEmbeddedOverlayWidth(screenWidth);
            var layout = new FrameLayout.LayoutParams(
                overlayWidth > 0 ? overlayWidth : ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.MatchParent)
            {
                Gravity = GravityFlags.Right
            };
            gameRoot.AddView(overlay, layout);
        }

        private void HideOnUiThread()
        {
            var overlay = _overlay;
            if (overlay?.Parent == null || overlay.Visibility != ViewStates.Visible)
                return;

            overlay.Visibility = ViewStates.Gone;
            if (_gameRoot.TryGetTarget(out var gameRoot))
                gameRoot.GetChildAt(0)?.RequestFocus();
        }

        private void DestroyOnUiThread()
        {
            var overlay = _overlay;
            _overlay = null;
            if (overlay == null)
                return;

            if (overlay.Parent is ViewGroup parent)
                parent.RemoveView(overlay);
            overlay.DestroyTracker();
        }
    }

    internal sealed class MagpieTrackerOverlay : LinearLayout
    {
        private WebView _webView;

        public MagpieTrackerOverlay(Context context, Action close) : base(context)
        {
            Orientation = Orientation.Vertical;
            Elevation = Dp(12);
            Background = new global::Android.Graphics.Drawables.ColorDrawable(Color.Rgb(20, 20, 20));

            var toolbar = new LinearLayout(context)
            {
                Orientation = Orientation.Horizontal,
                Background = new global::Android.Graphics.Drawables.ColorDrawable(Color.Rgb(35, 35, 35))
            };
            toolbar.SetGravity(GravityFlags.CenterVertical);
            toolbar.SetPadding(Dp(8), Dp(4), Dp(8), Dp(4));

            var closeButton = new Button(context) { Text = "Close tracker" };
            closeButton.Click += (_, _) => close();
            toolbar.AddView(closeButton, new LayoutParams(
                ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent));

            var title = new TextView(context)
            {
                Text = "Magpie Tracker — game paused",
                TextSize = 16,
                Gravity = GravityFlags.Center
            };
            title.SetTextColor(Color.White);
            toolbar.AddView(title, new LayoutParams(0, ViewGroup.LayoutParams.MatchParent, 1));

            var reloadButton = new Button(context) { Text = "Reload" };
            reloadButton.Click += (_, _) => _webView?.Reload();
            toolbar.AddView(reloadButton, new LayoutParams(
                ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent));

            AddView(toolbar, new LayoutParams(
                ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent));

            _webView = new WebView(context);
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

            AddView(_webView, new LayoutParams(ViewGroup.LayoutParams.MatchParent, 0, 1));
            _webView.LoadUrl(MagpieTrackerProtocol.CreateEmbeddedTrackerUri().AbsoluteUri);
        }

        public bool TryNavigateBack()
        {
            if (_webView?.CanGoBack() != true)
                return false;
            _webView.GoBack();
            return true;
        }

        public void DestroyTracker()
        {
            if (_webView == null)
                return;
            _webView.StopLoading();
            _webView.RemoveAllViews();
            _webView.Destroy();
            _webView = null;
        }

        private int Dp(int value) => (int)(value * Resources.DisplayMetrics.Density + 0.5f);

        private sealed class MagpieWebViewClient : WebViewClient
        {
            private bool _dnsFallbackAttempted;

            public override bool ShouldOverrideUrlLoading(WebView view, string url) =>
                !IsAllowedTrackerUrl(url);

            public override bool ShouldOverrideUrlLoading(WebView view, IWebResourceRequest request) =>
                !IsAllowedTrackerUrl(request?.Url?.ToString());

            public override void OnReceivedError(
                WebView view, IWebResourceRequest request, WebResourceError error)
            {
                base.OnReceivedError(view, request, error);
                if (_dnsFallbackAttempted || view == null || request?.IsForMainFrame != true ||
                    error?.ErrorCode != ClientError.HostLookup ||
                    !MagpieTrackerProtocol.TryCreateEmbeddedTrackerDnsFallback(
                        request.Url?.ToString(), out var fallbackUri))
                    return;

                _dnsFallbackAttempted = true;
                view.LoadUrl(fallbackUri.AbsoluteUri);
            }

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
}
