/** The envelope every successful API response is wrapped in. */
export type ApiEnvelope<T> = {
  isSuccess: boolean;
  data: T;
};

/** One page of results, mirroring PagedResult<T> on the server. */
export type PagedResult<T> = {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
};

/** RFC 7807 problem response, as produced by the API for every failure. */
export type ProblemDetails = {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  /** Machine-readable error code, e.g. "Referral.DuplicateReference". */
  code?: string;
  /** Correlation reference for unhandled exceptions. */
  reference?: string;
  /** Field-level failures, keyed by property name, from a 400. */
  errors?: Record<string, string[]>;
};
