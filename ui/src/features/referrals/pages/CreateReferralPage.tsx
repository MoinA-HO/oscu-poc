import { useNavigate } from 'react-router-dom';
import { BackLink } from '@/components/hods';
import { ReferralForm } from '../components/ReferralForm';
import type { ReferralFormValues } from '../components/ReferralForm';
import { toIsoDate } from '../dateFields';
import { useCreateReferral, useReferralStatuses } from '../hooks/useReferrals';

export function CreateReferralPage() {
  const navigate = useNavigate();
  const { data: statuses = [] } = useReferralStatuses();
  const { mutate, isPending, error } = useCreateReferral();

  function handleSubmit(values: ReferralFormValues) {
    const receivedDate = toIsoDate({
      day: values.receivedDay,
      month: values.receivedMonth,
      year: values.receivedYear
    });

    // The form's validate rule has already rejected an unreal date, so this
    // is a type narrowing rather than a second line of defence.
    if (receivedDate === null) {
      return;
    }

    mutate(
      {
        referralReference: values.referralReference,
        subject: values.subject,
        description: values.description.trim() === '' ? null : values.description,
        status: values.status,
        receivedDate
      },
      { onSuccess: () => navigate('/referrals') }
    );
  }

  return (
    <>
      <BackLink to="/referrals" />
      <h1 className="govuk-heading-l">Create referral</h1>

      <ReferralForm
        mode="create"
        statuses={statuses}
        submitError={error}
        isSubmitting={isPending}
        onSubmit={handleSubmit}
        onCancel={() => navigate('/referrals')}
      />
    </>
  );
}
