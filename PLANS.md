# ExecPlan: FHAS Bootstrap (React + C# Agent Backend + OpenTelemetry)

## Goal
Bootstrap a working, reviewable starter repo for an interoperable “health skills” platform:
- Frontend: React app based on `venikman/poke` tooling and UX patterns.
- Backend: C# ASP.NET Core service using Microsoft Agent Framework primitives as the backbone for future agent/skill orchestration.
- Observability: OpenTelemetry-first plumbing suitable for local development and Grafana Cloud ingestion.

## Success criteria (observable)
- `npm test` passes for the frontend package.
- `dotnet build` succeeds for all .NET projects.
- Running the backend exposes:
  - `GET /healthz` returns 200
  - `GET /swagger` loads OpenAPI UI in development
  - `POST /api/v1/chat/completions` returns an OpenAI-compatible JSON response shape sufficient for the UI to function
- OpenTelemetry emits traces/metrics (at least to console or OTLP endpoint) and includes request correlation.

## Non-goals
- Full SMART on FHIR auth flows, EHR launch context, or clinical-grade security posture.
- Production-grade multi-tenant identity, secrets management, or deployment.
- Implementing the full `health-skillz` feature set; only scaffolding for “skills” and interop boundaries.

## Constraints (sandbox, network, OS, time, dependencies)
- OS: macOS (Darwin arm64).
- Tooling: `dotnet` 10.x, Node.js 25.x available.
- Network is required to:
  - Pull `venikman/poke` as a starting template reference.
  - Restore npm and NuGet dependencies.
- Avoid long-running foreground processes; prefer `--timeout` for smoke runs.

## Repo map (key files/dirs)
- `AGENTS.md`: contribution/agent workflow rules.
- `PLANS.md`: this execution plan and progress log.
- `apps/web/`: React frontend (from `venikman/poke`, adapted).
- `apps/api/`: C# backend service (ASP.NET Core).
- `docs/`: architecture notes, interop notes, and observability/Grafana setup.

## Milestones
1. Repo scaffolding + plan
   - Steps:
     - Add `AGENTS.md`, `PLANS.md`, `.gitignore`, basic `README.md`.
     - Initialize git and create baseline commit.
   - Validation:
     - `git status` shows clean worktree.
   - Rollback:
     - `git revert` the baseline commit.

2. Import and verify frontend template
   - Steps:
     - Vendor the `venikman/poke` frontend into `apps/web/`.
     - Ensure tests run and the dev server starts.
   - Validation:
     - `cd apps/web && npm install && npm test`
   - Rollback:
     - `git revert` the import commit.

3. Add C# backend skeleton compatible with frontend
   - Steps:
     - Create `apps/api` ASP.NET Core Web API.
     - Implement `GET /healthz` and `POST /api/v1/chat/completions` (OpenAI-ish shape).
     - Update frontend dev proxy to point at the C# service.
   - Validation:
     - `dotnet build`
     - `dotnet run --project apps/api/...` then `curl` health + chat endpoints
   - Rollback:
     - `git revert` backend commit.

4. Observability plumbing (OTel + Grafana-ready)
   - Steps:
     - Add OpenTelemetry tracing/metrics/logging to backend.
     - Document Grafana Cloud OTLP env var configuration.
     - (Optional) add local docker compose for an OTLP-compatible stack.
   - Validation:
     - Run backend and confirm OTel output or export succeeds (no exporter errors).
   - Rollback:
     - `git revert` observability commit.

5. DX polish
   - Steps:
     - Root-level scripts to run web + api together.
     - Add concise docs for configuration and troubleshooting.
   - Validation:
     - `./scripts/dev.sh` (or equivalent) brings up both with expected ports.
   - Rollback:
     - `git revert` DX commit.

## Decisions log (why changes)
- Use `venikman/poke` as the frontend base to preserve familiar tooling.
- Use an OpenAI-compatible HTTP surface for early UI integration; agent/skill orchestration can evolve behind it without breaking the UI.
- Use OpenTelemetry as the canonical evidence/telemetry surface so Grafana (and other backends) can be added without app rewrites.

## Progress log (ISO-8601 timestamps)
- 2026-02-07T03:31:53Z
  - Done: Initialized git repo; added baseline docs (`AGENTS.md`, `PLANS.md`, `README.md`) and repo hygiene.
  - Done: Imported `venikman/poke` template into `apps/web` and verified frontend tests.
  - Next: Replace the Node/Hono API with a C# backend in `apps/api` (OpenAI-compatible HTTP surface).
  - Blockers: None.
- 2026-02-07T03:44:57Z
  - Done: Added C# API (`apps/api/Fhas.Api`) implementing `GET /api/v1/chat/health` and `POST /api/v1/chat/completions` with OpenRouter proxy + mock mode.
  - Done: Added backend integration tests (`apps/api/Fhas.Api.Tests`) and verified `dotnet test`.
  - Done: Updated web dev proxy to the C# API; `apps/web` no longer compiles/depends on the Node server.
  - Next: Decide how deeply to adopt Microsoft Agent Framework in the first “skill” and whether to add a full .NET Aspire AppHost (not just dashboards).
  - Blockers: None.
- 2026-02-07T03:47:26Z
  - Done: Added `apps/api/Fhas.Agent` (class library) with Microsoft Agent Framework packages referenced and a minimal “skills” scaffold.
  - Done: API now exposes `GET /api/v1/skills` and registers a starter `echo` skill.
  - Next: Wire a first real “health skill” (FHIR-aware) behind the agent framework abstractions.
  - Blockers: None.
