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
import { Dialog } from 'primeng/dialog';

@Component({
  selector: 'app-client-info',
  imports: [TableModule, ToastModule, CommonModule, TagModule, SelectModule, ButtonModule, InputTextModule, FormsModule, Dialog],
  templateUrl: './client-info.component.html',
  styleUrl: './client-info.component.css'
})
export class ClientInfoComponent implements OnInit {
  clients: Client[] = [];
  clonedClients: { [s: string]: Client } = {}
  private clientService = inject(ClientService);
  visible: boolean = false;
  newClientName : string = "";
  newTotalBlockSessions : number = 0;

  ngOnInit() {
      this.getClients();
  }

  showDialog(){
    this.visible = true;
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
        this.getClients();
      },
      error: (error) => {
        console.log(`Error deleting client with id: ${clientId} ` + error)
      }
    })
  }

  addNewClient(clientName: string, totalBlockSessions: number){
    const newClient = {
      name: clientName,
      currentBlockSession: 0,
      totalBlockSessions: totalBlockSessions,
      workouts: [],
    }
    this.clientService.addClient(newClient).subscribe({
      next: (response) => {
        console.log(`Success added client: ${clientName} `, response)
        // success toast
        this.visible = false;
        this.getClients();
      },
      error: (error) => {
        console.log(`Error adding client: ${clientName} `, error)
        // error toast
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
