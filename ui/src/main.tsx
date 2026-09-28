import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { BrowserRouter } from 'react-router-dom';
import { QueryClientProvider } from '@tanstack/react-query';
import { initAll } from 'govuk-frontend';
import { App } from './App';
import { createQueryClient } from './lib/queryClient';
import './styles/main.scss';

const container = document.getElementById('root');

if (!container) {
  throw new Error('Root element #root was not found in index.html.');
}

// Wires up the design system's progressive enhancement (skip link focus,
// error summary focus management, character counts).
initAll();

createRoot(container).render(
  <StrictMode>
    <QueryClientProvider client={createQueryClient()}>
      <BrowserRouter>
        <App />
      </BrowserRouter>
    </QueryClientProvider>
  </StrictMode>
);
