import { Component, effect, inject, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MenuItem } from 'primeng/api';
import { Menubar } from 'primeng/menubar';
import { LoginComponent } from '../login/login.component';
import { AccountService } from '../services/account.service';
import { DrawerModule } from 'primeng/drawer';
import { NotificationToggleComponent } from '../notification-toggle/notification-toggle.component';
import { UserRole } from '../enums/user-role';

@Component({
  selector: 'app-navbar',
  imports: [Menubar, DrawerModule, NotificationToggleComponent],
  providers: [LoginComponent],
  templateUrl: './user-navbar.html',
  styleUrl: './user-navbar.css'
})
export class UserNavbar{

  functionItems: MenuItem[] | undefined;
  generalItems: MenuItem[] | undefined;
  notificationVisibility: boolean = false;
 
  loginComponent = inject(LoginComponent);
  accountService = inject(AccountService);

    constructor(){
        effect(() => {
            const user = this.accountService.currentUser();

            console.log('Effect triggered, user:', user);

            if(!user){
                this.functionItems = [];
                this.generalItems = [];
                return;
            }

            if(this.accountService.currentUser()?.role == UserRole.Trainer){
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
              command: () => {
                this.notificationVisibility = true;

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
        else if (this.accountService.currentUser()?.role == UserRole.Client) {
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
            // {
            //     label: 'Profile',
            //     routerLink: '/trainer-profile',
            //     icon: 'pi pi-user-edit'
            // },
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
        })
        
    }
}
