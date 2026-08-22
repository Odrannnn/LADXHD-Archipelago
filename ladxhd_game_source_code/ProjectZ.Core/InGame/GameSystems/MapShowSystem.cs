using System;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProjectZ.InGame.GameObjects.Enemies;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.SaveLoad;
using ProjectZ.InGame.Things;

namespace ProjectZ.InGame.GameSystems
{
    class MapShowSystem : GameSystem
    {
        private Thread _loadingThread;

        public Color TransitionColor = Color.White;

        private Vector2[] _cameraTargets = new Vector2[] {
            new Vector2(248, 1344),
            new Vector2(120, 1488),
            new Vector2(416, 1984),
            new Vector2(256, 704),
            new Vector2(400, 1216) };

        private float _counter;
        private const float ChangeTargetTime = 5350;
        private const float FadeTimeOut = 2500;
        private const float FadeTimeIn  = 250;
        private const float FadeLock    = 250;
        private int _targetIndex;

        private bool _isActive;
        private bool _finished;
        private bool _finishedLoading;
        private bool _init;

        public override void OnLoad()
        {
            _targetIndex = 0;
            _finished = false;
            _isActive = false;
            Camera.LockCamera = false;
        }

        public override void Update()
        {
            if (!_isActive)
                return;

            if (!_init)
            {
                if (_finishedLoading && _counter <= 0)
                {
                    _counter = ChangeTargetTime;
                    _finishedLoading = false;
                    _init = true;
                    FinishLoading();
                }
                else
                {
                    _counter -= Game1.DeltaTime;
                }
                return;
            }

            _counter -= Game1.DeltaTime;

            if (_finished && _counter < ChangeTargetTime - FadeTimeIn - FadeLock)
            {
                _isActive = false;
                Camera.LockCamera = false;
                return;
            }

            if (_counter < 0)
            {
                _counter += ChangeTargetTime;

                _targetIndex++;
                if (_targetIndex < _cameraTargets.Length)
                {
                    Game1.GameManager.MapManager.CurrentMap.CameraTarget = _cameraTargets[_targetIndex];
                    MapManager.Camera.ForceUpdate(Game1.GameManager.MapManager.GetCameraTarget());
                }
                else
                {
                    _finished = true;
                    Camera.LockCamera = false;

                    // switch back to the ending map
                    var oldMap = Game1.GameManager.MapManager.CurrentMap;
                    Game1.GameManager.MapManager.CurrentMap = Game1.GameManager.MapManager.NextMap;
                    Game1.GameManager.MapManager.NextMap = oldMap;
                    Game1.GameManager.InGameOverlay.StartSequence("final");
                }
            }
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (!_isActive)
                return;

            spriteBatch.Begin();

            var fadePercentage = 0f;

            // Slowly fade out the current camera target.
            if (_counter < FadeTimeOut + FadeLock)
                fadePercentage = 1 - MathF.Sin(MathHelper.Clamp((_counter - FadeLock) / FadeTimeOut, 0, 1) * MathF.PI * 0.5f);

            // Quickly fade back in to the next camera target.
            else if (ChangeTargetTime - FadeTimeIn - FadeLock < _counter)
                fadePercentage = MathF.Sin((1 - MathHelper.Clamp((ChangeTargetTime - FadeLock - _counter) / FadeTimeIn, 0, 1)) * MathF.PI * 0.5f);

            spriteBatch.Draw(Resources.SprWhite, new Rectangle(0, 0, Game1.RenderWidth, Game1.RenderHeight), TransitionColor * fadePercentage);
            spriteBatch.End();
        }

        public void StartEnding()
        {
            if (_isActive)
                return;

            _isActive = true;
            _finishedLoading = false;
            _init = false;
            _counter = FadeLock + FadeTimeOut;

            Camera.LockCamera = true;

            // used to spawn different npcs on the overworld
            Game1.GameManager.SaveManager.SetString("final_show", "1");
            Game1.GameManager.SaveManager.SetString("marin_state", "1");

            // start loading the new map in a thread
            _loadingThread = new Thread(o => ThreadLoading("overworld.map"));
            _loadingThread.Start();
        }

        private void ThreadLoading(string mapFileName)
        {
            // load the map file
            SaveLoadMap.LoadMap(mapFileName, Game1.GameManager.MapManager.NextMap);

            // create the objects
            Game1.GameManager.MapManager.NextMap.Objects.LoadObjects();

            // These damn birds are always causing me problems.
            var ravenList = Game1.GameManager.MapManager.NextMap.Objects.GetObjectsOfType(typeof(EnemyRaven));

            // Completely disable them from being able to do anything.
            foreach (EnemyRaven raven in ravenList)
                raven.IsActive = false;
            _finishedLoading = true;
        }

        private void FinishLoading()
        {
            // switch to the new map
            var oldMap = Game1.GameManager.MapManager.CurrentMap;
            Game1.GameManager.MapManager.CurrentMap = Game1.GameManager.MapManager.NextMap;
            Game1.GameManager.MapManager.NextMap = oldMap;

            // center the camera
            Game1.GameManager.MapManager.CurrentMap.CameraTarget = _cameraTargets[_targetIndex];
            MapManager.Camera.ForceUpdate(Game1.GameManager.MapManager.GetCameraTarget());

            Game1.GameManager.StartDialogPath("final_show_map");
        }
    }
}
