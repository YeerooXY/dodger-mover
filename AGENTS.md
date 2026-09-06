# Dodger Mover agent contract

Read `docs/PRODUCT_CHARTER.md`, `docs/WORKFLOW.md`, the active slice in `docs/slices/`, and `docs/ARCHITECTURE.md` before changing code.

## Working agreement

- Audit the affected code and current slice before planning edits.
- Work on one bounded slice at a time. Do not silently add adjacent features.
- Record meaningful discoveries and decisions in the active slice document.
- Keep gameplay rules in the pure C# domain whenever they do not require Unity APIs.
- Keep configuration immutable at runtime. Runtime state must not mutate ScriptableObject assets.
- Add or update tests for every behavioral change.
- Profile before optimizing, but do not introduce recurring allocations in per-frame combat paths.
- Never commit credentials, generated Unity folders, local agent state, or `meshy_output/`.
- New production dependencies require an architecture decision record.
- Generated assets require provenance metadata before entering `Assets/`.

## Required verification

Run these checks before reporting completion:

```bash
python -m unittest discover -s tools/tests -p "test_*.py"
python tools/repo_guard.py
```

When Unity is available, also run all EditMode tests. PlayMode tests and a Windows development build are required for playable slices.

## Autonomous roles

### Reviewer

Review only the diff and relevant repository context. Do not modify code. Report consequential findings with severity, evidence, and a safe correction. Avoid style comments that deterministic tooling can enforce.

### Planner

Reconcile every review finding against the slice goal. Update the execution plan with ordered, testable steps and explicit non-goals. Split the slice if a correction would materially expand scope.

### Implementer

Follow the approved plan, make the smallest cohesive change, run verification, and report changed behavior, evidence, and remaining risk. Do not declare success when a required check was unavailable.

Maximum autonomous review/fix passes per pull-request revision: three. After that, redesign or split the work and record why.

## Code Review Rules

### Combat correctness

- Flag gameplay outcomes that depend on render frame rate, unordered physics queries, or mutable shared configuration. The safe path is deterministic simulation state with explicit timing and stable ordering.
- Flag state transitions that can leave a combatant simultaneously in incompatible states, skip recovery, or accept inputs outside documented cancel windows.

### Architecture

- Flag Unity presentation code that owns combat rules or domain code that depends on scenes, GameObjects, or global singletons. The safe dependency direction is presentation toward application/domain.
- Flag runtime mutation of ScriptableObject configuration. Create per-session state objects instead.

### Performance and evidence

- Flag recurring allocations, broad scene searches, or repeated component lookups in combat update paths.
- Flag behavioral changes without focused tests or without updating the active slice's acceptance evidence.

### Assets and security

- Flag generated or third-party assets without provenance, source, and usage-rights records.
- Flag secrets, personal tokens, API keys, or unsafe workflow permissions. Workflows receive the minimum permissions needed for their job.
