import React, { useState } from 'react';
import OpenAI from 'openai';
import Counter from '../components/Counter';

/** Token from auth/SSO session — wire your auth provider here. No keys stored in frontend. */
function getAuthToken(): string | null {
  // TODO: return token from auth context / SSO session when wired
  return null;
}

const baseURL =
  typeof window !== 'undefined'
    ? `${window.location.origin}/api/v1`
    : 'http://localhost:8080/api/v1';

const DEFAULT_PROMPT =
  'Analyze the following normalized numeric data and provide a concise summary: -1.22, 0, 1.22';

type Message = { role: 'user' | 'assistant'; content: string };
// eslint-disable-next-line @typescript-eslint/no-explicit-any
type ApiPayload = any;

const Page: React.FC = () => {
  const [status, setStatus] = useState<string>('idle');
  const [prompt, setPrompt] = useState<string>(DEFAULT_PROMPT);
  const [messages, setMessages] = useState<Message[]>([]);
  const [payload, setPayload] = useState<ApiPayload>(null);

  const runInference = async () => {
    if (!prompt.trim()) return;

    setStatus('running');
    setPayload(null);

    // Add user message to the conversation
    const userMessage: Message = { role: 'user', content: prompt };
    setMessages((prev) => [...prev, userMessage]);

    const token = getAuthToken();
    const openai = new OpenAI({
      baseURL,
      apiKey: token ?? 'dummy', // token from auth/SSO; backend validates if API_TOKEN set
      dangerouslyAllowBrowser: true,
    });

    try {
      const completion = await openai.chat.completions.create({
        model: 'x-ai/grok-4.1-fast',
        messages: [userMessage],
      });

      // Add assistant response to the conversation
      const assistantContent =
        completion.choices[0]?.message?.content ?? '(no response)';
      setMessages((prev) => [
        ...prev,
        { role: 'assistant', content: assistantContent },
      ]);

      setPayload(completion);
      setStatus('done');
    } catch (error) {
      setStatus('error');
      setMessages((prev) => [
        ...prev,
        {
          role: 'assistant',
          content: `Error: ${error instanceof Error ? error.message : 'Unknown error'}`,
        },
      ]);
      setPayload({
        ok: false,
        error: error instanceof Error ? error.message : 'Unknown error',
      });
    }
  };

  const clearConversation = () => {
    setMessages([]);
    setPayload(null);
    setStatus('idle');
  };

  return (
    <main style={{ fontFamily: 'system-ui, sans-serif', padding: '2rem' }}>
      <h1>Full-Stack AI RS Template</h1>
      <p>Rsbuild + Hono + React Router + Rstest + OpenRouter (Grok)</p>

      <section style={{ marginBottom: '2rem' }}>
        <h2>Counter Demo</h2>
        <Counter initialValue={0} min={0} max={100} step={1} />
      </section>

      <section>
        <h2>AI Inference</h2>

        {/* Message input */}
        <div style={{ marginBottom: '1rem' }}>
          <textarea
            value={prompt}
            onChange={(e) => setPrompt(e.target.value)}
            placeholder="Enter your message..."
            rows={3}
            style={{
              width: '100%',
              maxWidth: '600px',
              padding: '0.5rem',
              fontFamily: 'inherit',
              fontSize: '1rem',
              borderRadius: '4px',
              border: '1px solid #ccc',
            }}
          />
        </div>

        <div style={{ display: 'flex', gap: '0.5rem', marginBottom: '1rem' }}>
          <button
            type="button"
            onClick={runInference}
            disabled={status === 'running' || !prompt.trim()}
          >
            {status === 'running' ? 'Sending...' : 'Send Message'}
          </button>
          <button type="button" onClick={clearConversation}>
            Clear
          </button>
        </div>

        <p>Status: {status}</p>

        {/* Conversation display */}
        {messages.length > 0 && (
          <div
            style={{
              maxWidth: '600px',
              border: '1px solid #ddd',
              borderRadius: '8px',
              overflow: 'hidden',
              marginBottom: '1rem',
            }}
          >
            {messages.map((msg, idx) => (
              <div
                key={idx}
                style={{
                  padding: '0.75rem 1rem',
                  background: msg.role === 'user' ? '#e3f2fd' : '#f5f5f5',
                  borderBottom:
                    idx < messages.length - 1 ? '1px solid #ddd' : 'none',
                }}
              >
                <strong
                  style={{
                    color: msg.role === 'user' ? '#1565c0' : '#424242',
                  }}
                >
                  {msg.role === 'user' ? '📤 You' : '🤖 Assistant'}:
                </strong>
                <div
                  style={{
                    marginTop: '0.25rem',
                    whiteSpace: 'pre-wrap',
                    wordBreak: 'break-word',
                  }}
                >
                  {msg.content}
                </div>
              </div>
            ))}
          </div>
        )}

        {/* Raw response (collapsible) */}
        {payload && (
          <details style={{ maxWidth: '600px' }}>
            <summary style={{ cursor: 'pointer', marginBottom: '0.5rem' }}>
              Raw API Response
            </summary>
            <pre
              style={{
                background: '#f6f6f6',
                padding: '1rem',
                overflow: 'auto',
                fontSize: '0.85rem',
              }}
            >
              {JSON.stringify(payload, null, 2)}
            </pre>
          </details>
        )}
      </section>
    </main>
  );
};

export default Page;
