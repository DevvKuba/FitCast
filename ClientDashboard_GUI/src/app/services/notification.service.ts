import { HttpClient } from '@angular/common/http';
import { inject, Injectable, signal } from '@angular/core';
import { UserDto } from '../models/dtos/user-dto';
import { environment } from '../environments/environment';
import { Observable } from 'rxjs';
import { ApiResponse } from '../models/api-response';
import { NotificationSmsStatusDto } from '../models/dtos/notification-sms-status-dto';
import { Notification } from '../models/notification';
import { NotificationReadStatusDto } from '../models/dtos/notification-read-status-dto';

@Injectable({
  providedIn: 'root'
})
export class NotificationService {
  http = inject(HttpClient);
  unreadNotificationCount = signal<number>(0);
  baseUrl = environment.apiUrl;

  toggleUserSMSNotificationStatus(statusInfo: NotificationSmsStatusDto): Observable<any>{
    return this.http.put(this.baseUrl + 'notification/changeNotificationStatus', statusInfo);
  }

  markUserNotificationsAsRead(notifications: NotificationReadStatusDto) : Observable<any>{
    return this.http.put(this.baseUrl + 'notification/markNotificationsAsRead', notifications);
  }

  gatherUserNotificationStatus(userId: number) : Observable<ApiResponse<boolean>> {
    return this.http.get<ApiResponse<boolean>>(this.baseUrl + `notification/getNotificationStatus?userId=${userId}`);
  }

  gatherLatestUserNotifications(userId: number) : Observable<ApiResponse<Notification[]>>{
    return this.http.get<ApiResponse<Notification[]>>(this.baseUrl + `notification/gatherLatestUserNotifications?userId=${userId}`);
  }

  gatherUnreadUserNotificationCount(userId: number) : Observable<ApiResponse<number>>{
    return this.http.get<ApiResponse<number>>(this.baseUrl + `notification/gatherUnreadUserNotificationCount?userId=${userId}`);
  }
}
