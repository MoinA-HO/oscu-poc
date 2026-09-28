/// <reference types="vitest/config" />
import { fileURLToPath, URL } from 'node:url';
import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

export default defineConfig({
  plugins: [react()],

  resolve: {
    alias: {
      // Absolute imports from src. Keeps feature code free of ../../../ chains
      // that break the moment a file moves.
      '@': fileURLToPath(new URL('./src', import.meta.url))
    }
  },

  server: {
    port: 5173,
    proxy: {
      // Dev requests to /api are proxied to the API, so the browser sees one
      // origin and CORS never enters the picture locally. It also means
      // VITE_API_BASE_URL can stay empty in development.
      '/api': {
        // The http launch profile, deliberately: the https one uses a
        // self-signed certificate that the proxy would reject.
        target: 'http://localhost:7501',
        changeOrigin: true
      }
    }
  },

  css: {
    preprocessorOptions: {
      scss: {
        loadPaths: ['node_modules'],
        // govuk-frontend still emits legacy Sass API deprecations; they are
        // not ours to fix and drown out real warnings.
        quietDeps: true
      }
    }
  },

  test: {
    globals: true,
    environment: 'jsdom',
    setupFiles: ['./src/test/setup.ts'],
    // Compiling the full govuk-frontend stylesheet for every test file costs
    // seconds and asserts nothing.
    css: false,
    coverage: {
      provider: 'v8',
      reporter: ['text', 'lcov'],
      include: ['src/**/*.{ts,tsx}'],
      exclude: ['src/**/*.test.{ts,tsx}', 'src/test/**', 'src/main.tsx']
    }
  }
});
