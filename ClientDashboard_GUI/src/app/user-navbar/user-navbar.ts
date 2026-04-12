import { Component, effect, inject, OnInit, signal, WritableSignal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MenuItem } from 'primeng/api';
import { Menubar } from 'primeng/menubar';
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
  imports: [Menubar, DrawerModule, NotificationToggleComponent, OverlayBadgeModule],
  providers: [LoginComponent],
  templateUrl: './user-navbar.html',
  styleUrl: './user-navbar.css'
})
export class UserNavbar{

    loginComponent = inject(LoginComponent);
    accountService = inject(AccountService);
    notificationService = inject(NotificationService);

    functionItems: MenuItem[] | undefined;
    generalItems: MenuItem[] | undefined;
    mobileMenuItems: MenuItem[] = [];
    latestNotifications: Notification[] = [];
    notificationVisibility: boolean = false;

    constructor(){
        effect(() => {
            const user = this.accountService.currentUser();

            if(!user){
                this.functionItems = [];
                this.generalItems = [];
                this.mobileMenuItems = [];
                return;
            }
            this.notificationService.refreshUnreadCount(user.id);

        });

        // watch signal and rebuild when the signal changes
        effect(() => {
            const count = this.notificationService.unreadNotificationCount();
            const user = this.accountService.currentUser();

            if(user){
                this.buildMenuItems(user.role);
            }
        })
        
    }

    buildMobileMenuItems() {
        const functionItems = this.functionItems ?? [];
        const generalItems = this.generalItems ?? [];

        this.mobileMenuItems = [...functionItems, ...generalItems];
    }

    buildMenuItems(role: UserRole){
        if(role == UserRole.Trainer){
            console.log(this.accountService.currentUser()?.role)
            this.functionItems = [
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
        this.generalItems = [
            {
              icon: 'pi pi-bell',
              badge: this.getBellBadge(),
              command: () => {
                this.notificationVisibility = true;
                this.onNotificationDrawerOpen();
              }
              
            },
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
            this.buildMobileMenuItems();
        }
        else if (role == UserRole.Client) {
            this.functionItems = [
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
        this.generalItems = [
             {
              icon: 'pi pi-bell',
              badge: this.getBellBadge(),
              command: () => {
                this.notificationVisibility = true;
                this.onNotificationDrawerOpen();
              }
              
            },
            {
                label: 'Home',
                routerLink: '/',
                icon: 'pi pi-home'
            },            
            // client profile
            {
                label: 'Logout',
                icon: 'pi pi-sign-out',
                command: () => this.loginComponent.userLogout(this.loginComponent.storageItem)
            },
        ]
            this.buildMobileMenuItems();
        }
        else {
            this.functionItems = [];
            this.generalItems = [
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
                this.buildMobileMenuItems();
        }
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
