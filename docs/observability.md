# Observability (OpenTelemetry)

The API (`apps/api/Fhas.Api`) is instrumented with OpenTelemetry:
- ASP.NET Core request tracing
- HttpClient tracing for upstream calls (OpenRouter)
- A custom span around the chat completion step (`ActivitySource` = `Fhas.Api.Chat`)
- A custom span around agent execution (`ActivitySource` = `Fhas.Agent.Runtime`)
- Internal skill/tool invocation spans (`skill.invoke`)

## Default behavior
- In `Development`, traces are exported to the console if no OTLP endpoint is configured.
- If `OTEL_EXPORTER_OTLP_ENDPOINT` (or per-signal OTLP env vars) is set, the API exports to OTLP instead.

## Correlation
Each API response includes an `x-trace-id` header. Use it to jump from the UI into traces.

## Aspire dashboard (local UI)
You can run the Aspire dashboard standalone and send OTLP to it:
```bash
docker run --rm -it -d \
  -p 18888:18888 \
  -p 4317:18889 \
  --name aspire-dashboard \
  mcr.microsoft.com/dotnet/aspire-dashboard:latest
```

Then start the API with:
```bash
export OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317
dotnet run --project apps/api/Fhas.Api
```

Open `http://localhost:18888` and enter the token printed in the container logs.

## Grafana Cloud (OTLP)
Grafana Cloud can ingest OTLP directly. At a high level:
1. Create a Grafana Cloud stack and an API key for OTLP ingest.
2. Configure these env vars when running the API:

```bash
export OTEL_EXPORTER_OTLP_ENDPOINT="<your Grafana Cloud OTLP endpoint>"
export OTEL_EXPORTER_OTLP_HEADERS="Authorization=Basic <base64(instance_id:api_key)>"
```

Notes:
- If Grafana Cloud expects HTTP/protobuf instead of gRPC, set `OTEL_EXPORTER_OTLP_PROTOCOL=http/protobuf`.
- Add resource attributes as needed:
  - `OTEL_RESOURCE_ATTRIBUTES="deployment.environment=dev,service.namespace=fhas"`
