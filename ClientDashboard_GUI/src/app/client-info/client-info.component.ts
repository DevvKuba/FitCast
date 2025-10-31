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
import { AccountService } from '../services/account.service';
import { ToastService } from '../services/toast.service';
import { InputNumberModule } from 'primeng/inputnumber';

@Component({
  selector: 'app-client-info',
  imports: [TableModule, CommonModule, TagModule, SelectModule, ButtonModule, InputTextModule, FormsModule, Dialog, SpinnerComponent, Toast, Ripple, InputNumberModule],
  providers: [MessageService],
  templateUrl: './client-info.component.html',
  styleUrl: './client-info.component.css'
})
export class ClientInfoComponent implements OnInit {
  private clientService = inject(ClientService);
  private toastService = inject(ToastService);
  private accountService = inject(AccountService);

  clients: Client[] | null = null;
  activityStatuses!: SelectItem[];
  clonedClients: { [s: string]: Client } = {}

  trainerId : number = 0;
  deleteDialogVisible: boolean = false;
  addDialogVisible: boolean = false;
  newClientName : string = "";
  newActivity: boolean = true;
  newTotalBlockSessions : number = 0;
  toastSummary: string = "";
  toastDetail: string = "";
  deleteClientId: number = 0;
  deleteClientName: string = "";

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
              this.toastService.showSuccess(this.toastSummary, this.toastDetail);
            },
            error: (response) => {
              console.log('Update Failed', response.message);
              this.toastSummary = 'Error Updating';
              this.toastDetail = `client: ${newClient.name} not updated successfully`;
              this.toastService.showError(this.toastSummary, this.toastDetail);
            }
          })
      } else {
        console.log("Input values are not valid");
        this.toastSummary = 'Incorrect Values';
        this.toastDetail = `make sure correct update values are provided`;
        this.toastService.showError(this.toastSummary, this.toastDetail)
      }
  }

  onRowEditCancel(client: Client, index: number) {
      this.clients![index] = this.clonedClients[client.id as number];
      delete this.clonedClients[client.id as number];
  }

  onRowDelete(clientId: number){
    this.clientService.deleteClient(clientId).subscribe({
      next: (response) => {
        console.log(response.message);
        this.toastSummary = 'Success Deleting';
        this.toastDetail = `successfully deleted client with id: ${clientId}`;
        this.toastService.showSuccess(this.toastSummary, this.toastDetail);
        this.deleteDialogVisible = false;
        this.getClients();
      },
      error: (response) => {
        console.log(response.message);
        this.toastSummary = 'Error Deleting';
        this.toastDetail = `unsuccessful deletion process of client with id: ${clientId}`;
        this.toastService.showError(this.toastSummary, this.toastDetail);
      }
    })
  }

  addNewClient(clientName: string, totalBlockSessions: number){
    this.trainerId = this.accountService.currentUser()?.id ?? 0;
    const newClient = {
      name: clientName,
      totalBlockSessions: totalBlockSessions,
      trainerId: this.trainerId,
    }
    this.clientService.addClient(newClient).subscribe({
      next: (response) => {
        console.log(response.message)
        this.toastSummary = 'Success Adding';
        this.toastDetail = `added client: ${clientName} successfully`
        this.toastService.showSuccess(this.toastSummary, this.toastDetail);
        this.addDialogVisible = false;
        this.getClients();
      },
      error: (response) => {
        console.log(response.message)
        this.toastSummary = 'Error Adding';
        this.toastDetail = `adding client: ${clientName} was not successful`
        this.toastService.showError(this.toastSummary, this.toastDetail)
      }
    })
  }

  getClients(){
    this.trainerId = this.accountService.currentUser()?.id ?? 0;
    this.clientService.getAllTrainerClients(this.trainerId).subscribe({
      next: (response) => {
        this.clients = response.data ?? [];
      }
    })
  }

  showDialogForDelete(clientId: number, clientName: string){
    this.deleteDialogVisible = true;
    this.deleteClientId = clientId;
    this.deleteClientName = clientName;
  }

  showDialogForAdd(){
    this.addDialogVisible = true;
  }

  getActivities(isActive : boolean) : string {
    return isActive ? 'success' : 'danger';
  }

  getActivityLabel(isActive: boolean) : string {
    return isActive ? 'Active' : 'Inactive';
  }

}
