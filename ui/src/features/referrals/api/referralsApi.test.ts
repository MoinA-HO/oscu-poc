import { describe, expect, it, vi } from 'vitest';
import { ApiError } from '@/lib/apiError';
import { jsonResponse } from '@/test/renderWithProviders';
import { buildListQueryString, createReferral, deleteReferral, getReferral, getReferrals } from './referralsApi';
import { defaultReferralListQuery } from '../types';
import type { Referral } from '../types';

const aReferral: Referral = {
  id: '0195f0a0-0000-7000-8000-000000000001',
  referralReference: 'REF-0001',
  subject: 'Safeguarding concern',
  description: 'Details.',
  status: 'New',
  receivedDate: '2026-08-01T09:30:00Z',
  createdDate: '2026-08-11T12:00:00Z'
};

function stubFetch(response: Response) {
  const stub = vi.fn(() => Promise.resolve(response));
  vi.stubGlobal('fetch', stub);

  return stub;
}

describe('buildListQueryString', () => {
  it('always sends page, pageSize and sort', () => {
    const params = new URLSearchParams(buildListQueryString(defaultReferralListQuery));

    expect(params.get('page')).toBe('1');
    expect(params.get('pageSize')).toBe('20');
    expect(params.get('sortBy')).toBe('ReceivedDate');
    expect(params.get('sortDirection')).toBe('Descending');
  });

  it('omits an empty search rather than sending a blank filter', () => {
    // Sending search= would make the server filter on an empty term instead of
    // not filtering at all.
    const params = new URLSearchParams(buildListQueryString({ ...defaultReferralListQuery, search: '   ' }));

    expect(params.has('search')).toBe(false);
  });

  it('trims a search term before sending it', () => {
    const params = new URLSearchParams(
      buildListQueryString({ ...defaultReferralListQuery, search: '  REF-0001  ' })
    );

    expect(params.get('search')).toBe('REF-0001');
  });

  it('includes a status filter when one is set', () => {
    const params = new URLSearchParams(buildListQueryString({ ...defaultReferralListQuery, status: 'Closed' }));

    expect(params.get('status')).toBe('Closed');
  });
});

describe('getReferrals', () => {
  it('unwraps the envelope and returns the page', async () => {
    stubFetch(
      jsonResponse({
        isSuccess: true,
        data: { items: [aReferral], page: 1, pageSize: 20, totalCount: 1, totalPages: 1 }
      })
    );

    const page = await getReferrals(defaultReferralListQuery);

    expect(page.items).toHaveLength(1);
    expect(page.items[0]?.referralReference).toBe('REF-0001');
  });
});

describe('getReferral', () => {
  it('throws an ApiError carrying the status and problem details on 404', async () => {
    stubFetch(
      jsonResponse({ status: 404, title: 'Not Found', detail: 'No referral was found.', code: 'Referral.NotFound' }, 404)
    );

    const error = await getReferral('missing-id').catch((caught: unknown) => caught);

    expect(error).toBeInstanceOf(ApiError);
    expect((error as ApiError).isNotFound).toBe(true);
    expect((error as ApiError).problem.code).toBe('Referral.NotFound');
  });
});

describe('createReferral', () => {
  it('POSTs the request as JSON', async () => {
    const stub = stubFetch(jsonResponse({ isSuccess: true, data: aReferral }, 201));

    await createReferral({
      referralReference: 'REF-0001',
      subject: 'Safeguarding concern',
      description: null,
      status: 'New',
      receivedDate: '2026-08-01T09:30:00.000Z'
    });

    const [, init] = stub.mock.calls[0] as unknown as [string, RequestInit];

    expect(init.method).toBe('POST');
    expect(JSON.parse(init.body as string)).toMatchObject({ referralReference: 'REF-0001' });
  });

  it('surfaces field-level validation errors from a 400', async () => {
    stubFetch(jsonResponse({ status: 400, title: 'Bad Request', errors: { Subject: ['Enter a subject.'] } }, 400));

    const error = (await createReferral({
      referralReference: 'REF-0001',
      subject: '',
      description: null,
      status: 'New',
      receivedDate: '2026-08-01T09:30:00.000Z'
    }).catch((caught: unknown) => caught)) as ApiError;

    expect(error.isValidationError).toBe(true);
    expect(error.validationErrors.Subject).toEqual(['Enter a subject.']);
  });
});

describe('deleteReferral', () => {
  it('tolerates a 204 with no body', async () => {
    stubFetch(new Response(null, { status: 204 }));

    await expect(deleteReferral(aReferral.id)).resolves.toBeUndefined();
  });
});
