import { Component, effect, inject } from '@angular/core';
import { MenuItem } from 'primeng/api';
import { Menu } from 'primeng/menu';
import { Avatar } from 'primeng/avatar';
import { LoginComponent } from '../login/login.component';
import { AccountService } from '../services/account.service';
import { DrawerModule } from 'primeng/drawer';
import { NotificationToggleComponent } from '../notification-toggle/notification-toggle.component';
import { UserRole } from '../enums/user-role';
import { OverlayBadgeModule } from 'primeng/overlaybadge';
import { NotificationService } from '../services/notification.service';
import { Notification } from '../models/notification';
import { NotificationReadStatusDto } from '../models/dtos/notification-read-status-dto';

@Component({
  selector: 'app-navbar',
  imports: [Menu, Avatar, DrawerModule, NotificationToggleComponent, OverlayBadgeModule],
  providers: [LoginComponent],
  templateUrl: './user-navbar.html',
  styleUrl: './user-navbar.css'
})
export class UserNavbar{

    loginComponent = inject(LoginComponent);
    accountService = inject(AccountService);
    notificationService = inject(NotificationService);

    sidebarItems: MenuItem[] = [];
    accountMenuItems: MenuItem[] = [];
    mobileMenuItems: MenuItem[] = [];
    latestNotifications: Notification[] = [];
    notificationVisibility: boolean = false;
    mobileNavVisible: boolean = false;

    constructor(){
        effect(() => {
            const user = this.accountService.currentUser();

            if(!user){
                this.sidebarItems = [];
                this.accountMenuItems = [];
                this.mobileMenuItems = [];
                return;
            }
            this.notificationService.refreshUnreadCount(user.id);

        });

        // watch signal and rebuild when the signal changes
        effect(() => {
            const user = this.accountService.currentUser();

            if(user){
                this.buildMenuItems(user.role);
            }
        })

    }

    buildMenuItems(role: UserRole){
        if(role == UserRole.Trainer){
            this.sidebarItems = [
            {
                label: 'Client Info',
                routerLink: '/client-info',
                icon: 'pi pi-users'
            },
            {
                label: 'Client Workouts',
                routerLink: '/client-workouts',
                icon: 'pi pi-table'
            },
            {
                label: 'Client Payments',
                routerLink: '/client-payments',
                icon: 'pi pi-credit-card'
            },
            {
                label: 'Trainer Analytics',
                routerLink: '/trainer-analytics',
                icon: 'pi pi-chart-bar'
            },

        ];
        this.accountMenuItems = [
            {
                label: 'Home',
                routerLink: '/',
                icon: 'pi pi-home'
            },
            {
                label: 'Profile',
                routerLink: '/trainer-profile',
                icon: 'pi pi-user-edit'
            },
            {
                label: 'Logout',
                icon: 'pi pi-sign-out',
                command: () => this.loginComponent.userLogout(this.loginComponent.storageItem)
            },
        ]
        }
        else if (role == UserRole.Client) {
            this.sidebarItems = [
            {
                label: 'Workouts',
                routerLink: '/client-personal-workouts',
                icon: 'pi pi-table'
            },
            {
                label: 'Payments',
                routerLink: '/client-personal-payments',
                icon: 'pi pi-credit-card'
            }

        ];
        this.accountMenuItems = [
            {
                label: 'Home',
                routerLink: '/',
                icon: 'pi pi-home'
            },
            {
                label: 'Logout',
                icon: 'pi pi-sign-out',
                command: () => this.loginComponent.userLogout(this.loginComponent.storageItem)
            },
        ]
        }
        else {
            this.sidebarItems = [];
            this.accountMenuItems = [
                {
                label: 'Home',
                routerLink: '/',
                icon: 'pi pi-home'
            },
            {
                label: 'Logout',
                icon: 'pi pi-sign-out',
                command: () => this.loginComponent.userLogout(this.loginComponent.storageItem)
            },
            ];
        }

        this.mobileMenuItems = [...this.sidebarItems, ...this.accountMenuItems];
    }

    onNotificationBellClick(){
        this.notificationVisibility = true;
        this.onNotificationDrawerOpen();
    }

    onNotificationDrawerOpen(){
        const userId = this.accountService.currentUser()?.id;
        if(!userId) return;

        this.notificationService.gatherLatestUserNotifications(userId).subscribe({
            next: (response) => {
                this.latestNotifications = response.data ?? [];
                const notificationIds = this.latestNotifications.map((notification) => notification.id);

                if(notificationIds.length > 0){
                    const readStatus: NotificationReadStatusDto = {
                        userId,
                        NotificationIds: notificationIds
                    };

                    this.notificationService.markUserNotificationsAsRead(readStatus).subscribe({
                        next: () => {
                            this.notificationService.refreshUnreadCount(userId);
                        }
                    });
                }
            }
        });
    }

    getBellBadge(): string {
        return this.notificationService.unreadNotificationCount().toString();
    }
}
