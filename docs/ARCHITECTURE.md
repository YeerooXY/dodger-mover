# Architecture

## Dependency direction

The codebase uses explicit assembly boundaries:

1. **Domain** — deterministic combat and movement rules; pure C#; no Unity engine references.
2. **Application** — commands, simulation orchestration, encounter lifecycle, and ports.
3. **Presentation** — MonoBehaviours, animation, camera, input sampling, VFX, and audio.
4. **Infrastructure** — save data, platform services, diagnostics, and generated-asset importers.

Dependencies point inward. Domain code never knows about scenes, GameObjects, animation controllers, input devices, or persistence.

## Runtime rules

- Simulation advances on an explicit 60 Hz combat tick.
- Input is sampled independently and converted into timestamped domain commands.
- Attack configuration is immutable. Per-use cooldowns, combo position, hit lists, and status live in runtime state.
- Physics query results that affect gameplay are sorted before resolution.
- Randomness affecting gameplay uses an injected seeded source.
- Scene references are wired through composition roots, not global lookups.

## Performance budgets

Playable slices target:

- 60 FPS at 1080p on the eventual reference low-spec PC.
- No recurring managed allocations in the steady-state combat loop.
- No `Find*`, reflection, asset loading, or component discovery inside per-frame gameplay paths.
- Object pooling only after profiling proves churn; pools must have bounded capacity and reset tests.

## Testing strategy

- EditMode domain tests cover attacks, timing, state transitions, and deterministic replays.
- PlayMode tests cover Unity integration, collision layers, input wiring, scene boot, and checkpoints.
- Recorded command streams reproduce gameplay bugs without requiring the original frame rate.
- Each slice preserves one short acceptance replay used as a regression test.

## Content boundaries

Game rules should be data-driven only where designers benefit from iteration. Avoid generic frameworks and deep inheritance trees. Prefer small immutable definitions, explicit state machines, and composition.

