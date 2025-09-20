import { HttpClient } from '@angular/common/http';
import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { Navbar } from './navbar/navbar';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, Navbar],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App implements OnInit {
  http = inject(HttpClient);
  protected readonly title = signal('ClientDashboard_GUI');
  clients: any;

  ngOnInit(): void {
  // general structure
  // this.http.get('https://localhost:7217/onFirstSession').subscribe({
  //   next: response => this.clients = response,
  //   error: error => console.log(error),
  //   complete: () => console.log('Request has completed')
  // })
}
}


