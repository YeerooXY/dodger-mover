# Asset pipeline

## Principle

Generate only assets that serve the active slice. Concepts, runtime-ready files, source files, and provenance are separate deliverables; a concept image is not silently treated as production art.

## Preferred paths

1. Code-drawn primitives and placeholders for mechanical prototypes.
2. Image generation for concepts, textures, backgrounds, effects, and 2D source material.
3. SVG or procedural construction for exact interface and diagrammatic elements.
4. Meshy for complex props, weapons, turnarounds, 3D references, or render-to-sprite workflows where it adds clear value.

Meshy is not the default character-animation solution for this 2D game.

## Provenance

Every generated or third-party production asset records:

- stable asset ID and repository path;
- source tool/service and model when known;
- prompt or source URL;
- creation date and task ID;
- material edits and conversion steps;
- license or usage-rights status;
- active slice that requested it.

## Meshy budget contract

- Hard ceiling: 200 credits per UTC calendar day.
- Only requests with `approved` status may be reserved.
- The queue reserves the full estimated maximum before dispatch.
- Concurrent jobs are grouped into bounded waves; concurrency never weakens the daily ceiling.
- Work that does not fit remains approved and is reconsidered by priority on the next daily run.
- No API call occurs until the exact batch and cost have been confirmed by the user.
- Completed Meshy outputs are downloaded immediately because provider-side retention is limited.
- `MESHY_API_KEY` stays in secret storage or a local ignored environment file.

Repository code plans and reserves work; actual Meshy API calls use the official installed Meshy integration rather than a second API implementation.
