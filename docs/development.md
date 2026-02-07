# Development

## Prereqs
- .NET SDK 10
- Node.js (use a current LTS or newer)
- Docker (optional, for local telemetry dashboards)

## Run (web + api)
From `apps/web`:
```bash
npm install
npm run dev
```

This starts:
- Web dev server: `http://localhost:5173`
- API (C#): `http://localhost:8080`

## Environment variables (API)
- `OPENROUTER_MOCK=1`: return a deterministic mocked chat completion (no upstream call).
- `GROK_KEY`: OpenRouter API key (required when not mocking).
- `API_TOKEN`: if set, require `Authorization: Bearer <token>` for chat requests.
- `OPENROUTER_REFERER`: optional header forwarded to OpenRouter as `HTTP-Referer`.
- `OPENROUTER_TITLE`: optional header forwarded to OpenRouter as `X-Title`.
- `CORS_ORIGIN`: comma-separated list of allowed origins. Default is `*` (dev-friendly).

## Useful endpoints
- `GET http://localhost:8080/api/v1/chat/health`
- `POST http://localhost:8080/api/v1/chat/completions`
- `GET http://localhost:8080/api/v1/skills`
