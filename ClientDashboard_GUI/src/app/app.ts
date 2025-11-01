import { HttpClient } from '@angular/common/http';
import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { Navbar } from './navbar/navbar';
import { Toast } from 'primeng/toast';
import { AccountService } from './services/account.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, Navbar, Toast],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App implements OnInit {
  http = inject(HttpClient);
  protected readonly title = signal('ClientDashboard_GUI');
  clients: any;
  accountService = inject(AccountService);

  ngOnInit(): void {
    this.accountService.initializeAuthState();
}
}


