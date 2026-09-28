export type Referral = {
  id: string;
  referralReference: string;
  subject: string;
  description: string | null;
  status: string;
  /** ISO 8601 UTC instant. */
  receivedDate: string;
  /** ISO 8601 UTC instant. */
  createdDate: string;
};

export type CreateReferralRequest = {
  referralReference: string;
  subject: string;
  description: string | null;
  status: string;
  receivedDate: string;
};

export type UpdateReferralRequest = Omit<CreateReferralRequest, 'referralReference'>;

/**
 * Must match the ReferralSortField enum on the server. Minimal API binds enums
 * by name, so these are sent verbatim in the query string.
 */
export type ReferralSortField = 'ReceivedDate' | 'CreatedDate' | 'ReferralReference' | 'Subject' | 'Status';

export type ReferralSortDirection = 'Ascending' | 'Descending';

export type ReferralListQuery = {
  page: number;
  pageSize: number;
  search?: string;
  status?: string;
  sortBy: ReferralSortField;
  sortDirection: ReferralSortDirection;
};

export const defaultReferralListQuery: ReferralListQuery = {
  page: 1,
  pageSize: 20,
  sortBy: 'ReceivedDate',
  sortDirection: 'Descending'
};
