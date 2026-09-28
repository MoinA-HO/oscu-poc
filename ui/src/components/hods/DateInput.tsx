import type { InputHTMLAttributes, Ref } from 'react';

/**
 * Props for one of the three fields. `ref` is included because react-hook-form's
 * `register()` returns one, and the whole return value is spread onto the input.
 */
export type DatePartProps = Omit<InputHTMLAttributes<HTMLInputElement>, 'id' | 'type'> & {
  ref?: Ref<HTMLInputElement>;
};

type DateInputProps = {
  id: string;
  legend: string;
  hint?: string;
  error?: string;
  day: DatePartProps;
  month: DatePartProps;
  year: DatePartProps;
};

const partWidths = {
  day: 'govuk-input--width-2',
  month: 'govuk-input--width-2',
  year: 'govuk-input--width-4'
} as const;

/**
 * The GOV.UK three-field date input.
 *
 * Not `<input type="date">`. The native picker is inconsistent across
 * browsers, awkward for a date someone already knows and is transcribing, and
 * offers no control over the announced format. Three labelled numeric fields
 * is the researched pattern.
 */
export function DateInput({ id, legend, hint, error, day, month, year }: DateInputProps) {
  const hintId = hint ? `${id}-hint` : undefined;
  const errorId = error ? `${id}-error` : undefined;
  const describedBy = [hintId, errorId].filter(Boolean).join(' ') || undefined;

  const renderPart = (part: 'day' | 'month' | 'year', props: DatePartProps) => (
    <div className="govuk-date-input__item">
      <div className="govuk-form-group">
        <label className="govuk-label govuk-date-input__label" htmlFor={`${id}-${part}`}>
          {part.charAt(0).toUpperCase() + part.slice(1)}
        </label>
        <input
          {...props}
          id={`${id}-${part}`}
          // inputMode="numeric" rather than type="number": it raises the
          // numeric keypad on mobile without the spinner arrows or the
          // scroll-wheel-silently-changes-the-value trap of a number input.
          type="text"
          inputMode="numeric"
          className={`govuk-input govuk-date-input__input ${partWidths[part]}${error ? ' govuk-input--error' : ''}`}
          aria-invalid={error ? true : undefined}
        />
      </div>
    </div>
  );

  return (
    <div className={`govuk-form-group${error ? ' govuk-form-group--error' : ''}`}>
      {/* role="group" ties the legend to all three inputs, so a screen reader
          announces "Date received, Day" rather than a bare "Day". */}
      <fieldset className="govuk-fieldset" role="group" aria-describedby={describedBy}>
        <legend className="govuk-fieldset__legend">{legend}</legend>

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

        <div className="govuk-date-input" id={id}>
          {renderPart('day', day)}
          {renderPart('month', month)}
          {renderPart('year', year)}
        </div>
      </fieldset>
    </div>
  );
}
