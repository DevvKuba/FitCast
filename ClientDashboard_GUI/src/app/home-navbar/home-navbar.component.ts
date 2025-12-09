import { Component, inject, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { Menubar } from 'primeng/menubar';
import { AccountService } from '../services/account.service';
import { MenuItem } from 'primeng/api';
import { LoginComponent } from '../login/login.component';

@Component({
  selector: 'app-home-navbar',
  imports: [ButtonModule, Menubar],
  providers: [LoginComponent],
  templateUrl: './home-navbar.component.html',
  styleUrl: './home-navbar.component.css'
})
export class HomeNavbarComponent implements OnInit {
  accountService = inject(AccountService);
  loginComponent = inject(LoginComponent);
  leftMenuItems: MenuItem[] | undefined;
  rightMenuItems: MenuItem[] | undefined;
  
  ngOnInit() {
      // clear for spacing
      this.leftMenuItems = [];
      
      this.rightMenuItems = [
          {
              label: 'Home',
              routerLink: '/',
              icon: 'pi pi-home'
          },
          {
              label: 'Login',
              routerLink: '/login',
              icon: 'pi pi-sign-in'
          },
          {
              label: 'Register',
              routerLink: '/register',
              icon: 'pi pi-user-plus'
          },
      ];
  }
}
