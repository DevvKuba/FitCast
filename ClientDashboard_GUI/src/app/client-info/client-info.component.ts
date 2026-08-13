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
import { Dialog } from 'primeng/dialog';
import { SpinnerComponent } from "../spinner/spinner.component";
import { Ripple } from 'primeng/ripple';
import { AccountService } from '../services/account.service';
import { ToastService } from '../services/toast.service';
import { InputNumberModule } from 'primeng/inputnumber';
import { InputMask } from 'primeng/inputmask';
import { NotificationService } from '../services/notification.service';
import { Popover, PopoverModule } from 'primeng/popover';
import { TooltipModule } from 'primeng/tooltip';
import { WorkoutService } from '../services/workout.service';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';

@Component({
  selector: 'app-client-info',
  imports: [TableModule, CommonModule, TagModule, SelectModule, ButtonModule, InputTextModule, FormsModule,
     Dialog, SpinnerComponent, Toast, Ripple, InputNumberModule, InputMask, PopoverModule, TooltipModule,
     IconFieldModule, InputIconModule],
  providers: [MessageService, ConfirmationService],
  templateUrl: './client-info.component.html',
  styleUrl: './client-info.component.css'
})
export class ClientInfoComponent implements OnInit {
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
  newClientName : string = "";
  newPhoneNumber: string = "";
  editingPhoneNumber: string = "";
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

  getProgressPercentage(client: Client): number {
    if (!client.totalBlockSessions) return 0;
    return Math.round((client.currentBlockSession / client.totalBlockSessions) * 100);
  }

  getInitials(client: Client): string {
    const first = client.firstName?.charAt(0) ?? '';
    const second = client.surname?.charAt(0) ?? client.firstName?.charAt(1) ?? '';
    return (first + second).toUpperCase();
  }

  // Six evenly-spread, complementary pastel pairs - assigned by first-letter bucket so the
  // same client name always lands on the same colour.
  private readonly avatarColorPalette: string[] = [
    'bg-primary-fixed text-primary',
    'bg-secondary-fixed text-secondary',
    'bg-violet-100 text-violet-700',
    'bg-amber-100 text-amber-700',
    'bg-rose-100 text-rose-700',
    'bg-cyan-100 text-cyan-700'
  ];

  getAvatarColorClass(client: Client): string {
    const letter = (client.firstName?.charAt(0) ?? 'A').toUpperCase();
    const letterIndex = Math.max(0, letter.charCodeAt(0) - 'A'.charCodeAt(0));
    const bucketSize = 26 / this.avatarColorPalette.length;
    const index = Math.min(Math.floor(letterIndex / bucketSize), this.avatarColorPalette.length - 1);
    return this.avatarColorPalette[index];
  }

  getAddedLabel(createdAt: string): string {
    const diffDays = Math.floor((Date.now() - new Date(createdAt).getTime()) / (1000 * 60 * 60 * 24));

    if (diffDays < 1) return 'Added today';
    if (diffDays < 7) return `Added ${diffDays} day${diffDays === 1 ? '' : 's'} ago`;

    const diffWeeks = Math.floor(diffDays / 7);
    if (diffWeeks < 5) return `Added ${diffWeeks} week${diffWeeks === 1 ? '' : 's'} ago`;

    const diffMonths = Math.floor(diffDays / 30);
    if (diffMonths < 12) return `Added ${diffMonths} month${diffMonths === 1 ? '' : 's'} ago`;

    const diffYears = Math.floor(diffDays / 365);
    return `Added ${diffYears} year${diffYears === 1 ? '' : 's'} ago`;
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
