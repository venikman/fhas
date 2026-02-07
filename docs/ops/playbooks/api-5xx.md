# Playbook: API 5xx Spike

## Symptoms
- Spike in 5xx responses from the API.
- UI reports errors on `/api/v1/chat/completions`.

## First Queries
- Metrics: error rate by route and status code (focus on 5xx).
- Logs: search for `upstream_error`, `network_error`, or missing-config errors.
- Traces: pick a failing request using `x-trace-id` and inspect:
  - inbound request span (route/status)
  - `agent.chat.completions` span
  - outbound HTTP spans (OpenRouter)

## Decision Tree
1. Is the failure only for `/api/v1/chat/completions`?
2. What status code?
   - 500: often misconfiguration (missing `OPENROUTER_API_KEY`) or unhandled exception.
   - 502: upstream connectivity issues (network) or upstream failure propagation.
3. Is the error envelope `error.code`:
   - `network_error`
   - `upstream_error`
   - `server_error`

## Mitigations
- If missing configuration:
  - Ensure `OPENROUTER_API_KEY` is set in the runtime environment.
  - Validate `MODEL_ALLOWLIST` doesn't exclude the requested model unexpectedly.
- If upstream failure:
  - Follow `docs/ops/playbooks/openrouter-upstream.md`.
- If app exception:
  - Reproduce with a minimal request body.
  - Add a targeted try/catch + better error envelope mapping (avoid leaking secrets).

## Post-Incident Update Checklist
- Add an alert for 5xx rate by route.
- Add a log line (without PHI) for each `error.code` class if diagnosis was slow.
- Add a regression test if a specific request shape triggered the error.

