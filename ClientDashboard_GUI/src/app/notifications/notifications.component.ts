import { Component, inject, OnInit } from '@angular/core';
import { NotificationService } from '../services/notification.service';
import { ClientService } from '../services/client.service';
import { AccountService } from '../services/account.service';
import { ToastService } from '../services/toast.service';
import { Notification } from '../models/notification';
import { NotificationReadStatusDto } from '../models/dtos/notification-read-status-dto';
import { NotificationType } from '../enums/notification-type';
import { UserRole } from '../enums/user-role';

@Component({
  selector: 'app-notifications',
  imports: [],
  templateUrl: './notifications.component.html',
  styleUrl: './notifications.component.css'
})
export class NotificationsComponent implements OnInit {
  notificationService = inject(NotificationService);
  clientService = inject(ClientService);
  accountService = inject(AccountService);
  toastService = inject(ToastService);

  allNotifications: Notification[] = [];
  clients: { id: number, name: string }[] = [];
  selectedClient: { id: number, name: string } = { id: 0, name: '' };
  searchText: string = '';
  smsNotificationsToggled: boolean | undefined;

  ngOnInit(): void {
    this.gatherAllUserNotifications();
    this.gatherNotificationStatus();
  }

  isTrainer(): boolean {
    return this.accountService.currentUser()?.role === UserRole.Trainer;
  }

  filteredNotifications(): Notification[] {
    if (!this.allNotifications) return [];
    let result = this.allNotifications;

    if (this.selectedClient.id) {
      result = result.filter((notification) => notification.clientId === this.selectedClient.id);
    }

    if (this.searchText.trim() !== '') {
      const search = this.searchText.trim().toLowerCase();
      result = result.filter((notification) => notification.message.toLowerCase().includes(search));
    }

    return result;
  }

  todayNotifications(): Notification[] {
    const today = new Date();

    return this.filteredNotifications().filter((notification) => this.isSameCalendarDay(new Date(notification.sentAt), today));
  }

  pastWeekNotifications(): Notification[] {
    const today = new Date();
    const cutoff = new Date();
    cutoff.setDate(cutoff.getDate() - 7);

    return this.filteredNotifications().filter((notification) => {
      const sentAt = new Date(notification.sentAt);
      return sentAt >= cutoff && !this.isSameCalendarDay(sentAt, today);
    });
  }

  private isSameCalendarDay(a: Date, b: Date): boolean {
    return a.getFullYear() === b.getFullYear() && a.getMonth() === b.getMonth() && a.getDate() === b.getDate();
  }

  gatherAllUserNotifications() {
    this.notificationService.gatherAllUserNotifications().subscribe({
      next: (response) => {
        this.allNotifications = response.data ?? [];
      }
    });
  }

  gatherNotificationStatus() {
    this.notificationService.gatherUserNotificationStatus().subscribe({
      next: (response) => {
        this.smsNotificationsToggled = response.data ?? false;
      }
    });
  }

  gatherClientNames() {
    const trainerId = this.accountService.currentUser()?.id;
    if (!trainerId) return;

    this.clientService.gatherClientNames(trainerId).subscribe({
      next: (response) => {
        this.clients = response;
      }
    });
  }

  onNotificationToggle(event: { checked: boolean }) {
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

  markAsRead(ids: number[]) {
    if (ids.length === 0) return;

    const userId = this.accountService.currentUser()?.id;
    if (!userId) return;

    const readStatus: NotificationReadStatusDto = { userId, notificationIds: ids };

    this.notificationService.markUserNotificationsAsRead(readStatus).subscribe({
      next: (response) => {
        this.toastService.showSuccess('Success', response.message);
        this.notificationService.refreshUnreadCount();
        this.gatherAllUserNotifications();
      },
      error: (response) => {
        this.toastService.showError('Error', response.error.message);
      }
    });
  }

  dismissNotification(id: number) {
    this.markAsRead([id]);
  }

  markAllTodayAsRead() {
    const unreadIds = this.todayNotifications().filter((notification) => !notification.isRead).map((notification) => notification.id);
    this.markAsRead(unreadIds);
  }

  getNotificationTypeIcon(type: NotificationType): string {
    switch (type) {
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
