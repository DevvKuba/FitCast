import { Component, inject, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ToggleSwitch } from 'primeng/toggleswitch';
import { NotificationService } from '../services/notification.service';
import { AccountService } from '../services/account.service';
import { ToastService } from '../services/toast.service';
import { resetFakeAsyncZone } from '@angular/core/testing';

@Component({
  selector: 'app-notification-toggle',
  imports: [ToggleSwitch, FormsModule],
  templateUrl: './notification-toggle.component.html',
  styleUrl: './notification-toggle.component.css'
})
export class NotificationToggleComponent implements OnInit {
  accountService = inject(AccountService);
  notificationService = inject(NotificationService);
  toastService = inject(ToastService)

  currentUserId: number = 0;
  notificationsToggled: boolean = false;
  latestNotifications: Notification[] | null = null;

  ngOnInit(): void {
    this.currentUserId = this.accountService.currentUser()?.id ?? 0;
  }

  onNotificationToggle(event: {checked: boolean}){
    this.notificationsToggled = event.checked;
    const statusInfo = {
      id: this.currentUserId,
      notificationStatus: this.notificationsToggled
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

  gatherLatestNotifications(){
    this.notificationService.gatherUserNotifications(this.currentUserId).subscribe({
      next: (response) => {
        this.latestNotifications = response.data ?? [];
      }
    })
  }
}
