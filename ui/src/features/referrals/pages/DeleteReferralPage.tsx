import { useNavigate, useParams } from 'react-router-dom';
import { BackLink, Button } from '@/components/hods';
import { formatDate } from '../dateFields';
import { useDeleteReferral, useReferral } from '../hooks/useReferrals';

/**
 * Deletion is confirmed on its own page, not in a modal.
 *
 * That is the GOV.UK pattern for a destructive, irreversible action: it has a
 * URL, it works without JavaScript, the browser back button behaves sensibly,
 * and there is no focus-trap accessibility burden to get wrong.
 */
export function DeleteReferralPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();

  const { data: referral, isPending, isError } = useReferral(id);
  const { mutate, isPending: isDeleting, error } = useDeleteReferral();

  if (isPending) {
    return <p className="govuk-body">Loading referral…</p>;
  }

  if (isError || !referral) {
    return (
      <>
        <BackLink to="/referrals" />
        <h1 className="govuk-heading-l">Referral not found</h1>
        <p className="govuk-body">The referral you are looking for does not exist or has already been deleted.</p>
      </>
    );
  }

  return (
    <>
      <BackLink to="/referrals" />

      <h1 className="govuk-heading-l">Are you sure you want to delete this referral?</h1>

      {error ? (
        <div className="govuk-error-summary" role="alert" tabIndex={-1}>
          <h2 className="govuk-error-summary__title">There is a problem</h2>
          <div className="govuk-error-summary__body">
            <p className="govuk-body">{error instanceof Error ? error.message : 'Could not delete the referral.'}</p>
          </div>
        </div>
      ) : null}

      <dl className="govuk-summary-list">
        <div className="govuk-summary-list__row">
          <dt className="govuk-summary-list__key">Reference</dt>
          <dd className="govuk-summary-list__value">{referral.referralReference}</dd>
        </div>
        <div className="govuk-summary-list__row">
          <dt className="govuk-summary-list__key">Subject</dt>
          <dd className="govuk-summary-list__value">{referral.subject}</dd>
        </div>
        <div className="govuk-summary-list__row">
          <dt className="govuk-summary-list__key">Status</dt>
          <dd className="govuk-summary-list__value">{referral.status}</dd>
        </div>
        <div className="govuk-summary-list__row">
          <dt className="govuk-summary-list__key">Date received</dt>
          <dd className="govuk-summary-list__value">{formatDate(referral.receivedDate)}</dd>
        </div>
      </dl>

      <p className="govuk-body">This cannot be undone.</p>

      <div className="govuk-button-group">
        <Button
          variant="warning"
          disabled={isDeleting}
          onClick={() => mutate(referral.id, { onSuccess: () => navigate('/referrals') })}
        >
          {isDeleting ? 'Deleting…' : 'Delete referral'}
        </Button>
        <Button variant="secondary" onClick={() => navigate('/referrals')}>
          Cancel
        </Button>
      </div>
    </>
  );
}
