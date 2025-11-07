export interface Notification {
  id : number,
  trainerId? : number,
  clientId? : number,
  message: string,
  reminderType: string,
  sentThrough: string,
  sentAt: string,
}