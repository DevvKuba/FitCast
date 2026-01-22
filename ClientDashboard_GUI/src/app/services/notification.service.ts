import { HttpClient } from '@angular/common/http';
import { inject, Injectable, signal } from '@angular/core';
import { UserDto } from '../models/dtos/user-dto';
import { environment } from '../environments/environment';
import { Observable } from 'rxjs';
import { ApiResponse } from '../models/api-response';
import { NotificationStatusDto } from '../models/dtos/notification-status-dto';

@Injectable({
  providedIn: 'root'
})
export class NotificationService {
  http = inject(HttpClient);
  currentUser = signal<UserDto | null>(null);
  baseUrl = environment.apiUrl;

  toggleUserSMSNotificationStatus(statusInfo: NotificationStatusDto): Observable<any>{
    return this.http.put(this.baseUrl + 'notification/changeNotificationStatus', statusInfo);
  }

  gatherUserNotificationStatus(userId: number) : Observable<ApiResponse<boolean>> {
    return this.http.get<ApiResponse<boolean>>(this.baseUrl + `notification/getNotificationStatus?userId=${userId}`);
  }

  gatherUserNotifications(userId: number) : Observable<ApiResponse<Notification[]>>{
    return this.http.get<ApiResponse<Notification[]>>(this.baseUrl + `notification/GatherLatestUserNotifications?userId=${userId}`);
  }
}
