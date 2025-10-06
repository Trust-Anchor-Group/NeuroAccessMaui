"""
find_unused_resx_keys.py

Purpose:
- Parse NeuroAccessMaui/Resources/Languages/AppResources.resx to collect all translation key names.
- Search the entire solution for usages of those keys in source files.
- Print the keys that are NOT referenced anywhere (excluding the .resx and generated Designer files).

Usage (from repo root):
  python Build/find_unused_resx_keys.py

Optional args:
  --resx <path>   Path to the .resx file (default: NeuroAccessMaui/Resources/Languages/AppResources.resx)
  --root <path>   Root folder to search (default: repo root = parent of Build/)

Notes:
- Excludes common build and generated directories (bin, obj, .git, .vs, etc.).
- Excludes *.Designer.cs and *.resx files to avoid false positives.
- Treats files as text by sampling bytes; binary files are skipped.
"""

from __future__ import annotations

import argparse
import os
from pathlib import Path
import re
import sys
import xml.etree.ElementTree as ET
from typing import Iterable, Set


DEFAULT_RELATIVE_RESX = Path("NeuroAccessMaui/Resources/Languages/AppResources.resx")

EXCLUDED_DIRS = {
    ".git",
    ".vs",
    ".idea",
    ".vscode",
    "bin",
    "obj",
    "packages",
    ".nuget",
    "node_modules",
}

EXCLUDED_FILE_PATTERNS = [
    re.compile(r"\\?AppResources\.resx$", re.IGNORECASE),
    re.compile(r"\.Designer\.cs$", re.IGNORECASE),
    re.compile(r"\.resx$", re.IGNORECASE),
    re.compile(r"\.resources$", re.IGNORECASE),
]


def is_probably_text_file(path: Path, sample_size: int = 4096) -> bool:
    """Heuristic to skip binary files without relying on extensions."""
    try:
        with path.open("rb") as f:
            chunk = f.read(sample_size)
        if b"\x00" in chunk:
            return False
        # Try decoding a small sample as UTF-8/Latin-1 to confirm textiness
        try:
            chunk.decode("utf-8")
        except UnicodeDecodeError:
            try:
                chunk.decode("latin-1")
            except UnicodeDecodeError:
                return False
        return True
    except Exception:
        return False


def iter_repo_files(root: Path) -> Iterable[Path]:
    for dirpath, dirnames, filenames in os.walk(root):
        # Prune excluded directories in-place
        dirnames[:] = [d for d in dirnames if d not in EXCLUDED_DIRS]
        for fname in filenames:
            p = Path(dirpath) / fname
            # Exclude by pattern
            s = str(p)
            if any(pat.search(s) for pat in EXCLUDED_FILE_PATTERNS):
                continue
            if is_probably_text_file(p):
                yield p


def parse_resx_keys(resx_path: Path) -> Set[str]:
    if not resx_path.exists():
        raise FileNotFoundError(f".resx not found: {resx_path}")
    tree = ET.parse(resx_path)
    root = tree.getroot()
    # In .resx files, data nodes are usually under the default namespace or none.
    keys: Set[str] = set()
    for data in root.iter():
        if data.tag.lower().endswith("data"):
            name = data.attrib.get("name")
            if name:
                keys.add(name)
    return keys


def compile_patterns(keys: Iterable[str], chunk_size: int = 200):
    """Yield compiled regex patterns combining keys in chunks for efficiency."""
    buf = []
    for k in keys:
        # Escape keys for literal search; avoid word boundaries to not miss cases like x:Static bindings
        buf.append(re.escape(k))
        if len(buf) >= chunk_size:
            yield re.compile("|".join(buf))
            buf = []
    if buf:
        yield re.compile("|".join(buf))


def find_used_keys(root: Path, keys: Set[str]) -> Set[str]:
    remaining = set(keys)
    if not remaining:
        return set()

    # Pre-compile patterns for faster scanning per file
    patterns = list(compile_patterns(sorted(remaining)))

    found: Set[str] = set()
    for file_path in iter_repo_files(root):
        # Early-exit if all keys found
        if len(found) == len(keys):
            break
        try:
            text = file_path.read_text(encoding="utf-8", errors="ignore")
        except Exception:
            continue

        # Quick skip: if none of the chunk patterns match, continue
        if not any(p.search(text) for p in patterns):
            continue

        # Identify which of the remaining keys are present in this file
        hits = {k for k in remaining if k in text}
        if hits:
            found.update(hits)
            remaining.difference_update(hits)
    return found


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description="Find unused .resx translation keys across the solution.")
    parser.add_argument("--resx", type=str, default=None, help="Path to AppResources.resx")
    parser.add_argument("--root", type=str, default=None, help="Root folder to search (solution root)")
    args = parser.parse_args(argv)

    script_path = Path(__file__).resolve()
    repo_root = (script_path.parent.parent if script_path.parent.name == "Build" else script_path.parent).resolve()
    search_root = Path(args.root).resolve() if args.root else repo_root
    resx_path = Path(args.resx).resolve() if args.resx else (repo_root / DEFAULT_RELATIVE_RESX)

    print(f"Scanning repo: {search_root}")
    print(f"Reading keys from: {resx_path}")

    try:
        keys = parse_resx_keys(resx_path)
    except Exception as e:
        print(f"Failed to parse resx: {e}", file=sys.stderr)
        return 2

    if not keys:
        print("No keys found in the .resx file.")
        return 0

    print(f"Collected {len(keys)} keys. Searching for usages...")
    found = find_used_keys(search_root, keys)
    unused = sorted(keys - found)

    if unused:
        print("\nKeys not found in source:")
        for k in unused:
            print(k)
        print(f"\nTotal unused: {len(unused)} / {len(keys)}")
        return 1
    else:
        print("All keys are referenced somewhere in the repository (excluding .resx/Designer/generated/build files).")
        return 0


if __name__ == "__main__":
    raise SystemExit(main())
