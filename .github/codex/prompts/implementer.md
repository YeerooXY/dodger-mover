# Implementer task

Act as the implementer defined in `AGENTS.md`. Follow the active slice's latest correction plan exactly.

Audit affected code before editing, keep dependency direction intact, add focused tests, run every available required check, and update progress/evidence in the slice. Do not add adjacent features. Never report an unavailable check as passing.

Report changed behavior, verification evidence, remaining risk, and `<!-- dodger-loop:implementation:NEW_HEAD_SHA -->` using the resulting commit.

