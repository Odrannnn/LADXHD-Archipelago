# Links Awakening DX HD Multiworld Setup

This world targets the native LADXHD recreation. It does not patch or distribute Nintendo game data.

1. Build or install the LADXHD Archipelago fork and supply the original game assets as required by LADXHD.
2. Install `ladxhd.apworld` into the `custom_worlds` directory of a compatible Archipelago installation.
3. Generate a `Links Awakening DX HD` player. The MVP requires normal logic, the normal overworld, no entrance or dungeon shuffle, the instrument goal, all eight instruments, and trade quest shuffle off.
4. On Android, open the generated `.apladxhd` from Files or share it to **Import LADXHD Randomizer**, then enter the server, optional password, and one of the four save positions. The importer keeps an independent profile for every position.
5. On desktop, create `Archipelago/Profiles/SaveN` under the game's user-data directory, put the manifest there as `seed.apladxhd`, and copy `connection.example.json` there as `connection.json`. Set the server, slot, optional password, and zero-based `save_slot` matching `SaveN` (`Save1` uses `0`, through `Save4` using `3`).
6. Create a new in-game save in the configured save slot. The seed and slot identity are permanently bound to that save.

You can import all four positions with different seeds and servers. Switching in-game saves
switches profiles and live connections automatically. Re-import the same position to change its
host, port, or password. A different seed in an already-bound save is rejected until that
in-game save is deleted and recreated, preventing accidental cross-seed progress.

The supported MVP configuration emits a complete 220-location manifest. If a manifest lists
anything under `unmapped_locations`, do not start that seed; its settings or world version are
outside the validated configuration.
