import type { ProblemDetails } from './types';

/**
 * A non-2xx response from the API, carrying the parsed ProblemDetails.
 *
 * Subclassing Error rather than throwing a plain object keeps stack traces and
 * lets callers narrow with `instanceof` instead of duck-typing.
 */
export class ApiError extends Error {
  public readonly status: number;
  public readonly problem: ProblemDetails;

  constructor(status: number, problem: ProblemDetails) {
    super(problem.detail ?? problem.title ?? `Request failed with status ${status}`);

    this.name = 'ApiError';
    this.status = status;
    this.problem = problem;
  }

  /** Field-level validation failures from a 400, keyed by property name. */
  public get validationErrors(): Record<string, string[]> {
    return this.problem.errors ?? {};
  }

  public get isValidationError(): boolean {
    return this.status === 400 && Object.keys(this.validationErrors).length > 0;
  }

  public get isNotFound(): boolean {
    return this.status === 404;
  }

  public get isConflict(): boolean {
    return this.status === 409;
  }
}
