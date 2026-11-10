// Body for creating or updating a one-off session slot
export interface BookedSessionSlotDto {
  clientId: number;
  title: string;
  description?: string;
  startDateTime: string; // UTC ISO string ending in "Z"
  endDateTime: string; // UTC ISO string ending in "Z", after the start
}
