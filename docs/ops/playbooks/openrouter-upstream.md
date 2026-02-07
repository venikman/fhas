# Playbook: OpenRouter Upstream Errors

## Symptoms
- API returns `502` with `error.code=network_error` or `error.code=upstream_error`.
- Increased latency where outbound OpenRouter spans dominate the trace.
- Sudden increase in `401`, `403`, `429`, or `5xx` from upstream.

## First Queries
- Traces: find outbound HTTP spans and inspect status codes and duration.
- Metrics: outbound error rate (HttpClient) if collected.
- Logs: look for "OpenRouter returned a non-success response" or "Failed to reach OpenRouter".

## Decision Tree
1. Upstream status category?
   - 401/403: likely bad/expired API key.
   - 429: rate limit / quota exhaustion.
   - 5xx: upstream incident or transient failure.
   - timeouts/DNS: local network path issue.
2. Is it model-specific (only one `model`)?
3. Are we sending unusual parameters (very high `max_tokens`)?

## Mitigations
- 401/403:
  - Verify `OPENROUTER_API_KEY` is set correctly (and not empty/rotated).
  - Rotate the key if needed.
- 429:
  - Tighten API rate limiting.
  - Reduce concurrency from callers.
  - Enforce `MODEL_ALLOWLIST` to prevent expensive models if needed.
- 5xx / timeouts:
  - Retry at the caller with jittered backoff (if safe).
  - Switch to an alternate model/provider as a controlled change (later milestone).

## Post-Incident Update Checklist
- Add a dashboard panel for upstream status breakdown (2xx/4xx/5xx).
- Add an alert for sustained upstream 429/5xx.
- Record which `model` was affected and what mitigation worked.

