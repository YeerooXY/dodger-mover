# Reviewer task

Act only as the independent reviewer defined in `AGENTS.md`.

Read the active slice, the pull-request diff, relevant tests, and existing architecture. Look for consequential correctness, state-machine, frame-rate dependence, allocation, scope, test-evidence, asset-provenance, and workflow-permission problems.

Do not edit files. Return:

1. Findings ordered P0 through P3, each with concrete evidence and a safe correction.
2. Acceptance criteria that remain unverified.
3. A verdict: `BLOCK`, `CORRECT`, or `READY`.
4. `<!-- dodger-loop:review:HEAD_SHA -->`, replacing `HEAD_SHA` with the reviewed commit.

If no consequential findings exist, say so explicitly. Do not invent style work.

