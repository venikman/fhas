# ExecPlan: FHAS Baseline (React Template + C# Agent Framework + Grafana-Ready OTel)

## Goal

Bootstrap a working, reviewable starter repo for an interoperable “health skills” platform:

- Frontend: keep the React template as the stable developer UX baseline (tests/lint/build already wired).
- Backend: C# ASP.NET Core with agentic behavior implemented using Microsoft Agent Framework.
- Skills: internal-only in this repo (no A2A, no Mastra, no MCP exposure for now).
- Observability: OpenTelemetry-first plumbing suitable for local development (Aspire dashboard) and Grafana Cloud ingestion.

## Success criteria (observable)

- `cd apps/web && npm ci && npm test && npm run build` succeeds.
- `dotnet build` and `dotnet test` succeed for all .NET projects.
- Running the backend exposes:
  - `GET /healthz` returns 200
  - `GET /swagger` loads OpenAPI UI in development
  - `POST /api/v1/chat/completions` returns an OpenAI-compatible JSON response shape sufficient for the UI to function
  - `GET /api/v1/skills` returns `{ id, description }[]`
- Agent Framework execution happens inside the chat endpoint (agent run + internal skill/tool execution).
- OpenTelemetry emits traces/metrics (console in dev or OTLP export) and includes request correlation via `x-trace-id`.

## Non-goals

- Full SMART on FHIR auth flows, EHR launch context, or clinical-grade security posture.
- Production-grade multi-tenant identity, secrets management, or deployment.
- Implementing the full `health-skillz` feature set; only scaffolding for “skills” and interop boundaries.

## Constraints (sandbox, network, OS, time, dependencies)

- OS: macOS (Darwin arm64).
- Tooling: `dotnet` 10.x, Node.js 25.x available.
- Network is required to restore npm and NuGet dependencies.
- Microsoft Agent Framework is preview; pin versions and treat upgrades as deliberate milestones.
- Avoid long-running foreground processes; prefer `--timeout` for smoke runs.

## Repo map (key files/dirs)

- `AGENTS.md`: contribution/agent workflow rules.
- `PLANS.md`: this execution plan and progress log.
- `apps/web/`: React frontend (from `venikman/poke`, adapted).
- `apps/api/`: C# backend service (ASP.NET Core).

## Milestones

1. Frontend template lock-in
   - Steps:
     - Keep `apps/web` independent of any Node API runtime; use only the C# API surface.
     - Ensure lint/test/build remain stable.
   - Validation:
     - `cd apps/web && npm ci && npm test && npm run build`
     - Expected: all commands succeed.
   - Rollback:
     - `git revert` the milestone commit(s).

2. Agent Framework runtime inside C# API
   - Steps:
     - Add an agent runner service in `apps/api/Fhas.Agent`:
       - build OpenAI `ChatClient` pointed at OpenRouter base URL
       - wrap it into an Agent Framework agent
       - expose internal `ISkill` implementations as function tools
     - Update `POST /api/v1/chat/completions` to call the runner.
     - Keep `OPENROUTER_MOCK=1` deterministic path for tests.
     - Add `MODEL_ALLOWLIST` (comma-separated) enforcement.
     - Decide streaming behavior (either implement or return explicit 400 when `stream=true`).
   - Validation:
     - `dotnet build`
     - `dotnet test`
     - `OPENROUTER_MOCK=1 dotnet run --project apps/api/Fhas.Api --urls http://localhost:8080`
     - `curl http://localhost:8080/api/v1/chat/health`
     - `curl -X POST http://localhost:8080/api/v1/chat/completions -H 'Content-Type: application/json' -d '{\"messages\":[{\"role\":\"user\",\"content\":\"Hello\"}]}'`
     - Expected: 200s + OpenAI-format response.
   - Rollback:
     - `git revert` the milestone commit(s).

3. OTel: agent + tool tracing and Grafana-ready export
   - Steps:
     - Ensure traces cover inbound HTTP, agent run(s), tool/skill execution, outbound LLM calls.
     - Ensure responses include `x-trace-id`.
   - Validation:
     - Start API in development without OTLP env vars and confirm spans are printed (console exporter).
     - Start API with `OTEL_EXPORTER_OTLP_ENDPOINT` pointing at a local collector/Aspire dashboard and confirm ingestion.
   - Rollback:
     - `git revert` the milestone commit(s).

## Decisions log (why changes)

- Use `venikman/poke` as the frontend base to preserve familiar tooling.
- Use an OpenAI-compatible HTTP surface for early UI integration; agent/skill orchestration can evolve behind it without breaking the UI.
- Use OpenTelemetry as the canonical evidence/telemetry surface so Grafana (and other backends) can be added without app rewrites.
- LLM provider: OpenRouter first (OpenAI-compatible API).
- Skills remain internal-only in this baseline (no A2A, no Mastra, no MCP exposure).

## Progress log (ISO-8601 timestamps)

Note: older entries are historical and may reference files removed in later cleanups.

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
- 2026-02-07T06:14:00Z
  - Done: Validated frontend template lock-in: `npm ci`, `npm test`, `npm run build` all succeed in `apps/web`.
  - Next: Replace `/api/v1/chat/completions` internals to run via Microsoft Agent Framework and OpenAI .NET `ChatClient` pointed at OpenRouter.
  - Blockers: None.
- 2026-02-07T06:28:27Z
  - Done: Replaced `/api/v1/chat/completions` to run via Microsoft Agent Framework (`OpenRouterAgentRunner`) with internal skills exposed as function tools.
  - Done: Updated docs for `OPENROUTER_API_KEY` + `MODEL_ALLOWLIST`; validated `dotnet build`, `dotnet test`, and a mock-mode curl smoke run.
  - Next: Add explicit per-skill/tool spans and repo ops playbooks (Grafana Assistant rules/playbooks guidance).
  - Blockers: None.
- 2026-02-07T06:33:40Z
  - Done: Added explicit OpenTelemetry spans for skill/tool invocation (`skill.invoke`) and ensured `x-trace-id` is returned on all endpoints.
  - Done: Added a test proving skill invocation emits an Activity; validated `dotnet build` and `dotnet test`.
  - Next: Add ops playbooks + `docs/ops/grafana-assistant.md`.
  - Blockers: None.
- 2026-02-07T06:35:43Z
  - Done: Added repo-first ops guidance: `docs/ops/grafana-assistant.md` plus initial playbooks in `docs/ops/playbooks/`.
  - Done: Validated `dotnet test` still passes.
  - Next: (Optional) mirror playbooks into Grafana Cloud and start the first FHIR/SMART skill milestone.
  - Blockers: None.
- 2026-02-07T08:48:39Z
  - Done: Removed `docs/` and cleaned references.
  - Done: `dotnet test --no-restore` passes (6 tests).
  - Next: None.
  - Blockers: None.
