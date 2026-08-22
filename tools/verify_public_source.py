#!/usr/bin/env python3
"""Fail when a public snapshot tracks private keys or original LADXHD game data."""

from __future__ import annotations

from pathlib import Path, PurePosixPath
import subprocess
import sys


FORBIDDEN_PREFIXES = (
    ".local/",
    "assets_original/Content/",
    "assets_original/Data/",
    "ladxhd_game_source_code/ProjectZ.Core/Data/",
    "ladxhd_game_source_code/ProjectZ.Content/Content/",
    "ladxhd_game_source_code/ProjectZ.Content/Precompiled/Android/",
)
FORBIDDEN_SUFFIXES = (
    ".idsig",
    ".jks",
    ".keystore",
    ".p12",
    ".password",
)


def tracked_files() -> list[str]:
    result = subprocess.run(
        ["git", "ls-files", "-z"],
        check=True,
        stdout=subprocess.PIPE,
    )
    return [
        PurePosixPath(path).as_posix().lstrip("/")
        for path in result.stdout.decode("utf-8").split("\0")
        if path
    ]


def main() -> int:
    forbidden = sorted(
        path
        for path in tracked_files()
        if path.startswith(FORBIDDEN_PREFIXES)
        or path.lower().endswith(FORBIDDEN_SUFFIXES)
        or (path.startswith("dist/") and path.lower().endswith(".apk"))
        or "signing-lineage" in path.lower()
    )
    if forbidden:
        print("error: private or original game-data files are tracked:", file=sys.stderr)
        for path in forbidden:
            print(f"  {path}", file=sys.stderr)
        return 1

    android_project = Path(
        "ladxhd_game_source_code/ProjectZ.Android/ProjectZ.Android.csproj"
    )
    text = android_project.read_text(encoding="utf-8")
    for forbidden_fragment in (
        "ProjectZ.Core\\Data\\**\\*",
        "ProjectZContentEmbedInAndroid>true",
        "ProjectZ.Content\\BuildContent.targets",
    ):
        if forbidden_fragment in text:
            print(
                f"error: Android project embeds game data through {forbidden_fragment!r}",
                file=sys.stderr,
            )
            return 1

    print("Public-source guard passed: no private key or original Content/Data tree is tracked.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
