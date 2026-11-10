// Conversions for the API's DateOnly ("yyyy-MM-dd") and TimeOnly ("HH:mm:ss") values.
// These are plain calendar dates and clock times with no zone, so they're built from the
// local date parts directly - never through toISOString(), which would shift them to UTC.

export function toDateOnlyString(date: Date): string {
  const month = (date.getMonth() + 1).toString().padStart(2, '0');
  const day = date.getDate().toString().padStart(2, '0');
  return `${date.getFullYear()}-${month}-${day}`;
}

export function toTimeOnlyString(time: Date): string {
  const hours = time.getHours().toString().padStart(2, '0');
  const minutes = time.getMinutes().toString().padStart(2, '0');
  return `${hours}:${minutes}:00`;
}

// "2026-11-10" -> local Date at midnight
export function parseDateOnly(value: string): Date {
  const [year, month, day] = value.split('-').map(part => Number(part));
  return new Date(year, month - 1, day);
}

// "09:30:00" -> local Date today at that clock time (for time pickers)
export function parseTimeOnly(value: string): Date {
  const [hours, minutes] = value.split(':').map(part => Number(part));
  const time = new Date();
  time.setHours(hours, minutes, 0, 0);
  return time;
}
