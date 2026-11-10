import { SessionStatus } from "../../enums/session-status";

export interface BookedSessionSlotViewDto {
  id: number;
  clientId: number;
  clientName: string;
  bookingSeriesId?: number;
  title: string;
  description?: string;
  startDateTime: string; // UTC ISO string, e.g. "2026-11-10T09:00:00Z"
  endDateTime: string; // UTC ISO string
  status: SessionStatus;
}
