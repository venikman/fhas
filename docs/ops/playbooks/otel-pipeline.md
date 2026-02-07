# Playbook: OpenTelemetry Pipeline Problems (Missing Traces/Metrics/Logs)

## Symptoms
- No traces in Grafana Cloud or the Aspire dashboard.
- Exporter errors on startup or during requests.
- `x-trace-id` exists in responses, but nothing shows up in the backend.

## First Checks
- Confirm the app is emitting instrumentation locally:
  - In `Development` without OTLP env vars, the API should export traces to console.
- Confirm required env vars are set:
  - `OTEL_EXPORTER_OTLP_ENDPOINT`
  - `OTEL_EXPORTER_OTLP_HEADERS` (Grafana Cloud)
  - `OTEL_EXPORTER_OTLP_PROTOCOL` (if required by the endpoint)
- Confirm the service name is stable:
  - `service.name=fhas-api`

## Decision Tree
1. Are you using the Aspire dashboard locally?
   - Verify the dashboard container is running and ports are mapped.
2. Are you exporting to Grafana Cloud?
   - Ensure OTLP endpoint is correct and headers are set.
3. Protocol mismatch?
   - Some endpoints require `http/protobuf` instead of gRPC.
4. Network path issues?
   - DNS, firewall, corporate proxy, or blocked ports.

## Mitigations
- Local Aspire:
  - Restart the dashboard container and re-check the token from logs.
  - Point the API at the correct OTLP endpoint.
- Grafana Cloud:
  - Recreate/regenerate the OTLP API key and update `OTEL_EXPORTER_OTLP_HEADERS`.
  - Explicitly set `OTEL_EXPORTER_OTLP_PROTOCOL=http/protobuf` if required.
- If the pipeline is down:
  - Keep console exporter enabled in development for debugging.
  - Avoid disabling tracing during incidents; it removes the evidence you need.

## Post-Incident Update Checklist
- Add a quick-start snippet for the failing configuration (endpoint/protocol/headers).
- Add an alert on exporter error logs (if you collect logs).
- Add one "known good" trace query example using `x-trace-id`.

