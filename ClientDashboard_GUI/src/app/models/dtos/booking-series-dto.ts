import { SessionRecurrence } from "../../enums/session-recurrence";

export interface BookingSeriesDto {
  id: number;
  clientId: number;
  clientName: string;
  title: string;
  description?: string;
  duration: number; // minutes
  startDate: string; // trainer's local date, "yyyy-MM-dd"
  startTime: string; // trainer's local clock time, "HH:mm:ss"
  recurrence: SessionRecurrence;
}
