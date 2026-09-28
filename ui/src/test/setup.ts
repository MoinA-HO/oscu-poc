import '@testing-library/jest-dom/vitest';
import { cleanup } from '@testing-library/react';
import { afterEach, beforeEach, vi } from 'vitest';

// Unmount between tests so a leaked component cannot satisfy the next test's
// queries.
afterEach(() => {
  cleanup();
  vi.restoreAllMocks();
});

beforeEach(() => {
  // Every test that talks to the API must stub fetch explicitly. Leaving the
  // real one in place lets a forgotten stub turn into a hanging network call.
  vi.stubGlobal(
    'fetch',
    vi.fn(() => Promise.reject(new Error('Unexpected network call: stub fetch in the test.')))
  );
});
