# **The Legend of Zelda: Link's Awakening DX HD Changelog:**

## **v2.0.24**
### Hosted room wake-up and automatic port recovery
  * Added a stable archipelago.gg room-page field to Android's in-app seed setup and installed-profile editor, including an optional intent extra for companion applications.
  * Automatically wakes a sleeping hosted room, reads its current server port from Archipelago's room-status API, and atomically saves a changed port to the active save profile before connecting.
  * Preserved cached-endpoint fallback, capped reconnect backoff, offline check replay, direct/self-hosted server behavior, and isolation between multiple seed profiles.

## **v2.0.23**
### Complete Magpie integration and randomized event fixes
  * Replayed the complete Archipelago item history whenever Magpie opens or reconnects, added full item-quantity coverage including progressive, duplicate, filler, trap, health, ammunition, medicine, and dungeon items, and retained stable fallback identifiers for future items.
  * Matched every supported Archipelago check to Magpie's native identifiers and identified bridge messages as Archipelago state so the embedded and LAN trackers select the correct logic automatically.
  * Kept Marin's randomized Ballad teaching check available when Link has already received the Ballad from another location.
  * Completed the Rooster revival location safely without starting a duplicate resurrection sequence when the Rooster is already following Link.

## **v2.0.22**
### Persistent Magpie tracking and randomized shop purchases
  * Kept pending randomized shop checks purchasable when Link already owns the shelf's vanilla Shovel or Bow.
  * Kept the embedded Magpie WebView loaded and connected while hidden, and added controller B/Select support for closing the tracker overlay.
  * Added Magpie GPS updates with precise overworld and dungeon positions, coarse underworld following for houses and caves, and full position replay after tracker reconnects.

## **v2.0.21**
### Magpie Tracker compatibility and live overlay
  * Translated numeric Archipelago choice options into the names and booleans expected by Magpie, preventing its accessibility renderer from crashing with an empty logic list.
  * Replaced the separate Android tracker activity with a right-side in-game overlay. Opening Magpie now closes the pause menu while gameplay and music continue behind the tracker.

## **v2.0.20**
### Embedded Magpie Tracker and randomizer behavior fixes
  * Added an Android pause-menu page for `magpietracker.us`, preconfigured to connect to LADXHD's local autotracking bridge, plus optional LAN autotracking for an external device.
  * Added inventory and location-check synchronization with full state replay after either the game or tracker reconnects, without exposing the Archipelago password.
  * Made the Color Dungeon fairy grant both randomized checks and added telephone-booth switching between the green tunic and received blue or red tunics.
  * Restored the Ghost House barrel check independently of the vanilla ghost quest.
  * Made the 200- and 980-rupee shop displays follow their Archipelago check completion instead of Link's owned Shovel or Bow, preventing a pending randomized Bow-slot check from becoming arrows.

## **v2.0.19**
### In-app Archipelago seed setup
  * Added an Archipelago setup screen inside the Android app, accessible from the file-select Settings menu, so generated `.apladxhd` seeds can be imported without the companion app.
  * Added direct entry of the server address and port, optional password, and target in-game save position while preserving seed-file verification of the player slot and location mapping.
  * Added an installed-profile chooser for updating a seed's server, port, or password without reselecting its manifest or risking another save position.

## **v2.0.18**
### Randomizer event and inventory fixes
  * Prevented owl conversations from persisting the inventory-disabled save flag, so interrupted Yarna Desert owl events can no longer leave the inventory unavailable until the save is reloaded.
  * Kept the Toadstool and Magic Powder independent in Archipelago. Receiving Powder no longer consumes the Toadstool before the Witch check, and compatible affected saves restore a missing Toadstool from item history while that check remains pending.
  * Ended Marin's beach escort after its dialog finishes in Archipelago, advancing to the normal post-Walrus state because the randomizer removes the Walrus. Compatible saves where Marin is already stuck following Link are repaired automatically.

## **v2.0.17**
### Connection and randomized item recovery fixes
  * Reworked Archipelago socket shutdown so dropped sessions cancel their receive and send workers, dispose the old WebSocket before reconnecting, and use capped exponential backoff instead of accumulating abandoned sessions until a game restart.
  * Made the Trendy Game identify its randomized prize by source location instead of displayed item. The crane no longer permanently collects a prize before Link receives it, and compatible affected saves respawn an unchecked vanished prize.
  * Changed Boomerang Guy to the APWorld's gift behavior so he never removes randomized equipment. Existing saves restore equipment removed by the old trade, and Archipelago item replay restores a received but missing Boomerang.
  * Added the Boomerang to the Give All Items cheat.

## **v2.0.16**
### Expanded inventory layout fix
  * Kept all 16 equipment positions inside the inventory panel's map-safe area so the minimap no longer covers the expanded storage slots.
  * Used a compact four-column grid in both four-button and six-button modes while retaining enough room for item sprites, counts, and level labels.
  * Moved the Rooster ownership indicator into the unused inventory header so it remains visible instead of being drawn behind the minimap.

## **v2.0.15**
### Randomizer behavior and save-repair update
  * Decoupled the desert Walrus, Moblin Cave encounter, BowWow, Flying Rooster, Wind Fish egg book, and Seashell Mansion checks from vanilla story state so they follow Archipelago ownership and location progress.
  * Kept the randomizer Rooster after Eagle Tower, suppressed the vanilla Ghost follower event, and repaired Rooster, BowWow, Ghost, level-two Sword, progressive equipment, and capacity-upgrade state in compatible existing saves after item replay.
  * Implemented Zol Attack traps and the normal Piece of Power and Guardian Acorn effects for remote items.
  * Made capacity upgrades refill their matching ammunition and prevented bushes or enemies from dropping Bombs before Archipelago grants Bombs.
  * Corrected progressive Sword, Shield, and Bracelet tier tracking when higher tiers arrive before lower tiers or native state is temporarily changed.
  * Rejected APWorld settings whose required runtime behavior is not implemented, including roosterless routes and non-vanilla warps, controls, stealing, and Trendy Game behavior. The APWorld is now version 0.2.0.
  * Added regression coverage for randomizer event independence, replay migration, progressive tiers, traps, temporary powerups, capacity refills, and supported-option validation.

## **v2.0.14**
### Raccoon Tarin event fix
  * Kept Raccoon Tarin present in the Mysterious Forest until Link actually cures him, even when randomized trade items advance Tarin's shared vanilla event state out of order.
  * Re-evaluated the current map's conditional objects when an Archipelago save becomes active so compatible existing saves receive the corrected spawn immediately.

## **v2.0.13**
### Expanded randomizer inventory
  * Expanded the equipment inventory from 12 to 16 positions so independently randomized items such as the Shovel and Boomerang can coexist without exhausting the vanilla trade-based capacity.
  * Expanded the visible equipment grid in both four-button and six-button modes and added a dedicated Rooster ownership indicator using the Rooster's actual sprite.
  * Prevented received equipment from being silently discarded when storage is full by safely using an empty action slot. Existing saves can now restore tracked equipment that older builds could not fit.

## **v2.0.12**
### Rooster save-loading crash fix
  * Prevented the inventory rooster from reading dungeon state before the save's map is initialized. This fixes the null-reference crash while loading affected saves without changing the existing rule that hides the rooster inside dungeons.

## **v2.0.11-ap1**
### Save-loading crash fix
  * Prevented Archipelago world-event updates, including Marin's Mabe Village song override, from running before the gameplay screen, map, Link, and save state are fully initialized. This fixes the null-reference crash encountered while loading a save.

## **v2.0.10-ap1**
### Raccoon Tarin and actionable crash diagnostics
  * Restored Raccoon Tarin in the Mysterious Forest without requiring the first Progressive Sword. His Archipelago appearance now follows Tail Key access and the pending Tarin's Gift check, including compatible existing saves.
  * Added an exact build identifier and up to eight privacy-safe game frames to crash telemetry. Frames contain only game assembly, type and method identifiers, metadata tokens, and IL offsets; raw stacks, paths, messages, arguments, locals, and framework frames remain excluded.

## **v2.0.9-ap1**
### Randomizer event fixes and test diagnostics
  * Fixed the Mad Batter reward softlocking Link when Archipelago replaces the vanilla capacity-upgrade dialog with a location check.
  * Kept Marin's Mabe Village song check available whenever Link has the Ocarina, matching the Archipelago 0.6.7 access rule without permanently changing her vanilla story state.
  * Enabled privacy-safe crash and randomizer telemetry by default for new installations of this early test build. The mandatory first-run notice is shown before any upload, existing choices are preserved, and either category can still be disabled at any time.

## **v2.0.8-ap1**
### Optional privacy-safe diagnostics
  * Added separate, disabled-by-default consent choices for crash diagnostics and randomizer connection statistics on Android.
  * Added a bounded offline event queue, next-launch crash delivery, 30-day anonymous identifier rotation, and immediate queue purging when consent is withdrawn.
  * Added aggregate Archipelago diagnostics for connection attempts, disconnects, reconnects, checks, received items, and unsupported item mappings.
  * Added a Cloudflare Worker and D1 ingestion service with a strict allowlist, keyed identifier hashing, request/rate limits, and automatic 60-day retention.
  * Server addresses, passwords, player/slot/seed names, save or seed data, exact items/locations, file paths, arbitrary logs, exception messages, and raw stack traces are never accepted by the telemetry schema.

## **v2.0.5**
## The "Next Big Thing" Update
  * Added updated German language files for menus and dialog by user [mrmercy82](https://gitlab.com/mrmercy82).
  * Everything in the project now builds with .NET 9.0 since this is the direction MonoGame is moving in.
  * Huge refactor/cleanup the codebase to make it easier to add additional backends. Credit: [aitorciki](https://gitlab.com/aitorciki)!
  * Implement GBC color correction shader based on the RetroArch shader. Toggle is in "Graphics" settings.
  * Add LAHDMod to swap button labels for Trendy Game and map teleport. Useful for certain Android devices.
  * Clear save data progress flags when deleting a save file. Restores "green Link" cursor with no weapons.
  * During the intro, Marin turns towards to the player from much farther away than while she is outside.
  * Block using the right analog stick to adjust the free camera when any type of sequence is playing.
  * Implement the ability to pick up and throw certain enemies as a weapon after they have been stunned.
  * If a carriable component goes null or inactive while being picked up, actively stop the grab and pickup.
  * When carrying an object and throwing it, prevent picking up a new object on the same button press.
  * Shield bump knockback now uses the contact point to determine the direction instead of always straight back.
  * After dungeon 1 is complete, reposition the owl, adjust dialog trigger range, and remove the "fly-in".
  * Remove owl encounter west of Moblin Cave and fix east encounter position, dialog range, remove "fly-in".
  * Slightly adjust the entry position of the owl when he appears outside dungeon 3 after completing it.
  * More owl position adjustments: when returning the ghost to the grave and outside the Lower Face Shrine.
  * Rework enemy "OnHit" so components deactivate immediately. Prevents death knockback damage/interactions.
  * Standardize spear throwing enemies, their functions, and fix vertical offset when thrown left and right.
  * Add cooldown to spear throwing enemies spear throw so that they can't throw multiple spears concurrently.
  * Prevent ice blocks (found in 2D dungeon 8 maps) from melting when hit with Magic Rod afterburn effect.
  * Add various lighting effects to the final boss: energy ball, tail parts, fire bats, and DethI eyeball.
  * Fix a crash on the file selection screen that existed since v1.0.0 when spamming controller directions.
  * Fix a bug on Android where pressing start to load a save file opens the inventory before the game world.
  * Fix music transition detection to be more accurate when entering the field where Marin is on the beach. 
  * Fix deleting a save file and resetting the items on the "Link cursor" (tunic color, sword, and shield).
  * Fix "Disable Helper Mesages" from earning the 26 seashell achievement and 5 golden leaves achievement.
  * Fix the achievement to talk to the all 9 owl statues in one session that was not tracking each owl statue.
  * Fix the music fade system overriding the volume to 100% instead of what the player set in Audio Settings.
  * Fix the music fading out when moving between overworld locations while under the effect of a powerup.
  * Fix the music transition when BowWow is kidnapped when entering Mabe Village by the ball tossing children.
  * Fix the rain texture corrupting on Android devices with Mali GPUs when returning to the intro cutscene.
  * Fix a color mismatch for sleeping Tarin and the bed by disabling shadows for Tarin's sleeping sprite.
  * Fix Marin's comment about bad ocarina playing when she is a follower to not appear while inside a dungeon.
  * Fix the owl's sprites; missing black pixels around the ears/eyebrows that make it look more "pointy".
  * Fix certain effects (like fireball death effect) not being deleted after they are finished animating.
  * Fix custom camera "Bosses" set to "Classic" to actually enable Classic Camera when fighting King Moblin.
  * Fix being able to enter the dungeon 8 west cave back entrance from the ladder due to lack of collision.
  * Fix enemy Armos Statue from being pushed into the collision if shield bumping it while it is asleep.
  * Fix enemy Coconut to have a AiState and AiDamageState for accurate hit behavior and can drop powerups.
  * Fix enemy Ghini to move with the gravestone if it spawned from that gravestone and the stone was pushed.
  * Fix enemy Moblin spear and metallic "tink" sound effect sometimes triggering at random when exiting maps.
  * Fix enemy Red Zol (again) to not die isntantly with Level 1 sword hits and experience proper knockbacks.
  * Fix miniboss King Moblin custom camera set to "Classic" actually setting Classic Camera during the fight.
  * Fix miniboss Turtle Rock to properly reset back to it's initial position when "Classic Camera" is active.

## **v2.0.4**
## The "Yet Another Emergency" Update
  * This is an emergency update to fix an issue where it's impossible to defeat the dungeon 5 boss.
  * Updated Swedish language files for achievements by user [romraptor](https://github.com/romraptor).
  * MonoGame updated to v3.8.5 and MonoGame.Extended updated to v6.1.0 for future DX12/Vulkan support.
  * Huge thanks to [aitorciki](https://gitlab.com/aitorciki) for helping to smooth over the transition to the new MonoGame version.
  * Implemented MonoGame's new Content Builder system to generate the XNB files in the "Content" folder.
  * When entering Mysterious Woods for the first time the owl will appear immediately when transitioning.
  * Music tracks now fade out whenever music changes on the same map. This mostly affects the overworld.
  * Slightly increase hookshot collision box and slightly increase collision box of stones, pots, and skulls.
  * Fix OGG music mods on Android that failed to loop. Thanks to [Fransis J. J](https://gitlab.com/fransis.jjj) for the detailed report and fix.
  * Fix talking to Manbo while performing an action, learning the song, and getting stuck holding the ocarina.
  * Fix talking to Manbo while jumping into the air and triggering the dialog, which causes the same bug above.
  * Fix Manbo's palette so that it matches across frames. For some reason the shade of green was alternating.
  * Fix enemy Bomber when Classic Camera is active so that it remains in place and does not leave its field.
  * Fix boss Slime Eel not being able to be hit with the hookshot and fix it sometimes retreating after death.

## **v2.0.3**
## The "Loot Tables and Chairs (or Beetle Bugs are Buggy?)" Update
  * The main focus of this update was a huge push for accuracy for anything that still remained different.
  * A new "Patcher-Lite" is offered that downloads things as needed and functions just like the main patcher.
  * Added updated Swedish language files for menus and achievements by user [romraptor](https://github.com/romraptor).
  * Rework controller support in Launcher so that multiple controllers can simultaneously manipulate menus.
  * Replace Patcher GitHub links with GitLab and add 7-Zip "7zz" for Linux and MacOS for creating an APK.
  * Implement a workaround for SteamOS devices (aka Steam Deck) to auto-hide the on-screen keyboard at launch.
  * Update the Launcher with all options added over time including Achievement options and Camera options.
  * Implemented enemy loot tables. The items and rates that enemies drop items now matches the original game.
  * Reduced the player's directional influence on sword and shield repel velocities (lessened while jumping).
  * Made a small rework to sword beam (sword shot) so that it can never hit multiple targets in a single shot.
  * Do not spawn enemy Spiny Beetle on the overworld using "ObjEnemyRespawner" to prevent duplicate beetles.
  * Magic Rod passive fire duration is shorter, sound effect removed, and burns enemies that come in contact.
  * When throwing a bomb and it hits shallow water, instead of bouncing it will immediately stop in place.
  * The option "Classic Sword" now prevents two bushes or crystals from being destroyed when poking between them.
  * Fix sword poking bushes and crystals to always destroy the object instead of playing metallic "ting" sound.
  * Fix Launcher controller support for the Steam Deck while it is in "Game Mode" that was broken since v1.9.1.
  * Fix MacOS Launcher script "fix-macos-permissions.command" and fix codesigning the Launcher file itself.
  * Fix controller not working for Launcher on Steam Deck when enabling and running it from "Game Mode".
  * Fix a crash that happens when hitting a bush-carrying "Spiny Beetle" with the Magic Rod or the Boomerang.
  * Fix overworld map large "area icon" to draw on top of the smaller icons when "Map Teleport" is enabled.
  * Fix Magic Rod to spawn fire when it hits enemies that don't take damage (Blade Trap, Spiked Beetle, etc).
  * Fix Hookshot damagebox so it's slightly smaller and can not come in contact with objects a tile below.
  * Fix crystal clusters so they can only be smashed with the sword (or sword shot if enabling the modifier).
  * Fix pushable component on Cuccos and Mutts to repel Link when shield bumping but not repel the animal.
  * Fix enemy Beamos to have no effect from sword, spawn fire from the Magic Rod, and no effect from Boomerang.
  * Fix enemy Bomber to die in one hit to the Boomerang and turn into a puff of smoke like with Magic Powder.
  * Fix enemy Bombite (Red) to not absorb any hits, can not be stunned, and all hit types reset pinball mode.
  * Fix enemy Bombite (Green) so that bumping with the shield starts the timer and they absorb projectiles.
  * Fix enemy Bone Putter to properly react to the various weapon, lose its wings, and drop the correct items. 
  * Fix enemy Gel to have a pushable component (shield bump) and a burn state (burns from Magic Powder/Rod).
  * Fix enemy Ghini to properly reset and respawn from gravestones in Classic Camera when switching fields.
  * Fix enemy Green Stalfos spawned by Hardhit Beetle to only spawn hearts and die when the boss is defeated.
  * Fix enemy Iron Mask weapon interactions. Only the sword stuns + knockback, magic rod creates fire, etc.
  * Fix enemy Peahat so that only sword can deal the temporary stun and fix Boomerang & Magic Rod interactions.
  * Fix enemy Spiny Beetle so that all items and weapons interactions are fully accurate to the original game.
  * Fix enemy Spiny Beetle that carries a stone to have a chance to spawn a fairy when the stone is smashed.
  * Fix enemy Spiny Beetle that carries stones and skulls so the hookshot repels instead of grappling them.
  * Fix enemy Zol (Red & Green) to get knocked back by weapons other than the sword except in specific situations.
  * Fix boss Hardhit Beetle going invincible when given lives via modifiers and the final hit doesn't kill it.
  * Fix boss Hardhit Beetle reaction to the various weapon types: some have more color change impact than others.
  * Fix "Custom Camera" boss fights option to work while in the color dungeon (I forgot to implement it here).
  * Fix "Custom Camera" boss fights switch getting stuck on Classic Camera if dying or quitting to main menu.
  
## **v2.0.2**
### The "Candid Camera" Update
  * Reimplement "Camera Settings". Modern/Classic camera can now be customized based on specific map types.
  * Remove camera options "Overworld Only" and "Dungeons Only". The new camera options gives more flexibility.
  * Optimize the camera-swap system which switches (Modern Camera <-> Classic Camera) between map transitions.
  * Implement a new camera feature to swap to "Classic Camera" during boss fights; found in new "Custom" menu.
  * Grabbing items with the sword while jumping that "show" the item, Link falls instead of freezing in air.
  * Added the "Purist" game type when creating a new save file. Game types now use the "Preset" functions.
  * Fix Android on-screen controls to allow "sliding" the finger and fix buttons rapidly repeating inputs.
  * Fix being able to walk between two holes if lining up with them just right by increasing the pull strength.
  * Fix interacting with Richard after handing him the golden leaves and before he reaches his destination.
  * Fix enemy Pols Voice being immune to bomb arrows. This only affected impact damage and not the explosion.
  * Fix enemy Red Zol dying instantly when damage dealt is one which also caused both Gels to instantly die.
  * Fix enemy Zora spawn patterns. Some spawn on one tile, some alternate between two tiles, some are random.
  * Fix enemy Zora spawn timers so that they are somewhat random. Also increase the range of their fireballs.

## **v2.0.1**
### The "Project Resurrection" Update
  * I released v2.0.0 in a panic, and it was a fine enough stopping point. But there are still bugs to fix!
  * Added updated German language files for menus and achievements by user [mrmercy82](https://gitlab.com/mrmercy82).
  * Block the in-game menu during the witch making magic powder cutscene after handing over the toadstool.
  * Rework fireballs so they are accurate to the original game and add the proper "green smoke" death effect.
  * Add overworld missing enemies: Bomber & Zora northwest of Animal Village (field left of the 2x Buzz Blobs).
  * Animate stone particles when the world is frozen. This is used in dungeon 6 for the nightmare key chest.
  * Launcher: Fix missing selection highlight and cursor when manually editing the field of Numeric Up/Downs.
  * Fix restoring carrying state after picking up an item that is shown and fix showing the pickup animation.
  * Fix an "error" message from popping up when collecting the Golden Leaves before the achievement is earned.
  * Fix the powerup "spark effect" so that there is four sparks that converge on the powerup instead of two.
  * Fix dungeon 6 torches: the bottom two should not shoot fireballs (room in looping hallway with 2 Red Zols).
  * Fix dungeon 6 face statues (the ones that require level 2 power bracelet) to not have hookshot collision.
  * Fix enemy Arm Mimic to absorb all projectiles it's immune to, instead of absoring one every half a second.
  * Fix enemy Red Zol/Gel inaccuracies: Red Zol dies to Magic Rod, no knockback, and both die instantly.
  * Fix enemy Green Zol so that it can be knocked back up until it stops shaking after it unburrows itself.
  * Fix enemy Mini-Moldorm tail to be hidden during knockbacks and during Classic Camera screen transitions.
  * Fix miniboss Moblin King to not make a "ting" sound when piece of power is active and fix defeat sound effect.
  * Fix miniboss Dodongo Snakes to reset when entering their field in Classic Camera and fix stray body parts.
  * Fix miniboss Smasher in dungeon 6 to behave closer to the original game. Too many issues to list here.

## **v2.0.0**
### The FINAL UPDATE
  * That's right folks, this is it. GitHub destroyed my repo. I wanted to get this last version out though.
  * Draw the sword in front of Link when facing right or down, and draw behind when facing up or right.
  * Add a camera field object in Seashell Mansion to fix Classic Camera transition showing Link on the right.
  * Fix several enemies that have their animation speed increased when falling down holes to properly reset.
  * Fix Stalfos "sliding" around after jumping and fix a bug where they could get permanently stuck sliding.
  * Fix desert cactus having collision with a low depth where Pokey could be hit over top of it.
  * Fix double sword beam explosion on crystal clusters. This was a bug due to hitting collision and hitbox.
  * Fix sworded enemies being permanently alerted when smacking the sword against the wall.
  * ITS BEEN FUN EVERYONE. ENJOY THE FINAL RELEASE! IT WAS A LONG ROAD BUT I AM PROUD OF WHAT I ACCOMPLISHED!
  
## **v1.9.7**
### The "Neverending Quest for Accuracy" Update
  * Refresh the achievements page when closing the in-game menu with select and then reopening it with select.
  * Prevent item collection during hole absorb: fixes angler key missing when grabbing + absorbed by sand trap.
  * Create two sparks around a powerup (acorn and piece of power) that converge on the item when collecting it.
  * Launcher updates itself then restarts before patching the game so that the launcher doesn't miss new files.
  * Add an option to the Launcher and OverlayManager.lahdmod to allow map navigation to undiscovered map nodes.
  * Increase the accuracy of Link's position on the minimap + discovery by referencing his "center" position.
  * Play missing error sound when attempting to use bombs or arrows while their current count is zero.
  * Bushes destroyed by Magic Rod or Boomerang cause an explosion effect and do not drop rupees or hearts.
  * Make the flying rooster do a high jump when running into walls and getting bounced back into the air.
  * Delay the rooster's spawning animation and increase the wait until the dialog box is closed out.
  * Dungeon barriers block horse heads when in the up position and trying to throw them over top of them.
  * Implement behavior of sword carrying enemies chasing after a sword poke (Moblin, Pig Moblin, Darknut, Stalfos).
  * Bosses where the player can leave the field were redesigned to reset (with effects) when leaving the field.
  * Rename the option "L2 Beam Cuts Grass & Bushes" to "L2 Beam Cuts Env. Objects" since it can smash crystals.
  * Fix sword shot beams to use the correct sparking effect (it should be green unlike most orange sparks).
  * Fix a rare situation where Link could get stuck carrying nothing if the carried object is lost or removed.
  * Fix bushes to not ignore throw weapon collision and make Magic Rod shots pass through all bushes.
  * Fix ObjButtonPush which forces the player through doorways to keep their facing direction when charging.
  * Fix music in moblin cave when rescuing BowWow so that powerup music plays over the current music of the cave.
  * Fix "no damage" boss achievements potentially being granted when they shouldn't for six of the nine bosses.
  * Fix a dialog message that says "error" after purchasing certain shop items: arrows, hearts, and the shield.
  * Fix calculation of Classic Camera fields to allow negative fields. Fixes going out of bounds north and west.
  * Fix a few cliffs on the overworld that had too many "upper level" objects set allowing to shoot up the wall.
  * Fix cracked tiles when Classic Camera is active to sometimes keep a hole active when resetting the tile.
  * Fix 2D spikes and Flame Fountain no longer dealing damage due to a mistake in the "HitPlayer" function.
  * Fix dungeon 2 pots in 2D to be instant pickup and make it possible to lift them while standing on them.
  * Fix dungeon 5 set of Blade Traps to fire off a warning shot when entering the room with Classic Camera.
  * Fix dungeon 7 Stalfos near the switch and Like-Like enemies on the first floor to be a bone thrower.
  * Fix dungeon 7 "low-collider" tiles in near the top-left pillar (the walls made out of small square blocks).
  * Fix dungeon 7 ball to remember position when exiting map while holding it and after throwing it in a hole.
  * Fix dungeon 7 "NPC Walls" that consumed too much space. Enemies can freely roam the full area of their field.
  * Fix dungeon 8 chest that spawns from defeating Dodongo Snakes to always reset no matter the exit taken.
  * Fix dungeon 8 secret passage after defeating Arm Mimics to not let the player enter it before it's spawned.
  * Fix enemy Anti-Kirby to repel sword shot, arrows, hookshot, and magic powder and knockback is sword only.
  * Fix enemy Bomber to reset HP after classic field change, more accurate flee behavior, and immune to hookshot.
  * Fix enemy Buzz Blobs to rarely and randomly have a chance to drop a single arrow, bomb, and fairy.
  * Fix enemy Gibdo to be immune to Magic Powder and not experience knockback when bumped with the shield.
  * Fix enemy Hardhat Beetle to repel sword shot, arrows, magic rod, and magic powder and remove knockbacks.
  * Fix enemy Like-Like to be able to absorb the player immediately after the current map has been loaded.
  * Fix enemy Mini-Moldorm to move at the correct speed and exhibit the same behaviors as the original game.
  * Fix enemy Peahat to not have any effect to sword shots when flying in the air (other attacks stun briefly).
  * Fix enemy Pols Voice to not get knocked back when getting stunned with Magic Powder. 
  * Fix enemy Red Zol and Gel to not get knocked back when hit to match the behavior of the original game.
  * Fix enemy Spiny Beetle to not get knocked back while hiding under objects + fix respawning after hole death.
  * Fix enemy Stalfos (both orange and green) to be fully immune to both Magic Rod shots and Magic Powder.
  * Fix miniboss Moblin King to knock the player back further and only knockback on the X axis when charging.
  * Fix miniboss Smasher to have much better aim, fix the ball damagebox size, and fix the ball's collision.
  * Fix boss Moldorm starting position, movements, behaviors, and spacing of body parts to match the original game.
  * Fix boss Genie to be immune to arrows, make bottle knockback Link, and hit types that can stun the bottle.

## **v1.9.6**
### The "Painful Cactus" Update
  * Translate the Achievements page "Earned:" text into all the currently available languages.
  * New achievement notification that pops up when an achievement is earned. Can be disabled in "Game" settings.
  * Bumping enemies with the shield applies a perpindicular offset based on the distance between center points.
  * Modified the dungeon keyhole object to accept parameters and made dungeon 4 entrance sequence more accurate.
  * The modifier for movement speed now also affects the movement speed when carrying the flying rooster.
  * Launcher: Add a shortcut to opening the game's saves folder by pressing key combination "Shift + f2".
  * Redux option "Map Teleport" will now also allow teleporting to any of the world teleporters when discovered.
  * Fix Marin when playing the ocarina to say that it's "Not very good" for all songs but Manbo's song.
  * Move "Epilepsy Safe" option to the more fitting "Video" settings. Hopefully this is the last time it's moved.
  * Fix taking damage while running with the boots to reset the boots charge time (aka: dash counter reset to 0).
  * Fix multiple issues with Master Stalfos: going invincible, more accurate bug replication, behaviors, etc.
  * Fix losing the "Carrying" state when climbing down a ladder in 2D while carring a jar which bugged out the jar.
  * Fix the sword shot being able to destroy crystal clusters. The modifier can be used to restore this behavior.
  * Fix the behavior and sound effect of the sword and shield when reacting to Orange Stalfos thrown bones.
  * Fix multiple issues with the desert cactus including variable knockback and being able to block it with shield.
  * Fix duplicate entry of "Moldorm" in "nightmares.atlas" that prevented its sprite from showing up in the editor.
  * Fix a single Orange Stalfos in dungeon 5 to be a bone thrower (I missed it last update due to obfuscation).
  * Fix the dungeon 6 Pols Voice achievement to play the ocarina so that only Ballad of the Wind Fish works.
  * Fix screen shake effect when opening dungeon 7 to show with normal screen shake effects (not "extra" effects).
  * Fix placement of a chest, rocks, and a wrong tile on Tal Tal Mountain Range (just outside cave with crystals).
  * Fix enemy Pokey to have the correct hit and explosion sounds and not deal knockback when knocking off parts.

## **v1.9.5**
### The "Achieving Better Achievements" Update
  * Achievements have been machine translated into all of the currently available languages. 
  * Scroll the titles and descriptions for achievements that don't fully fit within the interface element.
  * Add a button to the Achievements window to reset achievements with a confirmation message that defaults "No".
  * Slightly loosen the timer for the Hardhat Beetle kill achievement and remove the "reset beetles" requirement.
  * The option "Swap Confirm/Cancel" works in the Launcher. Settings must be saved before the change is applied.
  * Show a counter for the number of achievements that have been earned out of the maximum amount above the list.
  * Add a new option in the Launcher "Mods" options to preserve the sword's charge time when poking the sword.
  * Add color previews for Link's tunic colors in the Launcher "Mods" options. The color is instantly viewable.
  * Remove most lighting and color options from the Launcher in favor of offering these options via LAHDMods.
  * Shopkeeper will fully track Link's movements during the intro before he grabs the sword from the beach.
  * Add dungeon 7 pillars to always animate list so they continue falling when leaving the field in Classic Camera.
  * Fix pushable blocks in forest cave and cave near pig moblins to have the correct pushable directions.
  * Fix the swamp achievement not tracking when entering from the left and when loading from dungeon 2 entrance.
  * Fix the achievement for obtaining all photos to trigger immediately after obtaining the final photo.
  * Fix volume buttons not working on some Android systems (Android 14+?) by manually mapping them to API calls.
  * Fix the monkey fight with BowWow by the castle when Modern Camera is active and add a fade out/fade in effect.
  * Fix sword shot being able to pass through "low collider" collision (the walls made out of small square blocks).
  * Fix tiles: staircase in dungeon 6 near northwest exit and blocks in dungeon 8 in dark room with two lamps.
  * Fix some sound effects playing when "Mute Inactive Window" is enabled such as the lightning strikes in the intro.
  * Fix enemy orange stalfos in dungeon 5 so that all of them are bone throwers.
  * Fix enemy boo buddy so that they take a single arrow to defeat when they are in fleeing state.
  * Fix enemy Spark travel paths in dungeon 7 on floor 2 above Hinox and floor 3/4 in the room with dungeon barriers.
  * Fix enemy mask mimic to register sword beam shots from the front so it doesn't bounce forward when dying.
  * Fix miniboss Lanmola reset in Classic Camera field change & not trigger rentry dialog in Modern Camera when dying.
  * Fix boss Evil Eagle so that his head reflects the boomerang but it still deals no damage to him.

## **v1.9.4**
### The "Mandatory Post-Achievement Emergency" Update
  * Mandatory bug-fix release fixing several softlocks and bugs due to sword code being accidentally deleted.
  * Add a counter to the achievements page that shows how many achievements were earned out of the maximum amount.
  * If updating via the Launcher and achievement images are missing, next Launcher boot will prompt to install them.
  * Fix the Great Fairy in the color dungeon where her dialog would infinitely loop causing a softlock.
  * Fix multiple bugs caused by attacking including a softlock with the ocarina and picking up carriable objects.
  * Fix swimming and sword being on the same button. Link would swing the sword but not actually gain speed.
  * Fix enemy Giant Ghini from being able to be eaten by BowWow. Not adding it to "don't eat" list was an oversight.
  * Fix earning "Shadow Slayer" achievement being inverted where taking damage would grant the achievement.

## **v1.9.3**
### The "Pursuit of Accuracy Never Ends" Update
  * Implement an Achievements system. Achievements have been modelled after the LADX "RetroAchievements" entries.
  * Added collision to the three skeletons that are found in the desert since the original game has collision.
  * Modify the two skulls at the desert entrance to blend better with the sand like the non-DX version of the game.
  * Improve option "Select Last Save" to start the selection on the stored save slot rather than moving after load.
  * Master Stalfos damage bypass bug has been 100% faithfully recreated when using either sword shots or bomb arrows.
  * The "low heart beep" and low heart detection in general has been made to fully match the original game.
  * When playing the Ocarina, restore the last facing direction the player was facing before playing it.
  * Add option to Classic Camera that allows biasing the viewport when border is enabled to top or bottom of screen.
  * Fix Main Menu selection bug with "Select Last Save" when highlighting bottom bar, backing to intro, and returning.
  * Fix loading a custom "MapOverlay.data" file which is used to assign icons and descriptions for the world map.
  * Fix Tarin despawning after raccoon transformation. Field transition in Classic Camera, three fields in Modern.
  * Fix the shovel in Classic Camera so that it is not possible to dig tiles that are adjacent to the current field.
  * Fix item casting direction when on 2D maps to always face left and right (magic rod, bow, powder, bombs, etc.).
  * Fix giving the sleepy toadstool to the witch when "using" the item so that Link must be much closer to her.
  * Fix the fisherman on the boat photo to be more accurate to the original game where Link holds up his sword.
  * Fix the spawn positions of the Angler Key and Link when falling down the Lanmola hole sink hole in the desert.
  * Fix showing the Toadstool animation when on a 2D map while also climbing on a ladder or swimming underwater.
  * Fix Link's walking animation when entering the top of the tower of dungeon 7 (a.k.a. the Evil Eagle map).
  * Fix enemy Iron Mask (found within the level 5 dungeon) to block sword shot beams when attacked from the front.
  * Fix miniboss Grim Creeper so the animations of his bats are in sync with each other (like the original game).
  * Fix boss Genie from being able to be pushed with the shield while in the "rotating clone" phase after an attack.

## **v1.9.2**
### The "Damage Overhaul" Update
  * Overhaul damage systems to properly reflect the HP, damage multipliers, and damage dealt of the original game.
  * Keep items from leaving the field if dug up or moved along the edge of the field when Classic Camera is active.
  * Master Stalfos sword shot bug has been (mostly) faithfully recreated: 128 shots: fights 1-3, 96 shots: fight 4.
  * The "Have All Items" cheat does not give the Boomerang and items not obtained legit can not be traded for it.
  * Update the on-hit key setter object (used in Dungeon 8 in the one-eyed statue) to consume the arrow shot.
  * Change the passageway in dungeon 8 that leads to the Blaino rooms so that Link exits south instead of east.
  * Fix boomerang being unusable if leaving the map while still on the map by despawning it when transition starts.
  * Fix enemy Arm Mimic to not react while also absorbing some item types: Bow, Bombs, Magic Rod, and Magic Powder.
  * Fix enemy Sea Urchin reset in Classic Camera where burning removed collision and burning again would crash.
  * Fix miniboss Blaino to match the behaviors of the original game: knockback on hits, must hit closer to back.
  * Fix boss Facade from being able to take damage before the intro dialog has played out so it can't be cheesed.
  * Fix chinese dialog for chest stolen by Master Stalfos running outside of the dialog box (credit @Darth-Koopa).

## **v1.9.1**
### The "Dulling the Blade" Update
  * Implement the correct damage caps for the sword based on original game (L1 = 2 damage, L2 = 4 damage).
  * Sword multipliers no longer stack. This includes sword spin, boots dashing, piece of power, and red tunic.
  * Add support for changing the colors of the "disabled" state of several UI elements via "LAHDMods" for mods.
  * Custom track OGG <VOLUME> tags take priority over "volume.txt" allowing to finetune songs individually.
  * Support loading a custom "MapOverlay.data" file that can specify the dialog and icons on the overworld.
  * Launcher "Create LAHDPak" window info can be quickly filled out by dropping an existing mod onto the image.
  * Allow jumping off the side of cliffs while running at them with the Pegasus Boots and no direction held.
  * Disable number pad navigation in the Launcher and do not allow controller input unless the window is focused.
  * When multiple mods are imported that contain custom "scripts.zScript" merge the contents keeping the newest.
  * Give the egg a carriable component which allows grabbing and pulling on it. This has no use other than for fun.
  * Reduce the hitbox size of color dungeon switches to 8x8 from 16x14 to greatly reduce the chance of "wrong hits".
  * If both BowWow photo is available and "Nothing is Missable" is enabled, prioritize photo over taking for a walk.
  * When returning BowWow after a walk, give Madam MeowMoew a slightly unique line of dialog that references the walk.
  * Rework carriable component to have a "IsInstant" flag which is used instead of checking against object types.
  * Fix Link's missing "throwing" animation that was brought on by the change to more quickly pick up objects.
  * Fix the overworld minimap discovery revealing the field to the right of the current field by 32x pixels.
  * Fix color dungeon "jump tiles" to regenerate in Classic Camera and more quickly replenish in Modern Camera.
  * Fix the changes to castle gate collision allowing the player to obtain Richard's photo from behind the gate.
  * Fix all bed sprites across house tileset, dream shrine tileset, and Link's sprites to have consistent colors.
  * Fix being able to grab flying cuccos during a swarm and remove collision avoid types so they don't get stuck.
  * Fix duplicating the Slime Key in Classic Camera by digging it up and leaving the field and returning to it.
  * Fix the overworld tilset brown "house" tile to not have a few stray black pixels in the bottom left corner.
  * Fix removing a small stray black triangular shadow tile in the cave where the flying rooster's bones are found.
  * Fix "Always Instant Pickup" to not immediately enter the "pulling" state when pulling on collision objects.
  * Fix "Always Instant Pickup" to not deadlock the player when trying to lift heavy objects with level 1 bracelet.
  * Fix "Always Instant Pickup" to allow pulling on the pull levers that are found within dungeons 4 and 7.
  * Fix enemy Mega Thwomp found in dungeon 3 (the one you have to smash into) to reset in Classic Camera.
  * Fix enemy Goponga Flower taking damage while blinking. This prevents the hookshot from passing through it. 
  * Fix miniboss Armos Knight to take damage from normal Level 2 sword swings (only immune to level 1 sword swings).
  * Fix miniboss Grim Creeper in dungeon 7 floor 4 to close the doors when entering the room (floor 3 was unaffected).
  * Fix boss Genie to have a pushable component. This is what allows game objects to react to shield blocking.
  
## **v1.9.0**
### The "Physics is Fun" Update
  * Rework remaining tilesets so they are easier to make use of in custom maps which required remaking many maps.
  * Note that the above change will break any mods that use custom maps. They will need updated with new tilesets.
  * Implement running into the wall with Pegasus Boots in 2D maps and being knocked up backwards into the air.
  * When landing on spikes in 2D, a portion of the downward velocity is applied to the upward knockback.
  * Add an option to the Launcher in the "Link" section and "ObjLink.lahdmod" to make all pickups instant.
  * Throwing an object allows entering a new grab state during the throw allowing to pick up objects sooner.
  * Create a solution to push-blocks that block doorways using "dynamic" colliders in Dungeon 1, 3, and 7.
  * Collecting shells will say "something good is bound to happen" until the level 2 sword is collected.
  * Modify the castle gate drawing and collision to make it so Link can appear both in front and behind it.
  * Fix movement velocity being able to fight against repel velocities when sword hits or shield bumps repel.
  * Respawners that respawn objects in place only do this in Modern Camera. Classic camera requires a field change.
  * Disable the cheat "No Clipping" on 2D maps so the player doesn't fall through the floor into nothingness.
  * Fix shield bumping an enemy while climbing in 2D maps which would send the player in a direction forever.
  * Fix rail jumping when setting down bombs or when throwing objects/bombs by blocking them during the jump.
  * Fix being able to pick up bombs while in the air. This bug was exclusive to bombs and not other objects.
  * Fix Link showing the "pushing" animation for the first frame when saving BowWow from the Moblin Cave.
  * Fix missing "Classic Sprites" (alt Magic Rod, Boomerang, etc) in the Swedish "items" texture files.
  * Fix custom music currently playing track to leak into the next track after fading out during a map transition.
  * Fix cheat "One Hit Kills" to work on the final boss. This only defeats each phase instantly, not the boss.
  * Fix enemy Goponga Flower damage taken: boots + lvl 2 sword dash = destroy. Hookshot takes two hits to destroy.
  * Fix miniboss Grim Creeper bats dropping items, which could drop a powerup, which then bugs out the music.

## **v1.8.9**
### The "Fixes & Features Never Stop" Update
  * Add new "under the bridge" maps near the castle & monkey to act as a shortcut through the waterway.
  * Stealing from the shopkeeper will now earn the stealing photo even before talking to the photo mouse.
  * Stones that are pushed when Classic Camera is active will now reset on field change instead of actively.
  * Triggers to reset stones with Modern Camera have been moved to the edges of the field the stone is on.
  * Make Moblins start in "idle" state instead of "init". Fixes first screen appearance in Classic Camera.
  * Don't activate Vire if player is not in the same field. Fixes Vire popping up in the level 8 dark rooms.
  * Cap the damage dealt in a single hit to Moldorm (dungeon 1 first boss) to 2 damage like the original game.
  * Replace barrier sprites that spawn when fighting Angler Fish with a more fitting "blue brick" texture.
  * Extend the "fade out" during the ending sequence that shows the various cutscenes of the island vanishing.
  * Fix loading "Game: Advanced Settings" found the in the Launcher that broke due to "advanced" file caching.
  * Fix a rare bug when pushing stones into holes that could prevent pushing stones until the game was reset.
  * Fix the "Have All Items" cheat to remember the shield from Tarin and not remember when eaten by Like-Like.
  * Fix issue with attack types that failed to distinguish between Repelling, Particle, and RepellingParticle.
  * Fix blocking while climbing animation in 2D to be framerate independent (was still broken with VSync). 
  * Fix sword offset on File Selection screen when no shield is held & fix 1 pixel offset of no sword/shield.
  * Fix south facing jumping animation to correctly line up with other animation frames (was 1 pixel off).
  * Fix sword shot that was 1 pixel too far to the left when facing north.
  * Fix texture seam in the dungeon 7 sky where Evil Eagle is fought on Android devices with Mali GPUs.
  * Fix a missing cliff jump in dungeon 7 floor 1 north of the rectangular hole near the Gibdo's and Pots.
  * Fix Pols Voice making a metallic "tink" sound when attacked while holding out the sword (charging/poking).
  * Fix Smasher when trying to pick up the ball if the player is holding an object that is not the ball.

## **v1.8.8**
### The "Animations are Finally Flawless" Update
  * The "jump-fix hack" has finally been removed for a proper solution to jump => landing => jump animations.
  * Instead of parsing the "advanced" file on every reference, cache it using a dictionary on game load.
  * Make the "Mirror Shield Reflects" option reflection check much more robust to eliminate false positives.
  * When entering the Moblin Cave after BowWow was kidnapped, the first moblin will start attacking immediately.
  * Fix issue where standing on/raising the barriers in dungeon 7 would clip through Link not lifting him up.
  * Fix the Boomerang potentially getting "stuck" due to a desync between ObjLink and ObjBoomerang references.
  * Fix crash when collecting Dungeon 7 instrument (and potentially other places that modified follower list).
  * Fix alignment of sword in upward attack animation and fix alignments across all sword spin animations.
  * Fix sword offset on File Selection Screen when a player has the Level 2 sword in the selected save file.
  * Fix walking and blocking animation in 2D so that Link does not appear like he is "sliding" when walking.
  * Fix playing the song in front of the Egg with less than 8 instruments multiple times concurrently.

## **v1.8.7**
### The "Not Cheating is Cheating" Update
  * NOTICE: Emergency release. The "Have All Items" cheat causes save file problems trading for the boomerang.
  * Fix trading for the boomerang. Saving the game and loading would return the item that was traded for.
  * Fix hookshot on the raft causing Link to freeze due to forced "Idle" state until another item was used.
  * Fix the Patcher showing the "map file 'dungeon3.map' was not present" when creating an Android APK.

## **v1.8.6**
### The "Dreams Can Come True" Update
  * Add an option to the Launcher and "Game1.lahdmod" to allow background input from keyboard and mouse.
  * Closing the in-game menu will remember the last menu that was opened and the current position.
  * Make it possible to jump off the side of cliffs with an object in hand without dropping the object.
  * When Editor Mode is enabled, hide the mouse cursor until opening the editor with the Escape key.
  * Reduce the "body box" of Bushes so the generated shadow has the proper Y offset when it is carried.
  * Launcher: Add controller navigation, missing game options, and a "Reset" button on "Settings" window.
  * Launcher: Mouse scroll wheel change numeric up/down and add confirmation message when deleting mods.
  * More German menu translation fixes and German dialog fixes primarily new "Cheats" menu (credit @mrmercy82).
  * Fix dungeon doors to disable the carriable component while open which blocks being able to grab them.
  * Fix "walking in" map transitions where Link would appear as if he's walking in transitions he shouldn't.
  * Fix the "Dream Shrine" bed transition music from getting cut off early by changing it's priority.
  * Fix bushes being destroyed on impact when they are thrown and come in contact with an enemy.
  * Fix Launcher progress bars on both "Create LAHDPak" and "Install LAHDPak" windows to fill up.
  * Fix the game being able to handle having multiple follower which is possible via skipping instruments.

## **v1.8.5**
### The "Road to Perfection is Never Easy" Update
  * Launcher update feature has been made to also work on Linux and MacOS (credit @aitorciki for MacOS).
  * Rework main "Settings" page so the option categories are split into two columns for easier navigation.
  * Add a "Cheats" page to enable cheats like "Infinite X Item", Invincibility, No Clipping, and more.
  * Allow toggling fullscreen with the "F11" key and block "Enter" input when pressing "Alt + Enter" combo.
  * New option for feather to accelerate 2D swimming on Launcher "Mods" page or through "ObjLink.lahdmod".
  * Secret Seashells now display alternate dialog lines to the player after finding at least 20 of them.
  * Fix level 1 sword positions which were off by 1 since v1.8.2. Also fix some level 2 sword positions.
  * Fix Hinox miniboss to stop charging when stuck and increase the knockback distance when hit by its bombs.
  * Fix music volume transitions that were not working like during the secret ending or when opening dungeons.
  * Fix Marin to not leave the house after the intro until the sword has been grabbed off the beach.
  * Fix Ball & Chain soldier to take double damage from bombs so it's defeated in 2 hits like original game.
  * Fix CiaoCiao dialog in German language so the first line is properly interrupted with the second line.
  * Fix HUD Overlay Launcher options or "HUDOverlay.lahdmod" to hide rupees/hearts which were inverted.
  * Fix the ending sequence so that the owl fades out like the original game instead of flying away.
  * Fix a minor error in an entry in the Swedish language file "swe.lng".

## **v1.8.4**
### The "Hopefully Stable" Update
  * Implement Swedish language + textures. Thanks to [romraptor](https://github.com/romraptor) (creator).
  * Windows Launcher has a new feature to detect updates from Github and update to the latest version.
  * Android option "D-Pad Menus + Analog In-Game" switches to D-Pad for the Photo Album and Manbo Warp.
  * Linux executables for the Patcher and Migration Tool are now distributed as AppImage binaries.
  * Migration Tool & game "publish.bat" script no longer needlessly utilizes WSL for Linux/MacOS builds.
  * Fix Tarin appearing as a raccoon in the forest before the sword is retrieved from the beach.
  * Fix "Saves in Shared Storage" for some Android 16 devices that prevent writing to scoped storage.
  * Fix Patcher creating an APK and replacing the patched game executable with the unpatched version.
  * Fix BowWow being unable to eat enemies in the current field when Classic Camera is active (again).

## **v1.8.3**
### The "Multi-Platform is Hard" Update
  * New Android option "Saves in Shared Storage" which reads/writes saves outside of scoped storage.
  * Add ".bin" extension to Linux executables to simplify running them (credit @aitorciki).
  * Minor color adjustments to cave/house tilesets & fix wrong tile in upgrade bat & moblin caves.
  * Allow up to four connected controllers to function simultaneously instead of just "input #1".
  * Game version is now loaded from a central file "Directory.Build.props" (credit @aitorciki).
  * Add CLI option (headless mode) to Migration Tool to migrate assets from v1.0.0 (credit @aitorciki).
  * Potentially fix a rare issue where the level 7 tower collision was not working allowing early entry.
  * Fix a crash in Migration Tool when importing assets from fresh repo download (credit @aitorciki).
  * Fix MacOS patcher perserving symlinks when deep-copying MacOS app bundle structure (credit @aitorciki).
  * Fix MacOS patcher mod installation in MacOS app bundles, was skipping backup folder (credit @aitorciki).

## **v1.8.2**
### The "Aiming for Stability" Update
  * This version mostly focuses on stability as the last few versions had major breaking changes.
  * Make the "Pixel Grid Shader" option work on Android. Like desktop, it requires "Pixel Snapping".
  * Importing mods with language files will merge them and keep the most recent lines that conflict.
  * Implemented new tilesets for caves, houses, and reworked maps that use them (credit @GLEclipseGaming).
  * Translate the "Print" text in the Spanish Photo album textures (credit @IPeluchito).
  * Separate sword sprites from "link0.png" to "weapons.png" so modders can easily modify the sword.
  * Modify grass hitbox to be closer to original game. This mostly affects the "Classic Sword" option.
  * Fix Android crashing when building the APK with some mods that included "ObjDungeonBlacker.lahdmod".
  * Fix the "Game Over" texture to change when changing language. It only worked when first loaded.
  * Fix the Launcher "Mods" options "HUD Overlay" and "Link Tunic Colors" to actually work in-game.
  * Fix the Launcher crashing when using "Remove All Mods" when a custom "scripts.zScript" was present. 
  * Fix the Patcher to move v1.0.0 save files to the global save directory when applying a patch.
  * Fix the Patcher to not try to move v1.0.0 save files when generating an Android APK file.

## **v1.8.1**
### The "Next Generation Tooling" Update
  * This update was mainly a focus on updating the tooling, but also comes with the usual bug fixes.
  * Rewrite the Patcher to be cross platform using Avalonia. It is available on all desktop platforms.
  * All patching is now using "vcdiff" instead of "xdelta" which is faster and supports cross platform.
  * The LADXHD Mod Maker has been deprecated. Mods are now created and installed with the Launcher.
  * The new mod distribution format is ".lahdpak" files which are a zip file containing modded files.
  * Update the LADXHD Migration Tool to build with .NET 8.0 and rewrite the GUI to use Avalonia.
  * Dialog in language files of any type can now be replaced with "_redux", "_mod", and "_alt" tags.
  * Add Cukeman "Uncensored" lines to German translation from the original Link's Awakening.
  * Fix some music starting when a map transition starts before the fadeout and the next map is loaded.
  * Fix BowWow occasionally getting stuck moving between fields while a follower (affected all cameras).
  * Fix despawned moblins in the cave where BowWow is kidnapped from firing arrows in Classic Camera.
  * Fix Turtle Rock causing the ball children music to reset when BowWow is kidnapped in Classic Camera.
  * Fix a bug that allowed duplicating seashells when "Classic Camera" was the active camera.
  * Fix fireball to properly despawn after it fades out instead of remaining alive indefinitely.

## **v1.8.0**
### The "Clash of the 2D Systems" Update
  * This is an emergency update. The fix below greatly impacted the game making it feel awful to play.
  * Disable "corner correction" in 2D maps which was interfering with the 2D collision snapping system.

## **v1.7.9**
### The "Always More Room to Grow" Update
  * New Video option "Pixel Snapping" which makes sprite pixels snap to game pixels like a LCD grid.
  * New Video option "Pixel Grid Shader" which draws a pixel grid. Requires enabling "Pixel Snapping".
  * The Pixel Grid opacity can be configured via the Launcher "Mods" section or with "Game1.lahdmod". 
  * New Graphics option "Classic Item Sprites" which forces some item to use original DX sprites.
  * When colliding with a corner, Link will now sidestep it. Configurable via Launcher or ObjLink.lahdmod.
  * New bomb options: Auto-Pickup, Remote Bombs, Self-Damage. Configurable via Launcher or ObjBomb.lahdmod.
  * Overworld map and tileset were greatly optimized removing many unused tiles (credit @GLEclipseGaming).
  * Presets that apply the Classic Camera border also set the transparency of the classic border to 100%.
  * Minor changes to the German and French photo sprites to bring them in line with other languages.
  * Match move speed reduction of original game to 25% (from 20%) when moving through tall grass or water.
  * Increase the detection and attack range of the Pincer enemy to more closely match the original game.
  * Implement high DPI scaling for all of this project's tooling: Migration Tool, Patcher, and Mod Maker.
  * Add support for "MarinBeachSequence.lahdmod" which allows recoloring the sky during the beach sequence.
  * Fix Ocarina having Ballad of the Windfish immediately available without first learning it from Marin.
  * Fix the currently played music track getting "loud" or "stuttering" between map transitions.
  * Fix hitting the Genie bottle while in chase mode to play the correct sound not repelling particle.
  * Fix defeating the Genie heart container spawn so that it will never spawn inside a torch.
  * Fix room darkeners adding lamps outside the room into the calculation preventing full brightness.
  * Fix mistake in "[NAME]" placeholder tag in Russian language file (credit @NoseDevilEugen).

## **v1.7.8**
### The "Modding Improvements Continue" Update
  * Mods can now be packed into an APK by copying a "Mods" folder in the same path when patching.
  * Android version can now load "Mods" that are built into the APK file using the method above.
  * Mods can now add unique sprites/animations for the level 2 sword using a custom animation file.
  * Rework enemy Goomba stomping mechanic to be more accurate, remove collision box, and always animate.
  * The sword knockback effect on enemy "Hardhat Beetle" is now further back and more accurate. 
  * Patcher now creates all the "Mods" folder if they are missing. The game also does this on load.
  * Update Mod Maker to use a rich textbox for it's description instead of a label so text can scroll.
  * Many more objects have lahdmods: Karakoro, color switches, UI elements, textbox, inventory, and more.
  * A custom menu border is now supported via a "menuBackground.png" file in the mods folder.
  * More subtle fixes to the German language dialog files (credit @mrmercy82).
  * Icon files for game and tooling recreated from scalable vector graphics file (credit: @aitorciki).
  * Fix "ObjLink.lahdmod" to actually make use of the "mark0" and "mark1" damage shader values.

## **v1.7.7**
### The "Supporting My LAHD Mod" Update
  * More modding support: allow loading custom maps, dungeon minimap files, and scripts.zscript.
  * Update Mod Maker to support latest additions to modding & fix crash if a mod folder is missing.
  * Add support for "Genie" LAHDMod that can configure his fireball throw speed and fireball flicker.
  * Implement latest LAHDMod options into Launcher's "Mods" (Hinox, Genie, Island Background).
  * Created an "official-unofficial" Link's Awakening HD mod that rolls back "DX" specific changes.
  * Check this post for more details: https://github.com/BigheadSMZ/Zelda-LA-DX-HD-Mods/discussions/7

## **v1.7.6**
### The "Audio & Launcher Maintenance" Update
  * A repo has been opened for mods! Check here: https://github.com/BigheadSMZ/Zelda-LA-DX-HD-Mods
  * Music replacement should now be fully functional. There was a lot more to it than I imagined.
  * Implement sound effect replacement via WAV files that match the names of the XNB sound effects.
  * New "Mods" folder categories: Animations, Graphics, LAHDMods, Languages, Music, and SoundEffects.
  * Update the Mod Maker to reflect the recent changes with mod folders and fix description limit.
  * Support changing the overworld sky color via "ObjIslandBackground.lahdmod" which I overlooked.
  * Add a "Reset" button to the Launcher "Mod" menu. It is suggested to use this to fix issue below.
  * Fix the Launcher's default "Mod" lighting values of Magic Powder, Magic Rod, and Burning effect.
  * Fix the patcher being able to create the Android version (fixed in a v1.7.5 followup patcher).

## **v1.7.5**
### The "Music to My Ears" Update
  * Implement music replacement via OGG files placed in "Mods\Music" folder. See the wiki for more info.
  * Add "(Brazil)" to Portuguese and implement Portuguese (Portugal). (credit @tomasraimundo48-cyber).
  * Update LAHDMod for textbox overlay to allow floats for non-integer scaling (though not recommended).
  * Support LAHDMods to individually scale game menus and inventory: can use floats (though not recommended).
  * Add LAHDMod to allow recoloring the various Hinox minibosses found in dungeons 2, 7, and 8.
  * Show "Success" label on patcher progress bar after patching is complete and message box is closed.
  * Give dungeon 7 ball collision when it's height drops to 4 or lower so it bounces off walls on descent.
  * Improve some descriptions on launcher "Mod" menu and add options from latest LAHDMods.
  * Fix launcher language not lining up with game menu and fix damage taken multiplier to use decimal value.
  * Fix launcher scaling issues and intercept fullscreen button so it can not actually fullscreen.
  * Fix removing repelling velocities when a transition starts so player doesn't get stuck in a loop.
  * Fix MacOS finalization scripts & bundling for different Wine distributions (credit @aitorciki).
  * Fix mouse positioning in the map editor for devices that have a "notch" (credit @aitorciki).
  * Fix miniboss Armos Knight from sometimes sliding after death when arrows are fired in succession.

## **v1.7.4**
### The "Android Mali GPUs Fix" Update
  * Allow resizing the launcher vertically for smaller screens like the one for the Steam Deck.
  * Implement HD lighting effects for Mamu sequence when "Global Lighting" is enabled.
  * Recolor "red Link" to green on "link0.png" to make it easier to create a custom "link_cloak.png".
  * Add a higher quality icon to the Launcher on both MacOS and Linux (credit @aitorciki).
  * Give dungeon switches unique collision that is ignored by all projectiles. No more missed hits ever.
  * Fix pixel bleeding on the bottom of tiles that appears on Android devices with Mali GPUs.
  * Fix duplicated sprite shadow when picking up some items like "Guardian Acorn" or "Piece of Power".
  * Fix areas in level 8 dungeon map where collision could be breached by items (north stairs, west wall).

## **v1.7.3**
### The "Cukeman, We Have a Launcher" Update
  * A new launcher is now included to configure game settings and settings only found in LAHDMods.
  * Add transparency to the hookshot chain to match how it appears in the original game.
  * Pegasus Boots can now be cast while holding out the shield.
  * When Classic Camera border is hidden, visible objects are animated but their logic isn't updated.
  * The Sleepy Toadstool can be given to the witch by assigning it to a button and using it.
  * Level 2 sword beam modifier to cut bushes now slices through all no matter FPS or refresh rate.
  * When creating a new save, return to the file selection screen which allows entering code names.
  * The ending now works with Classic Camera and has had several improvements made to the camera.
  * Allow loading custom animation (.ani) files anywhere from within the "Mods" folders.
  * Remove the Power Bracelet requirement to push gravestones to match the behavior of original game.
  * Dungeon 2 must be completed and Bow Wow returned before the Color Dungeon entrance will open.
  * Fix Piece of Power and Guardian Acorn kill counts and spawn behaviors to match the original game.
  * Fix being able to push a pushable object while carrying an object (stones, graves, blocks, etc).
  * Fix accuracy of kill count. This does not have any use case and can only be viewed in save file.
  * Fix the Intro video to play the missing wave sound effects when Marin is walking along the beach.
  * Fix "sliding" during some transitions: SeaShell Mansion, Final Stairs - walking animation fixed.
  * Fix Bow Wow being able to find and attack monsters while Classic Camera is the active camera.
  * Fix missing shell present sprite in Seashell Mansion when "Disable Censorship" was untoggled.
  * Fix dialogue crashes due to (…) glyph in French, Spanish, Italian, Portugeuse, and Russian.
  * Fix setting "second_chance" which allows recruiting followers after dungeon 8 without map reload.
  * Fix several issues with game, launcher, and patcher builds for Linux and MacOS (credit @aitorciki).
  * Fix enemy Ghini (graveyard ghost) to be able to drop single arrows or bombs when killed. 

## **v1.7.2**
### The "I Wish I Didn't Have to Make this Update" Update
  * Chinese dialog fixes + implement placeholder tags around the script (credit @uplush).
  * Leaves spawned from grass textures are now semi-transparent to match the original game.
  * Add support for LAHDMods to adjust both the transparency of BowWow chain and leaf textures.
  * Fix a crash during the ending sequence that happened just before the credits appeared.
  * Fix Bow Wow forcing a redraw of Layer 0 textures which was deforming grass textures when nearby.

## **v1.7.1**
### The "WOOF! DIG! RUFF!" Update
  * Remove BowWow collision, update AI to be more like the original game, and make chain transparent.
  * When the UI is hidden with the ~ key, don't hide the textbox or the the dialog choice boxes.
  * Add controller support for Android devices where it is not properly detected like GameMT E5 Ultra.
  * Fix BowWow temporarily getting stuck and the chain refused to pull him when he is a follower.
  * Fix MacOS builds which were compiled incorrectly with Linux dependencies (credit @aitorciki).
  * Fix hiding the UI when "Disable Blur" is enabled. Note that disable blur breaks rounded corners.
  * Fix window scaling/stuttering issues for some Windows users. Only affects the DirectX build.
  * Fix some dialog for the man at the Hen House in the German translation (credit @mrmercy82).

## **v1.7.0**
### The "Reaching New Heights" Update
  * MacOS versions of the game are now available using the patcher. Big thanks to @aitorciki.
  * More German menu translation fixes and German dialog fixes (credit @mrmercy82, added in v1.6.9b).
  * Rework button remapping to show ?? for the button being remapped and cancel remap after 5 seconds.
  * Fix Richard not moving out of the way of the box to the shrub maze upon return visits to his villa.
  * Fix knockback strength when swimming in 2D maps. It was never meant to be as strong as it was.
  * Fix a bug where knockback never decays while swimming and charging the sword in 2D maps.
  * Fix continue playing village music when losing the powerup while inside the village.
  * Fix stealing crashing the game due to infinite loop causing a stack overflow (fixed in v1.6.9b).

## **v1.6.9**
### The "Finally Fully Finished Finalizing the Final Finishing Touches Finale" Update
  * More German menu translation fixes and German dialog fixes (credit @mrmercy82).
  * The editor is now fully functional on Linux. Forms was replaced with "NativeFileDialogSharp".
  * The Photo Book has finally been finished. Sound effects, button labels, hints, and textbox.
  * Add the Mamu spotlight lighting effects from the original games when Mamu is singing his song.
  * Adjust hitboxes of sword wielding enemies. I noticed sometimes the sword would pass through them.
  * Don't search for diggable treasure for Bow Wow when he is not a follower (slight optimization).
  * Add enemy colliders to some of the rooms in Level 7 dungeon on the first and second floors.
  * When collecting "shown" items with hookshot or boomerang, immediately remove the hookshot/boomerang.
  * Fix a crash with the hookshot to retrieve a Piece of Power or a Guardian Acorn upon its return.
  * Fix missing tooltip for "Graphics" which also threw off the tooltips for all options below it.
  * Fix the "this looks really heavy" message from popping up after collecting the power bracelet.
  * Fix sword beam travelling in the correct direction if Link turns on the last second after attacking.
  * Fix dungeon switches that raise/lower barriers to be hittable with projectiles from the top.

## **v1.6.8**
### The "Closing the Gap" Update
  * Support additional Android versions: Android-x86, Android-x64, Android-ARM, and Android-ARM64.
  * Reduce the damage boxes of all enemies in an attempt to make them much closer to original game.
  * Add options for on-screen controls to toggle visibility of D-pad and Analog Stick (credit @xsm2).
  * Hide certain on-screen buttons (triggers, right stick) if the option that uses it is not enabled.
  * Potentially fix an issue with the hookshot getting stuck in conditions it does not return to Link.
  * Fix instances in German language "deu.lng" such as verbs with capitalization (credit @mrmercy82).
  * Fix some of the on-screen buttons (namely triggers) on Android from rapid firing when held down.
  * Fix Android on-screen buttons "View" and "Menu" buttons to use the correct glyphs. 
  * Fix enemy Leever to not get "stuck" looking for a new place to spawn for long periods of time.
  * Fix enemy Sea Urchin to knock Link back the correct distance (was about half of normal knockback).
  * Fix enemy Fireballs to exist outside their field when "Modern Camera" is active (Classic unchanged).
  * Fix Egg dungeon crashing on Android/Linux when entering the boss room (case sensitive folder).

## **v1.6.7**
### The "Final Frontier" Update
  * Final "oldest" known issue was fixed. Music from GBS Player now (mostly) matches original game.
  * Add an on-screen always visible controller button for Android to toggle the controls on and off.
  * Implement sliders for on-screen buttons for Android to adjust both button and shadow opacity.
  * Fix viewing the painting by Schule Donavitch (the alligator in Animal Village) to be centered.
  * Fix patcher to restore the dungeon3_1.map (backup file) if it is missing which caused crashes.

## **v1.6.6**
### The "Stabilizing the Stability" Update
  * Add Android configuration options for on-screen controls in the "Control" settings menu.
  * Calculate the maximum UI Scale on Android using a vertical value of 240 as opposed to 256.
  * Split some of the in-game options in the "Video" menu into another menu named "Graphics". 
  * New option "Scale Sequence Amplifier" which can increase the scale of sequences beyond UI Scale.
  * Make the modifiers option "Mirror Shield Reflects Projectiles" to work on Wizzrobe shots.
  * Fix camera viewport transform matrix causing visible "lines" in tiles due to half pixel offsets.
  * Fix a crash when fighting Angler Fish on both Android and Linux (case sensitive folder issue).
  * Fix a crash (Android only?) due to the patcher not applying patch to dungeon 3 when patching v1.0.0.

## **v1.6.5**
### The "Cleaning up the Tooling" Update
  * Fix migration tool and patcher to always remove files no longer used which could cause conflicts.
  * More automation with patch and publish generation scripts to make creating new patchers much easier.
  * Implement on-screen controller for Android. This can be disabled on the "Controls" settings menu.
  * Slightly optimize the file loading pipeline. This should slightly increase the game's boot-up speed.
  * Fix loading all forms of mod files on all platforms: language mods, graphics mods, and LAHDMods.
  * Fix chicken swarm with Classic Camera active to stop the swarm on field transition and despawn chickens.
  * Fix a crash with Level 3 dungeon on Android. This most likely was due to old files creeping back in.
  * Fix two keys in level 5 dungeon to be "location bound" so they can not be brought into the overworld.

## **v1.6.4**
### The "Next Generation" Update
  * Enormous refactoring of the code base. This is only mentioned as it can lead to new bugs to squash.
  * Fix tons of regressions due to both the refactoring and other fixes which broke lots of stuff.
  * The Windows version now comes in DX11 or OpenGL. The patcher can select which version to install.
  * Android and Linux ports are supported if manually built from source. Builds coming later in some form.
  * MonoGame has been updated to v3.8.4.1 (from 3.8.1.303). This brings with it increased executable size.
  * Added a 1.5 second delay between the window appearing and the game starting for a smoother start.
  * Large portions of the audio code/GBS Player have been reworked to fix audio dropouts on Android.
  * Screen modes: Window, Borderless Fullscreen, and Exclusive Fullscreen has been turned into a slider.
  * Add graphics option to disable the UI blur effect and to disable the fog effect in Mysterious Woods.
  * New camera option "Classic Scale Lock" that allows rescaling the "Classic Camera" when disabled.
  * Chinese fonts are now properly implemented via bitmap fonts (PNG/FNT combo) using MonoGame Extended.
  * Rework the map transition system to be more deterministic to hopefully fix crashing on slow systems.
  * Implement dialog randomness: lines hitting chickens and breaking pots with Marin and the Hippo's lines.
  * Both the LADXHD Patcher and Migration Tool have been updated to support the new OpenGL Windows build.
  * If Link has a follower when using the map teleport feature, teleport the follower along with him.
  * Analog stick range must be pushed more than 85% towards a ledge to jump off it (does not affect D-Pad).
  * Marin now waits outside of all dungeons and her position changes depending on which dungeon is entered.
  * When running out of magic powder on the overworld, spawn a new toadstool without needing a map reload.
  * Adjust the "eat box" of the miniboss Dodongo Snake to be larger and size itself based on direction.
  * Add secret song for "Zelda" ("塞尔达") in Chinese language which is entered on the Name Entry screen.
  * Hopefully fix a rare bug where picking up items could stick to Link's head and not be collected.
  * Fix a rare bug with camera field objects in Classic Camera sometimes not snapping the camera in time.
  * Fix the Tarin bee-chase honeycomb to line up with the sound effect the moment it falls from the tree.
  * Fix using the analog stick to limit the distance of a cast while fishing by holding it to the right.
  * Fix miniboss Rolling Bones to not start sliding around when slashed while Piece of Power is active.
  * Fix dungeon 5 door near the Gohma miniboss that did not open due to an ObjLeaveButton that was moved.
  * Fix dungeon 7 ball from being able to be thrown through staircases and onto an unreachable platform.

## **v1.6.3**
### The "Very Quick Maintenance" Update
  * Fix some maps not automatically scaling correctly when the camera option "Modern Overworld" is enabled.
  * Fix Link's animation speed returning to normal when using "Pegasus Boots" into a map transitions.

## **v1.6.2**
### The "Lunar New Year" Update
  * Implement Chinese Language. Thanks to [Darth-Koopa](https://github.com/Darth-Koopa) (creator).
  * Name entry now allows just about any character to be entered (assuming the spritefont supports it).
  * Add a new "Modifier" that makes the Mirror Shield reflect enemy projectiles back at them.
  * Remove sprite shadows from several enemies whose Z-position never goes above a value of zero.
  * Perform optimizations on ObjectManager game loop and the sword carrying enemy weapon update loops.
  * Prevent damage when transitioning between maps. The "invulnerability" begins and ends with the transition.
  * Fix options "Camera Type" and "Controller Type" buttons to update text with the selected language.
  * Fix translating "Modern Camera" and "Classic Camera". It was a "do it later" thing that only came now.
  * Fix photo sequence to properly show a photograph after a previous photo sequence showed a photograph.
  * Fix sword/shield not being properly equipped/unequipped when changing inventory buttons between 4/6.
  * Fix false hits breaking through the shield (again). I think the last "fix" actually broke it worse.
  * Fix resetting the signpost maze on overworld reload when leaving it halfway through and returning later.
  * Fix playing the proper sound effect for fireballs whether hit with the sword or blocked with the shield.
  * Fix enemies sometimes "dying twice" which could show two different death explosions and spawn two items.
  * Fix enemy Anti-Kirby animation bugging out when Classic Camera is enabled and after a field change.
  * Fix enemy Bomber from being able to leave or throw bombs outside of the field that it spawned in.
  * Fix enemy Beamos lasers to not be blockable with the level 1 shield. This is a long standing v1.0.0 bug.
  * Fix enemy Red Zol when using "Extra Enemy Hit Points" modifier and incoming damage took more than 1 hit.
  * Fix enemy Winged Octorok pushable component (shield bump) getting disabled when it jumps into the air.
  * Fix miniboss Smasher to be easier to hit, slow down when running, and be impervious to knockback effects.
  * Fix dungeon 8 ledge above the room with the Green Zol in a chest where the player could not jump down.
  * Fix color dungeon grave stone sometimes not being able to be pushed to open the dungeon entrance.
  * Fix egg dungeon to have the default path when the player has never actually read the library book.

## **v1.6.1**
### The "Shadows From the Past" Update
  * Modify sprite shadows to only be shown when the shadow caster is in the air like the original game.
  * Allow force drawing sprite shadows, force draw on Trendy Game crane, and add shadows to Trendy items.
  * While unlikely Link's sprite shadow would ever vanish, it has been added to the "always animate" list.
  * Add sprite shadows to bombs. This includes both player bombs and bombs dropped by enemy "Bomber".
  * Bombs dropped by the player can also be picked up via the Power Bracelet so make it an instant pickup.
  * Do not add a carriable component to bombs dropped by enemies. This prevents picking up enemy bombs.
  * Link's projectiles no longer get stuck and instead collide with the edge of the screen with Modern Camera.
  * Projectiles from Link are also added to the "always animate" list to prevent them ever getting stuck.
  * Add more enemy colliders to all dungeons to keep enemies out of places like doorways and between rooms.
  * Increase the offset of ladder entry to 2D maps by 4 pixels to prevent accidentally leaving when entering.
  * Fix sword spin attack occasionally not dealing 2x damage, usually near the start of the attack.
  * Fix adding sprite shadows to items dropped by enemies. This includes: rupees, hearts, and powerups.
  * Fix Marin and the rooster's sprite shadows. They could sometimes disappear at random or not spawn at all.
  * Fix directional collision detection for enemy damage launch from Piece of Power and the Red Tunic.
  * Fix Classic Camera transition effect during the raccoon teleport or the teleport in level 6 dungeon.
  * Fix offsets of item "Heart Container" dropped from bosses to be more accurate to the original game.
  * Fix a cave on Tal Tal Mountain Range which contained a Spiny Beetle with an improper "type" set.
  * Fix enemy Flying Tile sprite shadow to be slightly more offset downward to appear higher in the air.
  * Fix enemy Spark flashing lights to be disabled when the option "Epilepsy Safe" is enabled.
  * Fix enemy Piranha damage state to not activate until its jump animation plays so it doesn't die as a fin.
  * Fix miniboss Giant Buzz Blob shots getting stuck on the screen edge by adding to "always animate" list.
  * Fix dungeon 3 "push buttons" in the room with the 3 green bombites and two green Zols to be more accurate.
  * Fix dungeon 7 "ball" used to destroy pillars to bounce off the screen edge when Classic Camera is active.
  * Fix the offset of all instruments in all dungeons to match their exact positions from the original game.
  * Fix cave shortcut near level 4 to exit passageway with Link walking down instead of left.

## **v1.6.0**
### The "Ultimate Version" Update
  * Slightly reduce the hitbox of the Mini Moldorm enemy. It was slightly larger than its sprite.
  * Rework sprite shadow objects slightly and update the sprites to be slightly more accurate to original game.
  * Fix knockback velocities not decaying while swimming underwater in 2D. This was a game breaking bug.
  * Fix being able to push two stones simultaneously. One stone will always win even when perfectly centered.
  * Fix invisible Tarin near the Honeycomb tree before finishing Level 3 dungeon that shows an error message.
  * Fix offsets of items "Piece of Heart" and "Yoshi Doll" to be more accurate to the original game.
  * Fix all collectable item "collection boxes" to match their sprites. This fixes collecting items between gaps.
  
## **v1.5.9**
### The "Squashing the Eldest Bugs" Update
  * Patcher: Add progress bar and fix an issue where it could fail to delete the temp folder for some users.
  * Add a new way to disable the in-game "Options" menu and disable the menu during the ending sequence.
  * Knockback velocity while in the air continues to be applied until the player hits the ground.
  * Fix fishing minigame so Link has correct Tunic color and fix casting directional press and block buttons.
  * Fix Trendy Game items and roll bands to remain animated while being partially outside of the camera bounds.
  * Fix sprite shadows (which are active with Dynamic Shadows disabled) to not be active on inactive enemies.
  * Fix the sword modifier to break pots/skulls to not allow breaking Spiny Beetle skulls with level 1 sword.
  * Fix moblin cave "ObjPushButton" to not be activated until the first instrument has been collected.
  * Fix dungeon doors to remain closed after grabbing the instrument. A return visit will open the doors.
  * Fix dungeon 7 teleporter to center Classic Camera on first version of top floor (before pillars destroyed).
  * Fix dungeon 7 nightmare key block puzzle solving in Classic Camera when pushing blocks outward and resetting.
  * Fix map teleport via Manbo's song to not appear while inside dungeons and instead warp to the entrance.
  * Fix Classic Camera panning effect when transitioning to the final room in the Egg with the pit to final boss.
  * Fix ending sequence properly setting "modern" camera depending on which options were enabled when starting.

## **v1.5.8** 
### The "Tying up Some Loose Ends" Update
  * It's now possible to replace text with a custom ".lng" file in the Mods folder (see wiki for more info).
  * The Redux option "Dungeon Teleport" has been transformed into a new option named "Map Teleport".
  * Redux option "Map Teleport" allows teleporting to cleared dungeons via map, Manbo's song, or both.
  * Remove damage launch of Golden Leaf carrying enemies: Crow and Mad Bomber so it is easier to collect.
  * Sword carrying enemies now aggro at shorter distance 50 pixels (from 60) and follow up to 65 pixels.
  * Fix the shield to no longer take false hits from direct damage from some enemies (especially swords).
  * Fix Classic Camera music cues to match field transitions that now line up with Link's "center".
  * Fix Link floating with Classic Camera when jumping + spinning + transitioning fields.
  * Fix sword carrying enemies so that their swords actually use the damage box (instead of collision box).
  * Fix sword carrying enemies so that their swords disable "hittable" & "pushable" components when burned.

## **v1.5.7**
### The "Slip Slide and Shake" Update
  * The game has been updated to build with .NET 8.0 instead of .NET 6.0 which is outdated.
  * Dynamically reduce the hitbox size of sword-bearing enemies and allow swords to have more pixel overlap.
  * Link is only drawn over transition circle effect during Modern Camera with "Overworld Only" disabled.
  * Lock and center camera when switching to "Modern Camera" in hybrid modes (Overworld Only/Dungeons Only).
  * Fix "Modern Camera" transition circle effect to not draw over top of Link when tranisitioning maps.
  * Fix walking and blocking simultaneously make Link appear as if he's sliding across the ground.
  * Fix the "Classic Sword" option to cut the tile that is directly in front of Link when facing up or down.
  * Fix NPC sword avoidance to not cause the sword to "flicker" when holding the sword over a NPC's sprite.
  * Fix screen shake effects to be closer to the original game. Add additional "Extra" screen shake effects.
  * Fix knockback distance of Pegasus Boots and add an "arc" which more closely matches the original game.
  * Fix 2D "one way colliders" from teleporting the player upwards when enemies bump into them.
  * Fix enemy "Gibdo" burned to "Stalfos" to be added to enemy triggers (used in level 8 dungeon for key).
  * Fix miniboss "Stone Hinox" to clear out all currently falling rocks as soon as it is defeated.
  * Fix boss "Slime Eye" starting height so his fall duration is closer to the original game.
  * Fix boss "Angler Fish" spawned fish "Angler Fry" to actually spawn more often with Classic Camera.
  * Fix boss "Slime Eel" intro sound effects to match the original game when about to burst through the floor.
  * Fix new save files to start with "Auto-Select" for Game Scale as the default selected option.

## **v1.5.6**
### The "Even More Accuracy?!" Update
  * Implement new options for "Restore Defaults" and "Presets" which can automatically set option groups.
  * Update the normal font (non-VWF) playstation button glyphs to use the ones from the VWF font.
  * Change the "Classic Camera" option into a toggle that offers between: Modern Camera and Classic Camera.
  * Add a new option for Modern Camera named "Overworld Only" where Classic Camera is applied everywhere else.
  * Track Link's center position and use it to reference Link's position for determining fields.
  * When Classic Camera is active it's no longer possible to get knocked back into a different field.
  * When Classic Camera is active bushes and grass can only be chopped down in the current field.
  * Optimize and increase accuracy of playing (or not playing) landing sound when landing on or near holes.
  * Prevent using the sword while performing a spin attack since this is original game behavior.
  * Reworked knockback physics to use frame based velocity. This allows more control over knockback effects.
  * Reduced enemy swords to match collision of the original game. Damage boxes still match visible sword length.
  * Enemy spears now bounce off of the shield when deflected instead of disappearing instantly.
  * Enemy Blade Traps have a delay with Classic Camera and dungeon 2 traps can fire a "warning shot".
  * Optimize the function that counts how many Dodongo Snakes remain which is used to restore dungeon music.
  * Reduce final boss Aghanim form's exploding shot chance to 25% from 33%. This is a revert to what it was.
  * Fix "snap camera" function for which caused visual stutters during Modern Camera map transitions.
  * Fix restoring "Classic Camera" after the ending has played out if the game was completed with it on.
  * Fix particles to actually spawn when repelling effects happen during any sword-based attacks.
  * Fix poke particles to properly apply an offset based on the direction of the poke.
  * Fix a serious exploit where a "sword charge" could be released on normal sword attacks indefinitely.
  * Fix the sword's charge-up time to be more accurate to the original game (670ms vs. 500ms).
  * Fix the Pegasus Boot's charge-up time to be more accurate to the original game (533ms vs. 500ms).
  * Fix dungeon 4 locked door that can prevent passing through after it's already opened on a return visit.
  * Fix enemies getting stuck on Classic Camera field transitions if they were in a damage state during transition.
  * Fix poke vs. sword enemies to repel properly: knockback reduced, charge interrupted, sword is not put away.
  * Fix enemy Blade Trap detection range to match the original game where it's 5 pixels from their line of sight.
  * Fix enemy Pincer to be able to fall down its hole when stunned or when defeated while it's over the hole.
  * Fix slightly moving when exiting underground cave near Kanalet Castle (the one with the seashell).

## **v1.5.5**
### The "Out of Clever Update Names" Update
  * Add new Redux option "Teleport to Cleared Dungeons" which can be done from the map on the inventory screen.
  * Add translated textures for the remaining languages that didn't have them: Italian, Portuguese, and Russian.
  * Add an option "Extra Screen Shake Effects" that separates new screen shakes from original screen shakes.
  * Add support for changing Classic Camera field transition speed via "lahdmod" file. (Credit: @Mandras2).
  * Add lahdmod support for changing colors of things not controlled via textures like the intro, ocean, sky, etc.
  * Add support for changing the damage shader colors in "ObjLink.lahdmod". This affects almost all flashing colors.
  * Fix Link's missing right-facing blocking sprites. When facing right, his arm should be seen holding the shield.
  * Fix inventory/map screens to render at screen resolution rather than render at low resolution then scale up.
  * Fix 2D Link "landing" sound effect to play when falling from ladders while pressing a downward diagonal direction.
  * Fix Classic Camera border rounding errors when using certain window resolutions and game scaling values.
  * Fix Buzz Blob shock effect getting stuck in Classic Camera on a field transition or hitting from a different field.
  * Fix crash when letting a fish get away in the fishing minigame when playing with Spanish language.
  * Fix "Enable Global Lighting" and "Enable Object Lighting" to be enabled by default and rename internal variables.
  * Fix properly resetting the various Thwomp enemies when leaving the field and returning in Classic Camera.
  * Fix loading custom "Intro" graphics. These require being in a folder named "Intro" in the "Mods\Graphics" folder.
  * Fix loading custom "UI" graphics (ui.png, ui_deu.png, ui_esp.png, etc). No special folder required for these.

## **v1.5.4**
### The "Opening the Path to Customs" Update
  * Add preliminary custom texture replacement which can replace in-game sprites in a non-destructive manner.
  * Graphics mods are added to "..\GameFolder\Mods\Graphics" and lahdmods to "..\GameFolder\Mods\LAHDMods".
  * Restore default number of inventory buttons to four. Game scale is adjusted by the front two top buttons.
  * Implement "Triggers Scale Game" which can toggle the bumpers/triggers having the ability to change game scale.
  * Implement "Six Inventory Buttons" option in "Control Settings" which also moves game scale to the back two buttons.
  * Change the lighting method of Witch's Hut and House by the Bay so that lighting the torches lights up the room.
  * Rename the sword interaction modifier "Slice Enemy Missiles" to "Slice Enemy Projectiles".
  * Extend support for the "HUDOverlay.lahdmod" file. It's now possible to use it to hide parts of the HUD.
  * Despawn Link's projectiles and bombs during a field transition when Classic Camera is enabled.
  * Fix Bow Wow being able to eat monsters that fall outside of the current field when Classic Camera is enabled.
  * Fix enemy Wizzrobe collision types so that it remains in the current field when Classic Camera is enabled.
  * Fix Mutts and Cuccos found around Mabe Village to properly reset during field transitions with Classic Camera.
  * Fix all remaining "Settings" menu tooltip strings across multiple languages causing game crashes.
  * Fix a small typo in the tooltip for the sword interaction "Ricochet Boomerang". 

## **v1.5.3**
### The "Lighting the Way to a Brighter Future" Update
  * Add Graphics setting to toggle "Global Lighting" which is the full screen darkening effect for object lights.
  * Add Graphics setting to toggle "Object Lighting" which is lights casted by items, enemies, etc.
  * Disabling these settings gives a more "vanilla" experience and eliminates the need for "Fullbright" modpack.
  * Move "Epilepsy Safe" to Game Settings. I was out of space and this was the only generalized option left.
  * Dark rooms in dungeons are now fully neutralized by light emitters such as items, enemies, torches, etc.
  * Add more lighting effects to objects such as magic rod, sword beam, bomb explosions, fairies, and more.
  * Most lighting effects now fade out rather than instantly disappear when the emitter is removed from the map.
  * Implement more "lahdmod" files and rework some previous ones. Several options were moved out of "ObjLink".
  * Fix missing death effect when under influence of Piece of Power or red tunic. It never worked in this port.
  * Fix enemies that carry sword to be able to fall down holes and tweak other enemies that may struggle.
  * Fix dungeon 2 power bracelet room playing compass sound. This should only play for rooms with keys in them.

## **v1.5.2b**
  * Fix a serious issue with the dialog path system that could break game logic when multiple actions happened at once.
  
## **v1.5.2**
### The "Monkeying Around With Swords" Update
  * Add a sub-menu to "Modifiers" that expands "Extra Sword Interactions" and offers a ton of new sword options.
  * Rework sword-item collection. Items now have a dedicated field that determines if the sword can collect them.
  * Fix player bombs so that items do not interact with them. Sword can interact with them with an option above.
  * Fix enemy bombs so that nothing interacts with them. It's possible to restore this behavior via a "lahdmod".
  * Fix several issues with the monkey near the castle: sound effects, Bow Wow duel, fleeing state, etc.
  * Fix animated lilypads found in the swamp from being culled along the camera's edges.
  * Fix another dungeon 3 stairs exit direction. This is the stairs in the first section leading to the "cross" rooms.
  * Fix skulls being destroyed when carried on the backs of Spiny Beetles.
  * Fix the moblin cave to remove the ObjPushButton from the doorway on a return visit which traps the player.

## **v1.5.1**
### The "Following the Dream" Update
  * Hide the instruments on the File Selection screen during additional menu transitions.
  * Don't try to select the last save on the File Selection screen when backing out of the "New Game" screen.
  * Skulls that are carried on Spiny Beetles have a 25% chance to spawn a fairy when broken.
  * Unblock the keyboard tooltip button on New Game page for anything other than the name entry field.
  * Implement Link being able to dodge Marin when falling into the well for the photo event.
  * Fix the sword poke from destroying two consecutive bushes with a single poke when poking in certain directions.
  * Fix a new file from being "cleared" if other files also have cleared state. Also add a fix to saves affected by this.
  * Fix one-way colliders used for bridges to be closer to the bridges so Link can't turn around after hitting water.
  * Fix remaining photo/picture sequences to be skippable: Bow Wow, Ghost, Face Shrine, Christine, and Painting.
  * Fix some "GameManager" states (stores life, rupees, deaths, etc) persisting through save file selections.
  * Fix the fisherman on the boat to continue to face the player when talking to him after the trade sequence is finished.
  * Fix a weird animation glitch when jumping or down towards collision where Link would push without the stick being held.
  * Fix dungeon 3 dark room behind bombable wall to light up when bombed. Also hide enemies inside that room.
  * Fix dungeon 5 room where falling down a hole respawns the player inside a door that closed behind them.
  * Fix dungeon 8 ice blocks hitbox size of 10x10 to 14x14. It was possible to shoot a fire rod shot between them.
  * Fix more movable blocks to be reset when attempting to leave the rooms they are in.

## **v1.5.0**
### **The "Awakening From the Dream" Update**

### **General:**
  * Fix the Marin dungeon sequence (a.k.a. longstanding Marin beach spawning issue) from firing when it shouldn't.
  * Picking up the sword at the beginning of the game on the beach now deals damage (spin will kill that urchin now).
  * Created a new object for Classic Camera that focuses the camera during a map transition. This fixes Classic Camera bugs.
  * Fix all Classic Camera respawning issues including: respawning on current field, field edge, and not respawning at all.
  * Rework pushable stones so that their behavior when pushed is much closer to how it is in the original games.
  * Pushable stones respawn when pushed down holes and when changing fields when Classic Camera is active.
  * Properly clear the sword damage box and the shield block box when they are no longer in use (fixes strange interactions).
  * Implement ability to grab and pull on most objects. This has no practical use case but was possible in the original game.
  * Rework grab box so it interacts with objects more precisely and rework pulling animation to match the original game.
  * When climbing a ladder on a 2D map and reaching the bottom of it, Link will properly transfer to ground instead of climbing.
  * When climbing a ladder on a 2D map and reaching the top of it, Link will perform a small flip.
  * One tile high objects in 2D maps will now push the player to the top of it when jumping into and not quite reaching the top.
  * Most ledges in 2D sections are now slightly sloped so they are easier to jump onto, but are flat when on top of them.
  * Disable the free camera and center it during most sequences. This includes trading items, cutscenes, photo events, etc.
  * Rework the shopkeeper's "revenge" sequence to be more like the original game & it will no longer take the player's potion.
  * When buying back your name from the shopkeeper after stealing, it will also remove a death from the save file.
  * Remove collision from the various animals found around the game: birds at Mr. Write's house, rats at Crazy Tracy, etc.
  * Fix the move speed modifier to increase movement speed on 2D maps. Also fix it from applying X velocity when it should be 0.
  * Fix attacking and jumping in 2D maps so that Link always faces Left or Right even if diagonal directions are being held.
  * Fix the jump in 2D mode to not apply extra velocity when releasing the button at the height of the jump.
  * Fix swimming in 2D maps so that Link can "jump" (which just ascends quicker) while also charging the sword.
  * Fix ladder transition in 2D maps so that Link exits the map facing north and continues the climbing animation.
  * Fix removing the "poking" state when poking is finished. This bugged out the jump fix hack.
  * Fix death sequence to use potion first. This fixes a "stuck in air" bug when dying/healing while flying with the rooster.
  * Fix cracked tiles to always detect when Link is pushing against a stone. Sometimes pushing would prevent them from breaking.
  * Fix pushable stones so that they can not be pushed when Link is not pushing or is positioned sideways (like when charging).
  * Fix several caves with pushable stones so that their positions reset when crossing over a certain threshold.
  * Fix Marin singing, Great Fairy, Hinox, and Dodongo Snakes to always reset music when leaving current field with Classic Camera.
  * Fix the burning sound effect to have the proper sound which is a combination of two sound effects.
  * Fix transition walk animation freezing when jumping and landing at the same moment a transition starts.
  * Fix old save files to work with "Nothing is Missable" option which allows getting the followers back and other stuff.
  * Fix cuccos from being targetable during a "cucco swarm" after attacking one 35 times.
  * Fix a Raven from taking off during the ending cutscene causing the "wings flapping" sound to play.
  * Fix the ending credits so that the final part of the music somewhat lines up when "THE END" pops up on the screen.
  * Fix the secret ending seagull chants to be evenly spaced apart like in the original game.

### **User Interface:**
  * Update File Select screen to show deaths, seashells, and collected instruments. Add sprites to values for identification.
  * File Select screen Link sprite: show tunic color, sword if Lvl2 sword collected, and shield if Mirror Shield collected.
  * Tooltips on the various options menus now automatically scale with the resolution and UI scale.
  * Players are now required to enter at least a single character for the save file name on the "New Game" screen.
  * Allow pressing "Start" to select a save file from the File Selection screen. Start also progresses "New Game" screen instantly.
  * Reverse the player's current hearts on the File Selection screen to go from left to right instead of right to left.
  * Added a new "Controls" page where control options are now located, remapping can be done, as well as a new "Dead Zone" option.
  * Separate analog input from directional pad input logic. This allows Dead Zone to not affect D-Pad but can disable analog.
  * Replace the Cyrillic characters (Russian) in both the normal and variable width fonts with new, more fitting fonts.
  * Add a new Input Option called "Digital Analog" which forces the analog stick to move Link in 8 directions like the D-Pad.
  * Add "Game Type" option to the New Game menu. Options are: Use Settings, Modern, Classic, and Hybrid which mostly affect camera.
  * Rename option "Damage Screen Shake" to "Screen Shake Effects" and disable all screen shake effects when it toggled off.
  * Update the tooltips for a few in-game options to be more accurate: Enable Dialog Skip, Nothing is Missable, and No Heart Drops.
  * Add a new Modifier named "Extra Sword Interactions". This allows collecting fairies, smacking bombs, and juggling the boomerang.
  * Extra Sword Interactions also allows breaking pots with Level 2 sword and beam, and the level 2 beam can also cut grass and bushes.
  * Modifier "Damage Multiplier" can now be set in 0.25 increments and 0% damage makes Link invincible.
  * Added a new modifier "Damage Cooldown" which can increase or reduce the length Link is invincible after taking a hit.
  * The option "Disable Helper Messages" also blocks the dialog shown when touching the nightmare door without the nightmare key.
  * Photo, picture, and painting sequences can now be skipped sooner by pressing the "Start" button.
  * Skipping a dialog with the Start button now rapidly scrolls the text and closes the box instead of just closing the box.
  * Block pressing "Start" to open the inventory screen during a number of events and cutscenes.
  * Fix French font characters "œ" and "Œ" which were not showing up because they were using the wrong Unicode character.
  * Fix skipping text during "question" dialogs by pressing "Start" to not cut off the text when skipping.
  * Fix showing sepia photos after photo sequences when Redux option "Colored Photos" is disabled.
  * Fix the Redux option "Variable Width Font" from moving the selection downward after toggling.
  * Fix the "space" character in the variable width font to take up less space. It now matches the ROM hack it was based on.
  * Fix the Trendy Game showing the correct buttons if they were remapped when giving the player instructions.
  * Fix several typos and missing text from the books found at the library. Also fix mistake in Papahl's dialog.
  
### **Items:**
  * Some items will no longer stop Link while casting: Boomerang, Magic Powder, Fire Rod, Bow & Arrow, Bombs, and throwing objects.
  * Tweak the sword's damage box to be larger during the second/diagonal animation frame so there is no "dead space" between frames.
  * The Boomerang item has been reworked so that it can collect multiple items on a single throw, but can still only grab 1 fairy.
  * Implement "Classic Sword" option that works like the original game where it can only cut a single bush, grass, or crystal at a time.
  * Rework the level 2 sword spawn at Seashell Mansion. The timings could get messed up if the message box wasn't closed immediately.
  * Increase the size of the damage box of Magic Powder to the width or height of a tile (or 16 pixels, depends on direction).
  * Magic Powder can no longer hit grass. Bushes and grass share an object so they also share hit types so they need filtering code.
  * Running with Pegasus Boots can now smash large crystals without needing to have the sword equipped.
  * Fire spawned from Magic Rod shots added to the "always animate" list so despawns when outside of the current viewport.
  * Bombs have been added to the "always animate" list which allows them to explode when outside of the current viewport.
  * Remove the message when buying bombs at the item shop that said something like "You got a bomb!".
  * Remove the message when getting your shield back from Like Like since the original game did not have one.
  * The hookshot makes Link invincible while it is in use. This allows him to use it over spikes without taking damage.
  * It's now possible to cancel the ocarina song by pressing the button again. This doesn't trigger ocarina listener events.
  * Opening chests immediately freezes the world instead of after a delay. Also, the items raises slightly higher above the chest.
  * The Trendy Game will now allow Link to move immediately after the crane starts grabbing like in the original game.
  * Fix the Trendy Game to close up shop when all items are grabbed by the claw rather than when all items are collected.
  * Fix shield appearing on Link when swallowed by a Like Like. It will now properly disappear and reappear depending on equipped status.
  * Fix firing Arrows and Magic Rod shots to make contact and damage enemies when jumping and the enemy & projectile sprites overlap.
  * Fix playing the "holding" animation when jumping onto the Level 2 sword sprite after turning in 20 shells in Seashell Mansion.
  * Fix being able to use bombs while jumping in the air. This doesn't have any practical use but was like this in the original game.
  * Fix Piece of Power/Guardian Acorn status between map transitions. It is kept when current/next map are either a cave or a dungeon.
  * Fix being able to fire projectiles outside of several 2D maps (such as the "Bridge" map and "Seashell Mansion").
  * Fix hookshot from occasionally firing extremely fast if pressed at just the right time by adding a small cooldown of 75ms.
  * Fix Link's facing direction when teleporting with the ocarina on the overworld using Manbo's song then saving/loading the game.
  * Fix Link's facing direction when teleporting with the ocarina in Level 7 dungeon floors 2+ or Level 8 dungeon via backdoor entry.
  * Fix the instruments to have collision against projectiles (boomerang, hookshot, etc.) since it was like this in the original game.

### **Enemies:**
  * Defeated enemies and effects are added to "always animate" list. This prevents frozen effects if knocked out of current viewport.
  * Update "stunned" state of several enemies to either lose their damage field while stunned or gain it back as soon as stun ends.
  * Don't respawn Spiny Beetle carry object when grabbing it and throwing it into a hole when Classic Camera is active.
  * Change enemy Piranha collision types to "non-water" type like the Water Tektites in dungeon 4. This collision only blocks them.
  * Reduce fairy spawn rate of Ghini to 25% (from 75%) and allow it to drop other items at normal drop rates.
  * Remove activation range check from Pols Voice, Red Zol, and Gel. They will become active when entering the field they are on.
  * Increase the knockback of the Flame Fountain on the way to dungeon 8 so the player can't force their way through with boots.
  * Disable the hit component of some minibosses until the boss is enaged: Rolling Bones, Cue Ball, and Hinox.
  * Three-of-a-Kind will now respawn when returning to the map/room they were previously defeated on.
  * When fighting the boss Angler Fish in dungeon 4, a barrier is spawned at the top of the arena to prevent leaving.
  * Angler Fish will no longer have a hit component (it won't take damage) until the fight starts so it can't be sniped.
  * Bow Wow can no longer eat certain types of enemies: Sea Urchins, Ghini, and Zombies.
  * The enemy Flame Trap has been added to the "always animate" list so it can't be exploited by keeping it out of camera bounds.
  * Increase the body box size of sworded enemies to 16x16 pixels: Moblins, Pig-Moblins, Shrouded Stalfos, and Darknuts.
  * Enemies that respawned in the original game when changing fields now respawn here: Bombite, Spiny Beetle, Hardhat Beetle.
  * Fix the burning state to reset when changing fields in Classic Camera. Enemy should return to normal state and remove burn.
  * Fix the sound effect played when enemies are either drowned or dunked into lava (splash sound instead of fall down hole sound).
  * Fix a hole in Moldorm room to drop the player in the correct location. Also move all drop points left by one tile (16 pixels).
  * Fix missing explosion sounds when defeating some nightmares. This includes the bosses from Level 5 dungeon and onwards.
  * Fix enemy Antifairy to properly remove its damage field when burning it with Magic Powder and fix it's interaction with shield.
  * Fix enemy Arm Mimic to get stunned when throwing objects (like pots) at them and make them invincible to Magic Powder.
  * Fix enemy Bomber to have unique death with Magic Powder that spawns a fairy 100% of the time. Magic rod spawns fairy 50% of the time.
  * Fix enemy Bombite to bounce off the sword when held outward in a charge state without the sword being put away afterwards.
  * Fix enemy Cheep Cheep from launching the player if jumped on just right while slightly in the water.
  * Fix enemy Crows from being invincible while perched in a tree. Only the Crow near Kanalet Castle should be invincible on the tree.
  * Fix enemy Darknut size and fix the spawn position when blowing up the walls they are imprisoned inside.
  * Fix enemy Gel to properly respawn when changing fields in Classic Camera instead of burning to death off screen.
  * Fix enemy Goomba to lose it's collision box when set on fire (does not affect Goombas on 3D maps, aka Level 7 dungeon).
  * Fix enemy Hardhat Beetle to be killable by Bow Wow or bombs. This also spawns a bomb after death. Also remove initial idle delay.
  * Fix enemy Karakoro to be walkable when thrown into a hole. Also fix resetting their states during a Classic Camera field transition.
  * Fix enemy Keese to not be affected by conveyor belts. Dungeon 3 is about the only place this is relevant.
  * Fix enemy Leevers from spawning on the ledges above the cave in the desert where Link can't reach by adding collision.
  * Fix enemy Like Like taking damage from items when swallowed. Link now also takes damage from outside sources when trapped.
  * Fix enemy Mini-Moldorm to make the falling sound effect when it is pushed into a hole and falls.
  * Fix enemy Pincer stun state, when it can deal damage, activation range, and replicate narrow window to kill instantly with powder.
  * Fix enemy Red Zol to spawn Gels in Classic Camera after burning & respawning after a field change. Also shrink their damage box.
  * Fix enemy Sea Urchins to properly deal damage when touched from the sides. This was a really tricky problem.
  * Fix enemy Spiny Beetle crashing the game when carrying its carried object from the current field in Classic Camera.
  * Fix enemy Spiny Beetle AI behavior to act much closer to how they do in the original game.
  * Fix enemy Three-of-a-Kind face transitions to be 0.25 seconds instead of 0.50 seconds. This is closer to the original game.
  * Fix enemy Three-of-a-Kind to drop hearts when matching three hearts and drop rupees when matching three diamonds.
  * Fix enemy Vacuum from causing Link to get stuck in a specific direction if it fell outside the camera viewport.
  * Fix enemy Vacuum to properly Spin Link and his sword when dashing with the Pegasus Boots.
  * Fix enemy Vire death when falling into lava or when burning. The bats it spawns were not destroyed and would not trigger events.
  * Fix enemy Zombies from spawning inside walls, from under steps, and from stairs objects by adding a special collision.
  * Fix miniboss Master Stalfos to have a longer delay before his entry dialog. Also fix him dealing damage to the player during flee.
  * Fix miniboss Dodongo Snakes from resetting current music to dungeon music while they were not encountered.
  * Fix miniboss Smasher when both Link and him pick up the ball simultaneously. Also the ball spawns a fairy when destroyed.
  * Fix miniboss Giant Buzz Blob to remove its damage field after death if it dies while in the "Buzz Blob" form.
  * Fix boss Moldorm to have the "ticking" sound effects. Also increase it's knockbacks and damage box size.
  * Fix boss Angler Fish arena camera to not fall below the boss field which is just a black screen.
  * Fix boss Slime Eel missing explosion sound effects and fix the duration before it starts to attack.
  * Fix boss Facade missing "hit" sounds. When hitting it with a bomb, it would play no sound effect at all.
  * Fix boss Evil Eagle cutscene to play at the same position on the ladder as the original game instead of on top of the tower.
  * Fix boss Hot Head missing explosion sound effects.
  * Fix boss Hardhit Beetle shots to despawn when hitting the wall so they don't fly into the ethos.
  * Fix final boss Giant Zol from being able to be spammed with Magic Powder which can result in an instant kill.
  * Fix final boss Shadow Ganon form to take the correct amount of damage. It should always take exactly six hits to defeat.
  * Fix final boss DethI arms falling behind the shadow layer around the Egg boss room by removing the shadow layer.

### **Overworld:**
  * The fisherman on the boat on the "Bridge" map is now drawn on a lower layer than Link so his sprite is always behind Link.
  * Signposts can now be targeted with the hookshot to pull Link. This has no practical use except that it's fun.
  * Disable the 2D directional hack when trading the fishing hook to the fisherman for the necklace/pink bra.
  * Link gets a slight "push" when jumping into water. The push when walking into water was slightly reduced.
  * Play the secret sound in Richard's Villa when pushing the box to open the passage to the Slime Key.
  * Add a secret sound to the shortcut passageway near Level 4 dungeon when pushing the rock into the hole and creating a shortcut.
  * Update Bow Wow's teleportation behavior when jumping into a world teleporter. It's still not perfect but improved.
  * Bow Wow now has water effects when in deep water. Includes a water effect around him and a splash is played when bouncing. 
  * In the cave passage to Tal Tal Mountain range, respawn the skull in the room with the large hookshot gap and chest.
  * Prevent players from jumping to Marin when stuck on the bridge by adding an invisible barrier, forcing hookshot usage.
  * Don't spawn the owl if Marin is saved on the bridge and Level 8 dungeon was completed before Level 7 dungeon.
  * The mountain bridge photo now requires no followers to be present and the Spiny Beetle must be killed beforehand.
  * Throwing the rooster over land no longer causes Link to briefly slide. Throwing it over water the slide was reduced by 50%.
  * Increase the distance when jumping off of cliffs onto land or water below. Also try to push Link slightly when landing in water.
  * Reduce the one-way bridge collider depth to 3 (from 4) which allows throwing the flying rooster and still crossing it.
  * Seal up the bushes entrance to the hedge maze. It was possible to cheese past it and get the Level 3 Slime Key early.
  * Fix clipping off the raft by walking against the stairs just right. Also add a secret entrance to Rapids Ride using the rooster.
  * Add a secret entrance into Rapids Ride using the rooster. Can you find the secret entrance and collect everything there?
  * Cracked rocks will no longer show the message when holding bombs and ice blocks don't show the message if magic rod is obtained.
  * NPC facing animation has been made more responsive. Also add a new method for NPCs with only two facing directions.
  * Fix the Trendy Game NPC to have his animation where he appears to be rapidly running in place.
  * Fix ball kids running backwards during the "Bow Wow kidnapped" event when Link is approaching them from the north.
  * Fix the rooster from being drawn in the background layer when borrowing it from the hen house (after dungeon 8 is complete).
  * Fix crossing a hole reset point while flying with the rooster from storing the Z position which reset Link in the air.
  * Fix the stone in the forest passageway to the bat (Li'l Devil/Mad Batter, whatever you want to call it) to play the secret sound.
  * Fix number of bushes outside Sales House O'Bananas so that there is two blocking the path instead of three.
  * Fix jumping into water. It would play Link's "stand" animation for a single frame before it played his swimming animation.
  * Fix sequence of trading bananas to monkeys with Classic Camera enabled by adding monkeys to always animate list.
  * Fix the monkey respawning when fighting with Bow Wow, Classic Camera is enabled, and a field change takes place.
  * Fix the owl "freeze" trigger to only not apply during the egg opening sequence and the end credits.
  * Fix trading the stick to Tarin for the honeycomb. Talking to him would just show a message that says "error".
  * Fix the goat at animal village to be able to turn to face the player. This required implementing her sprites and animations.
  * Fix bushes to show the proper smoke graphics and sound effects when sprinkling Magic Powder on them.
  * Fix bushes & rocks from respawning that cover stairs when changing fields when Classic Camera is active.
  * Fix bushes & rocks that open up into passageways to play the "secret sound" when revealing the staircase.
  * Fix the dresser in the Raft House to have the same interaction message as all other dressers.
  * Fix the camera in Manbo's cave to not fall below the cave's main field which is just a black screen.
  * Fix several tiles of land north of the lake near Dungeon 3 to be diggable. Trying to dig them with the shovel would repel it.
  * Fix desert quicksand at Moldorm fight to continue pulling link during the dialog box after defeating it and grabbing the key.
  * Fix visible tiles in the desert cave that make it apparent there is a secret room in the cave.
  * Fix Tal Tal Mountain Range hole entry into the water cave to show a "falling" entry instead of just appearing in the cave.
  * Fix the cave where the Magnifying Lens is to not hide Bow Wow's chain before grabbing the Magnifying Lens.
  * Fix visibility of rooms behind bombable walls in a cave near pig moblins and a cave on the mountain with shallow water.
  * Fix missing stairs object in the same mountain cave with bombable wall which slows down movement when walking on stairs.
  * Fix collision above the cave just north of Mr. Write's house. With standard camera Moblins would often walk into the cliff.
  * Fix collision blockers in a cave on the way to Level 8 dungeon with a large gap. Flying could move the camera to a bad field.
  * Fix collision of the Hen House roof. It may have been possible to get into the sky with a clever jump.
  * Fix Hen House NPC to be at his original position and make it so cuccos do not collide with him (like the original game).
  * Fix setting most enemies on fire then resetting them by transitioning fields when Classic Camera is enabled.
  * Fix owl when breaking open the egg to spawn immediately after the song is played instead of from a certain distance.

### **Dungeons:**
  * When entering a doorway that closes behind Link, push Link forward into the room like the original game did.
  * Implement three additional missing Level 4 dungeon tile puzzle variations. This port only had a single puzzle implemented.
  * Handle the situation where someone completes Level 8 dungeon before Level 7 dungeon (as in setting the correct states).
  * Dungeon doors now make the same hollow "clank" sound when poked as bombable walls like the original games.
  * Jars that contain a fairy and are respawned no longer contain a new fairy after it has been respawned.
  * Pull bridges found in dungeon 8 now reset on a field change when Classic Camera is enabled.
  * Barriers have been added to always animate lists which should fix all the issues people experienced with them over time.
  * Ice Blocks (found in underground maps in dungeon 8) respawn when changing fields when Classic Camera is active.
  * It's now possible to return to the Great Fairy in the Color Dungeon to restore clothes back to green.
  * Fix the falling animation when being dropped into a 2D underground map: Link flips and faces north.
  * Fix visible tiles in the Color Dungeon that should not have been visible near where the nightmare key was obtained.
  * Fix horse heads to not be able to clip through the wall if standing beside/parallel from the wall and throwing it.
  * Fix dungeon doors so that they consume small keys again. This was a huge game-breaking issue.
  * Fix dungeon teleporters (spawned after defeating mini-boss) to not teleport Link if he's standing on it when it spawns.
  * Fix several 2D maps in dungeon 3, dungeon 6, and dungeon 8 to not change and update Link's position on the minimap.
  * Fix the pull levers found in dungeon 4 and dungeon 7 to not be able to be grabbed from the sides.
  * Fix dungeon 1 to remove a crystal structure that did not exit in the original game. It could also get in the way of Classic Camera.
  * Fix dungeon 2 underground area that has a vase to be able to throw the vase while climbing a ladder.
  * Fix dungeon 3 bombable walls around the nightmare key room to not have cracks. This is how it was in the original game.
  * Fix dungeon 4 button near the water so it's possible to walk onto the button from the water.
  * Fix dungeon 5 nightmare key room to be blacked out in normal camera. This room is supposed to be hidden from the player.
  * Fix dungeon 5 entrance so that there is enough space to stand and play the ocarina before entering the dungeon door.
  * Fix dungeon 5 missing leave button that resets block positions and push multiple buttons inward towards the room to reset sooner.
  * Fix dungeon 5 hookshot anchors (koopa faces) to have collision (they have two types: hookshot collision + movement collision).
  * Fix dungeon 7 main keyhole to open up the dungeon so that the key can only be inserted from the front.
  * Fix dungeon 7 missing collision near torches and bombable wall in the small block passage in the room with the final pillar.
  * Fix dungeon 7 3rd floor before the pillars are brought down to more closely match the original layout.
  * Fix dungeon 8 bombable walls around the switch to not have cracks. This is how it was in the original games.
  * Fix dungeon 8 block to be movable near a stairway in a room north of the map with the Peahat enemies.
  * Fix dungeon 8 chest spawned from Dodongo Snakes to where the snakes must be killed from the ledge above.
  * Fix Egg dungeon when Classic Camera is enabled. Two of the paths would not take the player to the jump at the end.
  * Fix Egg camera transition to the final maze room when Classic Camera is enabled. It would not transition smoothly.
  * Fix Egg boss room from showing a black circle in the top left corner while the dialog is shown.
  * Fix Manbo's Song when played in the Egg to teleport the player to the entrance and not the pond.
  * Fix running into final stairs with boots which doubled the player's animation speed and bounced off the wall at the end.

## **v1.4.9**
### **The "Beginning of the End" Update**
  * Add a new Audio option "Classic Music Cues" which slightly alters the overworld music cues to match the original game.
  * Add a new Game option "Select Last Save" which automatically selects the last accessed save when first loading the main menu.
  * Maximum game scale can now be adjusted via "Game1.lahdhmod" file. This can also enable "editor mode" more easily.
  * Stones created by object respawners are no longer respawned on a field change when Classic Camera is active. This prevents potential duplicates.
  * Cracked floors reset on field change with Classic Camera. They reset after 15 seconds with the modern camera but not with Classic Camera.
  * Holes created after chopping down a bush are now despawned on a field change when Classic Camera is enabled.
  * Digging holes with the shovel are now also reset after a field change when Classic Camera is enabled.
  * Tarin now spawns next to the honeycomb tree after Level 3 is completed rather than after collecting the stick.
  * Enemy Spiny Beetle is now respawned on a field change when Classic Camera is enabled since they do in the original game.
  * Enemy Blade Traps now use a predictive movement system rather than collision detection. This prevents movement when there shouldn't be any.
  * Reduce the detection range of sword bearing enemies from 80 down to 60 which more closely matches the original game.
  * The option "Disable Helper Messages" also blocks the dialog text for the nightmare door when missing the nightmare key.
  * Nerf the signpost maze by allowing the current sign in the sequence to be read multiple times instead of failing.
  * Bushes now deal damage when throwing them at enemies. However, they can not destroy other bushes like rocks do.
  * Flying with the rooster and then throwing it transfers some of that velocity to Link like the original game.
  * Bridge colliders (one-way colliders) no longer stop Link + rooster from flying from the water and onto the bridge.
  * Remove collision from the fisherman on the boat near the bridge since he didn't have any in the original game.
  * Fix the jump hack from preventing the dying animation from playing if the player dies while landing on the ground.
  * Fix alligator trade sequence (canned food for bananas) where Link could move and get stuck just as the can is tossed into the air.
  * Fix item sticking to Link's head if activating the toadstool immediately after picking up an item (like Piece of Power or Guardian Acorn).
  * Fix Marin's forward and rear facing jump animations. The animations existed but played the wrong frames.
  * Fix the stick being skippable by jumping over it. A collider is now spawned behind it so the player can't jump past it.
  * Fix resetting the sword charge counter when dashing with Pegasus Boots. If sword was charged and interrupted, dashing would make it flash.
  * Fix potential softlock in Animal Village with heart piece if jumping down without bombs. A skull was added to the exit that has a bomb in it.
  * Fix seashell mansion where Link should be frozen but could change his direction as the meter is counting up.
  * Fix missing Buzz Blobs near Animal Village and missing Mushroom Bomber near the river passageway close to Animal Village.
  * Fix a single tile outside of the cave to not play Animal Village music when new option "Classic Music Cues" is disabled.
  * Fix enemy Anti-Kirby vacuum sound effect to be silenced if the player dies while it happening. This prevents an endless loop of the sound.
  * Fix enemy Arm Mimic movement animations fighting between two directions when walking diagonally.
  * Fix enemy Bomber bombs to despawn when changing fields in Classic Camera. Also always spawn a fairy when killed with Boomerang.
  * Fix enemy Cheep-Cheep stomp conditions and remove its damage field component after it has been stomped.
  * Fix enemy Flame Trap flames to persist through playing the ocarina. This exploit allowed the player to skip the mirror shield requirement.
  * Fix enemy Pairodd teleport activation range to be closer to the original game. This seems to almost perfectly duplicate behaviors.
  * Fix enemy Sea Urchins to deal damage from the side when blocking. This is not perfect by any means but it's an improvement.
  * Fix enemy Pols Voice attack sound effects. Stun items were making the metallic "ting" sound and sword had all the wrong sound effects.
  * Fix enemy Spiny Beetle to be able to fall down holes by fixing it's size dimensions. Also fix it's reset with Classic Camera.
  * Fix dungeon 3 blocks to reset positions when walking over certain leave buttons.
  * Fix dungeon 5 blocks to reset positions when walking over certain leave buttons. Also fix door to shut after leaving passageway.
  * Fix dungeon 7 keyhole to show the "missing key" message when the player does not have the bird key.
  * Fix the Egg follower turnaround to handle the situation of holding the rooster while activating it which stuck Link in the air.
  * Fix collision of the wall behind the bird key in the cave that it is found that allowed the player to walk out of bounds. 
  * Fix collision of Kanalet Castle eastern wall. This is a v1.0.0 bug so the collision was never implemented to begin with.
  * Fix partially "visible tiles" in the Magnifying Glass cave that should not have been visible.

## **v1.4.8**
### **The "Reaching the Final Stretch" Update**
  * The "Always Animate" system has been overhauled to be safer and more reliable and more objects have been added to it.
  * Classic Camera: Turn "Classic Border" into a slider from a toggle and add "Super Game Boy" border option.
  * Classic Camera: Field transitions are now more accurate. Objects that reset on field transition now happen during the transition.
  * Classic Camera: Several objects are now respawned on field transition: grass, bushes, crystals, rocks, pots, etc.
  * Classic Camera: Throwing most destructible objects now shatter against the edge of the field.
  * Bow Wow can now be taken as a follower after Level 8 has been completed and "Nothing is Missable" is enabled.
  * Add a block reset in dungeon 2 nightmare key room with the Pols Voice, Keese, and Stalfos.
  * Dungeon 3 map has been rebuilt to be a single map instead of 4 smaller maps which had an incorrect minimap. 
  * Nightmare key is no longer consumed on usage. This is not retroactive and only works on future dungeon clears.
  * Marin can now swim in deep water. This is only possible after Level 8 and "Nothing is Missable" is enabled as Marin will join as a follower.
  * The flying rooster can now be picked up while swimming like the original game.
  * Created redrawn icons for the built-in map editor. It appears they were upscaled which made them look blurry.
  * Adjusted the formula for how 2D jump height is calculated based on how long the button is held for more variation in height.
  * Bow Wow can eat underwater fish and has a 50% chance to eat something immediately after a map loads in.
  * Created a "no followers" object that is placed on maps to disable followers rather than a hardcoded map list in the game code.
  * Fix disabling Bow Wow push and hit components immediately after rescuing him from the moblin cave.
  * Fix the bed transition when entering the dream in the Dream Shrine. Link would jump on the bed and just stand there.
  * Fix a soft lock in Seashell Mansion where the player could jump over the door and into the abyss.
  * Fix Trendy Game crane to start playing its "activation" sound immediately after picking up an item.
  * Fix playing "item get" sound when unlocking the photo album after the very first photo event.
  * Fix enemy Pairodd activation range to be closer to original game. Also conveyor belts should not affect them.
  * Fix Pairodd to not shoot a projectile immediately after resetting the room they are in when Classic Camera is active.
  * Fix some incorrect positions of enemies on different maps and fix them activating too soon.
  * Fix dungeon teleporters to drop an item when teleportation to begins. This affected all dungeons except (old) dungeon 3 and dungeon 7.
  * Fix dungeon 2 room with a Spark and dungeon barriers where the Spark wouldn't take the correct path.
  * Fix dungeon 6 door condition by room near entrance with 3 Wizzrobes. It is now possible to exit through that door later on.
  * Fix miniboss music continuing to play when leaving the room and it wasn't defeated. Affects Dodongo Snakes (dungeon 3/6/8) and Hinox (dungeon 7).
  * Fix similar but slightly different miniboss scenarios: Lanmola (from the desert) and Turtle Rock (entrance to dungeon 8).
  * Fix various stones/blocks in different maps that should either be or not be pushable compared to original game.
  * Fix cucco attack swarm to stop on field transition, not freeze on the screen edge (both Classic Camera), and reset the attack counter when done.
  * Fix a potential "hitch" in the air when falling jumping/falling into 2D maps from a hole above (such as dungeon 1 Moldorm pit).
  * Fix keyboard keys when in editor mode to actually map to a real keyboard.
  * Many other small fixes and tweaks spread out everywhere. This version had too many changes to keep track of everything.

## **v1.4.7**
### **The "Feature Complete" Update**
  * Implement system to reset enemy positions when changing fields when Classic Camera is enabled.
  * Options menus finally have tooltips implemented. Press the top face button on the controller to view them.
  * Pressing LT/RT to change the scale can now be held down to rapidly shift through scaling values.
  * Dark rooms in dungeons 2, 6, and 8 have been made slightly darker to encourage powder usage.
  * Fix scale changes during Classic Camera. The controller buttons are supposed to be blocked when it's active.
  * Fix regression of some of the "always animate" objects to not hang around after death.
  * Fix "ghost" Ravens from somehow activating between map transitions (related to above fix).
  * Fix sword charging NPC avoidance to not detect NPCs that are currently not active on the game field.
  * Fix even more animation quirks when jumping > landing > performing action for both 2D and 3D Link.
  * Fix the shield from interacting with and bouncing against gravestones that spawn Ghini enemies. 
  * Fix missing collision field in the top left corner of Kanalet Castle upstairs map.
  * Fix Stalfos enemies getting stuck in a nearby field when damage launching them into it.
  * Fix transitional weirdness when Classic Camera is enabled on the "bridge" map (the one with the fisherman under the bridge).
  * Fix missing pushable component on Goponga Flowers which made it so the shield does not "bump" off of them.
  * Fix Spark hitbox so it's somewhat closer to the original game (differences mean it is not perfect).

## **v1.4.6**
### **The "Stayin' Alive" Update**
  * The "always animate" code has been optimized and updated to be far more reliable and is also applied to the "normal" camera.
  * Added several more objects to the always animate list: projectiles, boos, falling rocks, butterflies, crows, ravens, and more.
  * Reworked the code for picking up objects. Instant pickups work with a button tap, full animation works, and fixes issues with Spiny Beetles.
  * Reworked the code for Ocarina listening and fixed an issue with playing the ocarina a tile down from the Egg with Classic Camera.
  * Enemy Crows and Ravens now despawn after they fly so far away from Link's current position instead of persisting forever.
  * Enemy Ravens now have a 1 second start delay after the map load to prevent some weird transition bugs.
  * When Link "drowns" in either water without flippers or within lava tiles, he is pushed further into the water/lava.
  * Fix animation issues with Link when landing + performing 1 of these actions on the same frame: magic powder, boomerang, or poking.
  * Fix enemy Spiny Beetle from repelling the sword while vulnerable. It could still be attacked so this bug was strange.
  * Fix enemy falling rock spawners to not reach over into the egg stairs when using the normal camera.
  * Fix several cave map terrains where cliffs pop through the surrounding darkness.
  * Fix Kanalet castle from acting as a "dungeon" object and showing a blank map.
  * Fix shadow tiles overwriting ground tiles in Dungeons 1, 2, and 6.
  * Fix issues with rounding errors for the cloud textures in Dungeon 7 boss map. 
  * Fix misplaced eyegore statue in Dungeon 4. This statue is located just above the flippers chest.
  * Fix Classic Camera + Dungeon Only options to work when inside the egg and the now "not dungeon" castle.

## **v1.4.5**
### **The "Even More Maintenance" Update**
  * More Classic Camera fixes: Keep more objects "alive" offscreen, fix navigating the egg, disable during the ending.
  * Add "lahdmod" support for the textbox and the HUD. These can be used to customize the size and positions.
  * Start to skip dialog boxes won't skip options. Options can only be selected with the confirm button.
  * Implement the Flying Roosters jump that he does when Link jumps or jumps off walls.
  * Objects that have instant pickup such as Flying Rooster, Spiny Beetle, Genie Lamp, Balls, etc. are now actually instantly.
  * Fix jumping > landing > walking appearing as if Link is sliding across the ground briefly.
  * Fix enemy "Spark" damagebox to match original game.
  * Fix enemy "Stalfos" to be able to be damaged while it is jumping.
  * Fix Dungeon 5 dive entrance to boss key to only a single tile.
  * Fix a missing tile on Dungeon 5 map.
  * Fix two missing stairs objects on Dungeon 7 maps.
  * Fix Dungeon 7 room where horse heads can be tossed outside the room.

## **v1.4.4**
### **The "Mandatory Maintenance After a Big Update" Update**
  * Great Fairy music is now based on the current field like the original game rather than range based.
  * Update stairway in Dungeon 4 from the underwater key room to exit to the left instead of the right.
  * Update stairway in Dungeon 5 near Gohma midboss to exit to the left instead of the right.
  * Update teleport transition in Dungeon 6 while Classic Camera is enabled to be quicker and have a scrolling effect.
  * Slightly increase the Ocarina listener rectangle for objects that require playing the ocarina.
  * Vacuum enemies can no longer be killed with sword shots and fix a bug where direction can get stuck if killing while vacuuming.
  * Fix crash with Marin-Walrus sequence and slightly adjust the parameters so it works with Classic Camera.
  * Fix crash when returning the ghost to his grave. This is the same crash as with Marin and I have no idea why it started happening.
  * Fix BowWow from getting stuck in a previous field when Classic Camera is enabled.
  * Fix several maps that did not spawn BowWow. Also fix it so a "ghost BowWow" does not spawn.
  * Fix the water under lilypads in Goponga Swamp to be animated.
  * Fix ball playing children music change to change back when changing fields when Classic Camera is enabled.
  * Fix push block on the way to the feather in Dungeon 1 to have the correct push directions (was stuck towards door).
  * Fix Lower Face Shrine mural sequence to not close out the image prematurely allowing the dialog to finish.
  * Fix misplaced collider on Tal Tal Mountain Range just under the bird key cave near the triple waterfalls.
  * Fix Dungeon 7 pillars so that they can be hit from behind with the ball.
  * Fix Dungeon 7 boss Evil Eagle issues: Classic Camera, freezing player during intro, attack locking, and reduce fly away time.
  * Fix mountain photo bridge sequence to work with Classic Camera mode.

## **v1.4.3**
### **The "Nostalgic Screen-Effect" Update**
  * Scaling has been reworked to scale based on the resolution of the original game. Game scale now goes to 20x.
  * Implement a "Classic Camera" that aims to replicate the screen-scrolling based system of the original game.
  * Add a Classic Camera option "Dungeon Only" which only applies it when inside of dungeons.
  * Implemented a "graying out" system for currently invalid options. For example, some options don't work with Classic Camera (and vice versa).
  * Movement has been modified to be more like the original game. "Classic Movement" option only restores the ability to "moonwalk" (walk backwards).
  * Jump height is now variable when holding the button down longer when on 2D maps.
  * When charging + jumping, a brief "standing" animation is played in between so Link doesn't appear to "slide".
  * Boomerang can now be thrown in any direction at any point. It now relies on controller direction instead of Link's current direction.
  * Dungeon 7 tower state is now reflected in the overworld. When all pillars are destroyed, it appears shorter.
  * Falling down a hole in the bird key cave now leads back to the waterfall below it on the overworld.
  * Marin's singing is now based on field rectangle instead of distance. This is more accurate to the original game.
  * Freeze the game while teleporting after playing Manbo's song. Game resumes when teleportation ends.
  * Instruments can no longer be collected with items like the Boomerang and Hookshot.
  * When adjusting scale with the controller, it should now save when quitting. Previously it only saved when adjusting from menus.
  * Zombies no longer spawn when the game is currently frozen such as playing ocarina or when owl is speaking.
  * Remove Hardhat beetle initialize state which did nothing. This affected Classic Camera where it would just stand there for a bit.
  * Fix playing the jumping "flip" animation when cancelling a sword charge while in air.
  * Fix the "falling" animation while in 2D mode. It should play a flip even when not jumping.
  * Fix diving and then walking to land causing permanent invincibility until walking into the water again.
  * Fix another animation quirk when attacking then jumping as the animation finishes which would cause Link to stand in air.
  * Fix boomerang and hookshot when collecting fairies to not sometimes blast them into outer space.
  * Fix a black spot on the overworld where there should have been ocean on Tal Tal Mountain Range.
  * Fix partial tiles popping up in the edges of some caves.
  * Fix playing ocarina + quitting to cause 8 seconds of invinciblity when game resumes.
  * Fix the white fade out effect when using a dungeon teleporter after playing Manbo's song.
  * Fix dungeon minimap to properly scale with UI scaling. I missed this when fixing main map and inventory.
  * Fix instance in Dungeon 1 and Dungeon 6 where Link should be able to squeeze through but couldn't in this port.
  * Fix Dungeon 2 and Dungeon 6 minimap chest locations.
  * Fix missing tiles in Dungeon 5 map. 
  * Fix Dungeon 5 crystal structure hitboxes so they don't interrupt sword and so they can be smashed from more angles.
  * Fix Dungeon 5 Gohma miniboss to behave accurately to the original games when playing the ocarina.
  * Fix Dungeon 6 instrument room door closing behind the player after the instrument has been collected.
  * Fix falling through certain flying tiles in Dungeon 6 that are covering up holes.
  * Fix a door in dungeon 7 floor 1 blocked by a barrier to actually stop Link from crossing it when it's up.
  * Fix Dungeon 7 room with horse heads so that they can not leave the room. Previously they could then they would bug out.

## **v1.4.2**
### **The "On the Road to Glory" Update**
  * Note: Classic Camera has not made it into this release as it still needs more work.
  * Implement Indonesia language + textures. Thanks to [PangranggaCLAMP](https://github.com/PangranggaCLAMP) (creator).
  * Exclusive Fullscreen mode has finally been fixed! There is a slight delay entering it when booting the game with it enabled.
  * Huge map overhaul: add all the missing details of "upper levels" instead of "black" nothingness.
  * Initial window size has been modified to be a 6x integer scale of the original game's resolution (+widescreen).
  * Add more sword beam options to "ObjLink.lahdmod" file. Needs to be redownloaded from "Discussions" page.
  * Update several maps so that their fields line up with with the "screens" of the original games.
  * Make all photo sequences time out instead of waiting for a button press to prevent accidental skips.
  * For other image events (Christine photo, Face Shrine mural, alligator painting) add a delay before button exits.
  * Rework the Level 2 sword beam to behave as it did in the original games + extend travel distance.
  * Modify holes again: more accurate offsets, removed respawn hack, updated alternate respawn points.
  * Add a new dedicated drowning animation and rework drowning to be frame based instead of timed.
  * Move some of the settings around on the menus and create a new "Camera" settings menu.
  * If trading the fisherman for the necklace and leaving the scren before grabbing it, respawn it on next entry.
  * Add new animations when charging the sword while jumping and fix the transitions of animations between states.
  * Fix inventory and map overlays to correctly scale when changing the UI scale.
  * Fix more enemies to not deal damage or bounce when they are either burning or dying. 
  * Fix being able to pick up the Flying Rooster while he is bones and instantly resurrecting him.
  * Fix jumping out of the water when the movement speed modifier is 70% or greater when in 2D mode.
  * Fix jumping out of the water to not play the jumping sound effect which lead to it quickly playing twice.
  * Fix changing the attack direction while the button is held when in 2D mode.
  * Fix taking continuous damage when playing the ocarina while simultaneously colliding with an enemy.
  * Fix the Horse Head puzzle pieces from being able to be thrown through a nearby wall.
  * Fix Bow-Wow to be able to eat the monkey on the beach and eat the Winged Octoroks without them jumping.
  * Fix a Level 7 barrier from disappearing when pushing a block near it close to the top-right pillar.
  * Fix the Level 8 hacks that set the position to the front door so it doesn't force the player to leave the dungeon.
  * Fix the hookshot from being interrupted when moving left or right when in 2D mode.
  * Fix missing or misplaced chest icons on dungeon maps. This affects Level 2, 3, 5, and 8.
  * Fix Water Tektites with new collision type so they can not travel outside of the water and onto land.
  * Fix the sound effect that is played when picking up chickens to play the correct sound.
  * Fix drowning in water to not deal damage. Drowning in lava still deals damage however.
  * Fix the "Save & Continue" button from activating the boots when on the confirm button.
  * Fix a small error in the Pineapple sprite on the font texture.

## **v1.4.1**
### **A Maintenance Update**
  * Update the new hole code so that the bottom bias does not affect anything other than Link.
  * Move the new "NPC" type collision to the bottom of the enum list. This fixes a bug with the Photo Mouse.
  * Fix a bug that caused the NPC avoidance code to trigger even when an NPC wasn't spawned in.
  * Add support for a bunch more "lighting" lahdmod files so a new "no lighting" modpack can be created.

## **v1.4.0**
### **The "Sound is a 'Hole' Lot Better" Update**
  * Update holes so they act closer to the original game. The pull "size" is now accurate down to the pixel.
  * During the intro sequence, play Marin's missing animations. This is the only place in the game she has them.
  * When charging the sword and approaching a NPC, cancel the charge until not facing them (original game behavior).
  * Add support for Link mod file "ObjLink.lahdmod" file which can configure various settings for Link.
  * Add support for enemy mod file "ObjLives.lahdmod" file which can configure HP for individual enemies.
  * Update mod file "ObjDungeonBlacker.lahdmod" to include all maps where the lighting can be modified.
  * Don't play the "menu back" sound when exiting the name entry page via "special name" entry that plays music.
  * Add flashing effect to the sword shot like in the original games. This is the same flashing effect as when charging.
  * Fix various popping and distortion found in various sound effects: 77 sound effects fixed in total.
  * Fix various terrain differences around Kanalet Castle and Ukuku Prairie. The map almost appeared "unfinished".
  * Fix the flowers near Goponga Swamp to have their animation. Also add unused flower as an easter egg (somewhere).
  * Fix the animation speed for all flowers in the game. While some had the proper speed, others were over double.
  * Fix Marin wall clipping issues when she is a follower by reducing her body box size to match Link's.
  * Fix Marin cliff jumping sound effect to use the proper sound when she is a follower.
  * Fix missing collision near the cave to the right of the egg allowing the player to walk into the sky.

## **v1.3.9**
### **The "Old is New" Update**
  * Modify sword hitbox so it hits enemies at any "height" like the original games.
  * Add "Classic Movement" option which makes the movement feel much closer to the original games.
  * Implement more sound effects for menus: slider uses "selection sound", back uses "menu back" sound.
  * Support fairy mod file "ObjDungeonFairy.lahdmod". Enables sword collection/heal amount.
  * Dialog box text scrolling can be proceeded by using either the confirm or cancel buttons.
  * Movement speed modifier now affects more movement actions. This is probably still incomplete.
  * Fix sword to be able to grab items that end up on higher cliffs which was possible in original games.
  * Fix sword flashing when starting a game and using pegasus boots before swinging the sword.
  * Fix Marin to have her animated "idle" state when facing forward which has been missing since the beginning.
  * Fix Marin following Link through overworld teleporters. Not sure when this broke but it's fixed now.
  * Fix Marin from double spawning when taking a walk at the end of the game and sending her back.
  * Fix lifting the pot in 2D mode in the second dungeon which is required to proceed. Epic fail.
  * Fix facing direction when charging the sword in 2D mode. Like the above, this was due to 2D direction hack.
  * Fix missing title screen if pressing start when the intro fades to white. This has existed since v1.1.0.
  * Fix the landing sound effect not playing when jumping on cracked floor tiles (dare be invisible holes).
  * Fix in-game Settings menu from closing when pressing "left" on dpad or analog.
  * Fix Yes/No menu when choosing Quit/Exit game to not have false up/down selections (don't play a sound).
  * Fix audio distortion or a "popping" sound at the beginning of the "menu back" sound effect.
  * Fix projectiles to collide with the "just a kid" NPCs found around Mabe Village.

## **v1.3.8**
### **The "Holes are not Cheat Codes" Update**
  * Fix a bug where falling down a hole could make the player invincible, including overworld portals.

## **v1.3.7**
### **Vanilla Fixes & The Skip Dialog Update**
  * Allow skipping dialog with Redux option "Enable Dialog Skip". When enabled, press "Start" to skip a dialog box.
  * When sprinkling magic powder on Tarin in raccoon form, prefer the powder item over interaction if they share "confirm" button.
  * Support custom lighting of maps through a mod file named "ObjDungeonBlacker.lahdmod" when placed in "Data\Mods" folder.
  * Followers will no longer prevent Link from trying to go up to the egg before Level 8 is finished.
  * Fix being able to force through to the egg when followers attempt to prevent Link from approaching it.
  * Fix Link trying to use the Shield or Pegasus boots if equipped on the "confirm" button and interacting with an object.
  * Fix spawning inside some doors when falling down a hole and the reset point was inside the door.
  * Fix a room in Dungeon 8 that should have been a dark room with torches but was fully lit up.

## **v1.3.6**
### **The "Perfect Shield" Update**
  * Add updated Spanish translations by [IPeluchito](https://github.com/IPeluchito).
  * Better command line support for loading save files: use the command "loadSave #" where # is 0-3 (for slots 1-4).
  * Rework the shield one more time. Bigger hitbox, direction never fails, no more false hits, and no more false blocks.
  * Small update to the in-game menu. Some options were shortened. Saving only happens when autosave is enabled.
  * Save file version has been incremented to version 2 due to world teleporter fix and v1 save files are set to v2.
  * Fix a game ruining bug where if saving and quitting while the world was "frozen" would permanently freeze it.
  * Fix world teleporter IDs which fixes the teleport order: Mabe village > Level 8 > Level 4 > Animal Village.
  * Fix potential softlock on some devices where sound could fail and the instrument sequence would not proceed.
  * Fix Richard's dialog to restart when denying his quest instead of always acting as if you accepted it.
  * Fix the hint Ulrira gives you about finding bananas to not get stuck if talking to the monkey before Richard.
  * Fix any languages that displayed the monkey's dialog incorrectly (some would exceed the bounds of the dialog box).
  * Fix loading the wrong photobook sprites for certain languages that currently support alternate sprites.
  * Fix the saving icon (disk bottom right) to always appear whenever the game is saved or an autosave takes place.

## **v1.3.5**
### **The "Controller Buttons" Update**
  * Trendy Game button textures now reflect the current controller and the "Swap Confirm/Cancel" setting.
  * When trading for the Boomerang, the item button the Goriya asks to trade for now reflects the current controller.
  * NPC/book dialog windows that reference buttons now reflect the currently selected controller.
  * Increase the velocity of using Roc's Feather in 2D underwater maps while swimming and play the sound effect.
  * Play missing sound effect when sworded enemies attack Link and it's blocked with the shield.
  * Add Totaka's song easter egg at file select screen. Start a new game and enter: Totaka, Totakeke, or MOYSE. Case insensitive.
  * Add a new easter egg that plays a remix of "The Legend of Zelda" theme by entering the name: Zelda. Case insensitive.
  * When setting dogs or chickens on fire, remove their components so they can no longer be interacted with.
  * Fix a rare game breaking issue that could happen when taking damage that caused the entire screen to go black or blue.
  * Fix entering Level 8 backdoors to properly set the last position when saving to the dungeon entrance.
  * Fix underwater attack with level 2 sword in 2D maps to be able to shoot sword beam both up and down.
  * Fix multiple instances of an invalid font glyph scattered throughout the German translation around the (ü) character.

## **v1.3.4**
### **Emergency Crash-Fix Update**
  * Fix crash that happens when attacking Pokey in Yarna Desert.
  * Adjust shield hitbox so that it completely covers the body box.

## **v1.3.3**
### **German Language Added Update**
  * Implement German language + textures. Thanks to [MasterPhW](https://github.com/MasterPhW) (creator).
  * Created language textures for French language that covers inventory, shops, map icons, photos, and title screen.
  * Support loading alternate images when changing language setting (like German and Spanish) for all supported languages.
  * Completely rework shield mechanics: hitbox reworked, no damage if object collides with both shield + bodybox.
  * New option to swap the "Confirm" and "Cancel" buttons. This works with any controller, and only swaps the functionality.
  * When enemies are defeated, remove all components. This prevents situations like bumping them when dashing with shield.
  * Add language option to load alternate scripts of the same language. Supports Switch remake and alternate scripts.
  * Implement more placeholder tags for font glyphs that represent font sprites like trade icons, marin face, etc.
  * Increase size of hitbox and bodybox of Arm Mimic. This makes dashing through them in dream shrine more like the original games.
  * Greatly simplify the hack fix used for when Link jumps over dungeon barriers as they are entering the raised state.
  * Make is so Link can lift up chickens and throw them with the Power Bracelet since it could be done in the original games.
  * Fix map icons for Mabe Village Shop to have "shop" icon instead of "!?" and Raft Shop to have "shop icon" instead of "cave".
  * Fix followers to not appear in certain maps. This includes fishing pond, shell mansion, dungeons, and dream shrine.
  * Fix a dungeon barrier in Level 7 to not pop through when a block is pushed over top of it by removing it.
  * Fix vacuum enemies in Level 2 and Level 8 to cause Link to properly spin when they are sucking him towards them.
  * Fix misplaced sprite shadows that spawn by the Level 8 sub-entrances when Dynamic Shadows is enabled.
  * Fix Level 8 midbosses Hinox and Rolling Bone to respawn after they have been defeated whenever the map is reloaded.
  * Fix final boss Ganon form weapon from potentially dealing double damage if both sides hit at the same time.
  * Fix Boomerang and Hookshot to not grab items that are flying and fix a crash with Hookshot when grabbing a fairy.

## **v1.3.2**
### **A Bug Fixes Update**
  * When handing the witch the toadstool to make powder, reset the music when it speeds up, and reset again afterwards.
  * Modify the movements of the Hardhat Beetle to more closely resemble the original game versions.
  * Reset the "free" camera when qutting to main menu and/or selecting a save file so it doesn't persist through saves.
  * Restore ability to set animals on fire with Magic Powder and Fire Rod.
  * Add option to completely disable animal damage in "Redux" options.
  * Fix crash from Hookshot grabbing fairies and modify the Boomerang to grab fairies instead of instant collecting them.
  * Fix crash with French and Italian languages in places where an invalid apostrophe was used.
  
## **v1.3.1**
### **Emergency & Sprite Shadows Update**
  * Implement many more sprite shadows. At this point it should be nearly feature complete aside from some objects.
  * Rename the Graphics Settings "Shadows" option to "Dynamic Shadows" since disabling still creates sprite shadows.
  * Fix the level 2 boss Genie from going invincible after the first hit after breaking his bottle.
  * Fix the level 7 boss Evil Eagle intro to wait until the top of the tower is reached.
  * Fix the Trendy Game crane to close back up after dropping an item off.
  
## **v1.3.0**
### **The "Even MORE Features and Fixes" Update**
  * Implement free camera which can be moved around with the right stick. Press in the right stick to recenter on Link.
  * You can enable this "free camera" by entering the "Game Settings" and unchecking "Lock Camera".
  * Implement a "sprite shadow" for certain game objects when dynamic shadows are disabled. This will be expanded on later.
  * Sword spin attack now spins clockwise when facing right like in original games. Other directions go counter clockwise.
  * Added missing sound effects when opening/navigating the island map from the inventory screen.
  * The painting by Schule Donavitch (the alligator with the hippo in Animal Village) can now be viewed.
  * Further reduce flashing effects with Epilepsy Safe: Upgrade Bat, Anti-Fairy, Spark, Giant Bubble, and Rooster Spirit.
  * Add "Save & Continue" option to save and keep playing. Rename "Quit to Menu" to "Save & Quit to Menu".
  * Play proper boss music during the level 8 boss Hot Head, which was playing the miniboss music.
  * When taking Bow-Wow for a walk, disable the "push component" which is what makes the shield "bump" him.
  * Bosses can no longer be interacted with in any way when they are defeated and in the "dying" phase.
  * Winged Octoroks now have an attack cooldown like regular Octoroks. This cooldown is also refreshed after jumping.
  * Fix Manbo's song to warp to level 8 entrance when entering the dungeon through the top two entrances from warp point.
  * Fix Gohma resurrecting and going invincible if playing "Ballad of the Windfish" when it is in "dying" phase.

## **v1.2.9**
### **The "MARIN IS FINALLY FIXED" Update**
  * Marin will no longer despawn after talking to her on the beach if playing up to that point uninterrupted.
  * Sword no longer blocks projectiles but a "Modifier" has been added to somewhat restore the functionality.
  * The "Sword Blocking" modifier can block projectiles when swinging, but can no longer block just by holding/charging it.
  * Add missing sound effects to "Vire" enemy in level 8 when attacking and when destroying it's fireball.
  * Don't preserve the "Thief" state when deleting a save with it and creating a new file in it's place.
  * Fill missing hearts when collecting the Level 2 Sword at Seashell Mansion.
  * Fix Like Like not stealing your shield when it is equipped to the top controller buttons as opposed to the face button.
  * Fix running through Like Like with boots which "traps" the player but keeps running invisible leaving a trail of smoke.
  * Fix several "trade icon" mistakes in various languages (especially Russian and Spanish) along with other minor fixes.
  * Fix Boomerang to deal damage to enemies regardless of the player Z-position. This mostly affected the blocks in level 7.
  * Fix the shield sometimes not playing the sound effect when pressing the button to block with it.
  * Fix travelling through an unspawned staircase in level 8 that could put you in the corner of the map.
  * Fix the "dungeon clear" music sometimes resuming after picking up the instrument at the end of a dungeon.
  * Fix applying sound effects volume at game load and when moving the slider before a save file is selected.

## **v1.2.8**
### **The "Sorry Marin I Give up For Now" Update**
  * Revert back to the old method to attempt to spawn Marin at the beach as this created more problems than it solved.
  * I have to give up on the "Marin not spawning" issue for now. I don't have a clue as to why this is happening to some people.
  * Fix the "Sound Effects" volume slider that has been broken since v1.1.0. So basically, the whole time.
  * Move the owl 8 pixels to the left when first entering Mysterious Wood to match his position in the original game.
  * Play the "bump" sound effect when attacking Goponga Flowers without being strong enough to destroy them.
  * Octorok's now have a cooldown between shots so they don't sometimes fire off like a machine gun.
  * Potentially fix the shield sound effect when blocking from sometimes not playing.

## **v1.2.7**
### **The "Missing Marin" Test Update**
  * Add alternate method to spawn Marin after the beach scene. Please report if it works or doesn't work!
  * This version is being released quicker than usual to get it into the hands of anyone who can test if new Marin method works.
  * Add a "Game Setting" to greatly reduce the flashing of shop keeper revenge and the bat that upgrades powder/bombs/arrows.
  * Add the flapping wing sound to Ravens which are the birds you can find around Turtle Rock.
  * Fix a potential crash when shooting bomb arrows into the mouth of Dodongo Snakes.
  * Fix Spiny Beetle sometimes losing its hitbox and becoming invincible if lifting the object off its back while moving.

## **v1.2.6**
### **The "Neverending" Update:**
  * NOTE: If Marin doesn't spawn after the beach scene, try a swordspin and report to me if it makes her appear or not.
  * Reset powerup counter when powerup ends. Fixes-example: Get acorn; Kill 11 monsters; Lose it; Kill 1 enemy; Another acorn.
  * Set Kanalet Castle upstairs button state to "pressed" after finishing level 2 so it's in the proper state.
  * Revert Arm Mimics to their previous behavior. Following facing direction when charging was not correct.
  * Add two extra cliff jumps to Tal Tal Mountain range near the hen house for slightly easier travel.
  * Modified the Ghost photograph to better match the in-game colors before the photograph is taken.
  * Don't play the "landing" sound effect when jumping directly into an overworld teleporter hole.
  * When hookshotting Iron Mask and stealing it's mask, play the "collection" sound effect (same as fairy).
  * Playing Ballad of the Windfish when fighting Gohma opens it's eyes for 1.5 seconds and doesn't shoot fireballs.
  * Add enemy wall between North/South Face Shrines to try to keep Mushroom Bomber from interacting when at South Shrine path.
  * Don't store save file and content paths in the "settings" file. It was counterintuitive when moving between "portable.txt".
  
## **v1.2.5**
### **The "Even More Polish" Update:**
  * A 'potential' fix for Marin sometimes disappearing when she joins you after the beach scene.
  * Fix music priorities between villages, powerups, and Marin singing. A combination of these could get jumbled.
  * Fix hearts to have the proper sound effect and use the old effect for when a heart heals.
  * Fix King Moblin to have more accurate behavior to the original game.
  * Fix Bow-Wow to have the proper sound effect when munching on an enemy.
  * Fix Crows to have the "flapping wings" sound effect loop when attacking.
  * Fix Kanalet Castle buttons (again) to hopefully properly close and open the front gate.
  * Fix playing the landing sound any time Link is airborne and hits the ground.
  * Fix DethI dying in a single hit from any damage source (only the boomerang should do this).
  * When spawning level 2 sword, freeze the player a little longer to prevent a glitch grabbing sword too early.

## **v1.2.4**
### **The "Final Touches" Update:**
  * Fully implemented the "secret ending" that is shown after the credits with 0 deaths.
  * The secret ending can also be viewed if the Redux option "Nothing is Missable" is enabled.
  * Add Spanish textures for title screen, menus, objects, items, various others. Thanks to [IPeluchito](https://github.com/IPeluchito).
  * Photographs are now sepia by default. A "Redux" option has been added to use colored photos instead.
  * Implement a "Modifiers" page that can alter the difficulty of gameplay.
    * Extra Enemy HP: Adds up to 30 additional hit points to all enemies and bosses.
    * Damage Taken Multiplier: Increases damage taken from 1x up to 10x.
    * Added Move Speed: Increases movement speed from 10% up to 100%.
    * No Damage Launch: Piece of Power / Red Tunic do not launch enemies.
    * No Heart Drops: Disable hearts dropping from all sources (enemy, bushes, digging).
  * Force Link into "idle" state with 0 depth when encountering the owl so he's not frozen in weird poses.
  * Reduce final boss "Giant Zol" chance to despawn after a jump from 50% to 33%.
  * Fix Dungeon 1 room with 4 Keese so that they can not fly outside the bounds of the room and trap you in.
  * Fix potential issue with Madam MeowMeow not getting excited when Bow-Wow is kidnapped.
  * Fix Red Zol from losing its hitbox and not splitting if damage was somehow less than 1.
  * Fix holes near Kanalet Castle leading to seashell by adding pathing blockers to the tops of the trees.

## **v1.2.3**
### **The Last Boss Update:**
  * Fix final boss "Giant Zol" form for real this time. It was getting stuck in the elongated state.
  * Increase chance "Agahnim" state of final boss will shoot exploding shot from 25% to 33%.
  * Stop music after "Lanmola" state of the final boss. It's supposed to be silent for DethI transition.
  * Fix DethI to be able to be damaged from bombs (more specifically, bomb arrows which I did as a kid).
  * Disable the low hearts alarm if the final boss is defeated while at low health so its not beeping through the ending.

## **v1.2.2**
### **The "Fix Everything" Update:**
  * Disable Helper Messages also includes completing heart containers and opening rupee chests.
  * Menu border in Redux options has been turned into a slider with a black border option.
  * Add translated text to the boat in the fisherman falling in the water photo.
  * Play the "secret sound" when monkey's finish fixing the bridge near Kanalet Castle.
  * When meeting Marin at the beach, play a jingle and silence the music to where only the ocean is heard.
  * Playing Ballad of the Windfish opens Gohma's eyes (mid-boss of Level 5 dungeon).
  * Add Horse Heads and Ball in level 7 to the list of instant pickup items.
  * Potentially fix cave on path to level 7 that is always bombed open before ever even visting it.
  * Fix hookshot getting stuck when picking up a powerup at close range by using a reset countdown timer.
  * Fix playing the correct overworld music after taking the photo with Marin at the beach cliff.
  * Fix dungeon music cues: silence after boss kill, clear music after boss defeat.
  * Put extra effort into dungeons/bosses: Slime Eel, Facade, Evil Eagle, and Hot Head.
  * Fix the timing of gobble/explosion sound effects when feeding Dodongo Snakes bombs.
  * Fix missing pathing blocker over torch in dungeon 2 room with skeleton, switch, and up/down blocks.
  * Fix the textbox background from going missing and inventory transparency when hiding the UI.
  * Fix Color Dungeon boss Hardhit Beetle from spamming the message about resetting when blue color.
  * Fix Hardhit Beetle projectile to be on a lower layer than link so it doesn't overwrite his sprite.
  * Fix editor not saving the position of "moveStone" in dungeons (another dig hole map bug).
  * Fix the exits of several caves and passageways to push you in the correct direction.
  * Fix picking up an invisible rooster in level 7. Yes he's always with you until the level is finished...
  * Fix Link sleeping sprite drifting upward under the right conditions when jumping into Dream Shrine bed.
  * Fix 1.2.1 bug: Button would not open the door outside of Kanalet Castle.
  * Fix 1.2.1 bug: Madam MeowMeow has the wrong dialog when village is under attack.
  * Fix 1.2.1 bug: Alligator trading sequence was broken. He would not ask for bananas.

## **v1.2.1**
### **The "Break Everything" Update:**
  * Don't play this version. It has some game breaking bugs that prevent progression.
  * Add redux option "Nothing is Missable" option. This does the following:
    * Shopkeeper name buyback has been added to this option since it's non-canon.
    * Seashells 5/10 at seashell mansion can be obtained even if you pass the markers.
    * A button is added upstairs to Kanalet Castle to close the gate (for Richard photo).
    * The flying rooster can be borrowed from the Hen House after level 8 is completed.
    * Marin will take a walk with the player for another shot at photos after level 8 is completed.
  * Modify drop rates of rupees and hearts: 30% chance for drop, 70/30 it will be rupee/heart.
    * Old formula had a 33% chance to drop rupees, so only a slight nerf there.
    * Old formula had hearts at 40% but AFTER rupees, meaning only a 7% chance so a buff there.
  * Don't stop spawning seashells when reaching 20 shells allowing all 26 to be collected.
  * Only allow pushing a single object at a time with a bias towards the closer object.
  * The UI can be hidden/shown with either the "Tilde (~)" key or the "Delete" key.
  * Piece of Power and Guardian Acorn will no longer drop if a powerup is currently active.
  * The total number of kills the player has made is now tracked in the save file.
  * Remove 3D effect from small rocks so they don't overlap the sword and remove their shadows.
  * Massive overhaul of "scripts.zScript" to make it more readable and contiguous.
  * Fix "error" message appearing after the final photo taken and any repeat events (like stealing).
  * Fix dialog background size when selecting between VWF and standard font.
  * Fix text of "Auto-Scale" for graphics slider to have a space after the colon.
  * Fix Crazy Tracy keeping her mid-conversation state between entering/exiting her shop.
  * Fix missing secret sound after sprinkling raccoon with powder and walking towards tail key chest.
  * Fix potential extremely rare crash with moblin-swords under the right conditions.

## **v1.2.0**
### **The "Redux" Update:**
  * Fix final boss "Giant Zol" form to have the correct graphics.
  * Rename Miscellaneous options to "Redux" options on the Settings menu.
  * Add Redux option to use a variable width font as is used in the romhack.
  * Add Redux option to disable helper messages. This encompasses a large number of popups:
    * Items: Keys, Compass, Map, Nightmare Key, Stone Beak, Seashell, Piece of Power, Guardian Acorn, Golden Leaf, and Piece of Heart.
    * Objects: Stones/Pots, Crystals, Cracked Rocks, Keyhole Blocks, Dungeon Keyholes, and Ice Blocks.
  * Add Redux option to disable censored content. This includes the hippo and mermaid sprites/text.
  * Added Hippo missing original standing sprite and implement animation states.
  * Alternate font versions for any combination of Variable Width Font and Disable Censorship.
  * Shoplifting photo has been updated to have the translated text from Redux romhack.

## **v1.1.9**
### **The "Endless Bug Fixes" Update:**
  * Fix Marin to teach the Ocarina song after being saved on the mountain.
  * Fix wrong enemy types in cave near pig moblins; from Buzz Blobs to Spiny Beetles in skulls.
  * Fix shield animation when charging while blocking when in 2D mode.
  * Fix losing powerups when transitioning between floors in Castle with the golden leaves.
  * Fix Dream Shrine bed transition again: additional states were added but not checked for here.
  * Fix destroyable wall sword poke sound effect to sound more impactful and not so soft.
  * Fix being able to pick up rooster's dead bones before he was revived with the song.
  * Fix photo mouse having no interaction and collision during fisherman photo sequence.
  * Fix heart piece location in castle moat to be in it's proper position one tile down next to wall.
  * Fix Arm Mimic facing direction when charging using same fix as Mask Mimic (Shy Guys).
  * Fix Pokey (desert cactus enemy) dying in a single hit with a sword spin even at level 1 sword.
  * Fix Crazy Tracy being able to talk to you from across her table. 
  * Fix missing Zora south-center of overworld near tiny island with bush & seashell.
  * Objects on top of Spiny Beetles are now picked up instantly without a direction pushed.
  * Karakoro (the ball-type enemy in the color dungeon) is also now instantly picked up.
  * Rolling Bone spiked roller will not interact with sword while jumping so it won't repel.
  * Master Stalfos can be damaged with level 2 sword beam. Behavior is as close to GameBoy as possible.
  * Don't interrupt sword charge on sources that spark when hit (spike roller, traps, masked enemies).
  * Tweak main menus to have more usable space and make sure bottom labels are never overlapped.
  * Added a space after the colon for GamePad on Game Settings page to match the language button.
  * Controller buttons on inventory screen were moved one pixel upward.
  * Dev: Fix map editor's ability to delete objects. Was broken with shovel hole changes.

## **v1.1.8**
### **The "Long-Standing Issues Fixed" Update:**
  * Updated Spanish translation. Thanks to [IPeluchito](https://github.com/IPeluchito).
  * Low Heart Alarm was moved to the "Audio Settings" page.
  * When fishing, give the fish just a little bit more fight, especially the lunkers!
  * Fix arrows freezing the game when shot in some caves due to changes in hole logic in v1.1.1.
  * Fix shovel holes from being overwritten by flowers while also being covererable by stones.
  * Fix certain areas on the overworld to be diggable that previously were not.
  * Fix Tarin spawn states during the trade sequence before/after trading for bananas. 
  * Fix behavior of Winged Octoroks to not jump and be invincible against sword spin attack.
  * Fix setting the reset point when falling down holes if jumping while transitioning "rooms".
  * Fix disabling powerup music to not play powerup music when transitioning dungeon floors.
  * Fix shopkeeper name buyback. Rupee/item checks have been broken since it's implementation.
  * Make dungeon 2 boss Genie Bottle instant pickup without needing a direction pressed.

## **v1.1.7**
### **The "Sword Fixes" Update:**
  * Modify attack sword hitboxes. Frame 1: hitbox added, 2: hitbox expanded, 3: hitbox reduced.
  * Modify the hitbox of bushes to match their sprite/tile size. This fixes wonky "cutting" with sword.
  * Allow collecting items with the sword when holding it out in charging state.
  * Level 2 sword shot now matches height of Link and doesn't collide when standing on raised blocks.
  * Fix the hookshot where it would never fire again when trying to use it with other weapons.
  * Item collection box height can exceed its width or 8. Fixes collection range of items like rupees.
  * Add a hack-fix when hitting the switch to raise blocks while jumping over them and getting stuck.
  * Renamed FPS Lock to "Vertical Sync" as this is a far more accurate description of what it does.
  * Make UI scale slider static 1-11, 11=Auto-Detect, and only apply scale if screen can handle it.
  * New brick texture for main menu that better matches DX on GBC. Can swap to old texture.
  * New Miscellaneous page. Currently only contains option to swap to old menu brick texture.

## **v1.1.6**
### **A "General Fixes" Update:**
  * Expand size of most UI elements on settings pages to better fix non-english languages.
  * Translate "Controller" on controller selection button on Game Settings page.
  * Restore fisherman's line "You have to have more passion. Live a little." when exiting pond early.
  * Add option to "Audio Settings" to mute powerup music (Piece of Power/Guardian Acorn).
  * Remember scaling settings on window resize when not set to Auto-Detect.
  * Change "Controller" selection text to "GamePad" in all langauges.
  * Fix potential crash when using items immediately after save is loaded.

## **v1.1.5**
### **The Features Update:**
  * Add controller type selection to Game Settings menu: XBox, Nintendo, Playstation.
  * Make UI Scale slider "Auto-Detect" the rightmost value.
  * Freeze the game world while playing the ocarina.
  * Hookshot can be retracted with a second button press.
  * Translate "Back" and "Select" on the menus. Thanks to [xsm2](https://github.com/xsm2).
  * Translate "Thief" across all language files. Thanks to [xsm2](https://github.com/xsm2).

## **v1.1.4**
### **The Bugfix Update:**
  * Implement French language. Thanks to [JC](https://itch.io/profile/jc2111) (creator).
  * Patcher now backs up unpatched files so future patchers don't need to rely on v1.0.0.
  * Creating "portable.txt" next to game executable restores saving to game folder.
  * Fix sprite of hole dug with shovel overwriting rocks pushed over it.
  * When attacking and standing still, it's possible to change facing direction.
  * Shooting arrows left and right when next to a south wall no longer triggers collision.
  * Goomba can no longer be attacked after it has already been stomped.
  * Hardhat Beetle is affected by knockback effect of piece of power and red tunic.

## **v1.1.3**
### **The Languages Update:**
  * Implement Spanish Language. Thanks to [Álcam](https://www.youtube.com/@Alcam211) (creator), [IPeluchito](https://github.com/IPeluchito) (assistance), [orderorder](https://github.com/orderorder) (suggestions).
  * Implement Italian Language. Thanks to [Ryomare123](https://itch.io/profile/ryomare123) (creator), [Ryunam](https://github.com/Ryunam) (assistance).
  * Implement Russian language. Thanks to [Dima353](https://github.com/Dima353) (creator), plus font files.
  * Implement Portuguese language. Thanks to [altiereslima](https://github.com/altiereslima) (creator), plus font files.
  * Fixed muting audio when window is out of focus if option is enabled.
  * Shadow toggle in graphics options. Note that it still needs work as it removes all shadows.
  * Playtime is now tracked and visible on file selection. Thanks to [LouisSF](https://github.com/LouisSF).  
  
## **v1.1.2:**
### **An Emergency Update:**
 * Don't apply smaller collision scale to holes already smaller than 16x16.

## **v1.1.1:**
### **A Minor Update:**
 * Reimplement the option to unlock FPS.
 * Fix alligator can throw with unlocked FPS (credit @buttcheeks69).
 * Fix incorrectly calculated custom text height for confirm button (credit @squiddingme).
 * More accurate timing when picking up sword on beach (credit: @buttcheeks69).
 * Show key counter on HUD when in dungeons (credit: @squiddingme)
 * Custom sprite for key counter.
 * Add option to display items in the bottom right (credit: @squiddingme).
 * Pay the Shop Keeper to no longer be called "Thief" after stealing.
 * Reduced collision size / pull strength of holes to more closely match the original game.

## **v1.1.0:**
### **General:**
 * Saves and settings files are now located in "..\AppData\Local\Zelda_LA".
 * Intro and title screen reworked to better match original game.
 * Always show the title screen even when skipping intro video.
 * Fix crashing and many other issues when starting in "Fullscreen" with "Borderless" disabled.
 * Keyboard controls will now properly save.
 * Swimming sound effect when in deep water was added.
 * Low hearts will play the beeping sound.
 * Photographs are now colorized.
 * Save files reference hearts/health as "maxHearts/maxHealth" instead of "Hearth".
 * New version compatible with old save files but not vice versa.
 
 ### **User Interface:**
 * Swapped the Start and Select buttons from the original release.
 * Quit button on the main menu after the title screen.
 * Restored commented out Audio Settings page and move volume controls there.
 * Restored commented out UI scale code and added it to Graphics Settings page.
 * Added option to mute sound when window is not active in Audio Settings page.
 * Rename "Back to Game" button text to "Return to Game".
 * Added "Save & Quit" button on the in-game pause menu.
 * Game scale can be adjusted with LT and RT buttons.
 * Low hearts beep can be disabled from game settings menu.
 * Screen shake when taking damage can be disable from game settings menu.
 
 ### **Items:**
 * Items can be assigned to two additional buttons: LB and RB.
 * Inventory cursor sound was added.
 * Items can be used when pushing against objects.
 * Cooldown of the sword has been removed.
 * The sword can be charged while using the shield.
 * The sword can be charged while jumping off cliffs.
 * The sword can be charged while underwater in 2D.
 * The shield can be used while charging the sword.
 * The shield plays the proper sound effect when blocking.
 * Bombs will appear in the shop after buying the shovel.
 * Bomb-arrows now deal full damage to the initial target.
 * Pegasus Boots directional influence has been reworked.
 * Bracelets pick up Flying Rooster or Smasher Ball instantly.
 * Piece of Power and Guardian Acorn can be collected with sword.
 * Piece of Power and Guardian Acorn proper sound and music delay.
 * Piece of Power and Guardian Acorn will not lose music in some dungeons.
 * Knockback effect (piece of power) of sword lvl 2 removed.
 * Knockback effect (piece of power) of red tunic restored.
 
 ### **Enemies:**
 * Stunned enemies no longer deal damage.
 * Enemies dying from burning no longer deal damage.
 * Goponga Swamp flowers can be killed with Lvl 2 sword + spin attack/piece of power/red tunic.
 * Blade Traps will now collide with pushed blocks.
 * Green Zol damage delay while spawning.
 * Shy Guy facing direction fixed while charging sword.
 * Pols Voice can be defeated with the ocarina.
 * Flying Tile sound loop now more accurate to original.
 * All bosses have had their damage field deactivated on death.
 * Lanmola body (worm in desert) can now deal damage.
 * Stone Hinox (color dungeon) AI fixed from deadlocking into "jump" state.
 * Armos Knight (south of Face Shrine) can be hit with sword spin.
 * Final boss Aghanim's Shadow sound effect for bursting projectile.
 * Final boss Ganon's Shadow can be damaged with spin attack.
 * Final boss Lanmola can be attacked with spin attack.
 * Final boss DethI can be killed with boomerang.
 
 ### **Overworld:**
 * Mabe/Animal Village music always takes priority over piece of power/acorn music.
 * Trendy game timings are closer to original game.
 * Owl conversations now properly freeze all enemies until he flies away.
 * Marin will teach Ballad of the Wind Fish after acquiring Ocarina.
 * Fixed Marin's message when attacking chickens to not default to sadistic comment.
 * When Marin sings to the walrus it now matches the duration of the original game.
 * Fixed rare issue in Dream Shrine where sleeping sprite would float past the bed.
 * Randomized sound effect frequency at Seashell Mansion when counting shells.
 * Richard's Villa maze sequence can no longer be cheated by jumping the holes.
 * Richard's castle photograph event fixed.
 * The ghost sequence must be respected: house by the bay  grave stone.
 * The ghost must be returned before his photo and sprinkling powder on grave.
 * The ghost return sequence now freezes the animations of nearby monsters.
 * Fixed getting the ghost photograph before returning him to the grave.
 * Increased the "grab" distance of the raft so it can't be clipped through.
 * Sign post maze now starts at the correct sign.
 * Flying rooster can no longer skip the flame trap on the way to Turtle Rock.
 * Fixed dying when holding onto the flying rooster.
 
 ### **Dungeons:**
 * Softlock fixed when picking up instruments.
 * Increased interaction range of dungeon teleporters.
 * Tail Cave (dungeon 1) block near trap is now pushable.
 * Face Shrine (dungeon 6) now references the correct key.
 * Face Shrine (dungeon 6) doors can only be opened with statues.
 * Face Shrine (dungeon 6) fixed spot where it was possible to get stuck in a wall.
 * Eagle Tower (dungeon 7) pull lever now works with small analog left/right values.
 * Eagle Tower (dungeon 7) when falling in holes Z-axis is remembered.
 * Turtle Rock (dungeon 8) fixed locked door that took a key but did not open.
 * Turtle Rock (dungeon 8) breakable wall was fixed (it couldn't be broken).
 * Play the secret discovery chime when taking the correct path in the egg.


