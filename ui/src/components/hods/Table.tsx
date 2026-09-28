import type { ReactNode } from 'react';

export type SortDirection = 'ascending' | 'descending';

export type Column<TRow> = {
  /** Stable key, also used as the sort field sent to the API. */
  key: string;
  header: string;
  sortable?: boolean;
  numeric?: boolean;
  render: (row: TRow) => ReactNode;
};

type TableProps<TRow> = {
  caption: string;
  columns: ReadonlyArray<Column<TRow>>;
  rows: readonly TRow[];
  rowKey: (row: TRow) => string;
  sortKey?: string;
  sortDirection?: SortDirection;
  onSortChange?: (key: string) => void;
  emptyMessage?: string;
};

/**
 * A GOV.UK table with optional server-driven sorting.
 *
 * Sorting is expressed through `aria-sort` on the header cell and a real
 * button inside it, so the current order is announced and the control is
 * reachable by keyboard. A `<div onClick>` header would be neither.
 */
export function Table<TRow>({
  caption,
  columns,
  rows,
  rowKey,
  sortKey,
  sortDirection,
  onSortChange,
  emptyMessage = 'No records found.'
}: TableProps<TRow>) {
  if (rows.length === 0) {
    return (
      <>
        <h2 className="govuk-heading-m">{caption}</h2>
        <p className="govuk-body">{emptyMessage}</p>
      </>
    );
  }

  return (
    <table className="govuk-table">
      <caption className="govuk-table__caption govuk-table__caption--m">{caption}</caption>
      <thead className="govuk-table__head">
        <tr className="govuk-table__row">
          {columns.map((column) => {
            const isSorted = sortKey === column.key;

            return (
              <th
                key={column.key}
                scope="col"
                className={`govuk-table__header${column.numeric ? ' govuk-table__header--numeric' : ''}`}
                aria-sort={column.sortable ? (isSorted ? sortDirection : 'none') : undefined}
              >
                {column.sortable && onSortChange ? (
                  <button type="button" className="govuk-link govuk-body-s" onClick={() => onSortChange(column.key)}>
                    {column.header}
                  </button>
                ) : (
                  column.header
                )}
              </th>
            );
          })}
        </tr>
      </thead>
      <tbody className="govuk-table__body">
        {rows.map((row) => (
          <tr key={rowKey(row)} className="govuk-table__row">
            {columns.map((column) => (
              <td
                key={column.key}
                className={`govuk-table__cell${column.numeric ? ' govuk-table__cell--numeric' : ''}`}
              >
                {column.render(row)}
              </td>
            ))}
          </tr>
        ))}
      </tbody>
    </table>
  );
}

export function Tag({ children, colour }: { children: ReactNode; colour?: string }) {
  return <strong className={`govuk-tag${colour ? ` govuk-tag--${colour}` : ''}`}>{children}</strong>;
}

type PaginationProps = {
  page: number;
  totalPages: number;
  onPageChange: (page: number) => void;
};

export function Pagination({ page, totalPages, onPageChange }: PaginationProps) {
  if (totalPages <= 1) {
    return null;
  }

  return (
    <nav className="govuk-pagination" role="navigation" aria-label="Pagination">
      <div className="govuk-pagination__prev">
        <button
          type="button"
          className="govuk-link govuk-pagination__link"
          onClick={() => onPageChange(page - 1)}
          disabled={page <= 1}
        >
          Previous<span className="govuk-visually-hidden"> page</span>
        </button>
      </div>

      <p className="govuk-pagination__results govuk-body">
        Page <b>{page}</b> of <b>{totalPages}</b>
      </p>

      <div className="govuk-pagination__next">
        <button
          type="button"
          className="govuk-link govuk-pagination__link"
          onClick={() => onPageChange(page + 1)}
          disabled={page >= totalPages}
        >
          Next<span className="govuk-visually-hidden"> page</span>
        </button>
      </div>
    </nav>
  );
}
