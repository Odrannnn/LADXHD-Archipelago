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
        BowWowTargets();
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

    private static void BowWowTargets()
    {
        var assembly = typeof(GameObject).Assembly;
        var hitType = assembly.GetType("ProjectZ.InGame.GameObjects.Base.Components.HittableComponent")!;
        var callbackType = hitType.GetNestedType("HitTemplate")!;
        var callback = Delegate.CreateDelegate(callbackType,
            typeof(GameplayCrashRegressionTests).GetMethod(nameof(Hit), BindingFlags.Static | BindingFlags.NonPublic)!);
        var hit = (Component)Activator.CreateInstance(hitType,
            new object[] { new CBox(0, 0, 0, 8, 8, 8), callback })!;
        var target = new GameObject(new Map()) { EntityPosition = new CPosition(8, 16, 0) };
        var method = typeof(ObjBowWow).GetMethod("TryGetAttackTarget", BindingFlags.Static | BindingFlags.NonPublic)!;
        bool Valid(GameObject candidate) => (bool)method.Invoke(null, [candidate, null])!;
        Check(!Valid(null) && !Valid(target), "BowWow must ignore missing targets and enemy-tagged objects without hitboxes.");
        target.Components[7] = hit;
        Check(Valid(target), "A live hittable target must remain edible.");
        hitType.GetField("IsActive")!.SetValue(hit, false);
        Check(Valid(target), "Selection must preserve fish's make-vulnerable attack behavior.");
        target.IsDead = true;
        Check(!Valid(target), "Dead targets must be rejected.");
        target.IsDead = false;
        target.IsActive = false;
        Check(!Valid(target), "Inactive targets must be rejected.");
        target.IsActive = true;
        hitType.GetField("HittableBox")!.SetValue(hit, null);
        Check(!Valid(target), "A component without its hitbox must be rejected.");
        hitType.GetField("HittableBox")!.SetValue(hit, new CBox(0, 0, 0, 8, 8, 8));
        hitType.GetField("Hit")!.SetValue(hit, null);
        Check(!Valid(target), "A target without a hit callback must be rejected.");
        hitType.GetField("Hit")!.SetValue(hit, callback);
        target.Map = null;
        Check(!Valid(target), "An enemy removed mid-lunge must be rejected.");

        // This is the same guard called again by UpdateAttack on every lunge
        // update. Restore the target, then remove its component mid-attack.
        target.Map = new Map();
        Check(Valid(target), "The restored target must be valid before component removal.");
        target.Components[7] = null;
        Check(!Valid(target), "The lunge guard must reject a target whose component disappeared.");
    }

    private static Values.HitCollision Hit(GameObject origin, Vector2 direction, HitType type, int damage, bool power) =>
        Values.HitCollision.Enemy;

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
