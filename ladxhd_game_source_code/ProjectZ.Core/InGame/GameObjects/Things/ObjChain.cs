using System.IO;
using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameObjects.Things
{
    internal class ObjChain : GameObject
    {
        private readonly ObjSprite[] _objChains =
            new ObjSprite[BowWowChainGameplay.VisibleLinkCount];
        private BowWowChainGameplay _chain;
        private float _chainLengthInit = 7.5f;
        private float _chainLengthEnd = 4f;

        // Values configurable via lahdmod.
        private float chain_alpha = 0.55f;

        public ObjChain(Map.Map map, Vector2 startPosition) : base(map)
        {
            string modFile = Path.Combine(Values.PathLAHDMods, "ObjChain.lahdmod");
            ModFile.Parse(modFile, this);

            _chain = new BowWowChainGameplay(
                startPosition, _chainLengthInit, _chainLengthEnd);

            for (var i = 0; i < BowWowChainGameplay.VisibleLinkCount; i++)
            {
                _objChains[i] = new ObjSprite(map, (int)startPosition.X, (int)startPosition.Y, "bowwow chain", Vector2.Zero, Values.LayerPlayer, null);
                _objChains[i].Sprite.Color = new Color(255, 255, 255) * chain_alpha;
                map.Objects.SpawnObject(_objChains[i]);
            }
        }

        public void SetChainPosition(Vector2 position)
        {
            _chain.SetPosition(position);
        }

        public void UpdateChain(Vector3 startPosition, Vector3 endPosition)
        {
            _chain.Update(startPosition, endPosition);
            for (var i = 0; i < _objChains.Length; i++)
            {
                var link = _chain.Links[i];
                _objChains[i].EntityPosition.Set(link.Position);
                _objChains[i].EntityPosition.Z = link.Height;
            }
        }

        public Vector2 GetEndPosition()
        {
            return _chain.EndPosition;
        }
    }
}
