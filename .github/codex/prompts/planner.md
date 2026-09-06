# Planner task

Act as the planner defined in `AGENTS.md`. Read the active slice, latest independent review, CI evidence, and earlier decision log.

For every finding, record one disposition: required now, rejected with repository evidence, or deferred with a named future slice. Update the active slice with the smallest ordered correction plan, tests, non-goals, and risks. Split work if the correction would change the player-facing hypothesis.

Do not implement. End with `<!-- dodger-loop:plan:HEAD_SHA -->`, replacing `HEAD_SHA` with the reviewed commit.

