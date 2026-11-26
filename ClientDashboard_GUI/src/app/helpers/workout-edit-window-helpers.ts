// Mirrors the API's WorkoutEditWindowHelper: workouts before the current month are locked, but the
// previous month stays open for the first GRACE_DAYS of a new month. The API enforces it either way.
const GRACE_DAYS = 7;

export function getEarliestEditableDate(today: Date): Date {
  if (today.getDate() <= GRACE_DAYS) {
    return new Date(today.getFullYear(), today.getMonth() - 1, 1);
  }
  return new Date(today.getFullYear(), today.getMonth(), 1);
}

export function isWorkoutDateLocked(sessionDate: Date, today: Date): boolean {
  return sessionDate < getEarliestEditableDate(today);
}
