import dataclasses
import json
import logging
import os
from importlib.resources import files

from BaseClasses import Entrance, Item, ItemClassification, Location, Tutorial
from worlds.AutoWorld import WebWorld
from worlds.ladx import LinksAwakeningWorld
from worlds.ladx.Items import LinksAwakeningItem
from worlds.ladx.Locations import (
    LinksAwakeningEntrance,
    LinksAwakeningLocation,
    LinksAwakeningRegion,
    ladxr_region_to_name,
)
from worlds.ladx.Options import (
    BootsControls,
    DungeonShuffle,
    EntranceShuffle,
    Goal,
    HardMode,
    Logic,
    Overworld,
    Quickswap,
    Stealing,
    TrendyGame,
    Warps,
    ladx_option_groups,
)


GAME_NAME = "Links Awakening DX HD"
MANIFEST_VERSION = 1
LOGGER = logging.getLogger("Links Awakening DX HD")


class LinksAwakeningDXHDWebWorld(WebWorld):
    tutorials = [Tutorial(
        "Multiworld Setup Guide",
        "Configure the LADXHD native client for Archipelago.",
        "English",
        "setup_en.md",
        "setup/en",
        ["LADXHD Archipelago contributors"],
    )]
    theme = "ocean"
    # The built-in WebWorld registrar mutates its option-group list by adding
    # the common item/location group. Use a fresh outer list so registering this
    # derived game does not attempt to add that common group twice.
    option_groups = [
        group for group in ladx_option_groups
        if group.name != "Item & Location Options"
    ]


class LinksAwakeningDXHDItem(LinksAwakeningItem):
    game = GAME_NAME


class LinksAwakeningDXHDLocation(LinksAwakeningLocation):
    game = GAME_NAME


class LinksAwakeningDXHDRegion(LinksAwakeningRegion):
    pass


def create_ladxhd_regions(player, multiworld, logic):
    used_names = {}
    regions = {}

    for ladxr_location in logic.location_list:
        name = ladxr_region_to_name(ladxr_location)
        index = used_names.get(name, 0) + 1
        used_names[name] = index
        if index != 1:
            name += f" {index}"

        region = LinksAwakeningDXHDRegion(
            name=name,
            ladxr_region=ladxr_location,
            hint="",
            player=player,
            world=multiworld,
        )
        region.locations += [
            LinksAwakeningDXHDLocation(player, region, item)
            for item in ladxr_location.items
        ]
        regions[ladxr_location] = region

    for ladxr_location in logic.location_list:
        connections = ladxr_location.simple_connections + ladxr_location.gated_connections
        for connection_location, connection_condition in connections:
            region_a = regions[ladxr_location]
            region_b = regions[connection_location]
            entrance = LinksAwakeningEntrance(
                player,
                f"{region_a.name} -> {region_b.name}",
                region_a,
                connection_condition,
            )
            region_a.exits.append(entrance)
            entrance.connect(region_b)

    return list(regions.values())


class LinksAwakeningDXHDWorld(LinksAwakeningWorld):
    """Native LADXHD target using the maintained LADX Archipelago ruleset."""

    game = GAME_NAME
    web = LinksAwakeningDXHDWebWorld()

    # IDs are scoped by game name, so retaining the official LADX tables keeps
    # the inherited LADXR logic and item pool stable without conflicting with it.
    item_name_to_id = LinksAwakeningWorld.item_name_to_id
    item_name_to_data = LinksAwakeningWorld.item_name_to_data
    item_name_groups = LinksAwakeningWorld.item_name_groups
    location_name_to_id = LinksAwakeningWorld.location_name_to_id
    location_name_groups = LinksAwakeningWorld.location_name_groups

    def generate_early(self):
        unsupported = []
        if self.options.logic.value != Logic.option_normal:
            unsupported.append("logic must be normal")
        if self.options.experimental_entrance_shuffle.value != EntranceShuffle.option_none:
            unsupported.append("entrance shuffle must be none")
        if self.options.experimental_dungeon_shuffle.value != DungeonShuffle.option_false:
            unsupported.append("dungeon shuffle must be false")
        if self.options.hard_mode.value != HardMode.option_none:
            unsupported.append("hard mode must be none")
        if self.options.overworld.value != Overworld.option_normal:
            unsupported.append("overworld must be normal")
        if self.options.goal.value != Goal.option_instruments:
            unsupported.append("goal must be instruments")
        if int(self.options.instrument_count) != 8:
            unsupported.append("instrument count must be 8")
        if self.options.tradequest:
            unsupported.append("trade quest shuffle is not hooked yet")
        if not self.options.rooster:
            unsupported.append("rooster must be enabled until roosterless map routes are implemented")
        if self.options.warps.value != Warps.option_vanilla:
            unsupported.append("warps must be vanilla")
        if self.options.trendy_game.value != TrendyGame.option_normal:
            unsupported.append("Trendy Game must be normal")
        if self.options.boots_controls.value != BootsControls.option_vanilla:
            unsupported.append("boots controls must be vanilla")
        if self.options.quickswap.value != Quickswap.option_none:
            unsupported.append("quickswap must be disabled")
        if self.options.stealing.value == Stealing.option_disabled:
            unsupported.append("stealing cannot be disabled by the HD runtime")

        if unsupported:
            raise ValueError("LADXHD MVP does not support these options: " + "; ".join(unsupported))

        super().generate_early()

    def create_regions(self):
        self.convert_ap_options_to_ladxr_logic()
        regions = create_ladxhd_regions(self.player, self.multiworld, self.ladxr_logic)
        self.multiworld.regions += regions

        start = next((region for region in regions if region.name == "Start House"), None)
        if start is None:
            raise RuntimeError("LADX logic did not create Start House")

        menu_region = LinksAwakeningDXHDRegion("Menu", None, "Menu", self.player, self.multiworld)
        menu_region.exits = [Entrance(self.player, "Start Game", menu_region)]
        menu_region.exits[0].connect(start)
        self.multiworld.regions.append(menu_region)

        for region in regions:
            for location in region.locations:
                if location.address is None:
                    location.place_locked_item(self.create_event(location.ladxr_item.event))

        windfish = self.multiworld.get_region("Windfish", self.player)
        victory = Location(self.player, "Windfish", parent=windfish)
        windfish.locations = [victory]
        victory.place_locked_item(self.create_event("An Alarm Clock"))
        self.multiworld.completion_condition[self.player] = (
            lambda state: state.has("An Alarm Clock", player=self.player)
        )

    def create_item(self, item_name):
        return LinksAwakeningDXHDItem(self.item_name_to_data[item_name], self, self.player)

    def create_event(self, event):
        return Item(event, ItemClassification.progression, None, self.player)

    def generate_output(self, output_directory):
        with files(__package__).joinpath("location_mapping.json").open("r", encoding="utf-8") as mapping_file:
            game_keys = json.load(mapping_file)

        locations = []
        unmapped = []
        for region in self.multiworld.get_regions(self.player):
            for location in region.locations:
                if not isinstance(location, LinksAwakeningDXHDLocation) or location.address is None:
                    continue
                if location.item is None:
                    raise RuntimeError(f"Location was not filled: {location.name}")

                game_key = game_keys.get(location.name)
                if not game_key:
                    unmapped.append(location.name)

                locations.append({
                    "game_key": game_key,
                    "location_id": location.address,
                    "location_name": location.name,
                    "item_name": location.item.name,
                    "item_game": location.item.game,
                    "item_player": location.item.player,
                    "item_player_name": self.multiworld.get_player_name(location.item.player),
                    "local_player": self.player,
                    "classification": int(location.item.classification),
                })

        slot_data = super().fill_slot_data()
        manifest = {
            "format_version": MANIFEST_VERSION,
            "game": GAME_NAME,
            "seed_name": self.multiworld.seed_name,
            "slot_name": self.player_name,
            "world_version": slot_data.get("world_version", "0.2.0"),
            "mapping_complete": not unmapped,
            "unmapped_locations": unmapped,
            "locations": locations,
            "options": self._serializable_options(),
        }

        if unmapped:
            LOGGER.warning(
                "%s generated with %d unmapped source locations; see unmapped_locations in the manifest",
                GAME_NAME,
                len(unmapped),
            )

        output_name = self.multiworld.get_out_file_name_base(self.player) + ".apladxhd"
        output_path = os.path.join(output_directory, output_name)
        with open(output_path, "w", encoding="utf-8") as output_file:
            json.dump(manifest, output_file, indent=2, sort_keys=True)
            output_file.write("\n")

    def fill_slot_data(self):
        slot_data = super().fill_slot_data()
        slot_data.update({
            "seed_name": self.multiworld.seed_name,
            "manifest_version": MANIFEST_VERSION,
            "native_client": True,
        })
        return slot_data

    def modify_multidata(self, multidata):
        # Native clients authenticate with the normal slot name. The ROM-only
        # multi_key alias from the parent world is intentionally not generated.
        return None

    def _serializable_options(self):
        result = {}
        for name, option in dataclasses.asdict(self.options).items():
            if hasattr(option, "value"):
                result[name] = self._json_value(option.value)
            elif isinstance(option, (str, int, float, bool)) or option is None:
                result[name] = option
        return result

    @staticmethod
    def _json_value(value):
        if isinstance(value, (set, frozenset)):
            return sorted(LinksAwakeningDXHDWorld._json_value(entry) for entry in value)
        if isinstance(value, dict):
            return {
                str(key): LinksAwakeningDXHDWorld._json_value(entry)
                for key, entry in value.items()
            }
        if isinstance(value, (list, tuple)):
            return [LinksAwakeningDXHDWorld._json_value(entry) for entry in value]
        if isinstance(value, (str, int, float, bool)) or value is None:
            return value
        return str(value)
