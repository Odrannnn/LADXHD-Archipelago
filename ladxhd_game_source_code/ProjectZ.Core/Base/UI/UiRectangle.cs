using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.Things;

namespace ProjectZ.Base.UI
{
    public class UiRectangle : UiElement
    {
        public Color BlurColor;
        public float Radius = 0;
        public bool IsHudElement;

        // Base HUD color and opacity for the opaque background mode
        public static readonly Color OpaqueHudColor = new Color(255, 255, 190) * 0.75f;

        public UiRectangle(Rectangle rectangle, string elementId, string screen, Color color, Color blurColor, UiFunction update)
            : base(elementId, screen)
        {
            Rectangle = rectangle;
            BackgroundColor = color;
            BlurColor = blurColor;
            UpdateFunction = update;
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (!GameSettings.OpaqueHudBg || BackgroundColor.A <= 0)
                return;

            Color drawColor;
            if (IsHudElement)
            {
                // Scale opacity to match the element's current transparency
                var transparency = BackgroundColor.A / (255f * 0.55f);
                drawColor = OpaqueHudColor * Math.Clamp(transparency, 0f, 1f);
            }
            else
            {
                // Non-HUD elements (menus, textboxes) use their own color as solid fallback
                drawColor = BackgroundColor;
            }

            var radiusPx = (int)(Radius * Game1.UiScale);
            if (radiusPx <= 0)
            {
                spriteBatch.Draw(Resources.SprWhite, Rectangle, drawColor);
                return;
            }

            radiusPx = Math.Clamp(radiusPx, 1, Math.Min(Rectangle.Width, Rectangle.Height) / 2);

            var x = Rectangle.X;
            var y = Rectangle.Y;
            var w = Rectangle.Width;
            var h = Rectangle.Height;

            spriteBatch.Draw(Resources.SprWhite, new Rectangle(x + radiusPx, y, w - radiusPx * 2, h), drawColor);
            spriteBatch.Draw(Resources.SprWhite, new Rectangle(x, y + radiusPx, radiusPx, h - radiusPx * 2), drawColor);
            spriteBatch.Draw(Resources.SprWhite, new Rectangle(x + w - radiusPx, y + radiusPx, radiusPx, h - radiusPx * 2), drawColor);
        }

        public override void DrawBlur(SpriteBatch spriteBatch)
        {
            float radiusPx = Radius * Game1.UiScale;

            Resources.RoundedCornerBlurEffect.Parameters["blurColor"].SetValue(BlurColor.ToVector4());
            Resources.RoundedCornerBlurEffect.Parameters["width"]?.SetValue(Rectangle.Width);
            Resources.RoundedCornerBlurEffect.Parameters["height"]?.SetValue(Rectangle.Height);
            Resources.RoundedCornerBlurEffect.Parameters["scale"]?.SetValue(1f);
            Resources.RoundedCornerBlurEffect.Parameters["radius"]?.SetValue(radiusPx);
            spriteBatch.Draw(Resources.SprWhite, Rectangle, BackgroundColor);
        }
    }
}
