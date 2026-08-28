import { Component, inject, Input, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
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

  smsNotificationsToggled: boolean | undefined;
  communicationType = CommunicationType;

  ngOnInit(): void {
    this.gatherNotificationStatus();
  }

  onNotificationToggle(event: {checked: boolean}){
    this.smsNotificationsToggled = event.checked;
    const statusInfo = {
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

  gatherNotificationStatus() {
    this.notificationService.gatherUserNotificationStatus().subscribe({
      next: (response) => {
        this.smsNotificationsToggled = response.data ?? false;
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

  getNotificationTypeIcon(type: NotificationType): string {
    switch(type) {
      case NotificationType.TrainerBlockCompletionReminder:
      case NotificationType.ClientBlockCompletionReminder:
        return 'pi-calendar-times';
      case NotificationType.NewClientConfigurationReminder:
        return 'pi-user-plus';
      case NotificationType.ClientStepsTrackedNotification:
        return 'pi-chart-line';
      case NotificationType.RetrievalWorkoutsCountNotification:
        return 'pi-history';
      case NotificationType.PendingPaymentCreatedAlert:
        return 'pi-credit-card';
      case NotificationType.QuickAddWorkoutReminder:
        return 'pi-bolt';
      default:
        return 'pi-bell';
    }
  }
}
