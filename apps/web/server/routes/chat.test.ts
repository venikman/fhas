import { post, get } from './chat.js';

// ─────────────────────────────────────────────────────────────────────────────
// Completions tests
// ─────────────────────────────────────────────────────────────────────────────

test('returns OpenAI-format chat completion', async () => {
  const previous = process.env.OPENROUTER_MOCK;
  process.env.OPENROUTER_MOCK = '1';

  try {
    const result = await post({
      data: {
        messages: [{ role: 'user', content: 'Hello' }],
      },
    });

    if (result instanceof Response) {
      throw new Error('Expected data, got Response');
    }

    expect(result.id).toMatch(/^chatcmpl-/);
    expect(result.object).toBe('chat.completion');
    expect(result.model).toBeDefined();
    expect(result.choices).toHaveLength(1);
    expect((result.choices as Array<{ message: { role: string; content: string } }>)[0].message.role).toBe('assistant');
    expect((result.choices as Array<{ message: { role: string; content: string } }>)[0].message.content).toBe('Mocked response.');
    expect((result.choices as Array<{ finish_reason: string }>)[0].finish_reason).toBe('stop');
    expect(result.usage).toBeDefined();
    expect((result.usage as { prompt_tokens: number }).prompt_tokens).toBeGreaterThanOrEqual(0);
    expect((result.usage as { completion_tokens: number }).completion_tokens).toBeGreaterThanOrEqual(0);
  } finally {
    if (previous === undefined) {
      delete process.env.OPENROUTER_MOCK;
    } else {
      process.env.OPENROUTER_MOCK = previous;
    }
  }
});

test('returns error for missing messages', async () => {
  const result = await post({ data: {} as never });

  expect(result).toBeInstanceOf(Response);
  const res = result as Response;
  expect(res.status).toBe(400);
  const body = (await res.json()) as { error: { message: string } };
  expect(body.error).toBeDefined();
  expect(body.error.message).toContain('messages');
});

test('returns error for empty messages array', async () => {
  const result = await post({
    data: { messages: [] },
  });

  expect(result).toBeInstanceOf(Response);
  const res = result as Response;
  expect(res.status).toBe(400);
  const body = (await res.json()) as { error: { message: string } };
  expect(body.error.message).toContain('empty');
});

// ─────────────────────────────────────────────────────────────────────────────
// Health tests
// ─────────────────────────────────────────────────────────────────────────────

test('returns healthy status with required fields', async () => {
  const result = await get();

  expect(result.status).toBe('healthy');
  expect(typeof result.uptime_ms).toBe('number');
  expect(result.uptime_ms).toBeGreaterThanOrEqual(0);
  expect(result.timestamp).toMatch(/^\d{4}-\d{2}-\d{2}T/);
  expect(result.version).toBeDefined();
});

test('uptime increases over time', async () => {
  const first = await get();
  await new Promise((r) => setTimeout(r, 50));
  const second = await get();

  expect(second.uptime_ms).toBeGreaterThan(first.uptime_ms);
});

test('timestamp is valid ISO string', async () => {
  const result = await get();
  const parsed = new Date(result.timestamp);

  expect(parsed.toISOString()).toBe(result.timestamp);
});
