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
  templateUrl: './navbar.html',
  styleUrl: './navbar.css'
})
export class Navbar implements OnInit {
  items: MenuItem[] | undefined;
  loginComponent = inject(LoginComponent);
  accountService = inject(AccountService);

    ngOnInit() {
        this.items = [
            {
                label: 'Home',
                routerLink: '/',
                icon: 'pi pi-home'
            },
            {
                label: 'Client Info',
                routerLink: '/client-info',
                icon: 'pi pi-user'
            },
            {
                label: 'Client Workouts',
                routerLink: '/client-workouts',
                icon: 'pi pi-table'
            },
            {
                label: 'Logout',
                icon: 'pi pi-sign-out',
                command: () => this.loginComponent.trainerLogout(this.loginComponent.storageItem)
            },
        ]
    }
}
