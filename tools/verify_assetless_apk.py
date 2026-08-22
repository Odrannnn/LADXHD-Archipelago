#!/usr/bin/env python3
"""Reject Android packages that accidentally contain original LADXHD game data."""

from __future__ import annotations

import argparse
from pathlib import Path, PurePosixPath
import sys
import zipfile


REQUIRED_BOOTSTRAP = {
    "assets/Bootstrap/android_buttons.zip",
    "assets/Bootstrap/d3map",
    "assets/Bootstrap/d3mapdata",
    "assets/Bootstrap/patches_android.zip",
}
FORBIDDEN_PREFIXES = ("assets/Content/", "assets/Data/")


def verify(apk: Path) -> None:
    if not apk.is_file():
        raise ValueError(f"APK does not exist: {apk}")

    with zipfile.ZipFile(apk) as archive:
        names = {PurePosixPath(name).as_posix().lstrip("/") for name in archive.namelist()}

    forbidden = sorted(name for name in names if name.startswith(FORBIDDEN_PREFIXES))
    if forbidden:
        preview = "\n  ".join(forbidden[:20])
        raise ValueError(
            "APK contains original game-data paths and must not be published:\n  " + preview
        )

    missing = sorted(REQUIRED_BOOTSTRAP - names)
    if missing:
        raise ValueError("APK is missing required local-builder inputs: " + ", ".join(missing))

    print(
        f"Assetless APK verified: {apk} "
        f"({len(REQUIRED_BOOTSTRAP)} bootstrap inputs; no Content/Data assets)"
    )


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("apk", type=Path, help="signed or unsigned APK to inspect")
    args = parser.parse_args()
    try:
        verify(args.apk)
    except (OSError, ValueError, zipfile.BadZipFile) as error:
        print(f"error: {error}", file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
