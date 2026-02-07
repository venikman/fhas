/**
 * Chat API routes - OpenAI-compatible Chat Completions proxy.
 * POST /completions — forwards to OpenRouter
 * GET /health — service health check
 */

import { Hono } from 'hono';

const DEFAULT_MODEL = 'x-ai/grok-4.1-fast';
const startTime = Date.now();

export const chatRoutes = new Hono();

// ─────────────────────────────────────────────────────────────────────────────
// Health check
// ─────────────────────────────────────────────────────────────────────────────

export interface HealthResponse {
  status: 'healthy' | 'degraded';
  uptime_ms: number;
  timestamp: string;
  version: string;
}

chatRoutes.get('/health', (c) => {
  const response: HealthResponse = {
    status: 'healthy',
    uptime_ms: Date.now() - startTime,
    timestamp: new Date().toISOString(),
    version: process.env.npm_package_version ?? '1.0.0',
  };
  return c.json(response);
});

// ─────────────────────────────────────────────────────────────────────────────
// Chat completions
// ─────────────────────────────────────────────────────────────────────────────

interface ChatMessage {
  role: 'user' | 'assistant' | 'system';
  content: string;
}

interface CompletionsRequest {
  messages: ChatMessage[];
  model?: string;
  stream?: boolean;
  temperature?: number;
  max_tokens?: number;
}

chatRoutes.post('/completions', async (c) => {
  let body: CompletionsRequest;
  try {
    body = await c.req.json();
  } catch {
    return c.json(
      {
        error: {
          message: 'Invalid JSON body',
          type: 'invalid_request_error',
          code: 'invalid_request_error',
        },
      },
      400,
    );
  }

  // Validate messages
  if (
    !body?.messages ||
    !Array.isArray(body.messages) ||
    body.messages.length === 0
  ) {
    return c.json(
      {
        error: {
          message: 'Request must include non-empty "messages" array.',
          type: 'invalid_request_error',
          code: 'invalid_request_error',
        },
      },
      400,
    );
  }

  // Mock mode for testing
  if (process.env.OPENROUTER_MOCK === '1') {
    return c.json({
      id: `chatcmpl-mock-${Date.now()}`,
      object: 'chat.completion',
      created: Math.floor(Date.now() / 1000),
      model: body.model ?? DEFAULT_MODEL,
      choices: [
        {
          index: 0,
          message: { role: 'assistant', content: 'Mocked response.' },
          finish_reason: 'stop',
        },
      ],
      usage: { prompt_tokens: 10, completion_tokens: 3, total_tokens: 13 },
    });
  }

  // Auth check - validate token if API_TOKEN is configured
  const apiToken = process.env.API_TOKEN;
  if (apiToken) {
    const auth = c.req.header('Authorization');
    const token = auth?.startsWith('Bearer ') ? auth.slice(7) : null;
    if (token !== apiToken) {
      return c.json(
        {
          error: {
            message: 'Invalid or missing API token',
            type: 'invalid_api_key',
            code: 'invalid_api_key',
          },
        },
        401,
      );
    }
  }

  // Proxy to OpenRouter
  const apiKey = process.env.GROK_KEY;
  if (!apiKey) {
    return c.json(
      {
        error: {
          message: 'Missing GROK_KEY.',
          type: 'server_error',
          code: 'server_error',
        },
      },
      500,
    );
  }

  const headers: Record<string, string> = {
    Authorization: `Bearer ${apiKey}`,
    'Content-Type': 'application/json',
  };
  if (process.env.OPENROUTER_REFERER)
    headers['HTTP-Referer'] = process.env.OPENROUTER_REFERER;
  if (process.env.OPENROUTER_TITLE)
    headers['X-Title'] = process.env.OPENROUTER_TITLE;

  const payload = {
    model: body.model ?? DEFAULT_MODEL,
    messages: body.messages,
    ...(body.stream !== undefined && { stream: body.stream }),
    ...(typeof body.temperature === 'number' && {
      temperature: body.temperature,
    }),
    ...(typeof body.max_tokens === 'number' && { max_tokens: body.max_tokens }),
  };

  try {
    const res = await fetch('https://openrouter.ai/api/v1/chat/completions', {
      method: 'POST',
      headers,
      body: JSON.stringify(payload),
    });

    const text = await res.text();
    if (!res.ok) {
      const status =
        res.status >= 400 && res.status < 600 ? res.status : 500;
      return c.json(
        {
          error: {
            message: `OpenRouter failed: ${text || res.status}`,
            type: 'upstream_error',
            code: 'upstream_error',
          },
        },
        status as 400,
      );
    }

    return c.json(JSON.parse(text));
  } catch (error) {
    return c.json(
      {
        error: {
          message: `Failed to reach OpenRouter: ${error instanceof Error ? error.message : 'Unknown error'}`,
          type: 'network_error',
          code: 'network_error',
        },
      },
      502,
    );
  }
});

// ─────────────────────────────────────────────────────────────────────────────
// Exported handlers for direct testing (preserves existing test interface)
// ─────────────────────────────────────────────────────────────────────────────

type RequestOption = { data?: unknown };

function err(message: string, status = 400): Response {
  return new Response(
    JSON.stringify({
      error: {
        message,
        type: 'invalid_request_error',
        code: 'invalid_request_error',
      },
    }),
    { status, headers: { 'Content-Type': 'application/json' } },
  );
}

/**
 * Direct handler for unit tests (bypasses HTTP and calls the logic directly).
 */
export const post = async (
  req: RequestOption,
): Promise<Record<string, unknown> | Response> => {
  const body = req.data as Record<string, unknown> | undefined;

  if (
    !body?.messages ||
    !Array.isArray(body.messages) ||
    body.messages.length === 0
  ) {
    return err('Request must include non-empty "messages" array.');
  }

  if (process.env.OPENROUTER_MOCK === '1') {
    return {
      id: 'chatcmpl-mock-' + Date.now(),
      object: 'chat.completion',
      created: Math.floor(Date.now() / 1000),
      model: (body.model as string) ?? DEFAULT_MODEL,
      choices: [
        {
          index: 0,
          message: { role: 'assistant', content: 'Mocked response.' },
          finish_reason: 'stop',
        },
      ],
      usage: { prompt_tokens: 10, completion_tokens: 3, total_tokens: 13 },
    };
  }

  // In direct test mode, we can't access headers, so skip auth check.
  // Real auth is handled in the Hono route above.

  const apiKey = process.env.GROK_KEY;
  if (!apiKey) return err('Missing GROK_KEY.', 500);

  const headers: Record<string, string> = {
    Authorization: `Bearer ${apiKey}`,
    'Content-Type': 'application/json',
  };
  if (process.env.OPENROUTER_REFERER)
    headers['HTTP-Referer'] = process.env.OPENROUTER_REFERER;
  if (process.env.OPENROUTER_TITLE)
    headers['X-Title'] = process.env.OPENROUTER_TITLE;

  const payload = {
    model: body.model ?? DEFAULT_MODEL,
    messages: body.messages,
    ...(body.stream !== undefined && { stream: body.stream }),
    ...(typeof body.temperature === 'number' && {
      temperature: body.temperature,
    }),
    ...(typeof body.max_tokens === 'number' && { max_tokens: body.max_tokens }),
  };

  const res = await fetch('https://openrouter.ai/api/v1/chat/completions', {
    method: 'POST',
    headers,
    body: JSON.stringify(payload),
  });

  const text = await res.text();
  if (!res.ok) {
    return err(
      `OpenRouter failed: ${text || res.status}`,
      res.status >= 400 && res.status < 600 ? res.status : 500,
    );
  }

  return JSON.parse(text) as Record<string, unknown>;
};

/**
 * Direct handler for health check testing
 */
export const get = async (): Promise<HealthResponse> => {
  return {
    status: 'healthy',
    uptime_ms: Date.now() - startTime,
    timestamp: new Date().toISOString(),
    version: process.env.npm_package_version ?? '1.0.0',
  };
};
