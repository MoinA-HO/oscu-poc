import type { ReactNode } from 'react';
import { describe, expect, it, vi } from 'vitest';
import { QueryClientProvider } from '@tanstack/react-query';
import { renderHook, waitFor } from '@testing-library/react';
import { createTestQueryClient, jsonResponse } from '@/test/renderWithProviders';
import { referralKeys, useCreateReferral, useDeleteReferral, useReferral, useReferralList } from './useReferrals';
import { defaultReferralListQuery } from '../types';
import type { Referral } from '../types';

const aReferral: Referral = {
  id: '0195f0a0-0000-7000-8000-000000000001',
  referralReference: 'REF-0001',
  subject: 'Safeguarding concern',
  description: null,
  status: 'New',
  receivedDate: '2026-08-01T09:30:00Z',
  createdDate: '2026-08-11T12:00:00Z'
};

function wrapperFor(queryClient = createTestQueryClient()) {
  function Wrapper({ children }: { children: ReactNode }) {
    return <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>;
  }

  return { Wrapper, queryClient };
}

describe('referralKeys', () => {
  it('nests list and detail keys under the shared root so one call invalidates everything', () => {
    expect(referralKeys.lists()[0]).toBe('referrals');
    expect(referralKeys.detail('abc')[0]).toBe('referrals');
  });

  it('produces a different key for a different query, so filters are cached separately', () => {
    const first = referralKeys.list({ ...defaultReferralListQuery, status: 'New' });
    const second = referralKeys.list({ ...defaultReferralListQuery, status: 'Closed' });

    expect(first).not.toEqual(second);
  });
});

describe('useReferralList', () => {
  it('returns the page on success', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn(() =>
        Promise.resolve(
          jsonResponse({
            isSuccess: true,
            data: { items: [aReferral], page: 1, pageSize: 20, totalCount: 1, totalPages: 1 }
          })
        )
      )
    );

    const { Wrapper } = wrapperFor();
    const { result } = renderHook(() => useReferralList(defaultReferralListQuery), { wrapper: Wrapper });

    await waitFor(() => expect(result.current.isSuccess).toBe(true));

    expect(result.current.data?.items).toHaveLength(1);
  });

  it('surfaces an error state rather than throwing', async () => {
    vi.stubGlobal('fetch', vi.fn(() => Promise.resolve(jsonResponse({ status: 500, title: 'Boom' }, 500))));

    const { Wrapper } = wrapperFor();
    const { result } = renderHook(() => useReferralList(defaultReferralListQuery), { wrapper: Wrapper });

    await waitFor(() => expect(result.current.isError).toBe(true));
  });
});

describe('useReferral', () => {
  it('does not fetch until an id is supplied', () => {
    const fetchStub = vi.fn();
    vi.stubGlobal('fetch', fetchStub);

    const { Wrapper } = wrapperFor();
    renderHook(() => useReferral(undefined), { wrapper: Wrapper });

    expect(fetchStub).not.toHaveBeenCalled();
  });
});

describe('useCreateReferral', () => {
  it('seeds the detail cache and invalidates the lists on success', async () => {
    vi.stubGlobal('fetch', vi.fn(() => Promise.resolve(jsonResponse({ isSuccess: true, data: aReferral }, 201))));

    const { Wrapper, queryClient } = wrapperFor();
    const invalidate = vi.spyOn(queryClient, 'invalidateQueries');

    const { result } = renderHook(() => useCreateReferral(), { wrapper: Wrapper });

    result.current.mutate({
      referralReference: 'REF-0001',
      subject: 'Safeguarding concern',
      description: null,
      status: 'New',
      receivedDate: '2026-08-01T09:30:00.000Z'
    });

    await waitFor(() => expect(result.current.isSuccess).toBe(true));

    // The created referral is already in cache, so navigating straight to its
    // edit page does not trigger a redundant fetch.
    expect(queryClient.getQueryData(referralKeys.detail(aReferral.id))).toEqual(aReferral);
    expect(invalidate).toHaveBeenCalledWith({ queryKey: referralKeys.lists() });
  });

  it('does not retry a failed create', async () => {
    // Retrying could produce a duplicate referral.
    const fetchStub = vi.fn(() => Promise.resolve(jsonResponse({ status: 500, title: 'Boom' }, 500)));
    vi.stubGlobal('fetch', fetchStub);

    const { Wrapper } = wrapperFor();
    const { result } = renderHook(() => useCreateReferral(), { wrapper: Wrapper });

    result.current.mutate({
      referralReference: 'REF-0001',
      subject: 'Safeguarding concern',
      description: null,
      status: 'New',
      receivedDate: '2026-08-01T09:30:00.000Z'
    });

    await waitFor(() => expect(result.current.isError).toBe(true));

    expect(fetchStub).toHaveBeenCalledTimes(1);
  });
});

describe('useDeleteReferral', () => {
  it('drops the deleted referral from the detail cache', async () => {
    vi.stubGlobal('fetch', vi.fn(() => Promise.resolve(new Response(null, { status: 204 }))));

    const { Wrapper, queryClient } = wrapperFor();
    queryClient.setQueryData(referralKeys.detail(aReferral.id), aReferral);

    const { result } = renderHook(() => useDeleteReferral(), { wrapper: Wrapper });

    result.current.mutate(aReferral.id);

    await waitFor(() => expect(result.current.isSuccess).toBe(true));

    expect(queryClient.getQueryData(referralKeys.detail(aReferral.id))).toBeUndefined();
  });
});
