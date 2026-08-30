using ProjectZ;
using ProjectZ.InGame.Archipelago;
using ProjectZ.InGame.Assets;
using ProjectZ.InGame.Controls;
using ProjectZ.InGame.GameSystems;
using ProjectZ.InGame.Overlay;
using ProjectZ.InGame.Telemetry;
using ProjectZ.InGame.Things;
using Archipelago.MultiClient.Net.Helpers;
using System.Net;
using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Reflection;
using System.Text;

static void Assert(bool condition, string message)
{
    if (!condition)
        throw new InvalidOperationException(message);
}

static int FindBushDropSeed(string expectedItemName)
{
    for (var seed = 0; seed < 10_000; seed++)
    {
        var random = new Random(seed);
        if (BushDropRules.Roll(random.Next) == expectedItemName)
            return seed;
    }
    throw new InvalidOperationException(
        $"Could not find a deterministic bush-drop seed for {expectedItemName}.");
}

static async Task SendWebSocketText(ClientWebSocket socket, string text)
{
    var bytes = Encoding.UTF8.GetBytes(text);
    await socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true,
        CancellationToken.None);
}

static async Task<string> ReceiveWebSocketText(ClientWebSocket socket)
{
    var buffer = new byte[4096];
    using var stream = new MemoryStream();
    while (true)
    {
        var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None)
            .WaitAsync(TimeSpan.FromSeconds(2));
        Assert(result.MessageType == WebSocketMessageType.Text,
            "Magpie bridge returned a non-text WebSocket message.");
        stream.Write(buffer, 0, result.Count);
        if (result.EndOfMessage)
            return Encoding.UTF8.GetString(stream.ToArray());
    }
}

// Public CI has no copyrighted game data. Synthetic regressions always run;
// maintainers can additionally require the installed-asset tests with an explicit
// directory. Resolve the default from either repository or game-source cwd.
var wallpaperGameDataRoot = Environment.GetEnvironmentVariable("LADXHD_TEST_GAME_DATA");
if (!string.IsNullOrWhiteSpace(wallpaperGameDataRoot))
    Assert(Directory.Exists(wallpaperGameDataRoot),
           "The explicitly configured wallpaper game-data directory is missing.");
else
    wallpaperGameDataRoot = Directory.Exists(Path.Combine("ProjectZ.Core", "Data"))
        ? Path.Combine("ProjectZ.Core", "Data")
        : Path.Combine("ladxhd_game_source_code", "ProjectZ.Core", "Data");
var testInstalledWallpaperAssets = Directory.Exists(wallpaperGameDataRoot);
if (!testInstalledWallpaperAssets)
    Console.WriteLine("Private installed-asset wallpaper tests unavailable; running public synthetic regressions.");

WallpaperSceneEffectsTests.Run();
WallpaperDayCycleTests.Run();
WallpaperDecorationDrawingTests.Run();
WallpaperCameraResizeTests.Run();
WallpaperIndoorNavigationTests.Run();
GameplayCrashRegressionTests.Run();

const string wallpaperAnimationData = """
1
link0.png
stand_0;;0;-8;-20;1;250;0;0;16;24;0;0;0;0;8;8;false;false
walk_2;;0;-8;-20;2;100;16;0;16;24;-1;2;0;0;8;8;false;true;200;32;0;16;24;1;3;0;0;8;8;true;false
""";
Assert(LiveWallpaperAnimation.TryLoad(
           new StringReader(wallpaperAnimationData), ["missing", "walk_2", "stand_0"],
           out var wallpaperAnimation) &&
       wallpaperAnimation.SpritePath == "link0.png" &&
       wallpaperAnimation.AnimationId == "walk_2" &&
       wallpaperAnimation.LoopCount == 0 &&
       wallpaperAnimation.OffsetX == -8 && wallpaperAnimation.OffsetY == -20 &&
       wallpaperAnimation.Frames.Count == 2,
       "The live wallpaper must select and parse Link's preferred LADXHD animation safely.");
var wallpaperFirstFrame = wallpaperAnimation.GetFrame(99);
var wallpaperSecondFrame = wallpaperAnimation.GetFrame(100);
var wallpaperLoopedFrame = wallpaperAnimation.GetFrame(300);
Assert(wallpaperFirstFrame.X == 16 && wallpaperFirstFrame.MirroredHorizontally &&
       wallpaperSecondFrame.X == 32 && wallpaperSecondFrame.MirroredVertically &&
       wallpaperLoopedFrame.X == 16,
       "The live wallpaper animation must honor frame durations, mirroring, and looping.");
var wallpaperPlacement = wallpaperAnimation.GetPlacement(
    wallpaperSecondFrame, 100f, 200f, 2f);
Assert(Math.Abs(wallpaperPlacement.Left - 86f) < 0.001f &&
       Math.Abs(wallpaperPlacement.Top - 166f) < 0.001f &&
       Math.Abs(wallpaperPlacement.Right - 118f) < 0.001f &&
       Math.Abs(wallpaperPlacement.Bottom - 214f) < 0.001f,
       "The live wallpaper must place frames with the same animation and frame origin semantics as the game Animator.");
Assert(wallpaperAnimation.TryGetOneShotCollisionRectangle(
           0, out var wallpaperCollision0) &&
       Math.Abs(wallpaperCollision0.X - -1f) < 0.001f &&
       Math.Abs(wallpaperCollision0.Y - -18f) < 0.001f &&
       wallpaperCollision0.Width == 8 && wallpaperCollision0.Height == 8 &&
       wallpaperAnimation.TryGetOneShotCollisionRectangle(
           100, out var wallpaperCollision1) &&
       Math.Abs(wallpaperCollision1.X - -7f) < 0.001f &&
       Math.Abs(wallpaperCollision1.Y - -1f) < 0.001f,
       "Wallpaper weapon collisions must preserve ANI rectangles and Animator mirroring semantics.");
var wallpaperEngineAnimation = wallpaperAnimation.CreateEngineAnimation();
var wallpaperEngineFirstFrame = wallpaperEngineAnimation.Advance(0, animated: true);
var wallpaperEngineSecondFrame = wallpaperEngineAnimation.Advance(101, animated: true);
Assert(wallpaperEngineFirstFrame.X == 16 && wallpaperEngineSecondFrame.X == 32 &&
       wallpaperEngineAnimation.CurrentFrameIndex == 1,
       "The live wallpaper must drive character frames through the game's Animator state machine.");
var wallpaperEngineStoppedFrame = wallpaperEngineAnimation.Advance(500, animated: true);
wallpaperEngineAnimation.Restart(500);
var wallpaperEngineRestartedFrame = wallpaperEngineAnimation.Advance(500, animated: true);
Assert(wallpaperEngineStoppedFrame.X == 32 && wallpaperEngineRestartedFrame.X == 16 &&
       wallpaperEngineAnimation.CurrentFrameIndex == 0,
       "One-shot wallpaper animations must stop on their last frame and restart for the next action.");
Assert(!LiveWallpaperAnimation.TryLoad(
           new StringReader("1\nlink0.png\nwalk_2;;0;0;0;99\n"), ["walk_2"], out _),
       "The live wallpaper must reject malformed or excessive animation frame data.");
if (testInstalledWallpaperAssets)
using (var fallReader = File.OpenText(Path.Combine(
           wallpaperGameDataRoot, "Animations", "link0.ani")))
{
    Assert(LiveWallpaperAnimation.TryLoad(
               fallReader, ["fall"], out var fallAnimation) &&
           fallAnimation.Frames.Count == 6 &&
           fallAnimation.DurationMilliseconds == 850L &&
           fallAnimation.Frames[^1].Width == 0 &&
           fallAnimation.Frames[^1].Height == 0,
           "The wallpaper must retain link0/fall's legitimate hidden terminal frame and exact duration.");
}
Assert(LiveWallpaperAnimation.TryGetSpriteRelativeCandidates(
           "link0.png", out var wallpaperSpriteCandidates) &&
       wallpaperSpriteCandidates.SequenceEqual(["link0.png", "Map Objects/link0.png"]),
       "The live wallpaper must find Link's sprite in the Map Objects asset folder.");
Assert(!LiveWallpaperAnimation.TryGetSpriteRelativeCandidates(
           "../link0.png", out _),
       "The live wallpaper must reject sprite paths that escape the game-data root.");
Assert(LiveWallpaperAnimation.TryNormalizeRelativePath(
           "NPCs\\butterfly.ani", out var wallpaperNpcPath) &&
       wallpaperNpcPath == "NPCs/butterfly.ani" &&
       !LiveWallpaperAnimation.TryNormalizeRelativePath("/Data/Animations/owl.ani", out _),
       "The live wallpaper must normalize safe nested animation paths and reject rooted paths.");
const string wallpaperMapData = "3\n0\n0\noverworld.png\n3\n2\n2\n" +
                               "0,1,2,\n3,,4,\n,5,,\n6,7,8,\n";
Assert(LiveWallpaperMap.TryLoad(new StringReader(wallpaperMapData), out var wallpaperMap) &&
       wallpaperMap.TilesetPath == "overworld.png" &&
       wallpaperMap.Width == 3 && wallpaperMap.Height == 2 &&
       wallpaperMap.Depth == 2 && wallpaperMap.DrawableDepth == 1 &&
       wallpaperMap.GetTile(1, 0, 0) == 1 && wallpaperMap.GetTile(1, 1, 0) == -1 &&
       wallpaperMap.GetTile(2, 1, 1) == 8 && wallpaperMap.GetTile(99, 99, 0) == -1,
       "The live wallpaper must safely parse installed map tiles and preserve empty cells.");
Assert(!LiveWallpaperMap.TryLoad(
           new StringReader("3\n0\n0\n../outside.png\n1\n1\n1\n0,\n"), out _),
       "The live wallpaper must reject unsafe installed tileset paths.");
const string wallpaperCollisionMapData =
    "3\n0\n0\noverworld.png\n6\n3\n1\n" +
    ",,,,,,\n,,,,,,\n,,,,,,\n" +
    "6\nc1\nhole\ntree0\nfence\nenemyWall\nbush\n6\n" +
    "0;16;0;;;\n1;32;0;;;;;;\n2;48;0;;;;;;\n" +
    "3;0;16;15\n4;80;16;;;;\n5;32;16;;;;;;\n";
Assert(LiveWallpaperMap.TryLoad(
           new StringReader(wallpaperCollisionMapData), out var wallpaperCollisionMap) &&
       wallpaperCollisionMap.CollisionCount == 7 &&
       wallpaperCollisionMap.HazardCount == 1 &&
       wallpaperCollisionMap.NpcWallCount == 1 &&
       wallpaperCollisionMap.IntersectsCollision(18, 2, 8, 8, includeHoles: false) &&
       !wallpaperCollisionMap.IntersectsCollision(33, 1, 8, 8, includeHoles: false) &&
       wallpaperCollisionMap.IntersectsCollision(33, 1, 8, 8, includeHoles: true) &&
       Math.Abs(wallpaperCollisionMap.GetLinkHoleCoverage(35, 2, 8, 10) - 1f) <
           0.001f &&
       wallpaperCollisionMap.IntersectsCollision(50, 5, 8, 8, includeHoles: false) &&
       wallpaperCollisionMap.IntersectsCollision(1, 16, 4, 4, includeHoles: false) &&
       !wallpaperCollisionMap.IntersectsCollision(81, 17, 4, 4, includeHoles: true) &&
       wallpaperCollisionMap.IntersectsNpcWall(81, 17, 4, 4),
       "The live wallpaper must parse scenery, fence, solid, hole, and NPC-wall records from installed maps.");
const string wallpaperObjectParityMapData =
    "3\n0\n0\ndungeon.png\n11\n1\n1\n" +
    "0,0,0,0,0,0,0,0,0,0,0\n" +
    "11\nmoveStone\nmoveStoneCave\nmoveStoneFrogHouse\nmoveStoneD3\n" +
    "d6Statue\ncaveCrystal\ncrystalD4\nhardCrystal\n" +
    "dungeonStatue\ndungeonStatueGrey\ndungeon3Head\n11\n" +
    "0;0;0\n1;16;0\n2;32;0\n3;48;0\n4;64;0\n5;80;0\n" +
    "6;96;0\n7;112;0\n8;128;0\n9;144;0\n10;160;0\n";
Assert(LiveWallpaperMap.TryLoad(
           new StringReader(wallpaperObjectParityMapData),
           out var wallpaperObjectParityMap),
       "The wallpaper object-parity fixture must load as a valid installed map.");
var wallpaperObjectDecorations = wallpaperObjectParityMap.Decorations
    .ToDictionary(decoration => decoration.SpriteId);
Assert(wallpaperObjectParityMap.CollisionCount == 11 &&
       wallpaperObjectParityMap.Decorations.Count == 11 &&
       wallpaperObjectDecorations.Keys.Contains("movestone_0") &&
       wallpaperObjectDecorations.Keys.Contains("movestone_1") &&
       wallpaperObjectDecorations.Keys.Contains("movestone_2") &&
       wallpaperObjectDecorations.Keys.Contains("movestone_3") &&
       !wallpaperObjectDecorations["movestone_0"].PlayerLayer &&
       wallpaperObjectDecorations["movestone_0"].TopLeft &&
       wallpaperObjectDecorations["d6_statue"].StoneLayout &&
       wallpaperObjectDecorations.Keys.Contains("crystal_0") &&
       wallpaperObjectDecorations.Keys.Contains("crystal_1") &&
       wallpaperObjectDecorations.Keys.Contains("crystal_hard") &&
       wallpaperObjectDecorations.Keys.Contains("dungeonStatue_0") &&
       wallpaperObjectDecorations.Keys.Contains("dungeonStatue_1") &&
       wallpaperObjectDecorations.Keys.Contains("dungeon3Head") &&
       wallpaperObjectParityMap.IntersectsCollision(
           0, 0, 16, 16, includeHoles: false) &&
       wallpaperObjectParityMap.TryGetStoneKey(
           64, 1, 16, 13, out _, ignoredStones: null) &&
       wallpaperObjectParityMap.IntersectsCollision(
           81, 2, 14, 14, includeHoles: false) &&
       wallpaperObjectParityMap.IntersectsCollision(
           112, 4, 16, 12, includeHoles: false) &&
       wallpaperObjectParityMap.IntersectsCollision(
           128, 3, 16, 13, includeHoles: false) &&
       wallpaperObjectParityMap.IntersectsCollision(
           160, 4, 16, 12, includeHoles: false),
       "Wallpaper maps must retain the game's four push-block sprites, liftable D6 statue, crystals, dungeon statues, exact anchors, layers, and collision rectangles.");
var wallpaperMoveStoneKey = wallpaperObjectParityMap.GetMoveStoneKey(0, 0);
Assert(wallpaperObjectParityMap.TryGetMoveStone(
           wallpaperMoveStoneKey, out var wallpaperMoveStoneX,
           out var wallpaperMoveStoneY, out var wallpaperMoveStoneDirections) &&
       wallpaperMoveStoneX == 0f && wallpaperMoveStoneY == 0f &&
       wallpaperMoveStoneDirections == 15 &&
       wallpaperObjectParityMap.IsPushableMoveStone(wallpaperMoveStoneKey) &&
       !wallpaperObjectParityMap.IntersectsCollision(
           0, 0, 16, 16, includeHoles: false,
           includeMoveStones: false),
       "Push blocks must retain their canonical cell key and default four-direction mask while remaining selectively traversable to the route planner.");
var wallpaperMoveStoneMapData = new System.Text.StringBuilder(
    "3\n0\n0\ndungeon.png\n7\n3\n1\n");
for (var row = 0; row < 3; row++)
    wallpaperMoveStoneMapData.AppendLine(string.Join(',', Enumerable.Repeat("0", 7)));
wallpaperMoveStoneMapData.AppendLine("3");
wallpaperMoveStoneMapData.AppendLine("c1");
wallpaperMoveStoneMapData.AppendLine("moveStone");
wallpaperMoveStoneMapData.AppendLine("fullHole");
wallpaperMoveStoneMapData.AppendLine("16");
for (var column = 0; column < 7; column++)
{
    wallpaperMoveStoneMapData.AppendLine($"0;{column * 16};0");
    wallpaperMoveStoneMapData.AppendLine($"0;{column * 16};32");
}
wallpaperMoveStoneMapData.AppendLine("1;32;16;4");
wallpaperMoveStoneMapData.AppendLine("2;48;16");
Assert(LiveWallpaperMap.TryLoad(
           new StringReader(wallpaperMoveStoneMapData.ToString()),
           out var wallpaperMoveStoneMap),
       "The push-block corridor regression fixture must load.");
Assert(LiveWallpaperMapViewport.TryCreateCentered(
           320, 240, wallpaperMoveStoneMap.Width,
           wallpaperMoveStoneMap.Height, 24f, 28f, 0.5f,
           out var wallpaperMoveStoneViewport),
       "The push-block corridor regression fixture must produce a viewport.");
var wallpaperMoveStonePlan = LiveWallpaperJourneyPlanner.CreateToPoint(
    wallpaperMoveStoneMap, wallpaperMoveStoneViewport,
    24f, 28f, 80f, 28f);
Assert(wallpaperMoveStonePlan.Points.Any(point =>
           point.Action == LiveWallpaperJourneyAction.PushBlock &&
           point.MoveStoneKey == wallpaperMoveStoneMap.GetMoveStoneKey(32, 16)),
       "A route through a permitted push block must schedule the real push before entering its occupied cell.");
var wallpaperMoveStoneSimulation = new LiveWallpaperLinkSimulation();
wallpaperMoveStoneSimulation.EnterMap(24f, 28f);
wallpaperMoveStoneSimulation.UpdateJourney(
    1, 0, 0L, true, wallpaperMoveStoneMap,
    wallpaperMoveStoneViewport, allowIslandLife: false);
Assert(wallpaperMoveStoneSimulation.TryWalkTo(
           wallpaperMoveStoneMap, wallpaperMoveStoneViewport, 80f, 28f),
       "The simulated Link must accept a tap route through a permitted push block.");
var sawMoveStonePush = false;
var sawMoveStoneMotion = false;
LiveWallpaperSimulatedLinkState wallpaperMoveStoneState = default;
for (var frame = 1; frame < 500; frame++)
{
    wallpaperMoveStoneState = wallpaperMoveStoneSimulation.UpdateJourney(
        1, 0, frame * 17L, true, wallpaperMoveStoneMap,
        wallpaperMoveStoneViewport, allowIslandLife: false);
    sawMoveStonePush |= wallpaperMoveStoneState.Action ==
                        LiveWallpaperLinkRouteAction.Pushing;
    if (wallpaperMoveStoneState.MoveStones?.TryGetValue(
            wallpaperMoveStoneMap.GetMoveStoneKey(32, 16),
            out var movedBlock) == true && movedBlock.X > 32.01f)
        sawMoveStoneMotion = true;
    if (wallpaperMoveStoneState.FallenMoveStones?.Contains(
            wallpaperMoveStoneMap.GetMoveStoneKey(32, 16)) == true)
        break;
}
Assert(sawMoveStonePush && sawMoveStoneMotion &&
       wallpaperMoveStoneState.FallenMoveStones?.Contains(
           wallpaperMoveStoneMap.GetMoveStoneKey(32, 16)) == true,
       "ObjMoveStone parity must show Link's push state, preserve the 500 ms inertia/450 ms one-tile motion, replace collision, and drop the block into a destination hole.");
const string wallpaperWellMapData =
    "3\n0\n0\noverworld.png\n5\n3\n1\n" +
    "0,0,0,0,0\n0,0,0,0,0\n0,0,0,0,0\n" +
    "2\nhole\nholeTeleporter\n2\n" +
    "0;32;16;12;12;;2;0\n" +
    "1;32;16;cave0.map;cave0\n";
Assert(LiveWallpaperMap.TryLoad(
           new StringReader(wallpaperWellMapData), out var wallpaperWellMap) &&
       wallpaperWellMap.Portals.Count == 1 &&
       wallpaperWellMap.Portals[0].IsHoleTeleporter &&
       wallpaperWellMap.Portals[0].HasDestination &&
       wallpaperWellMap.Portals[0].NextMap == "cave0.map" &&
       wallpaperWellMap.Portals[0].ExitId == "cave0" &&
       wallpaperWellMap.IntersectsHoleTeleporter(32, 16, 8, 10),
       "The wallpaper must retain ObjHoleTeleporter's destination over its real ObjHole collider.");
Assert(LiveWallpaperMapViewport.TryCreateCentered(
           160, 128, wallpaperWellMap.Width, wallpaperWellMap.Height,
           40, 32, 0.5f, out var wallpaperWellViewport),
       "The well regression fixture must have a valid centered viewport.");
var wallpaperWellSimulation = new LiveWallpaperLinkSimulation();
wallpaperWellSimulation.EnterMap(36f, 32f);
var wellFell = false;
var wellFoughtHolePull = false;
for (var frame = 0; frame < 120 && !wellFell; frame++)
{
    var link = wallpaperWellSimulation.UpdateJourney(
        1, 0, frame * 17L, true,
        wallpaperWellMap, wallpaperWellViewport,
        allowIslandLife: false, followLoadingZones: true);
    wellFell = link.Action == LiveWallpaperLinkRouteAction.Falling;
    if (!wellFell && link.Input.Move.LengthSquared() > 0.0001f)
        wellFoughtHolePull = true;
}
Assert(wellFell && !wellFoughtHolePull,
       "Once the well's canonical hole pull catches Link, journey movement must stop fighting it until the teleporter fall begins.");
const string wallpaperPitChestMapData =
    "3\n0\n0\ncave.png\n4\n2\n1\n" +
    "0,0,0,0\n0,0,0,0\n" +
    "2\ncaveBreakingFloor\nchest\n2\n" +
    "0;16;0\n1;32;0;ruby50;;c6_ruby50;1;false\n";
Assert(LiveWallpaperMap.TryLoad(
           new StringReader(wallpaperPitChestMapData), out var pitChestMap) &&
       pitChestMap.HazardCount == 1 &&
       pitChestMap.CollisionCount == 1 &&
       !pitChestMap.IntersectsCollision(
           20, 3, 8, 10, includeHoles: false) &&
       pitChestMap.IntersectsCollision(
           20, 3, 8, 10, includeHoles: true) &&
       Math.Abs(pitChestMap.GetLinkHoleCoverage(20, 3, 8, 10) - 1f) <
           0.001f &&
       pitChestMap.IntersectsCollision(
           32, 3, 16, 11, includeHoles: false) &&
       pitChestMap.Decorations.Count == 2 &&
       pitChestMap.Decorations[0].SpriteId == "chest_back" &&
       pitChestMap.Decorations[0].EntityX == 32 &&
       pitChestMap.Decorations[0].EntityY == 13 &&
       pitChestMap.Decorations[0].TopLeft &&
       pitChestMap.Decorations[0].DrawOffsetX == 0 &&
       pitChestMap.Decorations[0].DrawOffsetY == -13 &&
       pitChestMap.Decorations[0].SourceOffsetX == 32 &&
       pitChestMap.Decorations[1].SpriteId == "chest_front" &&
       pitChestMap.Decorations[1].EntityX == 32 &&
       pitChestMap.Decorations[1].EntityY == 13 &&
       !pitChestMap.Decorations[1].TopLeft &&
       pitChestMap.Decorations[1].DrawOffsetX == 0 &&
       pitChestMap.Decorations[1].DrawOffsetY == 0 &&
       pitChestMap.Decorations[1].SourceOffsetX == 32 &&
       LiveWallpaperChestItem.TryResolve("ruby50", out var rubyChestVisual) &&
       rubyChestVisual.SpriteId == "rubyBlue" &&
       rubyChestVisual.ShowAnimation == 1 &&
       LiveWallpaperChestItem.TryResolve(
           "pieceOfPower", out var powerChestVisual) &&
       powerChestVisual.SpriteId == "pieceOfPower" &&
       powerChestVisual.ShowAnimation == 2 &&
       !LiveWallpaperChestItem.TryResolve("greenZol", out _),
       "Wallpaper cave pits must expose ObjBreakingFloor's owned hole, and chests must retain both canonical sprites, depth, offsets, and collision.");
var chestJourneyMapData = new System.Text.StringBuilder(
    "3\n0\n0\noverworld.png\n20\n16\n1\n");
for (var row = 0; row < 16; row++)
    chestJourneyMapData.AppendLine(
        string.Join(',', Enumerable.Repeat("0", 20)));
chestJourneyMapData.Append(
    "1\nchest\n1\n0;160;112;ruby50;;wallpaper_chest;0;false\n");
Assert(LiveWallpaperMap.TryLoad(
           new StringReader(chestJourneyMapData.ToString()),
           out var chestJourneyMap),
       "The wallpaper chest journey fixture must load as a valid installed map.");
Assert(LiveWallpaperMapViewport.TryCreateCentered(
           160, 128, chestJourneyMap.Width, chestJourneyMap.Height,
           168, 136, 0.5f, out var chestJourneyViewport),
       "The wallpaper chest journey fixture must have a centered viewport.");
LiveWallpaperJourneyPlan chestJourney = null;
var chestJourneyVariant = -1;
for (var variant = 0; variant < 300; variant++)
{
    var candidate = LiveWallpaperJourneyPlanner.Create(
        chestJourneyMap, chestJourneyViewport, 1, variant,
        allowIslandLife: true);
    if (!candidate.Points.Any(point =>
            point.Action == LiveWallpaperJourneyAction.OpenChest))
        continue;
    chestJourney = candidate;
    chestJourneyVariant = variant;
    break;
}
var chestKey = chestJourneyMap.GetChestKey(160, 112);
LiveWallpaperJourneyPoint? chestPoint = null;
if (chestJourney != null)
{
    foreach (var point in chestJourney.Points)
    {
        if (point.Action != LiveWallpaperJourneyAction.OpenChest)
            continue;
        chestPoint = point;
        break;
    }
}
Assert(chestJourney != null && chestPoint.HasValue &&
       chestPoint.Value.PixelX == 168 && chestPoint.Value.PixelY == 136 &&
       chestPoint.Value.ChestKey == chestKey &&
       chestPoint.Value.ChestItemName == "ruby50",
       "Wallpaper journeys must approach an unopened item chest from below using ObjChest's upward-facing interaction rule.");
var alreadyOpenedChests = new HashSet<int> { chestKey };
for (var variant = 0; variant < 300; variant++)
{
    var candidate = LiveWallpaperJourneyPlanner.Create(
        chestJourneyMap, chestJourneyViewport, 1, variant,
        allowIslandLife: true, openedChests: alreadyOpenedChests);
    Assert(!candidate.Points.Any(point =>
               point.Action == LiveWallpaperJourneyAction.OpenChest),
           "An opened wallpaper chest must not be selected by a later journey.");
}
var chestSimulation = new LiveWallpaperLinkSimulation();
var sawChestOpen = false;
var sawChestItem = false;
var chestStartTime = chestJourneyVariant * 20_000L;
for (var frame = 0; frame < 4_000 && !sawChestItem; frame++)
{
    var link = chestSimulation.UpdateJourney(
        1, 0, chestStartTime + frame * 17L, true,
        chestJourneyMap, chestJourneyViewport, allowIslandLife: true);
    sawChestOpen |= link.Action == LiveWallpaperLinkRouteAction.OpenChest &&
                    link.Direction == 1 && link.ActiveChestKey == chestKey &&
                    link.OpenedChests?.Contains(chestKey) == true;
    sawChestItem |= link.Action == LiveWallpaperLinkRouteAction.ShowItem &&
                    link.Direction == 1 && link.ActiveChestKey == chestKey &&
                    link.ChestItemSpriteId == "rubyBlue" &&
                    link.ChestItemShowAnimation == 1;
}
Assert(sawChestOpen && sawChestItem,
       "Wallpaper Link must open the chest, retain its opened state, and present the resolved item above his head after the canonical opening delay.");
const string wallpaperAtlasData = "1\n1\nnote:262,185,7,12,0,0\n" +
                                  "bowwow chain:310,91,6,6,3,6\n";
Assert(LiveWallpaperAtlas.TryLoad(
           new StringReader(wallpaperAtlasData), "bowwow chain", out var chainEntry) &&
       chainEntry.X == 310 && chainEntry.Y == 91 &&
       chainEntry.Width == 6 && chainEntry.Height == 6 &&
       Math.Abs(chainEntry.OriginX - 3f) < 0.001f &&
       Math.Abs(chainEntry.OriginY - 6f) < 0.001f &&
       !LiveWallpaperAtlas.TryLoad(
           new StringReader("1\n1\nnote:1,2,-3,4,0,0\n"), "note", out _),
       "The live wallpaper must parse installed atlas entries and reject invalid bounds.");
Assert(LiveWallpaperLighting.Resolve(0, 4) == LiveWallpaperTimePhase.Night &&
       LiveWallpaperLighting.Resolve(0, 5) == LiveWallpaperTimePhase.Sunset &&
       LiveWallpaperLighting.Resolve(0, 7) == LiveWallpaperTimePhase.Day &&
       LiveWallpaperLighting.Resolve(0, 18) == LiveWallpaperTimePhase.Sunset &&
       LiveWallpaperLighting.Resolve(0, 21) == LiveWallpaperTimePhase.Night &&
       LiveWallpaperLighting.Resolve(1, 0) == LiveWallpaperTimePhase.Day &&
       LiveWallpaperLighting.Resolve(2, 12) == LiveWallpaperTimePhase.Sunset &&
       LiveWallpaperLighting.Resolve(3, 12) == LiveWallpaperTimePhase.Night,
       "The live wallpaper time-of-day mode must honor system-time boundaries and overrides.");
Assert(LiveWallpaperInteraction.NextFeaturedCharacter(0) == 1 &&
       LiveWallpaperInteraction.NextFeaturedCharacter(1) == 2 &&
       LiveWallpaperInteraction.NextFeaturedCharacter(2) == 0 &&
       LiveWallpaperInteraction.NextFeaturedCharacter(3) == 0 &&
       LiveWallpaperInteraction.NextScene(0) == 1 &&
       LiveWallpaperInteraction.NextScene(1) == 2 &&
       LiveWallpaperInteraction.NextScene(2) == 3 &&
       LiveWallpaperInteraction.NextScene(3) == 5 &&
       LiveWallpaperInteraction.NextScene(5) == 6 &&
       LiveWallpaperInteraction.NextScene(6) == 7 &&
       LiveWallpaperInteraction.NextScene(7) == 8 &&
       LiveWallpaperInteraction.NextScene(8) == 9 &&
       LiveWallpaperInteraction.NextScene(9) == 10 &&
       LiveWallpaperInteraction.NextScene(10) == 11 &&
       LiveWallpaperInteraction.NextScene(11) == 12 &&
       LiveWallpaperInteraction.NextScene(12) == 13 &&
       LiveWallpaperInteraction.NextScene(13) == 14 &&
       LiveWallpaperInteraction.NextScene(14) == 15 &&
       LiveWallpaperInteraction.NextScene(15) == 1 &&
       LiveWallpaperInteraction.NextScene(4) == 1 &&
       LiveWallpaperInteraction.NextScene(99) == 1,
       "Wallpaper tap actions must cycle characters and scenery predictably.");
Assert(LiveWallpaperSceneSelection.Resolve(4, 0, true) == 1 &&
       LiveWallpaperSceneSelection.Resolve(4, 44_999, true) == 1 &&
       LiveWallpaperSceneSelection.Resolve(4, 45_000, true) == 2 &&
       LiveWallpaperSceneSelection.Resolve(4, 90_000, true) == 3 &&
       LiveWallpaperSceneSelection.Resolve(4, 135_000, true) == 5 &&
       LiveWallpaperSceneSelection.Resolve(4, 180_000, true) == 6 &&
       LiveWallpaperSceneSelection.Resolve(4, 225_000, true) == 7 &&
       LiveWallpaperSceneSelection.Resolve(4, 270_000, true) == 8 &&
       LiveWallpaperSceneSelection.Resolve(4, 315_000, true) == 9 &&
       LiveWallpaperSceneSelection.Resolve(4, 360_000, true) == 10 &&
       LiveWallpaperSceneSelection.Resolve(4, 405_000, true) == 11 &&
       LiveWallpaperSceneSelection.Resolve(4, 450_000, true) == 12 &&
       LiveWallpaperSceneSelection.Resolve(4, 495_000, true) == 13 &&
       LiveWallpaperSceneSelection.Resolve(4, 540_000, true) == 14 &&
       LiveWallpaperSceneSelection.Resolve(4, 585_000, true) == 15 &&
       LiveWallpaperSceneSelection.Resolve(4, 630_000, true) == 1 &&
       LiveWallpaperSceneSelection.Resolve(0, 0, true) == 1 &&
       LiveWallpaperSceneSelection.Resolve(2, 0, true) == 2 &&
       LiveWallpaperSceneSelection.Resolve(2, 0, false) == 0 &&
       LiveWallpaperSceneSelection.TryGetTileOrigin(1, out var mabeX, out var mabeY) &&
       mabeX == 20 && mabeY == 72 &&
       LiveWallpaperSceneSelection.TryGetTileOrigin(2, out var shoreX, out var shoreY) &&
       shoreX == 10 && shoreY == 112 &&
       LiveWallpaperSceneSelection.TryGetTileOrigin(3, out var forestX, out var forestY) &&
       forestX == 10 && forestY == 32 &&
       LiveWallpaperSceneSelection.TryGetTileOrigin(5, out var castleX, out var castleY) &&
       castleX == 92 && castleY == 42 &&
       LiveWallpaperSceneSelection.TryGetTileOrigin(6, out var animalX, out var animalY) &&
       animalX == 129 && animalY == 99 &&
       LiveWallpaperSceneSelection.TryGetTileOrigin(7, out var eggX, out var eggY) &&
       eggX == 61 && eggY == 6 &&
       LiveWallpaperSceneSelection.TryGetTileOrigin(8, out var marthaX, out var marthaY) &&
       marthaX == 91 && marthaY == 100 &&
       LiveWallpaperSceneSelection.TryGetTileOrigin(9, out var prairieX, out var prairieY) &&
       prairieX == 55 && prairieY == 73 &&
       LiveWallpaperSceneSelection.TryGetTileOrigin(10, out var cemeteryX, out var cemeteryY) &&
       cemeteryX == 61 && cemeteryY == 61 &&
       LiveWallpaperSceneSelection.TryGetTileOrigin(11, out var swampX, out var swampY) &&
       swampX == 28 && swampY == 43 &&
       LiveWallpaperSceneSelection.TryGetTileOrigin(12, out var rapidsX, out var rapidsY) &&
       rapidsX == 124 && rapidsY == 34 &&
       LiveWallpaperSceneSelection.TryGetTileOrigin(13, out var heightsX, out var heightsY) &&
       heightsX == 132 && heightsY == 8 &&
       LiveWallpaperSceneSelection.TryGetTileOrigin(14, out var desertX, out var desertY) &&
       desertX == 145 && desertY == 111 &&
       LiveWallpaperSceneSelection.TryGetTileOrigin(15, out var shrineX, out var shrineY) &&
       shrineX == 124 && shrineY == 70 &&
       !LiveWallpaperSceneSelection.TryGetTileOrigin(0, out _, out _),
       "Installed wallpaper scenes must resolve stable overworld crops and rotation intervals.");
Assert(LiveWallpaperSceneSelection.GetRotationTransitionOpacity(1, 0) == 0f &&
       LiveWallpaperSceneSelection.GetRotationTransitionOpacity(4, 0) == 1f &&
       Math.Abs(LiveWallpaperSceneSelection.GetRotationTransitionOpacity(4, 600) - 0.5f) < 0.001f &&
       LiveWallpaperSceneSelection.GetRotationTransitionOpacity(4, 1_200) == 0f &&
       LiveWallpaperSceneSelection.GetRotationTransitionOpacity(4, 43_800) == 0f &&
       Math.Abs(LiveWallpaperSceneSelection.GetRotationTransitionOpacity(4, 44_400) - 0.5f) < 0.001f &&
       LiveWallpaperSceneSelection.GetRotationTransitionOpacity(4, 45_000) == 1f,
       "Automatic installed wallpaper scenes must fade around each location boundary.");
var walkingStart = LiveWallpaperLinkActivity.Resolve(0, 0, true);
var walkingMiddle = LiveWallpaperLinkActivity.Resolve(0, 7_000, true);
var resting = LiveWallpaperLinkActivity.Resolve(1, 9_000, true);
var alternatingRest = LiveWallpaperLinkActivity.Resolve(2, 12_000, true);
var alternatingFinish = LiveWallpaperLinkActivity.Resolve(2, 19_000, true);
var animationDisabled = LiveWallpaperLinkActivity.Resolve(0, 7_000, false);
var hidden = LiveWallpaperLinkActivity.Resolve(3, 0, true);
Assert(walkingStart.Visible && walkingStart.Walking && walkingStart.Journey == 0f &&
       walkingMiddle.Walking && Math.Abs(walkingMiddle.Journey - 0.5f) < 0.001f &&
       resting.Visible && !resting.Walking && resting.Journey == 0.5f &&
       alternatingRest.Visible && !alternatingRest.Walking && alternatingRest.Journey == 0.5f &&
       alternatingFinish.Walking && Math.Abs(alternatingFinish.Journey - 0.75f) < 0.001f &&
       animationDisabled.Visible && !animationDisabled.Walking &&
       !hidden.Visible,
       "Installed Link wallpaper activity must resolve walking, resting, and hidden states.");
var mabeRouteStart = LiveWallpaperLinkRoute.Resolve(1, 0f, true);
var mabeRouteReturn = LiveWallpaperLinkRoute.Resolve(1, 0.75f, true);
var standingRoute = LiveWallpaperLinkRoute.Resolve(1, 0.5f, false);
var forestJump = LiveWallpaperLinkRoute.Resolve(3, 5f / 12f, true);
var eggStairs = LiveWallpaperLinkRoute.Resolve(7, 0.25f, true);
Assert(Math.Abs(mabeRouteStart.MapX - 23.5f) < 0.001f &&
       Math.Abs(mabeRouteStart.MapY - 77.5f) < 0.001f &&
       mabeRouteStart.Direction == 2 &&
       mabeRouteReturn.Direction == 0 &&
       standingRoute.Action == LiveWallpaperLinkRouteAction.Stand &&
       forestJump.Action == LiveWallpaperLinkRouteAction.FeatherJump &&
       forestJump.JumpHeight > 0.99f &&
       Math.Abs(forestJump.MapX - 18.5f) < 0.001f &&
       eggStairs.Direction == 3 && Math.Abs(eggStairs.MapY - 17.5f) < 0.001f,
       "Wallpaper Link routes must stay map-aligned, reverse direction, and jump marked gaps.");
var mabeAtOneSecond = LiveWallpaperLinkActivity.ResolveForScene(0, 1, 1_000, true);
var mabeAtOneSecondRoute = LiveWallpaperLinkRoute.Resolve(
    1, mabeAtOneSecond.Journey, mabeAtOneSecond.Walking);
Assert(Math.Abs(mabeAtOneSecondRoute.MapX - 27.25f) < 0.001f &&
       mabeAtOneSecondRoute.Direction == 2,
       "Wallpaper Link must traverse routes at ObjLink's 1-pixel-per-frame walk speed.");
Assert(LiveWallpaperMapViewport.TryCreate(
           1080, 2400, 128, 1, 0.5f, out var linkPlacementViewport),
       "The Link placement regression fixture must produce a map viewport.");
var linkPlacementState = new LiveWallpaperSimulatedLinkState(
    23.5f, 77.5f, 0f, 3, LiveWallpaperLinkRouteAction.Walk, default);
var linkPlacement = LiveWallpaperLinkPlacement.Resolve(
    linkPlacementViewport, linkPlacementState);
var nextTilePlacement = LiveWallpaperLinkPlacement.Resolve(
    linkPlacementViewport,
    new LiveWallpaperSimulatedLinkState(
        24.5f, 77.5f, 0f, 3, LiveWallpaperLinkRouteAction.Walk, default));
var expectedEntityX = linkPlacementViewport.Left +
                      (linkPlacementState.MapX - linkPlacementViewport.OriginX) *
                      linkPlacementViewport.TileSize;
var expectedEntityY = linkPlacementViewport.Top +
                      (linkPlacementState.MapY - linkPlacementViewport.OriginY) *
                      linkPlacementViewport.TileSize;
Assert(Math.Abs(linkPlacement.Scale * 16f - linkPlacementViewport.TileSize) < 0.001f &&
       Math.Abs(linkPlacement.AnchorX -
                (expectedEntityX - 7f * linkPlacement.Scale)) < 0.001f &&
       Math.Abs(linkPlacement.AnchorY -
                (expectedEntityY - 16f * linkPlacement.Scale)) < 0.001f &&
       Math.Abs(nextTilePlacement.AnchorX - linkPlacement.AnchorX -
                linkPlacementViewport.TileSize) < 0.001f,
       "Wallpaper Link must use the map scale and ObjLink's real sprite offset.");
var wallpaperLinkSimulation = new LiveWallpaperLinkSimulation();
var simulatedWalkStart = wallpaperLinkSimulation.Update(
    3, new LiveWallpaperLinkState(true, true, 0.20f), 0, animated: true);
var simulatedFeather = wallpaperLinkSimulation.Update(
    3, new LiveWallpaperLinkState(true, true, 0.35f), 17, animated: true);
Assert(simulatedWalkStart.Input.Move == Microsoft.Xna.Framework.Vector2.Zero &&
       simulatedFeather.Input.Move.X > 0 && simulatedFeather.Input.FeatherPressed &&
       simulatedFeather.Height > 0 && !wallpaperLinkSimulation.Body.IsGrounded &&
       wallpaperLinkSimulation.Body.Velocity.Z > 0,
       "Wallpaper Link must translate the scripted route into real body movement and feather input.");
var preCarryStart = Microsoft.Xna.Framework.Vector3.Zero;
var preCarryTarget = new Microsoft.Xna.Framework.Vector3(16f, 16f, 13f);
var preCarryAtStart = LinkGameplayMotion.ResolvePreCarryPosition(
    preCarryStart, preCarryTarget, 0f);
var preCarryAtHalf = LinkGameplayMotion.ResolvePreCarryPosition(
    preCarryStart, preCarryTarget, 100f);
var preCarryAtEnd = LinkGameplayMotion.ResolvePreCarryPosition(
    preCarryStart, preCarryTarget, 200f);
var featherHeight = 0f;
var featherVelocity = LinkGameplayMotion.FeatherVelocity;
var featherFrames = 0;
var firstFeatherHeight = 0f;
do
{
    featherVelocity = LinkGameplayMotion.ApplyGravity(
        featherVelocity, LinkGameplayMotion.Gravity, 1f);
    var nextHeight = featherHeight + featherVelocity;
    featherFrames++;
    featherHeight = nextHeight > 0f ? nextHeight : 0f;
    if (featherFrames == 1)
        firstFeatherHeight = featherHeight;
} while (featherHeight > 0f && featherFrames < 100);
var steeredAirVelocity = LinkGameplayMotion.ResolveAirVelocity(
    new Microsoft.Xna.Framework.Vector2(1f, 0f),
    new Microsoft.Xna.Framework.Vector2(0f, 1f), 1f, 1f);
Assert(preCarryAtStart == preCarryStart &&
       Math.Abs(preCarryAtHalf.X - 1.663697f) < 0.0001f &&
       Math.Abs(preCarryAtHalf.Y - 1.663697f) < 0.0001f &&
       Math.Abs(preCarryAtHalf.Z - 5.772206f) < 0.0001f &&
       preCarryAtEnd == preCarryTarget &&
       Math.Abs(firstFeatherHeight - 2.2f) < 0.0001f &&
       featherFrames == LinkGameplayMotion.FeatherAirborneFramesAt60Fps &&
       LinkGameplayMotion.FeatherTravelFramesAt60Fps == 32 &&
       Math.Abs(steeredAirVelocity.X - 0.9646447f) < 0.0001f &&
       Math.Abs(steeredAirVelocity.Y - 0.0353553f) < 0.0001f,
       "Wallpaper pickup and feather motion must use ObjLink's exact pre-carry easing, gravity-first jump arc, duration, and airborne steering.");
Assert(!LinkGameplayMotion.BlocksInsideCollisionMovement(
           10f, 0f, 80f, 0.5f) &&
       LinkGameplayMotion.BlocksInsideCollisionMovement(
           10f, 20f, 80f, 0.5f) &&
       !LinkGameplayMotion.BlocksInsideCollisionMovement(
           80f, 80f, 80f, 0.5f) &&
       LinkGameplayMotion.BlocksInsideCollisionMovement(
           40f, 40f, 80f, 0.5f) &&
       !LinkGameplayMotion.BlocksInsideCollisionMovement(
           39f, 39f, 80f, 0.5f) &&
       Math.Abs(LinkGameplayMotion.ResolveHorizontalCornerNudge(
                    1288f, 10f, 1296f, 1312f,
                    LinkGameplayMotion.CornerCorrectionThreshold) + 2.01f) <
       0.0001f &&
       Math.Abs(LinkGameplayMotion.ResolveVerticalCornerNudge(
                    376f, 8f, 382f, 398f,
                    LinkGameplayMotion.CornerCorrectionThreshold) + 2.01f) <
       0.0001f,
       "Wallpaper collision escape and corner correction must use SystemBody's exact overlap thresholds and 0.01-pixel overcorrection.");
var constrainedMapData = new System.Text.StringBuilder(
    "3\n0\n0\noverworld.png\n40\n90\n1\n");
for (var row = 0; row < 90; row++)
    constrainedMapData.AppendLine(string.Join(',', Enumerable.Repeat("0", 40)));
constrainedMapData.Append("1\nc1\n1\n0;384;1296;;;\n");
Assert(LiveWallpaperMap.TryLoad(
           new StringReader(constrainedMapData.ToString()), out var constrainedMap),
       "The collision regression fixture must be a valid installed map.");
Assert(Math.Abs(constrainedMap.GetBlockingOverlapArea(
                    377f, 1296f, 8f, 10f, includeHoles: false) - 10f) <
       0.0001f &&
       constrainedMap.GetBlockingOverlapArea(
           376f, 1296f, 8f, 10f, includeHoles: false) == 0f &&
       constrainedMap.TryGetBlockingCollisionBounds(
           377f, 1296f, 8f, 10f, includeHoles: false,
           out var constrainedCollision) &&
       constrainedCollision.X == 384f && constrainedCollision.Y == 1296f &&
       constrainedCollision.Width == 16f && constrainedCollision.Height == 16f,
       "The lightweight map must expose the exact installed collider overlap and bounds used by SystemBody-compatible movement.");
var actorMapData =
    "3\n0\n0\noverworld.png\n2\n2\n1\n0,0\n0,0\n" +
    "4\npersonNew\ndogo\nenemy_respawner\nphonehouse\n4\n" +
    "0;16;16;;npc_green_boy;;stand_3\n1;0;0\n" +
    "2;32;16;e2;\n3;48;16\n";
Assert(LiveWallpaperMap.TryLoad(new StringReader(actorMapData), out var actorMap) &&
       actorMap.Actors.Count == 2 &&
       actorMap.Actors[0].Kind == LiveWallpaperMapActorKind.Person &&
       actorMap.Actors[0].AnimationId == "npc_green_boy" &&
       actorMap.Actors[0].AnimationName == "stand_3" &&
       actorMap.Actors[0].BodyX == 17 && actorMap.Actors[0].BodyY == 22 &&
       actorMap.Actors[0].BodyWidth == 14 && actorMap.Actors[0].BodyHeight == 10 &&
       actorMap.IntersectsActor(18, 23, 8, 8) &&
       actorMap.Actors[1].Kind == LiveWallpaperMapActorKind.Dog &&
       actorMap.Enemies.Count == 1 &&
       actorMap.Enemies[0].Kind == LiveWallpaperMapEnemyKind.Octorok &&
       actorMap.Enemies[0].EntityX == 40 && actorMap.Enemies[0].EntityY == 28 &&
       actorMap.IntersectsEnemy(34, 19, 8, 8) &&
       actorMap.Objects.Count == 4 &&
       actorMap.Objects[2].Template == "enemy_respawner" &&
       actorMap.Objects[2].Arguments[0] == "e2" &&
       actorMap.Decorations.Count == 1 &&
       actorMap.Decorations[0].SpriteId == "tree_phonehouse" &&
       actorMap.Decorations[0].EntityX == 72 &&
       actorMap.Decorations[0].EntityY == 40,
       "Wallpaper maps must retain installed NPCs, enemies, and atlas-backed building objects.");
var houseFixtureMapData =
    "3\n0\n0\nhouse.png\n4\n4\n1\n" +
    string.Concat(Enumerable.Repeat("0,0,0,0\n", 4)) +
    "5\nhouseObject\ndoorLight\nlamp\nlamp_wall_house_1\nlamp_wall_house_3\n5\n" +
    "0;0;0\n1;72;112\n2;16;16\n3;48;0\n4;0;48\n";
Assert(LiveWallpaperMap.TryLoad(
           new StringReader(houseFixtureMapData), out var houseFixtureMap) &&
       houseFixtureMap.IsHouse &&
       houseFixtureMap.Lamps.Count == 3 &&
       houseFixtureMap.Lamps[0].AnimationPath == "Objects/lamp_floor.ani" &&
       houseFixtureMap.Lamps[0].PlayerLayer &&
       houseFixtureMap.Lamps[0].PixelX == 16 &&
       houseFixtureMap.Lamps[0].PixelY == 16 &&
       houseFixtureMap.Lamps[0].EntityX == 24 &&
       houseFixtureMap.Lamps[0].EntityY == 24 &&
       houseFixtureMap.Lamps[1].AnimationPath == "Objects/lamp_wall_1.ani" &&
       houseFixtureMap.Lamps[1].Rotation == 1 &&
       !houseFixtureMap.Lamps[1].PlayerLayer &&
       houseFixtureMap.Lamps[2].Rotation == 3 &&
       houseFixtureMap.Lights.Count == 1 &&
       houseFixtureMap.Lights[0].CenterX == 80 &&
       houseFixtureMap.Lights[0].CenterY == 120 &&
       houseFixtureMap.Lights[0].Size == 128 &&
       houseFixtureMap.Lights[0].Alpha == 100 &&
       houseFixtureMap.IntersectsCollision(
           16, 16, 16, 16, includeHoles: true) &&
       !houseFixtureMap.IntersectsCollision(
           48, 0, 16, 16, includeHoles: true),
       "House fixtures must retain ObjHouse, ObjLight, and ObjLamp's exact markers, anchors, rotations, layers, and collision.");
const string overworldTeleporterFixtureData =
    "3\n0\n0\ntileset0.png\n2\n2\n1\n,,\n,,\n" +
    "1\noverworldTeleporter\n1\n0;16;16;0\n";
Assert(LiveWallpaperMap.TryLoad(
           new StringReader(overworldTeleporterFixtureData),
           out var overworldTeleporterFixture) &&
       overworldTeleporterFixture.Lamps.Count == 1 &&
       overworldTeleporterFixture.Lamps[0].AnimationPath ==
           "Objects/holeTeleporter.ani" &&
       overworldTeleporterFixture.Lamps[0].PixelX == 24 &&
       overworldTeleporterFixture.Lamps[0].PixelY == 24 &&
       overworldTeleporterFixture.Lamps[0].EntityX == 16 &&
       overworldTeleporterFixture.Lamps[0].EntityY == 16 &&
       !overworldTeleporterFixture.Lamps[0].PlayerLayer,
       "The overworld warp hole must use ObjOverworldTeleporter's installed holeTeleporter animation, anchor, and bottom layer.");
if (testInstalledWallpaperAssets)
using (var floorLampReader = File.OpenText(Path.Combine(
           wallpaperGameDataRoot, "Animations", "Objects", "lamp_floor.ani")))
using (var wallLampReader = File.OpenText(Path.Combine(
           wallpaperGameDataRoot, "Animations", "Objects", "lamp_wall_1.ani")))
{
    Assert(LiveWallpaperAnimation.TryLoad(
               floorLampReader, ["idle"], out var floorLampAnimation) &&
           LiveWallpaperAnimation.TryLoad(
               wallLampReader, ["idle"], out var wallLampAnimation) &&
           floorLampAnimation.Frames.Count == 6 &&
           floorLampAnimation.DurationMilliseconds == 1448 &&
           wallLampAnimation.Frames.Count == 6 &&
           wallLampAnimation.DurationMilliseconds == 1335,
           "Wallpaper lamps must use the installed game's exact animator frames and timing.");
}
var residentMapData =
    "3\n0\n0\nhouse.png\n20\n8\n1\n" +
    string.Concat(Enumerable.Repeat(
        string.Join(',', Enumerable.Repeat("0", 20)) + "\n", 8)) +
    "12\nperson\nalligator\nchickenDude\nhippo\npainter\ntracy\n" +
    "letterBoy\nletterGirl\nletterBird\nletterBirdGreen\nphotoMouse\nsign\n13\n" +
    "0;64;40;npc07;0.0.16.10;2.-1;stand\n" +
    "1;16;16\n2;32;16;chicken_dude\n3;48;16\n4;80;16\n" +
    "5;96;16\n5;64;48\n6;112;32;npc_letter_boy\n" +
    "7;128;32;npc_letter_girl\n8;144;32;\n9;160;32;\n" +
    "10;176;32;spawnMouseZora;mouseSeqZora\n10;192;32;;mouse\n";
Assert(LiveWallpaperMap.TryLoad(
           new StringReader(residentMapData), out var residentMap) &&
       residentMap.Actors.Count == 11 &&
       residentMap.Actors[0].Kind == LiveWallpaperMapActorKind.LegacyPerson &&
       residentMap.Actors[0].AnimationId == "npc07" &&
       residentMap.Actors[0].AnimationName == "stand" &&
       residentMap.Actors[0].BodyX == 64 &&
       residentMap.Actors[0].BodyY == 46 &&
       residentMap.Actors[0].BodyWidth == 16 &&
       residentMap.Actors[0].BodyHeight == 10 &&
       residentMap.Actors[0].SpriteOffsetX == 2 &&
       residentMap.Actors[0].SpriteOffsetY == -1 &&
       residentMap.Actors.Count(actor =>
           actor.Kind == LiveWallpaperMapActorKind.Tracy) == 1 &&
       residentMap.Actors.Count(actor =>
           actor.Kind == LiveWallpaperMapActorKind.PhotoMouse) == 1 &&
       residentMap.Actors.Any(actor =>
           actor.Kind == LiveWallpaperMapActorKind.Alligator &&
           actor.BodyX == 12 && actor.BodyY == 16 &&
           actor.BodyWidth == 20 && actor.BodyHeight == 16) &&
       residentMap.Decorations.Any(decoration =>
           decoration.SpriteId == "bananas" &&
           decoration.EntityX == 16 && decoration.EntityY == 52) &&
       residentMap.IntersectsCollision(
           8, 38, 16, 14, includeHoles: true) &&
       residentMap.Actors.Any(actor =>
           actor.Kind == LiveWallpaperMapActorKind.LetterBird &&
           actor.AnimationId == "letterBirdGreen"),
       "House residents must retain their canonical templates and bodies while fake Tracy and conditionally inactive Photo Mouse stay hidden.");
var residentSimulation = new LiveWallpaperActorSimulation.Session();
var chickenIndex = residentMap.Actors.ToList().FindIndex(actor =>
    actor.Kind == LiveWallpaperMapActorKind.ChickenDude);
var hippoIndex = residentMap.Actors.ToList().FindIndex(actor =>
    actor.Kind == LiveWallpaperMapActorKind.Hippo);
var letterBoyIndex = residentMap.Actors.ToList().FindIndex(actor =>
    actor.Kind == LiveWallpaperMapActorKind.LetterBoy);
var chickenIdle = residentSimulation.Resolve(
    residentMap, chickenIndex, 0L, null);
residentSimulation.Resolve(
    residentMap, chickenIndex, 300L, null);
var chickenPowder = residentSimulation.Resolve(
    residentMap, chickenIndex, 317L, null);
var hippo = residentMap.Actors[hippoIndex];
var hippoReactionLink = new LiveWallpaperSimulatedLinkState(
    (hippo.PixelX + 8f) / 16f,
    (hippo.PixelY + 33f) / 16f,
    0, 0, LiveWallpaperLinkRouteAction.Walk, default);
residentSimulation.Resolve(residentMap, hippoIndex, 0L, null);
var hippoEmbarrassed = residentSimulation.Resolve(
    residentMap, hippoIndex, 17L, hippoReactionLink);
var letterBoy = residentMap.Actors[letterBoyIndex];
var letterLookLink = new LiveWallpaperSimulatedLinkState(
    (letterBoy.PixelX + 18f) / 16f,
    (letterBoy.PixelY + 18f) / 16f,
    0, 0, LiveWallpaperLinkRouteAction.Walk, default);
residentSimulation.Resolve(residentMap, letterBoyIndex, 0L, null);
var letterLooking = residentSimulation.Resolve(
    residentMap, letterBoyIndex, 17L, letterLookLink);
var letterStillLooking = residentSimulation.Resolve(
    residentMap, letterBoyIndex, 200L, null);
var letterIdleAgain = residentSimulation.Resolve(
    residentMap, letterBoyIndex, 500L, null);
Assert(chickenIdle.Action == LiveWallpaperActorAction.Idle &&
       chickenPowder.Action == LiveWallpaperActorAction.Walk &&
       hippoEmbarrassed.Action == LiveWallpaperActorAction.Walk &&
       letterLooking.Action == LiveWallpaperActorAction.Walk &&
       letterStillLooking.Action == LiveWallpaperActorAction.Walk &&
       letterIdleAgain.Action == LiveWallpaperActorAction.Idle,
       "House resident ambience must retain Chicken Dude's powder timing, the hippo reaction, and the letter child's delayed look reset.");
var mobileActorMapData = new System.Text.StringBuilder(
    "3\n0\n0\noverworld.png\n80\n16\n1\n");
for (var row = 0; row < 16; row++)
    mobileActorMapData.AppendLine(string.Join(',', Enumerable.Repeat("0", 80)));
mobileActorMapData.Append(
    "8\ndogo\nbird\nbutterfly\nBowWow\nfrog\nmouse\nbobWowSmall\nletterBirdGreen\n8\n" +
    "0;32;48\n1;192;48\n2;352;48\n3;512;48\n" +
    "4;672;48\n5;832;48\n6;992;48;bowWow1\n7;1152;48\n");
Assert(LiveWallpaperMap.TryLoad(
           new StringReader(mobileActorMapData.ToString()),
           out var mobileActorMap) && mobileActorMap.Actors.Count == 8 &&
       mobileActorMap.Actors[4].Kind == LiveWallpaperMapActorKind.Frog &&
       mobileActorMap.Actors[4].BodyX == 674 &&
       mobileActorMap.Actors[4].BodyY == 56 &&
       mobileActorMap.Actors[4].BodyWidth == 12 &&
       mobileActorMap.Actors[4].BodyHeight == 8 &&
       mobileActorMap.Actors[5].Kind == LiveWallpaperMapActorKind.Mouse &&
       mobileActorMap.Actors[5].BodyX == 835 &&
       mobileActorMap.Actors[5].BodyY == 52 &&
       mobileActorMap.Actors[5].BodyWidth == 10 &&
       mobileActorMap.Actors[5].BodyHeight == 8 &&
       mobileActorMap.Actors[6].Kind == LiveWallpaperMapActorKind.BowWowSmall &&
       mobileActorMap.Actors[6].BodyX == 995 &&
       mobileActorMap.Actors[6].BodyY == 56 &&
       mobileActorMap.Actors[6].BodyWidth == 10 &&
       mobileActorMap.Actors[6].BodyHeight == 8 &&
       mobileActorMap.Actors[7].Kind == LiveWallpaperMapActorKind.LetterBird &&
       mobileActorMap.Actors[7].AnimationId == "letterBirdGreen",
       "The mobile wallpaper actor fixture must be a valid installed map.");
var owlMapData = new System.Text.StringBuilder(
    "3\n0\n0\noverworld.png\n20\n16\n1\n");
for (var row = 0; row < 16; row++)
    owlMapData.AppendLine(string.Join(',', Enumerable.Repeat("0", 20)));
owlMapData.Append(
    "1\nowl\n1\n0;64;64;owl_key;-16.32.48.32;false;owl_text;0;true\n");
Assert(LiveWallpaperMap.TryLoad(
           new StringReader(owlMapData.ToString()), out var owlMap) &&
       owlMap.Actors.Count == 1 &&
       owlMap.Actors[0].Kind == LiveWallpaperMapActorKind.Owl &&
       owlMap.Actors[0].TriggerX == 56 &&
       owlMap.Actors[0].TriggerY == 104 &&
       owlMap.Actors[0].TriggerWidth == 48 &&
       owlMap.Actors[0].TriggerHeight == 32 &&
       owlMap.IntersectsActor(66f, 72f, 12f, 8f) &&
       !owlMap.IntersectsActor(
           66f, 72f, 12f, 8f, ignoreOwl: true),
       "Wallpaper owls must retain ObjOwl's installed trigger rectangle while route planning treats their perch as transient.");
var owlSimulation = new LiveWallpaperActorSimulation.Session();
var owlApproachLink = new LiveWallpaperSimulatedLinkState(
    80f / 16f, 116f / 16f, 0f, 1,
    LiveWallpaperLinkRouteAction.Walk, default);
owlSimulation.Resolve(owlMap, 0, 0L, null);
var owlFlying = owlSimulation.Resolve(
    owlMap, 0, 17L, owlApproachLink);
LiveWallpaperActorState owlGone = default;
for (var frame = 2; frame <= 130; frame++)
    owlGone = owlSimulation.Resolve(owlMap, 0, frame * 17L, null);
var owlPathSimulation = new LiveWallpaperLinkSimulation();
owlPathSimulation.UpdateLiveActorState(owlMap, 0, owlFlying);
owlPathSimulation.BeginLiveStateFrame(owlMap);
var intersectsLiveActor = typeof(LiveWallpaperLinkSimulation).GetMethod(
    "IntersectsLiveActor", BindingFlags.Instance | BindingFlags.NonPublic);
var flyingOwlStillBlocks = intersectsLiveActor?.Invoke(
    owlPathSimulation, [owlMap, 66f, 72f, 12f, 8f]);
Assert(owlFlying.Action == LiveWallpaperActorAction.Fly &&
       owlFlying.Visible && !owlFlying.BlocksMovement &&
       owlGone.Action == LiveWallpaperActorAction.Hidden &&
       flyingOwlStillBlocks is false,
       "Approaching Link must start ObjOwl's canonical two-second fly-away, immediately release its pathing body, and leave it hidden after departure.");
var mobileActorSimulation = new LiveWallpaperActorSimulation.Session();
var mobileActorStarts = mobileActorMap.Actors.Select(actor =>
    (X: actor.Kind == LiveWallpaperMapActorKind.BowWow
            ? actor.PixelX
            : actor.PixelX + 8f,
     Y: actor.Kind == LiveWallpaperMapActorKind.Butterfly
            ? actor.PixelY + 23f
            : actor.Kind == LiveWallpaperMapActorKind.Mouse
                ? actor.PixelY + 12f
            : actor.PixelY + 16f)).ToArray();
var mobileActorMaximumDistance = new float[8];
var mobileActorMaximumHeight = new float[8];
LiveWallpaperActorState bowWowState = default;
for (var frame = 0; frame < 600; frame++)
{
    for (var actorIndex = 0; actorIndex < 8; actorIndex++)
    {
        var state = mobileActorSimulation.Resolve(
            mobileActorMap, actorIndex, frame * 17L, null);
        var deltaX = state.EntityX - mobileActorStarts[actorIndex].X;
        var deltaY = state.EntityY - mobileActorStarts[actorIndex].Y;
        mobileActorMaximumDistance[actorIndex] = Math.Max(
            mobileActorMaximumDistance[actorIndex],
            MathF.Sqrt(deltaX * deltaX + deltaY * deltaY));
        mobileActorMaximumHeight[actorIndex] = Math.Max(
            mobileActorMaximumHeight[actorIndex], state.Height);
        if (actorIndex == 3)
            bowWowState = state;
    }
}
var bowWowOriginX = mobileActorMap.Actors[3].PixelX + 8f;
var bowWowOriginY = mobileActorMap.Actors[3].PixelY + 8f;
var bowWowDistance = MathF.Sqrt(
    MathF.Pow(bowWowState.EntityX - bowWowOriginX, 2) +
    MathF.Pow(bowWowState.EntityY - 4f - bowWowOriginY, 2));
Assert(mobileActorMaximumDistance.All(distance => distance > 1f) &&
       mobileActorMaximumHeight[0] > 0f &&
       mobileActorMaximumHeight[1] > 0f &&
       mobileActorMaximumHeight[3] > 0f &&
       mobileActorMaximumHeight[4] > 0f &&
       mobileActorMaximumHeight[5] > 0f &&
       mobileActorMaximumHeight[6] > 0f &&
       mobileActorMaximumHeight[7] > 0f &&
       bowWowDistance <= 40.01f,
       "Installed dogs, cuccos, butterflies, frogs, mice, small BowWows, and BowWow must run their gameplay movement while BowWow remains chained.");
var fairyFloatPeak = LiveWallpaperActorSimulation.ResolveFairyHeight(
    550, animated: true);
var grandmotherActor = new LiveWallpaperMapActor(
    LiveWallpaperMapActorKind.Grandmother, 100, 100);
var grandmotherRightLink = new LiveWallpaperSimulatedLinkState(
    124f / 16f, 110f / 16f, 0, 0,
    LiveWallpaperLinkRouteAction.Interact, default,
    interactionActorIndex: 5);
var grandmotherLeftLink = new LiveWallpaperSimulatedLinkState(
    92f / 16f, 110f / 16f, 0, 0,
    LiveWallpaperLinkRouteAction.Interact, default,
    interactionActorIndex: 5);
var raccoonActor = new LiveWallpaperMapActor(
    LiveWallpaperMapActorKind.Raccoon, 100, 100);
var raccoonLaughLink = new LiveWallpaperSimulatedLinkState(
    64f / 16f, 70f / 16f, 0, 0,
    LiveWallpaperLinkRouteAction.Walk, default);
var dogActor = mobileActorMap.Actors[0];
var dogStaticApproach = new Microsoft.Xna.Framework.Vector2(
    dogActor.BodyX + dogActor.BodyWidth + 8f,
    dogActor.BodyY + dogActor.BodyHeight / 2f + 5f);
var dogLiveApproach = LiveWallpaperActorSimulation.ResolveInteractionApproach(
    dogActor,
    new LiveWallpaperActorState(
        mobileActorStarts[0].X + 24f,
        mobileActorStarts[0].Y + 8f,
        0f, 1, LiveWallpaperActorAction.Walk),
    dogStaticApproach);
Assert(Math.Abs(fairyFloatPeak - 16f) < 0.001f &&
       Math.Abs(LiveWallpaperActorSimulation.ResolveFairyHeight(
           550, animated: false) - 12f) < 0.001f &&
       LiveWallpaperActorSimulation.ResolveGrandmotherDirection(
           grandmotherActor, -1, grandmotherRightLink) == 1 &&
       LiveWallpaperActorSimulation.ResolveGrandmotherDirection(
           grandmotherActor, 1, grandmotherLeftLink) == -1 &&
       LiveWallpaperActorSimulation.ShouldRaccoonLaugh(
           raccoonActor, raccoonLaughLink) &&
       LiveWallpaperActorSimulation.IsInteraction(
           grandmotherRightLink, 5) &&
       !LiveWallpaperActorSimulation.IsInteraction(
           grandmotherRightLink, 4) &&
       Math.Abs(dogLiveApproach.X - dogStaticApproach.X - 24f) < 0.001f &&
       Math.Abs(dogLiveApproach.Y - dogStaticApproach.Y - 8f) < 0.001f,
       "Installed NPC ambience must retain the fairy float, grandmother facing, raccoon laugh trigger, and interaction targeting from gameplay.");
var visualObjectMapData =
    "3\n0\n0\noverworld.png\n8\n2\n1\n0,0,0,0,0,0,0,0\n0,0,0,0,0,0,0,0\n" +
    "10\ntree0\nfence\nflower\naquaticPlant\nbush\ngrasForest\nstone\ngravestone\ntree9\noverworldDonut\n10\n" +
    "0;0;0\n1;16;0\n2;32;0\n3;48;0\n4;64;16\n5;80;16\n6;96;15\n7;112;16\n" +
    "8;128;16\n9;160;16\n";
Assert(LiveWallpaperMap.TryLoad(
           new StringReader(visualObjectMapData), out var visualObjectMap) &&
       Enumerable.Range(1, visualObjectMap.Decorations.Count - 1).All(index =>
       {
           var previous = visualObjectMap.Decorations[index - 1];
           var current = visualObjectMap.Decorations[index];
           var previousLayer = previous.PlayerLayer ? 1 : 0;
           var currentLayer = current.PlayerLayer ? 1 : 0;
           return previousLayer < currentLayer ||
                  previousLayer == currentLayer &&
                  previous.EntityY <= current.EntityY;
       }) &&
       visualObjectMap.Decorations.Count == 13 &&
       visualObjectMap.Decorations.Any(item => item.SpriteId == "tree_0") &&
       visualObjectMap.Decorations.Any(item =>
           item.SpriteId == "tree_9" &&
           item.EntityX == 144 && item.EntityY == 40 && item.PlayerLayer) &&
       visualObjectMap.Decorations.Count(item => item.SpriteId == "fence") == 4 &&
       visualObjectMap.Decorations.Any(item =>
           item.SpriteId == "aquatic_plant_top" && item.TopLeft) &&
       visualObjectMap.Decorations.Any(item =>
           item.SpriteId == "bush_0" &&
           item.EntityX == 72 && item.EntityY == 24 && item.PlayerLayer) &&
       visualObjectMap.Decorations.Any(item =>
           item.SpriteId == "grass_1" &&
           item.EntityX == 88 && item.EntityY == 24 && !item.PlayerLayer) &&
       visualObjectMap.Decorations.Any(item =>
           item.SpriteId == "stone_0" &&
           item.EntityX == 104 && item.EntityY == 28 && item.PlayerLayer) &&
       visualObjectMap.Decorations.Any(item =>
           item.SpriteId == "gravestone" &&
           item.EntityX == 120 && item.EntityY == 32 && item.PlayerLayer) &&
       visualObjectMap.Decorations.Any(item =>
           item.SpriteId == "overworldDonut" &&
           item.EntityX == 168 && item.EntityY == 16 && item.PlayerLayer) &&
       visualObjectMap.IntersectsCollision(
           96, 17, 16, 13, includeHoles: false) &&
       visualObjectMap.TryGetStoneKey(
           96, 17, 16, 13, out var visualStoneKey) &&
       visualObjectMap.Decorations.Any(item =>
           item.SpriteId == "stone_0" &&
           visualObjectMap.GetStoneKey(item) == visualStoneKey &&
           visualObjectMap.TryGetStoneMapPosition(
               visualStoneKey, out var exactStoneX, out var exactStoneY) &&
           exactStoneX == 96f && exactStoneY == 15f) &&
       !visualObjectMap.IntersectsCollision(
           96, 17, 16, 13, includeHoles: false, includeStones: false) &&
       !visualObjectMap.IntersectsCollision(
           96, 17, 16, 13, includeHoles: false,
           ignoredStones: new HashSet<int> { visualStoneKey }) &&
       visualObjectMap.IntersectsCollision(
           112, 20, 16, 12, includeHoles: false) &&
       visualObjectMap.IntersectsCollision(
           160, 16, 16, 16, includeHoles: false) &&
       visualObjectMap.TryGetBushKey(64, 17, 16, 14, out var visualBushKey) &&
       !visualObjectMap.TryGetBushKey(80, 16, 16, 16, out _) &&
       visualObjectMap.TryGetCuttableVegetationKey(
           80, 16, 16, 16, out var visualGrassKey) &&
       visualGrassKey == visualObjectMap.GetBushKey(80, 16) &&
       !visualObjectMap.IntersectsCollision(
           64, 17, 16, 14, includeHoles: false, includeBushes: false) &&
       !visualObjectMap.IntersectsCollision(
           64, 17, 16, 14, includeHoles: false,
           ignoredBushes: new HashSet<int> { visualBushKey }) &&
       visualObjectMap.AnimatedTiles.Count == 1 &&
       visualObjectMap.AnimatedTiles[0].SpriteId == "flower_0",
       "Wallpaper maps must use the installed GameObjectTemplates visuals instead of a building-only whitelist.");
Assert(GameObjectVisualLayout.TryGetClassicLeafState(
           0, 0, out var leafStart, out var leafFlipX,
           out var leafFlipY, out var leafStartAlpha) &&
       leafStart == new Microsoft.Xna.Framework.Vector2(-4, 2) &&
       !leafFlipX && !leafFlipY && leafStartAlpha == 1f &&
       GameObjectVisualLayout.TryGetClassicLeafState(
           3, GameObjectVisualLayout.ClassicLeafAnimationMilliseconds + 60,
           out _, out _, out _, out var leafFadeAlpha) &&
       Math.Abs(leafFadeAlpha - 0.5f) < 0.001f &&
       !GameObjectVisualLayout.TryGetClassicLeafState(
           0, GameObjectVisualLayout.ClassicLeafAnimationMilliseconds +
              GameObjectVisualLayout.ClassicLeafFadeMilliseconds,
           out _, out _, out _, out _),
       "Wallpaper vegetation effects must use ObjLeafClassic's exact path and fade timings.");
var terrainMapData =
    "3\n0\n0\noverworld.png\n4\n1\n1\n0,,0,0\n" +
    "3\nwater\nwaterDeep\nwave1\n3\n0;0;0;-2\n1;32;0\n2;0;0\n";
Assert(LiveWallpaperMap.TryLoad(new StringReader(terrainMapData), out var terrainMap) &&
       terrainMap.GetTerrain(0, 0) == LiveWallpaperMapTerrain.Water &&
       terrainMap.GetTerrain(1, 0) == LiveWallpaperMapTerrain.Void &&
       terrainMap.GetTerrain(2, 0) == LiveWallpaperMapTerrain.DeepWater &&
       terrainMap.GetTerrain(3, 0) == LiveWallpaperMapTerrain.Ground &&
       terrainMap.IsWaterAt(8, 8) && !terrainMap.IsDeepWaterAt(8, 8) &&
       terrainMap.IsWaterAt(40, 8) && terrainMap.IsDeepWaterAt(40, 8) &&
       terrainMap.IntersectsVoid(17, 1, 14, 14) &&
       terrainMap.AnimatedTiles.Count == 1 &&
       terrainMap.AnimatedTiles[0].SpriteId == "water_0" &&
       terrainMap.AnimatedTiles[0].FrameCount == 8 &&
       terrainMap.AnimatedTiles[0].FrameDurationMilliseconds == 125,
       "Wallpaper maps must classify water and void and retain installed water animation objects.");
const string oceanBaseMapData =
    "3\n0\n0\ntileset0.png\n3\n1\n1\n,235,\n" +
    "2\nwave3\nwave4\n2\n0;0;0\n1;16;0\n";
Assert(LiveWallpaperMap.TryLoad(new StringReader(oceanBaseMapData), out var oceanBaseMap) &&
       oceanBaseMap.NeedsOverworldOceanBase(oceanBaseMap.AnimatedTiles[0]) &&
       !oceanBaseMap.NeedsOverworldOceanBase(oceanBaseMap.AnimatedTiles[1]) &&
       oceanBaseMap.GetTile(0, 0, 0) == -1 &&
       oceanBaseMap.GetTerrain(2, 0) == LiveWallpaperMapTerrain.Void,
       "Ocean rendering must fill only empty transparent-wave cells without overwriting existing tiles or changing route terrain.");
var terminalDoor = new LiveWallpaperMapPortal(
    64, 192, 16, 16, 1, 0, "entry", "room.map", "exit", is2dDoor: true);
Assert(terminalDoor.ShouldActivateAt(72f, 208f, 0f, 1) &&
       terminalDoor.ShouldActivateAt(72f, 208f, -1f, 3) &&
       !terminalDoor.ShouldActivateAt(72f, 208f, 0f, 3) &&
       !terminalDoor.ShouldActivateAt(72f, 216f, -1f, 1),
       "A 2D door must accept its upward terminal frame but reject the wrong facing or a distant position.");
var journeyMapData = new System.Text.StringBuilder(
    "3\n0\n0\noverworld.png\n40\n90\n1\n");
for (var row = 0; row < 90; row++)
    journeyMapData.AppendLine(string.Join(',', Enumerable.Repeat("0", 40)));
journeyMapData.Append(
    "4\npersonNew\ndoor\nenemy_respawner\nmoblinSword\n4\n" +
    "0;384;1248;;npc_green_boy;;stand_3\n" +
    "1;416;1216;;;cave_rooster;cave rooster.map;cave_rooster;3;0;\n" +
    "2;400;1280;e2;\n" +
    "3;1000;1000;\n");
Assert(LiveWallpaperMap.TryLoad(
           new StringReader(journeyMapData.ToString()), out var journeyMap) &&
       journeyMap.Portals.Count == 1 &&
       Math.Abs(journeyMap.Portals[0].LinkTargetX - 424f) < 0.001f &&
       Math.Abs(journeyMap.Portals[0].LinkTargetY - 1232f) < 0.001f &&
       journeyMap.Portals[0].EntryId == "cave_rooster" &&
       journeyMap.Portals[0].NextMap == "cave rooster.map" &&
       journeyMap.Portals[0].ExitId == "cave_rooster" &&
       journeyMap.Portals[0].HasDestination,
       "Wallpaper maps must expose ObjDoor-compatible targets and canonical destination metadata.");
var enemySession = new LiveWallpaperEnemySimulation.Session();
var enemyStart = enemySession.Resolve(journeyMap, 0, 0, null);
var enemyMoved = false;
for (var frame = 1; frame <= 120; frame++)
{
    var enemyFrame = enemySession.Resolve(journeyMap, 0, frame * 17L, null);
    enemyMoved |= Math.Abs(enemyFrame.PixelX - enemyStart.PixelX) > 0.5f ||
                  Math.Abs(enemyFrame.PixelY - enemyStart.PixelY) > 0.5f;
}
Assert(enemyMoved,
       "Wallpaper enemies must retain movement state across frames instead of looping around their spawn formula.");
var sawOctorokShot = false;
var enemyTime = 120 * 17L;
var trackedEnemy = enemySession.Resolve(journeyMap, 0, enemyTime, null);
for (var frame = 1; frame <= 600 && !sawOctorokShot; frame++)
{
    var facing = trackedEnemy.Direction switch
    {
        0 => new Microsoft.Xna.Framework.Vector2(-32, 0),
        1 => new Microsoft.Xna.Framework.Vector2(0, -32),
        2 => new Microsoft.Xna.Framework.Vector2(32, 0),
        _ => new Microsoft.Xna.Framework.Vector2(0, 32)
    };
    var targetLink = new LiveWallpaperSimulatedLinkState(
        (trackedEnemy.PixelX + facing.X) / 16f,
        (trackedEnemy.PixelY + facing.Y) / 16f,
        0, trackedEnemy.Direction, LiveWallpaperLinkRouteAction.Stand,
        new LiveWallpaperLinkInput(Microsoft.Xna.Framework.Vector2.Zero, false));
    enemyTime += 17;
    trackedEnemy = enemySession.Resolve(
        journeyMap, 0, enemyTime, targetLink);
    sawOctorokShot |= trackedEnemy.Projectile.Kind ==
                       LiveWallpaperEnemyProjectileKind.OctorokShot;
}
Assert(sawOctorokShot,
       "Octoroks must fire their real projectile when Link is in range and in their facing direction.");
enemyTime += 17;
var contactLink = new LiveWallpaperSimulatedLinkState(
    trackedEnemy.PixelX / 16f, trackedEnemy.PixelY / 16f,
    0, trackedEnemy.Direction, LiveWallpaperLinkRouteAction.Stand,
    new LiveWallpaperLinkInput(Microsoft.Xna.Framework.Vector2.Zero, false));
var enemyContact = enemySession.Resolve(
    journeyMap, 0, enemyTime, contactLink);
Assert(enemyContact.LinkHit.Valid && enemyContact.LinkHit.Damage == 2 &&
       Math.Abs(enemyContact.LinkHit.PushMultiplier - 1.85f) < 0.001f,
       "An Octorok body or projectile must apply its real two-damage player hit and default push multiplier.");
var damagedLinkSimulation = new LiveWallpaperLinkSimulation();
Assert(damagedLinkSimulation.ApplyEnemyHit(enemyContact.LinkHit, enemyTime) &&
       !damagedLinkSimulation.ApplyEnemyHit(enemyContact.LinkHit, enemyTime + 17) &&
       !damagedLinkSimulation.IsDamageVisible(enemyTime) &&
       damagedLinkSimulation.IsDamageVisible(enemyTime + 66),
       "Wallpaper Link damage must use ObjLink's 66 ms blink and 1056 ms default invulnerability window.");
var attackingLink = new LiveWallpaperSimulatedLinkState(
    trackedEnemy.PixelX / 16f, trackedEnemy.PixelY / 16f,
    0, trackedEnemy.Direction, LiveWallpaperLinkRouteAction.Attack,
    new LiveWallpaperLinkInput(Microsoft.Xna.Framework.Vector2.Zero, false),
    combatEnemyIndex: 0, actionProgress: 0.5f,
    attackBox: new LiveWallpaperAttackBox(
        trackedEnemy.PixelX - 8, trackedEnemy.PixelY - 16, 16, 16));
enemyTime += 17;
var enemyStruck = enemySession.Resolve(
    journeyMap, 0, enemyTime, attackingLink);
enemyTime += 500;
var enemyDead = enemySession.Resolve(
    journeyMap, 0, enemyTime,
    new LiveWallpaperSimulatedLinkState(
        trackedEnemy.PixelX / 16f, trackedEnemy.PixelY / 16f,
        0, trackedEnemy.Direction, LiveWallpaperLinkRouteAction.Stand,
        new LiveWallpaperLinkInput(Microsoft.Xna.Framework.Vector2.Zero, false)));
Assert((enemyStruck.Action is LiveWallpaperEnemyAction.Hit or
            LiveWallpaperEnemyAction.Hidden) && !enemyDead.Visible,
       "A level-one sword hit must consume Octorok's real one HP, blink on the real cooldown, and remove it.");
Assert(LiveWallpaperMapViewport.TryCreate(
           1080, 2400, journeyMap.Height, 1, 0.5f, out var journeyViewport),
       "The wallpaper journey regression fixture must produce a map viewport.");
var holeResetAlignmentSimulation = new LiveWallpaperLinkSimulation();
holeResetAlignmentSimulation.EnterMap(319f, 1100f);
holeResetAlignmentSimulation.Body.Position.Set(
    new Microsoft.Xna.Framework.Vector3(321f, 1100f, 0f));
var updateHoleResetPosition = typeof(LiveWallpaperLinkSimulation).GetMethod(
    "UpdateHoleResetPosition", BindingFlags.Instance | BindingFlags.NonPublic);
var holeResetPositionField = typeof(LiveWallpaperLinkSimulation).GetField(
    "_holeResetPosition", BindingFlags.Instance | BindingFlags.NonPublic);
updateHoleResetPosition?.Invoke(holeResetAlignmentSimulation, [journeyMap]);
var alignedHoleResetPosition = holeResetPositionField?.GetValue(
    holeResetAlignmentSimulation) as Microsoft.Xna.Framework.Vector2?;
Assert(alignedHoleResetPosition.HasValue &&
       alignedHoleResetPosition.Value ==
           new Microsoft.Xna.Framework.Vector2(328f, 1100f),
       "Hole recovery must use ObjLink.UpdateSavePosition's tile alignment and eight-pixel inward field buffer.");
var blockingMoblinSimulation = new LiveWallpaperLinkSimulation();
blockingMoblinSimulation.UpdateJourney(
    1, 0, 0L, true, journeyMap, journeyViewport,
    allowIslandLife: true, followLoadingZones: false);
blockingMoblinSimulation.EnterMap(448f, 1120f);
Assert(blockingMoblinSimulation.TryWalkTo(
           journeyMap, journeyViewport, 504f, 1120f),
       "The live Moblin regression must begin with an unobstructed planned route.");
blockingMoblinSimulation.UpdateLiveEnemyState(
    journeyMap, 1, new LiveWallpaperEnemyState(
        472f, 1120f, 0, LiveWallpaperEnemyAction.Attack));
Assert(blockingMoblinSimulation.ApplyEnemyHit(
           new LiveWallpaperLinkHit(472f, 1120f, 2, 1.85f), 1L),
       "The Sword Moblin regression must apply its real knockback first.");
var attackedBlockingMoblin = false;
LiveWallpaperSimulatedLinkState blockingMoblinLink = default;
for (var frame = 1; frame <= 360 && !attackedBlockingMoblin; frame++)
{
    blockingMoblinLink = blockingMoblinSimulation.UpdateJourney(
        1, 0, frame * 17L, true, journeyMap, journeyViewport,
        allowIslandLife: true, followLoadingZones: false);
    attackedBlockingMoblin =
        blockingMoblinLink.Action == LiveWallpaperLinkRouteAction.Attack &&
        blockingMoblinLink.CombatEnemyIndex == 1;
}
Assert(attackedBlockingMoblin,
       $"After Moblin knockback, a live enemy blocking the resumed route must become Link's sword target instead of deadlocking movement ({blockingMoblinLink.MapX * 16f},{blockingMoblinLink.MapY * 16f},{blockingMoblinLink.Action},{blockingMoblinLink.CombatEnemyIndex}).");
var seaUrchinPathMapData = new System.Text.StringBuilder(
    "3\n0\n0\noverworld.png\n40\n90\n1\n");
for (var row = 0; row < 90; row++)
    seaUrchinPathMapData.AppendLine(string.Join(',', Enumerable.Repeat("0", 40)));
seaUrchinPathMapData.Append("1\nenemy_respawner\n1\n0;472;1120;e1;\n");
Assert(LiveWallpaperMap.TryLoad(
           new StringReader(seaUrchinPathMapData.ToString()),
           out var seaUrchinPathMap) &&
       seaUrchinPathMap.Enemies.Count == 1 &&
       seaUrchinPathMap.Enemies[0].Kind ==
           LiveWallpaperMapEnemyKind.SeaUrchin,
       "The route-blocking Sea Urchin regression fixture must be valid.");
var seaUrchinCombatSimulation = new LiveWallpaperLinkSimulation();
seaUrchinCombatSimulation.EnterMap(460f, 1133f);
seaUrchinCombatSimulation.UpdateLiveEnemyState(
    seaUrchinPathMap, 0, new LiveWallpaperEnemyState(
        480f, 1136f, 3, LiveWallpaperEnemyAction.Idle));
var startBlockingEnemyAttack = typeof(LiveWallpaperLinkSimulation).GetMethod(
    "TryStartBlockingEnemyAttack", BindingFlags.Instance | BindingFlags.NonPublic);
object[] seaUrchinAttackArguments =
[
    seaUrchinPathMap, Microsoft.Xna.Framework.Vector2.UnitX, 17L,
    Microsoft.Xna.Framework.Vector2.Zero
];
var attackedSeaUrchin = startBlockingEnemyAttack?.Invoke(
    seaUrchinCombatSimulation, seaUrchinAttackArguments);
Assert(attackedSeaUrchin is true &&
       seaUrchinAttackArguments[3] is Microsoft.Xna.Framework.Vector2
           seaUrchinAttackDirection && seaUrchinAttackDirection.X > 0f,
       "Any live enemy within the canonical sword approach corridor, including a stationary Sea Urchin, must trigger Link's normal attack.");
var removedSeaUrchinSimulation = new LiveWallpaperLinkSimulation();
removedSeaUrchinSimulation.EnterMap(460f, 1133f);
removedSeaUrchinSimulation.UpdateLiveEnemyState(
    seaUrchinPathMap, 0, new LiveWallpaperEnemyState(
        480f, 1136f, 3, LiveWallpaperEnemyAction.Hidden));
removedSeaUrchinSimulation.BeginLiveStateFrame(seaUrchinPathMap);
object[] removedSeaUrchinAttackArguments =
[
    seaUrchinPathMap, Microsoft.Xna.Framework.Vector2.UnitX, 34L,
    Microsoft.Xna.Framework.Vector2.Zero
];
var attackedRemovedSeaUrchin = startBlockingEnemyAttack?.Invoke(
    removedSeaUrchinSimulation, removedSeaUrchinAttackArguments);
Assert(attackedRemovedSeaUrchin is false,
       "A killed Sea Urchin must remain absent from collision and sword targeting when viewport culling begins the next live-state frame.");
var collisionEscapeSimulation = new LiveWallpaperLinkSimulation();
collisionEscapeSimulation.UpdateJourney(
    1, 0, 0L, true, constrainedMap, journeyViewport,
    allowIslandLife: true, followLoadingZones: false);
collisionEscapeSimulation.Body.Position.Set(
    new Microsoft.Xna.Framework.Vector3(381f, 1306f, 0f));
Assert(collisionEscapeSimulation.TryWalkTo(
           constrainedMap, journeyViewport, 360f, 1306f),
       "A continuation beginning partly inside installed collision must still produce a route out.");
LiveWallpaperSimulatedLinkState collisionEscapeLink = default;
for (var frame = 1; frame <= 180; frame++)
{
    collisionEscapeLink = collisionEscapeSimulation.UpdateJourney(
        1, 0, frame * 17L, true, constrainedMap, journeyViewport,
        allowIslandLife: true, followLoadingZones: false);
}
Assert(collisionEscapeLink.MapX * 16f < 376f &&
       constrainedMap.GetBlockingOverlapArea(
           collisionEscapeLink.MapX * 16f - 4f,
           collisionEscapeLink.MapY * 16f - 10f,
           8f, 10f, includeHoles: false) == 0f,
       "Link must reduce an inherited transition overlap and leave the collider instead of walking in place until replanning.");
var cornerCorrectionSimulation = new LiveWallpaperLinkSimulation();
cornerCorrectionSimulation.Body.Position.Set(
    new Microsoft.Xna.Framework.Vector3(376f, 1298f, 0f));
var constrainedMovement = typeof(LiveWallpaperLinkSimulation).GetMethod(
    "ApplyJourneyConstrainedMovement",
    BindingFlags.Instance | BindingFlags.NonPublic);
var correctedCorner = constrainedMovement?.Invoke(
    cornerCorrectionSimulation,
    [constrainedMap, new Microsoft.Xna.Framework.Vector2(12f, 0f), true, true]);
Assert(correctedCorner is true &&
       cornerCorrectionSimulation.Body.Position.X > 387f &&
       cornerCorrectionSimulation.Body.Position.Y < 1296f,
       "Link must apply ObjLink's real corner correction when grazing a collider instead of stopping at the corner.");
var featherGapMapData = new System.Text.StringBuilder(
    "3\n0\n0\noverworld.png\n40\n90\n1\n");
for (var row = 0; row < 90; row++)
    featherGapMapData.AppendLine(string.Join(',', Enumerable.Repeat("0", 40)));
featherGapMapData.AppendLine("1");
featherGapMapData.AppendLine("fullHole");
featherGapMapData.AppendLine("8");
for (var gapRow = 72; gapRow < 80; gapRow++)
    featherGapMapData.AppendLine($"0;400;{gapRow * 16}");
Assert(LiveWallpaperMap.TryLoad(
           new StringReader(featherGapMapData.ToString()), out var featherGapMap),
       "The exact-feather journey fixture must be a valid installed map.");
LiveWallpaperJourneyPlan featherGapPlan = null;
var featherGapVariant = -1;
for (var variant = 0; variant < 120 && featherGapPlan == null; variant++)
{
    var candidate = LiveWallpaperJourneyPlanner.Create(
        featherGapMap, journeyViewport, 1, variant,
        allowIslandLife: true, followLoadingZones: true);
    if (candidate.Points.Any(point =>
            point.Action == LiveWallpaperJourneyAction.FeatherJump) &&
        candidate.Points.Min(point => point.PixelX) < 392f &&
        candidate.Points.Max(point => point.PixelX) > 408f)
    {
        featherGapPlan = candidate;
        featherGapVariant = variant;
    }
}
Assert(featherGapPlan != null &&
       featherGapPlan.Points.Count(point =>
           point.Action == LiveWallpaperJourneyAction.FeatherJump) == 1,
       "A traversable gap must schedule one real feather press at its takeoff edge, not repeated jump markers across the pit.");
var featherGapPointIndex = Enumerable.Range(0, featherGapPlan.Points.Count)
    .First(index => featherGapPlan.Points[index].Action ==
                    LiveWallpaperJourneyAction.FeatherJump);
var featherGapPreviousPoint = featherGapPlan.Points[
    Math.Max(0, featherGapPointIndex - 1)];
var featherGapPoint = featherGapPlan.Points[featherGapPointIndex];
var featherGapNextPoint = featherGapPlan.Points[
    Math.Min(featherGapPlan.Points.Count - 1, featherGapPointIndex + 1)];
var featherGapSimulation = new LiveWallpaperLinkSimulation();
var featherGapPresses = 0;
var featherGapMinimumX = float.MaxValue;
var featherGapMaximumX = float.MinValue;
var featherGapStartTime = featherGapVariant * 20_000L;
for (var frame = 0; frame < 2_000; frame++)
{
    var link = featherGapSimulation.UpdateJourney(
        1, 0, featherGapStartTime + frame * 17L, true,
        featherGapMap, journeyViewport, allowIslandLife: true,
        followLoadingZones: true);
    if (link.Input.FeatherPressed)
        featherGapPresses++;
    featherGapMinimumX = Math.Min(featherGapMinimumX, link.MapX * 16f);
    featherGapMaximumX = Math.Max(featherGapMaximumX, link.MapX * 16f);
    if (featherGapMinimumX < 392f && featherGapMaximumX > 408f)
        break;
}
Assert(featherGapPresses == 1 && featherGapMinimumX < 392f &&
       featherGapMaximumX > 408f,
       $"Wallpaper Link must cross a one-tile pit with one gameplay-equivalent feather arc " +
       $"(presses={featherGapPresses}, minX={featherGapMinimumX}, maxX={featherGapMaximumX}, " +
       $"jump={featherGapPreviousPoint.PixelX},{featherGapPreviousPoint.PixelY}->" +
       $"{featherGapPoint.PixelX},{featherGapPoint.PixelY}->" +
       $"{featherGapNextPoint.PixelX},{featherGapNextPoint.PixelY}).");
var lowRateFeatherSimulation = new LiveWallpaperLinkSimulation();
var lowRateFeatherPresses = 0;
var lowRateFeatherFell = false;
var lowRateMinimumX = float.MaxValue;
var lowRateMaximumX = float.MinValue;
for (var frame = 0; frame < 600; frame++)
{
    var link = lowRateFeatherSimulation.UpdateJourney(
        1, 0, featherGapStartTime + frame * 67L, true,
        featherGapMap, journeyViewport, allowIslandLife: true,
        followLoadingZones: true);
    if (link.Input.FeatherPressed)
        lowRateFeatherPresses++;
    lowRateFeatherFell |= link.Action == LiveWallpaperLinkRouteAction.Falling;
    lowRateMinimumX = Math.Min(lowRateMinimumX, link.MapX * 16f);
    lowRateMaximumX = Math.Max(lowRateMaximumX, link.MapX * 16f);
    if (lowRateMinimumX < 392f && lowRateMaximumX > 408f)
        break;
}
Assert(lowRateFeatherPresses == 1 && !lowRateFeatherFell &&
       lowRateMinimumX < 392f && lowRateMaximumX > 408f,
       $"Battery-friendly 15 Hz rendering must preserve the same canonical feather arc " +
       $"instead of landing Link in the pit (presses={lowRateFeatherPresses}, " +
       $"fell={lowRateFeatherFell}, minX={lowRateMinimumX}, maxX={lowRateMaximumX}).");
var unsafeWideGapMapData = new System.Text.StringBuilder(
    "3\n0\n0\noverworld.png\n40\n90\n1\n");
for (var row = 0; row < 90; row++)
    unsafeWideGapMapData.AppendLine(string.Join(',', Enumerable.Repeat("0", 40)));
unsafeWideGapMapData.AppendLine("1");
unsafeWideGapMapData.AppendLine("fullHole");
unsafeWideGapMapData.AppendLine("8");
for (var gapRow = 72; gapRow < 80; gapRow++)
    unsafeWideGapMapData.AppendLine($"0;400;{gapRow * 16};64;16");
Assert(LiveWallpaperMap.TryLoad(
           new StringReader(unsafeWideGapMapData.ToString()),
           out var unsafeWideGapMap),
       "The overlong-feather regression fixture must be a valid installed map.");
var unsafeWideGapRouteFound = false;
for (var variant = 0; variant < 120 && !unsafeWideGapRouteFound; variant++)
{
    var candidate = LiveWallpaperJourneyPlanner.Create(
        unsafeWideGapMap, journeyViewport, 1, variant,
        allowIslandLife: true, followLoadingZones: true);
    unsafeWideGapRouteFound = candidate.Points.Any(point =>
                                  point.Action ==
                                  LiveWallpaperJourneyAction.FeatherJump) &&
                              candidate.Points.Min(point => point.PixelX) < 392f &&
                              candidate.Points.Max(point => point.PixelX) > 464f;
}
Assert(!unsafeWideGapRouteFound,
       "The planner must reject a gap wider than Link's real 31-update Pegasus running-jump arc.");
var pegasusGapMapData = new System.Text.StringBuilder(
    "3\n0\n0\noverworld.png\n40\n90\n1\n");
for (var row = 0; row < 90; row++)
    pegasusGapMapData.AppendLine(string.Join(',', Enumerable.Repeat("0", 40)));
pegasusGapMapData.AppendLine("1");
pegasusGapMapData.AppendLine("fullHole");
pegasusGapMapData.AppendLine("8");
for (var gapRow = 72; gapRow < 80; gapRow++)
    pegasusGapMapData.AppendLine($"0;400;{gapRow * 16};48;16");
Assert(LiveWallpaperMap.TryLoad(
           new StringReader(pegasusGapMapData.ToString()),
           out var pegasusGapMap),
       "The three-tile Pegasus gap fixture must be a valid installed map.");
LiveWallpaperJourneyPlan pegasusGapPlan = null;
var pegasusGapVariant = -1;
for (var variant = 0; variant < 120 && pegasusGapPlan == null; variant++)
{
    var candidate = LiveWallpaperJourneyPlanner.Create(
        pegasusGapMap, journeyViewport, 1, variant,
        allowIslandLife: true, followLoadingZones: true);
    if (candidate.Points.Any(point =>
            point.Action == LiveWallpaperJourneyAction.PegasusJump) &&
        candidate.Points.Any(point =>
            point.Action == LiveWallpaperJourneyAction.PegasusCharge) &&
        candidate.Points.Min(point => point.PixelX) < 392f &&
        candidate.Points.Max(point => point.PixelX) > 448f)
    {
        pegasusGapPlan = candidate;
        pegasusGapVariant = variant;
    }
}
Assert(pegasusGapPlan != null,
       "A three-tile pit must produce a Pegasus charge followed by one running feather jump.");
var pegasusGapSimulation = new LiveWallpaperLinkSimulation();
var sawPegasusGapCharge = false;
var sawPegasusGapJump = false;
var pegasusGapFell = false;
var pegasusGapMinimumX = float.MaxValue;
var pegasusGapMaximumX = float.MinValue;
var pegasusGapStartTime = pegasusGapVariant * 20_000L;
for (var frame = 0; frame < 5_000; frame++)
{
    var link = pegasusGapSimulation.UpdateJourney(
        1, 0, pegasusGapStartTime + frame * 8L, true,
        pegasusGapMap, journeyViewport, allowIslandLife: true,
        followLoadingZones: true);
    sawPegasusGapCharge |=
        link.Action == LiveWallpaperLinkRouteAction.PegasusCharge;
    sawPegasusGapJump |=
        link.Action == LiveWallpaperLinkRouteAction.PegasusJump;
    pegasusGapFell |= link.Action == LiveWallpaperLinkRouteAction.Falling;
    pegasusGapMinimumX = Math.Min(pegasusGapMinimumX, link.MapX * 16f);
    pegasusGapMaximumX = Math.Max(pegasusGapMaximumX, link.MapX * 16f);
    // At 120+ Hz, completing the jump requires Link's entire 8-pixel body to
    // clear the 48-pixel hole, not merely his entity anchor to cross its edge.
    if (sawPegasusGapJump && link.Height <= 0.001f &&
        pegasusGapMinimumX <= 392f && pegasusGapMaximumX >= 456f)
        break;
}
Assert(sawPegasusGapCharge && sawPegasusGapJump && !pegasusGapFell &&
       pegasusGapMinimumX <= 392f && pegasusGapMaximumX >= 456f,
       $"Link must retain canonical Pegasus momentum through a three-tile pit " +
       $"at a 120 Hz renderer cadence and land with his full body clear " +
       $"(charge={sawPegasusGapCharge}, jump={sawPegasusGapJump}, " +
       $"fell={pegasusGapFell}, minX={pegasusGapMinimumX}, maxX={pegasusGapMaximumX}).");
var holeFallSimulation = new LiveWallpaperLinkSimulation();
holeFallSimulation.EnterMap(384f, 1197f);
holeFallSimulation.Body.Position.Set(
    new Microsoft.Xna.Framework.Vector3(408f, 1197f, 0f));
holeFallSimulation.Body.IsGrounded = true;
var holeFallStart = holeFallSimulation.UpdateJourney(
    1, 0, 0L, true, featherGapMap, journeyViewport,
    allowIslandLife: true, followLoadingZones: true,
    holeFallAnimationMilliseconds: 850L);
var holeFallReset = holeFallSimulation.UpdateJourney(
    1, 0, 867L, true, featherGapMap, journeyViewport,
    allowIslandLife: true, followLoadingZones: true,
    holeFallAnimationMilliseconds: 850L);
Assert(holeFallStart.Action == LiveWallpaperLinkRouteAction.Falling &&
       holeFallReset.Action != LiveWallpaperLinkRouteAction.Falling &&
       holeFallReset.MapX * 16f < 400f &&
       !featherGapMap.IntersectsHole(
           holeFallReset.MapX * 16f - 4f,
           holeFallReset.MapY * 16f - 10f, 8f, 10f),
       $"A fully absorbed wallpaper Link must play the real fall state and reset to the field's saved safe position instead of remaining trapped in the hole " +
       $"(start={holeFallStart.Action}@{holeFallStart.MapX * 16f},{holeFallStart.MapY * 16f}; " +
       $"reset={holeFallReset.Action}@{holeFallReset.MapX * 16f},{holeFallReset.MapY * 16f}; " +
       $"coverage={featherGapMap.GetLinkHoleCoverage(404f, 1187f, 8f, 10f)}).");
var outdoorPitSimulation = new LiveWallpaperLinkSimulation();
outdoorPitSimulation.EnterMap(392f, 1197f);
var enteredOutdoorPit = constrainedMovement?.Invoke(
    outdoorPitSimulation,
    [featherGapMap, new Microsoft.Xna.Framework.Vector2(16f, 0f), false, true]);
var outdoorPitFall = outdoorPitSimulation.UpdateJourney(
    1, 0, 0L, true, featherGapMap, journeyViewport,
    allowIslandLife: true, followLoadingZones: true,
    holeFallAnimationMilliseconds: 850L);
Assert(enteredOutdoorPit is true &&
       outdoorPitFall.Action == LiveWallpaperLinkRouteAction.Falling,
       "Outdoor bottomless holes must be non-solid hazards that start ObjLink's fall after grounded movement enters them.");
var indoorPitMapData = new System.Text.StringBuilder(
    "3\n0\n0\nhouse.png\n40\n90\n1\n");
for (var row = 0; row < 90; row++)
    indoorPitMapData.AppendLine(string.Join(',', Enumerable.Repeat("0", 40)));
indoorPitMapData.Append(
    "2\nhouseObject\nvisiblehole\n2\n0;0;0\n1;400;1184\n");
Assert(LiveWallpaperMap.TryLoad(
           new StringReader(indoorPitMapData.ToString()), out var indoorPitMap) &&
       indoorPitMap.IsHouse && indoorPitMap.IntersectsHole(
           404f, 1187f, 8f, 10f),
       "Installed indoor visiblehole objects must retain ObjHole collision.");
var indoorPitSimulation = new LiveWallpaperLinkSimulation();
indoorPitSimulation.EnterMap(392f, 1197f);
var enteredIndoorPit = constrainedMovement?.Invoke(
    indoorPitSimulation,
    [indoorPitMap, new Microsoft.Xna.Framework.Vector2(16f, 0f), false, true]);
var indoorPitFall = indoorPitSimulation.UpdateJourney(
    1, 0, 0L, true, indoorPitMap, journeyViewport,
    allowIslandLife: true, followLoadingZones: false,
    holeFallAnimationMilliseconds: 850L);
Assert(enteredIndoorPit is true &&
       indoorPitFall.Action == LiveWallpaperLinkRouteAction.Falling,
       "Indoor bottomless holes must use the same non-solid fall and reset path as overworld holes.");
var tappedAcrossHolePlan = LiveWallpaperJourneyPlanner.CreateToPoint(
    featherGapMap, journeyViewport,
    384f, 1197f, 424f, 1197f);
Assert(tappedAcrossHolePlan.Points.Count >= 2 &&
       tappedAcrossHolePlan.Points.Any(point =>
           featherGapMap.IntersectsHole(
               point.PixelX - 4f, point.PixelY - 10f, 8f, 10f)) &&
       tappedAcrossHolePlan.Points.Any(point => point.Action ==
           LiveWallpaperJourneyAction.FeatherJump),
       "A safe manual destination beyond a one-tile pit must use the canonical feather jump instead of walking Link into the hole.");
var tappedHoleSimulation = new LiveWallpaperLinkSimulation();
tappedHoleSimulation.UpdateJourney(
    1, 0, 0L, true, featherGapMap, journeyViewport,
    allowIslandLife: true, followLoadingZones: false,
    holeFallAnimationMilliseconds: 850L);
tappedHoleSimulation.EnterMap(384f, 1197f);
Assert(tappedHoleSimulation.TryWalkTo(
           featherGapMap, journeyViewport, 408f, 1197f),
       "A tap whose destination is inside a hole must be accepted as a manual journey.");
var tappedHoleFell = false;
for (var frame = 1; frame <= 120 && !tappedHoleFell; frame++)
{
    var link = tappedHoleSimulation.UpdateJourney(
        1, 0, frame * 17L, true, featherGapMap, journeyViewport,
        allowIslandLife: true, followLoadingZones: false,
        holeFallAnimationMilliseconds: 850L);
    tappedHoleFell = link.Action == LiveWallpaperLinkRouteAction.Falling;
}
Assert(tappedHoleFell,
       "Tapping a bottomless hole must make Link walk into it and enter the canonical fall sequence.");
var tappedPegasusPlan = LiveWallpaperJourneyPlanner.CreateToPoint(
    pegasusGapMap, journeyViewport,
    384f, 1197f, 456f, 1197f);
Assert(tappedPegasusPlan.Points.Any(point => point.Action ==
           LiveWallpaperJourneyAction.PegasusCharge) &&
       tappedPegasusPlan.Points.Any(point => point.Action ==
           LiveWallpaperJourneyAction.PegasusJump),
       "A safe tap across a clear three-tile pit must schedule a real Pegasus charge and running feather jump.");
var tappedPegasusSimulation = new LiveWallpaperLinkSimulation();
tappedPegasusSimulation.UpdateJourney(
    1, 0, 0L, true, pegasusGapMap, journeyViewport,
    allowIslandLife: true, followLoadingZones: false,
    holeFallAnimationMilliseconds: 850L);
tappedPegasusSimulation.EnterMap(384f, 1197f);
Assert(tappedPegasusSimulation.TryWalkTo(
           pegasusGapMap, journeyViewport, 456f, 1197f),
       "The wallpaper must accept a safe manual destination beyond a clear three-tile pit.");
var tappedPegasusJumped = false;
var tappedPegasusFell = false;
var tappedPegasusReachedFarSide = false;
for (var frame = 1; frame <= 500 && !tappedPegasusReachedFarSide; frame++)
{
    var link = tappedPegasusSimulation.UpdateJourney(
        1, 0, frame * 17L, true, pegasusGapMap, journeyViewport,
        allowIslandLife: true, followLoadingZones: false,
        holeFallAnimationMilliseconds: 850L);
    tappedPegasusJumped |= link.Action ==
                           LiveWallpaperLinkRouteAction.PegasusJump;
    tappedPegasusFell |= link.Action == LiveWallpaperLinkRouteAction.Falling;
    tappedPegasusReachedFarSide = link.MapX * 16f >= 455f;
}
Assert(tappedPegasusJumped && !tappedPegasusFell &&
       tappedPegasusReachedFarSide,
       "A tapped three-tile Pegasus jump must retain enough canonical momentum to land fully on the clear far side.");
var blockedPegasusMapData = new System.Text.StringBuilder(
    "3\n0\n0\noverworld.png\n40\n90\n1\n");
for (var row = 0; row < 90; row++)
    blockedPegasusMapData.AppendLine(
        string.Join(',', Enumerable.Repeat("0", 40)));
blockedPegasusMapData.AppendLine("2");
blockedPegasusMapData.AppendLine("fullHole");
blockedPegasusMapData.AppendLine("c1");
blockedPegasusMapData.AppendLine("9");
for (var gapRow = 72; gapRow < 80; gapRow++)
    blockedPegasusMapData.AppendLine($"0;400;{gapRow * 16};48;16");
blockedPegasusMapData.AppendLine("1;448;1184");
Assert(LiveWallpaperMap.TryLoad(
           new StringReader(blockedPegasusMapData.ToString()),
           out var blockedPegasusMap),
       "The obstructed Pegasus landing fixture must be a valid installed map.");
var jumpValidator = typeof(LiveWallpaperJourneyPlanner).GetMethod(
    "HasValidJumpSpans",
    System.Reflection.BindingFlags.NonPublic |
    System.Reflection.BindingFlags.Static);
var blockedPegasusPath = Enumerable.Range(0, 9)
    .Select(index => new Microsoft.Xna.Framework.Point(
        392 + index * 8, 1200))
    .ToList();
Assert(jumpValidator?.Invoke(
           null, [blockedPegasusMap, blockedPegasusPath]) is false,
       "Pegasus jump validation must reject a three-tile span whose installed far-side landing body is obstructed.");
var biasedHoleCoverage = featherGapMap.GetLinkHoleCoverage(
    408f - 4f, 1200f - 10f, 8f, 10f);
var biasedHoleSimulation = new LiveWallpaperLinkSimulation();
biasedHoleSimulation.EnterMap(384f, 1200f);
biasedHoleSimulation.Body.Position.Set(
    new Microsoft.Xna.Framework.Vector3(408f, 1200f, 0f));
biasedHoleSimulation.Body.IsGrounded = true;
var biasedHoleStartedFalling = false;
for (var frame = 0; frame < 100 && !biasedHoleStartedFalling; frame++)
{
    var link = biasedHoleSimulation.UpdateJourney(
        1, 0, frame * 17L, true, featherGapMap, journeyViewport,
        allowIslandLife: true, followLoadingZones: true,
        holeFallAnimationMilliseconds: 850L);
    biasedHoleStartedFalling = link.Action ==
                               LiveWallpaperLinkRouteAction.Falling;
}
Assert(biasedHoleCoverage > biasedHoleSimulation.Body.AbsorbStop &&
       biasedHoleCoverage < biasedHoleSimulation.Body.AbsorbPercentage &&
       biasedHoleStartedFalling,
       "Link caught by the canonical bottom-edge hole bias must advance through the stuck timeout into ObjLink's fall/reset sequence.");
var hookshotGapMapData = new System.Text.StringBuilder(
    "3\n0\n0\noverworld.png\n40\n90\n1\n");
for (var row = 0; row < 90; row++)
    hookshotGapMapData.AppendLine(string.Join(',', Enumerable.Repeat("0", 40)));
hookshotGapMapData.AppendLine("2");
hookshotGapMapData.AppendLine("fullHole");
hookshotGapMapData.AppendLine("overworldDonut");
hookshotGapMapData.AppendLine("25");
for (var gapRow = 72; gapRow < 80; gapRow++)
for (var gapColumn = 25; gapColumn < 28; gapColumn++)
    hookshotGapMapData.AppendLine($"0;{gapColumn * 16};{gapRow * 16}");
hookshotGapMapData.AppendLine("1;464;1200");
Assert(LiveWallpaperMap.TryLoad(
           new StringReader(hookshotGapMapData.ToString()), out var hookshotGapMap) &&
       hookshotGapMap.HookshotTargets.Count == 1,
       "The Hookshot journey fixture must retain the exact installed grip rectangle.");
LiveWallpaperJourneyPlan hookshotGapPlan = null;
var hookshotGapVariant = -1;
for (var variant = 0; variant < 120 && hookshotGapPlan == null; variant++)
{
    var candidate = LiveWallpaperJourneyPlanner.Create(
        hookshotGapMap, journeyViewport, 1, variant,
        allowIslandLife: true, followLoadingZones: true);
    if (candidate.Points.Any(point =>
            point.Action == LiveWallpaperJourneyAction.Hookshot))
    {
        hookshotGapPlan = candidate;
        hookshotGapVariant = variant;
    }
}
Assert(hookshotGapPlan != null,
       "A gap wider than one feather jump must use a real installed Hookshot grip when its 120-pixel corridor is clear.");
var hookshotGapSimulation = new LiveWallpaperLinkSimulation();
var sawHookshot = false;
var sawHookshotChain = false;
var hookshotMinimumX = float.MaxValue;
var hookshotMaximumX = float.MinValue;
var hookshotStartTime = hookshotGapVariant * 20_000L;
for (var frame = 0; frame < 2_500; frame++)
{
    var link = hookshotGapSimulation.UpdateJourney(
        1, 0, hookshotStartTime + frame * 17L, true,
        hookshotGapMap, journeyViewport, allowIslandLife: true,
        followLoadingZones: true);
    sawHookshot |= link.Action == LiveWallpaperLinkRouteAction.Hookshot;
    sawHookshotChain |= link.HookshotVisible &&
                        MathF.Abs(link.HookshotMapX - link.MapX) > 0.25f;
    hookshotMinimumX = Math.Min(hookshotMinimumX, link.MapX * 16f);
    hookshotMaximumX = Math.Max(hookshotMaximumX, link.MapX * 16f);
    if (sawHookshotChain && hookshotMinimumX < 400f &&
        hookshotMaximumX > 448f)
        break;
}
Assert(sawHookshot && sawHookshotChain && hookshotMinimumX < 400f &&
       hookshotMaximumX > 448f,
       $"Hookshot simulation must extend the real chain and pull Link across the validated gap " +
       $"(minX={hookshotMinimumX}, maxX={hookshotMaximumX}).");

var pegasusPlan = LiveWallpaperJourneyPlanner.Create(
    journeyMap, journeyViewport, 1, 0,
    allowIslandLife: false, followLoadingZones: true);
var pegasusVariant = 0;
if (!pegasusPlan.Points.Any(point =>
        point.Action == LiveWallpaperJourneyAction.PegasusDash))
{
    for (var variant = 1; variant < 120; variant++)
    {
        var candidate = LiveWallpaperJourneyPlanner.Create(
            journeyMap, journeyViewport, 1, variant,
            allowIslandLife: false, followLoadingZones: true);
        if (!candidate.Points.Any(point =>
                point.Action == LiveWallpaperJourneyAction.PegasusDash))
            continue;
        pegasusPlan = candidate;
        pegasusVariant = variant;
        break;
    }
}
Assert(pegasusPlan.Points.Any(point =>
           point.Action == LiveWallpaperJourneyAction.PegasusCharge) &&
       pegasusPlan.Points.Any(point =>
           point.Action == LiveWallpaperJourneyAction.PegasusDash),
       "Long straight routes must schedule the real Pegasus charge before the dash.");
var pegasusSimulation = new LiveWallpaperLinkSimulation();
var pegasusStartTime = pegasusVariant * 20_000L;
var firstPegasusChargeAt = -1L;
var lastPegasusChargeAt = -1L;
var maximumPegasusStep = 0f;
LiveWallpaperSimulatedLinkState? previousPegasusLink = null;
var sawPegasusDash = false;
for (var frame = 0; frame < 2_500 && !sawPegasusDash; frame++)
{
    var elapsed = pegasusStartTime + frame * 17L;
    var link = pegasusSimulation.UpdateJourney(
        1, 0, elapsed, true, journeyMap, journeyViewport,
        allowIslandLife: false, followLoadingZones: true);
    if (link.Action == LiveWallpaperLinkRouteAction.PegasusCharge)
    {
        if (firstPegasusChargeAt < 0)
            firstPegasusChargeAt = elapsed;
        lastPegasusChargeAt = elapsed;
    }
    if (previousPegasusLink.HasValue &&
        link.Action == LiveWallpaperLinkRouteAction.PegasusDash)
    {
        var deltaX = (link.MapX - previousPegasusLink.Value.MapX) * 16f;
        var deltaY = (link.MapY - previousPegasusLink.Value.MapY) * 16f;
        maximumPegasusStep = Math.Max(
            maximumPegasusStep,
            MathF.Sqrt(deltaX * deltaX + deltaY * deltaY));
        sawPegasusDash = true;
    }
    previousPegasusLink = link;
}
Assert(firstPegasusChargeAt >= 0 &&
       lastPegasusChargeAt - firstPegasusChargeAt >= 500L &&
       sawPegasusDash && maximumPegasusStep is >= 1.9f and <= 2.1f,
       $"Pegasus Boots must charge for 533 ms and move at ObjLink's exact two pixels per frame " +
       $"(charge={lastPegasusChargeAt - firstPegasusChargeAt} ms, step={maximumPegasusStep}).");

var coverageMapData = new System.Text.StringBuilder(
    "3\n0\n0\noverworld.png\n30\n8\n1\n");
for (var row = 0; row < 8; row++)
    coverageMapData.AppendLine(string.Join(',', Enumerable.Repeat("0", 30)));
coverageMapData.AppendLine("0");
coverageMapData.AppendLine("0");
Assert(LiveWallpaperMap.TryLoad(
           new StringReader(coverageMapData.ToString()), out var coverageMap),
       "The multi-field coverage fixture must load.");
var reachableCoverageFields =
    LiveWallpaperJourneyPlanner.GetReachableOverworldFieldKeys(
        coverageMap, 80f, 64f);
var visitedCoverageFields = new HashSet<int>
{
    LiveWallpaperJourneyPlanner.GetOverworldFieldKey(80f, 64f),
    LiveWallpaperJourneyPlanner.GetOverworldFieldKey(240f, 64f)
};
Assert(reachableCoverageFields.Count == 3 &&
       LiveWallpaperJourneyPlanner.TryGetNextCoverageFieldKey(
           coverageMap, 80f, 64f, visitedCoverageFields,
           out var nextCoverageField) &&
       nextCoverageField ==
           LiveWallpaperJourneyPlanner.GetOverworldFieldKey(240f, 64f),
       "Coverage routing must backtrack through a visited field toward the nearest unseen reachable field instead of looping among recent screens.");
var enclosedFieldData = new System.Text.StringBuilder(
    "3\n0\n0\noverworld.png\n10\n8\n1\n");
for (var row = 0; row < 8; row++)
    enclosedFieldData.AppendLine(string.Join(',', Enumerable.Repeat("0", 10)));
var enclosedWalls = new List<(int X, int Y)>();
for (var x = 0; x < 10; x++)
{
    enclosedWalls.Add((x * 16, 0));
    enclosedWalls.Add((x * 16, 7 * 16));
}
for (var y = 1; y < 7; y++)
{
    enclosedWalls.Add((0, y * 16));
    enclosedWalls.Add((9 * 16, y * 16));
}
enclosedFieldData.AppendLine("1");
enclosedFieldData.AppendLine("c1");
enclosedFieldData.AppendLine(enclosedWalls.Count.ToString());
foreach (var wall in enclosedWalls)
    enclosedFieldData.AppendLine($"0;{wall.X};{wall.Y}");
Assert(LiveWallpaperMap.TryLoad(
           new StringReader(enclosedFieldData.ToString()), out var enclosedFieldMap),
       "The enclosed-field fallback fixture must be a valid installed map.");
var enclosedFallback = LiveWallpaperJourneyPlanner.Create(
    enclosedFieldMap, journeyViewport, 1, 0,
    allowIslandLife: true,
    continuationPixelX: 80,
    continuationPixelY: 64,
    followLoadingZones: true);
Assert(enclosedFallback.Points.Count > 1,
       "A field with no valid loading-zone edge must still produce a real collision-safe local route instead of leaving Link stuck.");
Assert(LiveWallpaperMapViewport.TryCreate(
           2400, 1080, journeyMap.Height, 1, 0.5f, out var landscapeJourneyViewport) &&
       Math.Abs(landscapeJourneyViewport.TileSize - journeyViewport.TileSize) < 0.001f &&
       landscapeJourneyViewport.Columns > journeyViewport.Columns &&
       landscapeJourneyViewport.Columns * landscapeJourneyViewport.TileSize >= 2400,
       "Rotating the wallpaper must preserve map zoom and reveal more columns in landscape.");
LiveWallpaperJourneyPlan interactionJourney = null;
LiveWallpaperJourneyPlan roosterJourney = null;
LiveWallpaperJourneyPlan combatJourney = null;
var interactionJourneyVariant = -1;
var roosterJourneyVariant = -1;
var combatJourneyVariant = -1;
var combatJourneyCount = 0;
var sawLoadingZoneExit = false;
var journeyMinimumX = journeyViewport.OriginX * 16 + 8;
var journeyMinimumY = journeyViewport.OriginY * 16 + 8;
var journeyMaximumX =
    (journeyViewport.OriginX + journeyViewport.Columns) * 16 - 8;
var journeyMaximumY =
    (journeyViewport.OriginY + journeyViewport.Rows) * 16 - 8;
var tappedLinkSimulation = new LiveWallpaperLinkSimulation();
var tappedLinkStart = tappedLinkSimulation.UpdateJourney(
    1, 0, 0, true, journeyMap, journeyViewport,
    allowIslandLife: true, followLoadingZones: false);
LiveWallpaperJourneyPlan tappedPlan = null;
foreach (var offset in new (int X, int Y)[]
         {
             (80, 0), (-80, 0), (0, 80), (0, -80), (64, 64), (-64, -64)
         })
{
    var requestedX = Math.Clamp(
        tappedLinkStart.MapX * 16f + offset.X,
        journeyMinimumX, journeyMaximumX);
    var requestedY = Math.Clamp(
        tappedLinkStart.MapY * 16f + offset.Y,
        journeyMinimumY, journeyMaximumY);
    var candidate = LiveWallpaperJourneyPlanner.CreateToPoint(
        journeyMap, journeyViewport,
        tappedLinkStart.MapX * 16f, tappedLinkStart.MapY * 16f,
        requestedX, requestedY);
    if (candidate.Points.Count < 2)
        continue;
    var candidateEnd = candidate.Points[^1];
    var candidateDeltaX = candidateEnd.PixelX - tappedLinkStart.MapX * 16f;
    var candidateDeltaY = candidateEnd.PixelY - tappedLinkStart.MapY * 16f;
    if (candidateDeltaX * candidateDeltaX + candidateDeltaY * candidateDeltaY <= 16f * 16f)
        continue;
    tappedPlan = candidate;
    break;
}
Assert(tappedPlan != null,
       "A reachable wallpaper tap must produce a collision-aware path from Link's live position.");
var tappedTarget = tappedPlan.Points[^1];
Assert(tappedLinkSimulation.TryWalkTo(
           journeyMap, journeyViewport,
           tappedTarget.PixelX, tappedTarget.PixelY),
       "The wallpaper simulation must accept a reachable tapped destination.");
var tappedStartDistance = MathF.Sqrt(
    MathF.Pow(tappedLinkStart.MapX * 16f - tappedTarget.PixelX, 2f) +
    MathF.Pow(tappedLinkStart.MapY * 16f - tappedTarget.PixelY, 2f));
var tappedNearestDistance = tappedStartDistance;
for (var frame = 1; frame <= 1_200; frame++)
{
    var tappedLink = tappedLinkSimulation.UpdateJourney(
        1, 0, frame * 17L, true, journeyMap, journeyViewport,
        allowIslandLife: true, followLoadingZones: false);
    var tappedDistance = MathF.Sqrt(
        MathF.Pow(tappedLink.MapX * 16f - tappedTarget.PixelX, 2f) +
        MathF.Pow(tappedLink.MapY * 16f - tappedTarget.PixelY, 2f));
    tappedNearestDistance = Math.Min(tappedNearestDistance, tappedDistance);
}
Assert(tappedNearestDistance <= 4f &&
       tappedNearestDistance < tappedStartDistance - 16f,
       "Tapping the wallpaper must make Link walk to the selected reachable map point.");
const int followedFieldMinimumX = 20 * 16 + 8;
const int followedFieldMinimumY = 72 * 16 + 8;
const int followedFieldMaximumX = 20 * 16 + 160 - 8;
const int followedFieldMaximumY = 72 * 16 + 128 - 8;
var followedEdgeExit = false;
var followedDoorExit = false;
for (var variant = 0; variant < 30; variant++)
{
    var followedCandidate = LiveWallpaperJourneyPlanner.Create(
        journeyMap, journeyViewport, 1, variant, allowIslandLife: true,
        followLoadingZones: true);
    if (followedCandidate.Points.Count == 0)
        continue;
    var last = followedCandidate.Points[^1];
    var isEdge = last.PixelX < followedFieldMinimumX ||
                 last.PixelX > followedFieldMaximumX ||
                 last.PixelY < followedFieldMinimumY ||
                 last.PixelY > followedFieldMaximumY;
    followedEdgeExit |= isEdge;
    followedDoorExit |= !isEdge &&
                        Math.Abs(last.PixelX - journeyMap.Portals[0].LinkTargetX) <= 1f &&
                        Math.Abs(last.PixelY - journeyMap.Portals[0].LinkTargetY) <= 1f;
}
Assert(followedEdgeExit && followedDoorExit,
       "Follow mode must cover overworld field edges and periodically select canonical interior doors.");

var installedOverworldPath = Path.Combine(
    wallpaperGameDataRoot, "Maps", "overworld.map");
if (testInstalledWallpaperAssets)
using (var installedOverworldReader = File.OpenText(installedOverworldPath))
{
    Assert(LiveWallpaperMap.TryLoad(installedOverworldReader, out var installedOverworld),
           "The real installed overworld fixture must load for wallpaper transition coverage.");
    var transparentOceanWave = installedOverworld.AnimatedTiles.First(tile =>
        tile.RequiresOverworldOceanBase &&
        tile.EntityX >= 0 && tile.EntityY >= 0 &&
        !installedOverworld.HasDrawableTile(
            tile.EntityX / 16, tile.EntityY / 16));
    Assert(installedOverworld.NeedsOverworldOceanBase(transparentOceanWave) &&
           LiveWallpaperMap.OverworldOceanTileIndex == 235,
           "Transparent outer-island waves must restore tileset0's exact solid ocean tile instead of exposing Android's black canvas.");
    const float overworldAuditStartX = 376f;
    const float overworldAuditStartY = 1240f;
    var overworldReachableFields =
        LiveWallpaperJourneyPlanner.GetReachableOverworldFieldKeys(
            installedOverworld, overworldAuditStartX, overworldAuditStartY);
    var overworldCoverageFields = new HashSet<int>
    {
        LiveWallpaperJourneyPlanner.GetOverworldFieldKey(
            overworldAuditStartX, overworldAuditStartY)
    };
    var overworldCoverageCurrentX = overworldAuditStartX;
    var overworldCoverageCurrentY = overworldAuditStartY;
    var overworldCoverageSteps = 0;
    var overworldCoverageLimit = Math.Max(
        1, overworldReachableFields.Count * overworldReachableFields.Count);
    while (overworldCoverageFields.Count < overworldReachableFields.Count &&
           overworldCoverageSteps++ < overworldCoverageLimit &&
           LiveWallpaperJourneyPlanner.TryGetNextCoverageFieldKey(
               installedOverworld,
               overworldCoverageCurrentX, overworldCoverageCurrentY,
               overworldCoverageFields, out var auditNextField))
    {
        overworldCoverageFields.Add(auditNextField);
        var auditFieldX = auditNextField & 0xffff;
        var auditFieldY = auditNextField >> 16;
        overworldCoverageCurrentX = Math.Min(
            installedOverworld.Width * 16f - 8f,
            auditFieldX * 160f + 80f);
        overworldCoverageCurrentY = Math.Min(
            installedOverworld.Height * 16f - 8f,
            auditFieldY * 128f + 64f);
    }
    Assert(overworldReachableFields.Count >= 60 &&
           overworldCoverageFields.SetEquals(overworldReachableFields) &&
           overworldCoverageSteps <= overworldCoverageLimit,
           $"Every collision-reachable overworld field must be discoverable without a reciprocal-edge loop or coverage softlock " +
           $"(reachable={overworldReachableFields.Count}, visited={overworldCoverageFields.Count}, steps={overworldCoverageSteps}).");
    var dreamShrineStones = installedOverworld.Decorations
        .Where(decoration => decoration.StoneLayout &&
                             decoration.EntityX is >= 520 and <= 552 &&
                             decoration.EntityY is >= 1101 and <= 1117)
        .ToArray();
    var stoneDrawOffset = GameObjectVisualLayout.GetStoneSpriteOffset(15, 15);
    Assert(dreamShrineStones.Length == 3 &&
           dreamShrineStones.Any(stone => stone.EntityX == 520 && stone.EntityY == 1101) &&
           dreamShrineStones.Any(stone => stone.EntityX == 536 && stone.EntityY == 1117) &&
           dreamShrineStones.Any(stone => stone.EntityX == 552 && stone.EntityY == 1101) &&
           Math.Abs(stoneDrawOffset.X + 7f) < 0.001f &&
           Math.Abs(stoneDrawOffset.Y + 12f) < 0.001f,
           "Dream Shrine stones must use ObjStone's canonical entity and sprite transforms.");
    var houseOneEntrance = installedOverworld.Portals.Single(portal =>
        portal.NextMap == "house1.map" && portal.ExitId == "h1");
    var installedHouseOnePath = Path.Combine(
        wallpaperGameDataRoot, "Maps", houseOneEntrance.NextMap);
    using (var installedHouseOneReader = File.OpenText(installedHouseOnePath))
    {
        Assert(LiveWallpaperMap.TryLoad(
                   installedHouseOneReader, out var installedHouseOne),
               "The real house-one map must load for wallpaper door coverage.");
        var matchingHouseEntry = installedHouseOne.Portals.Single(portal =>
            portal.EntryId == houseOneEntrance.ExitId);
        Assert(LiveWallpaperMapViewport.TryCreateCentered(
                   1080, 2400, installedHouseOne.Width,
                   installedHouseOne.Height,
                   80f, 120f, 0.5f, out var houseOneViewport),
               "A real house entry must produce an interior viewport.");
        var houseOneEntryScreenX = houseOneViewport.Left +
            (80f / 16f - houseOneViewport.OriginX) *
            houseOneViewport.TileSize;
        var houseOneEntryScreenY = houseOneViewport.Top +
            (120f / 16f - houseOneViewport.OriginY) *
            houseOneViewport.TileSize;
        Assert(installedHouseOne.IsHouse &&
               installedHouseOne.Lights.Count == 1 &&
               installedHouseOne.Lights[0].CenterX == 80 &&
               installedHouseOne.Lights[0].CenterY == 120 &&
               matchingHouseEntry.NextMap == "overworld.map" &&
               matchingHouseEntry.ExitId == "h1" &&
               Math.Abs(matchingHouseEntry.GetLinkSpawnX(
                            installedHouseOne.Is2DMap) - 80f) < 0.001f &&
               Math.Abs(matchingHouseEntry.GetLinkSpawnY(
                            installedHouseOne.Is2DMap) - 120f) < 0.001f &&
               Math.Abs(houseOneEntryScreenX - 540f) < 0.001f &&
               Math.Abs(houseOneEntryScreenY - 1200f) < 0.001f,
               "A real overworld door must resolve its matching interior entry, exact ObjDoor spawn, and centered camera.");
        var houseExitPlan = LiveWallpaperJourneyPlanner.CreateToPoint(
            installedHouseOne, houseOneViewport,
            80f, 120f,
            matchingHouseEntry.LinkTargetX,
            matchingHouseEntry.LinkTargetY);
        Assert(houseExitPlan.Points.Count >= 2 &&
               Math.Abs(houseExitPlan.Points[^1].PixelX -
                        matchingHouseEntry.LinkTargetX) < 0.001f &&
               Math.Abs(houseExitPlan.Points[^1].PixelY -
                        matchingHouseEntry.LinkTargetY) < 0.001f,
               "The canonical house exit on the bottom map boundary must remain reachable from its entry spawn.");
        var immediateReturnVariants = Enumerable.Range(0, 60)
            .Where(variant =>
            {
                var plan = LiveWallpaperJourneyPlanner.Create(
                    installedHouseOne, houseOneViewport, 1, variant,
                    allowIslandLife: true,
                    continuationPixelX: 80f,
                    continuationPixelY: 120f);
                if (plan.Points.Count == 0)
                    return false;
                var last = plan.Points[^1];
                return Math.Abs(last.PixelX -
                                matchingHouseEntry.LinkTargetX) <= 1f &&
                       Math.Abs(last.PixelY -
                                matchingHouseEntry.LinkTargetY) <= 1f;
            })
            .ToArray();
        Assert(immediateReturnVariants.Length > 0 &&
               immediateReturnVariants.All(variant =>
               {
                   var plan = LiveWallpaperJourneyPlanner.Create(
                       installedHouseOne, houseOneViewport, 1, variant,
                       allowIslandLife: true,
                       continuationPixelX: 80f,
                       continuationPixelY: 120f,
                       excludedPortalEntryId: matchingHouseEntry.EntryId);
                   if (plan.Points.Count == 0)
                       return true;
                   var last = plan.Points[^1];
                   return Math.Abs(last.PixelX -
                                   matchingHouseEntry.LinkTargetX) > 1f ||
                          Math.Abs(last.PixelY -
                                   matchingHouseEntry.LinkTargetY) > 1f;
               }),
               "A newly entered interior must not immediately route Link back through its reciprocal door.");
        var houseOnePots = installedHouseOne.Objects
            .Where(mapObject => mapObject.Template == "pot2")
            .ToArray();
        var houseOnePotSprites = installedHouseOne.Decorations
            .Where(decoration => decoration.SpriteId == "pot_1" &&
                                 decoration.StoneLayout)
            .ToArray();
        Assert(houseOnePots.Length == 2 && houseOnePotSprites.Length == 2 &&
               houseOnePots.All(pot => houseOnePotSprites.Any(sprite =>
                   sprite.EntityX == pot.PixelX + 8 &&
                   sprite.EntityY == pot.PixelY + 13)) &&
               houseOnePots.All(pot => installedHouseOne.TryGetStoneKey(
                   pot.PixelX, pot.PixelY + 1, 16, 13, out _)),
               "Installed house pots must render at ObjStone's canonical entity position and remain liftable stone collisions.");
    }
    var installedShopOnePath = Path.Combine(
        wallpaperGameDataRoot, "Maps", "shop1.map");
    using (var installedShopOneReader = File.OpenText(installedShopOnePath))
    {
        Assert(LiveWallpaperMap.TryLoad(
                   installedShopOneReader, out var installedShopOne),
               "The real Mabe shop map must load for merchandise collision coverage.");
        var itemPedestal = installedShopOne.Objects.Single(mapObject =>
            mapObject.Template == "itemShop");
        var itemPedestalSprite = installedShopOne.Decorations.Single(decoration =>
            decoration.SpriteId == "itemShop");
        var shopkeeper = installedShopOne.Objects.Single(mapObject =>
            mapObject.Template == "shopkeeper");
        Assert(itemPedestal.PixelX == 96 && itemPedestal.PixelY == 80 &&
               itemPedestalSprite.EntityX == 104 &&
               itemPedestalSprite.EntityY == 94 &&
               installedShopOne.IntersectsCollision(
                   96, 84, 16, 12, includeHoles: true) &&
               shopkeeper.PixelX == 112 && shopkeeper.PixelY == 80 &&
               installedShopOne.IntersectsCollision(
                   113, 86, 14, 10, includeHoles: true),
               "The shop pedestal and shopkeeper must retain their canonical normal collision bodies.");
    }
    var installedHouseElevenPath = Path.Combine(
        wallpaperGameDataRoot, "Maps", "house11.map");
    using (var installedHouseElevenReader = File.OpenText(installedHouseElevenPath))
    {
        Assert(LiveWallpaperMap.TryLoad(
                   installedHouseElevenReader, out var installedHouseEleven),
               "The real painting-house map must load for furnishing coverage.");
        var painting = installedHouseEleven.Decorations.Single(decoration =>
            decoration.SpriteId == "painting");
        var installedHippo = installedHouseEleven.Actors.Single(actor =>
            actor.Kind == LiveWallpaperMapActorKind.Hippo);
        var installedPainter = installedHouseEleven.Actors.Single(actor =>
            actor.Kind == LiveWallpaperMapActorKind.Painter);
        Assert(painting.EntityX == 104 && painting.EntityY == 71 &&
               installedHouseEleven.IntersectsCollision(
                   96, 59, 16, 8, includeHoles: true) &&
               installedHippo.BodyX == 31 && installedHippo.BodyY == 31 &&
               installedHippo.BodyWidth == 18 && installedHippo.BodyHeight == 12 &&
               installedPainter.BodyX == 79 && installedPainter.BodyY == 59 &&
               installedPainter.BodyWidth == 18 && installedPainter.BodyHeight == 12,
               "The painting house must retain ObjPainting plus the hippo and painter's canonical bodies.");
    }
    var installedHouseFourPath = Path.Combine(
        wallpaperGameDataRoot, "Maps", "house4.map");
    using (var installedHouseFourReader = File.OpenText(installedHouseFourPath))
    {
        Assert(LiveWallpaperMap.TryLoad(
                   installedHouseFourReader, out var installedHouseFour),
               "The two-screen house fixture must load for interior camera coverage.");
        var installedLegacyPerson = installedHouseFour.Actors.Single(actor =>
            actor.Kind == LiveWallpaperMapActorKind.LegacyPerson);
        var installedWallLamp = installedHouseFour.Lamps.Single();
        Assert(installedLegacyPerson.AnimationId == "npc07" &&
               installedLegacyPerson.AnimationName == "stand" &&
               installedLegacyPerson.BodyX == 192 &&
               installedLegacyPerson.BodyY == 54 &&
               installedLegacyPerson.BodyWidth == 16 &&
               installedLegacyPerson.BodyHeight == 10 &&
               installedWallLamp.AnimationPath == "Objects/lamp_wall_1.ani" &&
               installedWallLamp.PixelX == 48 &&
               installedWallLamp.PixelY == 16 &&
               installedWallLamp.Rotation == 0 &&
               !installedWallLamp.PlayerLayer,
               "The real two-screen house must retain ObjPerson plus its exact wall-lamp asset, anchor, rotation, and layer.");
        var houseFourEntry = installedHouseFour.Portals.Single(portal =>
            portal.EntryId == "h4-1");
        var houseFourSpawnX = houseFourEntry.GetLinkSpawnX(
            installedHouseFour.Is2DMap);
        var houseFourSpawnY = houseFourEntry.GetLinkSpawnY(
            installedHouseFour.Is2DMap);
        Assert(LiveWallpaperMapViewport.TryCreateCentered(
                   1080, 2400, installedHouseFour.Width,
                   installedHouseFour.Height,
                   houseFourSpawnX, houseFourSpawnY, 0.5f,
                   out var houseFourViewport),
               "The two-screen house must produce an interior viewport.");
        var houseFourSimulation = new LiveWallpaperLinkSimulation();
        houseFourSimulation.EnterMap(houseFourSpawnX, houseFourSpawnY);
        var houseFourBeforeScroll = houseFourSimulation.UpdateJourney(
            1, 0, 0L, true, installedHouseFour, houseFourViewport,
            allowIslandLife: true, followLoadingZones: false,
            allowViewportFollow: true);
        var scrolledHouseViewport = houseFourViewport.WithCameraOrigin(
            1f, houseFourViewport.CameraOriginY,
            installedHouseFour.Width, installedHouseFour.Height);
        var houseFourAfterScroll = houseFourSimulation.UpdateJourney(
            1, 0, 17L, true, installedHouseFour, scrolledHouseViewport,
            allowIslandLife: true, followLoadingZones: false,
            allowViewportFollow: true);
        var houseScrollStep = MathF.Sqrt(
            MathF.Pow((houseFourAfterScroll.MapX - houseFourBeforeScroll.MapX) * 16f, 2f) +
            MathF.Pow((houseFourAfterScroll.MapY - houseFourBeforeScroll.MapY) * 16f, 2f));
        Assert(houseFourAfterScroll.Action != LiveWallpaperLinkRouteAction.Hidden &&
               houseScrollStep <= 6.1f,
               "Interior camera scrolling must not reset, teleport, or hide Link's live journey.");
    }
    var dungeonSeven2dPath = Path.Combine(
        wallpaperGameDataRoot, "Maps", "dungeon7_2d.map");
    var dungeonSevenFourPath = Path.Combine(
        wallpaperGameDataRoot, "Maps", "dungeon7_4.map");
    using (var dungeonSeven2dReader = File.OpenText(dungeonSeven2dPath))
    using (var dungeonSevenFourReader = File.OpenText(dungeonSevenFourPath))
    {
        var loadedDungeonSeven2d = LiveWallpaperMap.TryLoad(
            dungeonSeven2dReader, out var dungeonSeven2d);
        var loadedDungeonSevenFour = LiveWallpaperMap.TryLoad(
            dungeonSevenFourReader, out var dungeonSevenFour);
        Assert(loadedDungeonSeven2d && loadedDungeonSevenFour,
               "The real side-view door pair must load for wallpaper transition coverage.");
        var sideViewDoor = dungeonSeven2d.Portals.Single(portal =>
            portal.Is2DDoor && portal.EntryId == "d7_2d");
        var topDownDoor = dungeonSevenFour.Portals.Single(portal =>
            portal.EntryId == sideViewDoor.ExitId);
        Assert(dungeonSeven2d.Is2DMap && sideViewDoor.HasDestination &&
               sideViewDoor.NextMap == "dungeon7_4.map" &&
               Math.Abs(sideViewDoor.LinkTargetX - 72f) < 0.001f &&
               Math.Abs(sideViewDoor.LinkTargetY - 208f) < 0.001f &&
               Math.Abs(sideViewDoor.GetLinkSpawnX(true) - 72f) < 0.001f &&
               Math.Abs(sideViewDoor.GetLinkSpawnY(true) - 208f) < 0.001f &&
               topDownDoor.NextMap == "dungeon7_2d.map",
               "ObjDoor2d must use its installed destination and exact pos+8,pos+16 transition point.");
        Assert(sideViewDoor.ShouldActivateAt(
                   sideViewDoor.LinkTargetX, sideViewDoor.LinkTargetY,
                   0f, 1) &&
               !sideViewDoor.ShouldActivateAt(
                   sideViewDoor.LinkTargetX, sideViewDoor.LinkTargetY,
                   0f, 3) &&
               sideViewDoor.ShouldActivateAt(
                   sideViewDoor.LinkTargetX, sideViewDoor.LinkTargetY,
                   -1f, 3),
               "A 2D doorway must activate on the terminal zero-input frame only when Link retains the preceding upward entry direction.");
    }
    var dungeonTwoPath = Path.Combine(
        wallpaperGameDataRoot, "Maps", "dungeon2.map");
    using (var dungeonTwoReader = File.OpenText(dungeonTwoPath))
    {
        Assert(LiveWallpaperMap.TryLoad(dungeonTwoReader, out var dungeonTwo) &&
               dungeonTwo.MapOffsetX == 1 && dungeonTwo.MapOffsetY == 1,
               "Wallpaper maps must preserve dungeon room-grid offsets from the installed map header.");
        const float dungeonRoomCenterX = (41f + 5f) * 16f;
        const float dungeonRoomCenterY = (41f + 4f) * 16f;
        Assert(LiveWallpaperMapViewport.TryCreateCentered(
                   1080, 2400, dungeonTwo.Width, dungeonTwo.Height,
                   dungeonRoomCenterX, dungeonRoomCenterY, 0.5f,
                   out var dungeonRoomViewport) &&
               !dungeonRoomViewport.TryGetRoomScrollTarget(
                   dungeonRoomCenterX, dungeonRoomCenterY,
                   dungeonTwo.MapOffsetX, dungeonTwo.MapOffsetY,
                   dungeonTwo.Width, dungeonTwo.Height, out _, out _) &&
               dungeonRoomViewport.TryGetRoomScrollTarget(
                   dungeonRoomCenterX - 10f * 16f, dungeonRoomCenterY,
                   dungeonTwo.MapOffsetX, dungeonTwo.MapOffsetY,
                   dungeonTwo.Width, dungeonTwo.Height,
                   out var adjacentRoomX, out var adjacentRoomY) &&
               Math.Abs(adjacentRoomX -
                        (dungeonRoomViewport.CameraOriginX - 10f)) < 0.001f &&
               Math.Abs(adjacentRoomY -
                        dungeonRoomViewport.CameraOriginY) < 0.001f,
               "Dungeon wallpaper scrolling must stay centred within a room and move by one offset-aware 10x8 room at its boundary.");
        Assert(LiveWallpaperMapViewport.TryCreateCentered(
                   1200, 2608, dungeonTwo.Width, dungeonTwo.Height,
                   680f, 728f, 0.5f, out var dungeonEntryViewport),
               "The on-device dungeon entry must produce a wallpaper viewport.");
        var dungeonContinuationVariants = Enumerable.Range(0, 120).Count(
            variant => LiveWallpaperJourneyPlanner.Create(
                dungeonTwo, dungeonEntryViewport, 1, variant,
                allowIslandLife: true,
                continuationPixelX: 680f,
                continuationPixelY: 728f).Points.Count > 0);
        Assert(dungeonContinuationVariants > 0,
               $"The real Dungeon 2 entry must offer autonomous continuation routes (variants={dungeonContinuationVariants}).");
        var dungeonEntrySimulation = new LiveWallpaperLinkSimulation();
        dungeonEntrySimulation.EnterMap(680f, 728f);
        var dungeonEntryMoved = false;
        for (var frame = 0; frame < 1_200 && !dungeonEntryMoved; frame++)
        {
            var dungeonLink = dungeonEntrySimulation.UpdateJourney(
                1, 0, 68L * 20_000L + frame * 17L, true,
                dungeonTwo, dungeonEntryViewport, allowIslandLife: true,
                allowViewportFollow: true);
            var deltaX = dungeonLink.MapX * 16f - 680f;
            var deltaY = dungeonLink.MapY * 16f - 728f;
            dungeonEntryMoved = deltaX * deltaX + deltaY * deltaY > 64f;
        }
        Assert(dungeonEntryMoved,
               "An empty dungeon journey variant must retry instead of leaving Link walking against the entry collider forever.");
    }
    Assert(LiveWallpaperMapViewport.TryCreate(
               1080, 2400, installedOverworld.Height, 1, 0.5f,
               out var followedOverworldViewport),
           "The real overworld must produce a wallpaper viewport.");

    var followedOverworldSimulation = new LiveWallpaperLinkSimulation();
    var visitedOverworldFields = new HashSet<(int X, int Y)>();
    var completedOverworldTransitions = 0;
    var followedViewportChanges = 0;
    LiveWallpaperSimulatedLinkState? previousFollowedLink = null;
    var maximumFollowedStep = 0f;
    var stationaryFrames = 0;
    var maximumStationaryFrames = 0;
    var stationaryMapX = 0f;
    var stationaryMapY = 0f;
    var stationaryAction = LiveWallpaperLinkRouteAction.Stand;
    var stationaryInputX = 0f;
    var stationaryInputY = 0f;
    (int X, int Y)? currentFollowedField = null;
    var sameFieldFrames = 0;
    var maximumSameFieldFrames = 0;
    (int X, int Y) maximumSameField = default;
    var lastNewFollowedFieldFrame = 0;
    for (var frame = 0; frame < 60_000; frame++)
    {
        var link = followedOverworldSimulation.UpdateJourney(
            1, 0, frame * 17L, true,
            installedOverworld, followedOverworldViewport,
            allowIslandLife: true, followLoadingZones: true);
        var followedField =
            ((int)MathF.Floor(link.MapX / 10f),
             (int)MathF.Floor(link.MapY / 8f));
        if (visitedOverworldFields.Add(followedField))
            lastNewFollowedFieldFrame = frame;
        if (currentFollowedField.HasValue &&
            currentFollowedField.Value == followedField)
            sameFieldFrames++;
        else
        {
            currentFollowedField = followedField;
            sameFieldFrames = 1;
        }
        if (sameFieldFrames > maximumSameFieldFrames)
        {
            maximumSameFieldFrames = sameFieldFrames;
            maximumSameField = followedField;
        }
        if (previousFollowedLink.HasValue)
        {
            var deltaX = (link.MapX - previousFollowedLink.Value.MapX) * 16f;
            var deltaY = (link.MapY - previousFollowedLink.Value.MapY) * 16f;
            maximumFollowedStep = Math.Max(
                maximumFollowedStep,
                MathF.Sqrt(deltaX * deltaX + deltaY * deltaY));
            if (deltaX * deltaX + deltaY * deltaY <= 0.01f &&
                link.Action != LiveWallpaperLinkRouteAction.Hidden)
            {
                stationaryFrames++;
                if (stationaryFrames > maximumStationaryFrames)
                {
                    maximumStationaryFrames = stationaryFrames;
                    stationaryMapX = link.MapX * 16f;
                    stationaryMapY = link.MapY * 16f;
                    stationaryAction = link.Action;
                    stationaryInputX = link.Input.Move.X;
                    stationaryInputY = link.Input.Move.Y;
                }
            }
            else
            {
                stationaryFrames = 0;
            }
        }
        previousFollowedLink = link;
        if (followedOverworldViewport.TryGetEdgeScrollTarget(
                link.MapX * 16f, link.MapY * 16f,
                link.Input.Move.X, link.Input.Move.Y,
                installedOverworld.Width, installedOverworld.Height,
                out var targetOriginX, out var targetOriginY))
        {
            followedOverworldViewport = followedOverworldViewport.WithCameraOrigin(
                targetOriginX, targetOriginY,
                installedOverworld.Width, installedOverworld.Height);
            followedViewportChanges++;
        }
        if (link.Action == LiveWallpaperLinkRouteAction.Hidden)
            completedOverworldTransitions++;
    }
    var emptyBoundaryVariants = Enumerable.Range(0, 60)
        .Where(variant => LiveWallpaperJourneyPlanner.Create(
            installedOverworld, followedOverworldViewport, 1, variant,
            allowIslandLife: true,
            continuationPixelX: 48f,
            continuationPixelY: 1928f,
            followLoadingZones: true).Points.Count < 2)
        .ToArray();
    Assert(completedOverworldTransitions >= 2 &&
           visitedOverworldFields.Count >= 60 &&
           lastNewFollowedFieldFrame > 20_000 &&
           followedViewportChanges >= 1 &&
           maximumFollowedStep <= 8f &&
           maximumStationaryFrames < 600 &&
           maximumSameFieldFrames < 900 &&
           emptyBoundaryVariants.Length == 0,
           $"Following Link must visibly leave the selected starting scene and continue through multiple real overworld fields " +
           $"(exits={completedOverworldTransitions}, fields={visitedOverworldFields.Count}, " +
           $"lastNewFieldFrame={lastNewFollowedFieldFrame}, " +
           $"camera={followedViewportChanges}, maxStep={maximumFollowedStep}, " +
           $"sameFieldFrames={maximumSameFieldFrames} at {maximumSameField}, " +
           $"stationary={maximumStationaryFrames} at {stationaryMapX},{stationaryMapY}, " +
           $"action={stationaryAction}, input={stationaryInputX},{stationaryInputY}, " +
           $"emptyBoundaryVariants={string.Join(',', emptyBoundaryVariants)}).");
}

var bushJourneyMapData = new System.Text.StringBuilder(
    "3\n0\n0\noverworld.png\n40\n90\n1\n");
for (var row = 0; row < 90; row++)
    bushJourneyMapData.AppendLine(string.Join(',', Enumerable.Repeat("0", 40)));
// Keep the fixture wall on a key divisible by eight so ObjBush's first
// deterministic 1-in-8 wallpaper roll emits a rupee.
var bushWallX = (journeyMinimumX + journeyMaximumX) / 256 * 128;
var bushWallStartY = Math.Max(0, (journeyMinimumY - 16) / 16 * 16);
var bushWallEndY = Math.Min(89 * 16, (journeyMaximumY + 16) / 16 * 16);
var bushWallCount = (bushWallEndY - bushWallStartY) / 16 + 1;
bushJourneyMapData.AppendLine("1");
bushJourneyMapData.AppendLine("bush");
bushJourneyMapData.AppendLine(bushWallCount.ToString());
for (var bushY = bushWallStartY; bushY <= bushWallEndY; bushY += 16)
    bushJourneyMapData.AppendLine($"0;{bushWallX};{bushY}");
Assert(LiveWallpaperMap.TryLoad(
           new StringReader(bushJourneyMapData.ToString()), out var bushJourneyMap),
       "The bush-cutting journey fixture must be a valid installed map.");
var cornerBodyX = bushWallX + 15.5f;
var cornerBodyY = bushWallStartY - 9.5f;
Assert(!bushJourneyMap.TryGetBushKey(
           cornerBodyX + 1f, cornerBodyY + 1f, 8f, 10f, out _) &&
       bushJourneyMap.TryGetBushKeyAlongMovement(
           cornerBodyX, cornerBodyY, 8f, 10f,
           1f, 1f, out var cornerBushKey) &&
       cornerBushKey == bushJourneyMap.GetBushKey(
           bushWallX, bushWallStartY),
       "Diagonal movement must detect a bush touched by its intermediate axis step at a half-tile corner.");
LiveWallpaperJourneyPlan bushJourney = null;
var bushJourneyVariant = -1;
for (var variant = 0; variant < 60 && bushJourney == null; variant++)
{
    var candidate = LiveWallpaperJourneyPlanner.Create(
        bushJourneyMap, journeyViewport, 1, variant,
        allowIslandLife: true, followLoadingZones: true);
    if (candidate.Points.Any(point =>
            point.Action == LiveWallpaperJourneyAction.CutBush))
    {
        bushJourney = candidate;
        bushJourneyVariant = variant;
    }
}
Assert(bushJourney != null,
       "A route through installed bushes must schedule a real sword cut before crossing.");
var bushRolls = new Queue<int>(new[] { 0, 1 });
Assert(BushDropRules.Roll((minimum, maximum) => bushRolls.Dequeue()) ==
       BushDropRules.RupeeItemName && bushRolls.Count == 0,
       "The shared ObjBush drop rule must perform the real 1-in-8 roll followed by the heart/rupee roll.");
var suppressedHeartRolls = new Queue<int>(new[] { 0, 0 });
Assert(BushDropRules.Roll(
           (minimum, maximum) => suppressedHeartRolls.Dequeue(),
           noHeartDrops: true) == null && suppressedHeartRolls.Count == 0,
       "No-heart mode must suppress a rolled heart without changing the random-call sequence.");
var firstDropFrame = DroppedItemMotion.Resolve(
    Microsoft.Xna.Framework.Vector2.UnitX, 17L);
Assert(Math.Abs(firstDropFrame.Offset.X - 0.5f) < 0.0001f &&
       Math.Abs(firstDropFrame.Offset.Y) < 0.0001f &&
       Math.Abs(firstDropFrame.Height - 0.65f) < 0.0001f &&
       !firstDropFrame.Grounded,
       "Wallpaper drops must use ObjItem's gravity-first launch on their first 60 Hz body update.");
var settledDrop = DroppedItemMotion.Resolve(
    Microsoft.Xna.Framework.Vector2.UnitX, 2_000L);
Assert(settledDrop.Grounded && settledDrop.Height == 0f &&
       settledDrop.Offset.X > firstDropFrame.Offset.X,
       "Wallpaper drops must use ObjItem's bounce and drag until they settle on the floor.");
DroppedItemMotion.ResolveCollectedVisual(
    125L, out var collectedItemOffset, out var collectedItemAlpha,
    out var collectedItemVisible);
Assert(Math.Abs(collectedItemOffset + 8.660254f) < 0.0001f &&
       collectedItemAlpha == 1f && collectedItemVisible,
       "Collected wallpaper drops must use ObjItem's exact 250 ms upward fade curve.");
DroppedItemMotion.ResolveCollectedVisual(
    300L, out collectedItemOffset, out collectedItemAlpha,
    out collectedItemVisible);
Assert(collectedItemOffset == 0f &&
       Math.Abs(collectedItemAlpha - 0.5f) < 0.0001f &&
       collectedItemVisible,
       "Collected wallpaper drops must use ObjItem's exact 250-350 ms alpha fade.");
DroppedItemMotion.ResolveCollectedVisual(
    351L, out _, out _, out collectedItemVisible);
Assert(!collectedItemVisible,
       "Collected wallpaper drops must disappear after ObjItem's 350 ms despawn time.");
var bushSimulation = new LiveWallpaperLinkSimulation(
    FindBushDropSeed(BushDropRules.RupeeItemName));
var sawBushSword = false;
var sawCutBush = false;
var sawRupeeDrop = false;
var sawCollectedRupee = false;
var bushStartTime = bushJourneyVariant * 20_000L;
for (var frame = 0; frame < 4_000 && !sawCollectedRupee; frame++)
{
    var link = bushSimulation.UpdateJourney(
        1, 0, bushStartTime + frame * 17L, true,
        bushJourneyMap, journeyViewport, allowIslandLife: true,
        followLoadingZones: true);
    sawBushSword |= link.Action == LiveWallpaperLinkRouteAction.Attack;
    sawCutBush |= link.CutBushes?.Count > 0;
    sawRupeeDrop |= link.VegetationDrops?.Values.Any(
        drop => drop == LiveWallpaperVegetationDropKind.Rupee) == true;
    sawCollectedRupee |= link.CollectedRupees > 0;
}
Assert(sawBushSword && sawCutBush && sawRupeeDrop && sawCollectedRupee,
       "Wallpaper Link must swing the sword, remove the installed bush collider, and walk over a spawned rupee to collect it.");
var heartSimulation = new LiveWallpaperLinkSimulation(
    FindBushDropSeed(BushDropRules.HeartItemName));
var sawHeartDrop = false;
var sawCollectedHeart = false;
for (var frame = 0; frame < 4_000 && !sawCollectedHeart; frame++)
{
    var link = heartSimulation.UpdateJourney(
        1, 0, bushStartTime + frame * 17L, true,
        bushJourneyMap, journeyViewport, allowIslandLife: true,
        followLoadingZones: true);
    sawHeartDrop |= link.VegetationDrops?.Values.Any(
        drop => drop == LiveWallpaperVegetationDropKind.Heart) == true;
    sawCollectedHeart |= link.CollectedHearts > 0;
}
Assert(sawHeartDrop && sawCollectedHeart,
       "Wallpaper Link must walk over and collect a heart produced by the shared ObjBush drop rule.");
var bushContinuation = LiveWallpaperJourneyPlanner.Create(
    bushJourneyMap, journeyViewport, 1, 0,
    allowIslandLife: true,
    continuationPixelX: bushWallX + 16f,
    continuationPixelY: bushWallStartY + 8f,
    followLoadingZones: true);
Assert(bushContinuation.Points.Count > 1 &&
       bushContinuation.Points.Any(point =>
           point.Action == LiveWallpaperJourneyAction.CutBush),
       "A continuation rounded onto a bush boundary must retain a route and schedule the starting bush cut.");
var grassJourneyMapData = new System.Text.StringBuilder(
    "3\n0\n0\noverworld.png\n40\n90\n1\n");
for (var row = 0; row < 90; row++)
    grassJourneyMapData.AppendLine(string.Join(',', Enumerable.Repeat("0", 40)));
grassJourneyMapData.AppendLine("1");
grassJourneyMapData.AppendLine("grasForest");
grassJourneyMapData.AppendLine(bushWallCount.ToString());
for (var grassY = bushWallStartY; grassY <= bushWallEndY; grassY += 16)
    grassJourneyMapData.AppendLine($"0;{bushWallX};{grassY}");
Assert(LiveWallpaperMap.TryLoad(
           new StringReader(grassJourneyMapData.ToString()), out var grassJourneyMap),
       "The grass-cutting journey fixture must be a valid installed map.");
LiveWallpaperJourneyPlan grassJourney = null;
var grassJourneyVariant = -1;
for (var variant = 0; variant < 60 && grassJourney == null; variant++)
{
    var candidate = LiveWallpaperJourneyPlanner.Create(
        grassJourneyMap, journeyViewport, 1, variant,
        allowIslandLife: true, followLoadingZones: true);
    if (candidate.Points.Any(point =>
            point.Action == LiveWallpaperJourneyAction.CutBush))
    {
        grassJourney = candidate;
        grassJourneyVariant = variant;
    }
}
Assert(grassJourney != null,
       "Routes must recognize sword-hittable gras templates even though ObjBush gives them no collider.");
var grassSimulation = new LiveWallpaperLinkSimulation();
var sawGrassSword = false;
var sawCutGrass = false;
var grassStartTime = grassJourneyVariant * 20_000L;
for (var frame = 0; frame < 4_000 && !sawCutGrass; frame++)
{
    var link = grassSimulation.UpdateJourney(
        1, 0, grassStartTime + frame * 17L, true,
        grassJourneyMap, journeyViewport, allowIslandLife: true,
        followLoadingZones: true);
    sawGrassSword |= link.Action == LiveWallpaperLinkRouteAction.Attack;
    sawCutGrass |= link.CutVegetationTimes?.Count > 0;
}
Assert(sawGrassSword && sawCutGrass,
       "Wallpaper Link must use his sword and emit the real leaf event when a route crosses installed grass.");
var stoneJourneyMapData = new System.Text.StringBuilder(
    "3\n0\n0\noverworld.png\n40\n90\n1\n");
for (var row = 0; row < 90; row++)
    stoneJourneyMapData.AppendLine(string.Join(',', Enumerable.Repeat("0", 40)));
stoneJourneyMapData.AppendLine("1");
stoneJourneyMapData.AppendLine("stone");
stoneJourneyMapData.AppendLine(bushWallCount.ToString());
for (var stoneY = bushWallStartY; stoneY <= bushWallEndY; stoneY += 16)
    stoneJourneyMapData.AppendLine($"0;{bushWallX};{stoneY}");
Assert(LiveWallpaperMap.TryLoad(
           new StringReader(stoneJourneyMapData.ToString()), out var stoneJourneyMap),
       "The stone-lifting journey fixture must be a valid installed map.");
LiveWallpaperJourneyPlan stoneJourney = null;
var stoneJourneyVariant = -1;
for (var variant = 0; variant < 60 && stoneJourney == null; variant++)
{
    var candidate = LiveWallpaperJourneyPlanner.Create(
        stoneJourneyMap, journeyViewport, 1, variant,
        allowIslandLife: true, followLoadingZones: true);
    if (candidate.Points.Any(point =>
            point.Action == LiveWallpaperJourneyAction.LiftStone))
    {
        stoneJourney = candidate;
        stoneJourneyVariant = variant;
    }
}
Assert(stoneJourney != null,
       "A route through installed ObjStone objects must schedule their lift before crossing.");
Assert(StoneGameplayMotion.CreateThrowVelocity(0) ==
           new Microsoft.Xna.Framework.Vector2(-3f, 0f) &&
       StoneGameplayMotion.CreateThrowVelocity(1) ==
           new Microsoft.Xna.Framework.Vector2(0f, -3f) &&
       StoneGameplayMotion.CreateThrowVelocity(2) ==
           new Microsoft.Xna.Framework.Vector2(3f, 0f) &&
       StoneGameplayMotion.CreateThrowVelocity(3) ==
           new Microsoft.Xna.Framework.Vector2(0f, 3f),
       "The shared stone throw rule must preserve ObjLink's exact cardinal 3 px/frame velocities.");
Assert(Math.Abs(StoneGameplayMotion.ResolveHeight(13) - 1.625f) < 0.0001f &&
       StoneGameplayMotion.ResolveHeight(
           StoneGameplayMotion.ThrowFlightFrames) == 0f,
       "The shared stone trajectory must apply ObjStone gravity first and land on frame 14.");
var stoneSimulation = new LiveWallpaperLinkSimulation();
var sawStoneLift = false;
var sawStoneCarry = false;
var sawStoneThrow = false;
var sawLiftedStone = false;
var stoneSequenceFinished = false;
var maximumCarriedStoneHeight = 0f;
var throwStartX = 0f;
var throwStartY = 0f;
var maximumThrowTravel = 0f;
var sawStoneImpact = false;
var sawReleasedStone = false;
var sawReleasedStoneBehindLink = false;
var sawFirstThrownGravityStep = false;
var fullyCarriedStoneAt = -1L;
var firstStoneThrowAt = -1L;
var stoneStartTime = stoneJourneyVariant * 20_000L;
for (var frame = 0; frame < 4_000 && !stoneSequenceFinished; frame++)
{
    var link = stoneSimulation.UpdateJourney(
        1, 0, stoneStartTime + frame * 17L, true,
        stoneJourneyMap, journeyViewport, allowIslandLife: true,
        followLoadingZones: true);
    sawStoneLift |= link.Action == LiveWallpaperLinkRouteAction.LiftStone;
    sawStoneCarry |= link.Action == LiveWallpaperLinkRouteAction.CarryStone;
    if (fullyCarriedStoneAt < 0 && !link.ActiveStoneReleased &&
        link.ActiveLiftedStoneKey >= 0 && link.ActiveStoneHeight >= 12.999f)
        fullyCarriedStoneAt = stoneStartTime + frame * 17L;
    if (link.Action == LiveWallpaperLinkRouteAction.ThrowStone && !sawStoneThrow)
    {
        sawStoneThrow = true;
        firstStoneThrowAt = stoneStartTime + frame * 17L;
        throwStartX = link.ActiveStoneEntityX;
        throwStartY = link.ActiveStoneEntityY;
    }
    sawLiftedStone |= link.LiftedStones?.Count > 0;
    sawStoneImpact |= link.StoneImpactKind !=
                      LiveWallpaperStoneImpactKind.None;
    sawReleasedStone |= link.ActiveStoneReleased;
    sawReleasedStoneBehindLink |=
        LiveWallpaperLinkPlacement.DrawActiveStoneBeforeLink(link);
    if (link.ActiveLiftedStoneKey >= 0)
    {
        maximumCarriedStoneHeight = Math.Max(
            maximumCarriedStoneHeight, link.ActiveStoneHeight);
        if (sawStoneThrow)
        {
            var throwDeltaX = link.ActiveStoneEntityX - throwStartX;
            var throwDeltaY = link.ActiveStoneEntityY - throwStartY;
            maximumThrowTravel = Math.Max(
                maximumThrowTravel,
                MathF.Sqrt(throwDeltaX * throwDeltaX +
                           throwDeltaY * throwDeltaY));
            if (maximumThrowTravel >= 2.9f && maximumThrowTravel <= 3.1f &&
                link.ActiveStoneHeight < 13f)
                sawFirstThrownGravityStep = true;
        }
    }
    else if (sawStoneThrow)
    {
        stoneSequenceFinished = true;
    }
}
Assert(sawStoneLift && sawStoneCarry && sawStoneThrow && sawLiftedStone &&
       sawReleasedStone && sawReleasedStoneBehindLink &&
       sawFirstThrownGravityStep && sawStoneImpact && stoneSequenceFinished &&
       fullyCarriedStoneAt >= 0 &&
       firstStoneThrowAt - fullyCarriedStoneAt >=
           LinkGameplayMotion.MinimumSeparateInputMilliseconds &&
       maximumCarriedStoneHeight >= 12.5f &&
       maximumThrowTravel >= 30f,
       "Wallpaper Link must reproduce ObjLink's pull/carry/throw states and ObjStone's carried height, thrown motion, and impact.");
var stoneEnemySession = new LiveWallpaperEnemySimulation.Session();
var stoneEnemyStart = stoneEnemySession.Resolve(journeyMap, 0, 0, null);
var stoneHitLink = new LiveWallpaperSimulatedLinkState(
    0, 0, 0, 2, LiveWallpaperLinkRouteAction.Stand, default,
    stoneImpactKind: LiveWallpaperStoneImpactKind.Enemy,
    stoneImpactX: stoneEnemyStart.PixelX - 3,
    stoneImpactY: stoneEnemyStart.PixelY,
    stoneImpactStartedAt: 17,
    stoneImpactSerial: 1,
    stoneImpactEnemyIndex: 0);
var stoneEnemyHit = stoneEnemySession.Resolve(
    journeyMap, 0, 17, stoneHitLink);
var stoneEnemyDead = stoneEnemySession.Resolve(
    journeyMap, 0, 500, stoneHitLink);
Assert((stoneEnemyHit.Action is LiveWallpaperEnemyAction.Hit or
            LiveWallpaperEnemyAction.Hidden) && !stoneEnemyDead.Visible,
       "Thrown stones must apply ObjStone's two damage exactly once and kill a one-HP Octorok.");
for (var variant = 0; variant < 120; variant++)
{
    var candidate = LiveWallpaperJourneyPlanner.Create(
        journeyMap, journeyViewport, 1, variant, allowIslandLife: true);
    if (candidate.Points.Count > 0)
    {
        var last = candidate.Points[^1];
        sawLoadingZoneExit |= last.PixelX < journeyMinimumX ||
                              last.PixelX > journeyMaximumX ||
                              last.PixelY < journeyMinimumY ||
                              last.PixelY > journeyMaximumY;
        sawLoadingZoneExit |= last.Action == LiveWallpaperJourneyAction.Exit;
    }
    if (interactionJourney == null && candidate.HasInteraction)
    {
        interactionJourney = candidate;
        interactionJourneyVariant = variant;
    }
    if (roosterJourney == null && candidate.HasRoosterFlight)
    {
        roosterJourney = candidate;
        roosterJourneyVariant = variant;
    }
    if (combatJourney == null && candidate.HasCombat)
    {
        combatJourney = candidate;
        combatJourneyVariant = variant;
    }
    if (candidate.HasCombat)
        combatJourneyCount++;
}
Assert(interactionJourney != null && interactionJourney.Points.Count > 2 &&
       interactionJourney.InteractionActorIndex == 0 &&
       interactionJourney.Points[interactionJourney.InteractionPointIndex].Action ==
           LiveWallpaperJourneyAction.Interact &&
       roosterJourney != null && roosterJourney.Points.Count > 6 &&
       roosterJourney.Points.Any(point =>
           point.Action == LiveWallpaperJourneyAction.RoosterFly) &&
       combatJourney != null && combatJourney.Points.Count > 2 &&
       combatJourney.CombatEnemyIndex == 0 &&
       combatJourney.Points[combatJourney.CombatPointIndex].Action ==
           LiveWallpaperJourneyAction.Attack &&
       combatJourneyCount >= 60 &&
       sawLoadingZoneExit,
       "Wallpaper journeys must cross real boundaries, continue through loading zones, and include interactions, flights, and fights.");
foreach (var point in interactionJourney.Points)
{
    Assert(!journeyMap.IntersectsActor(
               point.PixelX - 4, point.PixelY - 10, 8, 10),
           "Wallpaper journey paths must not cross installed NPC bodies.");
}
var interactionSimulation = new LiveWallpaperLinkSimulation();
var sawInteraction = false;
var interactionStartTime = interactionJourneyVariant * 20_000L;
LiveWallpaperSimulatedLinkState? previousInteractionLink = null;
var maximumInteractionStep = 0f;
for (var frame = 0; frame < 3_000; frame++)
{
    var link = interactionSimulation.UpdateJourney(
        1, 0, interactionStartTime + frame * 17L, true,
        journeyMap, journeyViewport, allowIslandLife: true);
    if (previousInteractionLink.HasValue)
    {
        var previousPixelX = previousInteractionLink.Value.MapX * 16f;
        var previousPixelY = previousInteractionLink.Value.MapY * 16f;
        var previousWasVisible =
            previousInteractionLink.Value.Action !=
                LiveWallpaperLinkRouteAction.Hidden &&
            previousPixelX >= journeyMinimumX &&
            previousPixelX <= journeyMaximumX &&
            previousPixelY >= journeyMinimumY &&
            previousPixelY <= journeyMaximumY;
        if (previousWasVisible)
        {
            var deltaX = link.MapX * 16f - previousPixelX;
            var deltaY = link.MapY * 16f - previousPixelY;
            maximumInteractionStep = Math.Max(
                maximumInteractionStep,
                MathF.Sqrt(deltaX * deltaX + deltaY * deltaY));
        }
    }
    previousInteractionLink = link;
    sawInteraction |= link.Action == LiveWallpaperLinkRouteAction.Interact &&
                      link.InteractionActorIndex == 0;
    Assert(!journeyMap.IntersectsActor(
               link.MapX * 16f - 4, link.MapY * 16f - 10, 8, 10),
           "Wallpaper Link must not enter an NPC body while following an interaction route.");
}
Assert(maximumInteractionStep <= 2f,
       "Completing a wallpaper journey must continue from Link's current endpoint without teleporting across obstacles.");
var roosterSimulation = new LiveWallpaperLinkSimulation();
var roosterPickupStart = new Microsoft.Xna.Framework.Vector3(40f, 56f, 0f);
var roosterLinkPosition = new Microsoft.Xna.Framework.Vector3(40f, 56f, 0f);
Assert(RoosterGameplayMotion.ResolvePickupPosition(
           roosterPickupStart, roosterLinkPosition,
           RoosterGameplayMotion.PullMilliseconds).Z == 0f &&
       RoosterGameplayMotion.ResolvePickupPosition(
           roosterPickupStart, roosterLinkPosition,
           RoosterGameplayMotion.PickupSequenceMilliseconds).Z ==
       RoosterGameplayMotion.CarryHeight,
       "Rooster pickup must remain grounded for ObjLink's pull and finish at ObjCock's exact carry height.");
Assert(Math.Abs(RoosterGameplayMotion.ResolveHoverTarget(0) - 36f) < 0.0001f &&
       Math.Abs(RoosterGameplayMotion.AdvanceFlightHeight(
           RoosterGameplayMotion.CarryHeight, 0, 1f) - 14.5f) < 0.0001f,
       "Rooster flight must use ObjCock's real hover target and 0.5 px/frame ascent.");
var releasedRooster = RoosterGameplayMotion.AdvanceRelease(
    new RoosterReleaseMotionState(
        Microsoft.Xna.Framework.Vector2.Zero,
        RoosterGameplayMotion.HoverHeight,
        Microsoft.Xna.Framework.Vector2.UnitX * StoneGameplayMotion.ThrowSpeed,
        0f, grounded: false),
    1f);
Assert(Math.Abs(releasedRooster.Position.X - StoneGameplayMotion.ThrowSpeed) < 0.0001f &&
       Math.Abs(releasedRooster.Height - 35.925f) < 0.0001f &&
       Math.Abs(releasedRooster.Velocity.X - 2.925f) < 0.0001f &&
       !releasedRooster.Grounded,
       "Released rooster motion must match ObjCock's gravity and airborne drag instead of teleporting to the ground.");
var sawRoosterFlight = false;
var sawRoosterPickup = false;
var sawRoosterThrow = false;
var sawRoosterDescent = false;
var roosterReleaseHeight = 0f;
var roosterStartTime = roosterJourneyVariant * 20_000L;
for (var frame = 0; frame < 3_000; frame++)
{
    var link = roosterSimulation.UpdateJourney(
        1, 0, roosterStartTime + frame * 17L, true,
        journeyMap, journeyViewport, allowIslandLife: true);
    sawRoosterPickup |=
        link.Action == LiveWallpaperLinkRouteAction.RoosterPickup &&
        link.RoosterHeight <= RoosterGameplayMotion.CarryHeight;
    sawRoosterFlight |= link.CarryingRooster &&
                        link.Action == LiveWallpaperLinkRouteAction.RoosterFly &&
                        link.RoosterHeight > link.Height;
    if (link.Action == LiveWallpaperLinkRouteAction.RoosterThrow)
    {
        sawRoosterThrow = true;
        roosterReleaseHeight = Math.Max(roosterReleaseHeight, link.RoosterHeight);
    }
    sawRoosterDescent |= sawRoosterThrow && !link.CarryingRooster &&
                         link.RoosterVisible &&
                         link.RoosterHeight < roosterReleaseHeight - 0.05f;
}
Assert(sawInteraction && sawRoosterPickup && sawRoosterFlight &&
       sawRoosterThrow && sawRoosterDescent,
       "Wallpaper journey simulation must pause to face NPCs and reproduce ObjCock's pickup, flight, throw, and descent states.");
var combatSimulation = new LiveWallpaperLinkSimulation();
var sawCombat = false;
var combatStartTime = combatJourneyVariant * 20_000L;
LiveWallpaperSimulatedLinkState combatLink = default;
var sawEnemyReaction = false;
for (var frame = 0; frame < 3_000; frame++)
{
    combatLink = combatSimulation.UpdateJourney(
        1, 0, combatStartTime + frame * 17L, true,
        journeyMap, journeyViewport, allowIslandLife: true);
    var attacking = combatLink.Action == LiveWallpaperLinkRouteAction.Attack &&
                    combatLink.CombatEnemyIndex == 0;
    sawCombat |= attacking;
    if (attacking)
    {
        var enemyState = LiveWallpaperEnemySimulation.Resolve(
            journeyMap, 0, combatStartTime + frame * 17L, combatLink);
        sawEnemyReaction |= enemyState.Action is LiveWallpaperEnemyAction.Hit or
                            LiveWallpaperEnemyAction.Hidden;
    }
}
Assert(sawCombat && sawEnemyReaction,
       "Wallpaper combat must make Link attack an installed enemy and keep enemy reaction state side-effect-free.");
var constrainedSimulation = new LiveWallpaperLinkSimulation();
constrainedSimulation.Update(
    1, new LiveWallpaperLinkState(true, true, 0f), 0, true, constrainedMap);
LiveWallpaperSimulatedLinkState constrainedLink = default;
var detourMinimumY = float.MaxValue;
for (var frame = 1; frame <= 90; frame++)
{
    constrainedLink = constrainedSimulation.Update(
        1, new LiveWallpaperLinkState(true, true, 0.2f),
        frame * 17L, true, constrainedMap);
    detourMinimumY = Math.Min(detourMinimumY, constrainedLink.MapY);
}
Assert(constrainedLink.MapX > 25.5f && detourMinimumY < 81.25f &&
       !constrainedMap.IntersectsCollision(
           constrainedLink.MapX * 16f + constrainedSimulation.Body.OffsetX,
           constrainedLink.MapY * 16f + constrainedSimulation.Body.OffsetY,
           constrainedSimulation.Body.Width,
           constrainedSimulation.Body.Height,
           includeHoles: true),
       "Wallpaper Link must steer through a nearby passage without crossing installed collision.");
var wallpaperFollowerSimulation = new LiveWallpaperFollowerSimulation();
wallpaperFollowerSimulation.Update(2, -14f, 0, animated: true);
var simulatedRooster = wallpaperFollowerSimulation.Update(2, 14f, 17, animated: true);
Assert(simulatedRooster.HorizontalOffset > -14f && simulatedRooster.FacingRight &&
       simulatedRooster.Height > 0 && !wallpaperFollowerSimulation.Body.IsGrounded,
       "Wallpaper followers must use body-backed distance steering and rooster hopping.");
var simulatedBowWow = wallpaperFollowerSimulation.Update(1, 100f, 2_000, animated: true);
Assert(simulatedBowWow.HorizontalOffset <= 46f,
       "Wallpaper BowWow must remain constrained by the in-game chain radius.");
var constrainedBowWowSimulation = new LiveWallpaperFollowerSimulation();
constrainedBowWowSimulation.Update(
    1, 0, 0, true, wallpaperCollisionMap, 72, 26);
var constrainedBowWow = constrainedBowWowSimulation.Update(
    1, 30, 17, true, wallpaperCollisionMap, 72, 26);
Assert(Math.Abs(constrainedBowWow.HorizontalOffset) < 0.001f &&
       constrainedBowWowSimulation.Body.Width == 14,
       "Wallpaper BowWow must use its gameplay body width and respect installed NPC walls.");
Assert(LiveWallpaperFrameScheduler.GetDelayMilliseconds(true, 15) == 66 &&
       LiveWallpaperFrameScheduler.GetDelayMilliseconds(true, 30) == 33 &&
       LiveWallpaperFrameScheduler.GetDelayMilliseconds(true, 60) == 16 &&
       LiveWallpaperFrameScheduler.GetDelayMilliseconds(true, 999) == 33 &&
       LiveWallpaperFrameScheduler.GetDelayMilliseconds(false, 30) == 1_000,
       "The wallpaper scheduler must support 15, 30, and opt-in 60 FPS while keeping its static low-power cadence.");
var firstHighFpsDelay = LiveWallpaperFrameScheduler.GetCompensatedDelayMilliseconds(
    1_000, 0, true, 60, out var firstHighFpsDeadline);
var renderCompensatedDelay = LiveWallpaperFrameScheduler.GetCompensatedDelayMilliseconds(
    1_006, firstHighFpsDeadline - 16, true, 60, out var compensatedDeadline);
var missedFrameDelay = LiveWallpaperFrameScheduler.GetCompensatedDelayMilliseconds(
    1_020, firstHighFpsDeadline - 16, true, 60, out var recoveredDeadline);
Assert(firstHighFpsDelay == 16 && firstHighFpsDeadline == 1_016 &&
       renderCompensatedDelay == 10 && compensatedDeadline == 1_016 &&
       missedFrameDelay == 0 && recoveredDeadline == 1_020,
       "Wallpaper scheduling must subtract rendering time and immediately recover from a missed frame deadline.");
var dayWildlife = LiveWallpaperWildlife.Resolve(0, LiveWallpaperTimePhase.Day);
var sunsetWildlife = LiveWallpaperWildlife.Resolve(0, LiveWallpaperTimePhase.Sunset);
var nightWildlife = LiveWallpaperWildlife.Resolve(0, LiveWallpaperTimePhase.Night);
var allWildlife = LiveWallpaperWildlife.Resolve(1, LiveWallpaperTimePhase.Night);
Assert(dayWildlife.ShowButterflies && !dayWildlife.ShowOwl &&
       sunsetWildlife.ShowButterflies && sunsetWildlife.ShowOwl &&
       !nightWildlife.ShowButterflies && nightWildlife.ShowOwl &&
       allWildlife.ShowButterflies && allWildlife.ShowOwl,
       "Wallpaper wildlife must follow daylight unless the always-show override is selected.");
Assert(LiveWallpaperCharacterSelection.Resolve(0, 3, 90_000) == 0 &&
       LiveWallpaperCharacterSelection.Resolve(1, 1, 0) == 1 &&
       LiveWallpaperCharacterSelection.Resolve(2, 1, 0) == 2 &&
       LiveWallpaperCharacterSelection.Resolve(3, 0, 0) == 0 &&
       LiveWallpaperCharacterSelection.Resolve(3, 0, 30_000) == 1 &&
       LiveWallpaperCharacterSelection.Resolve(3, 0, 60_000) == 2 &&
       LiveWallpaperCharacterSelection.Resolve(4, 1, 60_000) == 0 &&
       LiveWallpaperCharacterSelection.Resolve(4, 2, 0) == 2 &&
       LiveWallpaperCharacterSelection.Resolve(4, 3, 0) == 1 &&
       LiveWallpaperCharacterSelection.Resolve(4, 5, 0) == 2 &&
       LiveWallpaperCharacterSelection.Resolve(4, 6, 0) == 0 &&
       LiveWallpaperCharacterSelection.Resolve(4, 7, 0) == 2 &&
       LiveWallpaperCharacterSelection.Resolve(4, 0, 30_000) == 1,
       "Wallpaper featured characters must honor fixed, rotating, and scene-matched modes.");
var marinMotion = LiveWallpaperCharacterMotion.Resolve(0, 2_000, true);
var staticMarinMotion = LiveWallpaperCharacterMotion.Resolve(0, 2_000, false);
var bowWowMotion = LiveWallpaperCharacterMotion.Resolve(1, 750, true);
var roosterMotion = LiveWallpaperCharacterMotion.Resolve(2, 1_300, true);
Assert(marinMotion.ShowNotes && marinMotion.HorizontalOffset == 0f &&
       !staticMarinMotion.ShowNotes && staticMarinMotion.Lift == 0f &&
       bowWowMotion.HorizontalOffset > 0.7f && bowWowMotion.Lift > 0.99f &&
       roosterMotion.HorizontalOffset > 0.99f && roosterMotion.Lift > 0.99f,
       "Wallpaper characters must sing, wander, and hop with deterministic motion.");
var defaultLayout = LiveWallpaperSceneLayouts.Resolve(0);
var mabeLayout = LiveWallpaperSceneLayouts.Resolve(1);
var shoreLayout = LiveWallpaperSceneLayouts.Resolve(2);
var forestLayout = LiveWallpaperSceneLayouts.Resolve(3);
var castleLayout = LiveWallpaperSceneLayouts.Resolve(5);
var animalLayout = LiveWallpaperSceneLayouts.Resolve(6);
var eggLayout = LiveWallpaperSceneLayouts.Resolve(7);
Assert(Math.Abs(defaultLayout.FeaturedXRatio - 0.72f) < 0.001f &&
       Math.Abs(mabeLayout.GroundTileRow - 5.6f) < 0.001f &&
       Math.Abs(mabeLayout.FeaturedXRatio - 0.72f) < 0.001f &&
       Math.Abs(shoreLayout.GroundTileRow - 6f) < 0.001f &&
       Math.Abs(shoreLayout.FeaturedXRatio - 0.82f) < 0.001f &&
       Math.Abs(forestLayout.GroundTileRow - 5.35f) < 0.001f &&
       Math.Abs(forestLayout.FeaturedXRatio - 0.66f) < 0.001f &&
       Math.Abs(castleLayout.FeaturedXRatio - 0.5f) < 0.001f &&
       Math.Abs(animalLayout.FeaturedXRatio - 0.76f) < 0.001f &&
       Math.Abs(eggLayout.FeaturedXRatio - 0.5f) < 0.001f &&
       Math.Abs(LiveWallpaperSceneLayouts.ResolveFeaturedXRatio(1, 2) - 0.24f) < 0.001f &&
       Math.Abs(LiveWallpaperSceneLayouts.ResolveFeaturedXRatio(2, 2) - 0.5f) < 0.001f &&
       Math.Abs(LiveWallpaperSceneLayouts.ResolveFeaturedXRatio(3, 2) - 0.76f) < 0.001f,
       "Wallpaper scene layouts must expose stable regional and user-overridden anchors.");
Assert(LiveWallpaperMapViewport.TryCreate(
           1080, 2400, 128, 1, 0.5f, out var portraitViewport) &&
       portraitViewport.Columns == 14 && portraitViewport.Rows >= 28 &&
       portraitViewport.TileSize == 90f &&
       portraitViewport.Left <= 0 &&
       portraitViewport.Left + portraitViewport.Columns * portraitViewport.TileSize >= 1080 &&
       portraitViewport.Top <= 0 &&
       portraitViewport.Top + portraitViewport.Rows * portraitViewport.TileSize >= 2400 &&
       portraitViewport.GroundY > 1500 && portraitViewport.GroundY < 1900 &&
       LiveWallpaperMapViewport.TryCreate(
           2400, 1080, 128, 2, 0f, out var landscapeViewport) &&
       landscapeViewport.Left <= 0 &&
       landscapeViewport.TileSize == portraitViewport.TileSize &&
       landscapeViewport.CameraOriginX == landscapeViewport.OriginX &&
       landscapeViewport.Left + landscapeViewport.Columns * landscapeViewport.TileSize >= 2400 &&
       !LiveWallpaperMapViewport.TryCreate(0, 2400, 128, 1, 0.5f, out _),
       "Installed map viewports must cover portrait and landscape canvases without synthetic art.");
Assert(portraitViewport.TryMoveToAdjacentField(
           1, 0, 160, 128, out var followedRight) &&
       followedRight.OriginX == portraitViewport.OriginX + 10 &&
       followedRight.OriginY == portraitViewport.OriginY &&
       portraitViewport.TryMoveToAdjacentField(
           0, -1, 160, 128, out var followedUp) &&
       followedUp.OriginX == portraitViewport.OriginX &&
       followedUp.OriginY == portraitViewport.OriginY - 8 &&
       !portraitViewport.TryMoveToAdjacentField(
           1, 1, 160, 128, out _),
       "Wallpaper loading-zone following must move by the engine's exact 10x8-tile field size.");
var rightExitPixelX =
    (portraitViewport.OriginX + portraitViewport.Columns) * 16f + 8f;
var viewportCenterPixelY =
    (portraitViewport.OriginY + portraitViewport.Rows / 2f) * 16f;
Assert(portraitViewport.TryFollowLinkThroughExit(
           rightExitPixelX, viewportCenterPixelY, 160, 128,
           out var centeredFollowViewport) &&
       centeredFollowViewport.OriginX > portraitViewport.OriginX &&
       Math.Abs(
           rightExitPixelX / 16f -
           (centeredFollowViewport.OriginX + centeredFollowViewport.Columns / 2f)) <= 1f,
       "A followed loading-zone transition must visibly recenter the wallpaper crop on Link.");
var rightScrollThresholdPixelX =
    (portraitViewport.CameraOriginX + portraitViewport.Columns - 2f) * 16f;
var beforeRightScrollThresholdPixelX = rightScrollThresholdPixelX - 2f;
Assert(!portraitViewport.TryGetEdgeScrollTarget(
           rightScrollThresholdPixelX, viewportCenterPixelY,
           -1f, 0f, 160, 128, out _, out _) &&
       !portraitViewport.TryGetEdgeScrollTarget(
           beforeRightScrollThresholdPixelX, viewportCenterPixelY,
           1f, 0f, 160, 128, out _, out _) &&
       portraitViewport.TryGetEdgeScrollTarget(
           rightScrollThresholdPixelX, viewportCenterPixelY,
           1f, 0f, 160, 128, out var edgeTargetX, out var edgeTargetY) &&
       Math.Abs(edgeTargetX - portraitViewport.CameraOriginX - 10f) < 0.001f &&
       Math.Abs(edgeTargetY - portraitViewport.CameraOriginY) < 0.001f,
       "The wallpaper camera must begin scrolling one visible tile before Link reaches the phone edge while moving outward.");
var topScrollThresholdPixelY =
    (portraitViewport.CameraOriginY + 3f) * 16f;
var bottomScrollThresholdPixelY =
    (portraitViewport.CameraOriginY + portraitViewport.Rows - 3f) * 16f;
var viewportCenterPixelX =
    (portraitViewport.CameraOriginX + portraitViewport.Columns / 2f) * 16f;
Assert(!portraitViewport.TryGetEdgeScrollTarget(
           viewportCenterPixelX, topScrollThresholdPixelY + 2f,
           0f, -1f, 160, 128, out _, out _) &&
       portraitViewport.TryGetEdgeScrollTarget(
           viewportCenterPixelX, topScrollThresholdPixelY,
           0f, -1f, 160, 128, out var topTargetX, out var topTargetY) &&
       Math.Abs(topTargetX - portraitViewport.CameraOriginX) < 0.001f &&
       Math.Abs(topTargetY - portraitViewport.CameraOriginY + 8f) < 0.001f &&
       !portraitViewport.TryGetEdgeScrollTarget(
           viewportCenterPixelX, bottomScrollThresholdPixelY - 2f,
           0f, 1f, 160, 128, out _, out _) &&
       portraitViewport.TryGetEdgeScrollTarget(
           viewportCenterPixelX, bottomScrollThresholdPixelY,
           0f, 1f, 160, 128, out var bottomTargetX, out var bottomTargetY) &&
       Math.Abs(bottomTargetX - portraitViewport.CameraOriginX) < 0.001f &&
       Math.Abs(bottomTargetY - portraitViewport.CameraOriginY - 8f) < 0.001f,
       "The wallpaper camera must begin vertical transitions two visible tiles before the status and navigation bar edges.");
var halfTileCamera = portraitViewport.WithCameraOrigin(
    portraitViewport.CameraOriginX + 0.5f,
    portraitViewport.CameraOriginY, 160, 128);
Assert(halfTileCamera.OriginX == portraitViewport.OriginX &&
       Math.Abs(halfTileCamera.CameraOriginX -
                portraitViewport.CameraOriginX - 0.5f) < 0.001f &&
       Math.Abs(halfTileCamera.Left - portraitViewport.Left +
                portraitViewport.TileSize * 0.5f) < 0.001f,
       "Fractional camera origins must scroll map rendering smoothly between tile boundaries.");
Assert(LiveWallpaperPresets.TryResolve(1, out var mabePreset) &&
       mabePreset.Scene == 1 && mabePreset.TimeOfDay == 2 &&
       mabePreset.FeaturedCharacter == 4 && mabePreset.LinkActivity == 1 &&
       LiveWallpaperPresets.TryResolve(2, out var forestPreset) &&
       forestPreset.Scene == 3 && forestPreset.TimeOfDay == 3 &&
       LiveWallpaperPresets.TryResolve(3, out var journeyPreset) &&
       journeyPreset.Scene == 4 && journeyPreset.TimeOfDay == 0 &&
       journeyPreset.LinkActivity == 2 &&
       !LiveWallpaperPresets.TryResolve(0, out _) &&
       !LiveWallpaperPresets.TryResolve(99, out _),
       "Wallpaper presets must resolve only the documented Mabe, forest, and journey profiles.");

Assert(ArchipelagoItemMapper.TryMap("Progressive Sword", 0, 0, 0, out var sword1) &&
       sword1.GameItemName == "sword1", "First progressive sword mapping failed.");
Assert(ArchipelagoItemMapper.TryMap("Progressive Sword", 1, 0, 0, out var sword2) &&
       sword2.GameItemName == "sword2", "Second progressive sword mapping failed.");
Assert(ArchipelagoItemMapper.TryMap("Small Key (Catfish's Maw)", 0, 0, 0, out var key) &&
       key.GameItemName == "smallkey" && key.LocationBounding == "five", "Dungeon key mapping failed.");
Assert(ArchipelagoItemMapper.TryMap("500 Rupees", 0, 0, 0, out var rupees) &&
       rupees.GameItemName == "ruby" && rupees.Count == 500, "Rupee mapping failed.");
Assert(ArchipelagoItemMapper.TryMap("Rooster", 0, 0, 0, out var rooster) &&
       rooster.GameItemName == "rooster" && rooster.Effect == ArchipelagoItemEffect.Rooster,
       "Rooster mapping must apply its follower ownership state.");
Assert(ArchipelagoItemMapper.TryMap("Zol Attack", 0, 0, 0, out var zolAttack) &&
       zolAttack.Effect == ArchipelagoItemEffect.ZolAttack &&
       ArchipelagoManager.ZolAttackSpawnCount == 5,
       "Zol Attack must spawn the five enemies used by the official AP trap.");
Assert(ArchipelagoItemMapper.TryMap("Guardian Acorn", 0, 0, 0, out var guardianAcorn) &&
       guardianAcorn.Effect == ArchipelagoItemEffect.GuardianAcorn &&
       ArchipelagoItemMapper.TryMap("Piece Of Power", 0, 0, 0, out var pieceOfPower) &&
       pieceOfPower.Effect == ArchipelagoItemEffect.PieceOfPower,
       "Remote temporary powerups must activate their normal gameplay effects.");
Assert(ArchipelagoItemMapper.TryMap("Stick", 0, 0, 0, out var stick) &&
       stick.GameItemName == "trade4" && stick.Effect == ArchipelagoItemEffect.TradeStick,
       "Stick mapping must spawn Tarin at the honeycomb tree.");
Assert(ArchipelagoItemMapper.TryMap("Pineapple", 0, 0, 0, out var pineapple) &&
       pineapple.GameItemName == "trade6" && pineapple.Effect == ArchipelagoItemEffect.TradePineapple,
       "Pineapple mapping must spawn Papahl in Tal Tal Heights.");
Assert(ArchipelagoItemMapper.TryMap("Scale", 0, 0, 0, out var scale) &&
       scale.GameItemName == "trade12" && scale.Effect == ArchipelagoItemEffect.TradeScale,
       "Scale mapping must complete the mermaid departure state.");
Assert(ArchipelagoItemMapper.TryMap("Magnifying Glass", 0, 0, 0, out var lens) &&
       lens.GameItemName == "trade13" && lens.Effect == ArchipelagoItemEffect.TradeMagnifyingGlass,
       "Magnifying Glass mapping must complete the photographer state.");
Assert(!ArchipelagoItemMapper.TryMap("An Item From Another Game", 0, 0, 0, out _),
       "Unknown items must not silently map to a local item.");
Assert(ArchipelagoLocationKey.Script("marin:reward", 7) == "script:marin%3Areward:7",
       "Script location keys must be deterministic and escape separators.");
Assert(ArchipelagoLocationKey.Shop(980) == "shop:980", "Shop location key mapping failed.");
Assert(ArchipelagoLocationKey.Event("rooster:cave") == "event:rooster%3Acave",
       "Event location keys must be deterministic and escape separators.");
Assert(ArchipelagoLocationKey.PersistentCheck(1001) == "ap_location_1001",
       "Persistent check key mapping failed.");
Assert(MagpieTrackerProtocol.GetCheckId(new ArchipelagoSeedLocation
       {
           LocationId = 10000673,
           LocationName = "Shop 200 Item (Mabe Village)"
       }) == "0x2A1-0" &&
       MagpieTrackerProtocol.GetCheckId(new ArchipelagoSeedLocation
       {
           LocationId = 10001259,
           LocationName = "Spiked Beetle Owl (Tail Cave)"
       }) == "0x103-Owl" &&
       MagpieTrackerProtocol.GetCheckId(new ArchipelagoSeedLocation
       {
           LocationId = 10001672,
           LocationName = "Trendy Game (Mabe Village)"
       }) == "0x2A0-Trade",
       "Magpie check IDs must reverse AP's numeric encoding and preserve LADXR suffixes.");
var specialMagpieChecks = new Dictionary<long, string>
{
    [10010786] = "0x2A22",
    [10005010] = "0x1392",
    [10004626] = "0x1212",
    [10009186] = "0x23E2",
    [10009042] = "0x2352"
};
Assert(specialMagpieChecks.All(check =>
        MagpieTrackerProtocol.GetCheckId(new ArchipelagoSeedLocation
        {
            LocationId = check.Key,
            LocationName = "Special Magpie Check"
        }) == check.Value),
    "Magpie special check IDs must match its authoritative AP mapping.");
Assert(MagpieTrackerProtocol.TryGetItemContribution("Progressive Sword", out var magpieSword) &&
       magpieSword.Id == "SWORD" && magpieSword.Quantity == 1 && magpieSword.Maximum == 2 &&
       MagpieTrackerProtocol.TryGetItemContribution("Small Key (Color Dungeon)", out var magpieKey) &&
       magpieKey.Id == "KEY9" && magpieKey.Maximum == int.MaxValue &&
       MagpieTrackerProtocol.TryGetItemContribution("Heart Piece", out var magpieHeartPiece) &&
       magpieHeartPiece.Id == "HEART_PIECE" &&
       MagpieTrackerProtocol.TryGetItemContribution("Zol Attack", out var magpieTrap) &&
       magpieTrap.Id == "GEL" &&
       MagpieTrackerProtocol.TryGetItemContribution("Future AP Item", out var magpieFutureItem) &&
       magpieFutureItem.Id == "FUTURE_AP_ITEM",
       "Magpie inventory mapping must cover standard, dungeon, trap, and future AP items.");
var embeddedTrackerUri = MagpieTrackerProtocol.CreateEmbeddedTrackerUri();
Assert(embeddedTrackerUri.Scheme == Uri.UriSchemeHttps &&
       embeddedTrackerUri.Host == "magpietracker.us" &&
       embeddedTrackerUri.Query.Contains("enable_autotracking=true", StringComparison.Ordinal) &&
       embeddedTrackerUri.Query.Contains("setting_autotrackerAddress=127.0.0.1%3A17026", StringComparison.Ordinal) &&
       embeddedTrackerUri.Query.Contains("setting_autotrackSettings=true", StringComparison.Ordinal) &&
       embeddedTrackerUri.Query.Contains("setting_gps=true", StringComparison.Ordinal) &&
       embeddedTrackerUri.Query.Contains("flag_ap_logic=true", StringComparison.Ordinal),
       "Embedded Magpie URL must enable AP and GPS autotracking against the local bridge.");
Assert(MagpieTrackerProtocol.TryCreateEmbeddedTrackerDnsFallback(
           embeddedTrackerUri.AbsoluteUri, out var embeddedTrackerFallback) &&
       embeddedTrackerFallback.Scheme == Uri.UriSchemeHttps &&
       embeddedTrackerFallback.Host == "www.magpietracker.us" &&
       embeddedTrackerFallback.AbsolutePath == embeddedTrackerUri.AbsolutePath &&
       embeddedTrackerFallback.Query == embeddedTrackerUri.Query &&
       !MagpieTrackerProtocol.TryCreateEmbeddedTrackerDnsFallback(
           embeddedTrackerFallback.AbsoluteUri, out _) &&
       !MagpieTrackerProtocol.TryCreateEmbeddedTrackerDnsFallback(
           "http://magpietracker.us/?enable_autotracking=true", out _) &&
       !MagpieTrackerProtocol.TryCreateEmbeddedTrackerDnsFallback(
           "https://example.com/?enable_autotracking=true", out _),
       "Embedded Magpie DNS fallback must preserve settings and only replace the failed primary HTTPS host.");
Assert(MagpieTrackerProtocol.CalculateEmbeddedOverlayWidth(1920) == 1344 &&
       MagpieTrackerProtocol.CalculateEmbeddedOverlayWidth(1) == 1 &&
       MagpieTrackerProtocol.CalculateEmbeddedOverlayWidth(0) == 0,
       "Embedded Magpie must use a bounded right-side panel that leaves gameplay visible.");
Assert(MagpieTrackerLocationMapper.TryCreate(
           isOverworld: true, isDungeon: false, isInterior: false,
           dungeonName: null, mapName: "overworld.map", mapOffsetX: 0, mapOffsetY: 0,
           linkX: 5 * 160 + 3 * 16, linkY: 10 * 128 + 4 * 16,
           out var overworldLocation) &&
       overworldLocation.Room == "0xA5" && overworldLocation.X == 3 &&
       overworldLocation.Y == 4 && overworldLocation.DrawFine,
       "Magpie GPS must translate HD overworld fields and tiles to LADX room coordinates.");
Assert(MagpieTrackerLocationMapper.TryCreate(
           isOverworld: false, isDungeon: true, isInterior: false,
           dungeonName: "one", mapName: "dungeon1.map", mapOffsetX: 0, mapOffsetY: 0,
           linkX: 3 * 160 + 5 * 16, linkY: 5 * 128 + 6 * 16,
           out var dungeonLocation) &&
       dungeonLocation.Room == "0x117" && dungeonLocation.X == 5 &&
       dungeonLocation.Y == 6 && dungeonLocation.DrawFine,
       "Magpie GPS must map HD dungeon minimap coordinates to Magpie room IDs.");
Assert(MagpieTrackerLocationMapper.TryCreate(
           isOverworld: false, isDungeon: false, isInterior: true,
           dungeonName: null, mapName: "house0.map", mapOffsetX: 0, mapOffsetY: 0,
           linkX: 80, linkY: 64, out var interiorLocation) &&
       interiorLocation.Room == "0x2A3" && !interiorLocation.DrawFine,
       "Interior GPS without an exposed GBC room ID must follow the underworld map coarsely.");
Assert(MagpieTrackerProtocol.ShouldCloseEmbeddedTracker(
           trackerVisible: true, isKeyDown: true, repeatCount: 0, CButtons.B) &&
       MagpieTrackerProtocol.ShouldCloseEmbeddedTracker(
           trackerVisible: true, isKeyDown: true, repeatCount: 0, CButtons.Select) &&
       !MagpieTrackerProtocol.ShouldCloseEmbeddedTracker(
           trackerVisible: true, isKeyDown: true, repeatCount: 0, CButtons.A) &&
       !MagpieTrackerProtocol.ShouldCloseEmbeddedTracker(
           trackerVisible: false, isKeyDown: true, repeatCount: 0, CButtons.B) &&
       !MagpieTrackerProtocol.ShouldCloseEmbeddedTracker(
           trackerVisible: true, isKeyDown: false, repeatCount: 0, CButtons.B),
       "Controller B/Select must close only a visible embedded tracker on the initial key press.");
Assert(!new ProjectZ.UnavailableMagpieTrackerService().IsAvailable,
       "Non-Android platforms must not expose the embedded tracker pause command.");
var warpTarget = ArchipelagoGameMenuPolicy.WarpToStartTarget;
Assert(warpTarget.MapName == "house1.map" &&
       warpTarget.Position == new Microsoft.Xna.Framework.Vector2(70, 70) &&
       warpTarget.Direction == 3,
       "Warp to Start must target the initial house save point directly.");
Assert(ArchipelagoGameMenuPolicy.KeepPauseOpenForEmbeddedTracker,
       "Opening embedded Magpie must keep the game pause page active.");

using (var magpieHandshake = System.Text.Json.JsonDocument.Parse(
           MagpieTrackerProtocol.CreateHandshakeAcknowledgement()))
{
    Assert(magpieHandshake.RootElement.GetProperty("type").GetString() == "handshAck" &&
           magpieHandshake.RootElement.GetProperty("version").GetString() == "1.32" &&
           magpieHandshake.RootElement.GetProperty("name").GetString() == "archipelago-ladx-client",
           "Magpie handshake acknowledgement must identify the bridge as an AP client.");
}
var magpieLocation = new ArchipelagoSeedLocation
{
    LocationId = 10000673,
    LocationName = "Shop 200 Item (Mabe Village)",
    ItemName = "Boomerang"
};
var magpieSeed = new ArchipelagoSeedManifest
{
    FormatVersion = ArchipelagoSeedManifest.CurrentFormatVersion,
    Game = ArchipelagoManager.GameName,
    SeedName = "Magpie Smoke Seed",
    SlotName = "Link",
    WorldVersion = "0.1.0",
    MappingComplete = true,
    Options = new Dictionary<string, System.Text.Json.JsonElement>
    {
        ["logic"] = System.Text.Json.JsonSerializer.SerializeToElement(1),
        ["goal"] = System.Text.Json.JsonSerializer.SerializeToElement(1),
        ["instrument_count"] = System.Text.Json.JsonSerializer.SerializeToElement(8),
        ["gfxmod"] = System.Text.Json.JsonSerializer.SerializeToElement(0),
        ["shuffle_nightmare_keys"] = System.Text.Json.JsonSerializer.SerializeToElement(0),
        ["shuffle_instruments"] = System.Text.Json.JsonSerializer.SerializeToElement(100),
        ["rooster"] = System.Text.Json.JsonSerializer.SerializeToElement(1),
        ["experimental_dungeon_shuffle"] = System.Text.Json.JsonSerializer.SerializeToElement(0)
    },
    Locations = new List<ArchipelagoSeedLocation> { magpieLocation }
};
magpieSeed.Validate();
using (var magpieBridge = new MagpieTrackerBridge(0))
{
    magpieBridge.Configure(enabled: false, allowLan: false, seed: magpieSeed);
    magpieBridge.SynchronizeReceivedItems(new[]
    {
        "Progressive Sword", "Hookshot", "20 Rupees", "Future AP Item"
    });
    magpieBridge.SetLocation(overworldLocation);
    Assert(magpieBridge.BoundPort == 0,
        "A disabled Magpie profile must not start the listener before the tracker is opened.");
    Assert(magpieBridge.Start(allowLan: false),
        "Opening the embedded tracker must be able to start its local bridge on demand.");
    Assert(magpieBridge.BoundPort > 0, "Magpie bridge did not bind its loopback listener.");
    using var magpieSocket = new ClientWebSocket();
    await magpieSocket.ConnectAsync(
        new Uri($"ws://127.0.0.1:{magpieBridge.BoundPort}/"), CancellationToken.None);
    await SendWebSocketText(magpieSocket, "{\"type\":\"handshake\",\"features\":[\"items\",\"checks\",\"gps\"]}");
    using (var acknowledgement = System.Text.Json.JsonDocument.Parse(
               await ReceiveWebSocketText(magpieSocket)))
        Assert(acknowledgement.RootElement.GetProperty("type").GetString() == "handshAck",
            "Magpie bridge did not acknowledge a live WebSocket handshake.");
    using (var slotData = System.Text.Json.JsonDocument.Parse(await ReceiveWebSocketText(magpieSocket)))
    {
        var options = slotData.RootElement.GetProperty("slot_data");
        Assert(slotData.RootElement.GetProperty("source").GetString() == "archipelago" &&
               options.GetProperty("seed_name").GetString() == "Magpie Smoke Seed" &&
               options.GetProperty("client_version").GetString() == "0.6.7" &&
               options.GetProperty("logic").GetString() == "normal" &&
               options.GetProperty("goal").GetString() == "instruments" &&
               options.GetProperty("instrument_count").GetInt32() == 8 &&
               options.GetProperty("gfxmod").GetString() == string.Empty &&
               options.GetProperty("shuffle_nightmare_keys").GetString() == "original_dungeon" &&
               options.GetProperty("shuffle_instruments").GetString() == "vanilla" &&
               options.GetProperty("rooster").GetBoolean() &&
               !options.GetProperty("experimental_dungeon_shuffle").GetBoolean(),
            "Magpie bridge must translate numeric AP options into Magpie-compatible slot data.");
    }

    await SendWebSocketText(magpieSocket, "{\"type\":\"sendFull\"}");
    using (var fullItems = System.Text.Json.JsonDocument.Parse(await ReceiveWebSocketText(magpieSocket)))
    {
        var quantities = fullItems.RootElement.GetProperty("items").EnumerateArray()
            .ToDictionary(item => item.GetProperty("id").GetString(), item => item.GetProperty("qty").GetInt32());
        Assert(!fullItems.RootElement.GetProperty("diff").GetBoolean() &&
               fullItems.RootElement.GetProperty("source").GetString() == "archipelago" &&
               quantities["SWORD"] == 1 && quantities["HOOKSHOT"] == 1 &&
               quantities["RUPEES_20"] == 1 && quantities["FUTURE_AP_ITEM"] == 1,
            "Magpie full inventory must include AP items received before the tracker starts.");
    }
    using (var fullChecks = System.Text.Json.JsonDocument.Parse(await ReceiveWebSocketText(magpieSocket)))
        Assert(!fullChecks.RootElement.GetProperty("diff").GetBoolean() &&
               fullChecks.RootElement.GetProperty("source").GetString() == "archipelago",
            "Magpie full check response was incorrectly marked as a diff.");
    using (var fullLocation = System.Text.Json.JsonDocument.Parse(await ReceiveWebSocketText(magpieSocket)))
        Assert(fullLocation.RootElement.GetProperty("type").GetString() == "location" &&
               fullLocation.RootElement.GetProperty("source").GetString() == "archipelago" &&
               fullLocation.RootElement.GetProperty("room").GetString() == "0xA5" &&
               fullLocation.RootElement.GetProperty("drawFine").GetBoolean(),
            "Magpie sendFull did not replay the current GPS position.");

    magpieBridge.SetLocation(dungeonLocation);
    using (var locationDiff = System.Text.Json.JsonDocument.Parse(await ReceiveWebSocketText(magpieSocket)))
        Assert(locationDiff.RootElement.GetProperty("room").GetString() == "0x117" &&
               locationDiff.RootElement.GetProperty("x").GetDouble() == 5 &&
               locationDiff.RootElement.GetProperty("y").GetDouble() == 6,
            "Magpie bridge did not stream a changed GPS position.");

    magpieBridge.RecordReceivedItem(4, "Boomerang");
    using (var itemDiff = System.Text.Json.JsonDocument.Parse(await ReceiveWebSocketText(magpieSocket)))
        Assert(itemDiff.RootElement.GetProperty("items")[0].GetProperty("id").GetString() == "BOOMERANG" &&
               itemDiff.RootElement.GetProperty("items")[0].GetProperty("qty").GetInt32() == 1,
            "Magpie bridge did not stream an AP item receipt as a differential update.");

    magpieBridge.RecordCheck(magpieLocation);
    using (var checkDiff = System.Text.Json.JsonDocument.Parse(await ReceiveWebSocketText(magpieSocket)))
        Assert(checkDiff.RootElement.GetProperty("checks")[0].GetProperty("id").GetString() == "0x2A1-0" &&
               checkDiff.RootElement.GetProperty("checks")[0].GetProperty("checked").GetBoolean(),
            "Magpie bridge did not stream a completed location as a differential update.");
}
Assert(ArchipelagoManager.ClientVersion == new Version(0, 6, 7),
       "The client handshake must advertise Archipelago 0.6.7 compatibility.");
Assert(ArchipelagoManager.GetReconnectDelaySeconds(0) == 0 &&
       ArchipelagoManager.GetReconnectDelaySeconds(1) == 5 &&
       ArchipelagoManager.GetReconnectDelaySeconds(2) == 10 &&
       ArchipelagoManager.GetReconnectDelaySeconds(3) == 20 &&
       ArchipelagoManager.GetReconnectDelaySeconds(5) == 60 &&
       ArchipelagoManager.GetReconnectDelaySeconds(20) == 60,
       "Reconnect delays must back off from five seconds and remain capped at one minute.");
Assert(ArchipelagoManager.GetReceivedItemSaveKey(0) == "ap_received_item_0" &&
       ArchipelagoManager.GetReceivedItemSaveKey(42) == "ap_received_item_42",
       "Magpie received-item snapshots must use deterministic per-save keys.");
Assert(ArchipelagoManager.ShouldRecoverMagpieSession(
           active: true, autoConnect: true, sessionConnected: false) &&
       !ArchipelagoManager.ShouldRecoverMagpieSession(
           active: true, autoConnect: true, sessionConnected: true) &&
       !ArchipelagoManager.ShouldRecoverMagpieSession(
           active: true, autoConnect: false, sessionConnected: false),
       "Opening Magpie must recover only a missing auto-connect AP session.");


const string testRoomId = "AAAAAAAAAAAAAAAAAAAAAA";
var normalizedRoomUrl = ArchipelagoHostedRoomResolver.NormalizeRoomUrl(
    $"https://archipelago.gg/room/{testRoomId}/?copied=1");
Assert(normalizedRoomUrl == $"https://archipelago.gg/room/{testRoomId}",
    "Hosted room URLs must normalize to the stable official room page.");
var rejectedUntrustedRoomUrl = false;
try
{
    ArchipelagoHostedRoomResolver.NormalizeRoomUrl($"https://example.com/room/{testRoomId}");
}
catch (InvalidDataException)
{
    rejectedUntrustedRoomUrl = true;
}
Assert(rejectedUntrustedRoomUrl,
    "Hosted room recovery must not issue wake requests to arbitrary imported hosts.");
Assert(ArchipelagoHostedRoomResolver.ParseLastPort("{\"last_port\": 49152}") == 49152,
    "Hosted room status parsing did not preserve the assigned port.");

var roomHandler = new RoomResolverHandler(testRoomId, 0, 49321);
using (var roomHttpClient = new HttpClient(roomHandler))
{
    var roomResolver = new ArchipelagoHostedRoomResolver(
        roomHttpClient, TimeSpan.Zero, TimeSpan.Zero, TimeSpan.FromSeconds(1), 3);
    var resolvedRoomServer = await roomResolver.ResolveServerAsync(normalizedRoomUrl);
    Assert(resolvedRoomServer == "archipelago.gg:49321" &&
           roomHandler.RequestedUris.SequenceEqual(new[]
           {
               $"https://archipelago.gg/room/{testRoomId}",
               $"https://archipelago.gg/api/room_status/{testRoomId}",
               $"https://archipelago.gg/api/room_status/{testRoomId}"
           }),
        "Hosted room recovery must wake the stable room page and poll until a port is assigned.");
}
Assert(ArchipelagoManager.ClassifySocketFailure(new WebSocketException()) ==
           TelemetryDisconnectReason.Network &&
       ArchipelagoManager.ClassifySocketFailure(new System.Text.Json.JsonException()) ==
           TelemetryDisconnectReason.Protocol &&
       ArchipelagoManager.ClassifySocketFailure(new InvalidOperationException()) ==
           TelemetryDisconnectReason.Unknown,
       "Socket telemetry must distinguish transport, protocol, and unknown failures.");

var fakeSocket = new BlockingWebSocket();
var socketHelper = new BaseArchipelagoSocketHelper<BlockingWebSocket>(fakeSocket);
socketHelper.StartPolling();
await fakeSocket.ReceiveStarted.Task.WaitAsync(TimeSpan.FromSeconds(1));
var socketCleanupStarted = DateTime.UtcNow;
await socketHelper.DisconnectAsync();
Assert(fakeSocket.Aborted && fakeSocket.Disposed &&
       DateTime.UtcNow - socketCleanupStarted < TimeSpan.FromSeconds(1),
       "Disconnect must cancel idle socket workers and dispose the old WebSocket promptly.");
Assert(ArchipelagoManager.HasSaveBinding("Seed", "Link") &&
       !ArchipelagoManager.HasSaveBinding("", "Link") &&
       !ArchipelagoManager.HasSaveBinding("Seed", null),
       "Offline gameplay overrides must follow the persistent AP save binding.");
Assert(ArchipelagoManager.ShouldUseBoomerangGiftBehavior(true) &&
       !ArchipelagoManager.ShouldUseBoomerangGiftBehavior(false),
       "Boomerang Guy must use gift behavior only for an AP-bound save.");
Assert(!ArchipelagoManager.ShouldReplaceToadstoolWithPowder(true) &&
       ArchipelagoManager.ShouldReplaceToadstoolWithPowder(false),
       "An independent AP Magic Powder receipt must preserve the Toadstool for the Witch check.");
Assert(ArchipelagoManager.ShouldUseColorFairyMultiReward(
           archipelagoActive: true, hasRedLocation: true, hasBlueLocation: true) &&
       !ArchipelagoManager.ShouldUseColorFairyMultiReward(
           archipelagoActive: false, hasRedLocation: true, hasBlueLocation: true) &&
       !ArchipelagoManager.ShouldUseColorFairyMultiReward(
           archipelagoActive: true, hasRedLocation: true, hasBlueLocation: false),
       "The Color Fairy must grant both mapped AP checks only on a complete bound seed.");
Assert(ArchipelagoManager.GetNextTunic(GameManager.CloakGreen, true, true) == GameManager.CloakBlue &&
       ArchipelagoManager.GetNextTunic(GameManager.CloakGreen, false, true) == GameManager.CloakRed &&
       ArchipelagoManager.GetNextTunic(GameManager.CloakBlue, true, true) == GameManager.CloakRed &&
       ArchipelagoManager.GetNextTunic(GameManager.CloakBlue, true, false) == GameManager.CloakGreen &&
       ArchipelagoManager.GetNextTunic(GameManager.CloakRed, true, true) == GameManager.CloakGreen,
       "Telephone booths must cycle only through the green and owned randomized tunics.");
Assert(ArchipelagoManager.ShouldUseGhostHouseShellPot(
           true, "hauntedhouse.map", 128, 96) &&
       !ArchipelagoManager.ShouldUseGhostHouseShellPot(
           false, "hauntedhouse.map", 128, 96) &&
       !ArchipelagoManager.ShouldUseGhostHouseShellPot(
           true, "hauntedhouse.map", 112, 96) &&
       !ArchipelagoManager.ShouldUseGhostHouseShellPot(
           true, "overworld.map", 128, 96),
       "The Ghost House barrel must contain its mapped shell on bound AP saves only.");
Assert(ArchipelagoManager.ResolveArchipelagoShopItemState(
           true, true, false, true, false, 2) == 0 &&
       ArchipelagoManager.ResolveArchipelagoShopItemState(
           true, true, true, true, false, 2) == 1 &&
       ArchipelagoManager.ResolveArchipelagoShopItemState(
           true, true, true, true, true, 0) == 2 &&
       ArchipelagoManager.ResolveArchipelagoShopItemState(
           false, true, true, true, false, 2) == 2 &&
       ArchipelagoManager.ResolveArchipelagoShopItemState(
           true, false, false, false, false, 1) == 1,
       "The shop display must follow AP check completion instead of owned Bow or Shovel state.");
Assert(!ArchipelagoManager.IsShopPurchaseAtCapacity(
           randomizedLocationPending: true, ownedCount: 1, maxCount: 1) &&
       ArchipelagoManager.IsShopPurchaseAtCapacity(
           randomizedLocationPending: false, ownedCount: 1, maxCount: 1) &&
       !ArchipelagoManager.IsShopPurchaseAtCapacity(
           randomizedLocationPending: false, ownedCount: 0, maxCount: 1),
       "A pending randomized shop check must remain purchasable when the vanilla item is already owned.");
Assert(ArchipelagoManager.ShouldRepairToadstoolReceipt(false, false) &&
       !ArchipelagoManager.ShouldRepairToadstoolReceipt(false, true) &&
       !ArchipelagoManager.ShouldRepairToadstoolReceipt(true, false),
       "A replayed Toadstool must be restored only while the Witch check is still pending.");
Assert(ArchipelagoManager.ShouldDismissMarinFollower(true, false, "3") &&
       !ArchipelagoManager.ShouldDismissMarinFollower(false, false, "3") &&
       !ArchipelagoManager.ShouldDismissMarinFollower(true, true, "3") &&
       !ArchipelagoManager.ShouldDismissMarinFollower(true, false, "8"),
       "The removed-Walrus repair must dismiss only the completed AP beach escort.");
Assert(ArchipelagoManager.ShouldTreatMarinSongAsUnlearned(
           true, "maria", "ocarina_maria", true, false) &&
       !ArchipelagoManager.ShouldTreatMarinSongAsUnlearned(
           false, "maria", "ocarina_maria", true, false) &&
       !ArchipelagoManager.ShouldTreatMarinSongAsUnlearned(
           true, "maria", "ocarina_maria", true, true) &&
       !ArchipelagoManager.ShouldTreatMarinSongAsUnlearned(
           true, "maria", "ocarina_maria", false, false) &&
       !ArchipelagoManager.ShouldTreatMarinSongAsUnlearned(
           true, "maria_song_repeat", "ocarina_maria", true, false) &&
       !ArchipelagoManager.ShouldTreatMarinSongAsUnlearned(
           true, "maria", "ocarina_manbo", true, false),
       "A received Ballad must not bypass Marin's independent pending AP teaching check.");
Assert(ArchipelagoManager.ShouldRepairBoomerangReceipt("0", "0", false) &&
       ArchipelagoManager.ShouldRepairBoomerangReceipt("1", "0", true) &&
       ArchipelagoManager.ShouldRepairBoomerangReceipt("1", "1", false) &&
       !ArchipelagoManager.ShouldRepairBoomerangReceipt("1", "1", true),
       "AP replay must recover a received boomerang missing from save state or inventory.");
Assert(ArchipelagoManager.ShouldRestoreBoomerangTradeItem("shovel", false) &&
       ArchipelagoManager.ShouldRestoreBoomerangTradeItem("hookshot", false) &&
       !ArchipelagoManager.ShouldRestoreBoomerangTradeItem("hookshot", true) &&
       !ArchipelagoManager.ShouldRestoreBoomerangTradeItem("sword1", false),
       "Old AP saves must restore only equipment removed by the vanilla boomerang trade.");
Assert(ArchipelagoManager.IsTrendyGamePrize("trade0Collected") &&
       !ArchipelagoManager.IsTrendyGamePrize("pieceOfHeartCollected"),
       "The randomized Trendy prize must be recognized by its stable source key.");
Assert(ArchipelagoManager.ShouldRepairTrendyPrize(true, "1", "0") &&
       !ArchipelagoManager.ShouldRepairTrendyPrize(true, "1", "1") &&
       !ArchipelagoManager.ShouldRepairTrendyPrize(true, "0", "0") &&
       !ArchipelagoManager.ShouldRepairTrendyPrize(false, "1", "0"),
       "Only an AP Trendy prize hidden before its persistent check should be respawned.");
Assert(ArchipelagoManager.ShouldEnableMoblinCave(true, "0") &&
       !ArchipelagoManager.ShouldEnableMoblinCave(true, "1") &&
       !ArchipelagoManager.ShouldEnableMoblinCave(false, "0"),
       "AP must enable the Moblin Cave encounter before Tail Cave without respawning its boss.");
Assert(ArchipelagoManager.ShouldPreserveRoosterAfterDungeonSeven(true, "instrument6", "rooster") &&
       ArchipelagoManager.ShouldPreserveRoosterAfterDungeonSeven(true, "instrument6", "has_rooster") &&
       !ArchipelagoManager.ShouldPreserveRoosterAfterDungeonSeven(false, "instrument6", "rooster") &&
       !ArchipelagoManager.ShouldPreserveRoosterAfterDungeonSeven(true, "instrument5", "rooster"),
       "Dungeon 7 must not remove an AP-delivered rooster or its follower state.");
Assert(ArchipelagoManager.ShouldSuppressGhostAfterDungeonFour(true, "instrument3", "spawn_ghost") &&
       !ArchipelagoManager.ShouldSuppressGhostAfterDungeonFour(false, "instrument3", "spawn_ghost") &&
       !ArchipelagoManager.ShouldSuppressGhostAfterDungeonFour(true, "instrument4", "spawn_ghost"),
       "The randomized Dungeon 4 reward must not start the vanilla ghost follower quest.");
Assert(ArchipelagoManager.ShouldRepairGhostFollowerState("1", "0", false) &&
       ArchipelagoManager.ShouldRepairGhostFollowerState("0", "1", false) &&
       ArchipelagoManager.ShouldRepairGhostFollowerState("0", "0", true) &&
       !ArchipelagoManager.ShouldRepairGhostFollowerState("0", "0", false),
       "Older AP saves must discard a ghost follower spawned before the randomizer fix.");
var owlType = typeof(GameManager).Assembly.GetType("ProjectZ.InGame.GameObjects.NPCs.ObjOwl");
var owlInventoryPolicy = owlType?.GetMethod(
    "ShouldDisableInventory", BindingFlags.Static | BindingFlags.NonPublic);
Assert(owlInventoryPolicy != null &&
       (bool)owlInventoryPolicy.Invoke(null, new object[] { "enter" }) &&
       (bool)owlInventoryPolicy.Invoke(null, new object[] { "talk" }) &&
       !(bool)owlInventoryPolicy.Invoke(null, new object[] { "leave" }) &&
       !(bool)owlInventoryPolicy.Invoke(null, new object[] { "wait" }),
       "Owl encounters must use a transient inventory lock only while entering and talking.");
Assert(ArchipelagoManager.ShouldIgnoreBowWowForDialog(true, "npc09", "bowWow") &&
       ArchipelagoManager.ShouldIgnoreBowWowForDialog(true, "npc09", "has_bowWow") &&
       ArchipelagoManager.ShouldIgnoreBowWowForDialog(true, "castle_monkey", "has_bowWow") &&
       ArchipelagoManager.ShouldIgnoreBowWowForDialog(true, "npc_frog_boy", "has_bowWow") &&
       !ArchipelagoManager.ShouldIgnoreBowWowForDialog(false, "npc09", "bowWow") &&
       !ArchipelagoManager.ShouldIgnoreBowWowForDialog(true, "photo_mouse_house", "has_bowWow"),
       "AP BowWow must not be returned or block the Kiki and Richard sequences.");
Assert(ArchipelagoManager.ShouldAllowSecretBookWithoutLens(true, "book8", "trade13") &&
       !ArchipelagoManager.ShouldAllowSecretBookWithoutLens(false, "book8", "trade13") &&
       !ArchipelagoManager.ShouldAllowSecretBookWithoutLens(true, "book7", "trade13"),
       "The AP egg-maze book must not require the trade quest's Magnifying Glass.");
Assert(ArchipelagoManager.ShouldSuppressBombDrop(true, false, "bomb_1") &&
       !ArchipelagoManager.ShouldSuppressBombDrop(true, true, "bomb_1") &&
       !ArchipelagoManager.ShouldSuppressBombDrop(false, false, "bomb_1") &&
       !ArchipelagoManager.ShouldSuppressBombDrop(true, false, "heart"),
       "Enemy drops must not grant Bombs before AP delivers the Bomb item.");
Assert(ArchipelagoManager.ReconcileProgressiveCount(1, 0, 0) == 1 &&
       ArchipelagoManager.ReconcileProgressiveCount(0, 2, 0) == 2 &&
       ArchipelagoManager.ReconcileProgressiveCount(0, 0, 1) == 1,
       "Progressive tiers must follow monotonic AP receipt history, not removable inventory alone.");
Assert(ArchipelagoManager.GetUpgradeAmmoCount(ArchipelagoItemEffect.MaxPowderUpgrade) == 40 &&
       ArchipelagoManager.GetUpgradeAmmoCount(ArchipelagoItemEffect.MaxBombsUpgrade) == 60 &&
       ArchipelagoManager.GetUpgradeAmmoCount(ArchipelagoItemEffect.MaxArrowsUpgrade) == 60,
       "Capacity upgrades must refill to the official AP powder, bomb, and arrow limits.");
Assert(ArchipelagoItemMapper.TryMap("Max Powder Upgrade", 0, 0, 0, out var maxPowder) &&
       maxPowder.Effect == ArchipelagoItemEffect.MaxPowderUpgrade &&
       ArchipelagoItemMapper.TryMap("Max Bombs Upgrade", 0, 0, 0, out var maxBombs) &&
       maxBombs.Effect == ArchipelagoItemEffect.MaxBombsUpgrade &&
       ArchipelagoItemMapper.TryMap("Max Arrows Upgrade", 0, 0, 0, out var maxArrows) &&
       maxArrows.Effect == ArchipelagoItemEffect.MaxArrowsUpgrade,
       "Capacity upgrade replay must retain distinct refill effects.");
Assert(ArchipelagoManager.IsSeashellMansionComplete(true, "0", "1") &&
       !ArchipelagoManager.IsSeashellMansionComplete(true, "1", "0") &&
       ArchipelagoManager.IsSeashellMansionComplete(false, "1", "0"),
       "The AP Seashell Mansion sequence must follow its checked source location.");
var recoverApSeashellPresents = ArchipelagoManager.ShouldRecoverSeashellMansionPresents(
    boundSave: true, unmissables: false, saveFileVersion: 6);
Assert(recoverApSeashellPresents &&
       ArchipelagoManager.ShouldRecoverSeashellMansionPresents(
           boundSave: false, unmissables: true, saveFileVersion: 6) &&
       !ArchipelagoManager.ShouldRecoverSeashellMansionPresents(
           boundSave: false, unmissables: false, saveFileVersion: 6) &&
       !ArchipelagoManager.ShouldRecoverSeashellMansionPresents(
           boundSave: true, unmissables: false, saveFileVersion: 0) &&
       ArchipelagoManager.ShouldSpawnSeashellMansionPresent(
           recoverApSeashellPresents, shellCount: 10, collectedPresentCount: 0) &&
       ArchipelagoManager.ShouldSpawnSeashellMansionPresent(
           recoverApSeashellPresents, shellCount: 11, collectedPresentCount: 1) &&
       !ArchipelagoManager.ShouldSpawnSeashellMansionPresent(
           recoverApSeashellPresents, shellCount: 12, collectedPresentCount: 2) &&
       !ArchipelagoManager.ShouldSpawnSeashellMansionPresent(
           recoverMissedPresents: false, shellCount: 11, collectedPresentCount: 1) &&
       ArchipelagoManager.ShouldKeepSeashellMansionActive(
           mansionComplete: true, recoverMissedPresents: recoverApSeashellPresents, shellCount: 20, collectedPresentCount: 1) &&
       !ArchipelagoManager.ShouldKeepSeashellMansionActive(
           mansionComplete: true, recoverMissedPresents: recoverApSeashellPresents, shellCount: 20, collectedPresentCount: 2) &&
       ArchipelagoManager.ShouldKeepSeashellMansionActive(
           mansionComplete: false, recoverMissedPresents: recoverApSeashellPresents, shellCount: 20, collectedPresentCount: 2),
       "AP saves must deliver both missed Seashell Mansion presents in threshold order.");
Assert(ArchipelagoManager.ShouldSetLevelTwoSwordFlag(2, "0") &&
       !ArchipelagoManager.ShouldSetLevelTwoSwordFlag(1, "0") &&
       !ArchipelagoManager.ShouldSetLevelTwoSwordFlag(2, "1"),
       "A remotely received level-two sword must retain its native ownership state.");
Assert(ArchipelagoManager.ShouldRepairRoosterReceipt("0", "0", false) &&
       ArchipelagoManager.ShouldRepairRoosterReceipt("1", "0", true) &&
       ArchipelagoManager.ShouldRepairRoosterReceipt("1", "1", false) &&
       !ArchipelagoManager.ShouldRepairRoosterReceipt("1", "1", true),
       "Replayed AP history must restore a rooster lost by an older save.");
Assert(ArchipelagoManager.ShouldCompleteRoosterLocationWithoutResurrection(
           true, true, false, true) &&
       !ArchipelagoManager.ShouldCompleteRoosterLocationWithoutResurrection(
           false, true, false, true) &&
       !ArchipelagoManager.ShouldCompleteRoosterLocationWithoutResurrection(
           true, false, false, true) &&
       !ArchipelagoManager.ShouldCompleteRoosterLocationWithoutResurrection(
           true, true, true, true) &&
       !ArchipelagoManager.ShouldCompleteRoosterLocationWithoutResurrection(
           true, true, false, false),
       "An already-owned AP rooster must complete the grave check without a duplicate revival.");
Assert(GameManager.EquipmentSlots == 16,
       "The expanded inventory must retain every independently randomized equipment item.");
Assert(CheatSystem.IsIndependentGiveAllItem("boomerang") &&
       !CheatSystem.IsIndependentGiveAllItem("rooster"),
       "Give All Items must include the boomerang without treating follower items as equipment.");
const int inventoryMapX = 118;
const int inventoryMapY = 58;
const int inventoryMapWidth = 144;
const int inventoryMapHeight = 144;
static bool RectanglesIntersect(
    int firstX, int firstY, int firstWidth, int firstHeight,
    int secondX, int secondY, int secondWidth, int secondHeight) =>
    firstX < secondX + secondWidth && firstX + firstWidth > secondX &&
    firstY < secondY + secondHeight && firstY + firstHeight > secondY;
foreach (var sixButtons in new[] { false, true })
{
    var layout = InventoryOverlayLayout.GetEquipmentLayout(sixButtons, GameManager.EquipmentSlots);
    var storageSlots = GameManager.EquipmentSlots - (sixButtons ? 6 : 4);

    Assert(layout.Columns * layout.Rows >= storageSlots,
           "The compact inventory layout must retain every expanded storage slot.");
    Assert(layout.CellWidth >= 16 && layout.CellHeight >= 16,
           "Expanded inventory cells must remain large enough for item sprites.");
    Assert(!RectanglesIntersect(
               layout.X,
               layout.Y + InventoryOverlayLayout.ContentOffsetY,
               layout.Width,
               layout.Height,
               inventoryMapX,
               inventoryMapY,
               inventoryMapWidth,
               inventoryMapHeight),
           "Expanded inventory storage must not be covered by the minimap.");
}
Assert(!RectanglesIntersect(
           InventoryOverlayLayout.RoosterX,
           InventoryOverlayLayout.RoosterY,
           InventoryOverlayLayout.RoosterWidth,
           InventoryOverlayLayout.RoosterHeight,
           inventoryMapX,
           inventoryMapY,
           inventoryMapWidth,
           inventoryMapHeight),
       "The Rooster ownership indicator must remain visible outside the minimap.");
Assert(ArchipelagoManager.ShouldOverrideRaccoonSpawnCondition(
           true, "tarin_state", "1", "raccoon", "0", "0") &&
       ArchipelagoManager.ShouldOverrideRaccoonSpawnCondition(
           true, "tarin_state", "1", "raccoon", "4", "0") &&
       ArchipelagoManager.ShouldOverrideRaccoonSpawnCondition(
           true, "tarin_state", "1", "raccoon", "5", "0"),
       "An active AP save must spawn Raccoon Tarin before his cure, including out-of-order trade states.");
Assert(!ArchipelagoManager.ShouldOverrideRaccoonSpawnCondition(
           false, "tarin_state", "1", "raccoon", "0", "0") &&
       !ArchipelagoManager.ShouldOverrideRaccoonSpawnCondition(
           true, "tarin_state", "1", "raccoon", "0", "1") &&
       !ArchipelagoManager.ShouldOverrideRaccoonSpawnCondition(
           true, "tarin_state", "1", "raccoon", "2", "0"),
       "The Raccoon Tarin override must not affect vanilla or cured states.");

var seedPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.apladxhd");
try
{
    File.WriteAllText(seedPath, """
    {
      "format_version": 1,
      "game": "Links Awakening DX HD",
      "seed_name": "Smoke Test Seed",
      "slot_name": "Link",
      "world_version": "0.1.0",
      "mapping_complete": true,
      "unmapped_locations": [],
      "locations": [
        {
          "game_key": "item:hookshot_collected",
          "location_id": 1001,
          "location_name": "Master Stalfos Item (Catfish's Maw)",
          "item_name": "Hookshot",
          "item_game": "Links Awakening DX HD",
          "item_player": 1,
          "local_player": 1,
          "classification": 1
        }
      ],
      "options": {}
    }
    """);

    var seed = ArchipelagoSeedManifest.Load(seedPath);
    Assert(seed.LocationsByGameKey.ContainsKey("item:hookshot_collected"), "Seed lookup was not built.");
    Assert(seed.LocationsByGameKey["item:hookshot_collected"].LocationId == 1001,
           "Seed location id was not preserved.");
}
finally
{
    if (File.Exists(seedPath))
        File.Delete(seedPath);
}

var profileRoot = Path.Combine(Path.GetTempPath(), $"ladxhd-ap-profiles-{Guid.NewGuid():N}");
try
{
    var save1Directory = ArchipelagoConnectionSettings.GetProfileDirectory(profileRoot, 0);
    var save4Directory = ArchipelagoConnectionSettings.GetProfileDirectory(profileRoot, 3);
    Directory.CreateDirectory(save1Directory);
    Directory.CreateDirectory(save4Directory);

    File.WriteAllText(ArchipelagoConnectionSettings.GetProfilePath(profileRoot, 0), """
    {
      "enabled": true,
      "server": "seed-one.example:38281",
      "slot": "LinkOne",
      "seed_file": "seed.apladxhd",
      "save_slot": 0
    }
    """);
    File.WriteAllText(ArchipelagoConnectionSettings.GetProfilePath(profileRoot, 3), """
    {
      "enabled": true,
      "server": "seed-four.example:48281",
      "slot": "LinkFour",
      "seed_file": "four.apladxhd",
      "save_slot": 3,
      "room_url": "https://archipelago.gg/room/AAAAAAAAAAAAAAAAAAAAAA",
      "magpie_tracker_enabled": true,
      "magpie_tracker_allow_lan": true
    }
    """);

    var save1 = ArchipelagoConnectionSettings.LoadProfile(profileRoot, 0);
    var save4 = ArchipelagoConnectionSettings.LoadProfile(profileRoot, 3);
    Assert(save1.Server == "seed-one.example:38281" && save1.SaveSlot == 0,
        "Save 1 profile did not load independently.");
    Assert(save4.Server == "seed-four.example:48281" && save4.SaveSlot == 3 &&
           save4.RoomUrl == "https://archipelago.gg/room/AAAAAAAAAAAAAAAAAAAAAA",
        "Save 4 profile did not load independently.");
    Assert(!save1.MagpieTrackerEnabled && !save1.MagpieTrackerAllowLan &&
           save4.MagpieTrackerEnabled && save4.MagpieTrackerAllowLan,
        "Magpie settings must remain profile-specific and default to disabled.");
    Assert(save1.ResolveProfileSeedPath(profileRoot, 0) ==
           Path.GetFullPath(Path.Combine(save1Directory, "seed.apladxhd")),
        "Save 1 relative seed path did not resolve inside its profile.");
    Assert(save4.ResolveProfileSeedPath(profileRoot, 3) ==
           Path.GetFullPath(Path.Combine(save4Directory, "four.apladxhd")),
        "Save 4 relative seed path did not resolve inside its profile.");
    save4.Server = "archipelago.gg:49321";
    save4.SaveCurrentProfile(profileRoot);
    var reloadedSave4 = ArchipelagoConnectionSettings.LoadProfile(profileRoot, 3);
    Assert(reloadedSave4.Server == "archipelago.gg:49321" &&
           reloadedSave4.RoomUrl == save4.RoomUrl && reloadedSave4.Slot == save4.Slot,
        "Persisting a changed hosted-room port must retain the rest of that save profile.");

    File.WriteAllText(Path.Combine(save1Directory, "seed.apladxhd"), "seed one");
    File.WriteAllText(Path.Combine(save4Directory, "four.apladxhd"), "seed four");
    Assert(ArchipelagoProfileCatalog.LoadInstalled(profileRoot).Count == 0,
        "The manual setup catalog must hide profiles whose seed identity cannot be validated.");
    File.WriteAllText(Path.Combine(save4Directory, "four.apladxhd"), """
    {
      "format_version": 1,
      "game": "Links Awakening DX HD",
      "seed_name": "Catalog Seed",
      "slot_name": "LinkFour",
      "world_version": "0.1.0",
      "mapping_complete": true,
      "unmapped_locations": [],
      "locations": [],
      "options": {}
    }
    """);
    var installedProfiles = ArchipelagoProfileCatalog.LoadInstalled(profileRoot);
    Assert(installedProfiles.Count == 1 &&
           installedProfiles[0].SaveSlot == 3 &&
           installedProfiles[0].SeedName == "Catalog Seed" &&
           installedProfiles[0].SlotName == "LinkFour" &&
           installedProfiles[0].Server == "archipelago.gg:49321",
        "The manual setup catalog did not expose the verified installed profile.");
    Assert(ArchipelagoConnectionSettings.DeleteProfile(profileRoot, 0),
        "Deleting Save 1's Archipelago profile failed.");
    Assert(!Directory.Exists(save1Directory),
        "Deleting Save 1 left its Archipelago profile data behind.");
    Assert(Directory.Exists(save4Directory),
        "Deleting Save 1 removed another save's Archipelago profile.");
    Assert(ArchipelagoConnectionSettings.DeleteProfile(profileRoot, 0),
        "Deleting an already absent Archipelago profile should be idempotent.");

    var rejectedInvalidSlot = false;
    try
    {
        ArchipelagoConnectionSettings.GetProfileDirectory(profileRoot, 4);
    }
    catch (ArgumentOutOfRangeException)
    {
        rejectedInvalidSlot = true;
    }
    Assert(rejectedInvalidSlot, "A fifth save profile must be rejected.");
}
finally
{
    if (Directory.Exists(profileRoot))
        Directory.Delete(profileRoot, true);
}

var telemetryRoot = Path.Combine(Path.GetTempPath(), $"ladxhd-telemetry-{Guid.NewGuid():N}");
try
{
    var handler = new CapturingHandler();
    using var telemetry = new TelemetryClient(new TelemetryClientOptions
    {
        Endpoint = new Uri("https://telemetry.example/v1/events"),
        StorageRoot = telemetryRoot,
        AppVersion = "2.0.7-ap1",
        Platform = "android",
        DiagnosticsEnabled = true,
        RandomizerEnabled = true,
        HttpClient = new HttpClient(handler),
        FlushInterval = TimeSpan.FromHours(1),
    });

    Exception diagnosticException;
    try
    {
        _ = new TelemetryClient(null);
        throw new InvalidOperationException("TelemetryClient unexpectedly accepted null options.");
    }
    catch (ArgumentNullException exception)
    {
        diagnosticException = exception;
    }
    telemetry.RecordCrash(diagnosticException, TelemetryGameState.Gameplay, fatal: true);
    telemetry.RecordConnectFailure(2, 3500, TelemetryConnectionError.Network);
    Assert(telemetry.PendingCount == 2 && telemetry.HasPendingCrash,
        "Telemetry events were not durably queued.");

    await telemetry.FlushAsync();
    Assert(handler.Body != null, "Telemetry flush did not send a request.");
    Assert(!handler.Body.Contains("options", StringComparison.OrdinalIgnoreCase) &&
           !handler.Body.Contains("Program.cs", StringComparison.OrdinalIgnoreCase),
        "Crash telemetry leaked exception messages, argument names, or paths.");
    Assert(handler.Body.Contains("stack_hash", StringComparison.Ordinal) &&
           handler.Body.Contains("build_id", StringComparison.Ordinal) &&
           handler.Body.Contains("frames", StringComparison.Ordinal) &&
           handler.Body.Contains("ProjectZ.Core", StringComparison.Ordinal) &&
           handler.Body.Contains("System.ArgumentNullException", StringComparison.Ordinal),
        "Sanitized crash diagnostics were not uploaded.");
    Assert(telemetry.PendingCount == 0, "Accepted telemetry was not removed from the queue.");

    telemetry.RecordRandomizerManifest("private-seed-name", "normal", false, null, null);
    await telemetry.FlushAsync();
    Assert(handler.Body.Contains("randomizer_manifest", StringComparison.Ordinal) &&
           !handler.Body.Contains("private-seed-name", StringComparison.Ordinal),
        "Manifest telemetry leaked a non-version seed value.");

    telemetry.SetConsent(diagnosticsEnabled: false, randomizerEnabled: true);
    telemetry.RecordCrash(new Exception("must remain local"), TelemetryGameState.Unknown, fatal: false);
    telemetry.RecordConnectAttempt(1);
    Assert(telemetry.PendingCount == 1, "Disabled diagnostic telemetry was queued.");
    telemetry.SetConsent(diagnosticsEnabled: false, randomizerEnabled: false);
    Assert(telemetry.PendingCount == 0, "Consent withdrawal did not purge the local queue.");
}
finally
{
    if (Directory.Exists(telemetryRoot))
        Directory.Delete(telemetryRoot, true);
}

if (args.Length > 0)
{
    var generatedSeed = ArchipelagoSeedManifest.Load(args[0]);
    Assert(generatedSeed.Locations.Count > 200, "Generated APWorld manifest has too few locations.");
    Assert(generatedSeed.Game == ArchipelagoManager.GameName, "Generated APWorld manifest has the wrong game.");
}

var sourceArchive = Environment.GetEnvironmentVariable("LADXHD_V100_ZIP");
var bootstrapRoot = Environment.GetEnvironmentVariable("LADXHD_ANDROID_BOOTSTRAP");
if (!string.IsNullOrWhiteSpace(sourceArchive) && !string.IsNullOrWhiteSpace(bootstrapRoot))
{
    var migrationRoot = Path.Combine(Path.GetTempPath(), $"ladxhd-assets-{Guid.NewGuid():N}");
    try
    {
        var result = GameAssetMigrator.Migrate(sourceArchive, migrationRoot, bootstrapRoot);
        Assert(result.SourceArchiveSha256 == GameAssetMigrator.ExpectedSourceArchiveSha256,
            "Asset migration accepted the wrong source archive.");
        Assert(result.FileCount > 500, "Asset migration generated too few files.");
        Console.WriteLine($"Migrated assets: sha256={result.TreeSha256} files={result.FileCount} bytes={result.TotalBytes}");
    }
    finally
    {
        if (Directory.Exists(migrationRoot))
            Directory.Delete(migrationRoot, recursive: true);
    }
}

Console.WriteLine("Archipelago smoke tests passed.");

sealed class CapturingHandler : HttpMessageHandler
{
    public string Body { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Method != HttpMethod.Post)
            throw new InvalidOperationException("Telemetry must use POST.");
        if (request.RequestUri != new Uri("https://telemetry.example/v1/events"))
            throw new InvalidOperationException("Telemetry used an unexpected endpoint.");
        Body = await request.Content.ReadAsStringAsync(cancellationToken);
        return new HttpResponseMessage(HttpStatusCode.Accepted)
        {
            Content = JsonContent.Create(new { accepted = 2 }),
        };
    }
}

sealed class RoomResolverHandler : HttpMessageHandler
{
    private readonly string _roomId;
    private readonly Queue<int> _ports;

    public RoomResolverHandler(string roomId, params int[] ports)
    {
        _roomId = roomId;
        _ports = new Queue<int>(ports);
    }

    public List<string> RequestedUris { get; } = new();

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        RequestedUris.Add(request.RequestUri.AbsoluteUri);
        if (request.Method != HttpMethod.Get)
            throw new InvalidOperationException("Hosted room recovery must use GET.");

        if (request.RequestUri == new Uri($"https://archipelago.gg/room/{_roomId}"))
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        if (request.RequestUri != new Uri($"https://archipelago.gg/api/room_status/{_roomId}"))
            throw new InvalidOperationException("Hosted room recovery used an unexpected endpoint.");
        if (_ports.Count == 0)
            throw new InvalidOperationException("Hosted room recovery polled too many times.");

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new { last_port = _ports.Dequeue() })
        });
    }
}

sealed class BlockingWebSocket : WebSocket
{
    private WebSocketState _state = WebSocketState.Open;

    public TaskCompletionSource<bool> ReceiveStarted { get; } =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    public bool Aborted { get; private set; }
    public bool Disposed { get; private set; }
    public override WebSocketCloseStatus? CloseStatus => null;
    public override string CloseStatusDescription => null;
    public override WebSocketState State => _state;
    public override string SubProtocol => null;

    public override void Abort()
    {
        Aborted = true;
        _state = WebSocketState.Aborted;
    }

    public override Task CloseAsync(
        WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken)
    {
        _state = WebSocketState.Closed;
        return Task.CompletedTask;
    }

    public override Task CloseOutputAsync(
        WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken)
    {
        _state = WebSocketState.CloseSent;
        return Task.CompletedTask;
    }

    public override void Dispose()
    {
        Disposed = true;
        _state = WebSocketState.Closed;
    }

    public override async Task<WebSocketReceiveResult> ReceiveAsync(
        ArraySegment<byte> buffer, CancellationToken cancellationToken)
    {
        ReceiveStarted.TrySetResult(true);
        await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        throw new InvalidOperationException("The blocking receive should only end through cancellation.");
    }

    public override Task SendAsync(
        ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage,
        CancellationToken cancellationToken) => Task.CompletedTask;
}
