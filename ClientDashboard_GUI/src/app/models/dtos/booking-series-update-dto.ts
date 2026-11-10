// Client and recurrence can't be changed on an existing series
export interface BookingSeriesUpdateDto {
  title: string;
  description?: string;
  duration: number; // minutes
  startDate: string; // trainer's local date, "yyyy-MM-dd"
  startTime: string; // trainer's local clock time, "HH:mm:ss"
}
