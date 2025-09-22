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
import { FormsModule } from '@angular/forms';
import { PrimeIcons, MenuItem } from 'primeng/api';
import { concatWith } from 'rxjs';

@Component({
  selector: 'app-client-info',
  imports: [TableModule, ToastModule, CommonModule, TagModule, SelectModule, ButtonModule, InputTextModule, FormsModule],
  templateUrl: './client-info.component.html',
  styleUrl: './client-info.component.css'
})
export class ClientInfoComponent implements OnInit {
  clients: Client[] = [];
  items!: MenuItem[];
  clonedClients: { [s: string]: Client } = {}
  private clientService = inject(ClientService);

  ngOnInit() {
      this.getClients();

      this.items = [
        {
          label: 'New',
          icon: PrimeIcons.PLUS,
        },
        {
          label: 'Delete',
          icon: PrimeIcons.TRASH,
        }
      ];
  }

  onRowEditInit(client: Client) {
        this.clonedClients[client.id as number] = { ...client };
    }

  onRowEditSave(newClient: Client) {
      if (newClient.currentBlockSession > 0 && newClient.totalBlockSessions > 0) {
          delete this.clonedClients[newClient.id as number];
          this.clientService.updateClient(newClient).subscribe({
            next: (response) => {
              console.log('Client updated successfully', response);
            },
            error: (error) => {
              console.log('Update Failed', error);
            }
          })
          
          // this.messageService.add({ severity: 'success', summary: 'Success', detail: 'Client updated' });
      } else {
        console.log("Input values are not valid")
          // this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Invalid Sessions' });
      }
  }

  onRowEditCancel(client: Client, index: number) {
      this.clients[index] = this.clonedClients[client.id as number];
      delete this.clonedClients[client.id as number];
  }

  onRowDelete(clientId: number){
    this.clientService.deleteClient(clientId).subscribe({
      next: (response) => {
        console.log(`Successfully deleted client with id: ${clientId} ` + response)
      },
      error: (error) => {
        console.log(`Error deleting client with id: ${clientId} ` + error)
      }
    })
  }


  getClients(){
    this.clientService.getAllClients().subscribe({
      next: (data) => {
        this.clients = data;
      }
    })
  }

}
