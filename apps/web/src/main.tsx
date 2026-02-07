import React from 'react';
import { createRoot } from 'react-dom/client';
import { createBrowserRouter, RouterProvider } from 'react-router';
import Layout from './routes/layout';
import Page from './routes/page';

const router = createBrowserRouter([
  {
    path: '/',
    element: <Layout />,
    children: [{ index: true, element: <Page /> }],
  },
]);

const container = document.getElementById('root');
if (!container) throw new Error('Root element not found');

createRoot(container).render(
  <React.StrictMode>
    <RouterProvider router={router} />
  </React.StrictMode>,
);
