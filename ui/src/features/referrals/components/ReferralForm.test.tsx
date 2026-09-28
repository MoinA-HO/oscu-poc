import { describe, expect, it, vi } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { ApiError } from '@/lib/apiError';
import { renderWithProviders } from '@/test/renderWithProviders';
import { ReferralForm } from './ReferralForm';
import type { Referral } from '../types';

const statuses = ['New', 'In Progress', 'On Hold', 'Closed', 'Rejected'];

const aReferral: Referral = {
  id: '0195f0a0-0000-7000-8000-000000000001',
  referralReference: 'REF-0001',
  subject: 'Safeguarding concern',
  description: 'Details from the referring officer.',
  status: 'In Progress',
  receivedDate: '2026-03-27T00:00:00.000Z',
  createdDate: '2026-08-11T12:00:00.000Z'
};

function renderForm(overrides: Partial<Parameters<typeof ReferralForm>[0]> = {}) {
  const onSubmit = vi.fn();
  const onCancel = vi.fn();

  renderWithProviders(
    <ReferralForm
      mode="create"
      statuses={statuses}
      isSubmitting={false}
      onSubmit={onSubmit}
      onCancel={onCancel}
      {...overrides}
    />
  );

  return { onSubmit, onCancel };
}

async function fillValidForm(user: ReturnType<typeof userEvent.setup>) {
  await user.type(screen.getByLabelText('Referral reference'), 'REF-0001');
  await user.type(screen.getByLabelText('Subject'), 'Safeguarding concern');
  await user.selectOptions(screen.getByLabelText('Status'), 'New');
  await user.type(screen.getByLabelText('Day'), '27');
  await user.type(screen.getByLabelText('Month'), '3');
  await user.type(screen.getByLabelText('Year'), '2026');
}

/**
 * The GOV.UK error pattern requires each message in two places: the error
 * summary at the top of the page, and inline beside the field. Asserting on
 * exactly two occurrences pins both down — a single match would mean one of
 * them went missing.
 */
async function expectErrorShownTwice(message: string) {
  expect(await screen.findAllByText(message)).toHaveLength(2);
}

describe('ReferralForm', () => {
  it('submits the entered values when everything is valid', async () => {
    const user = userEvent.setup();
    const { onSubmit } = renderForm();

    await fillValidForm(user);
    await user.click(screen.getByRole('button', { name: 'Create referral' }));

    await waitFor(() => expect(onSubmit).toHaveBeenCalledTimes(1));

    expect(onSubmit.mock.calls[0]?.[0]).toMatchObject({
      referralReference: 'REF-0001',
      subject: 'Safeguarding concern',
      status: 'New',
      receivedDay: '27',
      receivedMonth: '3',
      receivedYear: '2026'
    });
  });

  it('blocks submission and lists every failure in the error summary', async () => {
    const user = userEvent.setup();
    const { onSubmit } = renderForm();

    await user.click(screen.getByRole('button', { name: 'Create referral' }));

    const summary = await screen.findByRole('alert');

    expect(onSubmit).not.toHaveBeenCalled();
    expect(summary).toHaveTextContent('Enter a referral reference');
    expect(summary).toHaveTextContent('Enter a subject');
    expect(summary).toHaveTextContent('Select a status');
  });

  it('rejects a reference that does not match the expected format', async () => {
    const user = userEvent.setup();
    const { onSubmit } = renderForm();

    await user.type(screen.getByLabelText('Referral reference'), 'nonsense');
    await user.type(screen.getByLabelText('Subject'), 'Safeguarding concern');
    await user.selectOptions(screen.getByLabelText('Status'), 'New');
    await user.type(screen.getByLabelText('Day'), '27');
    await user.type(screen.getByLabelText('Month'), '3');
    await user.type(screen.getByLabelText('Year'), '2026');
    await user.click(screen.getByRole('button', { name: 'Create referral' }));

    await expectErrorShownTwice('Enter a referral reference in the format REF-0001');
    expect(onSubmit).not.toHaveBeenCalled();
  });

  it('rejects a date that does not exist', async () => {
    const user = userEvent.setup();
    const { onSubmit } = renderForm();

    await user.type(screen.getByLabelText('Referral reference'), 'REF-0001');
    await user.type(screen.getByLabelText('Subject'), 'Safeguarding concern');
    await user.selectOptions(screen.getByLabelText('Status'), 'New');
    await user.type(screen.getByLabelText('Day'), '31');
    await user.type(screen.getByLabelText('Month'), '2');
    await user.type(screen.getByLabelText('Year'), '2026');
    await user.click(screen.getByRole('button', { name: 'Create referral' }));

    await expectErrorShownTwice('Enter a real date');
    expect(onSubmit).not.toHaveBeenCalled();
  });

  it('rejects a date received in the future', async () => {
    const user = userEvent.setup();
    const { onSubmit } = renderForm();
    const nextYear = new Date().getUTCFullYear() + 1;

    await user.type(screen.getByLabelText('Referral reference'), 'REF-0001');
    await user.type(screen.getByLabelText('Subject'), 'Safeguarding concern');
    await user.selectOptions(screen.getByLabelText('Status'), 'New');
    await user.type(screen.getByLabelText('Day'), '1');
    await user.type(screen.getByLabelText('Month'), '6');
    await user.type(screen.getByLabelText('Year'), String(nextYear));
    await user.click(screen.getByRole('button', { name: 'Create referral' }));

    await expectErrorShownTwice('The date received cannot be in the future');
    expect(onSubmit).not.toHaveBeenCalled();
  });

  it('pre-populates every field when editing', () => {
    renderForm({ mode: 'edit', referral: aReferral });

    expect(screen.getByLabelText('Subject')).toHaveValue('Safeguarding concern');
    expect(screen.getByLabelText('Description')).toHaveValue('Details from the referring officer.');
    expect(screen.getByLabelText('Status')).toHaveValue('In Progress');
    expect(screen.getByLabelText('Day')).toHaveValue('27');
    expect(screen.getByLabelText('Month')).toHaveValue('3');
    expect(screen.getByLabelText('Year')).toHaveValue('2026');
  });

  it('shows the reference as read-only text when editing', () => {
    // The business identifier is fixed at creation, so there must be no input
    // inviting someone to change it.
    renderForm({ mode: 'edit', referral: aReferral });

    expect(screen.queryByLabelText('Referral reference')).not.toBeInTheDocument();
    expect(screen.getByText('REF-0001')).toBeInTheDocument();
  });

  it('maps a server validation error onto the field it belongs to', async () => {
    const submitError = new ApiError(400, {
      status: 400,
      title: 'Bad Request',
      errors: { Subject: ['Subject is already used by an open case.'] }
    });

    renderForm({ submitError });

    await expectErrorShownTwice('Subject is already used by an open case.');
  });

  it('maps a 409 conflict onto the reference field', async () => {
    const submitError = new ApiError(409, {
      status: 409,
      title: 'Conflict',
      detail: "A referral with reference 'REF-0001' already exists.",
      code: 'Referral.DuplicateReference'
    });

    renderForm({ submitError });

    await expectErrorShownTwice("A referral with reference 'REF-0001' already exists.");
  });

  it('disables the submit button while saving', () => {
    renderForm({ isSubmitting: true });

    expect(screen.getByRole('button', { name: 'Saving…' })).toBeDisabled();
  });

  it('calls onCancel without submitting', async () => {
    const user = userEvent.setup();
    const { onCancel, onSubmit } = renderForm();

    await user.click(screen.getByRole('button', { name: 'Cancel' }));

    expect(onCancel).toHaveBeenCalledTimes(1);
    expect(onSubmit).not.toHaveBeenCalled();
  });
});
