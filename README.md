# Dodger Mover

Working-title repository for a side-profile combat-platformer that brings back the immediate feel of classic Flash fighting games with modern input, combat depth, and production discipline.

## Product target

The first release target is a polished, downloadable 20-30 minute Windows demo. Combat should flow through platforms and vertical spaces rather than stopping whenever traversal begins.

The immediate milestone is [S001: Impact Proof](docs/slices/S001-impact-proof.md): a five-minute graybox that answers one question—are movement and hits satisfying enough to justify the full game?

## Stack

- Unity `6000.3.12f1`
- C# with a pure, deterministic combat domain
- Universal Render Pipeline and Unity Input System
- Unity Test Framework plus repository-level Python checks
- GitHub pull requests, automatic review, and slice-based playtest builds

## Start here

1. Read [the product charter](docs/PRODUCT_CHARTER.md).
2. Read [the autonomous workflow](docs/WORKFLOW.md).
3. Read [the architecture](docs/ARCHITECTURE.md).
4. Complete the [one-time workstation and CI setup](docs/ONBOARDING.md).
5. Open the project in the pinned Unity version.
6. Run EditMode tests before changing combat code.

Repository-only checks can be run without Unity:

```bash
python -m unittest discover -s tools/tests -p "test_*.py"
python tools/repo_guard.py
```

## Current status

S000 foundation is under review. S001 is specified but must not expand beyond its acceptance criteria until its playtest is complete.

No open-source license has been granted yet. All rights remain with the repository owner.
