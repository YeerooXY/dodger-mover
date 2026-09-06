# ADR 0001: Engine and code boundaries

- Status: Accepted
- Date: 2026-09-06

## Context

The project needs responsive 2D combat, fast Windows builds, strong profiling, controller support, an asset pipeline that can incorporate generated imagery or 3D references, and reliable automated tests.

## Decision

Use Unity `6000.3.12f1`, C#, Universal Render Pipeline, Unity Input System, and Unity Test Framework. Start with a side-profile 2D presentation. Keep combat rules in a pure C# assembly with no Unity engine reference.

Do not purchase or introduce Spine during the impact prototype. Revisit character-rig tooling only after S001 proves the game feel and an animation test exposes a concrete limitation.

## Consequences

- The project can use Unity's mature Windows, profiling, controller, physics, animation, and 2D rendering paths.
- Pure domain logic remains fast to test and less coupled to engine lifecycle details.
- Unity licensing is required for CI builds and must be configured once before playable-slice automation can be fully green.
- Render-pipeline and input packages are pinned; upgrades require a dedicated pull request and evidence.
