import { getEarliestEditableDate, isWorkoutDateLocked } from './workout-edit-window-helpers';

describe('workout edit window helpers', () => {
  it('keeps the previous month open for the first 7 days of a new month', () => {
    expect(getEarliestEditableDate(new Date(2026, 10, 7))).toEqual(new Date(2026, 9, 1));
  });

  it('starts at the current month from the 8th', () => {
    expect(getEarliestEditableDate(new Date(2026, 10, 8))).toEqual(new Date(2026, 10, 1));
  });

  it('opens the previous December in early January', () => {
    expect(getEarliestEditableDate(new Date(2027, 0, 3))).toEqual(new Date(2026, 11, 1));
  });

  it('locks the previous month once the grace days pass', () => {
    const lastDayOfOctober = new Date(2026, 9, 31);

    expect(isWorkoutDateLocked(lastDayOfOctober, new Date(2026, 10, 7))).toBeFalse();
    expect(isWorkoutDateLocked(lastDayOfOctober, new Date(2026, 10, 8))).toBeTrue();
  });
});
