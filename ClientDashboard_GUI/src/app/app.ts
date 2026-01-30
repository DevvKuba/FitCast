import { HttpClient } from '@angular/common/http';
import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { Toast } from 'primeng/toast';
import { AccountService } from './services/account.service';
import { UserNavbar } from './user-navbar/user-navbar';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, UserNavbar, Toast],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App implements OnInit {
  http = inject(HttpClient);
  protected readonly title = signal('FitCast');
  clients: any;
  accountService = inject(AccountService);

  ngOnInit(): void {
    this.accountService.initializeAuthState();
}
}


