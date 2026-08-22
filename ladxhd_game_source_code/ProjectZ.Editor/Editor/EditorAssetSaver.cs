using System;
using System.IO;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.SaveLoad;

namespace ProjectZ.Editor
{
    internal static class EditorAssetSaver
    {
        public static void SaveAnimator(string path, Animator animator)
        {
            if (animator == null) throw new ArgumentNullException(nameof(animator));
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Invalid path.", nameof(path));

            var tempPath = path + ".temp";
            using (var writer = new StreamWriter(tempPath))
            {
                writer.WriteLine("1");
                writer.WriteLine(animator.SpritePath ?? "");

                for (int i = 0; i < animator.Animations.Count; i++)
                {
                    var anim = animator.Animations[i];
                    var line = anim.Id + ";" + anim.NextAnimation + ";" + anim.LoopCount + ";" +
                        anim.Offset.X + ";" + anim.Offset.Y + ";" + anim.Frames.Length;

                    for (int j = 0; j < anim.Frames.Length; j++)
                    {
                        var frame = anim.Frames[j];
                        line += ";" + frame.FrameTime + ";" + frame.SourceRectangle.X + ";" +
                            frame.SourceRectangle.Y + ";" + frame.SourceRectangle.Width + ";" +
                            frame.SourceRectangle.Height + ";" + frame.Offset.X + ";" + frame.Offset.Y + ";" +
                            frame.CollisionRectangle.X + ";" + frame.CollisionRectangle.Y + ";" +
                            frame.CollisionRectangle.Width + ";" + frame.CollisionRectangle.Height + ";" +
                            frame.MirroredV + ";" + frame.MirroredH;
                    }

                    writer.WriteLine(line);
                }
            }

            if (File.Exists(path))
                File.Delete(path);
            File.Move(tempPath, path);
        }

        public static void SaveSpriteAtlas(string filePath, SpriteAtlasSerialization.SpriteAtlas spriteAtlas)
        {
            if (spriteAtlas == null) throw new ArgumentNullException(nameof(spriteAtlas));
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("Invalid path.", nameof(filePath));

            using var writer = new StreamWriter(filePath);
            writer.WriteLine("1");
            writer.WriteLine(spriteAtlas.Scale);

            var scale = spriteAtlas.Scale <= 0 ? 1 : spriteAtlas.Scale;
            for (int i = 0; i < spriteAtlas.Data.Count; i++)
            {
                var entry = spriteAtlas.Data[i];
                var rectangle = entry.SourceRectangle;
                var origin = entry.Origin;
                writer.WriteLine(
                    $"{entry.EntryId}:{rectangle.X / scale},{rectangle.Y / scale},{rectangle.Width / scale},{rectangle.Height / scale},{origin.X / scale},{origin.Y / scale}");
            }
        }
    }
}
