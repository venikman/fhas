# FHAS Web

Frontend (Rsbuild + React Router) that calls the C# API at `/api/v1`.

## Getting Started
1. Install dependencies:
   - `npm install`
2. Start dev:
   - `npm run dev`

This starts:
- Web dev server: `http://localhost:5173`
- API (C# / ASP.NET Core): `http://localhost:8080`

## API Usage
The UI uses the `openai` JS SDK, pointed at the local API (not OpenRouter directly):

```ts
import OpenAI from 'openai';

const openai = new OpenAI({
  baseURL: `${window.location.origin}/api/v1`,
  apiKey: 'dummy', // backend validates if API_TOKEN set; no keys stored in frontend
  dangerouslyAllowBrowser: true,
});

const completion = await openai.chat.completions.create({
  model: 'x-ai/grok-4.1-fast',
  messages: [{ role: 'user', content: 'Hello!' }],
});
```

## Backend Config (FYI)
Set these on the API process (see `/Users/stas-studio/Developer/fhas/docs/development.md`):
- `OPENROUTER_API_KEY` (or `GROK_KEY` as a legacy alias)
- `OPENROUTER_MOCK=1` to return deterministic responses without upstream calls
- `API_TOKEN` to require `Authorization: Bearer <token>`
- `MODEL_ALLOWLIST` to restrict which models can be used
- `OPENROUTER_REFERER` / `OPENROUTER_TITLE` optional attribution headers

## Legacy
The original Node/Hono server code from the template lives in `/Users/stas-studio/Developer/fhas/apps/web/_legacy/server-node` and is not used by `npm run dev`.
