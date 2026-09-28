import { QueryClient } from '@tanstack/react-query';
import { ApiError } from './apiError';

/**
 * TanStack Query is the server-state layer.
 *
 * It was already a dependency of the source React template but was never
 * wired up — no QueryClientProvider existed and every page did its own
 * useState/useEffect/fetch dance, reimplementing loading flags, error handling
 * and refetching each time. See docs/adr/0003-react-state-management.md.
 */
export function createQueryClient(): QueryClient {
  return new QueryClient({
    defaultOptions: {
      queries: {
        // Caseworkers keep tabs open for hours; refetching on focus keeps a
        // stale list from being mistaken for the current one.
        refetchOnWindowFocus: true,
        staleTime: 30_000,
        retry: (failureCount, error) => {
          // Retrying a 4xx just repeats the same rejection more slowly.
          if (error instanceof ApiError && error.status < 500) {
            return false;
          }

          return failureCount < 2;
        }
      },
      mutations: {
        // A failed create must never be replayed automatically: it could
        // produce a duplicate referral.
        retry: false
      }
    }
  });
}
