# Full-Stack AI RS Template

A full-stack template using **Node.js** + Rsbuild (frontend) + Hono (API server), Rstest, and OpenRouter for AI inference.

> **Runtime**: Node.js v24+ (with tsx for TypeScript execution)

## Getting Started

1. Install dependencies:
   - `npm install`
2. Start the dev server:
   - `npm run dev` — starts frontend (http://localhost:5173) + API server (http://localhost:3001), requires `GROK_KEY` for AI features
   - `npm run dev:mock` — mock AI responses at http://localhost:8080, no API key needed
3. Build for production:
   - `npm run build`
4. Run the production server:
   - `npm run start`
5. Run tests:
   - `npm run test` — runs API/unit tests only
   - `npm run test:browser` — runs browser tests only (requires Playwright)
   - `npm run test:all` — runs both API and browser tests
   - `npm run test:browser:install` — installs Chromium for Playwright (run once)

## Mock Server

For local development without an API key, use the mock server:

```bash
npm run dev:mock
```

This starts:
- Rsbuild dev server on port 5173 (internal)
- Mock proxy server on port 8080 (use this URL)

The mock server intercepts `/api/v1/chat/*` endpoints and returns simulated AI responses. All other requests are proxied to the Rsbuild dev server.

Environment variables for mock server:
- `MOCK_PORT` — mock server port (default: 8080)
- `RSBUILD_PORT` — Rsbuild internal port (default: 5173)
- `MOCK_DELAY_MS` — simulated API latency in ms (default: 500)

## Rs-family toolchain

This template uses **Node.js** runtime with the **rs-family** stack: **Rsbuild** (frontend build), **Rstest** (browser tests), **Hono** (API server).

| Concern | Where to configure |
|--------|---------------------|
| **Frontend build (Rsbuild)** | `rsbuild.config.ts` — entry, aliases, dev server proxy. |
| **API server (Hono)** | `server/index.ts` — routes, middleware, rate limiting. |
| **Tests (Rstest)** | `rstest.config.ts` — projects (browser vs api), coverage, Playwright provider. |
| **Linting (RSLint)** | `rslint.json` — FP-style rules, no mutations, prefer const. |

## Code Quality

Linting with **RSLint** (rs-family, Go-based, enforces functional programming style):

```bash
npm run lint          # Check for issues
npm run lint:fix      # Auto-fix where possible
npm run check         # Lint + type check
```

### Functional Programming Rules

The linter enforces a small FP-leaning baseline:
- Prefer `const` over `let` (and no `var`)
- No parameter reassignment
- Prefer arrow callbacks / rest params / spread where applicable
- Warn on unused variables

## API

`POST /api/v1/chat/completions` — OpenAI-compatible Chat Completions. Use the `openai` package:

```typescript
import OpenAI from 'openai';

// Token comes from auth/SSO session — wire your auth provider. No keys stored in frontend.
const token = getAuthToken(); // from auth context / SSO session

const openai = new OpenAI({
  baseURL: 'http://localhost:5173/api/v1',  // or :8080 with mock server
  apiKey: token ?? 'dummy',  // pass token from session; backend validates if API_TOKEN set
  dangerouslyAllowBrowser: true,
});

const completion = await openai.chat.completions.create({
  model: 'x-ai/grok-4.1-fast',
  messages: [{ role: 'user', content: 'Hello!' }],
});
```

Set `GROK_KEY` for OpenRouter. Bearer auth uses `API_TOKEN` on the server; the client sends the token from the auth/SSO session (no keys in frontend).

## Environment

Set your OpenRouter key:

```bash
export GROK_KEY=your_openrouter_key
```

Optional bearer auth (token from auth/SSO session, not stored in frontend):

```bash
export API_TOKEN=your_secret_token
```

When set, the backend expects `Authorization: Bearer <token>`. The client obtains the token from the auth/SSO layer at runtime (session, auth context, etc.). If unset, no auth is required.

Optional OpenRouter headers:

```bash
export OPENROUTER_REFERER=https://your-app.example
export OPENROUTER_TITLE="Full-Stack AI RS Template"
```
