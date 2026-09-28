import type {
  ButtonHTMLAttributes,
  InputHTMLAttributes,
  ReactNode,
  SelectHTMLAttributes,
  TextareaHTMLAttributes
} from 'react';
import { forwardRef } from 'react';

type FieldProps = {
  id: string;
  label: string;
  hint?: string;
  error?: string;
};

/**
 * Wraps a control with its label, hint and error message, and wires the
 * aria-describedby chain that screen readers rely on to announce them.
 * Doing this once here is why no form in the app has to remember it.
 */
function FormGroup({
  id,
  label,
  hint,
  error,
  children
}: FieldProps & { children: (describedBy: string | undefined) => ReactNode }) {
  const hintId = hint ? `${id}-hint` : undefined;
  const errorId = error ? `${id}-error` : undefined;
  const describedBy = [hintId, errorId].filter(Boolean).join(' ') || undefined;

  return (
    <div className={`govuk-form-group${error ? ' govuk-form-group--error' : ''}`}>
      <label className="govuk-label" htmlFor={id}>
        {label}
      </label>
      {hint ? (
        <div id={hintId} className="govuk-hint">
          {hint}
        </div>
      ) : null}
      {error ? (
        <p id={errorId} className="govuk-error-message">
          <span className="govuk-visually-hidden">Error:</span> {error}
        </p>
      ) : null}
      {children(describedBy)}
    </div>
  );
}

type TextInputProps = FieldProps & Omit<InputHTMLAttributes<HTMLInputElement>, 'id'>;

export const TextInput = forwardRef<HTMLInputElement, TextInputProps>(function TextInput(
  { id, label, hint, error, ...inputProps },
  ref
) {
  return (
    <FormGroup id={id} label={label} hint={hint} error={error}>
      {(describedBy) => (
        <input
          {...inputProps}
          ref={ref}
          id={id}
          className={`govuk-input${error ? ' govuk-input--error' : ''}`}
          aria-describedby={describedBy}
          aria-invalid={error ? true : undefined}
        />
      )}
    </FormGroup>
  );
});

type TextAreaProps = FieldProps & Omit<TextareaHTMLAttributes<HTMLTextAreaElement>, 'id'>;

export const TextArea = forwardRef<HTMLTextAreaElement, TextAreaProps>(function TextArea(
  { id, label, hint, error, rows = 5, ...textAreaProps },
  ref
) {
  return (
    <FormGroup id={id} label={label} hint={hint} error={error}>
      {(describedBy) => (
        <textarea
          {...textAreaProps}
          ref={ref}
          id={id}
          rows={rows}
          className={`govuk-textarea${error ? ' govuk-textarea--error' : ''}`}
          aria-describedby={describedBy}
          aria-invalid={error ? true : undefined}
        />
      )}
    </FormGroup>
  );
});

type SelectProps = FieldProps &
  Omit<SelectHTMLAttributes<HTMLSelectElement>, 'id'> & {
    options: readonly string[];
    placeholder?: string;
  };

export const Select = forwardRef<HTMLSelectElement, SelectProps>(function Select(
  { id, label, hint, error, options, placeholder, ...selectProps },
  ref
) {
  return (
    <FormGroup id={id} label={label} hint={hint} error={error}>
      {(describedBy) => (
        <select
          {...selectProps}
          ref={ref}
          id={id}
          className={`govuk-select${error ? ' govuk-select--error' : ''}`}
          aria-describedby={describedBy}
          aria-invalid={error ? true : undefined}
        >
          {placeholder ? <option value="">{placeholder}</option> : null}
          {options.map((option) => (
            <option key={option} value={option}>
              {option}
            </option>
          ))}
        </select>
      )}
    </FormGroup>
  );
});

export function Button({
  children,
  variant = 'primary',
  ...buttonProps
}: { children: ReactNode; variant?: 'primary' | 'secondary' | 'warning' } & ButtonHTMLAttributes<HTMLButtonElement>) {
  const variantClass =
    variant === 'secondary' ? ' govuk-button--secondary' : variant === 'warning' ? ' govuk-button--warning' : '';

  return (
    <button {...buttonProps} className={`govuk-button${variantClass}`} data-module="govuk-button">
      {children}
    </button>
  );
}

export type ErrorSummaryItem = {
  /** The id of the control to focus when the item is activated. */
  targetId: string;
  message: string;
};

/**
 * The GOV.UK error summary. Placed at the top of the page and focused on
 * submit, so a screen reader user hears every failure at once instead of
 * hunting the form for red text.
 */
export function ErrorSummary({ items, title = 'There is a problem' }: { items: ErrorSummaryItem[]; title?: string }) {
  if (items.length === 0) {
    return null;
  }

  return (
    <div
      className="govuk-error-summary"
      data-module="govuk-error-summary"
      role="alert"
      tabIndex={-1}
      aria-labelledby="error-summary-title"
    >
      <h2 className="govuk-error-summary__title" id="error-summary-title">
        {title}
      </h2>
      <div className="govuk-error-summary__body">
        <ul className="govuk-list govuk-error-summary__list">
          {items.map((item) => (
            <li key={`${item.targetId}-${item.message}`}>
              <a href={`#${item.targetId}`}>{item.message}</a>
            </li>
          ))}
        </ul>
      </div>
    </div>
  );
}

export function NotificationBanner({ children, title = 'Success' }: { children: ReactNode; title?: string }) {
  return (
    <div
      className="govuk-notification-banner govuk-notification-banner--success"
      role="alert"
      aria-labelledby="govuk-notification-banner-title"
      data-module="govuk-notification-banner"
    >
      <div className="govuk-notification-banner__header">
        <h2 className="govuk-notification-banner__title" id="govuk-notification-banner-title">
          {title}
        </h2>
      </div>
      <div className="govuk-notification-banner__content">
        <p className="govuk-notification-banner__heading">{children}</p>
      </div>
    </div>
  );
}
