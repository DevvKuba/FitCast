import { Component, inject, OnInit, ViewChild } from '@angular/core';
import { PaymentService } from '../services/payment.service';
import { TableModule } from 'primeng/table';
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
import { PasswordModule } from 'primeng/password';
import { Popover, PopoverModule } from 'primeng/popover';
import { Payment } from '../models/payment';
import { Client } from '../models/client';
import { AccountService } from '../services/account.service';
import { ClientWorkouts } from '../client-workouts/client-workouts.component';
import { ClientService } from '../services/client.service';
import { ToastService } from '../services/toast.service';
import { ClientNamePipe } from '../pipes/client-name.pipe';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';
import { TrainerService } from '../services/trainer.service';

@Component({
  selector: 'app-client-payments',
  imports: [TableModule, CommonModule, ButtonModule, SpinnerComponent, Toast, InputTextModule,
     Dialog, FormsModule, AutoCompleteModule, DatePicker, InputNumberModule, TagModule, SelectModule,
     ToggleButtonModule, PasswordModule, PopoverModule, IconFieldModule, InputIconModule],
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

  first = 0; 
  rows = 10;

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

  next() {
        this.first = this.first + this.rows;
    }

    prev() {
        this.first = this.first - this.rows;
    }

    reset() {
        this.first = 0;
    }

    pageChange(event: { first: number; rows: number; }) {
        this.first = event.first;
        this.rows = event.rows;
    }

    isLastPage(): boolean {
        return this.payments ? this.first + this.rows >= this.payments.length : true;
    }

    isFirstPage(): boolean {
        return this.payments ? this.first === 0 : true;
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
      var paymentInformation = {
        trainerId: this.currentUserId,
        clientId: selectedClientId,
        amount: paymentAmount,
        numberOfSessions: numberOfSessions,
        paymentDate: this.formatDateForApi(paymentDate),
        confirmed: selectedStatus?.value ?? false
      }
      console.log('Payment Information:', paymentInformation);
      
      // take in values then pass then into a payment-add-dto 
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
      this.trainerService.updateTrainerPaymentSetting(this.currentUserId, this.automaticPaymentsChecked ).subscribe({
        next: (response) => {
          this.toastService.showSuccess('Updated Payment Setting', response.message)
        },
        error: (response) => {
          this.toastService.showError('Error Updating Payment Setting', response.error.message)
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

    showDialogForDelete(paymentId: number){
      // can use payment id to set a payment id variable then use 
      this.deleteDialogVisible = true;
      this.deletePaymentId = paymentId;
      // within on RowDelete to delete payment with that id
    }

    showDialogForAutoPaymentSetting(){
      this.autoPaymentSettingVisible = true;
    }

    gatherAllTrainerPayments(){
      this.paymentService.getTrainerPayments(this.currentUserId).subscribe({
        next: (response) => {
          // add clientName property to each element
          this.payments = response.data.map((payment : Payment) => ({
            ...payment,
            clientName: this.clients.find(c => c.id === payment.clientId)?.name || `Client: #${payment.clientId}`
          }));
        },
        error: (response) => {
          console.log(response.error.message)
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

    gatherStatuses(){
    this.paymentStatuses = [
        {label: 'Confirmed', value: true},
        {label: 'Pending', value: false}
    ];
  }

  getAutoPaymentSettingStatus(){
    this.trainerService.getAutoPaymentSettingStatus(this.currentUserId).subscribe({
      next: (response) => {
        this.automaticPaymentsChecked = response.data;
      }
    })
  }

  setAutoPaymentInfoText(){
    this.autoPaymentInfoText = "Enabling this setting will automatically create a payment, for a given client that finalises their monthly " +
    "session block. Further setting to a 'Pending' status for you to adjust, confirm or delete"
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

}
