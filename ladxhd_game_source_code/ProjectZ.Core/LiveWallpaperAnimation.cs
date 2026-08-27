using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

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

    public sealed class LiveWallpaperAnimation
    {
        private const int MaximumFramesPerAnimation = 64;
        private readonly LiveWallpaperFrame[] _frames;
        private readonly long _durationMilliseconds;

        private LiveWallpaperAnimation(string spritePath, string animationId, LiveWallpaperFrame[] frames)
        {
            SpritePath = spritePath;
            AnimationId = animationId;
            _frames = frames;
            _durationMilliseconds = frames.Sum(frame => (long)frame.DurationMilliseconds);
        }

        public string SpritePath { get; }
        public string AnimationId { get; }
        public IReadOnlyList<LiveWallpaperFrame> Frames => _frames;

        public static bool TryGetSpriteRelativeCandidates(
            string spritePath,
            out string[] candidates)
        {
            candidates = [];
            if (string.IsNullOrWhiteSpace(spritePath))
                return false;

            var normalized = spritePath.Trim().Replace('\\', '/');
            if (normalized.StartsWith('/') || normalized.Contains(':'))
                return false;
            var segments = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length == 0 || segments.Any(segment => segment is "." or ".."))
                return false;

            normalized = string.Join('/', segments);
            candidates = [normalized, "Map Objects/" + normalized];
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

            var animations = new Dictionary<string, LiveWallpaperFrame[]>(StringComparer.OrdinalIgnoreCase);
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (line.Length > 16384 || !TryParseAnimationLine(line, out var id, out var frames))
                    continue;
                animations[id] = frames;
            }

            foreach (var preferredId in preferredAnimationIds)
            {
                if (!string.IsNullOrWhiteSpace(preferredId) &&
                    animations.TryGetValue(preferredId, out var frames) && frames.Length > 0)
                {
                    animation = new LiveWallpaperAnimation(spritePath, preferredId, frames);
                    return true;
                }
            }
            return false;
        }

        private static bool TryParseAnimationLine(
            string line,
            out string animationId,
            out LiveWallpaperFrame[] frames)
        {
            animationId = null;
            frames = [];
            if (string.IsNullOrWhiteSpace(line))
                return false;

            var values = line.Split(';');
            if (values.Length < 19)
                return false;
            animationId = values[0]?.Trim();
            if (string.IsNullOrWhiteSpace(animationId) || !TryInt(values[5], out var frameCount) ||
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
                    !TryInt(values[position++], out var offsetX) ||
                    !TryInt(values[position++], out var offsetY) ||
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
                    duration, x, y, width, height, offsetX, offsetY,
                    mirroredVertically, mirroredHorizontally);
            }
            return true;
        }

        private static bool TryInt(string value, out int result) =>
            int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
    }
}
