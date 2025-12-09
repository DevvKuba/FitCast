import { Component, inject, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MenuItem } from 'primeng/api';
import { Menubar } from 'primeng/menubar';
import { LoginComponent } from '../login/login.component';
import { AccountService } from '../services/account.service';

@Component({
  selector: 'app-navbar',
  imports: [Menubar],
  providers: [LoginComponent],
  templateUrl: './user-navbar.html',
  styleUrl: './user-navbar.css'
})
export class UserNavbar implements OnInit {
  functionItems: MenuItem[] | undefined;
  generalItems: MenuItem[] | undefined;
 
  loginComponent = inject(LoginComponent);
  accountService = inject(AccountService);

    ngOnInit() {
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
                command: () => this.loginComponent.trainerLogout(this.loginComponent.storageItem)
            },
        ]
    }
}
