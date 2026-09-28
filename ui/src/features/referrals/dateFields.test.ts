import { describe, expect, it } from 'vitest';
import { formatDate, toDateParts, toIsoDate } from './dateFields';

describe('toIsoDate', () => {
  it('combines valid parts into a UTC instant at midnight', () => {
    expect(toIsoDate({ day: '27', month: '3', year: '2026' })).toBe('2026-03-27T00:00:00.000Z');
  });

  it('accepts zero-padded parts', () => {
    expect(toIsoDate({ day: '07', month: '03', year: '2026' })).toBe('2026-03-07T00:00:00.000Z');
  });

  it.each([
    ['a missing day', { day: '', month: '3', year: '2026' }],
    ['a missing month', { day: '27', month: '', year: '2026' }],
    ['a missing year', { day: '27', month: '3', year: '' }],
    ['a non-numeric part', { day: 'x', month: '3', year: '2026' }],
    ['a month above 12', { day: '27', month: '13', year: '2026' }],
    ['a month below 1', { day: '27', month: '0', year: '2026' }],
    ['an implausible year', { day: '27', month: '3', year: '20' }]
  ])('returns null for %s', (_label, parts) => {
    expect(toIsoDate(parts)).toBeNull();
  });

  it('rejects a day that does not exist in the month rather than rolling it over', () => {
    // Date.UTC(2026, 1, 31) silently becomes 3 March. Accepting it would
    // store a date the user never typed.
    expect(toIsoDate({ day: '31', month: '2', year: '2026' })).toBeNull();
  });

  it('accepts 29 February in a leap year', () => {
    expect(toIsoDate({ day: '29', month: '2', year: '2028' })).toBe('2028-02-29T00:00:00.000Z');
  });

  it('rejects 29 February in a non-leap year', () => {
    expect(toIsoDate({ day: '29', month: '2', year: '2026' })).toBeNull();
  });
});

describe('toDateParts', () => {
  it('splits an ISO instant into day, month and year', () => {
    expect(toDateParts('2026-03-27T00:00:00.000Z')).toEqual({ day: '27', month: '3', year: '2026' });
  });

  it('reads the date in UTC, not the local timezone', () => {
    // 23:30 UTC is already the next day in some zones. Using the local getters
    // would show 28 March to a user in Europe/Paris.
    expect(toDateParts('2026-03-27T23:30:00.000Z')).toEqual({ day: '27', month: '3', year: '2026' });
  });

  it.each([undefined, '', 'not-a-date'])('returns empty parts for %s', (value) => {
    expect(toDateParts(value)).toEqual({ day: '', month: '', year: '' });
  });
});

describe('formatDate', () => {
  it('formats an ISO instant as a GOV.UK style date', () => {
    expect(formatDate('2026-03-27T00:00:00.000Z')).toBe('27 March 2026');
  });

  it('returns an empty string for an unparseable value', () => {
    expect(formatDate('not-a-date')).toBe('');
  });
});
