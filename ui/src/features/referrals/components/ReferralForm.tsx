import { useEffect, useRef } from 'react';
import { useForm } from 'react-hook-form';
import { Button, DateInput, ErrorSummary, Select, TextArea, TextInput } from '@/components/hods';
import type { ErrorSummaryItem } from '@/components/hods';
import { ApiError } from '@/lib/apiError';
import { toDateParts, toIsoDate } from '../dateFields';
import type { Referral } from '../types';

export type ReferralFormValues = {
  referralReference: string;
  subject: string;
  description: string;
  status: string;
  receivedDay: string;
  receivedMonth: string;
  receivedYear: string;
};

type ReferralFormProps = {
  mode: 'create' | 'edit';
  statuses: string[];
  referral?: Referral;
  submitError?: unknown;
  isSubmitting: boolean;
  onSubmit: (values: ReferralFormValues) => void;
  onCancel: () => void;
};

/**
 * Maps a server property name onto the form field it came from, so a 400 from
 * the API lands on the right input instead of a generic banner.
 */
const serverFieldMap: Record<string, keyof ReferralFormValues> = {
  ReferralReference: 'referralReference',
  Subject: 'subject',
  Description: 'description',
  Status: 'status',
  ReceivedDate: 'receivedDay'
};

function defaultValues(referral: Referral | undefined): ReferralFormValues {
  const received = toDateParts(referral?.receivedDate);

  return {
    referralReference: referral?.referralReference ?? '',
    subject: referral?.subject ?? '',
    description: referral?.description ?? '',
    status: referral?.status ?? '',
    receivedDay: received.day,
    receivedMonth: received.month,
    receivedYear: received.year
  };
}

export function ReferralForm({
  mode,
  statuses,
  referral,
  submitError,
  isSubmitting,
  onSubmit,
  onCancel
}: ReferralFormProps) {
  const {
    register,
    handleSubmit,
    setError,
    formState: { errors }
  } = useForm<ReferralFormValues>({ defaultValues: defaultValues(referral) });

  const errorSummaryRef = useRef<HTMLDivElement>(null);

  // Client-side rules mirror the server's. The server remains the authority —
  // these exist so the caseworker is not made to wait for a round trip to be
  // told a field is blank.
  useEffect(() => {
    if (!(submitError instanceof ApiError)) {
      return;
    }

    if (submitError.isValidationError) {
      for (const [property, messages] of Object.entries(submitError.validationErrors)) {
        const field = serverFieldMap[property];

        if (field && messages[0]) {
          setError(field, { type: 'server', message: messages[0] });
        }
      }
    }

    if (submitError.isConflict) {
      setError('referralReference', {
        type: 'server',
        message: submitError.problem.detail ?? 'That reference is already in use.'
      });
    }
  }, [submitError, setError]);

  // Move focus to the summary so a screen reader user is told what failed,
  // rather than being left at the bottom of the form after a rejected submit.
  useEffect(() => {
    if (Object.keys(errors).length > 0) {
      errorSummaryRef.current?.focus();
    }
  }, [errors]);

  const summaryItems: ErrorSummaryItem[] = [];

  if (errors.referralReference) {
    summaryItems.push({ targetId: 'referralReference', message: errors.referralReference.message ?? 'Invalid' });
  }
  if (errors.subject) {
    summaryItems.push({ targetId: 'subject', message: errors.subject.message ?? 'Invalid' });
  }
  if (errors.description) {
    summaryItems.push({ targetId: 'description', message: errors.description.message ?? 'Invalid' });
  }
  if (errors.status) {
    summaryItems.push({ targetId: 'status', message: errors.status.message ?? 'Invalid' });
  }
  if (errors.receivedDay || errors.receivedMonth || errors.receivedYear) {
    summaryItems.push({
      targetId: 'receivedDate-day',
      message:
        errors.receivedDay?.message ??
        errors.receivedMonth?.message ??
        errors.receivedYear?.message ??
        'Enter a valid date'
    });
  }

  const dateError = errors.receivedDay?.message ?? errors.receivedMonth?.message ?? errors.receivedYear?.message;

  return (
    <form onSubmit={handleSubmit(onSubmit)} noValidate>
      <div ref={errorSummaryRef} tabIndex={-1}>
        <ErrorSummary items={summaryItems} />
      </div>

      {mode === 'create' ? (
        <TextInput
          id="referralReference"
          label="Referral reference"
          hint="For example, REF-0001"
          error={errors.referralReference?.message}
          {...register('referralReference', {
            required: 'Enter a referral reference',
            maxLength: { value: 32, message: 'Referral reference must be 32 characters or fewer' },
            pattern: { value: /^REF-\d{4,}$/, message: 'Enter a referral reference in the format REF-0001' }
          })}
        />
      ) : (
        <div className="govuk-form-group">
          <span className="govuk-label">Referral reference</span>
          {/* Fixed at creation, so it is shown rather than edited. */}
          <p className="govuk-body govuk-!-font-weight-bold">{referral?.referralReference}</p>
        </div>
      )}

      <TextInput
        id="subject"
        label="Subject"
        error={errors.subject?.message}
        {...register('subject', {
          required: 'Enter a subject',
          maxLength: { value: 200, message: 'Subject must be 200 characters or fewer' }
        })}
      />

      <TextArea
        id="description"
        label="Description"
        hint="Optional"
        error={errors.description?.message}
        {...register('description', {
          maxLength: { value: 4000, message: 'Description must be 4000 characters or fewer' }
        })}
      />

      <Select
        id="status"
        label="Status"
        placeholder="Choose a status"
        options={statuses}
        error={errors.status?.message}
        {...register('status', { required: 'Select a status' })}
      />

      <DateInput
        id="receivedDate"
        legend="Date received"
        hint="For example, 27 3 2026"
        error={dateError}
        day={register('receivedDay', {
          required: 'Enter the date the referral was received',
          validate: (_value, values) => {
            const iso = toIsoDate({
              day: values.receivedDay,
              month: values.receivedMonth,
              year: values.receivedYear
            });

            if (iso === null) {
              return 'Enter a real date';
            }

            // Mirrors the server's five-minute skew tolerance.
            return new Date(iso).getTime() <= Date.now() + 5 * 60_000
              ? true
              : 'The date received cannot be in the future';
          }
        })}
        month={register('receivedMonth', { required: 'Enter the date the referral was received' })}
        year={register('receivedYear', { required: 'Enter the date the referral was received' })}
      />

      <div className="govuk-button-group">
        <Button type="submit" disabled={isSubmitting}>
          {isSubmitting ? 'Saving…' : mode === 'create' ? 'Create referral' : 'Save changes'}
        </Button>
        <Button type="button" variant="secondary" onClick={onCancel}>
          Cancel
        </Button>
      </div>
    </form>
  );
}
