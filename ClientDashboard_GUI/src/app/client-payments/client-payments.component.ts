import { Component, inject, OnInit, ViewChild } from '@angular/core';
import { PaymentService } from '../services/payment.service';
import { Table, TableModule } from 'primeng/table';
import { CommonModule } from '@angular/common';
import { ButtonModule } from 'primeng/button';
import { SpinnerComponent } from '../spinner/spinner.component';
import { Toast } from 'primeng/toast';
import { InputTextModule } from 'primeng/inputtext';
import { Dialog } from 'primeng/dialog';
import { FormsModule } from '@angular/forms';
import { AutoCompleteModule } from 'primeng/autocomplete';
import { DatePicker } from 'primeng/datepicker';
import { InputNumberModule } from 'primeng/inputnumber';
import { TagModule } from 'primeng/tag';
import { SelectModule } from 'primeng/select';
import { ToggleButtonModule } from 'primeng/togglebutton';
import { Popover, PopoverModule } from 'primeng/popover';
import { Payment } from '../models/payment';
import { AccountService } from '../services/account.service';
import { ClientService } from '../services/client.service';
import { ToastService } from '../services/toast.service';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';
import { TrainerService } from '../services/trainer.service';
import { TooltipModule } from 'primeng/tooltip';

@Component({
  selector: 'app-client-payments',
  imports: [TableModule, CommonModule, ButtonModule, SpinnerComponent, Toast, InputTextModule,
     Dialog, FormsModule, AutoCompleteModule, DatePicker, InputNumberModule, TagModule, SelectModule,
     ToggleButtonModule, PopoverModule, IconFieldModule, InputIconModule, TooltipModule],
  templateUrl: './client-payments.component.html',
  styleUrl: './client-payments.component.css'
})
export class ClientPaymentsComponent implements OnInit {
   @ViewChild('op') op!: Popover;

  paymentService = inject(PaymentService);
  accountService = inject(AccountService);
  clientService = inject(ClientService);
  trainerService = inject(TrainerService);
  toastService = inject(ToastService);

  addDialogVisible: boolean = false;
  filterDialogVisible: boolean = false;
  deleteDialogVisible: boolean = false;
  autoPaymentSettingVisible: boolean = false;

  currentUserId: number = 0;
  clients: {id: number, name: string}[] = [];
  paymentStatuses: {label: string, value: boolean}[] = [];
  selectedClient: {id: number, name: string} = {id: 0, name: ""};
  selectedStatus: {name: string, value: boolean} | null = null;
  automaticPaymentsChecked: boolean = false;

  autoPaymentInfoText: string = "";
  amount: number = 0;
  currency: string = 'GBP';
  numberOfSessions: number = 1;
  paymentDate: Date | null = null;
  deletePaymentId: number = 0;

  payments : Payment[] | null = null;
  clonedPayments: { [s: string]: Payment } = {};

  last30DaysOnly: boolean = false;

  ngOnInit(): void {
    this.currentUserId = this.accountService.currentUser()?.id ?? 0;
    this.gatherClientNames();
    this.getAutoPaymentSettingStatus();
    this.setAutoPaymentInfoText();
    this.paymentStatuses = [
        {label: 'Confirmed', value: true},
        {label: 'Pending', value: false}
    ];
  }

  clear(table: Table){
          table.clear();
          this.gatherClientNames();
      }

    get displayedPayments(): Payment[] {
      if (!this.payments) return [];
      if (!this.last30DaysOnly) return this.payments;

      const cutoff = new Date();
      cutoff.setDate(cutoff.getDate() - 30);
      return this.payments.filter((payment) => new Date(payment.paymentDate) >= cutoff);
    }

    onRowEditInit(payment: Payment) {
            this.clonedPayments[payment.id as number] = { ...payment };
    }

    onRowEditSave(updatedPayment: Payment) {
      this.paymentService.updatePaymentInfo(updatedPayment).subscribe({
        next: (response) => {
          this.toastService.showSuccess('Success Updating Payment', response.message);
          this.gatherAllTrainerPayments();
        },
        error: (response) => {
          this.toastService.showError('Error Updating Payment', response.error.message);
        }
      })
    }

    onRowEditCancel(payment: Payment, index: number) {
        if(this.payments && this.clonedPayments[payment.id as number]){
            this.payments[index] = this.clonedPayments[payment.id as number];
            delete this.clonedPayments[payment.id as number];
        }
    }

    onRowDelete(paymentId: number){
      this.paymentService.deleteTrainerPayment(paymentId).subscribe({
        next: (response) => {
          this.gatherAllTrainerPayments();
          this.deleteDialogVisible = false;
          this.toastService.showSuccess("Success removing payment", response.message);
        },
        error: (response) => {
          this.toastService.showError('Error removing payment', response.error.message);
        }
      })

    }

    addNewPayment(selectedClientId: number, paymentAmount: number, numberOfSessions: number, paymentDate : Date, selectedStatus : {name: string, value: boolean} | null){
      // Frontend validation
      if(!this.validatePaymentAddFields(selectedClientId, paymentAmount, numberOfSessions, paymentDate, selectedStatus)){
        return;
      }

      var paymentInformation = {
        trainerId: this.currentUserId,
        clientId: selectedClientId,
        amount: paymentAmount,
        numberOfSessions: numberOfSessions,
        paymentDate: this.formatDateForApi(paymentDate),
        confirmed: selectedStatus?.value ?? false
      }

      this.paymentService.addTrainerPayment(paymentInformation).subscribe({
        next: (response) => {
          this.toastService.showSuccess('Success Adding Payment', response.message)
          this.addDialogVisible = false;
          this.gatherAllTrainerPayments();
          this.resetForm();
        },
        error: (response) => {
          this.toastService.showError('Error Adding Payment', response.error.message)
        }
      })
    }

    automaticPaymentsSettingSave(){
      this.trainerService.updateTrainerPaymentSetting(this.automaticPaymentsChecked ).subscribe({
        next: (response) => {
          this.toastService.showSuccess('Updated Payment Setting', response.message)
        },
        error: (response) => {
          this.toastService.showError('Error Updating Payment Setting', response.error.message)
        }
      })
    }

    filterClientPayments(){
      this.paymentService.filterOldClientPayments().subscribe({
        next: (response) => {
          if(response.data === 0) {
            this.toastService.showNeutral('Filtering Not Complete', response.message);
            this.filterDialogVisible = false;
          }
          else {
            this.toastService.showSuccess('Filtering Successful', response.message);
            this.filterDialogVisible = false;
            this.gatherAllTrainerPayments();
          }
        },
        error: (response) => {
          this.toastService.showError('Unsuccessful Filtering', response.error.message);
        }
      })

    }

    resetForm() {
      this.selectedClient = {id: 0, name: ""};
      this.selectedStatus = null;
      this.amount = 0;
      this.numberOfSessions = 1;
      this.paymentDate = null;
    }

    toggle(event: any) {
        this.op.toggle(event);
    }

    showDialogForAdd() {
      this.addDialogVisible = true;
    }

    showDialogForFilter(){
      this.filterDialogVisible = true;
    }

    showDialogForDelete(paymentId: number){
      this.deleteDialogVisible = true;
      this.deletePaymentId = paymentId;
    }

    showDialogForAutoPaymentSetting(){
      this.autoPaymentSettingVisible = true;
    }

    gatherAllTrainerPayments(){
      this.paymentService.getTrainerPayments().subscribe({
        next: (response) => {
          // add clientName property to each element
          this.payments = response.data.map((payment : Payment) => ({
            ...payment,
            clientName: this.clients.find(c => c.id === payment.clientId)?.name || `Removed Client`
          }));
        },
        error: (response) => {
          this.toastService.showError('Error Loading Payments', response.error.message);
        }
      })
    }

    gatherClientNames(){
    this.clientService.gatherClientNames(this.currentUserId).subscribe({
        next: (response) => {
            this.clients = response
            // loaded client names needed within below method hence calling it here
            this.gatherAllTrainerPayments();
        }
    });
    }

  getAutoPaymentSettingStatus(){
    this.trainerService.getAutoPaymentSettingStatus().subscribe({
      next: (response) => {
        this.automaticPaymentsChecked = response.data;
      }
    })
  }

  setAutoPaymentInfoText(){
    this.autoPaymentInfoText = "Enabling this setting will automatically create a payment, for a given client that finalises their monthly " +
    "session block. Further setting to a 'Pending' status for you to adjust, confirm or delete";
  }

   getActivities(isConfirmed : boolean) : string {
    return isConfirmed ? 'success' : 'info';
  }

  getActivityLabel(isConfirmed: boolean) : string {
    return isConfirmed ? 'Confirmed' : 'Pending';
  }

  formatDateForApi(date: Date | undefined): string {
  if (!date) return '';

  const day = date.getDate().toString().padStart(2, '0');
  const month = (date.getMonth() + 1).toString().padStart(2, '0');
  const year = date.getFullYear();

  return `${year}/${month}/${day}`;
  }

  validatePaymentAddFields(selectedClientId: number, paymentAmount: number, numberOfSessions: number, paymentDate: Date, selectedStatus: {name: string, value: boolean} | null): boolean {
    if(!selectedClientId || selectedClientId === 0){
      this.toastService.showError('Error adding payment', 'Must select a valid client');
      return false;
    }

    if(!paymentAmount || paymentAmount === null || paymentAmount <= 0){
      this.toastService.showError('Error adding payment', 'Must provide a valid payment amount');
      return false;
    }

    if(!numberOfSessions || numberOfSessions === null || numberOfSessions <= 0){
      this.toastService.showError('Error adding payment', 'Must provide a valid number of sessions');
      return false;
    }

    if(!paymentDate){
      this.toastService.showError('Error adding payment', 'Must provide a payment date');
      return false;
    }

    if(!selectedStatus || selectedStatus === null){
      this.toastService.showError('Error adding payment', 'Must select a payment status');
      return false;
    }

    return true;
  }

  // Six evenly-spread, complementary pastel pairs - same palette/bucketing as the client
  // roster's and workouts feed's avatars so colours stay consistent app-wide.
  private readonly avatarColorPalette: string[] = [
    'bg-primary-fixed text-primary',
    'bg-secondary-fixed text-secondary',
    'bg-violet-100 text-violet-700',
    'bg-amber-100 text-amber-700',
    'bg-rose-100 text-rose-700',
    'bg-cyan-100 text-cyan-700'
  ];

  getAvatarColorClass(clientName: string): string {
    const letter = (clientName?.charAt(0) ?? 'A').toUpperCase();
    const letterIndex = Math.max(0, letter.charCodeAt(0) - 'A'.charCodeAt(0));
    const bucketSize = 26 / this.avatarColorPalette.length;
    const index = Math.min(Math.floor(letterIndex / bucketSize), this.avatarColorPalette.length - 1);
    return this.avatarColorPalette[index];
  }

  getInitials(clientName: string): string {
    return (clientName?.charAt(0) ?? '').toUpperCase();
  }
}
