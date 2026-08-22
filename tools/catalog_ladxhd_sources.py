#!/usr/bin/env python3
"""Catalog stable LADXHD reward sources from user-supplied Data assets."""

from __future__ import annotations

import argparse
import json
from pathlib import Path
from urllib.parse import quote


def spawned_source(object_id: str, parameter_text: str) -> dict[str, object] | None:
    """Return the persistent reward represented by a spawn action, if any."""
    parameters = [value.replace("$", ".") for value in parameter_text.split(".")]
    if object_id == "item":
        parameters += [""] * (4 - len(parameters))
        item_type, save_key, item_name, item_bound = parameters[:4]
        if save_key:
            return {
                "game_key": f"item:{save_key}",
                "kind": "spawned_item",
                "vanilla_item": item_name,
                "item_bound": item_bound,
                "item_type": item_type,
            }
    elif object_id == "chest":
        parameters += [""] * (3 - len(parameters))
        item_name, item_bound, save_key = parameters[:3]
        if save_key:
            return {
                "game_key": f"chest:{save_key}",
                "kind": "spawned_chest",
                "vanilla_item": item_name,
                "item_bound": item_bound,
            }
    return None


def parse_map(path: Path) -> tuple[list[dict[str, object]], list[dict[str, object]]]:
    lines = path.read_text(encoding="utf-8-sig").splitlines()
    cursor = 0
    version = int(lines[cursor])
    cursor += 1

    offset_x = offset_y = 0
    if version > 2:
        offset_x = int(lines[cursor])
        offset_y = int(lines[cursor + 1])
        cursor += 2

    tileset = lines[cursor]
    width = int(lines[cursor + 1])
    height = int(lines[cursor + 2])
    depth = int(lines[cursor + 3])
    cursor += 4 + height * depth

    template_count = int(lines[cursor])
    cursor += 1
    templates = lines[cursor:cursor + template_count]
    cursor += template_count

    object_count = int(lines[cursor])
    cursor += 1
    objects: list[dict[str, object]] = []
    for line in lines[cursor:cursor + object_count]:
        fields = line.split(";")
        template = templates[int(fields[0])]
        x = int(fields[1])
        y = int(fields[2])
        world_x = x + offset_x
        world_y = y + offset_y
        objects.append({
            "template": template,
            "parameters": fields[3:],
            "x": x,
            "y": y,
            "world_x": world_x,
            "world_y": world_y,
            "field_x": world_x // 160,
            "field_y": world_y // 128,
        })

    entries: list[dict[str, object]] = []
    for obj in objects:
        template = str(obj["template"])
        fields = ["", "", "", *obj["parameters"]]
        x = int(obj["x"])
        y = int(obj["y"])

        entry: dict[str, object] | None = None
        if template == "chest":
            item_name = fields[3] if len(fields) > 3 else ""
            item_bound = fields[4] if len(fields) > 4 else ""
            save_key = fields[5] if len(fields) > 5 else ""
            if save_key:
                entry = {
                    "game_key": f"chest:{save_key}",
                    "kind": "chest",
                    "vanilla_item": item_name,
                    "item_bound": item_bound,
                }
        elif template == "item":
            item_type = fields[3] if len(fields) > 3 else ""
            save_key = fields[4] if len(fields) > 4 else ""
            item_name = fields[5] if len(fields) > 5 else ""
            item_bound = fields[6] if len(fields) > 6 else ""
            if save_key:
                entry = {
                    "game_key": f"item:{save_key}",
                    "kind": "item",
                    "vanilla_item": item_name,
                    "item_bound": item_bound,
                    "item_type": item_type,
                }
        elif template == "storeItem":
            item_name = fields[3] if len(fields) > 3 else ""
            price = fields[4] if len(fields) > 4 else "0"
            count = fields[5] if len(fields) > 5 else "1"
            entry = {
                "game_key": f"shop:{price}",
                "kind": "shop",
                "vanilla_item": item_name,
                "price": int(price or 0),
                "count": int(count or 1),
            }
        elif template == "objectSpawner" and len(fields) > 7:
            entry = spawned_source(fields[5], fields[6])
            if entry is None and fields[5] in {
                "stone", "stoneWoods", "stoneSkull", "pot", "pot2", "pot2D"
            }:
                parameters = [value.replace("$", ".") for value in fields[6].split(".")]
                parameters += [""] * (3 - len(parameters))
                spawn_item = parameters[1]
                save_key = parameters[2]
                if spawn_item and save_key:
                    entry = {
                        "game_key": f"item:{save_key}",
                        "kind": "spawned_item",
                        "vanilla_item": spawn_item,
                    }
            if entry is not None:
                entry["kind"] = "map_" + str(entry["kind"])
                entry["spawn_condition"] = fields[3]
                entry["spawn_value"] = fields[4]
        elif template == "shellHitSpawner":
            save_key = fields[3] if len(fields) > 3 else ""
            item_name = fields[4] if len(fields) > 4 and fields[4] else "shell"
            if save_key:
                entry = {
                    "game_key": f"item:{save_key}",
                    "kind": "dash_spawned_item",
                    "vanilla_item": item_name,
                }
        elif template == "bush":
            spawn_spec = fields[3] if len(fields) > 3 else ""
            if ":" in spawn_spec:
                object_id, parameter_text = spawn_spec.split(":", 1)
                entry = spawned_source(object_id, parameter_text)
                if entry is not None:
                    entry["kind"] = "bush_" + str(entry["kind"])
        elif template in {"stone", "stoneWoods", "stoneSkull", "pot", "pot2", "pot2D"}:
            spawn_item = fields[4] if len(fields) > 4 else ""
            save_key = fields[5] if len(fields) > 5 else ""
            if spawn_item and save_key:
                entry = {
                    "game_key": f"item:{save_key}",
                    "kind": "lifted_item",
                    "vanilla_item": spawn_item,
                }

        if entry is not None:
            world_x = x + offset_x
            world_y = y + offset_y
            field_x = world_x // 160
            field_y = world_y // 128
            field_context = [
                {
                    "template": other["template"],
                    "parameters": other["parameters"],
                    "x": other["x"],
                    "y": other["y"],
                }
                for other in objects
                if other["field_x"] == field_x and other["field_y"] == field_y
            ]
            entry.update({
                "map": path.name,
                "x": x,
                "y": y,
                "world_x": world_x,
                "world_y": world_y,
                "field_x": field_x,
                "field_y": field_y,
                "field_context": field_context,
                "tileset": tileset,
                "map_width": width,
                "map_height": height,
            })
            entries.append(entry)

    return entries, objects


def parse_dig_map(path: Path, map_name: str, offset_x: int, offset_y: int) -> list[dict[str, object]]:
    lines = path.read_text(encoding="utf-8-sig").splitlines()
    if len(lines) < 2:
        return []
    width = int(lines[0])
    height = int(lines[1])
    entries: list[dict[str, object]] = []
    for y, line in enumerate(lines[2:2 + height]):
        cells = line.split(";")
        for x, cell in enumerate(cells[:width]):
            parts = cell.split(":")
            if len(parts) < 2 or not parts[0] or not parts[1]:
                continue
            world_x = x * 16 + offset_x
            world_y = y * 16 + offset_y
            entries.append({
                "game_key": f"item:{parts[1]}",
                "kind": "dug_item",
                "vanilla_item": parts[0],
                "map": map_name,
                "tile_x": x,
                "tile_y": y,
                "world_x": world_x,
                "world_y": world_y,
                "field_x": world_x // 160,
                "field_y": world_y // 128,
            })
    return entries


def parse_scripts(path: Path) -> list[dict[str, object]]:
    entries: list[dict[str, object]] = []
    for line_number, raw_line in enumerate(path.read_text(encoding="utf-8-sig").splitlines(), 1):
        line = raw_line.replace(" ", "")
        if not line or line.startswith("//"):
            continue
        parts = line.split("->")
        if len(parts) < 2:
            continue
        script_key = parts[0].split(":", 1)[0]
        for action_index, action in enumerate(parts[1:]):
            normalized = action.removeprefix("[").removesuffix("]")
            action_parts = normalized.split(":")
            entry: dict[str, object] | None = None
            if action_parts[0] == "add_item" and len(action_parts) >= 3:
                entry = {
                    "game_key": f"script:{quote(script_key, safe='')}:{action_index}",
                    "kind": "script",
                    "vanilla_item": action_parts[1],
                    "count": int(action_parts[2]),
                }
            elif action_parts[0] == "spawn" and len(action_parts) >= 3:
                entry = spawned_source(action_parts[1], action_parts[2])
                if entry is not None:
                    entry["kind"] = "script_" + str(entry["kind"])

            if entry is not None:
                entry.update({
                    "script_key": script_key,
                    "action_index": action_index,
                    "line": line_number,
                })
                entries.append(entry)
    return entries


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("data", type=Path, help="Path to ProjectZ.Core/Data")
    parser.add_argument("--output", type=Path, help="Write JSON to this path instead of stdout")
    args = parser.parse_args()

    maps_dir = args.data / "Maps"
    sources: list[dict[str, object]] = []
    map_objects: dict[str, list[dict[str, object]]] = {}
    for map_path in sorted(maps_dir.glob("*.map")):
        map_sources, objects = parse_map(map_path)
        sources.extend(map_sources)
        map_objects[map_path.name] = objects
        if objects:
            offset_x = int(objects[0]["world_x"]) - int(objects[0]["x"])
            offset_y = int(objects[0]["world_y"]) - int(objects[0]["y"])
        else:
            offset_x = offset_y = 0
        dig_path = map_path.with_name(map_path.name + ".data")
        if dig_path.exists():
            sources.extend(parse_dig_map(dig_path, map_path.name, offset_x, offset_y))
    sources.extend(parse_scripts(args.data / "scripts.zScript"))

    for source in sources:
        script_key = source.get("script_key")
        if not script_key:
            continue
        references = []
        for map_name, objects in map_objects.items():
            for obj in objects:
                if script_key not in obj["parameters"]:
                    continue
                field_x = int(obj["field_x"])
                field_y = int(obj["field_y"])
                references.append({
                    "map": map_name,
                    "template": obj["template"],
                    "parameters": obj["parameters"],
                    "x": obj["x"],
                    "y": obj["y"],
                    "field_x": field_x,
                    "field_y": field_y,
                    "field_context": [
                        {
                            "template": other["template"],
                            "parameters": other["parameters"],
                            "x": other["x"],
                            "y": other["y"],
                        }
                        for other in objects
                        if other["field_x"] == field_x and other["field_y"] == field_y
                    ],
                })
        if references:
            source["map_references"] = references
    sources.sort(key=lambda entry: (str(entry["game_key"]), str(entry.get("map", ""))))

    duplicate_keys: dict[str, list[dict[str, object]]] = {}
    for source in sources:
        duplicate_keys.setdefault(str(source["game_key"]), []).append(source)
    duplicate_keys = {key: values for key, values in duplicate_keys.items() if len(values) > 1}

    result = {
        "source_count": len(sources),
        "unique_key_count": len({source["game_key"] for source in sources}),
        "duplicate_keys": duplicate_keys,
        "sources": sources,
    }
    output = json.dumps(result, indent=2, ensure_ascii=False) + "\n"
    if args.output:
        args.output.parent.mkdir(parents=True, exist_ok=True)
        args.output.write_text(output, encoding="utf-8")
    else:
        print(output, end="")


if __name__ == "__main__":
    main()
