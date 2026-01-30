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
    latestNotifications: Notification[] = [];
    notificationVisibility: boolean = false;

    constructor(){
        effect(() => {
            const user = this.accountService.currentUser();

            if(!user){
                this.functionItems = [];
                this.generalItems = [];
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

    buildMenuItems(role: UserRole){
        if(role == UserRole.Trainer){
            console.log(this.accountService.currentUser()?.role)
            this.functionItems = [
            {
                label: 'Client Analytics',
                routerLink: '/client-analytics',
                icon: 'pi pi-chart-line'
            },
            {
                label: 'Trainer Analytics',
                routerLink: '/trainer-analytics',
                icon: 'pi pi-chart-bar'
            },
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
            this.functionItems = [
            {
                label: 'Personal Info',
                routerLink: '/client-info',
                icon: 'pi pi-users'
            },
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
        }
    }

    onNotificationDrawerOpen(){
        const userId = this.accountService.currentUser()?.id;
        if(!userId) return;

        this.notificationService.gatherLatestUserNotifications(userId).subscribe({
            next: (response) => {
                this.latestNotifications = response.data ?? [];
                const list = {
                    readNotificationsList: response.data ?? []
                }

                if(list.readNotificationsList.length > 0){
                    this.notificationService.markUserNotificationsAsRead(list).subscribe({
                        next: () => {
                            this.notificationService.refreshUnreadCount(this.accountService.currentUser()?.id ?? 0);
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
