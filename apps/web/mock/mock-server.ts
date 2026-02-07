/**
 * Mock Server for local development without real AI calls (Node.js runtime).
 *
 * Run with: npm run dev:mock
 * Then use exactly like the real server at http://localhost:8080
 *
 * The mock server proxies to Rsbuild dev server for static assets,
 * but intercepts /api/v1/chat/* endpoints with mock responses.
 */

import { serve } from '@hono/node-server';
import { spawn, type ChildProcess } from 'child_process';
import { setTimeout as sleep } from 'timers/promises';

// ─────────────────────────────────────────────────────────────────────────────
// Configuration
// ─────────────────────────────────────────────────────────────────────────────

const MOCK_PORT = parseInt(process.env.MOCK_PORT ?? '8080', 10);
const RSBUILD_PORT = parseInt(process.env.RSBUILD_PORT ?? '5173', 10);
const MOCK_DELAY_MS = parseInt(process.env.MOCK_DELAY_MS ?? '500', 10);

// ─────────────────────────────────────────────────────────────────────────────
// Mock Response Generators
// ─────────────────────────────────────────────────────────────────────────────

type ChatMessage = { role: string; content: string };
type ChatRequest = { messages: ChatMessage[]; model?: string; temperature?: number; max_tokens?: number };

const generateMockCompletion = (request: ChatRequest): Record<string, unknown> => {
  const lastUserMsg = [...request.messages].reverse().find((m) => m.role === 'user');
  const userContent = (lastUserMsg?.content ?? '').toLowerCase();

  let responseContent: string;

  if (userContent.includes('hello') || userContent.includes('hi')) {
    responseContent = "Hello! I'm a mock AI response. The real AI is not connected, but I can help you test the UI flow.";
  } else if (userContent.includes('analyze') || userContent.includes('data')) {
    responseContent = `## Mock Analysis

Based on the data provided:

1. **Pattern detected**: The values show a normalized distribution centered around zero
2. **Range**: From -1.22 to 1.22 (symmetric around origin)
3. **Interpretation**: This appears to be z-score normalized data

*Note: This is a mock response for local development.*`;
  } else if (userContent.includes('code') || userContent.includes('function')) {
    responseContent = `## Mock Code Response

Here's a sample implementation:

\`\`\`typescript
function processData(values: number[]): { mean: number; std: number } {
  const mean = values.reduce((a, b) => a + b, 0) / values.length;
  const variance = values.reduce((a, b) => a + (b - mean) ** 2, 0) / values.length;
  return { mean, std: Math.sqrt(variance) };
}
\`\`\`

*Note: This is a mock response for local development.*`;
  } else {
    responseContent = `## Mock Response

I received your message: "${lastUserMsg?.content ?? '(empty)'}"

This is a mock response because the server is running in mock mode. To use real AI:
1. Set the \`GROK_KEY\` environment variable
2. Run \`npm run dev\` instead of \`npm run dev:mock\`

*Note: This is a mock response for local development.*`;
  }

  return {
    id: `chatcmpl-mock-${Date.now()}`,
    object: 'chat.completion',
    created: Math.floor(Date.now() / 1000),
    model: request.model ?? 'mock-model',
    choices: [
      {
        index: 0,
        message: { role: 'assistant', content: responseContent },
        finish_reason: 'stop',
      },
    ],
    usage: {
      prompt_tokens: Math.floor(Math.random() * 100) + 50,
      completion_tokens: Math.floor(Math.random() * 200) + 100,
      total_tokens: Math.floor(Math.random() * 300) + 150,
    },
  };
};

const startTime = Date.now();
const generateMockHealth = (): Record<string, unknown> => ({
  status: 'healthy',
  mock: true,
  uptime_ms: Date.now() - startTime,
  timestamp: new Date().toISOString(),
  version: '1.0.0',
});

// ─────────────────────────────────────────────────────────────────────────────
// Mock Server Handler
// ─────────────────────────────────────────────────────────────────────────────

const handleMockRequest = async (req: Request): Promise<Response> => {
  const url = new URL(req.url);
  const path = url.pathname;

  // CORS preflight
  if (req.method === 'OPTIONS') {
    return new Response(null, {
      status: 204,
      headers: {
        'Access-Control-Allow-Origin': '*',
        'Access-Control-Allow-Methods': 'GET, POST, OPTIONS',
        'Access-Control-Allow-Headers': 'Content-Type, Authorization',
      },
    });
  }

  const corsHeaders = {
    'Access-Control-Allow-Origin': '*',
    'Content-Type': 'application/json',
  };

  // Intercept API endpoints
  if (path.startsWith('/api/v1/chat/')) {
    const apiPath = path.slice('/api/v1/chat'.length);
    console.log(`[Mock] ${req.method} ${path} -> intercepted`);

    try {
      // Health endpoint
      if (apiPath === '/health' && req.method === 'GET') {
        return Response.json(generateMockHealth(), { headers: corsHeaders });
      }

      // Completions endpoint
      if (apiPath === '/completions' && req.method === 'POST') {
        const body = (await req.json()) as ChatRequest;

        // Log the incoming request
        console.log('\n┌─────────────────────────────────────────────────────────────');
        console.log('│ INCOMING REQUEST');
        console.log('├─────────────────────────────────────────────────────────────');
        console.log('│ Model:', body.model ?? '(default)');
        console.log('│ Temperature:', body.temperature ?? '(default)');
        console.log('│ Max tokens:', body.max_tokens ?? '(default)');
        console.log('│ Messages:');
        for (const msg of body.messages ?? []) {
          const content = msg.content.length > 100 ? msg.content.slice(0, 100) + '...' : msg.content;
          console.log(`│   [${msg.role}]: ${content}`);
        }
        console.log('└─────────────────────────────────────────────────────────────\n');

        // Simulate network delay
        if (MOCK_DELAY_MS > 0) {
          await sleep(MOCK_DELAY_MS);
        }

        const response = generateMockCompletion(body);

        // Log the outgoing response
        const responseContent = (response.choices as any)?.[0]?.message?.content ?? '';
        const truncatedResponse = responseContent.length > 200 ? responseContent.slice(0, 200) + '...' : responseContent;
        console.log('┌─────────────────────────────────────────────────────────────');
        console.log('│ MOCK RESPONSE');
        console.log('├─────────────────────────────────────────────────────────────');
        console.log('│ ID:', response.id);
        console.log('│ Model:', response.model);
        console.log('│ Content:', truncatedResponse.split('\n').join('\n│          '));
        console.log('└─────────────────────────────────────────────────────────────\n');

        return Response.json(response, { headers: corsHeaders });
      }

      // Unknown API endpoint
      return Response.json({ error: { message: 'Not found', path: apiPath } }, { status: 404, headers: corsHeaders });
    } catch (error) {
      console.error('[Mock] Error:', error);
      return Response.json({ error: { message: String(error) } }, { status: 500, headers: corsHeaders });
    }
  }

  // Proxy everything else to Rsbuild dev server
  try {
    const proxyUrl = `http://localhost:${RSBUILD_PORT}${path}${url.search}`;
    const proxyResponse = await fetch(proxyUrl, {
      method: req.method,
      headers: req.headers,
      body: req.method !== 'GET' && req.method !== 'HEAD' ? req.body : undefined,
    });
    return proxyResponse;
  } catch (err) {
    console.error('[Proxy] Error:', err);
    return Response.json({ error: 'Proxy error', message: String(err) }, { status: 502, headers: corsHeaders });
  }
};

// ─────────────────────────────────────────────────────────────────────────────
// Start Rsbuild Dev Server + Mock Server
// ─────────────────────────────────────────────────────────────────────────────

const waitForServer = async (port: number, maxAttempts = 60, delayMs = 500): Promise<void> => {
  for (let i = 0; i < maxAttempts; i++) {
    try {
      const res = await fetch(`http://localhost:${port}/`, { signal: AbortSignal.timeout(1000) });
      if (res.status < 500) return;
    } catch {
      // Server not ready yet
    }
    await sleep(delayMs);
  }
  throw new Error(`Server on port ${port} not ready after ${maxAttempts} attempts`);
};

const main = async () => {
  console.log('[Mock] Starting Rsbuild dev server on port', RSBUILD_PORT);

  // Start Rsbuild dev server using Node.js child_process
  const rsbuildProcess: ChildProcess = spawn('npx', ['rsbuild', 'dev', '--port', String(RSBUILD_PORT)], {
    env: { ...process.env },
    stdio: ['ignore', 'pipe', 'pipe'],
    shell: true,
  });

  // Stream Rsbuild output
  rsbuildProcess.stdout?.on('data', (data: Buffer) => {
    const output = data.toString();
    if (output.includes('ready') || output.includes('error') || output.includes('http://')) {
      process.stdout.write(`[Rsbuild] ${output}`);
    }
  });

  rsbuildProcess.stderr?.on('data', (data: Buffer) => {
    process.stderr.write(`[Rsbuild] ${data.toString()}`);
  });

  // Handle shutdown
  const shutdown = () => {
    console.log('\n[Mock] Shutting down...');
    rsbuildProcess.kill();
    process.exit(0);
  };

  process.on('SIGINT', shutdown);
  process.on('SIGTERM', shutdown);

  // Wait for Rsbuild to be ready
  console.log('[Mock] Waiting for Rsbuild dev server to be ready...');
  try {
    await waitForServer(RSBUILD_PORT);
    console.log('[Mock] Rsbuild dev server is ready');
  } catch (error) {
    console.error('[Mock] Failed to start Rsbuild dev server:', error);
    rsbuildProcess.kill();
    process.exit(1);
  }

  // Start mock server using @hono/node-server
  serve({ fetch: handleMockRequest, port: MOCK_PORT }, () => {
    console.log(`
╔══════════════════════════════════════════════════════════════╗
║                    MOCK DEV SERVER                           ║
╠══════════════════════════════════════════════════════════════╣
║  Mock server:    http://localhost:${String(MOCK_PORT).padEnd(27)}║
║  Rsbuild:        http://localhost:${String(RSBUILD_PORT).padEnd(27)}║
║  Mock delay:     ${String(MOCK_DELAY_MS + 'ms').padEnd(43)}║
║                                                              ║
║  Intercepted endpoints:                                      ║
║    GET  /api/v1/chat/health       Mock health check          ║
║    POST /api/v1/chat/completions  Mock AI completions        ║
║                                                              ║
║  All other requests proxied to Rsbuild dev server.           ║
║                                                              ║
║  To use real AI: npm run dev (with GROK_KEY set)             ║
╚══════════════════════════════════════════════════════════════╝
`);
  });
};

main().catch((error) => {
  console.error('[Mock] Fatal error:', error);
  process.exit(1);
});
