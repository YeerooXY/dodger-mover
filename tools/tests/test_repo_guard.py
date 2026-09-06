from __future__ import annotations

import subprocess
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch

from tools import repo_guard


class RepositoryGuardTests(unittest.TestCase):
    def test_forbidden_check_uses_tracked_paths(self) -> None:
        result = subprocess.CompletedProcess(
            args=["git", "ls-files", "-z"],
            returncode=0,
            stdout=b"Library/cache.bin\0.env\0Assets/game.cs\0",
            stderr=b"",
        )
        errors: list[str] = []

        with patch.object(repo_guard.subprocess, "run", return_value=result):
            repo_guard._check_forbidden_paths(errors)

        self.assertIn("Unity transient path present: Library/cache.bin", errors)
        self.assertIn("possible credential file present: .env", errors)

    def test_non_git_fallback_skips_ignored_local_state(self) -> None:
        result = subprocess.CompletedProcess(
            args=["git", "ls-files", "-z"],
            returncode=128,
            stdout=b"",
            stderr=b"not a repository",
        )
        with tempfile.TemporaryDirectory() as temporary_directory:
            root = Path(temporary_directory)
            (root / "Library").mkdir()
            (root / "Library" / "cache.bin").write_bytes(b"cache")
            (root / ".env").write_text("LOCAL_ONLY=true", encoding="utf-8")
            (root / "Assets").mkdir()
            (root / "Assets" / "game.cs").write_text("// source", encoding="utf-8")

            with (
                patch.object(repo_guard, "ROOT", root),
                patch.object(repo_guard.subprocess, "run", return_value=result),
            ):
                paths = repo_guard._tracked_paths()

        self.assertEqual(paths, (Path("Assets/game.cs"),))


if __name__ == "__main__":
    unittest.main()
