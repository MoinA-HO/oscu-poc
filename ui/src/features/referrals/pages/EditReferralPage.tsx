import { useNavigate, useParams } from 'react-router-dom';
import { BackLink } from '@/components/hods';
import { ReferralForm } from '../components/ReferralForm';
import type { ReferralFormValues } from '../components/ReferralForm';
import { toIsoDate } from '../dateFields';
import { useReferral, useReferralStatuses, useUpdateReferral } from '../hooks/useReferrals';

export function EditReferralPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();

  const { data: referral, isPending, isError } = useReferral(id);
  const { data: statuses = [] } = useReferralStatuses();
  const { mutate, isPending: isSaving, error } = useUpdateReferral(id ?? '');

  function handleSubmit(values: ReferralFormValues) {
    const receivedDate = toIsoDate({
      day: values.receivedDay,
      month: values.receivedMonth,
      year: values.receivedYear
    });

    if (receivedDate === null) {
      return;
    }

    mutate(
      {
        subject: values.subject,
        description: values.description.trim() === '' ? null : values.description,
        status: values.status,
        receivedDate
      },
      { onSuccess: () => navigate('/referrals') }
    );
  }

  if (isPending) {
    return <p className="govuk-body">Loading referral…</p>;
  }

  if (isError || !referral) {
    return (
      <>
        <BackLink to="/referrals" />
        <h1 className="govuk-heading-l">Referral not found</h1>
        <p className="govuk-body">The referral you are looking for does not exist or has been deleted.</p>
      </>
    );
  }

  return (
    <>
      <BackLink to="/referrals" />
      <h1 className="govuk-heading-l">Change referral</h1>

      <ReferralForm
        mode="edit"
        statuses={statuses}
        referral={referral}
        submitError={error}
        isSubmitting={isSaving}
        onSubmit={handleSubmit}
        onCancel={() => navigate('/referrals')}
      />
    </>
  );
}
