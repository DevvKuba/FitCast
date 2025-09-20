import { Component, inject, OnInit } from '@angular/core';
import { TableModule } from 'primeng/table';
import { CommonModule } from '@angular/common';
import { ClientService } from '../services/client.service';
import { Client } from '../models/client';
import { MessageService, SelectItem } from 'primeng/api';
import { ToastModule } from 'primeng/toast';
import { TagModule } from 'primeng/tag';
import { SelectModule } from 'primeng/select';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';

@Component({
  selector: 'app-client-info',
  imports: [TableModule, ToastModule, CommonModule, TagModule, SelectModule, ButtonModule, InputTextModule],
  templateUrl: './client-info.component.html',
  styleUrl: './client-info.component.css'
})
export class ClientInfoComponent implements OnInit {
  clients: Client[] = [];
  clonedClients: { [s: string]: Client } = {}
  private clientService = inject(ClientService);

  ngOnInit() {
      this.getClients();
  }

  onRowEditInit(client: Client) {
        this.clonedClients[client.id as number] = { ...client };
    }

    onRowEditSave(client: Client) {
        if (client.currentBlockSession > 0 && client.totalBlockSession > 0) {
            delete this.clonedClients[client.id as number];
            console.log("successfully updated")
            // this.messageService.add({ severity: 'success', summary: 'Success', detail: 'Client updated' });
        } else {
          console.log("Unsuccessfully trying to update")
            // this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Invalid Sessions' });
        }
    }

    onRowEditCancel(client: Client, index: number) {
        this.clients[index] = this.clonedClients[client.id as number];
        delete this.clonedClients[client.id as number];
    }


  getClients(){
    this.clientService.getAllClients().subscribe({
      next: (data) => {
        this.clients = data;
      }
    })
  }

}
