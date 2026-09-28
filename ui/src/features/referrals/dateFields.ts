export type DateParts = {
  day: string;
  month: string;
  year: string;
};

export const emptyDateParts: DateParts = { day: '', month: '', year: '' };

/** Splits an ISO instant into the three GOV.UK date fields, in UTC. */
export function toDateParts(isoDate: string | undefined): DateParts {
  if (!isoDate) {
    return emptyDateParts;
  }

  const parsed = new Date(isoDate);

  if (Number.isNaN(parsed.getTime())) {
    return emptyDateParts;
  }

  // getUTC*, not the local getters: the server stores instants in UTC, and
  // reading them locally shifts the displayed date across midnight for anyone
  // west of Greenwich or during BST.
  return {
    day: String(parsed.getUTCDate()),
    month: String(parsed.getUTCMonth() + 1),
    year: String(parsed.getUTCFullYear())
  };
}

/**
 * Combines the three fields into an ISO instant at midnight UTC, or null if
 * they do not describe a real date.
 *
 * Rejects rolled-over dates: JavaScript's Date happily turns 31 February into
 * 3 March, which would silently store a date the user never entered.
 */
export function toIsoDate({ day, month, year }: DateParts): string | null {
  const dayNumber = Number(day);
  const monthNumber = Number(month);
  const yearNumber = Number(year);

  const allPresent = day.trim() !== '' && month.trim() !== '' && year.trim() !== '';
  const allIntegers = [dayNumber, monthNumber, yearNumber].every(Number.isInteger);

  if (!allPresent || !allIntegers) {
    return null;
  }

  if (yearNumber < 1900 || yearNumber > 2200 || monthNumber < 1 || monthNumber > 12 || dayNumber < 1) {
    return null;
  }

  const candidate = new Date(Date.UTC(yearNumber, monthNumber - 1, dayNumber));

  const isRealDate =
    candidate.getUTCFullYear() === yearNumber &&
    candidate.getUTCMonth() === monthNumber - 1 &&
    candidate.getUTCDate() === dayNumber;

  return isRealDate ? candidate.toISOString() : null;
}

/** Formats an ISO instant for display as a GOV.UK-style date, in UTC. */
export function formatDate(isoDate: string): string {
  const parsed = new Date(isoDate);

  if (Number.isNaN(parsed.getTime())) {
    return '';
  }

  return new Intl.DateTimeFormat('en-GB', {
    day: 'numeric',
    month: 'long',
    year: 'numeric',
    timeZone: 'UTC'
  }).format(parsed);
}
