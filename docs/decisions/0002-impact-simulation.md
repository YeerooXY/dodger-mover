# ADR 0002: Impact Proof simulation and presentation

- Status: Accepted
- Date: 2026-09-07

## Decision

S001 uses a pure C# 60 Hz simulation with one player, one approaching target, a floor, and one one-way platform. Descending foot sweeps determine contact. No Unity physics result or render frame decides gameplay.

The application queues Input System event timestamps relative to the editor/player's realtime clock, retains stable ordering, and consumes each event on its corresponding simulation tick. Commands delivered within one tick retain the newest distinct-time attack; equal-time attacks use launcher, heavy, light priority. Jump has its own slot. The queue has a fixed 128-event capacity. Frame hitches up to 100 ms retain all simulation steps; longer stalls discard excess elapsed time and rebase the input clock. Focus changes clear old commands and resynchronize held movement.

Jump buffers live for six ticks and attack buffers for nine, with exclusive expiry. Coyote grace lasts six ticks. Hit-stop freezes bodies, attack phases, and stun clocks while input tick time continues. A confirmed launcher consumes an eligible jump before entering hit-stop, aligning player/enemy takeoff.

| Move | Startup | Active | Recovery | Damage | Hit-stop |
| --- | ---: | ---: | ---: | ---: | ---: |
| Light 1 | 4 | 3 | 13 | 8 | 4 |
| Light 2 | 4 | 3 | 14 | 10 | 4 |
| Light 3 | 6 | 4 | 19 | 16 | 6 |
| Heavy | 13 | 4 | 23 | 26 | 7 |
| Launcher | 8 | 4 | 21 | 12 | 5 |
| Air light | 4 | 4 | 17 | 20 | 5 |

Light 1/2 can chain on a confirmed hit during grounded recovery, excluding its last three ticks. Heavy, Light 3, and air light complete recovery. Launcher can cancel into jump only after contact. Air light is the sole launcher follow-up. The target's identity is stable and each activation can hit it once.

## Presentation and provenance

`ImpactProject.Configure` serializes the bootstrap scene, URP 2D renderer, material, and project settings. The composition root creates and explicitly retains all runtime references. `ArenaArt` authors rectangular geometry and silhouettes; `ImpactFeedback` authors bounded flash geometry and synthesizes impact waveforms. These are original repository code, with no external art, model, sound sample, Meshy output, or new production dependency. Shader, font, and engine resources are Unity-provided and subject to Unity's terms.

The optional diagnostic HUD reports allocation and CPU measurements around simulation and owned rendering. Four-Hz diagnostic string formatting and event-driven health/label changes are outside that measured interval; these UI allocations are not represented as zero total-frame GC. Whole-frame duration includes frame pacing and the engine. Absolute low-spec performance remains a later reference-machine check.
