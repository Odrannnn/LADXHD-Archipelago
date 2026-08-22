using System.Collections.Generic;
using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects.Things;
using ProjectZ.InGame.Map;

namespace ProjectZ.InGame.Things
{
    public class CameraField
    {
        public List<ObjCameraField> CameraFieldList = new List<ObjCameraField>();
        public Vector2 CameraFieldCoords;

        public void FindClosestCoords()
        {
            // Always reset. If we fail to find anything, coords become Zero (not stale).
            CameraFieldList.Clear();
            CameraFieldCoords = Vector2.Zero;

            // Make sure a map exists before trying to find camera objects.
            var map = MapManager.ObjLink?.Map;
            var objects = map?.Objects;
            if (objects == null)
                return;

            // Get Link's position to determine the closest camera.
            var playerPos = MapManager.ObjLink.CenterPosition.Position;
            var objList = objects.GetObjects((int)playerPos.X - 160, (int)playerPos.Y - 100, 320, 200);

            // Loop through the game objects.
            foreach (var gameObject in objList)
                if (gameObject is ObjCameraField camField)
                    CameraFieldList.Add(camField);

            // If the list is empty, there is no camera coords to obtain.
            if (CameraFieldList.Count <= 0)
                return;

            // Holds the closest camera to Link.
            ObjCameraField closestCam = null;
            var closestDist = float.MaxValue;

            // Loop through all the camera objects found.
            foreach (ObjCameraField fieldCenter in CameraFieldList)
            {
                // If there are multiple camera objects, find the closest camera to Link.
                float dist = Vector2.Distance(playerPos, fieldCenter.EntityPosition.Position);

                if (dist < closestDist)
                {
                    closestDist = dist;
                    closestCam = fieldCenter;
                }
            }
            // If the closest camera is null, there is no camera coords to obtain.
            if (closestCam == null)
                return;

            // Return the coordinates of the closest camera.
            CameraFieldCoords = new Vector2(closestCam.EntityPosition.X, closestCam.EntityPosition.Y);
        }

        public void ClearList()
        {
            // Clear the camera field object list and clear properties.
            CameraFieldList.Clear();
            CameraFieldCoords = Vector2.Zero;
        }
    }
}
