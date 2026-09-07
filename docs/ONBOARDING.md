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
2. For the existing GameCI Personal-license workflow, add the `.ulf` file as `UNITY_LICENSE`, plus `UNITY_EMAIL` and `UNITY_PASSWORD`, in [repository Actions secrets](https://github.com/YeerooXY/dodger-mover/settings/secrets/actions). Enter credentials directly in GitHub; do not paste them into issues or chat. A modern Hub `UnityEntitlementLicense.xml` is not a `.ulf` activation file. See [GameCI's activation instructions](https://game.ci/docs/github/activation/) for supported activation methods; a Pro or license-server account needs its corresponding workflow configuration.
3. Manually run the `Unity Tests` workflow once on the implementation branch. It runs both EditMode and PlayMode, then builds a Windows development player. Missing activation secrets deliberately fail the preflight; they are never reported as passing tests.
4. After the first green run, change its trigger from `workflow_dispatch` to `pull_request` and make the check required before merge.

Never commit the Unity license, OpenAI credentials, `MESHY_API_KEY`, or account authentication files. The public repository intentionally contains no secrets.

## Repeatable local verification

Use an already activated editor. The script runs each editor process sequentially, verifies nonempty passing XML results, and writes ignored evidence under `Logs/S001`:

```powershell
./tools/verify_unity.ps1 -UnityEditor 'C:/path/to/6000.3.12f1/Editor/Unity.exe' -Build
```

The committed `ImpactProof` scene is ready to open. `-Configure` regenerates the authored scene, material, render pipeline, and project settings; it is only needed when deliberately rebuilding that setup. The editor menu offers the same operations under **Dodger Mover**.

## Playtest machine

Playable slices will publish a zipped Windows development build. The player should not need Unity, Python, or repository access to test it.
