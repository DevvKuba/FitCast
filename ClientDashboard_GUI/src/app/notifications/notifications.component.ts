import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ToggleSwitch } from 'primeng/toggleswitch';
import { TooltipModule } from 'primeng/tooltip';
import { AutoCompleteModule } from 'primeng/autocomplete';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';
import { InputTextModule } from 'primeng/inputtext';
import { ButtonModule } from 'primeng/button';
import { TableModule } from 'primeng/table';
import { ToggleButtonModule } from 'primeng/togglebutton';
import { NotificationService } from '../services/notification.service';
import { ClientService } from '../services/client.service';
import { AccountService } from '../services/account.service';
import { ToastService } from '../services/toast.service';
import { NotificationResponseDto } from '../models/dtos/notification-response-dto';
import { NotificationReadStatusDto } from '../models/dtos/notification-read-status-dto';
import { NotificationType } from '../enums/notification-type';
import { UserRole } from '../enums/user-role';

@Component({
  selector: 'app-notifications',
  imports: [CommonModule, FormsModule, ToggleSwitch, TooltipModule, AutoCompleteModule, IconFieldModule, InputIconModule, InputTextModule, ButtonModule, TableModule, ToggleButtonModule],
  templateUrl: './notifications.component.html',
  styleUrl: './notifications.component.css'
})
export class NotificationsComponent implements OnInit {
  notificationService = inject(NotificationService);
  clientService = inject(ClientService);
  accountService = inject(AccountService);
  toastService = inject(ToastService);

  allNotifications: NotificationResponseDto[] = [];
  clients: { id: number, name: string }[] = [];
  selectedClient: { id: number, name: string } = { id: 0, name: '' };
  searchText: string = '';
  smsNotificationsToggled: boolean | undefined;
  showAllNotifications: boolean = false;

  ngOnInit(): void {
    this.gatherAllUserNotifications();
    this.gatherNotificationStatus();
  }

  isTrainer(): boolean {
    return this.accountService.currentUser()?.role === UserRole.Trainer;
  }

  filteredNotifications(): NotificationResponseDto[] {
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

  todayNotifications(): NotificationResponseDto[] {
    const today = new Date();

    return this.filteredNotifications().filter((notification) => this.isSameCalendarDay(new Date(notification.sentAt), today));
  }

  pastWeekNotifications(): NotificationResponseDto[] {
    const today = new Date();
    const cutoff = new Date();
    cutoff.setDate(cutoff.getDate() - 7);

    return this.filteredNotifications().filter((notification) => {
      const sentAt = new Date(notification.sentAt);
      return sentAt >= cutoff && !this.isSameCalendarDay(sentAt, today);
    });
  }

  tableNotifications(): NotificationResponseDto[] {
    return this.showAllNotifications ? this.filteredNotifications() : this.pastWeekNotifications();
  }

  private isSameCalendarDay(a: Date, b: Date): boolean {
    return a.getFullYear() === b.getFullYear() && a.getMonth() === b.getMonth() && a.getDate() === b.getDate();
  }

  gatherAllUserNotifications() {
    this.notificationService.gatherAllUserNotifications().subscribe({
      next: (response) => {
        this.allNotifications = response.data ?? [];
        this.markTodayNotificationsAsRead();
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

  // Today's notifications are considered "seen" the moment they're loaded onto this page
  markTodayNotificationsAsRead() {
    const ids = this.unreadIds(this.todayNotifications());
    if (ids.length === 0) return;

    const userId = this.accountService.currentUser()?.id;
    if (!userId) return;

    const readStatus: NotificationReadStatusDto = { userId, notificationIds: ids };

    this.notificationService.markUserNotificationsAsRead(readStatus).subscribe({
      next: () => {
        this.notificationService.refreshUnreadCount();
        this.gatherAllUserNotifications();
      }
    });
  }

  private unreadIds(notifications: NotificationResponseDto[]): number[] {
    return notifications.filter((notification) => !notification.isRead).map((notification) => notification.id);
  }

  getRelativeTime(sentAt: string): string {
    const diffMinutes = Math.floor((Date.now() - new Date(sentAt).getTime()) / (1000 * 60));

    if (diffMinutes < 1) return 'Just now';
    if (diffMinutes < 60) return `${diffMinutes} minute${diffMinutes === 1 ? '' : 's'} ago`;

    const diffHours = Math.floor(diffMinutes / 60);
    return `${diffHours} hour${diffHours === 1 ? '' : 's'} ago`;
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
