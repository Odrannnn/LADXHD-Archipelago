using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.Base.UI;
using ProjectZ.InGame.Interface;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.Overlay
{
    public class AchievementOverlay
    {
        private static readonly Queue<ToastRequest> _pending = new Queue<ToastRequest>();

        private readonly struct ToastRequest
        {
            public ToastRequest(int achievementIndex, string prefix, string title, string subtitle)
            {
                AchievementIndex = achievementIndex;
                Prefix = prefix;
                Title = title;
                Subtitle = subtitle;
            }

            public int AchievementIndex { get; }
            public string Prefix { get; }
            public string Title { get; }
            public string Subtitle { get; }
            public bool IsAchievement => AchievementIndex >= 0;
        }

        private enum ToastState { Idle, SlideIn, Hold, SlideOut }
        private ToastState _state = ToastState.Idle;
        private float _counter;

        private Texture2D _icon;
        private string _prefixText = "";
        private string _titleText = "";
        private string _subtitleText = "";

        private readonly UiRectangle _background;

        private const int IconSize = 20;
        private const int Padding = 4;
        private const int BoxHeight = IconSize + Padding * 2;
        private const int MaxTextWidth = 190;

        private Rectangle _boxRect;
        private int _boxHeight = BoxHeight;
        private float _alpha;

        private Color _backgroundColor;
        private Color _titleColor;

        // Values configurable via lahdmod.
        private bool  custom_toast_show      = true;
        private int   custom_toast_offsetx   = 0;
        private int   custom_toast_offsety   = 0;
        private int   custom_toast_margin    = 16;
        private float custom_toast_slidetime = 350f;
        private float custom_toast_holdtime  = 3500f;
        private int   custom_toast_red       = 40;
        private int   custom_toast_grn       = 64;
        private int   custom_toast_blu       = 128;
        private float custom_toast_alpha     = 0.85f;
        private int   custom_title_red       = 255;
        private int   custom_title_grn       = 255;
        private int   custom_title_blu       = 70;

        public AchievementOverlay()
        {
            // If a mod file exists load the values from it.
            string modFile = Path.Combine(Values.PathLAHDMods, "AchievementOverlay.lahdmod");
            ModFile.Parse(modFile, this);

            _backgroundColor = new Color(custom_toast_red, custom_toast_grn, custom_toast_blu) * custom_toast_alpha;
            _titleColor = new Color(custom_title_red, custom_title_grn, custom_title_blu);

            // The rounded/blurred background rectangle, same pattern as the HUD boxes.
            _background = new UiRectangle(Rectangle.Empty, "achievementToast", Values.ScreenNameGame,
                Color.Transparent, Color.Transparent, null)
            { Radius = Values.UiBackgroundRadius, IsHudElement = true };
            Game1.UiManager.AddElement(_background);
        }

        public static void Push(int index)
        {
            lock (_pending)
                _pending.Enqueue(new ToastRequest(index, null, null, null));
        }

        public static void PushArchipelagoItem(string action, string itemName, string relation, string playerName)
        {
            lock (_pending)
            {
                _pending.Enqueue(new ToastRequest(-1,
                    string.IsNullOrWhiteSpace(action) ? "Item: " : action.Trim() + ": ",
                    string.IsNullOrWhiteSpace(itemName) ? "Unknown item" : itemName.Trim(),
                    (string.IsNullOrWhiteSpace(relation) ? "Player" : relation.Trim()) + ": " +
                    (string.IsNullOrWhiteSpace(playerName) ? "Unknown player" : playerName.Trim())));
            }
        }

        private bool TryStartNext()
        {
            ToastRequest request;
            lock (_pending)
            {
                if (_pending.Count == 0)
                    return false;
                request = _pending.Dequeue();
            }

            if (request.IsAchievement)
            {
                var forceEnglish = Game1.LanguageManager.CurrentLanguageCode == "chn";
                _icon = Resources.GetTexture("achievement" + request.AchievementIndex + ".png");
                _titleText = Game1.LanguageManager.GetString("achieveName" + request.AchievementIndex,
                    "error", false, forceEnglish);
                _prefixText = Game1.LanguageManager.GetString("achievements_earned", "error") + ": ";
                _subtitleText = "";
            }
            else
            {
                _icon = null;
                _prefixText = request.Prefix;
                _titleText = request.Title;
                _subtitleText = request.Subtitle;
            }

            _state = ToastState.SlideIn;
            _counter = 0;
            return true;
        }

        public void Update()
        {
            // Pick up the next queued notification if we're idle.
            if (_state == ToastState.Idle && !TryStartNext())
            {
                HideBackground();
                return;
            }

            if (!custom_toast_show)
            {
                // Popup disabled by mod file; drain the queue silently.
                _state = ToastState.Idle;
                lock (_pending) _pending.Clear();
                HideBackground();
                return;
            }

            _counter += Game1.DeltaTime;

            // Advance the state machine.
            switch (_state)
            {
                case ToastState.SlideIn:
                    if (_counter >= custom_toast_slidetime) { _state = ToastState.Hold; _counter = 0; }
                    break;
                case ToastState.Hold:
                    if (_counter >= custom_toast_holdtime) { _state = ToastState.SlideOut; _counter = 0; }
                    break;
                case ToastState.SlideOut:
                    if (_counter >= custom_toast_slidetime) { _state = ToastState.Idle; _counter = 0; }
                    break;
            }

            // How far "on screen" we are: 0 = fully hidden, 1 = resting position.
            float t = _state switch
            {
                ToastState.SlideIn  => Math.Clamp(_counter / custom_toast_slidetime, 0, 1),
                ToastState.Hold     => 1f,
                ToastState.SlideOut => 1f - Math.Clamp(_counter / custom_toast_slidetime, 0, 1),
                _ => 0f
            };
            float eased = MathF.Sin(MathF.PI / 2 * t);
            _alpha = eased;

            var scale = Game1.UiScale;

            // Measure and size the box around the text.
            var titleWidth = Math.Min(DrawHelper.MeasureString(_titleText).X, MaxTextWidth);
            var prefixWidth = DrawHelper.MeasureString(_prefixText).X;
            var subtitleWidth = string.IsNullOrEmpty(_subtitleText)
                ? 0
                : Math.Min(DrawHelper.MeasureString(_subtitleText).X, MaxTextWidth + prefixWidth);
            var textWidth = Math.Max(prefixWidth + titleWidth, subtitleWidth);
            var iconWidth = _icon == null ? 0 : IconSize + Padding;
            int boxWidth = (int)(Padding * 2 + iconWidth + textWidth);
            var lineHeight = DrawHelper.MeasureString(_prefixText).Y;
            _boxHeight = string.IsNullOrEmpty(_subtitleText)
                ? BoxHeight
                : (int)Math.Max(IconSize + Padding * 2, lineHeight * 2 + Padding * 2 + 1);

            // Resting position: bottom-right corner with a margin.
            int restX = Game1.WindowWidth - (boxWidth + custom_toast_margin) * scale + custom_toast_offsetx;
            int restY = Game1.WindowHeight - (_boxHeight + custom_toast_margin) * scale + custom_toast_offsety;

            // Slide up from just below the bottom edge of the screen.
            int hiddenY = Game1.WindowHeight;
            int posY = (int)MathHelper.Lerp(hiddenY, restY, eased);

            _boxRect = new Rectangle(restX, posY, boxWidth * scale, _boxHeight * scale);

            // Update the background rectangle.
            if (_state != ToastState.Idle && !UiManager.HideOverlay)
            {
                _background.Rectangle = _boxRect;
                _background.BackgroundColor = _backgroundColor * _alpha;
                _background.BlurColor = Values.OverlayBackgroundBlurColor * _alpha;
            }
            else
            {
                HideBackground();
            }
        }

        private void HideBackground()
        {
            _background.BackgroundColor = Color.Transparent;
            _background.BlurColor = Color.Transparent;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (_state == ToastState.Idle || !custom_toast_show || UiManager.HideOverlay)
                return;

            var scale = Game1.UiScale;
            var color = Color.White * _alpha;

            // Draw the achievement icon scaled into a IconSize x IconSize square.
            if (_icon != null)
            {
                var iconRect = new Rectangle(
                    _boxRect.X + Padding * scale,
                    _boxRect.Y + Padding * scale,
                    IconSize * scale, IconSize * scale);
                spriteBatch.Draw(_icon, iconRect, color);
            }

            // Vertically center one achievement line or the two-line Archipelago message.
            var textHeight = DrawHelper.MeasureString(_prefixText).Y;
            var textBlockHeight = string.IsNullOrEmpty(_subtitleText) ? textHeight : textHeight * 2 + 1;
            var iconWidth = _icon == null ? 0 : IconSize + Padding;
            var textPos = new Vector2(
                _boxRect.X + (Padding + iconWidth) * scale,
                _boxRect.Y + _boxRect.Height / 2f - textBlockHeight * scale / 2f);

            // Draw "Earned: " in white, then the title in the achievement title color.
            DrawHelper.DrawString(spriteBatch, _prefixText, textPos,
                InterfaceElement.MainTextColor * _alpha, 0, Vector2.Zero, scale, SpriteEffects.None, 0);

            var prefixWidth = DrawHelper.MeasureString(_prefixText).X;
            var titlePos = new Vector2(textPos.X + prefixWidth * scale, textPos.Y);

            DrawHelper.DrawString(spriteBatch, EllipsizedTitle(), titlePos,
                _titleColor * _alpha, 0, Vector2.Zero, scale, SpriteEffects.None, 0);

            if (!string.IsNullOrEmpty(_subtitleText))
            {
                var subtitlePos = new Vector2(textPos.X, textPos.Y + (textHeight + 1) * scale);
                DrawHelper.DrawString(spriteBatch, Ellipsized(_subtitleText, MaxTextWidth + prefixWidth), subtitlePos,
                    InterfaceElement.MainTextColor * _alpha, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
            }
        }

        private string EllipsizedTitle()
        {
            // Trim the title with "..." if it would exceed the max width.
            if (DrawHelper.MeasureString(_titleText).X <= MaxTextWidth)
                return _titleText;

            var text = _titleText;
            while (text.Length > 1 && DrawHelper.MeasureString(text + "...").X > MaxTextWidth)
                text = text.Substring(0, text.Length - 1);

            return text.TrimEnd() + "...";
        }

        private static string Ellipsized(string value, float maxWidth)
        {
            if (string.IsNullOrEmpty(value) || DrawHelper.MeasureString(value).X <= maxWidth)
                return value;

            var text = value;
            while (text.Length > 1 && DrawHelper.MeasureString(text + "...").X > maxWidth)
                text = text.Substring(0, text.Length - 1);

            return text.TrimEnd() + "...";
        }
    }
}
