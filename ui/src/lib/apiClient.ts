import { ApiError } from './apiError';
import type { ApiEnvelope, ProblemDetails } from './types';

/**
 * Origin only, with no path.
 *
 * Empty in development, where Vite proxies /api to the API and requests are
 * same-origin. The source template set this to a full endpoint path and then
 * appended the same path again in the client, producing /api/data-table twice.
 */
const baseUrl = (import.meta.env.VITE_API_BASE_URL as string | undefined) ?? '';

type RequestOptions = Omit<RequestInit, 'body'> & { body?: unknown };

async function readProblem(response: Response): Promise<ProblemDetails> {
  try {
    return (await response.json()) as ProblemDetails;
  } catch {
    // A proxy error or a crash before the handler can produce a non-JSON body.
    return { status: response.status, title: response.statusText };
  }
}

/**
 * Issues a request and returns the parsed body, throwing {@link ApiError} on
 * any non-2xx response.
 */
export async function apiFetch<T>(path: string, options: RequestOptions = {}): Promise<T> {
  const { body, headers, ...rest } = options;

  const response = await fetch(`${baseUrl}${path}`, {
    ...rest,
    headers: {
      Accept: 'application/json',
      ...(body === undefined ? {} : { 'Content-Type': 'application/json' }),
      ...headers
    },
    ...(body === undefined ? {} : { body: JSON.stringify(body) })
  });

  if (!response.ok) {
    throw new ApiError(response.status, await readProblem(response));
  }

  // 204 No Content, which DELETE returns.
  if (response.status === 204 || response.headers.get('content-length') === '0') {
    return undefined as T;
  }

  return (await response.json()) as T;
}

/**
 * Issues a request against an endpoint that returns the standard envelope and
 * unwraps it, so callers deal in domain types rather than transport shapes.
 */
export async function apiFetchData<T>(path: string, options: RequestOptions = {}): Promise<T> {
  const envelope = await apiFetch<ApiEnvelope<T>>(path, options);

  return envelope.data;
}
