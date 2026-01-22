import { Component, inject, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ToggleSwitch } from 'primeng/toggleswitch';
import { NotificationService } from '../services/notification.service';
import { AccountService } from '../services/account.service';

@Component({
  selector: 'app-notification-toggle',
  imports: [ToggleSwitch, FormsModule],
  templateUrl: './notification-toggle.component.html',
  styleUrl: './notification-toggle.component.css'
})
export class NotificationToggleComponent implements OnInit {
  accountService = inject(AccountService);
  notificationService = inject(NotificationService);

  currentUserId: number = 0;
  notificationsToggled: boolean = false;
  latestNotifications: Notification[] | null = null;

  ngOnInit(): void {
    this.currentUserId = this.accountService.currentUser()?.id ?? 0;
  }

  onNotificationToggle(event: {checked: boolean}){
    this.notificationsToggled = event.checked;

  }

  // gatherLatestNotifications(){
  //   this.notificationService.gatherUserNotifications(this.currentUserId).subscribe({
  //     next: (response) => {
  //       this.latestNotifications = response.data ?? [];
  //     }
  //   })
  // }
}
