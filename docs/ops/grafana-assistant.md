# Grafana Assistant (Rules + Playbooks) for FHAS

This repo keeps ops guidance in Markdown first, so it can be reviewed in PRs and iterated after incidents. The same content can later be mirrored into Grafana Cloud's Assistant rules and playbooks UI.

## Rules (always-on)
Rules are short, high-leverage statements that should apply to almost every ops conversation:
- Always ask for `x-trace-id` (or a timeframe + endpoint + model) before diagnosing.
- Never paste PHI/PII, API keys, or tokens into chats, issues, or dashboards.
- When describing a failure, include: HTTP status, endpoint, `model`, and error `code` from the API error envelope.
- Prefer links to dashboards/panels and paste the exact query used (PromQL/LogQL/TraceQL) when possible.
- If a mitigation changes behavior (rate limit, allowlist, model), record it in the post-incident checklist.

## Playbooks (runbooks)
Playbooks are repeatable investigation procedures for a specific symptom. Keep them:
- Narrow: one symptom per playbook.
- Chunked: use headings so assistants can jump to the right section.
- Actionable: queries to run, decision points, and safe mitigations.
- Iterated: after every incident, update the playbook with what you learned.

Repo playbooks live in `docs/ops/playbooks/`:
- `docs/ops/playbooks/api-latency.md`
- `docs/ops/playbooks/api-5xx.md`
- `docs/ops/playbooks/openrouter-upstream.md`
- `docs/ops/playbooks/otel-pipeline.md`

## Update Loop (after an incident)
1. Add a short "What changed?" note to the relevant playbook.
2. Add at least one new query that would have made detection faster.
3. Add one preventative action (alert, SLO, rate limit tweak, allowlist, or safer defaults).
4. If you added logs/spans/metrics to make this debuggable, link the PR in the playbook.

