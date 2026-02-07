# Playbook: API Latency Regression

## Symptoms
- p95/p99 latency increases for the API.
- UI stalls or times out on `/api/v1/chat/completions`.
- Elevated request duration without a matching increase in request volume.

## First Queries
- Traces: find slow requests using `x-trace-id` from the client response header.
- Traces: filter to `service.name=fhas-api` and sort by duration; inspect spans:
  - `agent.chat.completions` (API handler)
  - `agent.run` (Agent Framework)
  - `skill.invoke` (internal tools)
  - outbound HTTP spans to OpenRouter (HttpClient)
- Metrics: request duration by route and status code.
- Metrics: 429 rate limiting count (if present).

## Decision Tree
1. Is latency only on `/api/v1/chat/completions`?
2. Is latency correlated with a specific `model`?
3. Is time spent mostly in:
   - outbound OpenRouter call spans (upstream latency), or
   - internal spans (`agent.run`, `skill.invoke`)?
4. Are requests sending unusually high `max_tokens`?

## Mitigations
- If OpenRouter spans dominate:
  - Switch to a faster model (and/or enforce `MODEL_ALLOWLIST`).
  - Reduce `max_tokens` in callers.
  - Reduce concurrency at the edge (rate limit or queue).
- If internal tool spans dominate:
  - Optimize the hot skill/tool implementation.
  - Add caching inside the tool if it is deterministic and safe.
- If request volume dominates:
  - Tighten per-IP rate limiting.
  - Require `API_TOKEN` for non-local environments.

## Post-Incident Update Checklist
- Add one query/panel that would have shown this sooner.
- Add an alert threshold (route-level p95/p99).
- Record the affected `model` and request parameters (no PHI).
- Update default limits (rate limit / `max_tokens`) if needed.

