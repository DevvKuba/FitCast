import { HttpClient } from '@angular/common/http';
import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App implements OnInit {
  http = inject(HttpClient);
  protected readonly title = signal('ClientDashboard_GUI');
  clients: any;

  ngOnInit(): void {
  this.http.get('https://localhost:7217/onFirstSession').subscribe({
    next: response => this.clients = response,
    error: error => console.log(error),
    complete: () => console.log('Request has completed')
  })
}
}


