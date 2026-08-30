using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using ProjectZ.InGame.GameObjects;
using ProjectZ.InGame.GameObjects.Base;
using ProjectZ.InGame.GameObjects.Base.CObjects;

internal static class MapTransitionArrivalTests
{
    public static void Run()
    {
        EntryDialogGate();
        ArrivalPlacementKeepsFollowersAndShadowSafe();
    }

    private static void EntryDialogGate()
    {
        var transitionSystem = typeof(GameObjectFollower).Assembly.GetType("ProjectZ.InGame.GameSystems.MapTransitionSystem")!;
        var needsUpdate = transitionSystem.GetMethod("NeedsEntryDialogUpdate",
            BindingFlags.Static | BindingFlags.NonPublic)!;
        bool CheckGate(string entryId, Vector2? entryPosition, bool dialogPending) =>
            (bool)needsUpdate.Invoke(null, [entryId, entryPosition, dialogPending])!;

        Check(!CheckGate(null, null, true), "A null entry id must not advance dialogs.");
        Check(!CheckGate(string.Empty, null, true), "An unnamed map arrival must not advance dialogs.");
        Check(!CheckGate("stairs", new Vector2(8, 16), true), "A resolved named entry must not advance dialogs.");
        Check(!CheckGate("stairs", null, false), "A missing entry without a queued dialog must not advance dialogs.");
        Check(CheckGate("stairs", null, true), "A queued conditional named entry must advance its dialog before arrival resolution.");
    }

    private static void ArrivalPlacementKeepsFollowersAndShadowSafe()
    {
        var follower = (GameObjectFollower)RuntimeHelpers.GetUninitializedObject(typeof(GameObjectFollower));
        follower.EntityPosition = new CPosition(-1, -1, 0);

        var shadowType = typeof(GameObjectFollower).Assembly.GetType("ProjectZ.InGame.GameObjects.Things.ObjSpriteShadow")!;
        var shadow = (GameObject)RuntimeHelpers.GetUninitializedObject(shadowType);
        shadow.EntityPosition = new CPosition(-2, -2, 0);

        var place = typeof(GameObjectFollower).GetMethod("PlaceAtMapArrival",
            BindingFlags.Static | BindingFlags.NonPublic)!;
        place.Invoke(null, [new List<GameObjectFollower> { follower }, shadow, new Vector2(32, 48)]);

        Check(follower.EntityPosition.Position == new Vector2(32, 48),
            "Follower placement must use Link's resolved fallback position when the optional entry is absent.");
        Check(shadow.EntityPosition.Position == new Vector2(32, 48),
            "Sprite-shadow placement must use Link's resolved fallback position when the optional entry is absent.");

        place.Invoke(null, [new List<GameObjectFollower> { follower }, shadow, new Vector2(80, 96)]);
        Check(follower.EntityPosition.Position == new Vector2(80, 96) && shadow.EntityPosition.Position == new Vector2(80, 96),
            "A resolved named-entry arrival must still update followers and the shadow to its new position.");

        place.Invoke(null, [Array.Empty<GameObjectFollower>(), null, new Vector2(1, 2)]);
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
