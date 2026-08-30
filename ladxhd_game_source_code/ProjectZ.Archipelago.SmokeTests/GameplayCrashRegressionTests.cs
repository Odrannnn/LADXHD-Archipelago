using System;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;
using ProjectZ;
using ProjectZ.InGame.GameObjects;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;
using ProjectZ.InGame.GameObjects.Base.Components;
using ProjectZ.InGame.GameObjects.NPCs;
using ProjectZ.InGame.Map;
using ProjectZ.InGame.Things;

internal static class GameplayCrashRegressionTests
{
    public static void Run()
    {
        RepeatedObjectInitialization();
    }

    private static void RepeatedObjectInitialization()
    {
        var oldTemplates = GameObjectTemplates.ObjectTemplates;
        var oldSpawners = GameObjectTemplates.ObjectSpawner;
        var oldParameters = GameObjectTemplates.GameObjectParameter;
        var setup = typeof(GameObjectTemplates).GetMethod("RegisterGameObjects",
            BindingFlags.Static | BindingFlags.NonPublic)!;
        try
        {
            Func<string, Rectangle> rectangle = _ => new Rectangle(0, 0, 16, 16);
            setup.Invoke(null, [rectangle]);
            var firstSpawners = GameObjectTemplates.ObjectSpawner;
            var firstTemplates = GameObjectTemplates.ObjectTemplates;
            var firstParameters = GameObjectTemplates.GameObjectParameter;
            var expectedKeys = firstSpawners.Keys.Order().ToArray();
            Check(firstSpawners.ContainsKey("c1"), "Actual collider registration must be covered.");
            // A stale prior-session entry must disappear, not accumulate forever.
            firstSpawners.Add("test-stale-entry", firstSpawners["c1"]);
            setup.Invoke(null, [rectangle]);
            setup.Invoke(null, [rectangle]);
            Check(GameObjectTemplates.ObjectSpawner.Keys.Order().SequenceEqual(expectedKeys),
                "Repeated normal-game initialization must rebuild registrations without duplicates.");
            Check(!ReferenceEquals(firstSpawners, GameObjectTemplates.ObjectSpawner) &&
                  !ReferenceEquals(firstTemplates, GameObjectTemplates.ObjectTemplates) &&
                  !ReferenceEquals(firstParameters, GameObjectTemplates.GameObjectParameter),
                "All three registries must be replaced for the new game session.");
            Check(GameObjectTemplates.ObjectSpawner.Keys.Order().SequenceEqual(
                    GameObjectTemplates.GameObjectParameter.Keys.Order()),
                "Spawner and constructor registries must remain paired.");
        }
        finally
        {
            GameObjectTemplates.ObjectTemplates = oldTemplates;
            GameObjectTemplates.ObjectSpawner = oldSpawners;
            GameObjectTemplates.GameObjectParameter = oldParameters;
        }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
