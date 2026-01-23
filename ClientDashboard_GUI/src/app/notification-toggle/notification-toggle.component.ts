import { Component, inject, Input, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';import { CommonModule } from '@angular/common';
import { ToggleSwitch } from 'primeng/toggleswitch';
import { NotificationService } from '../services/notification.service';
import { AccountService } from '../services/account.service';
import { ToastService } from '../services/toast.service';
import { Notification } from '../models/notification';
import { CommunicationType } from '../enums/communication-type';
import { NotificationType } from '../enums/notification-type';

@Component({
  selector: 'app-notification-toggle',
  imports: [ToggleSwitch, FormsModule, CommonModule],
  templateUrl: './notification-toggle.component.html',
  styleUrl: './notification-toggle.component.css'
})
export class NotificationToggleComponent implements OnInit {
  @Input() latestNotifications: Notification[] | null = null;

  accountService = inject(AccountService);
  notificationService = inject(NotificationService);
  toastService = inject(ToastService);

  currentUserId: number = 0;
  smsNotificationsToggled: boolean | undefined;
  communicationType = CommunicationType;
  notificationType = NotificationType;

  ngOnInit(): void {
    this.currentUserId = this.accountService.currentUser()?.id ?? 0;
    this.gatherNotificationStatus();
    this.gatherLatestNotifications();
  }

  onNotificationToggle(event: {checked: boolean}){
    this.smsNotificationsToggled = event.checked;
    const statusInfo = {
      id: this.currentUserId,
      notificationStatus: this.smsNotificationsToggled
    }

    this.notificationService.toggleUserSMSNotificationStatus(statusInfo).subscribe({
      next: (response) => {
        this.toastService.showSuccess('Success', response.message);
      },
      error: (response) => {
        this.toastService.showError('Error', response.error.message);
      }
    });
  }

  // might not need at all since this is being done at the user-navbar / parent level 
  // changeNotificationsToReadStatus(notifications: Notification[]){
  //   const notificationList = {
  //     readNotificationsList: notifications
  //   }
  //   this.notificationService.markUserNotificationsAsRead(notificationList).subscribe({
  //     next: (response) => {
  //       console.log(response.message);
  //     }
  //   })
  // }

  gatherNotificationStatus() {
    this.notificationService.gatherUserNotificationStatus(this.currentUserId).subscribe({
      next: (response) => {
        this.smsNotificationsToggled = response.data ?? false;
      }
    })
  }

  gatherLatestNotifications(){
    this.notificationService.gatherLatestUserNotifications(this.currentUserId).subscribe({
      next: (response) => {
        this.latestNotifications = response.data ?? [];
      }
    })
  }

  getCommunicationType(type: CommunicationType) : string{
    switch(type) {
      case CommunicationType.Sms:
        return 'SMS';
      case CommunicationType.Email:
        return 'Email';
      case CommunicationType.InApp:
        return 'In-App';
      default:
        return 'Unknown';
    }
  }
}
