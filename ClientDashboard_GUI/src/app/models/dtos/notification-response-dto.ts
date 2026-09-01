import { CommunicationType } from "../../enums/communication-type";
import { NotificationType } from "../../enums/notification-type";
import { NotificationAudience } from "../../enums/notification-audience";

export interface NotificationResponseDto {
  id: number,
  trainerId?: number,
  clientId?: number,
  message: string,
  reminderType: NotificationType,
  sentThrough: CommunicationType,
  audience: NotificationAudience,
  sentAt: string,
  isRead: boolean,
}
