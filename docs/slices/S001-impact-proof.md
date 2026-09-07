# S001: Impact proof

- Status: REVIEWING (local tests and Windows build passed; final visual check and hosted CI pending)
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

## Implementation discoveries and decisions

- 2026-09-07: S000 is merged as PR #1. This workstation has a licensed Unity editor, but initially only 6000.3.19f1 and 6000.0.37f1 were installed. Install the pinned 6000.3.12f1 separately before import; do not silently upgrade the project.
- Baseline repository tests exposed a Windows path-separator mismatch in the guard diagnostics. Normalize diagnostic paths to POSIX notation so the same contract passes on Windows and Linux.
- GitHub has no Actions secrets. The workstation has a Hub entitlement license, not a transferable `.ulf`. Hosted GameCI execution remains an explicit external gate; local import, tests, and the Windows build can proceed with workstation licensing.
- Keep the single floor and raised platform as domain-owned axis-aligned surfaces. Sweep descending feet against surfaces; presentation renders their exact coordinates. No unordered Unity physics query controls gameplay.
- Input presses carry simulation-tick timestamps. Jump lives for 6 ticks, attack for 9 ticks (inclusive issue tick, exclusive expiry tick). Coyote time is 6 ticks. A held button does not repeat an action.
- Grounded light taps chain through three attacks only during documented recovery cancels. Launcher may cancel into a buffered jump on confirmed hit; an airborne light is the single follow-up. Heavy and launcher cannot bypass recovery.
- Build a primitive silhouette arena with a restrained teal/orange palette, synthesized impact audio, bounded impact visuals, and in-game controls. No external assets, new production dependencies, or Meshy spend.
- Review pass 1: accepted P1 input backdating and P2 launcher contact-buffer findings. Corrective plan: carry Input System event timestamps into a bounded application queue, apply them on their simulation ticks, test 30/60/144 Hz and an 83 ms hitch; then consume an eligible launcher jump immediately after contact and test contact/preceding-tick presses. Both corrections remain within S001.
- Review pass 2: original findings resolved. Accepted two P2 follow-ups: resynchronize held movement after restart/focus clock resets without replaying button edges, and preserve newest distinct-time attack selection within each tick while retaining simultaneous-button priority. Added held-input integration and ordering regression tests.
- Standalone inspection found proportional-font spacing shifted control labels away from their headings. Replaced the single padded text string with five explicitly positioned labels. This is a presentation correction; bindings and simulation rules are unchanged.

## Execution checklist

- [x] Pinned editor import, serialized settings, and baseline EditMode tests.
- [x] Pure C# movement, timestamped commands, combat phases, and replay tests.
- [x] Input, room composition, silhouette feedback, controls, and diagnostics.
- [x] PlayMode integration tests, allocation evidence, and Windows development build.
- [ ] Final corrected-build visual inspection (Computer Use was stopped during verification).
- [x] Independent diff review and corrections, repository checks, and playtest card.
- [ ] Hosted Unity CI (requires account secrets); user playtest remains the slice boundary.

## Verification evidence

- Unity `6000.3.12f1 (fca03ac9b0d5)` was installed separately and activated through the existing workstation entitlement. Import and configuration completed successfully; Unity generated `Packages/packages-lock.json` and serialized project settings.
- Unity EditMode: **40/40 passed**. Includes the original kernel checks, movement, buffering, cancels, deterministic command/realtime replays, restart, and allocation regressions.
- Unity PlayMode: **6/6 passed**. Covers scene boot, immediate restart without object growth, keyboard/gamepad bindings, held movement across restart, and the owned-loop allocation measurement.
- Final PlayMode profile: **0 bytes/frame** after warm-up; maximum measured owned-loop CPU **5.776 ms** (the preceding run measured 1.510 ms). Reference workstation: Ryzen 7 5800X, GeForce RTX 4060 Ti. This measures simulation and owned rendering, excluding diagnostic formatting; it is not a whole-frame or low-spec performance certification.
- Independent review completed in three passes; all four consequential findings were corrected and tested. Final review reports no unresolved P0/P1/P2 findings.
- Local raw evidence is ignored under `Logs/S001/EditMode.xml`, `PlayMode.xml`, and the accompanying editor logs. A supplemental .NET execution of the same 40 EditMode sources also passed; it is not used as a substitute for Unity results.
- Final Windows x64 Mono development build **succeeded** after the control-label correction. Unity reported **159,558,966 bytes** and **15.37 seconds** for the final incremental build. It is packaged as `S001-ImpactProof-v0.1.0-Windows.zip`; the archive includes a playtest card and source revision, and excludes Burst's `DoNotShip` debug directory.
- Standalone inspection confirmed the initial room, readable silhouettes, enemy health, and room-clear presentation. The control-label spacing issue was corrected and all 40 EditMode plus 6 PlayMode tests passed again. Computer Use was stopped before final visual verification; no claim is made of complete manual acceptance, measured 1080p frame pacing, audio audition, or physical-controller feel.
- Hosted CI remains unavailable until the repository owner provides GameCI activation secrets. Local tests do not satisfy this merge gate. No Meshy credits were used.
