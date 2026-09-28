import { useMemo } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { Pagination, Table, Tag } from '@/components/hods';
import type { Column } from '@/components/hods';
import { formatDate } from '../dateFields';
import { useReferralList, useReferralStatuses } from '../hooks/useReferrals';
import { defaultReferralListQuery } from '../types';
import type { Referral, ReferralListQuery, ReferralSortField } from '../types';

const statusColours: Record<string, string> = {
  New: 'blue',
  'In Progress': 'yellow',
  'On Hold': 'grey',
  Closed: 'green',
  Rejected: 'red'
};

const sortFields = new Set<ReferralSortField>([
  'ReceivedDate',
  'CreatedDate',
  'ReferralReference',
  'Subject',
  'Status'
]);

function parseSortField(value: string | null): ReferralSortField {
  return value && sortFields.has(value as ReferralSortField)
    ? (value as ReferralSortField)
    : defaultReferralListQuery.sortBy;
}

/**
 * The list state — page, search, status filter, sort — lives in the URL rather
 * than in component state.
 *
 * A caseworker can then bookmark a filtered view, share it with a colleague,
 * and use the browser back button to step through their own filtering, all of
 * which component state would throw away. It also means the query object
 * driving TanStack Query is derived, not duplicated, so the two can never
 * disagree.
 */
export function ReferralListPage() {
  const [searchParams, setSearchParams] = useSearchParams();

  const query: ReferralListQuery = useMemo(
    () => ({
      page: Number(searchParams.get('page') ?? defaultReferralListQuery.page) || defaultReferralListQuery.page,
      pageSize: defaultReferralListQuery.pageSize,
      search: searchParams.get('search') ?? undefined,
      status: searchParams.get('status') ?? undefined,
      sortBy: parseSortField(searchParams.get('sortBy')),
      sortDirection: searchParams.get('sortDirection') === 'Ascending' ? 'Ascending' : 'Descending'
    }),
    [searchParams]
  );

  const { data, isPending, isError, error } = useReferralList(query);
  const { data: statuses = [] } = useReferralStatuses();

  function updateParams(changes: Record<string, string | undefined>, resetPage = true) {
    const next = new URLSearchParams(searchParams);

    for (const [key, value] of Object.entries(changes)) {
      if (value === undefined || value === '') {
        next.delete(key);
      } else {
        next.set(key, value);
      }
    }

    if (resetPage) {
      // Staying on page 7 after narrowing to three results shows an empty
      // table and looks like the filter is broken.
      next.delete('page');
    }

    setSearchParams(next);
  }

  function handleSortChange(key: string) {
    const isSameField = key === query.sortBy;
    const nextDirection = isSameField && query.sortDirection === 'Ascending' ? 'Descending' : 'Ascending';

    updateParams({ sortBy: key, sortDirection: nextDirection });
  }

  const columns: ReadonlyArray<Column<Referral>> = [
    {
      key: 'ReferralReference',
      header: 'Reference',
      sortable: true,
      render: (referral) => <Link to={`/referrals/${referral.id}/edit`}>{referral.referralReference}</Link>
    },
    { key: 'Subject', header: 'Subject', sortable: true, render: (referral) => referral.subject },
    {
      key: 'Status',
      header: 'Status',
      sortable: true,
      render: (referral) => <Tag colour={statusColours[referral.status]}>{referral.status}</Tag>
    },
    {
      key: 'ReceivedDate',
      header: 'Date received',
      sortable: true,
      render: (referral) => formatDate(referral.receivedDate)
    },
    {
      key: 'actions',
      header: 'Actions',
      render: (referral) => (
        <>
          <Link to={`/referrals/${referral.id}/edit`} className="govuk-link govuk-!-margin-right-3">
            Change<span className="govuk-visually-hidden"> {referral.referralReference}</span>
          </Link>
          <Link to={`/referrals/${referral.id}/delete`} className="govuk-link">
            Delete<span className="govuk-visually-hidden"> {referral.referralReference}</span>
          </Link>
        </>
      )
    }
  ];

  return (
    <>
      <h1 className="govuk-heading-l">Referrals</h1>

      <div className="govuk-button-group">
        <Link to="/referrals/new" className="govuk-button" data-module="govuk-button">
          Create referral
        </Link>
      </div>

      <form
        className="govuk-grid-row"
        onSubmit={(event) => {
          event.preventDefault();
          const formData = new FormData(event.currentTarget);
          updateParams({ search: String(formData.get('search') ?? '') });
        }}
      >
        <div className="govuk-grid-column-one-half">
          <div className="govuk-form-group">
            <label className="govuk-label" htmlFor="search">
              Search by reference or subject
            </label>
            <input
              className="govuk-input"
              id="search"
              name="search"
              type="search"
              defaultValue={query.search ?? ''}
              key={query.search ?? ''}
            />
          </div>
        </div>

        <div className="govuk-grid-column-one-quarter">
          <div className="govuk-form-group">
            <label className="govuk-label" htmlFor="status-filter">
              Status
            </label>
            <select
              className="govuk-select"
              id="status-filter"
              value={query.status ?? ''}
              onChange={(event) => updateParams({ status: event.target.value })}
            >
              <option value="">All statuses</option>
              {statuses.map((status) => (
                <option key={status} value={status}>
                  {status}
                </option>
              ))}
            </select>
          </div>
        </div>

        <div className="govuk-grid-column-one-quarter">
          <button type="submit" className="govuk-button govuk-!-margin-top-6" data-module="govuk-button">
            Apply
          </button>
        </div>
      </form>

      {isPending ? <p className="govuk-body">Loading referrals…</p> : null}

      {isError ? (
        <div className="govuk-error-summary" role="alert" tabIndex={-1}>
          <h2 className="govuk-error-summary__title">There is a problem</h2>
          <div className="govuk-error-summary__body">
            <p className="govuk-body">{error instanceof Error ? error.message : 'Could not load referrals.'}</p>
          </div>
        </div>
      ) : null}

      {data ? (
        <>
          <Table
            caption={`${data.totalCount} referral${data.totalCount === 1 ? '' : 's'}`}
            columns={columns}
            rows={data.items}
            rowKey={(referral) => referral.id}
            sortKey={query.sortBy}
            sortDirection={query.sortDirection === 'Ascending' ? 'ascending' : 'descending'}
            onSortChange={handleSortChange}
            emptyMessage="No referrals match your search."
          />

          <Pagination
            page={data.page}
            totalPages={data.totalPages}
            onPageChange={(page) => updateParams({ page: String(page) }, false)}
          />
        </>
      ) : null}
    </>
  );
}
