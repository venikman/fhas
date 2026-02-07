# AGENTS.md instructions for /Users/stas-studio/Developer/fhas
#
# Long-horizon mode (multi-hour tasks)

## Objective
Maximize probability of a correct, reviewable result with minimal user interruptions.

## Plan + state (anti-forgetting)
- If task looks >30 min OR touches >3 files OR is a refactor/migration, create/update PLANS.md at repo root (or .agent/PLANS.md if repo already uses it).
- Treat PLANS.md as the source of truth and a living document. Keep it self-contained so work can resume after thread resume or context compaction.
- PLANS.md must include: Goal, Success criteria, Non-goals, Constraints (sandbox/network/OS), Milestones (numbered), per-milestone validation commands + expected signals, rollback notes.

## Execution loop (repeat per milestone)
1. Read the exact files needed.
2. Implement the smallest reversible change.
3. Run the exact validation commands for this milestone (tests/lint/build/run).
4. Fix failures before moving on.
5. Commit small and often (one milestone or sub-milestone per commit).
6. Update PLANS.md progress log: Done / Next / Blockers.

## Reporting cadence (reduce chatter)
- Do not report after each micro-step.
- Report only at: (a) plan ready, (b) milestone complete, (c) blocked, (d) final delivery.
- Each report is max 3 bullets: Done, Next, Blockers.
- Always include: files changed, commands run, test results (pass/fail).

## Compaction handling
- If you detect context compaction or low context remaining: immediately re-open PLANS.md, write a 10-line STATE snapshot (current milestone, last commit, files touched, last command, next 2 actions), then continue from the next milestone.

## Safety, approvals, and scope
- Stay inside the project root/workspace by default.
- Prefer Worktree mode for parallel explorations; do not let two threads modify the same files.
- Avoid network calls and dependency upgrades unless required; ask once for permission and document why in PLANS.md.
- Never run destructive actions (rm -rf, irreversible migrations, prod deploy) without explicit approval + rollback plan.

## ExecPlans
When writing complex features or significant refactors, use an ExecPlan in PLANS.md from design to implementation.

## Long-running commands hygiene
- Avoid foreground commands that never exit (dev servers, tail -f, log follow).
- If needed, run with a timeout or background execution and document stop commands + expected success signal.

