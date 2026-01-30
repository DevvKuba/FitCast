import { Component, inject, OnInit } from '@angular/core';
import { TableModule } from 'primeng/table';
import { CommonModule } from '@angular/common';
import { ClientService } from '../services/client.service';
import { Client } from '../models/client';
import { ConfirmationService, MessageService, SelectItem } from 'primeng/api';
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
import { RouterLink } from "@angular/router";
import { UserDto } from '../models/dtos/user-dto';
import { ConfirmPopupModule } from 'primeng/confirmpopup';
import { InputMask } from 'primeng/inputmask';

@Component({
  selector: 'app-client-info',
  imports: [TableModule, CommonModule, TagModule, SelectModule, ButtonModule, InputTextModule, FormsModule,
     Dialog, SpinnerComponent, Toast, Ripple, InputNumberModule, InputMask],
  providers: [MessageService, ConfirmationService],
  templateUrl: './client-info.component.html',
  styleUrl: './client-info.component.css'
})
export class ClientInfoComponent implements OnInit {
  private clientService = inject(ClientService);
  private toastService = inject(ToastService);
  private accountService = inject(AccountService);

  clients: Client[] | null = null;
  activityStatuses!: SelectItem[];
  clonedClients: { [s: string]: Client } = {};
  currentUserId: number = 0;

  trainerId : number = 0;
  deleteDialogVisible: boolean = false;
  addDialogVisible: boolean = false;
  phoneDialogVisible: boolean = false;
  newClientName : string = "";
  newPhoneNumber: string = "";
  editingPhoneNumber: string = "";
  editingClientName: string = "";
  editingClientId: number = 0;
  newActivity: boolean = true;
  newTotalBlockSessions : number = 0;
  deleteClientId: number = 0;
  deleteClientName: string = "";

  ngOnInit() {
      this.currentUserId = this.accountService.currentUser()?.id ?? 0;
      this.getClients();
      this.activityStatuses = [
      {label: 'Active', value: true},
      {label: 'Inactive', value: false}
  ];
  }

  onRowEditInit(client: Client) {
      this.clonedClients[client.id as number] = { ...client };

      this.clientService.getClientPhoneNumber(client.id).subscribe({
      next: (response) => {
        this.editingPhoneNumber = response.data?? "";
      }
    })
    }

  onRowEditSave(newClient: Client) {
      if (newClient.currentBlockSession >= 0 && newClient.totalBlockSessions > 0) {
          delete this.clonedClients[newClient.id as number];

          this.clientService.updateClient(newClient).subscribe({
            next: (response) => {
              this.toastService.showSuccess('Success Updating', response.message);
            },
            error: (response) => {
              this.toastService.showError('Error Updating', response.error.message);
            }
          })
      } else {
        this.toastService.showError('Incorrect Values', `Make sure correct update values are provided`)
      }
  }

  savePhoneNumber(){
    if(this.editingPhoneNumber !== ""){
      const clientInfo = {
      id: this.editingClientId,
      phoneNumber: this.editingPhoneNumber
    }
    this.clientService.updateClientPhoneNumber(clientInfo).subscribe({
      next: (response) => {
        this.toastService.showSuccess('Success Updating Phone Number', response.message);
        this.phoneDialogVisible = false;
      },
      error: (response) => {
        this.toastService.showError('Error Updating Phone Number', response.error.message);
      }
    })
    }
    
  }

  onRowEditCancel(client: Client, index: number) {
      this.clients![index] = this.clonedClients[client.id as number];
      delete this.clonedClients[client.id as number];
  }

  onRowDelete(clientId: number){
    this.clientService.deleteClient(clientId).subscribe({
      next: (response) => {
        this.toastService.showSuccess('Success Deleting', response.message);
        this.deleteDialogVisible = false;
        this.getClients();
      },
      error: (response) => {
        this.toastService.showError('Error Deleting', response.error.message);
      }
    })
  }


  addNewClient(clientName: string, totalBlockSessions: number, phoneNumber: string){
    const newClient = {
      firstName: clientName,
      totalBlockSessions: totalBlockSessions,
      phoneNumber: phoneNumber,
      trainerId: this.currentUserId,
    }
    this.clientService.addClient(newClient).subscribe({
      next: (response) => {
        this.toastService.showSuccess('Success Adding', response.message);
        this.addDialogVisible = false;
        this.getClients();
      },
      error: (response) => {
        this.toastService.showError('Error Adding', response.error.message)
      }
    })
  }

  getClients(){
    this.clientService.getAllTrainerClients(this.currentUserId).subscribe({
      next: (response) => {
        this.clients = response.data ?? [];
      }
    })
  }

  showPhoneDialog(client: Client){
    this.phoneDialogVisible = true;
    this.editingClientName = client.firstName;
    this.editingClientId = client.id;
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
