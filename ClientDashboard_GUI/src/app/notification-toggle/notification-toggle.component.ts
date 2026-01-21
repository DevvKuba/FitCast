import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ToggleSwitch } from 'primeng/toggleswitch';

@Component({
  selector: 'app-notification-toggle',
  imports: [ToggleSwitch, FormsModule],
  templateUrl: './notification-toggle.component.html',
  styleUrl: './notification-toggle.component.css'
})
export class NotificationToggleComponent {
  notificationsToggled: boolean = false;
}
