/**
 * Hono API server with security hardening (Node.js runtime).
 * - Serves API routes at /api/v1/chat/*
 * - In production, also serves static files from dist/
 */

import { serve } from '@hono/node-server';
import { serveStatic } from '@hono/node-server/serve-static';
import { Hono, type Context } from 'hono';
import { cors } from 'hono/cors';
import { secureHeaders } from 'hono/secure-headers';
import { chatRoutes } from './routes/chat.js';

const app = new Hono();

// Security middleware
app.use('*', secureHeaders());
app.use(
  '*',
  cors({
    origin: process.env.CORS_ORIGIN || '*',
    allowMethods: ['GET', 'POST', 'OPTIONS'],
    allowHeaders: ['Content-Type', 'Authorization'],
    maxAge: 86400,
  }),
);

// Simple in-memory rate limiting (100 requests per minute per IP)
const rateLimitMap = new Map<string, { count: number; resetTime: number }>();
const RATE_LIMIT = 100;
const WINDOW_MS = 60 * 1000;

app.use('/api/*', async (c: Context, next) => {
  const ip =
    c.req.header('x-forwarded-for') ||
    c.req.header('x-real-ip') ||
    'unknown';
  const now = Date.now();
  const record = rateLimitMap.get(ip);

  if (!record || now > record.resetTime) {
    rateLimitMap.set(ip, { count: 1, resetTime: now + WINDOW_MS });
  } else if (record.count >= RATE_LIMIT) {
    return c.json(
      { error: { message: 'Too many requests', type: 'rate_limit_error' } },
      429,
    );
  } else {
    record.count++;
  }

  await next();
});

// API routes
app.route('/api/v1/chat', chatRoutes);

// Architecture diagrams (LikeC4 static site)
app.use('/architecture/*', serveStatic({ root: './likec4/dist', rewriteRequestPath: (path) => path.replace('/architecture', '') }));
app.get('/architecture', (c) => c.redirect('/architecture/'));

// Production: serve static files
if (process.env.NODE_ENV === 'production') {
  app.use('/*', serveStatic({ root: './dist' }));
  // SPA fallback - serve index.html for all non-API routes
  app.get('*', serveStatic({ path: './dist/index.html' }));
}

const port = parseInt(process.env.PORT || '3001', 10);

serve({ fetch: app.fetch, port }, () => {
  console.log(`Server running on http://localhost:${port}`);
  console.log(`   Environment: ${process.env.NODE_ENV || 'development'}`);
});
