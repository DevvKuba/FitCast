import { addMinutes, atClockTime, combineDateAndTime, minutesBetweenClockTimes } from './session-time-helpers';

describe('session time helpers', () => {
  it('combineDateAndTime takes the day from the date and the clock time from the time', () => {
    const date = new Date(2026, 10, 10, 23, 59);
    const time = new Date(2020, 0, 1, 8, 15);

    expect(combineDateAndTime(date, time)).toEqual(new Date(2026, 10, 10, 8, 15));
  });

  it('addMinutes can cross into the next hour', () => {
    expect(addMinutes(new Date(2026, 10, 10, 9, 45), 30)).toEqual(new Date(2026, 10, 10, 10, 15));
  });

  it('minutesBetweenClockTimes ignores the day part of time pickers', () => {
    const start = new Date(2026, 10, 10, 9, 0);
    const end = new Date(2026, 10, 12, 10, 30);

    expect(minutesBetweenClockTimes(start, end)).toBe(90);
  });

  it('minutesBetweenClockTimes is negative when the end is before the start', () => {
    expect(minutesBetweenClockTimes(new Date(2026, 10, 10, 10, 0), new Date(2026, 10, 10, 9, 0))).toBe(-60);
  });

  it('atClockTime keeps the date and sets the clock time', () => {
    expect(atClockTime(new Date(2026, 10, 10, 17, 42), 12, 0)).toEqual(new Date(2026, 10, 10, 12, 0));
  });
});
