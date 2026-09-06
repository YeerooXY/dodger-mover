# One-time setup

These are infrastructure prerequisites, not recurring product decisions. Once complete, normal development should stop only at playable-slice feedback or an exceptional permission boundary.

## Workstation

1. Install Git LFS and run `git lfs install` once.
2. Install Unity `6000.3.12f1` through Unity Hub with Windows Build Support (Mono).
3. Clone the repository and open its root directory in Unity.
4. Allow Package Manager to resolve the pinned manifest.
5. Run the EditMode test assembly before beginning S001.

The first successful editor import will create additional serialized `ProjectSettings` and `Packages/packages-lock.json`. They must be reviewed and committed as the first S001 implementation step; generated `Library/`, `Temp/`, `Logs/`, and IDE project files stay ignored.

## GitHub CI

1. Obtain a Unity license suitable for the repository owner and intended commercial use.
2. Add it as the repository Actions secret `UNITY_LICENSE`.
3. Manually run the `Unity Tests` workflow once.
4. After the first green run, change its trigger from `workflow_dispatch` to `pull_request` and make the check required before merge.

Never commit the Unity license, OpenAI credentials, `MESHY_API_KEY`, or account authentication files. The public repository intentionally contains no secrets.

## Playtest machine

Playable slices will publish a zipped Windows development build. The player should not need Unity, Python, or repository access to test it.
