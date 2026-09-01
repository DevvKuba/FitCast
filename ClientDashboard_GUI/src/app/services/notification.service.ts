import { HttpClient } from '@angular/common/http';
import { inject, Injectable, signal } from '@angular/core';
import { UserDto } from '../models/dtos/user-dto';
import { environment } from '../environments/environment';
import { Observable } from 'rxjs';
import { ApiResponse } from '../models/api-response';
import { NotificationSmsStatusDto } from '../models/dtos/notification-sms-status-dto';
import { NotificationResponseDto } from '../models/dtos/notification-response-dto';
import { NotificationReadStatusDto } from '../models/dtos/notification-read-status-dto';

@Injectable({
  providedIn: 'root'
})
export class NotificationService {
  http = inject(HttpClient);
  unreadNotificationCount = signal<number>(0);
  baseUrl = environment.apiUrl;

  refreshUnreadCount() {
    this.gatherUnreadUserNotificationCount().subscribe({
      next: (response) => {
        this.unreadNotificationCount.set(response.data ?? 0);
      }
    })
  }

  toggleUserSMSNotificationStatus(statusInfo: NotificationSmsStatusDto): Observable<any>{
    return this.http.put(this.baseUrl + 'notification/changeNotificationStatus', statusInfo);
  }

  markUserNotificationsAsRead(notifications: NotificationReadStatusDto) : Observable<any>{
    return this.http.put(this.baseUrl + 'notification/markNotificationsAsRead', notifications);
  }

  gatherUserNotificationStatus() : Observable<ApiResponse<boolean>> {
    return this.http.get<ApiResponse<boolean>>(this.baseUrl + `notification/getNotificationStatus`);
  }

  gatherLatestUserNotifications() : Observable<ApiResponse<NotificationResponseDto[]>>{
    return this.http.get<ApiResponse<NotificationResponseDto[]>>(this.baseUrl + `notification/gatherLatestUserNotifications`);
  }

  gatherAllUserNotifications() : Observable<ApiResponse<NotificationResponseDto[]>>{
    return this.http.get<ApiResponse<NotificationResponseDto[]>>(this.baseUrl + `notification/gatherAllUserNotifications`);
  }

  gatherUnreadUserNotificationCount() : Observable<ApiResponse<number>>{
    return this.http.get<ApiResponse<number>>(this.baseUrl + `notification/gatherUnreadUserNotificationCount`);
  }
}
