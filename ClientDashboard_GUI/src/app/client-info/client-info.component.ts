import { Component, inject, OnInit, ViewChild } from '@angular/core';
import { Table, TableModule } from 'primeng/table';
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
import { NotificationService } from '../services/notification.service';
import { Popover, PopoverModule } from 'primeng/popover';
import { TooltipModule } from 'primeng/tooltip';
import { TWO_THIRDS_PI } from 'chart.js/helpers';
import { WorkoutService } from '../services/workout.service';

@Component({
  selector: 'app-client-info',
  imports: [TableModule, CommonModule, TagModule, SelectModule, ButtonModule, InputTextModule, FormsModule,
     Dialog, SpinnerComponent, Toast, Ripple, InputNumberModule, InputMask, PopoverModule, TooltipModule],
  providers: [MessageService, ConfirmationService],
  templateUrl: './client-info.component.html',
  styleUrl: './client-info.component.css'
})
export class ClientInfoComponent implements OnInit {
  @ViewChild('currentSessionPopover') currentSessionPopover!: Popover;
  @ViewChild('phoneNumberInfoPopover') phoneNumberInfoPopover!: Popover;

  showPopover(event: MouseEvent, popover: Popover){
    popover.show(event, event.currentTarget);
  }

  hidePopover(popover: Popover){
    popover.hide();
  }
  
  private clientService = inject(ClientService);
  private workoutService = inject(WorkoutService);
  private toastService = inject(ToastService);
  private accountService = inject(AccountService);
  private notificationService = inject(NotificationService);

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
  phoneNumberInputInfo: string = "test";

  ngOnInit() {
      this.currentUserId = this.accountService.currentUser()?.id ?? 0;
      this.getClients();
      this.setPhoneNumberInfoText();
      this.activityStatuses = [
      {label: 'Active', value: true},
      {label: 'Inactive', value: false}
  ];
  }

  clear(table: Table) {
    table.clear();
    this.getClients();
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

          newClient.phoneNumber = this.editingPhoneNumber;
          this.clientService.updateClient(newClient).subscribe({
            next: (response) => {
              this.toastService.showSuccess('Success Updating', response.message);
              this.notificationService.refreshUnreadCount(this.currentUserId);
              this.getClients();
            },
            error: (response) => {
              this.toastService.showError('Error Updating', response.error.message);
            }
          })
      } else {
        this.toastService.showError('Incorrect Values', `Make sure correct update values are provided`)
      }
  }

  toggleForInfo(event: any) {
    this.phoneNumberInfoPopover.toggle(event);
  }

  setPhoneNumberInfoText(){
    this.phoneNumberInputInfo = "To ensure the client phone number is saved make sure to save the record itself"
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
    const validationSuccessful = this.validateClientAddFields(clientName, totalBlockSessions);
    if(!validationSuccessful){
      return;
    }

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

  onQuickAddForClient(client: Client) {
    
    this.workoutService.quickAddWorkout(client).subscribe({
      next: (response) => {
        this.getClients();
         this.notificationService.refreshUnreadCount(this.currentUserId);
        this.toastService.showSuccess('Quick Add Complete', response.message);
      },
      error: (response) => {
        this.toastService.showSuccess('Quick Add Unsuccessful', response.error);
      }
    })
  }

  getActivities(isActive : boolean) : string {
    return isActive ? 'success' : 'danger';
  }

  getActivityLabel(isActive: boolean) : string {
    return isActive ? 'Active' : 'Inactive';
  }

  validateClientAddFields(clientName: string, totalBlockSessions: number) : boolean {
    if(!clientName || clientName.trim() === ''){
      this.toastService.showError('Error Adding client', 'Must provide the client name');
      return false;
    }
    
    if(!totalBlockSessions || totalBlockSessions === null){
      this.toastService.showError('Error Adding client', 'Must provide the client total block sessions');
      return false;
    }
    return true;
  }

}
