import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import type { UseMutationResult, UseQueryResult } from '@tanstack/react-query';
import type { PagedResult } from '@/lib/types';
import * as api from '../api/referralsApi';
import type { CreateReferralRequest, Referral, ReferralListQuery, UpdateReferralRequest } from '../types';

/**
 * Query key factory.
 *
 * Keys built inline at each call site drift, and then an invalidation quietly
 * stops matching the query it was meant to refresh. One factory, one source of
 * truth, and `referralKeys.all` invalidates every referral query at once.
 */
export const referralKeys = {
  all: ['referrals'] as const,
  lists: () => [...referralKeys.all, 'list'] as const,
  list: (query: ReferralListQuery) => [...referralKeys.lists(), query] as const,
  details: () => [...referralKeys.all, 'detail'] as const,
  detail: (id: string) => [...referralKeys.details(), id] as const,
  statuses: () => [...referralKeys.all, 'statuses'] as const
};

export function useReferralList(query: ReferralListQuery): UseQueryResult<PagedResult<Referral>> {
  return useQuery({
    queryKey: referralKeys.list(query),
    queryFn: ({ signal }) => api.getReferrals(query, signal),
    // Keeps the previous page rendered while the next one loads, so the table
    // does not collapse to a spinner on every page change.
    placeholderData: (previous) => previous
  });
}

export function useReferral(id: string | undefined): UseQueryResult<Referral> {
  return useQuery({
    queryKey: referralKeys.detail(id ?? ''),
    queryFn: ({ signal }) => api.getReferral(id!, signal),
    enabled: Boolean(id)
  });
}

export function useReferralStatuses(): UseQueryResult<string[]> {
  return useQuery({
    queryKey: referralKeys.statuses(),
    queryFn: ({ signal }) => api.getReferralStatuses(signal),
    // The controlled vocabulary changes when the server is redeployed, not
    // while someone has the page open.
    staleTime: Infinity
  });
}

export function useCreateReferral(): UseMutationResult<Referral, Error, CreateReferralRequest> {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: CreateReferralRequest) => api.createReferral(request),
    onSuccess: (created) => {
      queryClient.setQueryData(referralKeys.detail(created.id), created);
      // Every list is now potentially stale: the new referral may belong on
      // any page under any filter.
      void queryClient.invalidateQueries({ queryKey: referralKeys.lists() });
    }
  });
}

export function useUpdateReferral(
  id: string
): UseMutationResult<Referral, Error, UpdateReferralRequest> {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: UpdateReferralRequest) => api.updateReferral(id, request),
    onSuccess: (updated) => {
      queryClient.setQueryData(referralKeys.detail(id), updated);
      void queryClient.invalidateQueries({ queryKey: referralKeys.lists() });
    }
  });
}

export function useDeleteReferral(): UseMutationResult<void, Error, string> {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => api.deleteReferral(id),
    onSuccess: (_result, id) => {
      queryClient.removeQueries({ queryKey: referralKeys.detail(id) });
      void queryClient.invalidateQueries({ queryKey: referralKeys.lists() });
    }
  });
}
