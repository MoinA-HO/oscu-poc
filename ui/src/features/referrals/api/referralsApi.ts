import { apiFetch, apiFetchData } from '@/lib/apiClient';
import type { PagedResult } from '@/lib/types';
import type { CreateReferralRequest, Referral, ReferralListQuery, UpdateReferralRequest } from '../types';

const basePath = '/api/referrals';

/**
 * Builds the query string, omitting empty optional filters.
 *
 * Sending `search=` for a cleared search box would make the server filter on
 * an empty term rather than not filtering at all.
 */
export function buildListQueryString(query: ReferralListQuery): string {
  const params = new URLSearchParams({
    page: String(query.page),
    pageSize: String(query.pageSize),
    sortBy: query.sortBy,
    sortDirection: query.sortDirection
  });

  if (query.search?.trim()) {
    params.set('search', query.search.trim());
  }

  if (query.status?.trim()) {
    params.set('status', query.status.trim());
  }

  return params.toString();
}

export function getReferrals(query: ReferralListQuery, signal?: AbortSignal): Promise<PagedResult<Referral>> {
  return apiFetchData<PagedResult<Referral>>(`${basePath}?${buildListQueryString(query)}`, { signal });
}

export function getReferral(id: string, signal?: AbortSignal): Promise<Referral> {
  return apiFetchData<Referral>(`${basePath}/${id}`, { signal });
}

export function getReferralStatuses(signal?: AbortSignal): Promise<string[]> {
  return apiFetchData<string[]>(`${basePath}/statuses`, { signal });
}

export function createReferral(request: CreateReferralRequest): Promise<Referral> {
  return apiFetchData<Referral>(basePath, { method: 'POST', body: request });
}

export function updateReferral(id: string, request: UpdateReferralRequest): Promise<Referral> {
  return apiFetchData<Referral>(`${basePath}/${id}`, { method: 'PUT', body: request });
}

/** Returns 204 No Content, so there is nothing to unwrap. */
export function deleteReferral(id: string): Promise<void> {
  return apiFetch<void>(`${basePath}/${id}`, { method: 'DELETE' });
}
