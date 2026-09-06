# S000: Autonomous foundation

- Status: REVIEWING
- Owner role: Planner
- Player build: Not required

## Goal

Create a repository that can safely support autonomous, review-driven game development without relying on undocumented conversational context.

## Deliverables

- Product charter, architecture, workflow, roadmap, and decision record.
- Root and role-specific agent guidance.
- Pinned Unity project manifest.
- Pure C# combat kernel and EditMode tests.
- Automated repository guard.
- Asset request schema and hard 200-credit/day Meshy planner.
- S001 executable specification.
- GitHub pull-request event automation.

## Non-goals

- Playable movement or combat.
- Final scene, art, animation, audio, or game title.
- Automatic Meshy spending.
- Progression, content, or save systems.

## Acceptance criteria

- Repository-level unit tests pass.
- Repository guard reports no errors.
- All source assets under `Assets/` have metadata.
- Daily asset plans cannot exceed 200 reserved plus consumed credits.
- An independent review finds no unresolved P0/P1 issue.
- Missing Unity execution is explicitly reported rather than presented as passing.

## Progress

- [x] Repository audited; it was empty.
- [x] Engine version researched and pinned.
- [x] Architecture and workflow drafted.
- [x] Local verification complete: 11 repository automation tests and repository guard pass.
- [ ] Unity EditMode execution: not run because Unity is unavailable in the implementation environment; manual CI remains gated on `UNITY_LICENSE`.
- [x] Independent review complete: no unresolved P0-P3 findings after the correction pass.
- [ ] Pull request opened.
- [x] Event automation creation succeeded for pull-request lifecycle, commit, review, and comment events.
- [ ] Event automation live verification: pending the first pull-request event.

## Decisions and discoveries

- A root commit was necessary before a review branch could exist in the empty repository.
- Meshy execution remains outside repository code. The repository queue prepares and budgets approved work; the official Meshy integration performs actual API calls.
- Unity is not installed in the implementation environment. No claim is made that EditMode tests passed; the first licensed CI run is an explicit remaining gate before S001 implementation.
- Unity 6.3's documented package lines are pinned: Input System 1.20.0, URP 17.3.0, and Test Framework 1.6.0. The first editor import must still generate resolution evidence.
- Because the repository is public, write-capable webhook behavior is restricted to owner-authored, same-repository branches. External pull requests and untrusted comments cannot authorize mutations.
- The independent foundation review reported no remaining findings. Unity import, EditMode execution, package-lock generation, and the first live GitHub CI/webhook event remain explicitly unverified.
- The first live repository-guard event reached the coordinator and rejected duplicate terminal blank lines that local non-Git checks could not see. This revision normalizes every tracked text file and adds an explicit no-index whitespace verification to the evidence run.
