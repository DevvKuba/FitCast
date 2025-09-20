import { Component, inject, OnInit } from '@angular/core';
import { TableModule } from 'primeng/table';
import { CommonModule } from '@angular/common';
import { ClientService } from '../services/client.service';
import { Client } from '../models/client';

@Component({
  selector: 'app-client-info',
  imports: [TableModule, CommonModule],
  templateUrl: './client-info.component.html',
  styleUrl: './client-info.component.css'
})
export class ClientInfoComponent {
  clients: Client[] = [];
    private workoutService = inject(ClientService);

    ngOnInit() {
        // populate clients with data
    }
}
