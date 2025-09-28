import { Component, inject, OnInit } from '@angular/core';
import { TableModule } from 'primeng/table';
import { CommonModule } from '@angular/common';
import { ClientService } from '../services/client.service';
import { Client } from '../models/client';
import { MessageService, SelectItem } from 'primeng/api';
import { Toast } from 'primeng/toast';
import { TagModule } from 'primeng/tag';
import { SelectModule } from 'primeng/select';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { FormsModule } from '@angular/forms';
import { PrimeIcons, MenuItem } from 'primeng/api';
import { concatWith } from 'rxjs';
import { Dialog } from 'primeng/dialog';
import { SpinnerComponent } from "../spinner/spinner.component";
import { Ripple } from 'primeng/ripple';

@Component({
  selector: 'app-client-info',
  imports: [TableModule, CommonModule, TagModule, SelectModule, ButtonModule, InputTextModule, FormsModule, Dialog, SpinnerComponent, Toast, Ripple],
  providers: [MessageService],
  templateUrl: './client-info.component.html',
  styleUrl: './client-info.component.css'
})
export class ClientInfoComponent implements OnInit {
  private clientService = inject(ClientService);
  private messageService = inject(MessageService);

  clients: Client[] = [];
  activityStatuses!: SelectItem[];
  clonedClients: { [s: string]: Client } = {}

  deleteDialogVisible: boolean = false;
  addDialogVisible: boolean = false;
  newClientName : string = "";
  newActivity: boolean = true;
  newTotalBlockSessions : number = 0;
  toastSummary : string = "";
  toastDetail : string = "";

  ngOnInit() {
      this.getClients();

       this.activityStatuses = [
        {label: 'Active', value: true},
        {label: 'Inactive', value: false}
    ];
  }

  onRowEditInit(client: Client) {
        this.clonedClients[client.id as number] = { ...client };
    }

  onRowEditSave(newClient: Client) {
      if (newClient.currentBlockSession >= 0 && newClient.totalBlockSessions > 0) {
          delete this.clonedClients[newClient.id as number];

          this.clientService.updateClient(newClient).subscribe({
            next: (response) => {
              console.log('Client updated successfully', response.message);
              this.toastSummary = 'Success Updating';
              this.toastDetail = `updated client: ${newClient.name} successfully`;
              this.showSuccess();
            },
            error: (response) => {
              console.log('Update Failed', response.message);
              this.toastSummary = 'Error Updating';
              this.toastDetail = `client: ${newClient.name} not updated successfully`;
              this.showError();
            }
          })
      } else {
        console.log("Input values are not valid");
        this.toastSummary = 'Incorrect Values';
        this.toastDetail = `make sure correct update values are provided`;
        this.showError()
      }
  }

  onRowEditCancel(client: Client, index: number) {
      this.clients[index] = this.clonedClients[client.id as number];
      delete this.clonedClients[client.id as number];
  }

  onRowDelete(clientId: number){
    this.clientService.deleteClient(clientId).subscribe({
      next: (response) => {
        console.log(`Successfully deleted client with id: ${clientId} ` + response.message);
        this.toastSummary = 'Success Deleting';
        this.toastDetail = `successfully deleted client with id: ${clientId}`;
        this.showSuccess();
        this.deleteDialogVisible = false;
        this.getClients();
      },
      error: (response) => {
        console.log(`Error deleting client with id: ${clientId} ` + response.message);
        this.toastSummary = 'Error Deleting';
        this.toastDetail = `unsuccessful deletion process of client with id: ${clientId}`;
        this.showError();
      }
    })
  }

  addNewClient(clientName: string, totalBlockSessions: number){
    const newClient = {
      name: clientName,
      isActive: this.newActivity,
      currentBlockSession: 0,
      totalBlockSessions: totalBlockSessions,
      workouts: [],
    }
    this.clientService.addClient(newClient).subscribe({
      next: (response) => {
        console.log(`Success added client: ${clientName} `, response.message)
        this.toastSummary = 'Success Adding';
        this.toastDetail = `added client: ${clientName} successfully`
        this.showSuccess();
        this.addDialogVisible = false;
        this.getClients();
      },
      error: (response) => {
        console.log(`Error adding client: ${clientName} `, response.message)
        this.toastSummary = 'Error Adding';
        this.toastDetail = `adding client: ${clientName} was not successful`
        this.showError()
      }
    })
  }

  getClients(){
    this.clientService.getAllClients().subscribe({
      next: (response) => {
        this.clients = response.data ?? [];
      }
    })
  }

  showDialogForDelete(){
    this.deleteDialogVisible = true;
  }

  showDialogForAdd(){
    this.addDialogVisible = true;
  }

  showSuccess() {
        this.messageService.add({ severity: 'success', summary: this.toastSummary, detail: this.toastDetail });
  }

  showError() {
        this.messageService.add({ severity: 'error', summary: this.toastSummary, detail: this.toastDetail });
  }

  getActivities(isActive : boolean) : string {
    return isActive ? 'success' : 'danger';
  }

  getActivityLabel(isActive: boolean) : string {
    return isActive ? 'Active' : 'Inactive';
  }

}
