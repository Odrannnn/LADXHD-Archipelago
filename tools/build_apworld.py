#!/usr/bin/env python3
import json
from pathlib import Path
from zipfile import ZIP_DEFLATED, ZipFile, ZipInfo


ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "archipelago_world" / "ladxhd"
OUTPUT = ROOT / "dist" / "ladxhd.apworld"
CONTAINER_VERSION = 7
ZIP_TIMESTAMP = (1980, 1, 1, 0, 0, 0)


def write_reproducible(archive, name, contents):
    info = ZipInfo(str(name).replace("\\", "/"), ZIP_TIMESTAMP)
    info.compress_type = ZIP_DEFLATED
    info.external_attr = 0o100644 << 16
    archive.writestr(info, contents)


def main():
    metadata = json.loads((SOURCE / "archipelago.json").read_text(encoding="utf-8"))
    mappings = json.loads((SOURCE / "location_mapping.json").read_text(encoding="utf-8"))
    if metadata.get("game") != "Links Awakening DX HD":
        raise SystemExit("archipelago.json has the wrong game name")
    if metadata.get("minimum_ap_version") != "0.6.7":
        raise SystemExit("archipelago.json must target Archipelago 0.6.7")
    if len(mappings) != len(set(mappings.values())):
        raise SystemExit("location_mapping.json contains duplicate game keys")

    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    with ZipFile(OUTPUT, "w", ZIP_DEFLATED) as archive:
        container_manifest = dict(metadata)
        container_manifest.update({
            "version": CONTAINER_VERSION,
            "compatible_version": CONTAINER_VERSION,
        })
        write_reproducible(
            archive,
            "ladxhd/archipelago.json",
            json.dumps(container_manifest, indent=2) + "\n",
        )
        for path in sorted(SOURCE.rglob("*")):
            if (
                path.is_file()
                and "__pycache__" not in path.parts
                and path.name != "archipelago.json"
            ):
                write_reproducible(
                    archive,
                    Path("ladxhd") / path.relative_to(SOURCE),
                    path.read_bytes(),
                )

    print(f"Built {OUTPUT}")


if __name__ == "__main__":
    main()
