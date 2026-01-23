import { CommunicationType } from "../enums/communication-type";
import { NotificationType } from "../enums/notification-type";

export interface Notification {
  id : number,
  trainerId? : number,
  clientId? : number,
  message: string,
  reminderType: NotificationType,
  sentThrough: CommunicationType,
  sentAt: string,
}