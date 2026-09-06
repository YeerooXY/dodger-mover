# S001: Impact proof

- Status: PLANNED
- Player build: Windows development build
- Target playtime: 5 minutes

## Player-facing goal

Deliver one graybox room where moving into an enemy, choosing a grounded or launcher attack, following displacement, and restarting feels immediate and satisfying.

## Hypothesis

If input buffering, displacement, hit-stop, recovery, and camera response are tuned together, a tiny moveset against one enemy can already create the desire to replay.

## Scope

- Run, turn, jump, fall, and fast restart.
- Coyote time and jump buffering.
- Light attack, heavy attack, and launcher.
- One three-step grounded sequence and one launcher follow-up.
- One stationary/approaching test enemy with health, hit-stun, knockback, recovery, and defeat.
- One flat floor plus one raised platform used for follow-up positioning.
- Hit-stop, restrained camera impulse, placeholder VFX, and placeholder audio.
- Keyboard and common gamepad bindings.
- Debug HUD for state, buffered command, frame timing, and allocations.

## Non-goals

- Final art or animation rig.
- Enemy roster, progression, scoring, inventory, story, destructible levels, or boss logic.
- More attacks unless required to evaluate the hypothesis.

## Implementation plan

1. Import and serialize the pinned Unity project settings; establish the bootstrap scene and composition root.
2. Implement a 60 Hz movement simulation and timestamped command buffer in the domain assembly.
3. Bind keyboard/gamepad input in presentation code without embedding combat decisions.
4. Implement explicit combatant states, attack phases, cancel windows, hit resolution, and stable hit ordering.
5. Add simple presentation adapters for animation placeholders, VFX, audio, camera impulse, and debug HUD.
6. Build the graybox room and enemy behavior.
7. Add domain, integration, replay, allocation, and scene-boot tests.
8. Profile, tune, review, publish the Windows build, and write the playtest card.

## Automated acceptance

- The same recorded command stream produces identical domain snapshots across repeated runs.
- Attack damage and knockback are applied once per target per activation.
- Buffered jump and attack commands expire on documented ticks.
- Invalid state combinations cannot be constructed through public APIs.
- Scene boot and instant restart succeed in PlayMode tests.
- Steady-state combat produces no recurring managed allocations after warm-up.
- Repository and Unity tests pass in CI.

## Performance budget

- 60 FPS at 1080p on the first available reference machine.
- 16.67 ms total frame budget; initial graybox target below 8 ms CPU frame time on the development machine.
- Zero steady-state GC allocation from owned combat, input, and camera code.

## Asset order

Use code-drawn or primitive placeholders first. No Meshy credits are authorized for S001 unless a specific visual experiment proves necessary and an exact batch is separately approved.

## Playtest request

Play for five minutes without reading the move list beyond the displayed controls. Restart at least once, then answer:

1. Which action felt best?
2. What felt slow, weak, confusing, or accidental?
3. Did you voluntarily try a different sequence after understanding the controls?
4. If you could change only one thing, what would it be?
