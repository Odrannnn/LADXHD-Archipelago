using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace ProjectZ
{
    /// <summary>
    /// Shared implementation of ObjChain's constrained BowWow links. Keeping
    /// this stateful solver in Core lets gameplay and the wallpaper advance the
    /// same geometry without a renderer-specific chain approximation.
    /// </summary>
    public sealed class BowWowChainGameplay
    {
        public const int SegmentCount = 6;
        public const int VisibleLinkCount = SegmentCount - 1;
        public const float InitialLinkLength = 7.5f;
        public const float EndLinkLength = 4f;
        public const float Alpha = 0.55f;

        public readonly struct Link
        {
            public Link(Vector2 position, float height)
            {
                Position = position;
                Height = height;
            }

            public Vector2 Position { get; }
            public float Height { get; }
        }

        private readonly Vector2[] _starts = new Vector2[SegmentCount];
        private readonly Vector2[] _ends = new Vector2[SegmentCount];
        private readonly float[] _heights = new float[SegmentCount];
        private readonly Link[] _links = new Link[VisibleLinkCount];
        private readonly float _initialLinkLength;
        private readonly float _endLinkLength;

        public BowWowChainGameplay(
            Vector2 startPosition,
            float initialLinkLength = InitialLinkLength,
            float endLinkLength = EndLinkLength)
        {
            _initialLinkLength = initialLinkLength;
            _endLinkLength = endLinkLength;
            SetPosition(startPosition);
        }

        public IReadOnlyList<Link> Links => _links;
        public Vector2 EndPosition => _ends[^1];

        public void SetPosition(Vector2 position)
        {
            for (var index = 0; index < SegmentCount; index++)
            {
                _starts[index] = position;
                _ends[index] = position;
                _heights[index] = 0f;
                if (index < VisibleLinkCount)
                    _links[index] = new Link(position + new Vector2(0f, 3f), 0f);
            }
        }

        public void Update(Vector3 startPosition, Vector3 goalPosition)
        {
            var distance = Vector2.Distance(
                new Vector2(startPosition.X, startPosition.Y),
                new Vector2(goalPosition.X, goalPosition.Y));
            var linkLength = distance - _endLinkLength >
                             (SegmentCount - 1) * _initialLinkLength
                ? (distance - _endLinkLength) / (SegmentCount - 1)
                : _initialLinkLength;

            _ends[^1] = new Vector2(goalPosition.X, goalPosition.Y);
            _heights[^1] = goalPosition.Z;
            for (var index = SegmentCount - 1; index > 0; index--)
            {
                var direction = _starts[index] - _ends[index];
                var segmentLength = index < SegmentCount - 1
                    ? linkLength
                    : _endLinkLength;
                if (direction.Length() > segmentLength)
                {
                    direction.Normalize();
                    direction *= segmentLength;
                }
                _starts[index] = _ends[index] + direction;
                _ends[index - 1] = _starts[index];
                _heights[index - 1] = _heights[index] > 1.5f
                    ? _heights[index] - 1.5f
                    : 0f;
            }

            _starts[0] = new Vector2(startPosition.X, startPosition.Y);
            _heights[0] = startPosition.Z * 0.75f;
            for (var index = 0; index < SegmentCount; index++)
            {
                var direction = _ends[index] - _starts[index];
                var segmentLength = index < SegmentCount - 1
                    ? linkLength
                    : _endLinkLength;
                if (direction.Length() > segmentLength)
                {
                    direction.Normalize();
                    direction *= segmentLength;
                }
                _ends[index] = _starts[index] + direction;
                if (index < VisibleLinkCount)
                {
                    _links[index] = new Link(
                        _ends[index] + new Vector2(0f, 3f),
                        _heights[index]);
                    _starts[index + 1] = _ends[index];
                    _heights[index + 1] = _heights[index] > (index + 1) * 3f + 3f
                        ? _heights[index] - ((index + 1) * 3f + 3f)
                        : 0f;
                }
            }
        }
    }
}
