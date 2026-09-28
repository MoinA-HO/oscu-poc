import { describe, expect, it, vi } from 'vitest';
import { screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { jsonResponse, renderWithProviders } from '@/test/renderWithProviders';
import { ReferralListPage } from './ReferralListPage';
import type { Referral } from '../types';

const referrals: Referral[] = [
  {
    id: '0195f0a0-0000-7000-8000-000000000001',
    referralReference: 'REF-0001',
    subject: 'Safeguarding concern',
    description: null,
    status: 'New',
    receivedDate: '2026-08-01T09:30:00Z',
    createdDate: '2026-08-11T12:00:00Z'
  },
  {
    id: '0195f0a0-0000-7000-8000-000000000002',
    referralReference: 'REF-0002',
    subject: 'Fraud investigation',
    description: null,
    status: 'Closed',
    receivedDate: '2026-07-20T09:30:00Z',
    createdDate: '2026-08-11T12:00:00Z'
  }
];

/** Routes /statuses and the list endpoint, recording every URL requested. */
function stubApi(totalCount = referrals.length, totalPages = 1) {
  const requestedUrls: string[] = [];

  const stub = vi.fn((input: string | URL | Request) => {
    const url = String(input);
    requestedUrls.push(url);

    if (url.includes('/statuses')) {
      return Promise.resolve(jsonResponse({ isSuccess: true, data: ['New', 'In Progress', 'Closed'] }));
    }

    return Promise.resolve(
      jsonResponse({
        isSuccess: true,
        data: { items: referrals, page: 1, pageSize: 20, totalCount, totalPages }
      })
    );
  });

  vi.stubGlobal('fetch', stub);

  return { requestedUrls };
}

const listUrl = (urls: string[]) => urls.filter((url) => !url.includes('/statuses')).at(-1) ?? '';

/**
 * Waits for the table to render.
 *
 * Queried by role and accessible name rather than by text: the reference also
 * appears inside the visually hidden labels on the Change and Delete links, so
 * a bare text query matches three nodes.
 */
const findReferenceLink = (reference: string) => screen.findByRole('link', { name: reference });

describe('ReferralListPage', () => {
  it('renders a row per referral', async () => {
    stubApi();
    renderWithProviders(<ReferralListPage />, { route: '/referrals' });

    expect(await findReferenceLink('REF-0001')).toBeInTheDocument();
    expect(screen.getByText('Fraud investigation')).toBeInTheDocument();
  });

  it('formats the received date for display', async () => {
    stubApi();
    renderWithProviders(<ReferralListPage />, { route: '/referrals' });

    expect(await screen.findByText('1 August 2026')).toBeInTheDocument();
  });

  it('shows a message rather than an empty table when nothing matches', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn((input: string | URL | Request) =>
        Promise.resolve(
          String(input).includes('/statuses')
            ? jsonResponse({ isSuccess: true, data: [] })
            : jsonResponse({
                isSuccess: true,
                data: { items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0 }
              })
        )
      )
    );

    renderWithProviders(<ReferralListPage />, { route: '/referrals' });

    expect(await screen.findByText('No referrals match your search.')).toBeInTheDocument();
  });

  it('reads the initial filters from the URL', async () => {
    const { requestedUrls } = stubApi();
    renderWithProviders(<ReferralListPage />, { route: '/referrals?search=fraud&status=Closed&page=2' });

    await waitFor(() => expect(listUrl(requestedUrls)).toContain('search=fraud'));

    const url = listUrl(requestedUrls);
    expect(url).toContain('status=Closed');
    expect(url).toContain('page=2');
  });

  it('pushes a submitted search into the URL and refetches', async () => {
    const user = userEvent.setup();
    const { requestedUrls } = stubApi();
    renderWithProviders(<ReferralListPage />, { route: '/referrals' });

    await findReferenceLink('REF-0001');

    await user.type(screen.getByLabelText('Search by reference or subject'), 'fraud');
    await user.click(screen.getByRole('button', { name: 'Apply' }));

    await waitFor(() => expect(listUrl(requestedUrls)).toContain('search=fraud'));
  });

  it('returns to page 1 when a filter changes', async () => {
    // Staying on page 7 after narrowing to three results shows an empty table
    // and reads as a broken filter.
    const user = userEvent.setup();
    const { requestedUrls } = stubApi(60, 3);
    renderWithProviders(<ReferralListPage />, { route: '/referrals?page=3' });

    await findReferenceLink('REF-0001');

    await user.selectOptions(await screen.findByLabelText('Status'), 'Closed');

    await waitFor(() => expect(listUrl(requestedUrls)).toContain('status=Closed'));
    expect(listUrl(requestedUrls)).toContain('page=1');
  });

  it('toggles sort direction when the same column header is activated twice', async () => {
    const user = userEvent.setup();
    const { requestedUrls } = stubApi();
    renderWithProviders(<ReferralListPage />, { route: '/referrals' });

    await findReferenceLink('REF-0001');

    await user.click(screen.getByRole('button', { name: 'Subject' }));
    await waitFor(() => expect(listUrl(requestedUrls)).toContain('sortDirection=Ascending'));

    await user.click(screen.getByRole('button', { name: 'Subject' }));
    await waitFor(() => expect(listUrl(requestedUrls)).toContain('sortDirection=Descending'));
  });

  it('marks the sorted column with aria-sort for screen readers', async () => {
    stubApi();
    renderWithProviders(<ReferralListPage />, { route: '/referrals?sortBy=Subject&sortDirection=Ascending' });

    await findReferenceLink('REF-0001');

    const header = screen.getByRole('columnheader', { name: /Subject/ });
    expect(header).toHaveAttribute('aria-sort', 'ascending');
  });

  it('offers a change and a delete link for each referral, distinguishable by screen readers', async () => {
    stubApi();
    renderWithProviders(<ReferralListPage />, { route: '/referrals' });

    await findReferenceLink('REF-0001');

    // The visually hidden reference is what stops a screen reader announcing
    // five identical "Delete" links.
    expect(screen.getByRole('link', { name: 'Delete REF-0001' })).toHaveAttribute(
      'href',
      '/referrals/0195f0a0-0000-7000-8000-000000000001/delete'
    );
    expect(screen.getByRole('link', { name: 'Change REF-0002' })).toBeInTheDocument();
  });

  it('shows an error summary when the list cannot be loaded', async () => {
    vi.stubGlobal('fetch', vi.fn(() => Promise.resolve(jsonResponse({ status: 500, title: 'Boom' }, 500))));

    renderWithProviders(<ReferralListPage />, { route: '/referrals' });

    const alert = await screen.findByRole('alert');
    expect(within(alert).getByText('There is a problem')).toBeInTheDocument();
  });
});
