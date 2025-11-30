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

@Component({
  selector: 'app-client-info',
  imports: [TableModule, CommonModule, TagModule, SelectModule, ButtonModule, InputTextModule, FormsModule,
     Dialog, SpinnerComponent, Toast, Ripple, InputNumberModule, ConfirmPopupModule],
  providers: [MessageService, ConfirmationService],
  templateUrl: './client-info.component.html',
  styleUrl: './client-info.component.css'
})
export class ClientInfoComponent implements OnInit {
  private clientService = inject(ClientService);
  private toastService = inject(ToastService);
  private accountService = inject(AccountService);
  private confirmationService = inject(ConfirmationService);

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
  newActivity: boolean = true;
  newTotalBlockSessions : number = 0;
  toastSummary: string = "";
  toastDetail: string = "";
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
    }

  onRowEditSave(newClient: Client) {
      if (newClient.currentBlockSession >= 0 && newClient.totalBlockSessions > 0) {
          delete this.clonedClients[newClient.id as number];

          this.clientService.updateClient(newClient).subscribe({
            next: (response) => {
              console.log('Client updated successfully', response.message);
              this.toastSummary = 'Success Updating';
              this.toastDetail = `updated client: ${newClient.firstName} successfully`;
              this.toastService.showSuccess(this.toastSummary, this.toastDetail);
            },
            error: (response) => {
              console.log('Update Failed', response.message);
              this.toastSummary = 'Error Updating';
              this.toastDetail = `client: ${newClient.firstName} not updated successfully`;
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

  savePhoneNumber(){

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


  addNewClient(clientName: string, totalBlockSessions: number, phoneNumber: string){
    const newClient = {
      firstName: clientName,
      totalBlockSessions: totalBlockSessions,
      phoneNumber: phoneNumber,
      trainerId: this.currentUserId,
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
    this.clientService.getAllTrainerClients(this.currentUserId).subscribe({
      next: (response) => {
        this.clients = response.data ?? [];
      }
    })
  }

  showPhoneDialog(client: Client){
    this.phoneDialogVisible = true;
    this.editingPhoneNumber = client.phoneNumber ?? "";
    this.editingClientName = client.firstName;
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
