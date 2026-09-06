#!/usr/bin/env python3
"""Fail fast on repository states that should never reach a pull request."""

from __future__ import annotations

import json
import subprocess
import sys
from pathlib import Path

try:
    from tools.asset_queue import QueueValidationError, validate_repository_queue
except ModuleNotFoundError:  # Direct execution adds tools/, not repository root, to sys.path.
    from asset_queue import QueueValidationError, validate_repository_queue


ROOT = Path(__file__).resolve().parents[1]
PINNED_UNITY_VERSION = "6000.3.12f1"
TRANSIENT_DIRECTORY_NAMES = {
    "Library",
    "Temp",
    "Obj",
    "Build",
    "Builds",
    "Logs",
    "UserSettings",
}
SECRET_FILENAMES = {".env", "auth.json", "credentials.json"}
FALLBACK_IGNORED_PARTS = TRANSIENT_DIRECTORY_NAMES | {
    ".git",
    ".idea",
    ".vs",
    ".vscode",
    "__pycache__",
}


def _check_json(errors: list[str]) -> None:
    roots = (ROOT / "Packages", ROOT / "asset_queue")
    for root in roots:
        for path in root.rglob("*.json"):
            try:
                json.loads(path.read_text(encoding="utf-8"))
            except (OSError, json.JSONDecodeError) as exc:
                errors.append(f"invalid JSON: {path.relative_to(ROOT)} ({exc})")

    for path in (ROOT / "Assets").rglob("*.asmdef"):
        try:
            json.loads(path.read_text(encoding="utf-8"))
        except (OSError, json.JSONDecodeError) as exc:
            errors.append(f"invalid asmdef JSON: {path.relative_to(ROOT)} ({exc})")


def _check_unity_version(errors: list[str]) -> None:
    path = ROOT / "ProjectSettings" / "ProjectVersion.txt"
    expected = f"m_EditorVersion: {PINNED_UNITY_VERSION}"
    try:
        content = path.read_text(encoding="utf-8")
    except OSError as exc:
        errors.append(f"cannot read {path.relative_to(ROOT)}: {exc}")
        return
    if expected not in content.splitlines():
        errors.append(f"Unity version must be pinned to {PINNED_UNITY_VERSION}")


def _check_unity_metadata(errors: list[str]) -> None:
    assets = ROOT / "Assets"
    for path in assets.rglob("*"):
        if path.name.endswith(".meta"):
            continue
        meta_path = path.with_name(path.name + ".meta")
        if not meta_path.is_file():
            errors.append(f"missing Unity metadata: {meta_path.relative_to(ROOT)}")


def _check_forbidden_paths(errors: list[str]) -> None:
    for relative in _tracked_paths():
        if any(part in TRANSIENT_DIRECTORY_NAMES for part in relative.parts):
            errors.append(f"Unity transient path present: {relative}")
        if relative.name in SECRET_FILENAMES or (
            relative.name.startswith(".env.") and relative.name != ".env.example"
        ):
            errors.append(f"possible credential file present: {relative}")


def _tracked_paths() -> tuple[Path, ...]:
    """Return tracked files, or the intended source set before Git is initialized."""

    try:
        result = subprocess.run(
            ["git", "ls-files", "-z"],
            cwd=ROOT,
            check=False,
            capture_output=True,
        )
    except OSError:
        result = None

    if result is not None and result.returncode == 0:
        return tuple(
            Path(raw.decode("utf-8"))
            for raw in result.stdout.split(b"\0")
            if raw
        )

    paths: list[Path] = []
    for path in ROOT.rglob("*"):
        if not path.is_file():
            continue
        relative = path.relative_to(ROOT)
        if any(part in FALLBACK_IGNORED_PARTS for part in relative.parts):
            continue
        if relative.name in SECRET_FILENAMES or (
            relative.name.startswith(".env.") and relative.name != ".env.example"
        ):
            continue
        if relative.suffix in {".pyc", ".pyo"}:
            continue
        paths.append(relative)
    return tuple(paths)


def run() -> list[str]:
    errors: list[str] = []
    _check_json(errors)
    _check_unity_version(errors)
    _check_unity_metadata(errors)
    _check_forbidden_paths(errors)
    try:
        validate_repository_queue()
    except QueueValidationError as exc:
        errors.append(str(exc))
    return sorted(set(errors))


def main() -> int:
    errors = run()
    if errors:
        print("Repository guard failed:", file=sys.stderr)
        for error in errors:
            print(f"- {error}", file=sys.stderr)
        return 1
    print("Repository guard passed.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
