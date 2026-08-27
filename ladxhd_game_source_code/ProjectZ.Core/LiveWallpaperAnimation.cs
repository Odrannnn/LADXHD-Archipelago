using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;

namespace ProjectZ
{
    public readonly struct LiveWallpaperFrame
    {
        public LiveWallpaperFrame(
            int durationMilliseconds,
            int x,
            int y,
            int width,
            int height,
            int offsetX,
            int offsetY,
            bool mirroredVertically,
            bool mirroredHorizontally)
        {
            DurationMilliseconds = Math.Max(1, durationMilliseconds);
            X = x;
            Y = y;
            Width = width;
            Height = height;
            OffsetX = offsetX;
            OffsetY = offsetY;
            MirroredVertically = mirroredVertically;
            MirroredHorizontally = mirroredHorizontally;
        }

        public int DurationMilliseconds { get; }
        public int X { get; }
        public int Y { get; }
        public int Width { get; }
        public int Height { get; }
        public int OffsetX { get; }
        public int OffsetY { get; }
        public bool MirroredVertically { get; }
        public bool MirroredHorizontally { get; }
    }

    public readonly struct LiveWallpaperSpritePlacement
    {
        public LiveWallpaperSpritePlacement(float left, float top, float right, float bottom)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }

        public float Left { get; }
        public float Top { get; }
        public float Right { get; }
        public float Bottom { get; }
    }

    public sealed class LiveWallpaperAnimation
    {
        private const int MaximumFramesPerAnimation = 64;
        private readonly LiveWallpaperFrame[] _frames;
        private readonly long _durationMilliseconds;

        private LiveWallpaperAnimation(
            string spritePath,
            string animationId,
            int offsetX,
            int offsetY,
            LiveWallpaperFrame[] frames)
        {
            SpritePath = spritePath;
            AnimationId = animationId;
            OffsetX = offsetX;
            OffsetY = offsetY;
            _frames = frames;
            _durationMilliseconds = frames.Sum(frame => (long)frame.DurationMilliseconds);
        }

        public string SpritePath { get; }
        public string AnimationId { get; }
        public int OffsetX { get; }
        public int OffsetY { get; }
        public IReadOnlyList<LiveWallpaperFrame> Frames => _frames;

        public static bool TryGetSpriteRelativeCandidates(
            string spritePath,
            out string[] candidates)
        {
            candidates = [];
            if (!TryNormalizeRelativePath(spritePath, out var normalized))
                return false;

            candidates = [normalized, "Map Objects/" + normalized];
            return true;
        }

        public static bool TryNormalizeRelativePath(string path, out string normalized)
        {
            normalized = null;
            if (string.IsNullOrWhiteSpace(path))
                return false;

            normalized = path.Trim().Replace('\\', '/');
            if (normalized.StartsWith('/') || normalized.Contains(':'))
            {
                normalized = null;
                return false;
            }
            var segments = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length == 0 || segments.Any(segment => segment is "." or ".."))
            {
                normalized = null;
                return false;
            }

            normalized = string.Join('/', segments);
            return true;
        }

        public LiveWallpaperFrame GetFrame(long elapsedMilliseconds)
        {
            if (_frames.Length == 1 || _durationMilliseconds <= 0)
                return _frames[0];

            var position = elapsedMilliseconds % _durationMilliseconds;
            if (position < 0)
                position += _durationMilliseconds;
            foreach (var frame in _frames)
            {
                if (position < frame.DurationMilliseconds)
                    return frame;
                position -= frame.DurationMilliseconds;
            }
            return _frames[^1];
        }

        public LiveWallpaperSpritePlacement GetPlacement(
            LiveWallpaperFrame frame,
            float anchorX,
            float anchorY,
            float scale)
        {
            scale = Math.Max(0f, scale);
            var left = anchorX + (OffsetX + frame.OffsetX) * scale;
            var top = anchorY + (OffsetY + frame.OffsetY) * scale;
            return new LiveWallpaperSpritePlacement(
                left, top, left + frame.Width * scale, top + frame.Height * scale);
        }

        public LiveWallpaperEngineAnimation CreateEngineAnimation() => new(this);

        public static bool TryLoad(
            TextReader reader,
            IEnumerable<string> preferredAnimationIds,
            out LiveWallpaperAnimation animation)
        {
            animation = null;
            if (reader == null || preferredAnimationIds == null)
                return false;

            reader.ReadLine(); // LADXHD animation format version.
            var spritePath = reader.ReadLine()?.Trim();
            if (string.IsNullOrWhiteSpace(spritePath))
                return false;

            var animations = new Dictionary<string, (int OffsetX, int OffsetY, LiveWallpaperFrame[] Frames)>(
                StringComparer.OrdinalIgnoreCase);
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (line.Length > 16384 || !TryParseAnimationLine(
                        line, out var id, out var offsetX, out var offsetY, out var frames))
                    continue;
                animations[id] = (offsetX, offsetY, frames);
            }

            foreach (var preferredId in preferredAnimationIds)
            {
                if (!string.IsNullOrWhiteSpace(preferredId) &&
                    animations.TryGetValue(preferredId, out var parsed) && parsed.Frames.Length > 0)
                {
                    animation = new LiveWallpaperAnimation(
                        spritePath, preferredId, parsed.OffsetX, parsed.OffsetY, parsed.Frames);
                    return true;
                }
            }
            return false;
        }

        private static bool TryParseAnimationLine(
            string line,
            out string animationId,
            out int offsetX,
            out int offsetY,
            out LiveWallpaperFrame[] frames)
        {
            animationId = null;
            offsetX = 0;
            offsetY = 0;
            frames = [];
            if (string.IsNullOrWhiteSpace(line))
                return false;

            var values = line.Split(';');
            if (values.Length < 19)
                return false;
            animationId = values[0]?.Trim();
            if (string.IsNullOrWhiteSpace(animationId) ||
                !TryInt(values[3], out offsetX) ||
                !TryInt(values[4], out offsetY) ||
                !TryInt(values[5], out var frameCount) ||
                frameCount is <= 0 or > MaximumFramesPerAnimation || values.Length < 6 + frameCount * 13)
                return false;

            frames = new LiveWallpaperFrame[frameCount];
            var position = 6;
            for (var index = 0; index < frameCount; index++)
            {
                if (!TryInt(values[position++], out var duration) ||
                    !TryInt(values[position++], out var x) ||
                    !TryInt(values[position++], out var y) ||
                    !TryInt(values[position++], out var width) ||
                    !TryInt(values[position++], out var height) ||
                    !TryInt(values[position++], out var frameOffsetX) ||
                    !TryInt(values[position++], out var frameOffsetY) ||
                    !TryInt(values[position++], out _) ||
                    !TryInt(values[position++], out _) ||
                    !TryInt(values[position++], out _) ||
                    !TryInt(values[position++], out _) ||
                    !bool.TryParse(values[position++], out var mirroredVertically) ||
                    !bool.TryParse(values[position++], out var mirroredHorizontally) ||
                    width <= 0 || height <= 0)
                {
                    frames = [];
                    return false;
                }

                frames[index] = new LiveWallpaperFrame(
                    duration, x, y, width, height, frameOffsetX, frameOffsetY,
                    mirroredVertically, mirroredHorizontally);
            }
            return true;
        }

        private static bool TryInt(string value, out int result) =>
            int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
    }

    /// <summary>
    /// Drives wallpaper frames through the same Animator state machine used by game objects,
    /// without requiring gameplay, audio, saves, or Archipelago networking.
    /// </summary>
    public sealed class LiveWallpaperEngineAnimation
    {
        private readonly LiveWallpaperAnimation _source;
        private readonly Animator _animator;
        private long? _lastElapsed;

        internal LiveWallpaperEngineAnimation(LiveWallpaperAnimation source)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            var gameAnimation = new Animation(source.AnimationId)
            {
                Offset = new Point(source.OffsetX, source.OffsetY),
                LoopCount = -1,
                Frames = source.Frames.Select(frame => new Frame
                {
                    FrameTime = frame.DurationMilliseconds,
                    SourceRectangle = new Rectangle(frame.X, frame.Y, frame.Width, frame.Height),
                    Offset = new Point(frame.OffsetX, frame.OffsetY),
                    CollisionRectangle = Rectangle.Empty,
                    MirroredV = frame.MirroredVertically,
                    MirroredH = frame.MirroredHorizontally
                }).ToArray()
            };
            _animator = new Animator();
            _animator.AddAnimation(gameAnimation);
            _animator.Play(source.AnimationId);
        }

        public int CurrentFrameIndex => _animator.CurrentFrameIndex;

        public LiveWallpaperFrame Advance(long elapsedMilliseconds, bool animated)
        {
            if (!animated)
            {
                _animator.Play(_source.AnimationId, 0, 0);
                _lastElapsed = elapsedMilliseconds;
                return _source.Frames[0];
            }

            var delta = _lastElapsed.HasValue
                ? Math.Clamp(elapsedMilliseconds - _lastElapsed.Value, 0L, 250L)
                : 0L;
            _lastElapsed = elapsedMilliseconds;
            _animator.Update(delta);
            return _source.Frames[_animator.CurrentFrameIndex];
        }
    }
}
