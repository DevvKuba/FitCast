// Session start/end handling for time pickers, which give a Date whose day part doesn't matter

// The date picker gives the day and the time picker gives the clock time - this joins them in the browser's zone
export function combineDateAndTime(date: Date, time: Date): Date {
  return new Date(date.getFullYear(), date.getMonth(), date.getDate(), time.getHours(), time.getMinutes());
}

export function addMinutes(time: Date, minutes: number): Date {
  return new Date(time.getTime() + minutes * 60000);
}

export function minutesBetweenClockTimes(start: Date, end: Date): number {
  return (end.getHours() * 60 + end.getMinutes()) - (start.getHours() * 60 + start.getMinutes());
}

export function atClockTime(date: Date, hours: number, minutes: number): Date {
  return new Date(date.getFullYear(), date.getMonth(), date.getDate(), hours, minutes);
}
